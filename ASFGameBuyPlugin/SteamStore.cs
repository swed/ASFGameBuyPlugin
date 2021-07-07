using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;
using System.Net;
using System.Threading.Tasks;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Integration;

// TODO:
// Refactoring

namespace ASFGameBuyPlugin
{
    internal sealed class SteamStore
    {
        private Bot Bot;
        internal SteamStore(Bot bot)
        {
            Bot = bot;

            // Bypass age check by set Jan-1-1990 cookie header
            Bot.ArchiWebHandler.WebBrowser.CookieContainer.Add(new Cookie()
            {
                Name = "birthtime",
                Value = "628466401",
                Domain = ArchiWebHandler.SteamStoreURL.Host
            });
        }

        internal async Task<bool> BuyGameAsync(uint appID, uint subID)
        {
            if (appID == 0 || subID == 0)
                return false;

            var (appInfos, referer) = await GetAppInfoAsync(appID);

            if (appInfos == null || referer == null)
                return false;

            AppInfo? suitaleAppInfo = null;
            foreach (var appInfo in appInfos)
            {
                if (appInfo == null)
                    continue;

                if (appInfo.AppForm.SubID == subID)
                {
                    suitaleAppInfo = appInfo;
                    break;
                }
            }

            if (suitaleAppInfo == null)
            {
                Bot.ArchiLogger.LogGenericError($"Unable to found {subID} for {appID}");
                return false;
            }

            if ((long)suitaleAppInfo.Price > Bot.WalletBalance)
            {
                Bot.ArchiLogger.LogGenericError($"Unable to purchase {subID} for {appID}. Price: {suitaleAppInfo.Price / 100 :F2} > Bot balance: {Bot.WalletBalance / 100 :F2}");
                return false;
            }


            Bot.ArchiLogger.LogGenericInfo($"Purchasing game {appID} / {subID}. Price: {suitaleAppInfo.Price / 100:F2}. Bot balance: {Bot.WalletBalance / 100:F2}");

            return await ProceedPurchase(suitaleAppInfo, referer);
        }

        internal async Task<bool> BuyBundleAsync(uint bundleID)
        {
            if (bundleID == 0)
                return false;

            var (appInfo, referer) = await GetBundleInfoAsync(bundleID);

            if (appInfo == null || referer == null)
                return false;

            if ((long)appInfo.Price > Bot.WalletBalance)
            {
                Bot.ArchiLogger.LogGenericError($"Unable to purchase bundle {bundleID}. Price: {appInfo.Price / 100:F2} > Bot balance: {Bot.WalletBalance / 100:F2}");
                return false;
            }


            Bot.ArchiLogger.LogGenericInfo($"Purchasing bundle {bundleID}. Price: {appInfo.Price / 100:F2}. Bot balance: {Bot.WalletBalance / 100:F2}");

            ClearCart();

            return await ProceedPurchase(appInfo, referer);
        }

        private async Task<bool> ProceedPurchase(AppInfo appInfo, Uri referer)
        {
            var checkoutLink = await AddToCartAsync(appInfo, referer);
            if (checkoutLink == null)
                return false;

            var transactionInfo = await CheckoutAsync(checkoutLink);
            if (transactionInfo == null)
                return false;

            var initTransactionInfo = await InitTransactionAsync(transactionInfo, checkoutLink);
            if (initTransactionInfo == null)
                return false;

            var finalPriceInfo = await GetFinalPriceAsync(transactionInfo, initTransactionInfo, checkoutLink);
            if (finalPriceInfo == null)
                return false;

            var finalizeTransaction = await FinalizeTransactionAsync(initTransactionInfo, checkoutLink);

            ClearCart();

            return finalizeTransaction;
        }

        internal bool ClearCart()
        {
            const string SHOPPING_CART_GID = "shoppingCartGID";

            var cookies = Bot.ArchiWebHandler.WebBrowser.CookieContainer.GetCookies(ArchiWebHandler.SteamStoreURL);
            if (cookies == null)
            {
                Bot.ArchiLogger.LogGenericWarning("Unable to get cookies container");
                return false;
            }

            foreach (Cookie cookie in cookies)
            {
                if (cookie.Name == SHOPPING_CART_GID)
                {
                    if (cookie.Value == "-1")
                    {
                        Bot.ArchiLogger.LogGenericWarning("No need to clear shopping cart");
                        return false;
                    }

                    cookie.Value = "-1";
                    return true;
                }
            }

            Bot.ArchiLogger.LogGenericWarning($"Unable to find \"{SHOPPING_CART_GID}\" cookie");
            return false;
        }

