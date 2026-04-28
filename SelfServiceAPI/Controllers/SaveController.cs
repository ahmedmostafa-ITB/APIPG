using DataAccess;
using SelfServiceAPI.Classes;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using static SelfServiceAPI.Classes.Helper;

namespace SelfServiceAPI.Controllers
{
    [System.Web.Http.Cors.EnableCors(origins: "*", headers: "*", methods: "*")]
    public class SaveController : ApiController
    {
        [HttpPost]
        public HttpResponseMessage SaveApplication(ApplicationInfoRequest request)
        {
            IncompleteApplication incompleteApplication = null;
            ApplicationInfo deserializedApplicationInfo;
            List<PhoneNumberInfo> lstPhoneNumber = new List<PhoneNumberInfo>();
            List<ApplicationSourceInfo> lstApplicationSource = new List<ApplicationSourceInfo>();
            List<EducationInfo> lstEducation = new List<EducationInfo>();
            List<EmploymentInfo> lstEmployment = new List<EmploymentInfo>();
            List<ApplicationTestScoreInfo> lstTestScoreInfo = new List<ApplicationTestScoreInfo>();
            List<ApplicationTestsInfo> lstTestsInfo = new List<ApplicationTestsInfo>();

            try
            {
                if (request != null)
                {
                    using (ApplicationFormEntities entities = new ApplicationFormEntities())
                    {
                        incompleteApplication = entities.IncompleteApplications.FirstOrDefault(id => id.IncompleteApplicationId == request.IncompleteApplicationId);

                        if (incompleteApplication != null)
                        {
                            //Retreive the deserialized incompete application
                            deserializedApplicationInfo = ApplicationInfo.DeserializeApplicationInfo(incompleteApplication.ApplicationData);

                            //Insert personal information
                            InsertPersonalInformation(request, deserializedApplicationInfo);

                            //Insert government information
                            InsertGovernmentInformation(request, deserializedApplicationInfo);

                            //Insert postgraduate information
                            InsertPostgraduateInformation(request, deserializedApplicationInfo);

                            //Insert academic interest
                            InsertAcademicInterestInformation(request, deserializedApplicationInfo);

                            //Insert address information
                            InsertAddressInformation(request, deserializedApplicationInfo, entities);

                            //Insert contact information
                            InsertContactInformation(request, deserializedApplicationInfo, lstPhoneNumber);

                            //Insert relationship (Father)
                            InsertFatherRelationshipInformation(request, deserializedApplicationInfo);

                            //Insert relationship (Mother)
                            InsertMotherRelationshipInformation(request, deserializedApplicationInfo);

                            //Insert relationship (Guardian)
                            InsertGuradianRelationshipInformation(request, deserializedApplicationInfo);

                            //Insert health
                            InsertHealthInformation(request, deserializedApplicationInfo);

                            //Insert Sources
                            InsertSources(request, deserializedApplicationInfo, lstApplicationSource);

                            //Insert Education History
                            InsertEducationInformation(request, deserializedApplicationInfo, lstEducation);

                            //Insert Emloyment Info
                            InsertEmploymentInformation(request, deserializedApplicationInfo, lstEmployment);

                            //Insert Test Score
                            InsertTestScore(request, deserializedApplicationInfo, lstTestScoreInfo);

                            //Insert Test Score
                            InsertTests(request, deserializedApplicationInfo, lstTestsInfo);

                            //Insert Postgraduate Fields
                            InsertPostgraduateInformation(request, deserializedApplicationInfo);

                            deserializedApplicationInfo.SerializeApplicationInfo(incompleteApplication, entities, deserializedApplicationInfo);

                            SuccessReponse successReponse = new SuccessReponse()
                            {
                                StatusCode = (int)HttpStatusCode.OK,
                                Status = "success"
                            };
                            return Request.CreateResponse(successReponse);
                        }

                        else
                        {
                            ErrorResponse errorResponse = new ErrorResponse()
                            {
                                StatusCode = (int)HttpStatusCode.NotFound,
                                Status = "failure",
                                Msg = "Application id cannot be found "
                            };
                            return Request.CreateResponse(errorResponse);
                        }
                    }
                }
                else
                {
                    ErrorResponse errorResponse = new ErrorResponse()
                    {
                        StatusCode = (int)HttpStatusCode.NotFound,
                        Status = "failure",
                        Msg = "Request is null"
                    };
                    return Request.CreateResponse(errorResponse);
                }

            }
            catch (Exception exception)
            {
                ErrorResponse errorResponse = new ErrorResponse()
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Status = "failure",
                    Msg = "An error has occured. Please contact your administrator"
                };

                if (incompleteApplication != null)
                    LoggingManager.LogException(exception.Message, exception.StackTrace, DateTime.Now, string.Empty, incompleteApplication.IncompleteApplicationId.ToString());
                else
                    LoggingManager.LogException(exception.Message, exception.StackTrace, DateTime.Now, string.Empty, exception.Source);

                return Request.CreateResponse(errorResponse);
            }
        }

