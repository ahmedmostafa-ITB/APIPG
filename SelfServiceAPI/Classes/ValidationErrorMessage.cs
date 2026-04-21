using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SelfServiceAPI.Classes
{
    public class ValidationErrorMessage
    {

        [JsonProperty(Order = 1)]
        public int StatusCode { get; set; }

        [JsonProperty(Order = 2)]
        public string Status { get; set; }

        [JsonProperty(Order = 3)]
        public Dictionary<string,string> Msg = new Dictionary<string, string>();
    }

 
}


