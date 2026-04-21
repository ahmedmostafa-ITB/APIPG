using DataAccess;
using SelfServiceAPI.Classes;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace SelfServiceAPI.Controllers
{
    [System.Web.Http.Cors.EnableCors(origins: "*", headers: "*", methods: "*")]
    public class ValidateController : ApiController
    {
        [HttpPost]
        public HttpResponseMessage ValidateApplication([FromBody] OnValidate request)
        {
            if (request != null)
            {
                ApplicationInfo deserializedApplication;
                StringBuilder msgBuilder = new StringBuilder();
                Dictionary<string, string> dictMsg = new Dictionary<string, string>();
                string Nationality_Config = ConfigurationManager.AppSettings["NationalityCheck"];

                using (ApplicationFormEntities entities = new ApplicationFormEntities())
                {
                    IncompleteApplication incompleteApplication = entities.IncompleteApplications.FirstOrDefault(app => app.IncompleteApplicationId == request.IncompleteApplicationId);
                    if (incompleteApplication != null)
                    {
                        deserializedApplication = ApplicationInfo.DeserializeApplicationInfo(incompleteApplication.ApplicationData);

                        if (deserializedApplication != null)
                        {
                            //Validate Academic Interest
                            if (deserializedApplication.AcademicInterest != null)
                            {
                                int? programOfStudy = deserializedApplication.AcademicInterest.ProgramOfStudy;

                                if (programOfStudy.Value >= 0)
                                {
                                    bool programOfStudyExist = entities.ProgramOfStudies.Any(prog => prog.ProgramOfStudyId == programOfStudy.Value);
                                    if (!programOfStudyExist)
                                    {
                                        dictMsg.Add("ProgramOfStudy", "Wrong Entered");
                                    }
                                }

                                int sessionPerioId = deserializedApplication.AcademicInterest.SessionPeriodId;
                                if (sessionPerioId >= 0)
                                {
                                    bool sessionPriodExist = entities.ACADEMICCALENDARs.Any(session => session.SessionPeriodId == sessionPerioId);
                                    if (!sessionPriodExist)
                                    {
                                        dictMsg.Add("SessionPeriodId", "Wrong Entered");
                                    }
                                }

                                int programId = deserializedApplication.AcademicInterest.ProgramId;
                                if (programId >= 0)
                                {
                                    bool programExist = entities.CODE_PROGRAM.Any(prog => prog.ProgramId == programId);
                                    if (!programExist)
                                    {
                                        dictMsg.Add("ProgramId", "Wrong Entered");
                                    }
                                }
                            }

                            //Validate Phone Number 
                            

                            //Validate National ID
                            if (deserializedApplication.Demographic != null)
                            {
                                int primaryCitizenship = deserializedApplication.Demographic.PrimaryCitizenship;

                                if (primaryCitizenship >= 0)
                                {
                                    string nationality = entities.CODE_COUNTRY.FirstOrDefault(citizenShip => citizenShip.CountryId == primaryCitizenship).CODE_VALUE_KEY;

                                    if (!string.IsNullOrEmpty(nationality))
                                    {
                                        if (nationality.Equals(Nationality_Config, System.StringComparison.CurrentCultureIgnoreCase))
                                        {
                                            //National Id should be filled
                                            string governmnentId = deserializedApplication.GovernmentId;
                                            if (string.IsNullOrEmpty(governmnentId))
                                            {
                                                dictMsg.Add("GovernmentId", "National ID Number is missing");
                                            }
                                        }
                                        else
                                        {
                                            //Passport Number is required
                                            string passportNumber = deserializedApplication.PassportNumber;
                                            if (string.IsNullOrEmpty(passportNumber))
                                            {
                                                dictMsg.Add("PassportNumber", "Passport Number is missing");
                                            }

                                            DateTime? passportExpiryDate = deserializedApplication.PassportExpirationDate;
                                            if (!passportExpiryDate.HasValue)
                                            {
                                                dictMsg.Add("PassportExpirationDate", "Passport Expiration Date is missing");
                                            }

                                            int? passportCountryIssued = deserializedApplication.PassportCountryIssued;
                                            if (passportCountryIssued.HasValue && passportCountryIssued.Value <= 0)
                                            {
                                                dictMsg.Add("PassportCountryIssued", "Passport Country Issued is missing");
                                            }
                                        }
                                    }
                                }
                            }

                            //Validate Address Type
                            if (deserializedApplication.Address != null)
                            {
                                AddressInfo address = deserializedApplication.Address[0];

                                int addressTypeId = address.AddressTypeId;

                                if (addressTypeId >= 0)
                                {
                                    bool addressTypeExist = entities.CODE_ADDRESSTYPE.Any(add => add.AddressTypeId == addressTypeId);
                                    if (!addressTypeExist)
                                    {
                                        dictMsg.Add("AddressTypeId", "Wrong Entered");
                                    }
                                }
                            }

                            //Validate the test score
                            if (deserializedApplication.TestScores != null)
                            {
                                foreach (ApplicationTestScoreInfo testScoreInfo in deserializedApplication.TestScores)
                                {
                                    int testId = testScoreInfo.TestId;

                                    if (testId >= 0)
                                    {
                                        bool testExist = entities.CODE_TEST.Any(test => test.TestId == testId);
                                        if (!testExist)
                                        {
                                            dictMsg.Add("TestId", "Wrong Entered");
                                        }
                                    }

                                    int testTypeID = testScoreInfo.TestTypeId;

                                    if (testTypeID >= 0)
                                    {
                                        bool testTypeExist = entities.CODE_TESTTYPE.Any(testType => testType.TestTypeId == testTypeID);
                                        if (!testTypeExist)
                                        {
                                            dictMsg.Add("TestTypeId", "Wrong Entered");
                                        }
                                    }
                                }
                            }

                            //Validate the required test score

                            if (deserializedApplication.Tests != null && deserializedApplication.Tests.Count > 0)
                            {
                                bool hasDupes = deserializedApplication.Tests.GroupBy(x => new { x.Test, x.TestType }).Where(x => x.Skip(1).Any()).Any();

                                if (hasDupes)
                                {
                                    dictMsg.Add("ExistsTestType", "Please do not enter the same test type more than once.");
                                }
                            }

                            if (dictMsg.Count > 0)
                            {
                                ValidationErrorMessage errorResponse = new ValidationErrorMessage()
                                {
                                    StatusCode = (int)HttpStatusCode.NotFound,
                                    Status = "failure",
                                    Msg = dictMsg
                                };
                                return Request.CreateResponse(errorResponse);
                            }
                            else
                            {
                                deserializedApplication.Validated = true;

                                deserializedApplication.SerializeApplicationInfo(incompleteApplication, entities, deserializedApplication);
                            }

                        }
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
                    Msg = "Request is null"
                };
                return Request.CreateResponse(errorResponse);
            }
        }
    }
}
