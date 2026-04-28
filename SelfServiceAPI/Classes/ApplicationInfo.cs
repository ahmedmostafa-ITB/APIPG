using DataAccess;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using static SelfServiceAPI.Classes.Helper;

namespace SelfServiceAPI.Classes
{
    public class ApplicationInfo
    {
        #region Properties

        public string Prefix { get; set; }
        public string Email { get; set; }
        public string GovernmentId { get; set; }
        public string PassportNumber { get; set; }
        public int? PassportCountryIssued { get; set; }
        public bool Validated { get; set; }
        public string OtherSource { get; set; }

        [XmlElement(DataType = "dateTime")]
        public DateTime? PassportExpirationDate { get; set; }

        [XmlElement("Name")]
        public PersonalInfo PersonalInfo { get; set; }

        [XmlElement("Programs")]
        public AcademicInterest AcademicInterest { get; set; }

        public List<ApplicationUserDefinedInfo> UserDefined { get; set; }

        [XmlElement("Demographic")]
        public Demographic Demographic { get; set; }

        public List<AddressInfo> Address { get; set; }

        public List<PhoneNumberInfo> PhoneNumber { get; set; }

        public List<ApplicationRelationInfo> ApplicationRelations { get; set; }

        public List<ApplicationSourceInfo> SourceInfo { get; set; }

        public List<EducationInfo> PriorEducation { get; set; }

        public List<ApplicationTestScoreInfo> TestScores { get; set; }

        public List<ApplicationTestsInfo> Tests { get; set; }

        public List<EmploymentInfo> Employment { get; set; }

        [XmlElement("Postgraduate")]
        public PostgraduateInfo PostgraduateInfo { get; set; }

        #endregion

        #region Methods
        public void SerializeApplicationInfo(string email, string ApplicationEntitiesSettingsId, ApplicationFormEntities entities, ApplicationInfo applicationInfo)
        {
            XmlSerializer serializer = new XmlSerializer(applicationInfo.GetType());

            ObjectParameter incompleteApplicationId = new ObjectParameter("IncompleteApplicationId", typeof(int));

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Encoding = new UnicodeEncoding(false, false),
                Indent = true,
                OmitXmlDeclaration = true
            };

