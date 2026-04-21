using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SelfServiceAPI.Classes
{
    public class ErrorResponse
    {

        [Display(Name = "statuscode")]
        public int StatusCode { get; set; }

        [Display(Name = "status")]
        public string Status { get; set; }

        [Display(Name = "msg")]
        public string Msg { get; set; }
    }
}