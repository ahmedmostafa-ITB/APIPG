using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DataAccess;
using SelfServiceAPI.Classes;
using static SelfServiceAPI.Classes.Helper;

namespace SelfServiceAPI.Controllers
{
    [System.Web.Http.Cors.EnableCors(origins: "*", headers: "*", methods: "*")]
    public class PersonalInfoController : ApiController
    {
        #region RetrieveInformation

        //Retrieve Countries
        public List<getCountryList_Result> GetCountries()
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.getCountryList().ToList();
            }
        }
        public List<CountryPhoneFormat> GetCountriesWithPhoneFormat()
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                List < CountryPhoneFormat> countries= entities.CODE_COUNTRY.Select(x => new CountryPhoneFormat
                {
                    Id = x.CountryId,
                    value = x.PHONE_FORMAT
                }).ToList();
                foreach(var country in countries)
                {
                    if (!String.IsNullOrEmpty(country.value)) { 
                    string St = country.value;
                    int pFrom = St.IndexOf("(") + "(".Length;
                    int pTo = St.LastIndexOf(")")  ;
                    String result = St.Substring(pFrom, pTo - pFrom);
                    country.value = result;
                    }
                    else
                    {
                        country.value ="";
                    }

                }
                return countries;

                

            }
        }


        //Retrieve Cities
        public List<spSelCitiesByCountryId_Result> GetCitiesByCountry(int countryId)
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.spSelCitiesByCountryId(countryId, 1).ToList();
            }
        }

        //Retrieve States
        public List<spSelStatesByCityId_Result> GetSpSelStatesByCity(int cityId)
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.spSelStatesByCityId(cityId, 1).ToList();
            }
        }

        //Retrieve Marital Statuses
        public List<getMaritalStatusList_Result> GetMaritalStatus()
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.getMaritalStatusList().ToList();
            }
        }

        //Retrieve Religion List
        public List<getReligionList_Result> GetReligion()
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.getReligionList().ToList();
            }
        }

        //Retrieve Languages
        public List<getLanguageList_Result> GetLanguageLists()
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.getLanguageList().ToList();
            }
        }

        //Retrieve Phone Types
        public List<getPhoneType_Result> GetPhoneTypes()
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.getPhoneType().ToList();
            }
        }

        //Retrieve Prefixes
        public List<getPrefix_Result> GetPrefixes()
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.getPrefix().ToList();
            }
        }

        #endregion

        #region Post

        //The XML representation is mapped to Demographic tag
        [HttpPost]
        public HttpResponseMessage CreatePersonalInfo([FromBody] PersonalInformationRequest request)
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

                            PersonalInfo personalInfo = new PersonalInfo()
                            {
                                FirstName = request.FirstName,
                                MiddleName = request.MiddleName,
                                LastName = request.LastName,
                                Prefix = request.Prefix
                            };

                            deserializedApplicationInfo.PersonalInfo = personalInfo;

                            Demographic demographic = new Demographic()
                            {
                                BirthDate = request.DateOfBirth,
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
                                        Description = "ArabicData"
                                    });
                                }
                            }

                            deserializedApplicationInfo.UserDefined.RemoveAll(desc => desc.Description.Equals("ArabicData"));

                            foreach (ApplicationUserDefinedInfo item in lstApplicationUserDefinedInfo)
                            {
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
                        Msg = "Personal Information is not selected"
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
