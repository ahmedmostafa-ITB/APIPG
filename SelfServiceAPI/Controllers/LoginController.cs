using DataAccess;
using SelfServiceAPI.Classes;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Cors;

namespace SelfServiceAPI.Controllers
{
    [System.Web.Http.Cors.EnableCors(origins: "*", headers: "*", methods: "*")]
 
    public class LoginController : ApiController
    {
        private const string Mother = "MTHR";
        private const string Father = "FTHR";
        private const string ArabicData = "ArabicData";
        private const string HasBeenIll = "HasBeenIll";
        private const string IllDescription = "IllDescription";
        private const string GCSETest = "GCSE";
        private const string IGTest = "IG";

        [HttpPost]
        public HttpResponseMessage UserLogin([FromBody] Login login)
        {
   
            bool isAuthenticated = false;
            bool isSubmitted = false;

            if (login != null)
            {
                isAuthenticated = Login.UserLogin(login.Email, login.Password);
               //isSubmitted = false; 
                isSubmitted =  Login.ApplicationSubmitted(login.Email);
                //TODO:Ali
                if (isSubmitted)
                {
                    ErrorResponse appSubmitted = new ErrorResponse()
                    {
                        StatusCode = (int)HttpStatusCode.NotFound,
                        Status = "failure",
                        Msg = "You have already submitted your application."
                    };
                    return Request.CreateResponse(appSubmitted);
                }

                if (isAuthenticated)
                {
                    using (ApplicationFormEntities entities = new ApplicationFormEntities())
                    {
                        IncompleteApplication incompleteApplication = entities.IncompleteApplications.FirstOrDefault(app => app.Email == login.Email && app.IsEmailVerified == true);

                        if (incompleteApplication != null)
                        {
                            // Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity(login.Email), null);

                            AdditionalResponse userFound = new AdditionalResponse()
                            {
                                StatusCode = (int)HttpStatusCode.OK,
                                Status = "success",
                                Data = new AdditionalInfo { IncompleteApplicationId = incompleteApplication.IncompleteApplicationId.ToString(), Token = incompleteApplication.Token.ToString() }
                            };
                            return Request.CreateResponse(userFound);
                        }
                        else
                        {
                            ErrorResponse userNotFound = new ErrorResponse()
                            {
                                StatusCode = (int)HttpStatusCode.NotFound,
                                Status = "failure",
                                Msg = "You must verify your account first before accessing the application."
                            };
                            return Request.CreateResponse(userNotFound);
                        }
                    }
                }
                else
                {
                    ErrorResponse invalidCredentails = new ErrorResponse()
                    {
                        StatusCode = (int)HttpStatusCode.NotFound,
                        Status = "failure",
                        Msg = "Email address or Password are not valid."
                    };
                    return Request.CreateResponse(invalidCredentails);
                }
            }
            else
            {
                ErrorResponse error = new ErrorResponse()
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Status = "failure",
                    Msg = "Request body is null."
                };
                return Request.CreateResponse(error);
            }

        }


