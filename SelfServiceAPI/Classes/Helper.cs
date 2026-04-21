using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SelfServiceAPI.Classes
{
    public class Helper
    {

        public class ArabicDataRequest
        {
            public string ColumnName { get; set; }
            public string ColumnValue { get; set; }
        }

        public class PhoneNumberInfo
        {
            public string PhoneType { get; set; }
            public int? CountryId { get; set; }
            public string Email { get; set; }
            public string PhoneNumber { get; set; }
            public bool IsPrimary { get; set; }
        }

        public class EducationInfoRequest
        {
            public int CurriculumId { get; set; }
            public DateTime? EndDate { get; set; }
            public decimal GPA { get; set; }
            public string InstitutionName { get; set; }
            public string InstitutionId { get; set; }
            public int CountryId { get; set; }
            public int CityId { get; set; }
            public int TransferCountryId { get; set; }
            public int TransferCityId { get; set; }
            public string TransferedInstitutionName { get; set; }
            public string TransferedInstitutionId { get; set; }
            public bool IsTransfered { get; set; }
            public string TransferredCerti { get; set; }
            public string TransferredGPA { get; set; }
            public string EducationGrade { get; set; }

        }

        public class EmploymentInfoRequest
        {
            public string EmployerName { get; set; }
            public int Position { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
        }

        public class TestRequest
        {
            public int Test { get; set; }
            public int TestType { get; set; }
            public string Score { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }
            public int ScoreId { get; set; }
            public int SubjectId { get; set; }
            public int LevelId { get; set; }
            public double Total { get; set; }
        }


        public class TestsRequest
        {
            public int Test { get; set; }
            public int TestType { get; set; }
            public string Score { get; set; }
            public DateTime? DateTaken { get; set; }

        }
    }
}