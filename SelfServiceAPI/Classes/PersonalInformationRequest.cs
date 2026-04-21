using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static SelfServiceAPI.Classes.Helper;

namespace SelfServiceAPI.Classes
{
    public class PersonalInformationRequest
    {
        public int  IncompleteApplicationId { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int CountryOfBirth { get; set; }
        public int Nationality { get; set; }
        public string Gender { get; set; }
        public int Prefix { get; set; }
        public List<ArabicDataRequest> ArabicData { get; set; }
    }

}