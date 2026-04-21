using DataAccess;
using SelfServiceAPI.Classes;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using static SelfServiceAPI.Classes.Helper;

namespace SelfServiceAPI.Controllers
{
    [System.Web.Http.Cors.EnableCors(origins: "*", headers: "*", methods: "*")]
    public class SignUpController : ApiController
    {
        public HttpResponseMessage CreateUser([FromBody] SignUpRequest signUp)
        {
            string email = string.Empty;
            string password = string.Empty;
            string ApplicationFormSettingsId = ConfigurationManager.AppSettings["ApplicationFormSettings"];
            List<ITB_GetIncompleteApplicationByEmail_Result> incompleteApplicationResult;
            ApplicationInfo applicationInfo = new ApplicationInfo();

            try
            {
                using (ApplicationFormEntities entities = new ApplicationFormEntities())
                {
                    ObjectResult<string> objResult = entities.ITB_CheckIfApplicationExists(signUp.Email);

                    List<string> lstEmail = objResult.ToList();

                    if (lstEmail == null || lstEmail.Count == 0)
                    {
                        using (PowerCampusIdentityEntities pcIdentity = new PowerCampusIdentityEntities())
                        {
                            if (signUp != null)
                            {
                                email = signUp.Email;
                                password = signUp.Password;
                            }
                            var identity = pcIdentity.spGetUserByEmail(email).FirstOrDefault(e => e.Email == email);

                            //Check if the email exists
                            if (identity == null)
                            {
                                if (email.Contains("@"))
                                {
                                    //Retrieve the applicationId from the identity application table
                                    List<spGetApplicationById_Result> result = pcIdentity.spGetApplicationById(Convert.ToInt32(ConfigurationManager.AppSettings["ApplicationId"])).ToList();

                                    if (result != null)
                                    {
                                        //Insert into the identity user table - UserName extracted from the email
                                        // pcIdentity.spInsUserByApplicationId(result[0].ApplicationId, email.Substring(0, email.IndexOf("@")), email, EncryptDecryptText.Encryptword(password), 1);
                                        pcIdentity.spInsUserByApplicationId(result[0].ApplicationId, email, email, EncryptDecryptText.Encryptword(password), 1);
                                    }
                                }
                                else
                                {
                                    //Insert into the identity table
                                    pcIdentity.spInsUserByApplicationId(new Guid(), email, email, EncryptDecryptText.Encryptword(password), 1);
                                }


                                //Initialize the Application info object
                                PersonalInfo personalInfo = new PersonalInfo() { FirstName = signUp.FirstName, LastName = signUp.LastName };

                                List<PhoneNumberInfo> PhoneNumbers = new List<PhoneNumberInfo>();
                                PhoneNumberInfo phone = new PhoneNumberInfo();
                                phone.PhoneType = ConfigurationManager.AppSettings["defaultPhoneType"];
                                phone.CountryId = Convert.ToInt32(ConfigurationManager.AppSettings["defaultCountry"]);
                                phone.PhoneNumber = signUp.PhoneNumber;
                                phone.Email = signUp.Email;
                                phone.IsPrimary = true;

                                PhoneNumbers.Add(phone);

                                applicationInfo.PersonalInfo = personalInfo;
                                applicationInfo.PhoneNumber = PhoneNumbers;


                                applicationInfo.Email = signUp.Email;


                                //Serialize the incomplete application data and insert it
                                applicationInfo.SerializeApplicationInfo(email, ApplicationFormSettingsId, entities, applicationInfo);

                                //Get the inserted incomplete application
                                incompleteApplicationResult = entities.ITB_GetIncompleteApplicationByEmail(email).ToList();

                                //Return success
                                AdditionalResponse userCreated = new AdditionalResponse()
                                {
                                    StatusCode = (int)HttpStatusCode.OK,
                                    Status = "success",
                                    Data = new AdditionalInfo { IncompleteApplicationId = incompleteApplicationResult[0].IncompleteApplicationId.ToString(), Token = incompleteApplicationResult[0].Token.ToString() }
                                };

                                SendEmail.EmailValidation(personalInfo, signUp.Email, incompleteApplicationResult[0].IncompleteApplicationId);
                                return Request.CreateResponse(userCreated);
                            }
                            //If Email exists , return failure
                            else
                            {
                                ErrorResponse emailAlreadyExist = new ErrorResponse()
                                {
                                    StatusCode = (int)HttpStatusCode.Found,
                                    Status = "failure",
                                    Msg = "You have already created an account"
                                };
                                return Request.CreateResponse(emailAlreadyExist);
                            }
                        }
                    }
                    else
                    {
                        ErrorResponse emailAlreadyExist = new ErrorResponse()
                        {
                            StatusCode = (int)HttpStatusCode.Found,
                            Status = "failure",
                            Msg = "An application is already created with your email, use another email"
                        };
                        return Request.CreateResponse(emailAlreadyExist);
                    }
                };
            }

            catch (Exception exception)
            {

                ErrorResponse errorResponse = new ErrorResponse()
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Status = "failure",
                    Msg = "An error has occured. Please contact your administrator"
                };
                LoggingManager.LogException(exception.Message, exception.StackTrace, DateTime.Now, string.Empty, exception.Source);

                return Request.CreateResponse(errorResponse);
            }
        }
    }
}
