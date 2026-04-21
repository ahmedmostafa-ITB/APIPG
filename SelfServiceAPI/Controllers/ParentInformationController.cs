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
    public class ParentInformationController : ApiController
    {
        //Retrieve Prefixes
        public List<getPrefix_Result> GetPrefixes()
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.getPrefix().ToList();
            }
        }

        //Retrieve RelationShip
        public List<getRelationList_Result> GetRelations()
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.getRelationList().ToList();
            }
        }
        //The XML representation is mapped to ApplicationRelations tag
        [HttpPost]
        public HttpResponseMessage ParentInformation([FromBody] ParentInformationRequest request)
        {
            IncompleteApplication incompleteApplication = new IncompleteApplication();
            ApplicationInfo deserializedApplicationInfo = new ApplicationInfo();

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

                            List<ApplicationRelationInfo> lstApplicationRelationInfo = new List<ApplicationRelationInfo>
                             {
                                new ApplicationRelationInfo()
                                {
                                    Address = request.Address,
                                    Company = request.Company,
                                    CompanyAddress = request.CompanyAddress,
                                    IsGuardian = false ,
                                    MobileNumber = request.MobileNumber,
                                    Profession = request.Profession,
                                    RelationEmail = request.Email,
                                    RelationFirstName = request.FirstName,
                                    RelationLastName = request.FamilyName,
                                    RelationMiddleName = request.MiddleName,
                                    RelationPrefix = request.Prefix,
                                    RelationType = request.RelationType,
                                    AttendedInstitution = request.AttendedInstitution
                                }
                             };

                            if(request.RelationType >= 0)
                            deserializedApplicationInfo.ApplicationRelations.RemoveAll(relation => relation.RelationType == request.RelationType && relation.IsGuardian ==false);

                            foreach (ApplicationRelationInfo item in lstApplicationRelationInfo)
                            {
                                if(request.Deceased == false)
                                deserializedApplicationInfo.ApplicationRelations.Add(item);
                            }

                            deserializedApplicationInfo.SerializeApplicationInfo(incompleteApplication, entities, deserializedApplicationInfo);
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
                        Msg = "Parent information is not selected"
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
