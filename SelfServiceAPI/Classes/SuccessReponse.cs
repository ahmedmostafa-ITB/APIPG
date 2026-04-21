using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SelfServiceAPI.Classes
{
    public class SuccessReponse
    {
        [JsonProperty(Order = 1)]
        public int StatusCode { get; set; }

        [JsonProperty(Order = 2)]
        public string Status { get; set; }

        [JsonProperty(Order = 3)]
        public int PaymentTransactionId { get; set; }

        [JsonProperty(Order = 4)]
        public string Amount { get; set; }

        [JsonProperty(Order = 5)]
        public string PaymentGatewayURL { get; set; }
    }
}