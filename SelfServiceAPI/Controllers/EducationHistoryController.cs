using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DataAccess;
using SelfServiceAPI.Classes;

namespace SelfServiceAPI.Controllers
{
    [System.Web.Http.Cors.EnableCors(origins: "*", headers: "*", methods: "*")]
    public class EducationHistoryController : ApiController
    {
        #region PG Bachelor Degrees

        // Retrieve active degree types from CODE_DEGREE for PG bachelor dropdown
        [HttpGet]
        public HttpResponseMessage GetDegrees()
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                var degrees = entities.CODE_DEGREE
                    .Where(d => d.STATUS == "A")
                    .OrderBy(d => d.LONG_DESC)
                    .Select(d => new { Id = d.DegreeId, value = d.LONG_DESC })
                    .ToList();
                return Request.CreateResponse(HttpStatusCode.OK, degrees);
            }
        }

        #endregion

        #region PG Professional Exam Tests

        // Retrieve professional exam test codes (GRE/GMAT) linked to PG ApplicationFormSetting
        [HttpGet]
        public HttpResponseMessage GetProfessionalExamTests()
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                int pgTestSettingId = Convert.ToInt32(ConfigurationManager.AppSettings["ApplicationFormSettings"]);
                var tests = entities.Database.SqlQuery<IdValueResult>(
                    "EXEC ITB_GetPGProfessionalExamTests @p0",
                    pgTestSettingId).ToList();
                return Request.CreateResponse(HttpStatusCode.OK, tests);
            }
        }

        #endregion

        #region School

        //Retrieve School Certificate
        public List<ITB_GetSchoolCurriculums_Result> GetSchoolCurriculum()
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.ITB_GetSchoolCurriculums().ToList();
            }
        }

        //Retrieve School Name
        public List<spSelSchoolAdvanced_Result> GetSchoolName()
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.spSelSchoolAdvanced().ToList();
            }
        }

        //Retrieve School Grade
        public List<spSelGradesByCurriculumIdAdvanced_Result> GetSchoolGrade(int curriculumId)
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.spSelGradesByCurriculumIdAdvanced(curriculumId).ToList();
            }
        }

        #endregion

        #region University

        //Retrieve University Certificate
        public List<ITB_GetUniversitiesCurriculums_Result> GetUniversityCurriculum()
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.ITB_GetUniversitiesCurriculums().ToList();
            }
        }

        //Retrieve Universities
        public List<spSelUniversitiesAdvanced_Result> GetUniversitiesName()
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.spSelUniversitiesAdvanced().ToList();
            }
        }

        //The XML representation is mapped to PriorEducation tag
        [HttpPost]
        public HttpResponseMessage GraduateEducationHistory([FromBody] List<GraduateEducationHistoryRequest> lstRequest)
        {
            IncompleteApplication incompleteApplication = new IncompleteApplication();
            ApplicationInfo deserializedApplicationInfo = new ApplicationInfo();
            List<EducationInfo> lstEducationInfo = new List<EducationInfo>();

            try
            {
                if (lstRequest != null && lstRequest.Count > 0)
                {
                    using (ApplicationFormEntities entities = new ApplicationFormEntities())
                    {
                        foreach (GraduateEducationHistoryRequest request in lstRequest)
                        {
                            incompleteApplication = entities.IncompleteApplications.FirstOrDefault(id => id.IncompleteApplicationId == request.IncompleteApplicationId);

                            if (incompleteApplication != null)
                            {
                                deserializedApplicationInfo = ApplicationInfo.DeserializeApplicationInfo(incompleteApplication.ApplicationData);

                                lstEducationInfo.Add(new EducationInfo()
                                {
                                    CurriculumId = request.CurriculumId,
                                    EndDate = request.EndDate,
                                    InstitutionName = request.InstitutionName,
                                    InstitutionId = request.InstitutionId,
                                    InstitutionCountryId = request.CountryId,
                                    IsTransfer = false,
                                    GPA = request.GPA

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
                        deserializedApplicationInfo.PriorEducation.Clear();

                        foreach (EducationInfo item in lstEducationInfo)
                        {
                            deserializedApplicationInfo.PriorEducation.Add(item);
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
                        Msg = "Education information is not selected"
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

        //The XML representation is mapped to PriorEducation tag
        [HttpPost]
        public HttpResponseMessage UnderGraduateEducationHistory([FromBody] List<UnderGraduateEducationHistoryRequest> lstRequest)
        {
            IncompleteApplication incompleteApplication = new IncompleteApplication();
            ApplicationInfo deserializedApplicationInfo = new ApplicationInfo();
            List<EducationInfo> lstEducationInfo = new List<EducationInfo>();

            try
            {
                if (lstRequest != null && lstRequest.Count > 0)
                {
                    using (ApplicationFormEntities entities = new ApplicationFormEntities())
                    {
                        foreach (UnderGraduateEducationHistoryRequest request in lstRequest)
                        {
                            incompleteApplication = entities.IncompleteApplications.FirstOrDefault(id => id.IncompleteApplicationId == request.IncompleteApplicationId);

                            if (incompleteApplication != null)
                            {
                                deserializedApplicationInfo = ApplicationInfo.DeserializeApplicationInfo(incompleteApplication.ApplicationData);

                                lstEducationInfo.Add(new EducationInfo()
                                {
                                    InstitutionName = request.InstitutionName,
                                    InstitutionId = request.InstitutionId,
                                    InstitutionCountryId = request.CountryId,
                                    IsTransfer = !string.IsNullOrEmpty(request.TransferedInstitutionName) ? true : false,
                                    TransferedInstitutionName = request.TransferedInstitutionName,
                                    TransferedInstitutionId = request.TransferedInstitutionId,
                                    CurriculumId = request.CurriculumId,
                                    EndDate = request.EndDate

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
                        deserializedApplicationInfo.PriorEducation.Clear();
    
                        foreach (EducationInfo item in lstEducationInfo)
                        {
                            deserializedApplicationInfo.PriorEducation.Add(item);
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
                        Msg = "Contact information is not selected"
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
        #endregion
    }
}