        internal async Task<bool> BuyInGameItemAsync(uint appID, ulong itemID, uint quantity)
        {
            if (appID == 0 || itemID == 0)
                return false;

            var inGameInfo = await GetInGameInfoAsync(appID, itemID, quantity);

            if (inGameInfo == null)
                return false;

            if ((long)inGameInfo.Price > Bot.WalletBalance)
            {
                Bot.ArchiLogger.LogGenericError($"Unable to purchase {itemID} (x{quantity}) for {appID}. Price: {inGameInfo.Price / 100:F2} > Bot balance: {Bot.WalletBalance / 100:F2}");
                return false;
            }

            Bot.ArchiLogger.LogGenericInfo($"Purchasing in-game item {appID} / {itemID} / {quantity}. Price: {inGameInfo.Price / 100:F2}. Bot balance: {Bot.WalletBalance / 100:F2}");

            inGameInfo.InGameForm.Approved = true;

            return await ApproveInGamePurchaseAsync(inGameInfo.InGameForm);
        }

        private async Task<(AppInfo[]?, Uri?)> GetAppInfoAsync(uint appID)
        {
            if (appID == 0)
                throw new ArgumentException($"{nameof(appID)} cannot be 0");

            if (!Bot.IsConnectedAndLoggedOn)
            {
                Bot.ArchiLogger.LogGenericError("Is currently not logged on");
                return (null, null);
            }

            Uri storeUri = new(ArchiWebHandler.SteamStoreURL, $"/app/{appID}/");

            var response = await Bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(storeUri);
            if (response == null || response.StatusCode != HttpStatusCode.OK)
            {
                Bot.ArchiLogger.LogGenericError($"Unable to download app {appID} page");
                return (null, null);
            }

            const string META_SELECTOR = "meta[property = 'og:url']";
            var meta = response.Content.QuerySelector(META_SELECTOR);

            if (meta == null)
            {
                Bot.ArchiLogger.LogGenericError($"Unable to get link for {appID} page. Selector null");
                return (null, null);
            }

            string refererLink = meta.GetAttribute("content");
            if (string.IsNullOrWhiteSpace(refererLink))
            {
                Bot.ArchiLogger.LogGenericError($"Unable to get link for {appID} page. Content is empty");
                return (null, null);
            }

            const string BUY_SELECTOR = ".game_area_purchase_game_wrapper > .game_area_purchase_game";
            var buyElements = response.Content.QuerySelectorAll(BUY_SELECTOR);

            if (buyElements == null || buyElements.Length == 0)
            {
                Bot.ArchiLogger.LogGenericError($"No buy elements found");
                return (null, null);
            }

            List<AppInfo> appInfos = new(buyElements.Length);
            foreach (var buyElement in buyElements)
            {
                if (buyElement == null)
                    continue;

                var buyForm = buyElement.QuerySelector("form");

                if (buyForm == null)
                    continue;

                var inputs = buyForm.QuerySelectorAll("input");
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

                // SubID has the highest priority
                if (serializableForm.SubID == 0)
                    continue;

                const string PRICE_ATTRIBUTE = "data-price-final";
                var purchaseElement = buyElement.QuerySelector($".game_purchase_action [{PRICE_ATTRIBUTE}]");

                if (purchaseElement == null)
                    continue;

                string priceString = purchaseElement.GetAttribute(PRICE_ATTRIBUTE);
                if (string.IsNullOrWhiteSpace(priceString))
                    continue;

                if (!ulong.TryParse(priceString, out ulong price))
                    continue;

                appInfos.Add(new (price, serializableForm));
            }

            return (appInfos.ToArray(), new(refererLink));
        }