        [HttpPost]
        public HttpResponseMessage ResetPassword([FromBody] ResetPassword request)
        {
            List<spGetUserByEmail_Result> result;

            using (PowerCampusIdentityEntities pcEntities = new PowerCampusIdentityEntities())
            {
                if (request != null)
                {
                    result = pcEntities.spGetUserByEmail(request.Email).ToList();

                    if (result != null && result.Count > 0)
                    {
                        //Generate passwod
                        string generatedPassword = PasswordGenerator.GeneratePassword(3, 3, 1, 1);

                        //Sent the password to the candidate — look up applicant name from IncompleteApplication
                        string firstName = "";
                        string lastName = "";
                        using (ApplicationFormEntities appEntities = new ApplicationFormEntities())
                        {
                            var incApp = appEntities.IncompleteApplications.FirstOrDefault(app => app.Email == request.Email);
                            if (incApp != null && !string.IsNullOrEmpty(incApp.ApplicationData))
                            {
                                var appInfo = ApplicationInfo.DeserializeApplicationInfo(incApp.ApplicationData);
                                if (appInfo != null && appInfo.PersonalInfo != null)
                                {
                                    firstName = appInfo.PersonalInfo.FirstName ?? "";
                                    lastName = appInfo.PersonalInfo.LastName ?? "";
                                }
                            }
                        }
                        SendEmail.ProcessSendEmail(request.Email, generatedPassword, firstName, lastName);

                        //Update the password on the database
                        pcEntities.spUpdUserById(result[0].IdentityUserId, result[0].UserName, result[0].Email, EncryptDecryptText.Encryptword(generatedPassword));

                        ErrorResponse EmailSent = new ErrorResponse()
                        {
                            StatusCode = (int)HttpStatusCode.OK,
                            Status = "success",
                            Msg = "You received an E-mail with your new password"
                        };
                        return Request.CreateResponse(EmailSent);
                    }
                    else
                    {
                        ErrorResponse EmailNotRegistered = new ErrorResponse()
                        {
                            StatusCode = (int)HttpStatusCode.NotFound,
                            Status = "failure",
                            Msg = "The E-mail address you entered is invalid"
                        };
                        return Request.CreateResponse(EmailNotRegistered);
                    }
                }
                else
                {
                    ErrorResponse EmailNotRegistered = new ErrorResponse()
                    {
                        StatusCode = (int)HttpStatusCode.NotFound,
                        Status = "failure",
                        Msg = "The request is null"
                    };
                    return Request.CreateResponse(EmailNotRegistered);
                }
            }
        }

