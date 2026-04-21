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
    public class AddressInformationController : ApiController
    {
        //Retrieve Address Type
        public List<getAddressTypeList_Result> GetAddressTypes()
        {
            int applicationFormSettingId = Convert.ToInt32(ConfigurationManager.AppSettings["ApplicationFormSettings"]);

            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.getAddressTypeList(applicationFormSettingId).ToList();
            }
        }

        //Retrieve Cities
        public List<spSelCitiesByCountryIdAdvanced_Result> GetCitiesByCountry(int countryId)
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.spSelCitiesByCountryIdAdvanced(countryId, 1).ToList();
            }
        }

        //Retrieve Province
        public List<spSelStatesByCityIdAdvanced_Result> GetProvinceByCity(int cityId)
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.spSelStatesByCityIdAdvanced(cityId, 1).ToList();
            }
        }

        //Address request
        //The XML representation is mapped to Address tag
        [HttpPost]
        public HttpResponseMessage AddressInformation([FromBody] AddressInformationRequest request)
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

                            List<AddressInfo> lstAddressInformation = new List<AddressInfo>
                            {
                                new AddressInfo()
                                {
                                   // AddressTypeId = request.AddressTypeId,
                                   // Country = request.Country,
                                   // Line3 = request.Province,
                                   // State = request.City,
                                   // //HouseNumber = request.HouseNumber,
                                   // Line1 = request.Line1,
                                   // Line2 = request.Line2,
                                   // City = request.Line3,
                                   //// Line4 = request.Line4,
                                   //// PostalCode = request.PostalCode,
                                   // IsPrimary = true
                                }
                            };

                            //Clear the existing data to override it
                            deserializedApplicationInfo.Address.Clear();

                            foreach (AddressInfo item in lstAddressInformation)
                            {
                                deserializedApplicationInfo.Address.Add(item);
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
                        Msg = "Address Information are not selected"
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