        private async Task<(AppInfo?, Uri?)> GetBundleInfoAsync(uint bundleID)
        {
            if (bundleID == 0)
                throw new ArgumentException($"{nameof(bundleID)} cannot be 0");

            if (!Bot.IsConnectedAndLoggedOn)
            {
                Bot.ArchiLogger.LogGenericError("Is currently not logged on");
                return (null, null);
            }

            Uri storeUri = new(ArchiWebHandler.SteamStoreURL, $"/bundle/{bundleID}/");

            var response = await Bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(storeUri);
            if (response == null || response.StatusCode != HttpStatusCode.OK)
            {
                Bot.ArchiLogger.LogGenericError($"Unable to download bundle {bundleID} page");
                return (null, null);
            }

            const string META_SELECTOR = "meta[property = 'og:url']";
            var meta = response.Content.QuerySelector(META_SELECTOR);

            if (meta == null)
            {
                Bot.ArchiLogger.LogGenericError($"Unable to get link for {bundleID} page. Selector null");
                return (null, null);
            }

            string refererLink = meta.GetAttribute("content");
            if (string.IsNullOrWhiteSpace(refererLink))
            {
                Bot.ArchiLogger.LogGenericError($"Unable to get link for {bundleID} page. Content is empty");
                return (null, null);
            }

            const string BUY_SELECTOR = "#game_area_purchase > .game_area_purchase_game.bundle.ds_no_flags[data-ds-bundle-data]";
            var buyElement = response.Content.QuerySelector(BUY_SELECTOR);

            if (buyElement == null)
            {
                Bot.ArchiLogger.LogGenericError($"No buy elements found");
                return (null, null);
            }

            var buyForm = buyElement.QuerySelector("form");

            if (buyForm == null)
            {
                Bot.ArchiLogger.LogGenericError($"No buy form found");
                return (null, null);
            }

            var inputs = buyForm.QuerySelectorAll("input");
            if (inputs == null || inputs.Length == 0)
            {
                Bot.ArchiLogger.LogGenericError("No inputs found");
                return (null, null);
            }

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

            // BundleID has the highest priority
            if (serializableForm.BundleID == 0)
            {
                Bot.ArchiLogger.LogGenericError("Unable to find valid bundle");
                return (null, null);
            }

            const string PRICE_ATTRIBUTE = "data-price-final";
            var purchaseElement = buyElement.QuerySelector($".game_purchase_action [{PRICE_ATTRIBUTE}]");

            if (purchaseElement == null)
            {
                Bot.ArchiLogger.LogGenericError("Unable to find purchase element");
                return (null, null);
            }

            string priceString = purchaseElement.GetAttribute(PRICE_ATTRIBUTE);
            if (string.IsNullOrWhiteSpace(priceString))
            {
                Bot.ArchiLogger.LogGenericError("Unable to find price attribute");
                return (null, null);
            }

            if (!ulong.TryParse(priceString, out ulong price))
            {
                Bot.ArchiLogger.LogGenericError("Price string is not valid");
                return (null, null);
            }

            return (new(price, serializableForm), new(refererLink));
        }

        private async Task<Uri?> AddToCartAsync(AppInfo appInfo, Uri referer)
        {
            if (appInfo.AppForm.Count == 0 || (appInfo.AppForm.SubID == 0 && appInfo.AppForm.BundleID == 0))
                throw new ArgumentException($"{nameof(appInfo)} is invalid");

            if (!Bot.IsConnectedAndLoggedOn)
            {
                Bot.ArchiLogger.LogGenericError("Is currently not logged on");
                return null;
            }

            const string CART_URL = "/cart/";
            Uri cartUri = new(ArchiWebHandler.SteamStoreURL, CART_URL);

            var response = await Bot.ArchiWebHandler.UrlPostToHtmlDocumentWithSession(cartUri, data: appInfo.AppForm, referer: referer);

            var subID = appInfo.AppForm.IsBundle ? appInfo.AppForm.BundleID : appInfo.AppForm.SubID;

            if (response == null || response.StatusCode != HttpStatusCode.OK)
            {
                Bot.ArchiLogger.LogGenericError($"Unable to add {subID} form to cart");
                return null;
            }

            const string PURCHASE_BTN_SELECTOR = "#btn_purchase_self";
            var btn = response.Content.QuerySelector(PURCHASE_BTN_SELECTOR);

            if (btn == null)
            {
                Bot.ArchiLogger.LogGenericError($"Unable to find purchase link for {subID} form");
                return null;
            }

            string purchaseLink = btn.GetAttribute("href");

            if (string.IsNullOrWhiteSpace(purchaseLink))
            {
                Bot.ArchiLogger.LogGenericError($"Unable to get purchase link for {subID} form");
                return null;
            }

            return new(purchaseLink);
        }