        private static void InsertGovernmentInformation(ApplicationInfoRequest request, ApplicationInfo deserializedApplicationInfo)
        {
            deserializedApplicationInfo.PassportNumber = request.PassportNumber;
            deserializedApplicationInfo.PassportExpirationDate = request.PassportExpirationdate.HasValue ? request.PassportExpirationdate.Value : (DateTime?)null;
            deserializedApplicationInfo.GovernmentId = request.NationalIdNumber;
            deserializedApplicationInfo.PassportCountryIssued = request.PassportCountryIssued;
            deserializedApplicationInfo.OtherSource = request.OtherSource;
        }

        private static void InsertPostgraduateInformation(ApplicationInfoRequest request, ApplicationInfo deserializedApplicationInfo)
        {
            if (deserializedApplicationInfo.PostgraduateInfo == null)
            {
                deserializedApplicationInfo.PostgraduateInfo = new PostgraduateInfo();
            }

            var pg = deserializedApplicationInfo.PostgraduateInfo;

            pg.MilitaryStatus = request.MilitaryStatus;
            pg.MaritalStatus = request.MaritalStatus;
            pg.EmploymentStatus = request.EmploymentStatus;
            pg.EmployerCompanyName = request.EmployerCompanyName;
            pg.EmployerPosition = request.EmployerPosition;
            pg.EmployerStartDate = request.EmployerStartDate;
            pg.EmployerDuties = request.EmployerDuties;
            pg.EmployerIdNumber = request.EmployerIdNumber;
            pg.CovAlumni = request.CovAlumni;
            pg.BachelorTaughtInEnglish = request.BachelorTaughtInEnglish.ToString().ToLower();
            pg.HasProfExam = request.HasProfExam.ToString().ToLower();
            pg.CoventryId = request.CoventryId;
            pg.PgEmergencyContactRelationship = request.PgEmergencyContactRelationship;
            pg.PgEmergencyContactGivenName = request.PgEmergencyContactGivenName;
            pg.PgEmergencyContactMiddleName = request.PgEmergencyContactMiddleName;
            pg.PgEmergencyContactFamilyName = request.PgEmergencyContactFamilyName;
            pg.PgEmergencyContactMobile = request.PgEmergencyContactMobile;
            pg.PgEmergencyContactEmail = request.PgEmergencyContactEmail;
            pg.AcademicAwards = request.AcademicAwards;
            pg.PgSchoolName = request.PgSchoolName;
            pg.PgBachelorUniversity = request.PgBachelorUniversity;
            pg.PgBachelorUniversityName = request.PgBachelorUniversityName;
            pg.PgBachelorDegree = request.PgBachelorDegree;
            pg.PgBachelorFieldOfStudy = request.PgBachelorFieldOfStudy;
            pg.PgBachelorYearOfGrad = request.PgBachelorYearOfGrad;
            pg.PgHasAcademicAward = request.PgHasAcademicAward;
            pg.PgAcademicAwards = request.PgAcademicAwards;
            pg.PgProfExamsData = request.PgProfExamsData;
            pg.AcademicSupport = request.AcademicSupport;
            pg.AcademicSupportDetails = request.AcademicSupportDetails;
        }

        private void InsertTestScore(ApplicationInfoRequest request, ApplicationInfo deserializedApplicationInfo, List<ApplicationTestScoreInfo> lstTestScoreInfo)
        {
            if (request.TestScore != null)
            {
                foreach (TestRequest item in request.TestScore)
                {
                    lstTestScoreInfo.Add(new ApplicationTestScoreInfo()
                    {
                        TestId = item.Test,
                        TestTypeId = item.TestType,
                        Score = item.Score,
                        ScoreId = item.ScoreId,
                        LevelId = item.LevelId,
                        SubjectId = item.SubjectId,
                        Total = item.Total,
                        UserName = item.UserName,
                        Password = item.Password
                    });
                }

                //Clear the existing data to override it
                deserializedApplicationInfo.TestScores.Clear();

                foreach (ApplicationTestScoreInfo item in lstTestScoreInfo)
                {
                    deserializedApplicationInfo.TestScores.Add(item);
                }
            }

            else
            {
                deserializedApplicationInfo.TestScores.Clear();
            }
        }

