using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ASFGameBuyPlugin.JsonData
{
    public abstract class PluginJsonResponse {}

    public sealed class InitTransactionJsonResponse: PluginJsonResponse
    {
        [JsonProperty("success")]
        public int Success { get; set; }
        [JsonProperty("purchaseresultdetail")]
        public int PurchaseResultDetail { get; set; }
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
}
