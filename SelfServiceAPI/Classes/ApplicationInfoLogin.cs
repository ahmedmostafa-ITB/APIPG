using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static SelfServiceAPI.Classes.Helper;

namespace SelfServiceAPI.Classes
{
    public class ApplicationInfoLogin : SuccessReponse
    {
        [JsonProperty(Order = 3)]
        public ApplicationInfoData data { get; set; }
    }

    public class ApplicationInfoData
    {
        public int ApplicationProgramSettingId { get; set; }
        public int ApplicationFormSettingId { get; set; }
        public bool Validated { get; set; }
        public int? ProgramOfStudyId { get; set; }
        public int? UniversityId { get; set; }

        public int? SecondUniversityId { get; set; }
        public int? SecondProgramId { get; set; }
        public int? SecondMajorId { get; set; }

        public int? Level { get; set; }

        public int? SecondLevel { get; set; }
        public int EntryTerm { get; set; }
        public int ProgramId { get; set; }
        public string Description { get; set; }
        public string PreferredCampus { get; set; }

        public string SecDescription { get; set; }
        public int IncompleteApplicationId { get; set; }
        public int AddressTypeId { get; set; }
        public string Email { get; set; }
        public int MotherId { get; set; }
        public int FatherId { get; set; }
        public int UndergraduateId { get; set; }
        public int GraduateId { get; set; }
        public int ThanawiyaAmma { get; set; }
        public int IGCSE { get; set; }
        public int GCSE { get; set; }
        public int IBDiploma { get; set; }
        public int AmericanHighSchool { get; set; }
        public string OtherSource { get; set; }
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public bool DormPlanInterest { get; set; }
        //public string Line3 { get; set; }
        //public string Line4 { get; set; }
        public int? Country { get; set; }
        public int? City { get; set; } //County
        public int? StateProvinceId { get; set; }
       // public string PostalCode { get; set; }
        public string NationalIdNumber { get; set; }
        public string PassportNumber { get; set; }
        public int? PassportCountryIssued { get; set; }
        public DateTime? PassportExpirationDate { get; set; }
        public string HasBeenIll { get; set; }
        public string IllnessDescription { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
          public DateTime? DateOfBirth { get; set; }
        public int CountryOfBirth { get; set; }
        public int Nationality { get; set; }
        public string Gender { get; set; }
        public int? Prefix { get; set; }
 

       public string SiblingFName { get; set; }

        public string SiblingMName { get; set; }

        public string SiblingLName { get; set; }

        public string SiblingTKHNumber { get; set; }

        public List<ApplicationTestScoreLoginInfo> Test { get; set; }

        public List<ApplicationTestsLoginInfo> Tests { get; set; }

        public List<PhoneNumberInfo> ContactInformation { get; set; }

        public List<ApplicationSourceloginInfo> ApplicationTestSourceId { get; set; }

        public ApplicationRelationInfo FatherInfo { get; set; }

        public ApplicationRelationInfo MotherInfo { get; set; }

        public ApplicationRelationInfo GuardianInfo { get; set; }

        public List<ApplicationUserDefinedLoginInfo> ArabicData { get; set; }

        public List<EducationLoginHistory> EducationHistory { get; set; }

        public List<Employment> Employment { get; set; }

        // Postgraduate Fields
        public string MilitaryStatus { get; set; }
        public string MaritalStatus { get; set; }
        public string EmploymentStatus { get; set; }
        public string EmployerCompanyName { get; set; }
        public string EmployerPosition { get; set; }
        public string EmployerStartDate { get; set; }
        public string EmployerDuties { get; set; }
        public string EmployerIdNumber { get; set; }
        public string CovAlumni { get; set; }
        public string BachelorTaughtInEnglish { get; set; }
        public string HasProfExam { get; set; }
        public string CoventryId { get; set; }
        public string PgEmergencyContactRelationship { get; set; }
        public string PgEmergencyContactGivenName { get; set; }
        public string PgEmergencyContactMiddleName { get; set; }
        public string PgEmergencyContactFamilyName { get; set; }
        public string PgEmergencyContactMobile { get; set; }
        public string PgEmergencyContactEmail { get; set; }
        public string AcademicAwards { get; set; }
        public string PgSchoolName { get; set; }
        public string PgBachelorUniversity { get; set; }
        public string PgBachelorUniversityName { get; set; }
        public string PgBachelorDegree { get; set; }
        public string PgBachelorFieldOfStudy { get; set; }
        public string PgBachelorYearOfGrad { get; set; }
        public string PgHasAcademicAward { get; set; }
        public string PgAcademicAwards { get; set; }
        public string PgProfExamsData { get; set; }

    }

    public class ApplicationTestScoreLoginInfo
    {
        public int TestId { get; set; }
        public int TestTypeId { get; set; }
        public string Score { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public double Total { get; set; }
        public int SubjectId { get; set; }
        public int LevelId { get; set; }
        public int ScoreId { get; set; }
    }

    public class ApplicationTestsLoginInfo
    {
        public int Test { get; set; }
        public int TestType { get; set; }
        public string Score { get; set; }
        public DateTime? DateTaken { get; set; }

    }

    public class ApplicationUserDefinedLoginInfo
    {
        public string ColumnName { get; set; }
        public string ColumnValue { get; set; }
    }

    public class ApplicationSourceloginInfo
    {
        public int Id { get; set; }
        public string Value { get; set; }
    }

    public class EducationLoginHistory
    {
        public int CurriculumId { get; set; }
        public DateTime? EndDate { get; set; }
        public string InstitutionName { get; set; }
        public int? CountryId { get; set; }
        public int? CityId { get; set; }
        public int? TransferCountryId { get; set; }
        public int? TransferCityId { get; set; }
        public decimal GPA { get; set; }
        public string InstitutionId { get; set; }
        public string TranferedInstitutionName { get; set; }
        public string TranferedInstitutionId { get; set; }
        public string TransferredCerti { get; set; }
        public string TransferredGPA { get; set; }
        public string EducationGrade { get; set; }

    }

    public class Employment
    {
        public string EmployerName { get; set; }
        public int Position { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}