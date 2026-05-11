using DataAccess;
using SelfServiceAPI.Classes;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace SelfServiceAPI.Controllers
{
    [System.Web.Http.Cors.EnableCors(origins: "*", headers: "*", methods: "*")]
    public class AcademicInterestController : ApiController
    {
        //Retrieve Entry Terms
        public HttpResponseMessage GetPopulation()
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                int applicationFormSettingId = Convert.ToInt32(ConfigurationManager.AppSettings["ApplicationFormSettings"]);
                var populations = entities.Database.SqlQuery<IdValueResult>(
                    @"SELECT DISTINCT cp.PopulationId AS Id, cp.LONG_DESC AS value
                      FROM CODE_POPULATION cp
                      INNER JOIN PROGRAMOFSTUDY pos ON cp.PopulationId = pos.PopulationId
                      INNER JOIN ApplicationProgramSetting aps ON pos.ProgramOfStudyId = aps.ProgramOfStudyId
                      WHERE aps.ApplicationFormSettingId = @p0
                        AND cp.STATUS = 'A'
                        AND cp.isActive = 1
                      ORDER BY cp.LONG_DESC",
                    applicationFormSettingId).ToList();
                return Request.CreateResponse(System.Net.HttpStatusCode.OK, populations);
            }
        }


        //Retrieve Entry Terms
        public List<getYearSemesterList_Result> GetEntryTerm()
        {
            int applicationFormSettingId = Convert.ToInt32(ConfigurationManager.AppSettings["ApplicationFormSettings"]);
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.getYearSemesterList(applicationFormSettingId).ToList();
            }
        }

        //Retrieve Programs
        public List<spGetProgram_Result> GetProgram()
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.spGetProgram().ToList();
            }
        }

        [HttpGet]
        public bool CheckWestCampus()
        {
            int applicationFormSettingId = Convert.ToInt32(ConfigurationManager.AppSettings["ApplicationFormSettings"]);
            int westCampus = Convert.ToInt32(ConfigurationManager.AppSettings["West"]);
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                var a = entities.ApplicationCampusSettings.FirstOrDefault(appFormSettingId => appFormSettingId.ApplicationFormSettingId == applicationFormSettingId && appFormSettingId.OrganizationId == westCampus);
                return entities.ApplicationCampusSettings.FirstOrDefault(appFormSettingId => appFormSettingId.ApplicationFormSettingId == applicationFormSettingId && appFormSettingId.OrganizationId == westCampus) == null ? false : true;

            }
        }

        //Retrieve Active Universities
        public List<int> getActiveUniversities(string campus)
        {
            int campusId = 0;
            if (campus == "West")
            {
                campusId = Convert.ToInt32(ConfigurationManager.AppSettings["West"]);
            }
            else if (campus == "East")
            {
                campusId = Convert.ToInt32(ConfigurationManager.AppSettings["East"]);
            }
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.ITB_GetActivePopulationsForCampus(campusId).Select(i => i.GetValueOrDefault(0)).ToList();
            }

        }


        //Retrieve Major
        public List<getMajorList_Result> GetMajors(int programId, int populationId, string campus)
        {
            int applicationFormSettingId = Convert.ToInt32(ConfigurationManager.AppSettings["ApplicationFormSettings"]);
            int campusId = 0;
            if (campus == "West")
            {
                campusId = Convert.ToInt32(ConfigurationManager.AppSettings["West"]);
            }
            else if (campus == "East")
            {
                campusId = Convert.ToInt32(ConfigurationManager.AppSettings["East"]);
            }
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.getMajorList(programId, populationId, applicationFormSettingId, campusId).ToList();
            }
        }


        //Retrieve Level
        public List<ITB_selUndergraduateProgramLevels_Result> GetLevels(string programOfStudy, string campus)
        {
            int campusId = 0;
            if (campus == "West")
            {
                campusId = Convert.ToInt32(ConfigurationManager.AppSettings["West"]);
            }
            else if (campus == "East")
            {
                campusId = Convert.ToInt32(ConfigurationManager.AppSettings["East"]);
            }
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.ITB_selUndergraduateProgramLevels(programOfStudy, campusId).ToList();
            }
        }

        //The XML representation is mapped to Programs tag
        [HttpPost]
        public HttpResponseMessage AcademicInterest([FromBody] AcademicInterestRequest request)
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

                            AcademicInterest academicInterest = new AcademicInterest
                            {
                                ApplicationFormSettingId = Convert.ToInt32(ConfigurationManager.AppSettings["ApplicationFormEntitiesSettings"]),
                                ApplicationProgramSettingId = Convert.ToInt32(ConfigurationManager.AppSettings["ApplicationProgramSettings"]),
                                IncompleteApplicationId = request.IncompleteApplicationId,
                                ProgramOfStudy = request.ProgramOfStudyId,
                                FullPartTime =1, //always full time
                                FullPartTimeDescription = "Full Time",
                                IsFirstChoice = true,
                                ProgramDegreeCurriculumDescription = request.Description,
                                SessionPeriodId = request.EntryTerm,
                                ProgramId = request.ProgramId
                            };

                            deserializedApplicationInfo.AcademicInterest = academicInterest;
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
                        Msg = "Academic Interest is not selected"
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

        /// <summary>
        /// Returns distinct schools (colleges) for a given university, filtered to PG programs (Program = 3).
        /// Result format: { Id: CollegeId, value: "School of <LONG_DESC>" }
        /// </summary>
        [HttpGet]
        public IHttpActionResult GetPGSchools(int populationId)
        {
            try
            {
                using (ApplicationFormEntities entities = new ApplicationFormEntities())
                {
                    int applicationFormSettingId = Convert.ToInt32(ConfigurationManager.AppSettings["ApplicationFormSettings"]);
                    int pgProgramId = Convert.ToInt32(ConfigurationManager.AppSettings["GraduateProgram"]);
                    var sql = @"
                        SELECT DISTINCT c.CollegeId AS Id, ('School of ' + c.LONG_DESC) AS value
                        FROM PROGRAMOFSTUDY pos
                        INNER JOIN CODE_COLLEGE c ON c.CollegeId = pos.CollegeId
                        INNER JOIN ApplicationProgramSetting aps ON pos.ProgramOfStudyId = aps.ProgramOfStudyId
                        WHERE pos.Program = @programId
                          AND pos.PopulationId = @populationId
                          AND pos.CollegeId IS NOT NULL
                          AND aps.ApplicationFormSettingId = @settingId
                        ORDER BY value";

                    var results = entities.Database.SqlQuery<PGSchoolResult>(
                        sql,
                        new System.Data.SqlClient.SqlParameter("@programId", pgProgramId),
                        new System.Data.SqlClient.SqlParameter("@populationId", populationId),
                        new System.Data.SqlClient.SqlParameter("@settingId", applicationFormSettingId)
                    ).ToList();

                    return Ok(results);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        /// <summary>
        /// Returns majors (Programme of Study) for a given PG school (CollegeId) and university (PopulationId).
        /// Result format: { Id: ProgramOfStudyId, value: CurriculumDescription }
        /// </summary>
        [HttpGet]
        public IHttpActionResult GetPGMajorsBySchool(int collegeId, int populationId)
        {
            try
            {
                using (ApplicationFormEntities entities = new ApplicationFormEntities())
                {
                    int applicationFormSettingId = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["ApplicationFormSettings"]);
                    int campusId = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["East"]);
                    int pgProgramId = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["GraduateProgram"]);

                    // Use getMajorList but filter further by CollegeId
                    var allMajors = entities.getMajorList(pgProgramId, populationId, applicationFormSettingId, campusId).ToList();

                    // Filter by CollegeId via a direct SQL query on PROGRAM_OF_STUDY
                    var posIds = entities.Database.SqlQuery<int>(
                        @"SELECT pos.ProgramOfStudyId FROM PROGRAMOFSTUDY pos
                          WHERE pos.CollegeId = @collegeId",
                        new System.Data.SqlClient.SqlParameter("@collegeId", collegeId)
                    ).ToHashSet();

                    var filtered = allMajors
                        .Where(m => m.Id != null && posIds.Contains((int)m.Id))
                        .Select(m => new { Id = m.Id, value = m.value })
                        .ToList();

                    return Ok(filtered);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }

    public class PGSchoolResult
    {
        public int Id { get; set; }
        public string value { get; set; }
    }
}
