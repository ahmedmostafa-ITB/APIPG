using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SelfServiceAPI.Classes
{
    public class ContactInformationRequest
    {
        public int IncompleteApplicationId { get; set; }
        public string PhoneType { get; set; }
        public int CountryId { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsPrimary { get; set; }
    }
}