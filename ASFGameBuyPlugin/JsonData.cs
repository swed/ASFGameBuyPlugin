using Newtonsoft.Json;

namespace ASFGameBuyPlugin.JsonData
{
    public abstract class PluginJsonResponse
    {
        [JsonProperty("success")]
        public int Success { get; set; }
        [JsonProperty("purchaseresultdetail")]
        public int PurchaseResultDetail { get; set; }
    }

    public sealed class InitTransactionJsonResponse: PluginJsonResponse
    {
        [JsonProperty("paymentmethod")]
        public int PaymentMethod { get; set; }
        [JsonProperty("transid")]
        public string? TransactionID { get; set; }
        [JsonProperty("transactionprovider")]
        public int TransactionProvider { get; set; }
        [JsonProperty("paymentmethodcountrycode")]
        public string? PaymentMethodCountryCode { get; set; }
        [JsonProperty("paypaltoken")]
        public string? PaypalToken { get; set; }
        [JsonProperty("packagewitherror")]
        public long PackageWithError { get; set; }
        [JsonProperty("appcausingerror")]
        public long AppCausingError { get; set; }
        [JsonProperty("pendingpurchasepaymentmethod")]
        public int PendingPurchasePaymentMethod { get; set; }
        [JsonProperty("authorizationurl")]
        public string? AuthorizationUrl { get; set; }
    }

    public sealed class FinalPriceJsonResponse: PluginJsonResponse
    {
        [JsonProperty("base")]
        public string? Base { get; set; }
        [JsonProperty("tax")]
        public string? Tax { get; set; }
        [JsonProperty("discount")]
        public string? Discount { get; set; }
        [JsonProperty("shipping")]
        public string? Shipping { get; set; }
        [JsonProperty("importfee")]
        public string? ImportFee { get; set; }
        [JsonProperty("currencycode")]
        public ECurrencyCode CurrencyCode { get; set; }
        [JsonProperty("taxtype")]
        public int TaxType { get; set; }
        [JsonProperty("providerpaymentmethod")]
        public int ProvidePaymentMethod { get; set; }
        [JsonProperty("walletcreditchanged")]
        public int WalletCreditChanged { get; set; }
        [JsonProperty("hitminprovideramount")]
        public long HitMinProviderAmount { get; set; }
        [JsonProperty("requirecvv")]
        public int RequireCVV { get; set; }
        [JsonProperty("taxdetails")]
        public TaxDetail? TaxDetails { get; set; }
        [JsonProperty("promotions")]
        public string? Promotions { get; set; }
        [JsonProperty("lineitems")]
        public LineItem[]? LineItems { get; set; }
        [JsonProperty("walletcreditlineitems")]
        public string? WalletCreditLineItems { get; set; }
        [JsonProperty("useexternalredirect")]
        public int UseExternalRedirect { get; set; }
        [JsonProperty("totalloyaltypoints")]
        public ulong TotalLoyaltyPoints { get; set; }
        [JsonProperty("taxNotice")]
        public string? TaxNotice { get; set; }
        [JsonProperty("lineItemsHTML")]
        public string? LineItemsHTML { get; set; }
        [JsonProperty("purchaseNotice")]
        public string? PurchaseNotice { get; set; }
        [JsonProperty("steamAccountTotal")]
        public string? SteamAccountTotal { get; set; }
        [JsonProperty("total")]
        public ulong Total { get; set; }
        [JsonProperty("formattedTotal")]
        public string? FormattedTotal { get; set; }
        [JsonProperty("formattedSteamAccountTotal")]
        public string? FormattedSteamAccountTotal { get; set; }
        [JsonProperty("formattedProviderTotal")]
        public string? FormattedProviderTotal { get; set; }
        [JsonProperty("formattedDiscountedSubTotal")]
        public string? FormattedDiscountedSubTotal { get; set; }
        [JsonProperty("formattedSubTotal")]
        public string? FormattedSubTotal { get; set; }
        [JsonProperty("formattedTax")]
        public string? FormattedTax { get; set; }
        [JsonProperty("formattedTotalLoyaltyPoints")]
        public string? FormattedTotalLoyaltyPoints { get; set; }
        [JsonProperty("formattedShipping")]
        public string? FormattedShipping { get; set; }
        [JsonProperty("formattedImportFee")]
        public string? FormattedImportFee { get; set; }
        [JsonProperty("steamAccountBalance")]
        public ulong SteamAccountBalance { get; set; }
        [JsonProperty("formattedProviderRemaining")]
        public string? FormattedProviderRemaining { get; set; }
        [JsonProperty("storeCountryCode")]
        public string? StoreCountryCode { get; set; }
        [JsonProperty("priceOfASubChanged")]
        public bool PriceOfASubChanged { get; set; }


        public sealed class TaxDetail
        {
            [JsonProperty("billing")]
            public Details? Billing { get; set; }
            [JsonProperty("shipping")]
            public Details? Shipping { get; set; }

            public sealed class Details
            {
                [JsonProperty("country")]
                public string? Country { get; set; }
                [JsonProperty("state")]
                public string? State { get; set; }
                [JsonProperty("postal")]
                public string? Postal { get; set; }
            }
        }

        public sealed class LineItem
        {
            [JsonProperty("packageid")]
            public ulong PackageID { get; set; }
            [JsonProperty("base")]
            public string? Base { get; set; }
            [JsonProperty("tax")]
            public string? Tax { get; set; }
            [JsonProperty("discount")]
            public string? Discount { get; set; }
            [JsonProperty("shipping")]
            public string? Shipping { get; set; }
            [JsonProperty("currencycode")]
            public ECurrencyCode CurrencyCode { get; set; }
            [JsonProperty("parentbundleid")]
            public ulong ParentBundleID { get; set; }
            [JsonProperty("gidlineitem")]
            public string? GIDLineItem { get; set; }
            [JsonProperty("quantity")]
            public uint Quantity { get; set; }
            [JsonProperty("discountvalve")]
            public string? DiscountValve { get; set; }
            [JsonProperty("loyaltypoints")]
            public ulong LoyaltyPoints { get; set; }
        }
    }

    public sealed class FinalizeTransactionJsonResponse: PluginJsonResponse
    {
        [JsonProperty("bShowBRSpecificCreditCardError")]
        public bool ShowBRSpecificCreditCardError { get; set; }
    }

    public sealed class Configuration
    {
        public uint BuyTimeout { get; set; }
    }


    // steamd.md
    public enum ECurrencyCode
    {
        Invalid = 0,

        USD = 1,
        GBP = 2,
        EUR = 3,
        CHF = 4,
        RUB = 5,
        PLN = 6,
        BRL = 7,
        JPY = 8,
        NOK = 9,
        IDR = 10,
        MYR = 11,
        PHP = 12,
        SGD = 13,
        THB = 14,
        VND = 15,
        KRW = 16,
        TRY = 17,
        UAH = 18,
        MXN = 19,
        CAD = 20,
        AUD = 21,
        NZD = 22,
        CNY = 23,
        INR = 24,
        CLP = 25,
        PEN = 26,
        COP = 27,
        ZAR = 28,
        HKD = 29,
        TWD = 30,
        SAR = 31,
        AED = 32,
        ARS = 34,
        ILS = 35,
        BYN = 36,
        KZT = 37,
        KWD = 38,
        QAR = 39,
        CRC = 40,
        UYU = 41,
    };
}
