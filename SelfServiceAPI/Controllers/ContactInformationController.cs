using DataAccess;
using SelfServiceAPI.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using static SelfServiceAPI.Classes.Helper;

namespace SelfServiceAPI.Controllers
{
    [System.Web.Http.Cors.EnableCors(origins: "*", headers: "*", methods: "*")]
    public class ContactInformationController : ApiController
    {
        //Retrieve Phone Types
        public List<getPhoneType_Result> GetPhoneTypes()
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.getPhoneType().ToList();
            }
        }

        [HttpPost]
        public HttpResponseMessage SingleContactInformation([FromBody] List<ContactInformationRequest> lstRequest)
        {
            IncompleteApplication incompleteApplication = new IncompleteApplication();
            ApplicationInfo deserializedApplicationInfo = new ApplicationInfo();
            List<PhoneNumberInfo> lstPhoneNumber = new List<PhoneNumberInfo>();

            try
            {
                if (lstRequest != null && lstRequest.Count > 0)
                {
                    using (ApplicationFormEntities entities = new ApplicationFormEntities())
                    {
                        foreach (ContactInformationRequest request in lstRequest)
                        {
                            incompleteApplication = entities.IncompleteApplications.FirstOrDefault(id => id.IncompleteApplicationId == request.IncompleteApplicationId);

                            if (incompleteApplication != null)
                            {
                                deserializedApplicationInfo = ApplicationInfo.DeserializeApplicationInfo(incompleteApplication.ApplicationData);

                                lstPhoneNumber.Add(new PhoneNumberInfo()
                                {
                                    CountryId = request.CountryId,
                                    PhoneNumber = request.PhoneNumber,
                                    PhoneType = request.PhoneType,
                                    Email = request.Email,
                                    IsPrimary = request.IsPrimary
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
                        deserializedApplicationInfo.PhoneNumber.Clear();

                        foreach (PhoneNumberInfo item in lstPhoneNumber)
                        {
                            deserializedApplicationInfo.PhoneNumber.Add(item);
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

        //Contact Information request (multiple records)
        //The XML representation is mapped to PhoneNumber tag
        [HttpPost]
        public HttpResponseMessage MultipleContactInformation([FromBody] List<ContactInformationRequest> lstRequest)
        {
            IncompleteApplication incompleteApplication = new IncompleteApplication();
            ApplicationInfo deserializedApplicationInfo = new ApplicationInfo();
            List<PhoneNumberInfo> lstPhoneNumber = new List<PhoneNumberInfo>();

            try
            {
                if (lstRequest != null && lstRequest.Count > 0)
                {
                    using (ApplicationFormEntities entities = new ApplicationFormEntities())
                    {
                        foreach (ContactInformationRequest request in lstRequest)
                        {
                            incompleteApplication = entities.IncompleteApplications.FirstOrDefault(id => id.IncompleteApplicationId == request.IncompleteApplicationId);

                            if (incompleteApplication != null)
                            {
                                deserializedApplicationInfo = ApplicationInfo.DeserializeApplicationInfo(incompleteApplication.ApplicationData);

                                lstPhoneNumber.Add(new PhoneNumberInfo()
                                {
                                    CountryId = request.CountryId,
                                    PhoneNumber = request.PhoneNumber,
                                    PhoneType = request.PhoneType,
                                    Email = request.Email,
                                    IsPrimary = request.IsPrimary
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
                        deserializedApplicationInfo.PhoneNumber.Clear();

                        foreach (PhoneNumberInfo item in lstPhoneNumber)
                        {
                            deserializedApplicationInfo.PhoneNumber.Add(item);
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
    }
}
