using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SelfServiceAPI.Classes
{
    public class AcademicInterestRequest
    {
        public int ApplicationprogramSettingId { get; set; }
        public int ApplicationFormSettingId { get; set; }
        public int ProgramOfStudyId { get; set; }
        public int IncompleteApplicationId { get; set; }
        public string Description { get; set; }
        public int EntryTerm { get; set; }
        public int ProgramId { get; set; }
    }
}