        private void InsertTests(ApplicationInfoRequest request, ApplicationInfo deserializedApplicationInfo, List<ApplicationTestsInfo> lstTestsInfo)
        {
            if (request.Tests != null)
            {
                foreach (TestsRequest item in request.Tests)
                {
                    lstTestsInfo.Add(new ApplicationTestsInfo()
                    {
                        Test = item.Test,
                        TestType = item.TestType,
                        Score = item.Score,
                        DateTaken = item.DateTaken
                    });
                }

                //Clear the existing data to override it
                deserializedApplicationInfo.Tests.Clear();

                foreach (ApplicationTestsInfo item in lstTestsInfo)
                {
                    deserializedApplicationInfo.Tests.Add(item);
                }
            }
            else
            {
                deserializedApplicationInfo.Tests.Clear();
            }
        }

        private static void InsertSources(ApplicationInfoRequest request, ApplicationInfo deserializedApplicationInfo, List<ApplicationSourceInfo> lstApplicationSource)
        {
            if (request.SourceInformation != null)
            {
                foreach (ApplicationSourceInfo item in request.SourceInformation)
                {
                    lstApplicationSource.Add(new ApplicationSourceInfo()
                    {
                        SourceId = item.SourceId
                    });
                }

                //Clear the existing data to override it
                deserializedApplicationInfo.SourceInfo.Clear();

                foreach (ApplicationSourceInfo item in lstApplicationSource)
                {
                    deserializedApplicationInfo.SourceInfo.Add(item);
                }
            }
            else
            {
                deserializedApplicationInfo.SourceInfo.Clear();
            }
        }

        private static void InsertHealthInformation(ApplicationInfoRequest request, ApplicationInfo deserializedApplicationInfo)
        {
            List<ApplicationUserDefinedInfo> lstApplicationUserDefinedInfo = new List<ApplicationUserDefinedInfo>
                            {
                                new ApplicationUserDefinedInfo()
                                {
                                    ColumnLabel = "DISABILITIES",
                                    ColumnName = "DISABILITIES",
                                    ColumnType = 1,
                                    ColumnValue = request.HasBeenIll.ToString(),
                                    Description = "DISABILITIES",
                                    IsUploading = true

                                },

                                new ApplicationUserDefinedInfo()
                                {

                                    ColumnLabel = "DISABILITIES_DESC",
                                    ColumnName = "DISABILITIES_DESC",
                                    ColumnType =1,
                                    ColumnValue = request.HasBeenIll ? request.IllnessDescription : string.Empty,
                                    Description = "DISABILITIES_DESC",
                                    IsUploading =true

                                }
                            };

            if (deserializedApplicationInfo.UserDefined == null)
            {
                deserializedApplicationInfo.UserDefined = new List<ApplicationUserDefinedInfo>();
            }
            deserializedApplicationInfo.UserDefined.RemoveAll(desc => desc.Description.Equals("DISABILITIES") || desc.Description.Equals("DISABILITIES_DESC"));

            foreach (ApplicationUserDefinedInfo item in lstApplicationUserDefinedInfo)
            {
                if (!string.IsNullOrEmpty(item.ColumnValue))
                    deserializedApplicationInfo.UserDefined.Add(item);
            }
        }

        private static void InsertFatherRelationshipInformation(ApplicationInfoRequest request, ApplicationInfo deserializedApplicationInfo)
        {
            List<ApplicationRelationInfo> lstApplicationRelationInfo = new List<ApplicationRelationInfo>
                             {
                                new ApplicationRelationInfo()
                                {
                                    Address = request.FatherAddress,
                                    Company = request.FatherCompany,
                                    CompanyAddress = request.FatherCompanyAddress,
                                    IsGuardian = false ,
                                    MobileNumber = request.FatherMobileNumber,
                                    Profession = request.FatherProfession,
                                    RelationEmail = request.FatherEmail,
                                    RelationFirstName = request.FatherFirstName,
                                    RelationLastName = request.FatherFamilyName,
                                    RelationMiddleName = request.FatherMiddleName,
                                    RelationPrefix = request.FatherPrefix,
                                    RelationType = request.FatherRelationType,
                                    AttendedInstitution = request.FatherAttendedInstitution,
                                    Deceased = request.FatherDeceased,
                                    IsTKHEmployee=request.IsFatherTKHEmployee,
                                    IdNumber=request.FatherIdNumber,

                                }
                             };

            if (request.FatherRelationType >= 0)
                deserializedApplicationInfo.ApplicationRelations.RemoveAll(relation => relation.RelationType == request.FatherRelationType && relation.IsGuardian == false);

            foreach (ApplicationRelationInfo item in lstApplicationRelationInfo)
            {
                if (request.FatherRelationType > 0 && request.FatherPrefix > 0)
                    deserializedApplicationInfo.ApplicationRelations.Add(item);
            }
        }

