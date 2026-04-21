using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SelfServiceAPI.Classes
{
    public class ParentInformationRequest
    {
        public int IncompleteApplicationId { get; set; }
        public int RelationType { get; set; }
        public int Prefix { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string FamilyName { get; set; }
        public string Profession { get; set; }
        public string Company { get; set; }
        public string CompanyAddress { get; set; }
        public string MobileNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public bool IsGuardian { get; set; }
        public bool AttendedInstitution { get; set; }
        public bool Deceased { get; set; }
    }
}