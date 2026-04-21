using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SelfServiceAPI.Classes
{
    public class AdditionalResponse : SuccessReponse
    {
        [Display(Name = "data")]
        [JsonProperty(Order = 3)]
        public AdditionalInfo Data { get; set; }
    }
}