        private async Task<TransactionInfo?> CheckoutAsync(Uri checkoutUri)
        {
            if (!Bot.IsConnectedAndLoggedOn)
            {
                Bot.ArchiLogger.LogGenericError("Is currently not logged on");
                return null;
            }

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

        private async Task<JsonData.InitTransactionJsonResponse?> InitTransactionAsync(TransactionInfo transactionInfo, Uri referer)
        {
            if (transactionInfo.ShoppingCart == 0 || string.IsNullOrWhiteSpace(transactionInfo.ShippingCountry) || string.IsNullOrWhiteSpace(transactionInfo.PaymentMethod))
                throw new ArgumentException($"{nameof(transactionInfo)} is invalid");

            if (!Bot.IsConnectedAndLoggedOn)
            {
                Bot.ArchiLogger.LogGenericError("Is currently not logged on");
                return null;
            }

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

        private async Task<JsonData.FinalPriceJsonResponse?> GetFinalPriceAsync(TransactionInfo transactionInfo, JsonData.InitTransactionJsonResponse initTransactionJsonResponse, Uri referer)
        {
            if (transactionInfo.ShoppingCart == 0 || string.IsNullOrWhiteSpace(transactionInfo.ShippingCountry) || string.IsNullOrWhiteSpace(transactionInfo.PaymentMethod))
                throw new ArgumentException($"{nameof(transactionInfo)} is invalid");

            if (string.IsNullOrWhiteSpace(initTransactionJsonResponse.TransactionID))
                throw new ArgumentException($"{nameof(initTransactionJsonResponse)} is invalid");

            if (!Bot.IsConnectedAndLoggedOn)
            {
                Bot.ArchiLogger.LogGenericError("Is currently not logged on");
                return null;
            }

            string getFinalPriceUrl = $"/checkout/getfinalprice/?count=1&transid={initTransactionJsonResponse.TransactionID}&purchasetype=self&microtxnid=-1&cart={transactionInfo.ShoppingCart}&gidReplayOfTransID={transactionInfo.TransactionID}";
            Uri uri = new(ArchiWebHandler.SteamStoreURL, getFinalPriceUrl);

            var response = await Bot.ArchiWebHandler.WebBrowser.UrlGetToJsonObject<JsonData.FinalPriceJsonResponse>(uri, referer: referer);

            if (response == null || response.StatusCode != HttpStatusCode.OK)
            {
                Bot.ArchiLogger.LogGenericError($"Unable to finalize price {uri.AbsoluteUri}");
                return null;
            }

            if (response.Content.Success != 1)
            {
                Bot.ArchiLogger.LogGenericError($"Finalize price at {uri.AbsoluteUri} returns unsuccessful data / {response.Content.Success}");
                return null;
            }

            return response.Content;
        }

        private async Task<bool> FinalizeTransactionAsync(JsonData.InitTransactionJsonResponse initTransactionJsonResponse, Uri referer)
        {
            if (string.IsNullOrWhiteSpace(initTransactionJsonResponse.TransactionID))
                throw new ArgumentException($"{nameof(initTransactionJsonResponse)} is invalid");

            if (!Bot.IsConnectedAndLoggedOn)
            {
                Bot.ArchiLogger.LogGenericError("Is currently not logged on");
                return false;
            }

            const string FINALIZE_TRANSACTION_URL = "/checkout/finalizetransaction/";
            Uri uri = new(ArchiWebHandler.SteamStoreURL, FINALIZE_TRANSACTION_URL);

            const string BROWSER_INFO_HARDCODE = "{\"language\":\"en\",\"javaEnabled\":\"false\",\"colorDepth\":24,\"screenHeight\":768,\"screenWidth\":1366}";
            Dictionary<string, string> postData = new()
            {
                { "transid", initTransactionJsonResponse.TransactionID },
                { "CardCVV2", string.Empty },
                { "browserInfo", BROWSER_INFO_HARDCODE }
            };

            var response = await Bot.ArchiWebHandler.WebBrowser.UrlPostToJsonObject<JsonData.FinalizeTransactionJsonResponse, Dictionary<string, string>>(uri, data: postData, referer: referer);

            if (response == null || response.StatusCode != HttpStatusCode.OK)
            {
                Bot.ArchiLogger.LogGenericError($"Unable to finalize transaction {uri.AbsoluteUri}");
                return false;
            }

            const int SUCCESS_CODE = 22;
            if (response.Content.Success != SUCCESS_CODE)
            {
                Bot.ArchiLogger.LogGenericError($"Finalize transaction at {uri.AbsoluteUri} returns unsuccessful data / {response.Content.Success}");
                return false;
            }

            return true;
        }

        internal async Task<InGameInfo?> GetInGameInfoAsync(uint appID, ulong itemID, uint quantity)
        {
            if (appID == 0)
                throw new ArgumentException($"{nameof(appID)} is invalid");

            if (itemID == 0)
                throw new ArgumentException($"{nameof(itemID)} is invalid");

            if (!Bot.IsConnectedAndLoggedOn)
            {
                Bot.ArchiLogger.LogGenericError("Is currently not logged on");
                return null;
            }

            Uri uri = new(ArchiWebHandler.SteamStoreURL, $"/buyitem/{appID}/{itemID}/{quantity}");

            var response = await Bot.ArchiWebHandler.WebBrowser.UrlGetToHtmlDocument(uri);

            if (response == null || response.StatusCode != HttpStatusCode.OK)
            {
                Bot.ArchiLogger.LogGenericError($"Unable to initiate in-game purchase {appID} / {itemID} / {quantity}");
                return null;
            }

            const string FORM_SELECTOR = "#form_authtxn";
            var form = response.Content.QuerySelector(FORM_SELECTOR);

            if (form == null)
            {
                Bot.ArchiLogger.LogGenericError($"Unable to find buy form {appID} / {itemID}");
                return null;
            }

            var inputs = form.QuerySelectorAll("input");

            if (inputs == null)
            {
                Bot.ArchiLogger.LogGenericError($"Unable to find inputs from form {appID} / {itemID}");
                return null;
            }

            InGameForm serializableForm = new();

            foreach (var input in inputs)
            {
                if (input == null)
                    continue;

                string name = input.GetAttribute("name");
                string value = input.GetAttribute("value");

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                    serializableForm.Add(name, value);
            }

            if (serializableForm.Count == 0 || serializableForm.TransactionID == 0)
            {
                Bot.ArchiLogger.LogGenericError($"Serializable form is empty {appID} / {itemID}");
                return null;
            }

            const string PRICE_SELECTOR = "#review_subtotal_value";
            var priceElement = response.Content.QuerySelector(PRICE_SELECTOR);

            if (priceElement == null)
            {
                Bot.ArchiLogger.LogGenericError($"Unable to find total price {appID} / {itemID}");
                return null;
            }

            if (string.IsNullOrWhiteSpace(priceElement.TextContent))
            {
                Bot.ArchiLogger.LogGenericError($"Price text is empty {appID} / {itemID}");
                return null;
            }

            Match priceMatch = Regex.Match(priceElement.TextContent, @"[0-9.]+");

            if (!priceMatch.Success)
            {
                Bot.ArchiLogger.LogGenericError($"Unable to find price substring {appID} / {itemID}");
                return null;
            }

            if (!double.TryParse(priceMatch.Value, out double price))
            {
                Bot.ArchiLogger.LogGenericError($"Unable to get total price {appID} / {itemID}");
                return null;
            }

            return new((ulong)(price * 100), serializableForm);
        }

        private async Task<bool> ApproveInGamePurchaseAsync(InGameForm inGameForm)
        {
            if (inGameForm.TransactionID == 0)
                throw new ArgumentException($"{nameof(inGameForm)} is invalid");

            if (!Bot.IsConnectedAndLoggedOn)
            {
                Bot.ArchiLogger.LogGenericError("Is currently not logged on");
                return false;
            }

            Uri referer = new(ArchiWebHandler.SteamStoreURL, $"/checkout/approvetxn/{inGameForm.TransactionID}/?returnurl={HttpUtility.UrlEncode(inGameForm["returnurl"])}&canceledurl=https%3a%2f%2fstore.steampowered.com%2f");
            Uri approve = new(ArchiWebHandler.SteamStoreURL, "/checkout/approvetxnsubmit");

            var response = await Bot.ArchiWebHandler.WebBrowser.UrlPostToHtmlDocument(approve, data: inGameForm, referer: referer);

            if (response == null)
            {
                Bot.ArchiLogger.LogGenericError($"Unable to approve transaction {inGameForm.TransactionID}");
                return false;
            }



            return response.StatusCode == HttpStatusCode.OK;
        }

        internal record AppInfo(ulong Price, AppForm AppForm);
        internal record InGameInfo(ulong Price, InGameForm InGameForm);

        internal class AppForm: Dictionary<string, string>
        {
            internal uint SubID 
            {
                get
                {
                    if (!ContainsKey("subid"))
                        return 0;
                    if (uint.TryParse(this["subid"], out uint subID))
                        return subID;
                    else
                        return 0;
                }
            }

            internal uint BundleID
            {
                get
                {
                    if (!IsBundle)
                        return 0;
                    if (uint.TryParse(this["bundleid"], out uint bundleID))
                        return bundleID;
                    else
                        return 0;
                }
            }

            internal bool IsBundle => ContainsKey("bundleid");
        }

        internal class InGameForm: Dictionary<string, string>
        {
            internal ulong TransactionID
            {
                get
                {
                    if (!ContainsKey("transaction_id"))
                        return 0;
                    if (ulong.TryParse(this["transaction_id"], out ulong transactionID))
                        return transactionID;
                    else
                        return 0;
                }
            }

            internal bool Approved
            {
                get
                {
                    if (!ContainsKey("approved"))
                        return false;
                    if (this["approved"] == "1")
                        return true;
                    else
                        return false;
                }

                set
                {
                    if (!ContainsKey("approved"))
                        return;

                    this["approved"] = value ? "1" : "0";
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