        private static void InsertMotherRelationshipInformation(ApplicationInfoRequest request, ApplicationInfo deserializedApplicationInfo)
        {
            List<ApplicationRelationInfo> lstApplicationRelationInfo = new List<ApplicationRelationInfo>
                             {
                                new ApplicationRelationInfo()
                                {
                                    Address = request.MotherAddress,
                                    Company = request.MotherCompany,
                                    CompanyAddress = request.MotherCompanyAddress,
                                    IsGuardian = false ,
                                    MobileNumber = request.MotherMobileNumber,
                                    Profession = request.MotherProfession,
                                    RelationEmail = request.MotherEmail,
                                    RelationFirstName = request.MotherFirstName,
                                    RelationLastName = request.MotherFamilyName,
                                    RelationMiddleName = request.MotherMiddleName,
                                    RelationPrefix = request.MotherPrefix,
                                    RelationType = request.MotherRelationType,
                                    AttendedInstitution = request.MotherAttendedInstitution,
                                    Deceased = request.MotherDeceased,
                                    IsTKHEmployee =request.IsMotherTKHEmployee,
                                    IdNumber=request.MotherIdNumber
                                }
                             };

            if (request.MotherRelationType >= 0)
                deserializedApplicationInfo.ApplicationRelations.RemoveAll(relation => relation.RelationType == request.MotherRelationType && relation.IsGuardian == false);

            foreach (ApplicationRelationInfo item in lstApplicationRelationInfo)
            {
                if (request.MotherRelationType > 0 && request.MotherPrefix > 0)
                    deserializedApplicationInfo.ApplicationRelations.Add(item);
            }
        }

        private static void InsertGuradianRelationshipInformation(ApplicationInfoRequest request, ApplicationInfo deserializedApplicationInfo)
        {
            List<ApplicationRelationInfo> lstApplicationRelationInfo = new List<ApplicationRelationInfo>
                             {
                                new ApplicationRelationInfo()
                                {
                                    IsGuardian = true ,
                                    MobileNumber = request.GuardianMobileNumber,
                                    Profession = request.GuardianProfession,
                                    RelationEmail = request.GuardianEmail,
                                    RelationFirstName = request.GuardianFirstName,
                                    RelationLastName = request.GuardianFamilyName,
                                    RelationMiddleName = request.GuardianMiddleName,
                                    RelationPrefix = request.GuardianPrefix,
                                    RelationType = request.GuardianRelationType,
                                    GuardianAddress  = request.GuardianAddress
                                }
                             };


            if (request.GuardianRelationType >= 0)
                deserializedApplicationInfo.ApplicationRelations.RemoveAll(relation => relation.IsGuardian == true);

            foreach (ApplicationRelationInfo item in lstApplicationRelationInfo)
            {
                if (request.GuardianRelationType > 0 && request.GuardianPrefix > 0)
                    deserializedApplicationInfo.ApplicationRelations.Add(item);
            }
        }

        private static void InsertSiblingRelationshipInformation(ApplicationInfoRequest request, ApplicationInfo deserializedApplicationInfo)
        {
            List<ApplicationRelationInfo> lstApplicationRelationInfo = new List<ApplicationRelationInfo>
                             {
                                new ApplicationRelationInfo()
                                {
                                    IsGuardian = true ,
                                    MobileNumber = request.GuardianMobileNumber,
                                    Profession = request.GuardianProfession,
                                    RelationEmail = request.GuardianEmail,
                                    RelationFirstName = request.GuardianFirstName,
                                    RelationLastName = request.GuardianFamilyName,
                                    RelationMiddleName = request.GuardianMiddleName,
                                    RelationPrefix = request.GuardianPrefix,
                                    RelationType = request.GuardianRelationType,
                                    GuardianAddress = request.GuardianAddress
                                }
                             };


            if (request.GuardianRelationType >= 0)
                deserializedApplicationInfo.ApplicationRelations.RemoveAll(relation => relation.IsGuardian == true);

            foreach (ApplicationRelationInfo item in lstApplicationRelationInfo)
            {
                if (request.GuardianRelationType > 0 && request.GuardianPrefix > 0)
                    deserializedApplicationInfo.ApplicationRelations.Add(item);
            }
        }

