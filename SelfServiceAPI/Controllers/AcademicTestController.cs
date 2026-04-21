using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DataAccess;
using SelfServiceAPI.Classes;
using static SelfServiceAPI.Classes.AcademicTestRequest;

namespace SelfServiceAPI.Controllers
{
    [System.Web.Http.Cors.EnableCors(origins: "*", headers: "*", methods: "*")]
    public class AcademicTestController : ApiController
    {
        //Constant for American High Scholl Diploma Test value
        const string AHSD = "AMERICAN HIGH SCHOOL DIPLOMA";
        const string IGSE = "IGCSE";
        const string GCSE = "GCSE";
        const string TA = "All National and Arabic Thanaweya Amma";

        //Retrieve Academic Tests
        public List<getAcademicTestCode_Result> GetAcademicTestCodes()
        {
            int applicationFormSettingId = Convert.ToInt32(ConfigurationManager.AppSettings["ApplicationFormSettings"]);

            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.getAcademicTestCode(applicationFormSettingId).ToList();
            }
        }

        //Retrieve Academic Test
        public string GetAcademicTestCode(int testId)
        {
            string testName = string.Empty; ;

            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                int applicationFormSettingId = Convert.ToInt32(ConfigurationManager.AppSettings["ApplicationFormSettings"]);

                var testCode = entities.getAcademicTestCode(applicationFormSettingId).FirstOrDefault(test => test.Id == testId);

                if (testCode != null)
                {
                    testName = testCode.value;
                }
            }
            return testName;
        }

        //Retrieve Test Type List 
        public List<getTestTypeList_Result> GetTestTypeList()
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.getTestTypeList().ToList();
            }
        }
        
        // Retrieve the level list
        //public List<GetLevels_Result> GetLevels()
        //{
        //    using (ApplicationFormEntities entities = new ApplicationFormEntities())
        //    {
        //        return entities.GetLevels().ToList();
        //    }
        //}

        // Retrieve the level list
        //public List<GetTestScores_Result> GetTestScores()
        //{
        //    using (ApplicationFormEntities entities = new ApplicationFormEntities())
        //    {
        //        return entities.GetTestScores().ToList();
        //    }
        //}

        //Retrieve Test Type by TestId
        public List<spSelTestTypeByTestAdvanced_Result> GetTestTypeByTest([FromUri] int TestId)
        {
            List<spSelTestTypeByTestAdvanced_Result> lstTestTypeByTest = new List<spSelTestTypeByTestAdvanced_Result>();
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                //string testName = entities.CODE_TEST.FirstOrDefault(test => test.TestId == TestId).LONG_DESC;

               // if (testName.Equals(AHSD) || testName.Equals(IGSE) || testName.Equals(GCSE))
               // {
                    lstTestTypeByTest = entities.spSelTestTypeByTestAdvanced(TestId).ToList();
               // }
            }
            return lstTestTypeByTest;
        }

        //The XML representation is mapped to TestScores tag
        [HttpPost]
        public HttpResponseMessage TestAHSD([FromBody] List<AcademicTestAHSDRequest> lstRequest)
        {
            IncompleteApplication incompleteApplication = new IncompleteApplication();
            ApplicationInfo deserializedApplicationInfo = new ApplicationInfo();
            List<ApplicationTestScoreInfo> lstApplicationTestScore = new List<ApplicationTestScoreInfo>();

            try
            {
                if (lstRequest != null && lstRequest.Count > 0)
                {
                    using (ApplicationFormEntities entities = new ApplicationFormEntities())
                    {
                        foreach (AcademicTestAHSDRequest request in lstRequest)
                        {
                            incompleteApplication = entities.IncompleteApplications.FirstOrDefault(id => id.IncompleteApplicationId == request.IncompleteApplicationId);

                            if (incompleteApplication != null)
                            {
                                deserializedApplicationInfo = ApplicationInfo.DeserializeApplicationInfo(incompleteApplication.ApplicationData);

                                lstApplicationTestScore.Add(new ApplicationTestScoreInfo()
                                {
                                    TestId = request.Test,
                                    TestTypeId = request.TestType,
                                    Score = request.Score,
                                    UserName = request.UserName,
                                    Password = request.Password
                                });
                            }
                            else
                            {
                                ErrorResponse errorResponse = new ErrorResponse()
                                {
                                    StatusCode = (int)HttpStatusCode.NotFound,
                                    Status = "failure",
                                    Msg = "Invalid Incomplete Application Id"
                                };
                                return Request.CreateResponse(errorResponse);
                            }
                        }


                        //Clear the existing data to override it
                        deserializedApplicationInfo.TestScores.Clear();

                        foreach (ApplicationTestScoreInfo item in lstApplicationTestScore)
                        {
                            deserializedApplicationInfo.TestScores.Add(item);
                        }

                        deserializedApplicationInfo.SerializeApplicationInfo(incompleteApplication, entities, deserializedApplicationInfo);

                        SuccessReponse successReponse = new SuccessReponse()
                        {
                            StatusCode = (int)HttpStatusCode.OK,
                            Status = "success"
                        };
                        return Request.CreateResponse(successReponse);
                    }
                }
                else
                {
                    ErrorResponse errorResponse = new ErrorResponse()
                    {
                        StatusCode = (int)HttpStatusCode.NotFound,
                        Status = "failure",
                        Msg = "Test is not selected"
                    };
                    return Request.CreateResponse(errorResponse);
                }
            }
            catch (Exception ex)
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


        //The XML representation is mapped to TestScores tag
        [HttpPost]
        public HttpResponseMessage TestIB([FromBody] AcademicTestIBRequest request)
        {
            IncompleteApplication incompleteApplication;
            ApplicationInfo deserializedApplicationInfo;

            try
            {
                if (request != null)
                {
                    using (ApplicationFormEntities entities = new ApplicationFormEntities())
                    {
                        incompleteApplication = entities.IncompleteApplications.FirstOrDefault(id => id.IncompleteApplicationId == request.IncompleteApplicationId);

                        if (incompleteApplication != null)
                        {
                            deserializedApplicationInfo = ApplicationInfo.DeserializeApplicationInfo(incompleteApplication.ApplicationData);

                            List<ApplicationTestScoreInfo> lstApplicationTestScore = new List<ApplicationTestScoreInfo>
                            {
                                new ApplicationTestScoreInfo()
                                {
                                    TestId = request.Test,
                                    TestTypeId = entities.spSelTestTypeByTest(request.Test).FirstOrDefault().Id,
                                    UserName = request.UserName,
                                    Password = request.Password
                                }
                            };


                            //Clear the existing data to override it
                            deserializedApplicationInfo.TestScores.Clear();

                            foreach (ApplicationTestScoreInfo item in lstApplicationTestScore)
                            {
                                deserializedApplicationInfo.TestScores.Add(item);
                            }

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
                                Msg = "Invalid Incomplete Application Id"
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
                        Msg = "Test is not selected"
                    };
                    return Request.CreateResponse(errorResponse);
                }
            }
            catch (Exception ex)
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

        //The XML representation is mapped to TestScores tag
        [HttpPost]
        public HttpResponseMessage TestIGSE([FromBody] List<AcademicTestIGCSERequest> lstRequest)
        {
            IncompleteApplication incompleteApplication = new IncompleteApplication();
            ApplicationInfo deserializedApplicationInfo = new ApplicationInfo();
            List<ApplicationTestScoreInfo> lstApplicationTestScore = new List<ApplicationTestScoreInfo>();

            try
            {
                if (lstRequest != null && lstRequest.Count > 0)
                {
                    using (ApplicationFormEntities entities = new ApplicationFormEntities())
                    {
                        foreach (AcademicTestIGCSERequest request in lstRequest)
                        {
                            incompleteApplication = entities.IncompleteApplications.FirstOrDefault(id => id.IncompleteApplicationId == request.IncompleteApplicationId);

                            if (incompleteApplication != null)
                            {
                                deserializedApplicationInfo = ApplicationInfo.DeserializeApplicationInfo(incompleteApplication.ApplicationData);

                                lstApplicationTestScore.Add(new ApplicationTestScoreInfo()
                                {
                                    TestId = request.Test,
                                    TestTypeId = request.SubjectId,
                                    SubjectId = request.SubjectId,
                                    LevelId = request.LevelId,
                                    ScoreId = request.ScoreId,
                                    Total = request.Total
                                });
                            }
                            else
                            {
                                ErrorResponse errorResponse = new ErrorResponse()
                                {
                                    StatusCode = (int)HttpStatusCode.NotFound,
                                    Status = "failure",
                                    Msg = "Invalid Incomplete Application Id"
                                };
                                return Request.CreateResponse(errorResponse);
                            }
                        }

                        //Clear the existing data to override it
                        deserializedApplicationInfo.TestScores.Clear();

                        foreach (ApplicationTestScoreInfo item in lstApplicationTestScore)
                        {
                            deserializedApplicationInfo.TestScores.Add(item);
                        }

                        deserializedApplicationInfo.SerializeApplicationInfo(incompleteApplication, entities, deserializedApplicationInfo);

                        SuccessReponse successReponse = new SuccessReponse()
                        {
                            StatusCode = (int)HttpStatusCode.OK,
                            Status = "success"
                        };
                        return Request.CreateResponse(successReponse);
                    }
                }
                else
                {
                    ErrorResponse errorResponse = new ErrorResponse()
                    {
                        StatusCode = (int)HttpStatusCode.NotFound,
                        Status = "failure",
                        Msg = "Test is not selected"
                    };
                    return Request.CreateResponse(errorResponse);
                }
            }
            catch (Exception ex)
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

        //The XML representation is mapped to TestScores tag
        [HttpPost]
        public HttpResponseMessage TestANATA([FromBody] AcademicTestANATARequest request)
        {
            IncompleteApplication incompleteApplication;
            ApplicationInfo deserializedApplicationInfo;

            try
            {
                if (request != null)
                {
                    using (ApplicationFormEntities entities = new ApplicationFormEntities())
                    {
                        incompleteApplication = entities.IncompleteApplications.FirstOrDefault(id => id.IncompleteApplicationId == request.IncompleteApplicationId);

                        if (incompleteApplication != null)
                        {
                            deserializedApplicationInfo = ApplicationInfo.DeserializeApplicationInfo(incompleteApplication.ApplicationData);

                            List<ApplicationTestScoreInfo> lstApplicationTestScore = new List<ApplicationTestScoreInfo>
                            {
                                new ApplicationTestScoreInfo()
                                {
                                    TestId = request.Test,
                                    TestTypeId = entities.spSelTestTypeByTest(request.Test).FirstOrDefault().Id,
                                    Total = request.Total,
                                    Score = request.Score
                                }
                            };


                            //Clear the existing data to override it
                            deserializedApplicationInfo.TestScores.Clear();

                            foreach (ApplicationTestScoreInfo item in lstApplicationTestScore)
                            {
                                deserializedApplicationInfo.TestScores.Add(item);
                            }

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
                                Msg = "Invalid Incomplete Application Id"
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
                        Msg = "Test is not selected"
                    };
                    return Request.CreateResponse(errorResponse);
                }
            }
            catch (Exception ex)
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
    }
}


