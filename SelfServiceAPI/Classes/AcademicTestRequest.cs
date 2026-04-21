using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SelfServiceAPI.Classes
{
    public class AcademicTestRequest
    {
        public class AcademicTestAHSDRequest
        {
            public int IncompleteApplicationId { get; set; }
            public int Test { get; set; }
            public int TestType { get; set; }
            public string Score { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }
        }

        public class AcademicTestIBRequest
        {
            public int IncompleteApplicationId { get; set; }
            public int Test { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }
        }

        public class AcademicTestIGCSERequest
        {
            public int IncompleteApplicationId { get; set; }
            public int Test { get; set; }
            public int ScoreId { get; set; }
            public int SubjectId { get; set; }
            public int LevelId { get; set; }
            public double Total { get; set; }
        }

        public class AcademicTestANATARequest
        {
            public int IncompleteApplicationId { get; set; }
            public int Test { get; set; }
            public double Total { get; set; }
            public string Score { get; set; }
        }
    }
}