        [HttpPost]
        public HttpResponseMessage GetPrefilledData([FromBody] OnLogin request)
        {
            try
            {
                IncompleteApplication incompleteApplication;
                ApplicationInfo deserializedApplication;
                ApplicationInfoLogin loginData = new ApplicationInfoLogin();
                ApplicationInfoData data = new ApplicationInfoData();
                List<ApplicationTestScoreLoginInfo> lstTestScoresInfo = new List<ApplicationTestScoreLoginInfo>();
                List<ApplicationTestsLoginInfo> lstTestInfo = new List<ApplicationTestsLoginInfo>();
                List<ApplicationUserDefinedLoginInfo> lstUserDefinedLoginInfo = new List<ApplicationUserDefinedLoginInfo>();
                List<ApplicationSourceloginInfo> lstSourceLoginInfo = new List<ApplicationSourceloginInfo>();
                List<EducationLoginHistory> lstEducationLoginHistory = new List<EducationLoginHistory>();
                List<Employment> lstEmployment = new List<Employment>();

                if (request != null)
                {
                    using (ApplicationFormEntities entities = new ApplicationFormEntities())
                    {
                        incompleteApplication = entities.IncompleteApplications.FirstOrDefault(app => app.IncompleteApplicationId == request.IncompleteApplicationId
                                                                                                    && app.Token == request.Token);

                        if (incompleteApplication != null)
                        {
                            string applicationData = incompleteApplication.ApplicationData;
                            deserializedApplication = ApplicationInfo.DeserializeApplicationInfo(applicationData);

                            if (deserializedApplication != null)
                            {
                                data.IncompleteApplicationId = incompleteApplication.IncompleteApplicationId;
                                data.NationalIdNumber = deserializedApplication.GovernmentId;
                                data.PassportNumber = deserializedApplication.PassportNumber;
                                data.PassportCountryIssued = deserializedApplication.PassportCountryIssued;
                                data.PassportExpirationDate = deserializedApplication.PassportExpirationDate;
                                data.Email = deserializedApplication.Email;
                                data.Validated = deserializedApplication.Validated;
                                data.OtherSource = deserializedApplication.OtherSource;
                                data.UndergraduateId = Convert.ToInt32(ConfigurationManager.AppSettings["UnderGraduateProgram"]);
                                data.GraduateId = Convert.ToInt32(ConfigurationManager.AppSettings["GraduateProgram"]);
                                data.MotherId = Convert.ToInt32(ConfigurationManager.AppSettings["MotherRelationId"]);
                                data.FatherId = Convert.ToInt32(ConfigurationManager.AppSettings["FatherRelationId"]);
                                data.ThanawiyaAmma = Convert.ToInt32(ConfigurationManager.AppSettings["ThanaweyaAmaa"]);
                                data.IGCSE = Convert.ToInt32(ConfigurationManager.AppSettings["IGCSE"]);
                                data.GCSE = Convert.ToInt32(ConfigurationManager.AppSettings["GCSE"]);
                                data.IBDiploma = Convert.ToInt32(ConfigurationManager.AppSettings["IBDiploma"]);
                                data.AmericanHighSchool = Convert.ToInt32(ConfigurationManager.AppSettings["AMERDiploma"]);
                              
                            
                                if (deserializedApplication.PersonalInfo != null)
                                {
                                    data.FirstName = deserializedApplication.PersonalInfo.FirstName;
                                    data.LastName = deserializedApplication.PersonalInfo.LastName;
                                    data.MiddleName = deserializedApplication.PersonalInfo.MiddleName;
                                    data.Prefix = deserializedApplication.PersonalInfo.Prefix;
                                    data.SiblingFName = deserializedApplication.PersonalInfo.SiblingFName;
                                    data.SiblingMName = deserializedApplication.PersonalInfo.SiblingMName;
                                    data.SiblingLName = deserializedApplication.PersonalInfo.SiblingLName;
                                    data.SiblingTKHNumber = deserializedApplication.PersonalInfo.SiblingTKHNumber;
                                }

                                if (deserializedApplication.Demographic != null)
                                {
                                    data.DateOfBirth = deserializedApplication.Demographic.BirthDate;
                                    data.Nationality = deserializedApplication.Demographic.PrimaryCitizenship;
                                    data.CountryOfBirth = deserializedApplication.Demographic.CountryOfBirth;
                                    data.Gender = deserializedApplication.Demographic.Gender;
                                }

                                if (deserializedApplication.Address != null && deserializedApplication.Address.Count > 0)
                                {
                                    data.ApplicationFormSettingId = Convert.ToInt32(ConfigurationManager.AppSettings["ApplicationFormEntitiesSettings"]);
                                    data.ApplicationProgramSettingId = Convert.ToInt32(ConfigurationManager.AppSettings["ApplicationProgramSettings"]);
                                    data.AddressTypeId = deserializedApplication.Address[0].AddressTypeId;
                                    // data.HouseNumber = deserializedApplication.Address[0].HouseNumber;
                                    data.Line1 = deserializedApplication.Address[0].Line1;
                                    data.Line2 = deserializedApplication.Address[0].Line2;
                                    //data.Line4 = deserializedApplication.Address[0].Line4;
                                    //data.Line3 = deserializedApplication.Address[0].Line3;
                                    data.Country = deserializedApplication.Address[0].Country;
                                    data.City = deserializedApplication.Address[0].City;
                                    data.DormPlanInterest = deserializedApplication.Address[0].DormPlanInterest;
                                    data.StateProvinceId = deserializedApplication.Address[0].State;
                                    //data.PostalCode = deserializedApplication.Address[0].PostalCode;
                                }

                                if (deserializedApplication.TestScores != null && deserializedApplication.TestScores.Count > 0)
                                {
                                    foreach (ApplicationTestScoreInfo testScoreInfo in deserializedApplication.TestScores)
                                    {
                                        lstTestScoresInfo.Add(new ApplicationTestScoreLoginInfo()
                                        {
                                            TestId = testScoreInfo.TestId,
                                            TestTypeId = testScoreInfo.TestTypeId,
                                            UserName = testScoreInfo.UserName,
                                            Password = testScoreInfo.Password,
                                            Score = testScoreInfo.Score,
                                            Total = testScoreInfo.Total,
                                            SubjectId = testScoreInfo.SubjectId,
                                            ScoreId = testScoreInfo.ScoreId,
                                            LevelId = testScoreInfo.LevelId
                                        });
                                    }
                                    data.Test = lstTestScoresInfo;
                                }

                                if (deserializedApplication.Tests != null && deserializedApplication.Tests.Count > 0)
                                {
                                    foreach (ApplicationTestsInfo testsInfo in deserializedApplication.Tests)
                                    {
                                        lstTestInfo.Add(new ApplicationTestsLoginInfo()
                                        {
                                            Test = testsInfo.Test,
                                            TestType = testsInfo.TestType,
                                            Score = testsInfo.Score,
                                            DateTaken = testsInfo.DateTaken

                                        });
                                    }
                                    data.Tests = lstTestInfo;
                                }


                                if (deserializedApplication.PhoneNumber != null && deserializedApplication.PhoneNumber.Count > 0)
                                {
                                    data.ContactInformation = deserializedApplication.PhoneNumber;
                                }

                                if (deserializedApplication.AcademicInterest != null)
                                {
                                    int? firstUni = deserializedApplication.AcademicInterest.UniversityId.HasValue ? deserializedApplication.AcademicInterest.UniversityId.Value : (int?)null;
                                    int? secondUni = deserializedApplication.AcademicInterest.SecondUniversityId.HasValue ? deserializedApplication.AcademicInterest.SecondUniversityId.Value : (int?)null;

                                    data.ProgramOfStudyId = deserializedApplication.AcademicInterest.ProgramOfStudy;
                                    data.EntryTerm = deserializedApplication.AcademicInterest.SessionPeriodId;
                                    data.Description = deserializedApplication.AcademicInterest.ProgramDegreeCurriculumDescription;
                                    data.ProgramId = deserializedApplication.AcademicInterest.ProgramId;
                                    data.UniversityId = deserializedApplication.AcademicInterest.UniversityId.HasValue ? deserializedApplication.AcademicInterest.UniversityId.Value : (int?)null;
                                    data.SecondUniversityId = deserializedApplication.AcademicInterest.SecondUniversityId.HasValue ? deserializedApplication.AcademicInterest.SecondUniversityId.Value : (int?)null;
                                    data.SecondProgramId = deserializedApplication.AcademicInterest.SecondProgramId.HasValue ? deserializedApplication.AcademicInterest.SecondProgramId.Value : (int?)null;
                                    data.SecondMajorId = deserializedApplication.AcademicInterest.SecondMajorId.HasValue ? deserializedApplication.AcademicInterest.SecondMajorId.Value : (int?)null;
                                    data.SecDescription = deserializedApplication.AcademicInterest.ProgramDegreeCurriculumSecondDescription;
                                    data.Level = deserializedApplication.AcademicInterest.Level;
                                    data.SecondLevel = deserializedApplication.AcademicInterest.SecondLevel;
                                    data.PreferredCampus = deserializedApplication.AcademicInterest.PreferredCampus;
                                    if (data.UniversityId != null)
                                    {
                                        if (entities.CODE_POPULATION.FirstOrDefault(pop => pop.isActive == "1" && pop.PopulationId == firstUni) == null)
                                        {
                                            data.UniversityId = null;
                                            data.ProgramId = 0;
                                            data.EntryTerm = 0;
                                            data.Level = null;
                                            data.SecondUniversityId = null;
                                            data.SecondProgramId = 0;
                                            data.SecondLevel = null;

                                        }

                                    }
                                    if (data.SecondUniversityId != null)
                                    {
                                        if (entities.CODE_POPULATION.FirstOrDefault(pop => pop.isActive == "1" && pop.PopulationId == secondUni) == null)
                                        {
                                            data.SecondUniversityId = null;
                                            data.SecondProgramId = 0;
                                            data.SecondLevel = null;

                                        }

                                    }

                                }

                                if (deserializedApplication.SourceInfo != null && deserializedApplication.SourceInfo.Count > 0)
                                {
                                    foreach (ApplicationSourceInfo sourceInfo in deserializedApplication.SourceInfo)
                                    {
                                        if (sourceInfo.SourceId > 0)
                                        {
                                            lstSourceLoginInfo.Add(new ApplicationSourceloginInfo()
                                            {
                                                Id = sourceInfo.SourceId,
                                                Value = entities.SOURCEs.FirstOrDefault(source => source.SourceId == sourceInfo.SourceId).NAME
                                            });
                                        }
                                    }
                                    data.ApplicationTestSourceId = lstSourceLoginInfo;
                                }

                                if (deserializedApplication.ApplicationRelations != null && deserializedApplication.ApplicationRelations.Count > 0)
                                {
                                    CODE_RELATIONSHIP relation = entities.CODE_RELATIONSHIP.First(p => p.CODE_VALUE == Father);
                                    if (relation != null)
                                    {
                                        data.FatherInfo = deserializedApplication.ApplicationRelations.FirstOrDefault(app => app.RelationType == relation.RelationTypeId && app.IsGuardian == false);
                                    }
                                }

                                if (deserializedApplication.ApplicationRelations != null && deserializedApplication.ApplicationRelations.Count > 0)
                                {
                                    CODE_RELATIONSHIP relation = entities.CODE_RELATIONSHIP.First(p => p.CODE_VALUE == Mother);
                                    if (relation != null)
                                    {
                                        data.MotherInfo = deserializedApplication.ApplicationRelations.FirstOrDefault(app => app.RelationType == relation.RelationTypeId && app.IsGuardian == false);
                                    }
                                }

                                if (deserializedApplication.ApplicationRelations != null && deserializedApplication.ApplicationRelations.Count > 0)
                                {
                                    data.GuardianInfo = deserializedApplication.ApplicationRelations.FirstOrDefault(app => app.IsGuardian == true);
                                }

                                if (deserializedApplication.UserDefined != null && deserializedApplication.UserDefined.Count > 0)
                                {
                                    foreach (ApplicationUserDefinedInfo userDefinedInfo in deserializedApplication.UserDefined.Where(arData => arData.Description == ArabicData))
                                    {
                                        lstUserDefinedLoginInfo.Add(new ApplicationUserDefinedLoginInfo()
                                        {
                                            ColumnName = userDefinedInfo.ColumnName,
                                            ColumnValue = userDefinedInfo.ColumnValue
                                        });
                                    }
                                    data.ArabicData = lstUserDefinedLoginInfo;

                                    if (deserializedApplication.UserDefined.Where(desc => desc.Description == "DISABILITIES").ToList().Count > 0)
                                    {
                                        data.HasBeenIll = deserializedApplication.UserDefined.FirstOrDefault(desc => desc.Description == "DISABILITIES").ColumnValue;
                                    }

                                    if (deserializedApplication.UserDefined.Where(desc => desc.Description == "DISABILITIES_DESC").ToList().Count > 0)
                                    {
                                        data.IllnessDescription = deserializedApplication.UserDefined.FirstOrDefault(desc => desc.Description == "DISABILITIES_DESC").ColumnValue;
                                    }

                                    if (deserializedApplication.PostgraduateInfo != null)
                                    {
                                        var pg = deserializedApplication.PostgraduateInfo;
                                        data.MilitaryStatus = pg.MilitaryStatus;
                                        data.MaritalStatus = pg.MaritalStatus;
                                        data.EmploymentStatus = pg.EmploymentStatus;
                                        data.EmployerCompanyName = pg.EmployerCompanyName;
                                        data.EmployerPosition = pg.EmployerPosition;
                                        data.EmployerStartDate = pg.EmployerStartDate;
                                        data.EmployerDuties = pg.EmployerDuties;
                                        data.EmployerIdNumber = pg.EmployerIdNumber;
                                        data.CovAlumni = pg.CovAlumni;
                                        data.BachelorTaughtInEnglish = pg.BachelorTaughtInEnglish;
                                        data.HasProfExam = pg.HasProfExam;
                                        data.CoventryId = pg.CoventryId;
                                        data.PgEmergencyContactRelationship = pg.PgEmergencyContactRelationship;
                                        data.PgEmergencyContactGivenName = pg.PgEmergencyContactGivenName;
                                        data.PgEmergencyContactMiddleName = pg.PgEmergencyContactMiddleName;
                                        data.PgEmergencyContactFamilyName = pg.PgEmergencyContactFamilyName;
                                        data.PgEmergencyContactMobile = pg.PgEmergencyContactMobile;
                                        data.PgEmergencyContactEmail = pg.PgEmergencyContactEmail;
                                        data.AcademicAwards = pg.AcademicAwards;
                                        data.PgSchoolName = pg.PgSchoolName;
                                        data.PgBachelorUniversity = pg.PgBachelorUniversity;
                                        data.PgBachelorUniversityName = pg.PgBachelorUniversityName;
                                        data.PgBachelorDegree = pg.PgBachelorDegree;
                                        data.PgBachelorFieldOfStudy = pg.PgBachelorFieldOfStudy;
                                        data.PgBachelorYearOfGrad = pg.PgBachelorYearOfGrad;
                                        data.PgHasAcademicAward = pg.PgHasAcademicAward;
                                        data.PgAcademicAwards = pg.PgAcademicAwards;
                                        data.PgProfExamsData = pg.PgProfExamsData;
                                    }
                                }

                                if (deserializedApplication.PriorEducation != null && deserializedApplication.PriorEducation.Count > 0)
                                {
                                    for (int i = 0; i < deserializedApplication.PriorEducation.Count; i++)
                                    {
                                        lstEducationLoginHistory.Add(new EducationLoginHistory()
                                        {
                                            CountryId = deserializedApplication.PriorEducation[i].InstitutionCountryId,
                                            GPA = deserializedApplication.PriorEducation[i].GPA,
                                            InstitutionName = deserializedApplication.PriorEducation[i].InstitutionName,
                                            InstitutionId = deserializedApplication.PriorEducation[i].InstitutionId,
                                            CurriculumId = deserializedApplication.PriorEducation[i].CurriculumId,
                                            EndDate = deserializedApplication.PriorEducation[i].EndDate,
                                            TranferedInstitutionName = deserializedApplication.PriorEducation[i].TransferedInstitutionName,
                                            TranferedInstitutionId = deserializedApplication.PriorEducation[i].TransferedInstitutionId,
                                            TransferredCerti = deserializedApplication.PriorEducation[i].TransferredCerti,
                                            TransferredGPA = deserializedApplication.PriorEducation[i].TransferredGPA,
                                            EducationGrade = deserializedApplication.PriorEducation[i].EducationGrade,
                                            CityId= deserializedApplication.PriorEducation[i].InstitutionCityId,
                                            TransferCountryId=deserializedApplication.PriorEducation[i].TransferInstitutionCountryId,
                                            TransferCityId = deserializedApplication.PriorEducation[i].TransferInstitutionCityId,

                                        });
                                    }
                                    data.EducationHistory = lstEducationLoginHistory;
                                }

                                if (deserializedApplication.Employment != null && deserializedApplication.Employment.Count > 0)
                                {
                                    for (int i = 0; i < deserializedApplication.Employment.Count; i++)
                                    {
                                        lstEmployment.Add(new Employment()
                                        {
                                            EmployerName = deserializedApplication.Employment[i].EmployerName,
                                            Position = deserializedApplication.Employment[i].Position,
                                            StartDate = deserializedApplication.Employment[i].StartDate,
                                            EndDate = deserializedApplication.Employment[i].EndDate

                                        });
                                    }
                                    data.Employment = lstEmployment;
                                }
                            }

                            loginData.Status = "success";
                            loginData.StatusCode = (int)HttpStatusCode.OK;
                            loginData.data = data;
                            return Request.CreateResponse(loginData);
                        }
                        else
                        {
                            ErrorResponse errorResponse = new ErrorResponse()
                            {
                                StatusCode = (int)HttpStatusCode.NotFound,
                                Status = "failure",
                                Msg = "Could not found the incomplete application"
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
                        Msg = "The request in null"
                    };
                    return Request.CreateResponse(errorResponse);
                }
            }
            catch (System.Exception ex)
            {
                ErrorResponse errorResponse = new ErrorResponse()
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Status = "failure",
                    Msg = ex.Message
                };
                return Request.CreateResponse(errorResponse);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetPolicyLink()
        {
            return Request.CreateResponse(ConfigurationManager.AppSettings["Link"]);
        }

        [HttpGet]
        public Boolean GetIsApplicationActive()
        {
            bool IsActive = false;

            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                int applicationFormSettingId = Convert.ToInt32(ConfigurationManager.AppSettings["ApplicationFormSettings"]);
                IsActive= entities.ApplicationFormSettings.FirstOrDefault(appFormSettingId => appFormSettingId.ApplicationFormSettingId == applicationFormSettingId).IsActive;

            }
            return IsActive;
        }
    }
}