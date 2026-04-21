using System;


namespace SelfServiceAPI.Classes
{
    public class GovernmentInformationRequest
    {
        public int IncompleteApplicationId { get; set; }
        public string NationalIdNumber { get; set; }
        public string PassportNumber { get; set; }
        public int PassportCountryIssued { get; set; }
        public DateTime? PassportExpirationdate { get; set; } = null;
   }
}