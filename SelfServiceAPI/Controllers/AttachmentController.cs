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
    public class AttachmentController : ApiController
    {
        //[HttpGet]
        public List<ITB_GetMediaType_Result> GetFileTypes()
        {
            List<ITB_GetMediaType_Result> lstExtension = new List<ITB_GetMediaType_Result>();
          //  List<ITB_GetMediaType_Result> listValues = new List<ITB_GetMediaType_Result>();
            //List<string> unsupportedCharacters = new List<string> { "*" };
           // List<string> lstType = new List<string>();

            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                int applicationFormSettingId = Convert.ToInt32(ConfigurationManager.AppSettings["ApplicationFormSettings"]);

                lstExtension = entities.ITB_GetMediaType(applicationFormSettingId).ToList();

                //lstExtension = entities.ITB_ConcatenateExtension().ToList();
                //var test = lstExtension.Select(str => string.Concat(str.Split(unsupportedCharacters.ToArray(), StringSplitOptions.RemoveEmptyEntries)).Replace(";", ",").Replace(",,", ",").Replace(" ", ""));
                //return string.Join("", test);

                //foreach (ITB_GetMediaType_Result item in lstExtension)
                //{
                //    lstType = item.type.Split(',').ToList();

                //    foreach (string type in lstType)
                //    {
                //        if (!string.IsNullOrEmpty(type))
                //            listValues.Add(new ITB_GetMediaType_Result() { name = item.name, type = type });
                //    }
                //}

                return lstExtension;
            }
        }

        [HttpGet]
        public int GetMaxAttachmentNumber()
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                int applicationFormSettingId =  Convert.ToInt32(ConfigurationManager.AppSettings["ApplicationFormSettings"]);
                return entities.ApplicationFormSettings.FirstOrDefault(appFormSettingId => appFormSettingId.ApplicationFormSettingId == applicationFormSettingId).NumberOfAttachments;
                
            }
               
        }

        [HttpGet]
        public int GetMaxAttachmentSize()
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                int applicationFormSettingId = Convert.ToInt32(ConfigurationManager.AppSettings["ApplicationFormSettings"]);
                return entities.ApplicationFormSettings.FirstOrDefault(appFormSettingId => appFormSettingId.ApplicationFormSettingId == applicationFormSettingId).MaxAttachmentSize;

            }

        }

        [HttpGet]
        public int GetMaxApplicationAttachmentSize()
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                int applicationFormSettingId = Convert.ToInt32(ConfigurationManager.AppSettings["ApplicationFormSettings"]);
                return entities.ApplicationFormSettings.FirstOrDefault(appFormSettingId => appFormSettingId.ApplicationFormSettingId == applicationFormSettingId).MaxApplicationAttachmentSize;

            }

        }
    }
}
