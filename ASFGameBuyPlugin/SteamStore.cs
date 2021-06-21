﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Integration;

namespace ASFGameBuyPlugin
{
    internal class SteamStore
    {
        private Bot Bot;
        internal SteamStore(Bot bot) => Bot = bot;

        private async Task<AppInfo[]?> GetAppInfo(uint appID)
        {
            if (appID == 0)
                throw new ArgumentException($"{nameof(appID)} cannot be 0");

            if (!Bot.IsConnectedAndLoggedOn)
            {
                Bot.ArchiLogger.LogGenericError("Is currently not logged on");
                return null;
            }

            const string STORE_URL = "/app/{0}/";
            Uri storeUri = new(ArchiWebHandler.SteamStoreURL, string.Format(STORE_URL, appID));

            // Bypass age check by set Jan-1-1990 cookie header
            Bot.ArchiWebHandler.WebBrowser.CookieContainer.Add(new Cookie()
            {
                Name = "birthtime",
                Value = "628466401",
                Domain = ArchiWebHandler.SteamStoreURL.Host
            });

            var response = await Bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(storeUri);
            if (response == null || response.StatusCode != HttpStatusCode.OK)
            {
                Bot.ArchiLogger.LogGenericError($"Unable to download app {appID} page");
                return null;
            }

            const string META_SELECTOR = "meta[property = 'og:url']";
            var meta = response.Content.QuerySelector(META_SELECTOR);

            if (meta == null)
            {
                Bot.ArchiLogger.LogGenericError($"Unable to get referer link for {appID} page. Selector null");
                return null;
            }

            string refererLink = meta.GetAttribute("content");
            if (string.IsNullOrWhiteSpace(refererLink))
            {
                Bot.ArchiLogger.LogGenericError($"Unable to get referer link for {appID} page. Content is empty");
                return null;
            }

            const string BUY_FORM_SELECTOR = ".game_area_purchase_game_wrapper > .game_area_purchase_game form";
            var buyForms = response.Content.QuerySelectorAll(BUY_FORM_SELECTOR);

            if (buyForms == null || buyForms.Length == 0)
            {
                Bot.ArchiLogger.LogGenericError($"No buy form found");
                return null;
            }

            List<AppInfo> appInfos = new(buyForms.Length);
            foreach (var form in buyForms)
            {
                if (form == null)
                    continue;

                var inputs = form.QuerySelectorAll("input");
                if (inputs == null || inputs.Length == 0)
                    continue;

                AppForm serializableForm = new();
                foreach (var input in inputs)
                {
                    if (input == null)
                        continue;

                    string name = input.GetAttribute("name");
                    string value = input.GetAttribute("value");

                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                        serializableForm.Add(name, value);
                }

                // SubID  has the highest priority for the plugin to work
                if (string.IsNullOrEmpty(serializableForm.SubID))
                    continue;

                const string PRICE_ATTRIBUTE = "data-price-final";
                var purchaseElement = form.QuerySelector($".game_purchase_action [{PRICE_ATTRIBUTE}]");

                if (purchaseElement == null)
                    continue;

                string priceString = purchaseElement.GetAttribute(PRICE_ATTRIBUTE);
                if (string.IsNullOrWhiteSpace(priceString))
                    continue;

                if (!ulong.TryParse(priceString, out ulong price))
                    continue;

                appInfos.Add(new (price, serializableForm, new(refererLink)));
            }

            return appInfos.ToArray();
        }

