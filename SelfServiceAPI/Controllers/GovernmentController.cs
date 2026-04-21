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
    public class GovernmentController : ApiController
    {

        //Retrieve Passport Type List
        //public List<GetPassportType_Result> GetPassportType()
        //{
        //    using (ApplicationFormEntities entities = new ApplicationFormEntities())
        //    {
        //        return entities.GetPassportType().ToList();
        //    }
        //}

        //The XML representation is mapped to ApplicationInfo tag
        [HttpPost]
        public HttpResponseMessage GovernmentInformation([FromBody] GovernmentInformationRequest request)
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

                            deserializedApplicationInfo.PassportNumber = request.PassportNumber;
                            deserializedApplicationInfo.PassportExpirationDate = request.PassportExpirationdate.HasValue ?  request.PassportExpirationdate.Value : (DateTime?)null;
                            deserializedApplicationInfo.GovernmentId = request.NationalIdNumber;
                            deserializedApplicationInfo.PassportCountryIssued = request.PassportCountryIssued;

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
                        Msg = "Government Information are not selected"
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