        private static void InsertContactInformation(ApplicationInfoRequest request, ApplicationInfo deserializedApplicationInfo, List<PhoneNumberInfo> lstPhoneNumber)
        {
            if (request.ContactInformation != null && request.ContactInformation.Count > 0)
            {
                foreach (PhoneNumberInfo item in request.ContactInformation)
                {
                    lstPhoneNumber.Add(new PhoneNumberInfo()
                    {
                        CountryId = item.CountryId,
                        PhoneNumber = item.PhoneNumber,
                        PhoneType = item.PhoneType,
                        Email = item.Email,
                        IsPrimary = item.IsPrimary
                    });
                }

                //Clear the existing data to override it
                deserializedApplicationInfo.PhoneNumber.Clear();

                foreach (PhoneNumberInfo item in lstPhoneNumber)
                {
                    deserializedApplicationInfo.PhoneNumber.Add(item);
                }
            }
            else
            {
                deserializedApplicationInfo.PhoneNumber.Clear();
            }
        }

        private static void InsertAddressInformation(ApplicationInfoRequest request, ApplicationInfo deserializedApplicationInfo, ApplicationFormEntities entities)
        {

            List<AddressInfo> lstAddressInformation = new List<AddressInfo>
                            {
                                new AddressInfo()
                                {
                                    AddressTypeId = request.AddressTypeId,
                                    Country = request.Country,
                                    City = request.City,
                                    State = request.StateProvinceId,
                                    //HouseNumber = request.HouseNumber,
                                    Line1 = request.Line1,
                                    Line2 = request.Line2,
                                    //Line3 = request.Line3,
                                   // Line4 = request.Line4,
                                   // PostalCode = request.PostalCode,
                                   DormPlanInterest=request.DormPlanInterest,
                                    IsPrimary = true
                                }
                            };

            //Clear the existing data to override it
            deserializedApplicationInfo.Address.Clear();

            foreach (AddressInfo item in lstAddressInformation)
            {
                deserializedApplicationInfo.Address.Add(item);
            }
        }

        private static void InsertAcademicInterestInformation(ApplicationInfoRequest request, ApplicationInfo deserializedApplicationInfo)
        {
            AcademicInterest academicInterest = new AcademicInterest
            {
                ApplicationFormSettingId = Convert.ToInt32(ConfigurationManager.AppSettings["ApplicationFormSettings"]),
                ApplicationProgramSettingId = Convert.ToInt32(ConfigurationManager.AppSettings["ApplicationProgramSettings"]),
                IncompleteApplicationId = request.IncompleteApplicationId,
                ProgramOfStudy = request.ProgramOfStudyId,
                FullPartTime = 1, //always full time
                FullPartTimeDescription = "Full Time",
                IsFirstChoice = true,
                ProgramDegreeCurriculumDescription = request.Description,
                ProgramDegreeCurriculumSecondDescription = request.SecDescription,
                SessionPeriodId = request.EntryTerm,
                ProgramId = request.ProgramId,
                UniversityId = request.UniversityId.HasValue ? request.UniversityId.Value : (int?)null,
                SecondUniversityId = request.SecondUniversityId.HasValue ? request.SecondUniversityId.Value : (int?)null,
                SecondProgramId = request.SecondProgramId.HasValue ? request.SecondProgramId.Value : (int?)null,
                SecondMajorId = request.SecondMajorId.HasValue ? request.SecondMajorId.Value : (int?)null,
                Level = request.Level,
                SecondLevel = request.SecondLevel,
                PreferredCampus=request.PreferredCampus
            };

            deserializedApplicationInfo.AcademicInterest = academicInterest;
        }