        private async Task<Uri?> AddToCart(AppForm form, Uri referer)
        {
            if (form.Count == 0 || string.IsNullOrEmpty(form.SubID))
                throw new ArgumentException($"{nameof(form)} is invalid");

            const string CART_URL = "/cart/";
            Uri cartUri = new(ArchiWebHandler.SteamStoreURL, CART_URL);

            var response = await Bot.ArchiWebHandler.UrlPostToHtmlDocumentWithSession(cartUri, data: form, referer: referer);

            if (response == null || response.StatusCode != HttpStatusCode.OK)
            {
                Bot.ArchiLogger.LogGenericError($"Unable to add {form.SubID} form to cart");
                return null;
            }

            const string PURCHASE_BTN_SELECTOR = "#btn_purchase_self";
            var btn = response.Content.QuerySelector(PURCHASE_BTN_SELECTOR);

            if (btn == null)
            {
                Bot.ArchiLogger.LogGenericError($"Unable to find purchase link for {form.SubID} form");
                return null;
            }

            string purchaseLink = btn.GetAttribute("href");

            if (string.IsNullOrWhiteSpace(purchaseLink))
            {
                Bot.ArchiLogger.LogGenericError($"Unable to get purchase link for {form.SubID} form");
                return null;
            }

            return new(purchaseLink);
        }

        private async Task<TransactionInfo?> Checkout(Uri checkoutUri)
        {
            var response = await Bot.ArchiWebHandler.WebBrowser.UrlGetToHtmlDocument(checkoutUri);

            if (response == null || response.StatusCode != HttpStatusCode.OK)
            {
                Bot.ArchiLogger.LogGenericError($"Unable to checkout {checkoutUri.AbsoluteUri}");
                return null;
            }

            string errorMsg = $"Unable to checkout {checkoutUri.AbsoluteUri}";
            string shoppingCartIDString = GetValueAttributeBySelectorID("shopping_cart_gid");
            if (string.IsNullOrEmpty(shoppingCartIDString))
            {
                Bot.ArchiLogger.LogGenericError(errorMsg);
                return null;
            }
            // Additional check that value is valid
            if (!ulong.TryParse(shoppingCartIDString, out ulong shoppingCartID))
            {
                Bot.ArchiLogger.LogGenericError($"{errorMsg}. {nameof(shoppingCartID)} is not ulong");
                return null;
            }

            string transactionIDString = GetValueAttributeBySelectorID("transaction_id");
            if (string.IsNullOrEmpty(transactionIDString))
            {
                Bot.ArchiLogger.LogGenericError(errorMsg);
                return null;
            }
            if (!long.TryParse(transactionIDString, out long transactionID))
            {
                Bot.ArchiLogger.LogGenericError($"{errorMsg}. {nameof(transactionID)} is not long");
                return null;
            }

            string paymentMethod = GetValueAttributeBySelectorID("payment_method");
            if (string.IsNullOrEmpty(paymentMethod))
            {
                Bot.ArchiLogger.LogGenericError(errorMsg);
                return null;
            }

            string shippingCountry = GetValueAttributeBySelectorID("shipping_country");
            if (string.IsNullOrEmpty(shippingCountry))
            {
                Bot.ArchiLogger.LogGenericError(errorMsg);
                return null;
            }

            return new(shoppingCartID, transactionID, paymentMethod, shippingCountry);

            string GetValueAttributeBySelectorID(string selector)
            {
                if (string.IsNullOrWhiteSpace(selector))
                    throw new ArgumentException($"{nameof(selector)} is empty");

                if (response == null)
                    throw new InvalidOperationException($"{nameof(response)} is null");

                var elementByID = response.Content.QuerySelector($"#{selector}");

                if (elementByID == null)
                {
                    Bot.ArchiLogger.LogGenericError($"Unable to find by selector: {selector}");
                    return string.Empty;
                }

                string value = elementByID.GetAttribute("value");

                if (string.IsNullOrWhiteSpace(value))
                {
                    Bot.ArchiLogger.LogGenericError($"Value attribute is missing in element by selector: {selector}");
                    return string.Empty;
                }

                return value;
            }
        }