            using (StringWriter textWriter = new StringWriter())
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(textWriter, settings))
                {
                    serializer.Serialize(xmlWriter, applicationInfo);

                    entities.spInsIncompleteApplication(incompleteApplicationId, Guid.NewGuid(), Convert.ToInt32(ApplicationEntitiesSettingsId), email, null, textWriter.ToString());
                    //entities.spInsIncompleteApplication(Guid.NewGuid(), Convert.ToInt32(ApplicationEntitiesSettingsId), email, null, textWriter.ToString());
                }
            }
        }

        public void SerializeApplicationInfo(IncompleteApplication incompleteApplication, ApplicationFormEntities entities, ApplicationInfo applicationInfo)
        {
            XmlSerializer serializer = new XmlSerializer(applicationInfo.GetType());

            ObjectParameter incompleteApplicationId = new ObjectParameter("IncompleteApplicationId", typeof(int));

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Encoding = new UnicodeEncoding(false, false),
                Indent = true,
                OmitXmlDeclaration = true
            };

            using (StringWriter textWriter = new StringWriter())
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(textWriter, settings))
                {
                    serializer.Serialize(xmlWriter, applicationInfo);

                    entities.spUpdIncompleteApplication(incompleteApplicationId, incompleteApplication.Token, incompleteApplication.Email, incompleteApplication.PersonId, textWriter.ToString());
                }
            }
        }

        public static ApplicationInfo DeserializeApplicationInfo(string applicationData)
        {
            StringReader stringReader = new StringReader(applicationData);
            XmlSerializer ser = new XmlSerializer(typeof(ApplicationInfo));
            return (ApplicationInfo)ser.Deserialize(stringReader);
        }


        #endregion
    }

    public class ApplicationRelationInfo
    {
        public int RelationType { get; set; }
        public int RelationPrefix { get; set; }
        public string RelationFirstName { get; set; }
        public string RelationMiddleName { get; set; }
        public string RelationLastName { get; set; }
        public string Profession { get; set; }
        public string Company { get; set; }
        public string CompanyAddress { get; set; }
        public string MobileNumber { get; set; }
        public string RelationEmail { get; set; }
        public string Address { get; set; }
        public bool Deceased { get; set; }
        public bool IsGuardian { get; set; }
        public bool AttendedInstitution { get; set; }
        public string GuardianAddress { get; set; }
        public bool IsTKHEmployee { get; set; }
        public string IdNumber { get; set; }
    }


    public class PersonalInfo
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
          public int? Prefix { get; set; }

        public string SiblingFName { get; set; }
        public string SiblingMName { get; set; }
        public string SiblingLName { get; set; }
        public string SiblingTKHNumber { get; set; }

    }




    [XmlRoot("ApplicationUserDefinedInfo")]
    public class ApplicationUserDefinedInfo
    {
        public int ApplicationId { get; set; }
        public byte ColumnType { get; set; }
        public string ColumnName { get; set; }
        public string ColumnValue { get; set; }
        public string ColumnLabel { get; set; }
        public bool IsUploading { get; set; }
        public string Description { get; set; }
    }


    public class Demographic
    {
        [XmlElement(DataType = "dateTime")]
        public DateTime? BirthDate { get; set; }
        public int CountryOfBirth { get; set; }
        public int PrimaryCitizenship { get; set; }
        public string Gender { get; set; }
    }

    [XmlRoot("ApplicationProgramInfo")]
    public class AcademicInterest
    {
        public int ApplicationProgramSettingId { get; set; }
        public int ApplicationFormSettingId { get; set; }
        public int? ProgramOfStudy { get; set; }
        public int IncompleteApplicationId { get; set; }
        public int FullPartTime { get; set; }
        public string ProgramDegreeCurriculumDescription { get; set; }
        public string ProgramDegreeCurriculumSecondDescription { get; set; }
        public string FullPartTimeDescription { get; set; }
        public bool IsFirstChoice { get; set; }
        public int SessionPeriodId { get; set; }
        public int ProgramId { get; set; }
        public int? UniversityId { get; set; }

        public int? SecondUniversityId { get; set; }
        public int? SecondProgramId { get; set; }
        public int? SecondMajorId { get; set; }

        public int? Level { get; set; }

        public int? SecondLevel { get; set; }
        public string PreferredCampus { get; set; }
    }

    [XmlRoot("AddressInfo")]
    public class AddressInfo
    {
        public int AddressTypeId { get; set; }
        //public string HouseNumber { get; set; }
        public string Line1 { get; set; }
        public string Line2 { get; set; }
       // public string Line3 { get; set; } //Province
       // public string Line4 { get; set; }
        public int? Country { get; set; }
        public int? City { get; set; } //County
        public int? State { get; set; }
       // public string PostalCode { get; set; }
       public bool DormPlanInterest { get; set; }
        public bool IsPrimary { get; set; }
    }


    public class ApplicationSourceInfo
    {
        public int SourceId { get; set; }
    }

    public class EducationInfo
    {
        public decimal GPA { get; set; }
        public string InstitutionName { get; set; }
        public string TransferedInstitutionName { get; set; }
        public string TransferedInstitutionId { get; set; }
        public int? InstitutionCountryId { get; set; }
        public int? InstitutionCityId { get; set; }
        public string InstitutionId { get; set; }
        public bool IsTransfer { get; set; }
        [XmlElement(DataType = "dateTime")]
        public DateTime? EndDate { get; set; }
        public int CurriculumId { get; set; }
        public string TransferredCerti { get; set; }
        public string TransferredGPA { get; set; }
        public string EducationGrade { get; set; }
        public int? TransferInstitutionCountryId { get; set; }
        public int? TransferInstitutionCityId { get; set; }
    }

    public class EmploymentInfo
    {
        public string EmployerName { get; set; }
        public int Position { get; set; }
        [XmlElement(DataType = "dateTime")]
        public DateTime? StartDate { get; set; }
        [XmlElement(DataType = "dateTime")]
        public DateTime? EndDate { get; set; }
    }


    public class ApplicationTestScoreInfo
    {
        public int TestId { get; set; }
        public int TestTypeId { get; set; }
        public string Score { get; set; }
        public double Total { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int SubjectId { get; set; }
        public int LevelId { get; set; }
        public int ScoreId { get; set; }
    }


    public class ApplicationTestsInfo
    {
        public int Test { get; set; }
        public int TestType { get; set; }
        public string Score { get; set; }
        [XmlElement(DataType = "dateTime")]
        public DateTime? DateTaken { get; set; }

    }

    public class PostgraduateInfo
    {
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
        public string AcademicSupport { get; set; }
        public string AcademicSupportDetails { get; set; }
    }
}