        private static void InsertPersonalInformation(ApplicationInfoRequest request, ApplicationInfo deserializedApplicationInfo)
        {
            PersonalInfo personalInfo = new PersonalInfo()
            {
                FirstName = request.FirstName,
                MiddleName = request.MiddleName,
                LastName = request.LastName,
                Prefix = request.Prefix,
                SiblingFName = request.SiblingFName,
                SiblingMName = request.SiblingMName,
                SiblingLName = request.SiblingLName,
                SiblingTKHNumber = request.SiblingTKHNumber
               
            };

            deserializedApplicationInfo.PersonalInfo = personalInfo;

            Demographic demographic = new Demographic()
            {
                BirthDate = request.DateOfBirth.HasValue ? request.DateOfBirth.Value : (DateTime?)null,
                CountryOfBirth = request.CountryOfBirth,
                PrimaryCitizenship = request.Nationality,
                Gender = request.Gender,
            };

            deserializedApplicationInfo.Demographic = demographic;

            List<ApplicationUserDefinedInfo> lstApplicationUserDefinedInfo = new List<ApplicationUserDefinedInfo>();

            foreach (ArabicDataRequest arabicDataRequest in request.ArabicData)
            {
                if (!string.IsNullOrEmpty(arabicDataRequest.ColumnValue))
                {
                    lstApplicationUserDefinedInfo.Add(new ApplicationUserDefinedInfo()
                    {
                        ColumnLabel = arabicDataRequest.ColumnName,
                        ColumnName = arabicDataRequest.ColumnName,
                        ColumnType = 1,
                        ColumnValue = arabicDataRequest.ColumnValue,
                        Description = "ArabicData",
                        IsUploading = true
                    });
                }
            }

            if (deserializedApplicationInfo.UserDefined == null)
            {
                deserializedApplicationInfo.UserDefined = new List<ApplicationUserDefinedInfo>();
            }
            deserializedApplicationInfo.UserDefined.RemoveAll(desc => desc.Description != null && desc.Description.Equals("ArabicData"));

            foreach (ApplicationUserDefinedInfo item in lstApplicationUserDefinedInfo)
            {
                deserializedApplicationInfo.UserDefined.Add(item);
            }
        }

        private static void InsertEducationInformation(ApplicationInfoRequest request, ApplicationInfo deserializedApplicationInfo, List<EducationInfo> lstEducationInfo)
        {
            if (request.EducationHistory != null)
            {
                foreach (EducationInfoRequest item in request.EducationHistory)
                {
                    lstEducationInfo.Add(new EducationInfo()
                    {

                        CurriculumId = item.CurriculumId,
                        InstitutionId = item.InstitutionId,
                        InstitutionName = item.InstitutionName,
                        InstitutionCountryId = item.CountryId,
                        InstitutionCityId=item.CityId,
                        TransferInstitutionCountryId=item.TransferCountryId,
                        TransferInstitutionCityId = item.TransferCityId,
                        EndDate = item.EndDate.HasValue ? item.EndDate.Value : (DateTime?)null,
                        GPA = item.GPA,
                       // IsTransfer = item.TransferedInstitutionId != null ? item.TransferedInstitutionId.Equals("Other", StringComparison.CurrentCultureIgnoreCase) ? true : false : true,
                        IsTransfer = !string.IsNullOrEmpty(item.TransferedInstitutionId) ? true: false,
                        TransferedInstitutionId = item.TransferedInstitutionId,
                        TransferedInstitutionName = item.TransferedInstitutionName,
                        TransferredCerti = item.TransferredCerti,
                        TransferredGPA = item.TransferredGPA,
                        EducationGrade = item.EducationGrade

                    });
                }

                //Clear the existing data to override it
                deserializedApplicationInfo.PriorEducation.Clear();

                foreach (EducationInfo item in lstEducationInfo)
                {
                    deserializedApplicationInfo.PriorEducation.Add(item);
                }
            }

            /* else
             {
                 deserializedApplicationInfo.PriorEducation.Clear();
             }*/
        }

        private static void InsertEmploymentInformation(ApplicationInfoRequest request, ApplicationInfo deserializedApplicationInfo, List<EmploymentInfo> lstEmploymentInfo)
        {
            if (request.Employment != null)
            {
                foreach (EmploymentInfoRequest item in request.Employment)
                {
                    lstEmploymentInfo.Add(new EmploymentInfo()
                    {
                        EmployerName = item.EmployerName,
                        Position = item.Position,
                        StartDate = item.StartDate.HasValue ? item.StartDate.Value : (DateTime?)null,
                        EndDate = item.EndDate.HasValue ? item.EndDate.Value : (DateTime?)null,
                    });
                }

                //Clear the existing data to override it
                deserializedApplicationInfo.Employment.Clear();

                foreach (EmploymentInfo item in lstEmploymentInfo)
                {
                    deserializedApplicationInfo.Employment.Add(item);
                }
            }

            else
            {
                deserializedApplicationInfo.Employment.Clear();
            }
        }


    }
}
