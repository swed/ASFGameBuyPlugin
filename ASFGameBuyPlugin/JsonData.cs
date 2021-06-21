using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    public sealed class FinalPriceResponse: PluginJsonResponse
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
