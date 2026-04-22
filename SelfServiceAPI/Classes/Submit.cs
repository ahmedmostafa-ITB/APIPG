using DataAccess;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Core.Objects;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;
using static SelfServiceAPI.Classes.Helper;

namespace SelfServiceAPI.Classes
{
    public class Submit
    {
        public int IncompleteApplicationId { get; set; }
        public string FileContent { get; set; }
        public string FileName { get; set; }
        public string FileExtension { get; set; }

        public string label { get; set; }

        
        private const string HasBeenIll = "HasBeenIll";
        private const string IllDescription = "IllDescription";
        private HttpResponseMessage response = new HttpResponseMessage();

        public static int InsertApplication(ApplicationInfo applicationInfo, int applicationProgamSettingsId)
        {
            int insertedApplicationId = 0;
            ObjectParameter applicationId = new ObjectParameter("ApplicationId", typeof(int));
            string strGender = applicationInfo.Demographic.Gender;
            byte gender = 0;
            string illnesDescription = string.Empty;
            int sourceid = 0;

            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                switch (strGender)
                {
                    case "Male":
                        gender = 1;
                        break;
                    case "Female":
                        gender = 2;
                        break;
                    default:
                        gender = 1;
                        break;
                }

                //if (applicationInfo.UserDefined.Where(desc => desc.Description == HasBeenIll).ToList().Count > 0)
                //{
                //    hasIllnes = Convert.ToBoolean(applicationInfo.UserDefined.Single(desc => desc.Description == HasBeenIll).ColumnValue);
                //}

                //if (applicationInfo.UserDefined.Where(desc => desc.Description == IllDescription).ToList().Count > 0)
                //{
                //    illnesDescription = applicationInfo.UserDefined.Single(desc => desc.Description == IllDescription).ColumnValue;
                //}


                if (applicationInfo.SourceInfo != null && applicationInfo.SourceInfo.Count > 0)
                {
                    sourceid = applicationInfo.SourceInfo[0].SourceId;
                }


                bool isDorm = false;

                foreach (AddressInfo address in applicationInfo.Address)
                {
                    isDorm = address.DormPlanInterest;
                    break;
                }


                    entities.spInsApplication(applicationId, applicationInfo.PersonalInfo.Prefix == 0 ? null : applicationInfo.PersonalInfo.Prefix, applicationInfo.PersonalInfo.FirstName, applicationInfo.PersonalInfo.MiddleName, null, applicationInfo.PersonalInfo.LastName, null,
                null, null,null, applicationInfo.Email, applicationInfo.Demographic.BirthDate.HasValue ? applicationInfo.Demographic.BirthDate.Value < new DateTime(1900, 1, 1) || applicationInfo.Demographic.BirthDate.Value > new DateTime(2079, 6, 6) ? new DateTime(1900, 1, 1) : applicationInfo.Demographic.BirthDate.Value : (DateTime?)null, gender, null,
                null, null, null, null, applicationInfo.Demographic.PrimaryCitizenship, null, applicationInfo.Demographic.CountryOfBirth, null,
                null, null, applicationInfo.GovernmentId, null, null, null, null, applicationInfo.PassportNumber, applicationInfo.PassportCountryIssued == 0 ? null : applicationInfo.PassportCountryIssued,
                applicationInfo.PassportExpirationDate.HasValue ? applicationInfo.PassportExpirationDate.Value < new DateTime(1900, 1, 1) || applicationInfo.PassportExpirationDate.Value > new DateTime(2079, 6, 6) ? new DateTime(1900, 1, 1) : applicationInfo.PassportExpirationDate.Value : (DateTime?)null, applicationInfo.AcademicInterest.SessionPeriodId, applicationInfo.AcademicInterest.Level.HasValue && applicationInfo.AcademicInterest.Level > 0 ? applicationInfo.AcademicInterest.Level : (int?)null, null, null, null, sourceid, 1, false, isDorm, null, null, applicationProgamSettingsId, null,
                applicationInfo.OtherSource, null,4,applicationInfo.AcademicInterest.UniversityId.HasValue ? applicationInfo.AcademicInterest.UniversityId.Value: (int?)null, null, null,applicationInfo.AcademicInterest.SecondUniversityId.HasValue ? applicationInfo.AcademicInterest.SecondUniversityId.Value :(int?)null, null, null, null);

                insertedApplicationId = entities.Applications.Max(p => p.ApplicationId);
                return insertedApplicationId;
            }
        }