        private async Task<JsonData.InitTransactionJsonResponse?> InitTransaction(TransactionInfo transactionInfo, Uri referer)
        {
            Dictionary<string, string> postData = new()
            {
                { "gidShoppingCart", transactionInfo.ShoppingCart.ToString() },
                { "gidReplayOfTransID", transactionInfo.TransactionID.ToString() },
                { "PaymentMethod", transactionInfo.PaymentMethod },
                { "abortPendingTransactions", "0" },
                { "bHasCardInfo", "0" },
                { "CardNumber", string.Empty },
                { "CardExpirationYear", string.Empty },
                { "CardExpirationMonth", string.Empty },
                { "FirstName", string.Empty },
                { "LastName", string.Empty },
                { "Address", string.Empty },
                { "AddressTwo", string.Empty },
                { "Country", transactionInfo.ShippingCountry },
                { "City", string.Empty },
                { "State", string.Empty },
                { "PostalCode", string.Empty },
                { "Phone", string.Empty },
                { "ShippingFirstName", string.Empty },
                { "ShippingLastName", string.Empty },
                { "ShippingAddress", string.Empty },
                { "ShippingAddressTwo", string.Empty },
                { "ShippingCountry", transactionInfo.ShippingCountry },
                { "ShippingCity", string.Empty },
                { "ShippingState", string.Empty },
                { "ShippingPostalCode", string.Empty },
                { "ShippingPhone", string.Empty },
                { "bIsGift", "0" },
                { "GifteeAccountID", "0" },
                { "GifteeEmail", string.Empty },
                { "GifteeName", string.Empty },
                { "GiftMessage", string.Empty },
                { "Sentiment", string.Empty },
                { "Signature", string.Empty },
                { "ScheduledSendOnDate", "0" },
                { "BankAccount", string.Empty },
                { "BankCode", string.Empty },
                { "BankIBAN", string.Empty },
                { "BankBIC", string.Empty },
                { "TPBankID", string.Empty },
                { "bSaveBillingAddress", "1" },
                { "gidPaymentID", string.Empty },
                { "bUseRemainingSteamAccount", "1" },
                { "bPreAuthOnly", "0" }
            };

            const string INITTRANSACTION_URL = "/checkout/inittransaction/";
            Uri uri = new(ArchiWebHandler.SteamStoreURL, INITTRANSACTION_URL);

            var response = await Bot.ArchiWebHandler.UrlPostToJsonObjectWithSession<JsonData.InitTransactionJsonResponse>(uri, data: postData, referer: referer);

            if (response == null || response.StatusCode != HttpStatusCode.OK)
            {
                Bot.ArchiLogger.LogGenericError($"Unable to initiate transaction {uri.AbsoluteUri}");
                return null;
            }

            if (response.Content.Success != 1 || string.IsNullOrWhiteSpace(response.Content.TransactionID))
            {
                Bot.ArchiLogger.LogGenericError($"Initiate transaction at {uri.AbsoluteUri} returns unsuccessful data / {response.Content.Success} / {response.Content.TransactionID ?? string.Empty}");
                return null;
            }

            return response.Content;
        }

        private async Task<>

        internal record AppInfo(ulong Price, AppForm AppForm, Uri Referer);

        internal class AppForm: Dictionary<string, string>
        {
            internal string SubID 
            {
                get
                {
                    if (!ContainsKey("subid"))
                        return string.Empty;
                    return this["subid"];
                }
            }
        }

        internal class TransactionInfo
        {
            internal ulong ShoppingCart { get; init; }
            internal long TransactionID { get; init; }
            internal string PaymentMethod { get; init; }
            internal string ShippingCountry { get; init; }

            internal TransactionInfo(ulong shoppingCart, long transactionID, string paymentMethod, string shippingCountry)
            {
                if (string.IsNullOrWhiteSpace(paymentMethod) || string.IsNullOrWhiteSpace(shippingCountry))
                    throw new ArgumentException($"{nameof(paymentMethod)} and {nameof(shippingCountry)} must be filled");

                ShoppingCart = shoppingCart;
                TransactionID = transactionID;
                PaymentMethod = paymentMethod;
                ShippingCountry = shippingCountry;
            }
        }
    }
}
