using DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace SelfServiceAPI.Controllers
{
    [System.Web.Http.Cors.EnableCors(origins: "*", headers: "*", methods: "*")]
    public class EmploymentController : ApiController
    {
        //Retrieve Positions
        public List<GetPositionListAdvanced_Result> GetPositions()
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.GetPositionListAdvanced().ToList();
            }
        }
    }
}