        internal static void InsertApplicationUserDefined(List<ApplicationUserDefinedInfo> lstUserDefined, int applicationId)
        {
            if (lstUserDefined == null) throw new ArgumentNullException("applicationUserDefined");

            ObjectParameter applicationTestScoreId = new ObjectParameter("ApplicationUserDefinedId", typeof(int));
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                foreach (ApplicationUserDefinedInfo userDefined in lstUserDefined)
                {
                    entities.spInsApplicationUserDefined(applicationTestScoreId, applicationId, userDefined.ColumnLabel, userDefined.ColumnName, userDefined.ColumnValue, userDefined.ColumnType, userDefined.IsUploading);
                }
            }
        }

        internal static void InsertApplicationProgram(AcademicInterest academicInterest, int applicationId)
        {
            string levelDesc = string.Empty;
            string secondLevelDesc = string.Empty;
            if (academicInterest == null) throw new ArgumentNullException("applicationInterest");

            ObjectParameter applicationProgram = new ObjectParameter("ApplicationProgramId", typeof(int));
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
               if (academicInterest.Level.HasValue && academicInterest.Level.Value > 0)
               {
                   var colAttendLevel = entities.CODE_COLLEGEATTEND.FirstOrDefault(item => item.CollegeAttendId == academicInterest.Level.Value);
                   if (colAttendLevel != null) levelDesc = colAttendLevel.LONG_DESC;
               }

                entities.spInsApplicationProgram(applicationProgram, applicationId, academicInterest.ProgramOfStudy == 0 ? null : academicInterest.ProgramOfStudy, academicInterest.FullPartTime, true, academicInterest.ProgramDegreeCurriculumDescription, academicInterest.Level.HasValue && academicInterest.Level.Value > 0 ? academicInterest.Level : null, levelDesc);

                CODE_COLLEGEATTEND colAttend = entities.CODE_COLLEGEATTEND.FirstOrDefault(itemSec => itemSec.CollegeAttendId == academicInterest.SecondLevel);

                if (colAttend != null)
                {
                    secondLevelDesc = colAttend.LONG_DESC;
                }

                entities.spInsApplicationProgram(applicationProgram, applicationId, academicInterest.SecondMajorId == 0 ? null : academicInterest.SecondMajorId, academicInterest.FullPartTime, false, academicInterest.ProgramDegreeCurriculumSecondDescription, academicInterest.SecondLevel.Value > 0 ? academicInterest.SecondLevel : null, secondLevelDesc);

            }
        }


        internal static void InsertApplicationCampus(AcademicInterest academicInterest, int applicationId)
        {
            string levelDesc = string.Empty;
            string secondLevelDesc = string.Empty;
            if (academicInterest == null) throw new ArgumentNullException("applicationInterest");
            int east = Convert.ToInt32(ConfigurationManager.AppSettings["East"]);
            int west = Convert.ToInt32(ConfigurationManager.AppSettings["West"]);
            ObjectParameter applicationProgram = new ObjectParameter("ApplicationCampusId", typeof(int));
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
             
                if(academicInterest.PreferredCampus=="West")
                {
                    entities.spInsApplicationCampus(applicationProgram, applicationId,west);
                }
                else if(academicInterest.PreferredCampus == "East")
                {
                    entities.spInsApplicationCampus(applicationProgram, applicationId,east);
                }
                
            }
        }
        internal static void InsertApplicationEducation(List<EducationInfo> lstPriorEducation, int applicationId)
        {
            if (lstPriorEducation == null) throw new ArgumentNullException("applicationEducation");

            ObjectParameter applicationEducationId = new ObjectParameter("ApplicationEducationId", typeof(int));
            ObjectParameter applicationEducationEnrollmentId = new ObjectParameter("ApplicationEducationEnrollmentId", typeof(int));
            int transferCurr = Convert.ToInt32(ConfigurationManager.AppSettings["TransferCurrId"]);
            int insertedEducation = 0;

            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                foreach (EducationInfo education in lstPriorEducation)
                {
                    //Insert application education
                    //entities.spInsApplicationEducation(applicationEducationId, applicationId, education.InstitutionName, string.Empty, null, education.InstitutionCountryId,
                    //    string.Empty, string.Empty, string.Empty, education.GPA, education.IsTransfer, string.Empty, education.GradeId == 0 ? null : education.GradeId
                    //    , education.TransferredCerti, education.TransferredGPA, education.TotalMark, education.InstitutionId == "Other" ? education.InstitutionName : null,
                    //     education.InstitutionId == "Other" ? education.InstitutionName : null, education.TransferedInstitutionId == "Other" ? education.TransferedInstitutionName : null);

                    //TODO: check the application educatuion
                    string educationGrade = string.Empty;
                    string city = string.Empty;
                    string transferCity = string.Empty;
                    if (!string.IsNullOrEmpty(education.EducationGrade))
                    {
                        int educationGradeId = Convert.ToInt32(education.EducationGrade);
                        educationGrade = entities.CODE_RATING.FirstOrDefault(x => x.CodeRatingId == educationGradeId).LONG_DESC;
                    }
                    if (education.InstitutionCityId != null && education.InstitutionCityId != 0)
                    {
                        city = entities.CODE_COUNTY.FirstOrDefault(x => x.CountyId == education.InstitutionCityId).LONG_DESC;
                    }
                    if (education.TransferInstitutionCityId != null && education.TransferInstitutionCityId != 0)
                    {
                        transferCity = entities.CODE_COUNTY.FirstOrDefault(x => x.CountyId == education.TransferInstitutionCityId).LONG_DESC;
                    }
                    if (education.IsTransfer == true)
                    {

                        entities.spInsApplicationEducation(applicationEducationId, applicationId, education.InstitutionId == "8" ? "Other" : education.InstitutionName, String.IsNullOrEmpty(city)?null:city, null, education.InstitutionCountryId > 0 ? education.InstitutionCountryId : null, null, null, education.GPA > 0 ? education.GPA.ToString() : string.Empty, education.InstitutionId == "8" ? education.InstitutionName : string.Empty, null, null, null, "0", educationGrade);
                        insertedEducation = entities.ApplicationEducations.Max(p => p.ApplicationEducationId);
                        if (insertedEducation > 0)
                            entities.spInsApplicationEducationEnrollment(applicationEducationEnrollmentId, insertedEducation, null, null,

                                education.EndDate.HasValue ? education.EndDate.Value < new DateTime(1900, 1, 1) || education.EndDate.Value > new DateTime(2079, 6, 6) ? new DateTime(1900, 1, 1) : education.EndDate.Value : (DateTime?)null,
                                null, education.CurriculumId, null);

                        entities.spInsApplicationEducation(applicationEducationId, applicationId, education.TransferedInstitutionId == "other" ? "Other" : education.TransferedInstitutionName, String.IsNullOrEmpty(transferCity) ? null : transferCity, null, education.TransferInstitutionCountryId > 0 ? education.TransferInstitutionCountryId : null, null, null,education.TransferredGPA, education.TransferedInstitutionId == "other" ? education.TransferedInstitutionName : string.Empty, null, null, null, "1", string.Empty);
                        insertedEducation = entities.ApplicationEducations.Max(p => p.ApplicationEducationId);
                        if (insertedEducation > 0)
                            entities.spInsApplicationEducationEnrollment(applicationEducationEnrollmentId, insertedEducation, null, null,

                                education.EndDate.HasValue ? education.EndDate.Value < new DateTime(1900, 1, 1) || education.EndDate.Value > new DateTime(2079, 6, 6) ? new DateTime(1900, 1, 1) : education.EndDate.Value : (DateTime?)null,
                                null, transferCurr, education.TransferredCerti);
                    }
                    else
                    {
                        entities.spInsApplicationEducation(applicationEducationId, applicationId, education.InstitutionId == "8" ? "Other" : education.InstitutionName, String.IsNullOrEmpty(city) ? null : city, null, education.InstitutionCountryId > 0 ? education.InstitutionCountryId : null, null, null, education.GPA > 0 ? education.GPA.ToString() : string.Empty, education.InstitutionId == "8" ? education.InstitutionName : string.Empty, null, null, null, "0",educationGrade);
                        insertedEducation = entities.ApplicationEducations.Max(p => p.ApplicationEducationId);
                        if (insertedEducation > 0)
                            entities.spInsApplicationEducationEnrollment(applicationEducationEnrollmentId, insertedEducation, null, null,
                                education.EndDate.HasValue ? education.EndDate.Value < new DateTime(1900, 1, 1) || education.EndDate.Value > new DateTime(2079, 6, 6) ? new DateTime(1900, 1, 1) : education.EndDate.Value : (DateTime?)null,
                                null, education.CurriculumId, null);
                    }
                }
            }
        }

        internal static void InsertApplicationAttachment(Submit attachment, int applicationId)
        {
            ObjectParameter applicationAttchmentId = new ObjectParameter("ApplicationAttachmentId", typeof(int));
            int fileType;
            CODE_MEDIATYPE mediaType;
            string fileExtension = string.Empty;
            string fileContent = string.Empty;

            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                fileExtension = attachment.FileExtension + ";";

                mediaType = entities.CODE_MEDIATYPE.FirstOrDefault(extension => extension.EXTENSION.Contains(fileExtension)) as CODE_MEDIATYPE;


                if (mediaType == null)
                {
                    fileExtension = attachment.FileExtension;

                    mediaType = entities.CODE_MEDIATYPE.FirstOrDefault(extension => extension.EXTENSION.Contains(fileExtension)) as CODE_MEDIATYPE;
                }

                if (mediaType != null)
                {

                    fileType = mediaType.MediaTypeId;

                    if (fileType > 0)
                    {
                        int index = attachment.FileContent.IndexOf(',');

                        if (index > 0)
                        {
                            fileContent = attachment.FileContent.Substring(index + 1);
                            byte[] contentByte = Convert.FromBase64String(fileContent);
                            entities.spInsApplicationAttachment(applicationAttchmentId, applicationId, attachment.FileName, fileType, attachment.FileExtension, contentByte, attachment.FileName);
                        }
                        else
                        {
                            fileContent = attachment.FileContent;
                            byte[] contentByte = Convert.FromBase64String(fileContent);
                            entities.spInsApplicationAttachment(applicationAttchmentId, applicationId, attachment.FileName, fileType, attachment.FileExtension, contentByte, attachment.FileName);
                        }
                    }

                }
            }
        }

        internal static void InsertApplicationRelationShip(List<ApplicationRelationInfo> lstApplicationRelations, int applicationId)
        {
            if (lstApplicationRelations == null) throw new ArgumentNullException("applicationRelation");

            string level = string.Empty;
            ObjectParameter applicationRelationshipId = new ObjectParameter("ApplicationRelationshipId", typeof(int));
            ObjectParameter applicationEmergencyContactId = new ObjectParameter("ApplicationEmergencyContactId", typeof(int));
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                //TODO: Add deceased flag
                foreach (ApplicationRelationInfo relation in lstApplicationRelations)
                {
                    if (relation.IsGuardian == false)
                    {
                        entities.spInsApplicationRelationship(applicationRelationshipId, applicationId, relation.RelationType, relation.RelationPrefix, relation.RelationFirstName, relation.RelationMiddleName, null, relation.RelationLastName,
                            null, relation.AttendedInstitution, relation.Profession, relation.MobileNumber, relation.RelationEmail, null, relation.Address, null, null, null, null, null, null, relation.Company, relation.CompanyAddress, relation.IsGuardian, relation.Deceased,null,relation.IdNumber);
                    }
                    else
                    {
                        entities.spInsApplicationEmergencyContact(applicationEmergencyContactId, applicationId, relation.RelationPrefix, relation.RelationFirstName, relation.RelationMiddleName, null, relation.RelationLastName,null, relation.RelationType,relation.MobileNumber, relation.RelationEmail,relation.GuardianAddress,relation.Profession);
                    }
                }
            }
        }


        internal static void InsertApplicationSiblingRelationShip(PersonalInfo personalInfo, int applicationId, int? siblingPrefixId)
        {
            if (personalInfo == null) throw new ArgumentNullException("applicationRelation");

            string level = string.Empty;
            ObjectParameter applicationRelationshipId = new ObjectParameter("ApplicationRelationshipId", typeof(int));
            ObjectParameter applicationEmergencyContactId = new ObjectParameter("ApplicationEmergencyContactId", typeof(int));
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                entities.spInsApplicationRelationship(applicationRelationshipId, applicationId, siblingPrefixId.HasValue ? siblingPrefixId.Value : default(Int32), null, personalInfo.SiblingFName, personalInfo.SiblingMName,
                    null, personalInfo.SiblingLName, null,false, null, null, null, null, null, null, null, null, null, null, null, null, null, false, false,personalInfo.SiblingTKHNumber,null);
            }
        }

        internal static void InsertApplicationTestScore(List<ApplicationTestScoreInfo> lstTestScore, int applicationId)
        {
            if (lstTestScore == null) throw new ArgumentNullException("applicationTetScore");

            string level = string.Empty;
            ObjectParameter applicationTestScoreId = new ObjectParameter("ApplicationTestScoreId", typeof(int));
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                foreach (ApplicationTestScoreInfo testScore in lstTestScore)
                {
                    //if (testScore.LevelId > 0)
                    //    level = entities.ITB_Level.FirstOrDefault(l => l.Id == testScore.LevelId).Value;

                    entities.spInsApplicationTestScore(applicationTestScoreId, applicationId, testScore.TestId, testScore.TestTypeId, DateTime.Now, Convert.ToDecimal(testScore.Total), string.Empty, string.Empty, testScore.UserName, testScore.Password);
                }
            }
        }

        internal static void InsertApplicationTests(List<ApplicationTestsInfo> lstTests, int applicationId)
        {
            if (lstTests == null) throw new ArgumentNullException("applicationTetScore");
            decimal testScore = 0;

            ObjectParameter applicationTestScoreId = new ObjectParameter("ApplicationTestScoreId", typeof(int));
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                foreach (ApplicationTestsInfo tests in lstTests)
                {

                    bool isNumeric = decimal.TryParse(tests.Score, out testScore);

                    if (isNumeric)
                    {
                        entities.spInsApplicationTestScore(applicationTestScoreId, applicationId, tests.Test, tests.TestType, tests.DateTaken.HasValue ? tests.DateTaken.Value < SqlDateTime.MinValue.Value ? SqlDateTime.MinValue.Value : tests.DateTaken.Value : (DateTime?)null, testScore,string.Empty, null, null, null);
                    }
                    else
                    {
                        entities.spInsApplicationTestScore(applicationTestScoreId, applicationId, tests.Test, tests.TestType, tests.DateTaken.HasValue ? tests.DateTaken.Value < SqlDateTime.MinValue.Value ? SqlDateTime.MinValue.Value : tests.DateTaken.Value : (DateTime?)null, 0, tests.Score, null, null, null);
                    }
                }
            }
        }

        internal static void InsertApplicationAddress(List<AddressInfo> lstAddress, int applicationId)
        {
            if (lstAddress == null) throw new ArgumentNullException("applicationAddress");
           // string cityName = "";
            ObjectParameter applicationAddressId = new ObjectParameter("ApplicationAddressId", typeof(int));
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                foreach (AddressInfo address in lstAddress)
                {
                    //if (address.City.HasValue && address.City.Value > 0)
                    //    cityName = entities.CODE_COUNTY.FirstOrDefault(city => city.CountyId == address.City).LONG_DESC;

                    entities.spInsApplicationAddress(applicationAddressId,
                    applicationId,
                    address.AddressTypeId,
                    // ITB:Ali Hassan:301125 - passing empty string for address line1 to avoid null exception in stored procedure
                    address.Line1 ??  string.Empty,
                    address.Line2,
                    null, null, null,
                    address.City.HasValue? address.City.Value .ToString(): string.Empty,
                    address.State.HasValue ? (int?)address.State.Value : null,
                    null, null,
                    // ITB:Ali Hassan:301125 - passing country 128 as N/A if country is 0 to avoid null exception in stored procedure
                    //ITB: Ahmed Mostafa 011225 - Fixing int issue.
                      address.Country == 0 ? (int?)128 : address.Country,

                    address.IsPrimary, null);
                }
            }
        }

        public static void InsertApplicationPhone(List<PhoneNumberInfo> lstPhoneType, int applicationId)
        {
            if (lstPhoneType == null) throw new ArgumentNullException("applicationPhone");

            ObjectParameter applicationPhoneId = new ObjectParameter("ApplicationPhoneId", typeof(int));
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                foreach (PhoneNumberInfo phoneNumber in lstPhoneType)

                    entities.spInsApplicationPhone(applicationPhoneId, applicationId, phoneNumber.PhoneType, phoneNumber.CountryId == 0 ? null : phoneNumber.CountryId, phoneNumber.PhoneNumber, phoneNumber.IsPrimary);
            }
        }

        public static void InsertApplicationSource(List<ApplicationSourceInfo> lstSource, int applicationId)
        {
            if (lstSource == null) throw new ArgumentNullException("applicationSource");

            ObjectParameter applicationSourceId = new ObjectParameter("ApplicationSourceId", typeof(int));
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                foreach (ApplicationSourceInfo source in lstSource)
                {
                    entities.spInsApplicationSource(applicationSourceId, applicationId, source.SourceId);
                }
            }
        }

        internal static void InsertApplicationEmployment(List<EmploymentInfo> lstEmployment, int applicationId)
        {
            if (lstEmployment == null) throw new ArgumentNullException("applicationEmployment");

            ObjectParameter applicationEmploymentId = new ObjectParameter("ApplicationEmploymentId", typeof(int));
            using (ApplicationFormEntities entities = new ApplicationFormEntities())

                foreach (EmploymentInfo employment in lstEmployment)
                {
                    string position = entities.CODE_POSITION.FirstOrDefault(p => p.PositionId == employment.Position).LONG_DESC;
                    //Insert application employment
                    entities.spInsApplicationEmployment(applicationEmploymentId, applicationId, employment.EmployerName, position,
                       employment.StartDate.HasValue ? employment.StartDate.Value < new DateTime(1900, 1, 1) ? new DateTime(1900, 1, 1) : employment.StartDate.Value : (DateTime?)null,
                       employment.EndDate.HasValue ? employment.EndDate.Value < new DateTime(1900, 1, 1) && employment.EndDate.Value > new DateTime(2079, 6, 6) ? new DateTime(1900, 1, 1) : employment.EndDate.Value : (DateTime?)null);
                }
        }
    }
}
