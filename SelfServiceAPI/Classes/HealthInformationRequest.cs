using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SelfServiceAPI.Classes
{
    public class HealthInformationRequest
    {
        public bool HasBeenIll { get; set;}
        public string IllnessDescription { get; set; }
        public int IncompleteApplicationId { get; set; }
    }
}