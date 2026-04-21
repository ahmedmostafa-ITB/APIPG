using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SelfServiceAPI.Classes
{
    public class AddressInformationRequest
    {
        public int IncompleteApplicationId { get; set; }
        public int AddressTypeId { get; set; }
        //public string HouseNumber { get; set; }
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        //public string Line3 { get; set; }
       // public string Line4 { get; set; }
        public int Country { get; set; }
        public string City { get; set; }
        public int StateProvinceId { get; set; }
        //public string PostalCode { get; set; }
    }
}