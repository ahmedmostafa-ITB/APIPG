using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static SelfServiceAPI.Classes.Helper;

namespace SelfServiceAPI.Classes
{
    public class ApplicationInfoRequest
    {
        #region Main
        public int IncompleteApplicationId { get; set; }
        #endregion

        #region Personal Information
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
          public DateTime? DateOfBirth { get; set; }
        public int CountryOfBirth { get; set; }
        public int Nationality { get; set; }
        public string Gender { get; set; }
        public int PrimaryLanguage { get; set; }
        public int SecondaryLanguage { get; set; }
        public int Prefix { get; set; }
        public string OtherSource{ get; set; }
        public List<ArabicDataRequest> ArabicData { get; set; }

        public string SiblingFName { get; set; }
        public string SiblingMName { get; set; }
        public string SiblingLName { get; set; }
        public string SiblingTKHNumber { get; set; }

        #endregion

        #region Academic Interest
        public int ApplicationprogramSettingId { get; set; }
        public int ApplicationFormSettingId { get; set; }
        public int ProgramOfStudyId { get; set; }
        public string Description { get; set; }
        public string SecDescription { get; set; }
        public int EntryTerm { get; set; }
        public int ProgramId { get; set; }
        public int? UniversityId { get; set; }

        public int? SecondUniversityId { get; set; }
        public int? SecondProgramId { get; set; }
        public int? SecondMajorId { get; set; }

        public int Level { get; set; }

        public int SecondLevel { get; set; }
        public string PreferredCampus { get; set; }
        #endregion

        #region Government Information
        public string NationalIdNumber { get; set; }
        public string PassportNumber { get; set; }
        public int PassportCountryIssued { get; set; }
        public DateTime? PassportExpirationdate { get; set; } = null;
        #endregion

        #region Address
        public int AddressTypeId { get; set; }
       // public string HouseNumber { get; set; }
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        //public string Line3 { get; set; }
       // public string Line4 { get; set; }
        public int Country { get; set; }
        public int City { get; set; }
        public int StateProvinceId { get; set; }
        public bool DormPlanInterest { get; set; }
       // public string PostalCode { get; set; }
        #endregion

        #region Contact Information
        public List<PhoneNumberInfo> ContactInformation;
        #endregion

        #region  Father Information
        public int FatherRelationType { get; set; }
        public int FatherPrefix { get; set; }
        public string FatherFirstName { get; set; }
        public string FatherMiddleName { get; set; }
        public string FatherFamilyName { get; set; }
        public string FatherProfession { get; set; }
        public string FatherCompany { get; set; }
        public string FatherCompanyAddress { get; set; }
        public string FatherMobileNumber { get; set; }
        public string FatherEmail { get; set; }
        public string FatherAddress { get; set; }
        public bool FatherAttendedInstitution { get; set; }
        public bool FatherDeceased { get; set; }
        public bool IsFatherTKHEmployee { get; set; }
        public string FatherIdNumber { get; set; }
        #endregion

        #region Mother Information
        public int MotherRelationType { get; set; }
        public int MotherPrefix { get; set; }
        public string MotherFirstName { get; set; }
        public string MotherMiddleName { get; set; }
        public string MotherFamilyName { get; set; }
        public string MotherProfession { get; set; }
        public string MotherCompany { get; set; }
        public string MotherCompanyAddress { get; set; }
        public string MotherMobileNumber { get; set; }
        public string MotherEmail { get; set; }
        public string MotherAddress { get; set; }
        public bool MotherAttendedInstitution { get; set; }
        public bool MotherDeceased { get; set; }
        public bool IsMotherTKHEmployee { get; set; }
        public string MotherIdNumber { get; set; }
        #endregion

        #region Guardian Information
        public int GuardianRelationType { get; set; }
        public int GuardianPrefix { get; set; }
        public string GuardianFirstName { get; set; }
        public string GuardianMiddleName { get; set; }
        public string GuardianFamilyName { get; set; }
        public string GuardianProfession { get; set; }
        public string GuardianMobileNumber { get; set; }
        public string GuardianEmail { get; set; }

        public string GuardianAddress { get; set; }
        #endregion

        #region Education History
        public List<EducationInfoRequest> EducationHistory;
        #endregion

        #region Employment
        public List<EmploymentInfoRequest> Employment;
        #endregion

        #region Academic Test
        public List<TestRequest> TestScore;
        #endregion

        #region  Tests
        public List<TestsRequest> Tests;
        #endregion

        #region Health Information
        public bool HasBeenIll { get; set; }
        public string IllnessDescription { get; set; }
        #endregion

        #region Sources
        public List<ApplicationSourceInfo> SourceInformation;
        #endregion

        #region Postgraduate Specfic Fields
        public string MilitaryStatus { get; set; }
        public string MaritalStatus { get; set; }
        public string EmploymentStatus { get; set; }
        public string EmployerCompanyName { get; set; }
        public string EmployerPosition { get; set; }
        public string EmployerStartDate { get; set; }
        public string EmployerDuties { get; set; }
        public string EmployerIdNumber { get; set; }
        public string CovAlumni { get; set; }
        public bool BachelorTaughtInEnglish { get; set; }
        public bool HasProfExam { get; set; }
        public string CoventryId { get; set; }
        public string PgEmergencyContactRelationship { get; set; }
        public string PgEmergencyContactGivenName { get; set; }
        public string PgEmergencyContactMiddleName { get; set; }
        public string PgEmergencyContactFamilyName { get; set; }
        public string PgEmergencyContactMobile { get; set; }
        public string PgEmergencyContactEmail { get; set; }
        /// <summary>Legacy field — kept for XML back-compat. New data uses the fields below.</summary>
        public string AcademicAwards { get; set; }

        // ── Program of Study ─────────────────────────────────────────────
        /// <summary>School / Faculty name selected in the Program of Study step.</summary>
        public string PgSchoolName { get; set; }

        // ── Bachelor Degree (individual columns) ─────────────────────────
        /// <summary>Institution ID from the university dropdown (or "other").</summary>
        public string PgBachelorUniversity { get; set; }
        /// <summary>Free-text university name when "other" is selected.</summary>
        public string PgBachelorUniversityName { get; set; }
        /// <summary>Degree type: Bachelor | BachelorHonours | Associate | Other.</summary>
        public string PgBachelorDegree { get; set; }
        /// <summary>Faculty / Major free-text field of study.</summary>
        public string PgBachelorFieldOfStudy { get; set; }
        /// <summary>Graduation year (ISO date string, year portion used).</summary>
        public string PgBachelorYearOfGrad { get; set; }
        /// <summary>"Yes" or "No" — whether the applicant has additional academic awards.</summary>
        public string PgHasAcademicAward { get; set; }

        // ── JSON arrays (multiple entries per applicant) ──────────────────
        /// <summary>JSON array of academic award objects: [{type,institution,year,description}].</summary>
        public string PgAcademicAwards { get; set; }
        /// <summary>JSON array of professional exam entries: [{examName,score,dateTaken,notes}].</summary>
        public string PgProfExamsData { get; set; }
        public string AcademicSupport { get; set; }
        public string AcademicSupportDetails { get; set; }
        #endregion
    }
}