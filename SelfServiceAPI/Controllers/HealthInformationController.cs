using DataAccess;
using SelfServiceAPI.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace SelfServiceAPI.Controllers
{
    [System.Web.Http.Cors.EnableCors(origins: "*", headers: "*", methods: "*")]
    public class HealthInformationController : ApiController
    {
        //The XML representation is mapped to UserDefined tag
        [HttpPost]
        public HttpResponseMessage AddHealthInformation([FromBody] HealthInformationRequest request)
        {
            try
            {
                if (request != null)
                {
                    IncompleteApplication incompleteApplication;
                    ApplicationInfo deserializedApplicationInfo;

                    using (ApplicationFormEntities entities = new ApplicationFormEntities())
                    {
                        incompleteApplication = entities.IncompleteApplications.FirstOrDefault(id => id.IncompleteApplicationId == request.IncompleteApplicationId);

                        if (incompleteApplication != null)
                        {
                            deserializedApplicationInfo = ApplicationInfo.DeserializeApplicationInfo(incompleteApplication.ApplicationData);

                            List<ApplicationUserDefinedInfo> lstApplicationUserDefinedInfo = new List<ApplicationUserDefinedInfo>
                            {
                                new ApplicationUserDefinedInfo()
                                {
                                    ColumnLabel = "Disabilities",
                                    ColumnName = "DISABILITIES",
                                    ColumnType = 1,
                                    ColumnValue = request.HasBeenIll.ToString(),
                                    Description = "HasBeenIll"

                                },

                                new ApplicationUserDefinedInfo()
                                {

                                    ColumnLabel = "Disabilities description",
                                    ColumnName = "DISABILITIES_DESC",
                                    ColumnType =1,
                                    ColumnValue = request.IllnessDescription,
                                    Description = "IllDescription"

                                }
                            };

                            deserializedApplicationInfo.UserDefined.RemoveAll(desc => desc.Description.Equals("HasBeenIll") || desc.Description.Equals("IllDescription"));

                            foreach (ApplicationUserDefinedInfo item in lstApplicationUserDefinedInfo)
                            {
                                if(!string.IsNullOrEmpty(item.ColumnValue))
                                deserializedApplicationInfo.UserDefined.Add(item);
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
                            SuccessReponse empty = new SuccessReponse()
                            {
                                StatusCode = (int)HttpStatusCode.NotFound,
                                Status = "Incomplete Id application is not found"
                            };
                            return Request.CreateResponse(empty);
                        }
                    }
                }
                else
                {
                    SuccessReponse empty = new SuccessReponse()
                    {
                        StatusCode = (int)HttpStatusCode.OK,
                        Status = "Health information is not chosen"
                    };
                    return Request.CreateResponse(empty);
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

