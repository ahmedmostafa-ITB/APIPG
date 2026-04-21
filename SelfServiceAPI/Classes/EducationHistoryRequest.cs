using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SelfServiceAPI.Classes
{
    public class GraduateEducationHistoryRequest
    {
        public int IncompleteApplicationId { get; set; }
        public int CurriculumId { get; set; }
        public DateTime EndDate { get; set; }
        public string InstitutionName { get; set; }
        public string InstitutionId { get; set; }
        public int CountryId { get; set; }
        public decimal GPA {get;set;}
        public string EducationGrade { get; set; }


    }

    public class UnderGraduateEducationHistoryRequest
    {
        public int IncompleteApplicationId { get; set; }
        public int CurriculumId { get; set; }
        public DateTime EndDate { get; set; }
        public string InstitutionName { get; set; }
        public string InstitutionId { get; set; }
        public int CountryId { get; set; }
        public string TransferedInstitutionName { get; set; }
        public string TransferedInstitutionId { get; set; }
        public string EducationGrade { get; set; }

    }
}