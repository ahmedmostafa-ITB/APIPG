using DataAccess;
using Newtonsoft.Json;
using SelfServiceAPI.Classes;
using SelfServiceAPI.Helper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;

namespace SelfServiceAPI.Controllers
{

    [EnableCors(origins: "*", headers: "*", methods: "*")]

    public class SubmitController : ApiController
    {
        private const string ArabicData = "ArabicData";
        ////////

        [HttpPost]
        public void payTabsResponse([FromBody]Root value)
        {
            string custEmail = value.customer_details.email;
            string cusName = value.customer_details.name;
            string paymentRes = value.payment_result.response_status;
            string cardScheme = value.payment_info.card_scheme;
            string cartDesc = value.cart_description;
            var split = cartDesc.Split(' ');
            string paymentId = split[0];
            string ApplicationId = split[1];
            string result = string.Empty;
            string amount = value.cart_amount;
            string sessionVersion = value.tran_ref;
            string merchantId = Convert.ToString(ConfigurationManager.AppSettings["merchantID"]);
            string orderId = value.cart_id;
            string paymentResult = string.Empty;
            if (paymentRes == "A")
            {
                using (ApplicationFormEntities entities = new ApplicationFormEntities())
                {
                    using (var tranScope = entities.Database.BeginTransaction())
                    {
                        try
                        {
                            //int personId = entities.spGetPersonId.GetPersonIdFromTransId(Convert.ToInt32(paymentId));
                            entities.spUpdPaymentTransaction(Convert.ToInt32(paymentId), Convert.ToDecimal(amount), "EGP", true, null, cardScheme, sessionVersion, merchantId, orderId);
                            entities.ITB_UpdatePGLOG(Convert.ToInt32(paymentId), "Success");
                            entities.spUpdApplicationStatus(Convert.ToInt32(ApplicationId), 1, Convert.ToInt32(paymentId));
                            tranScope.Commit();
                        }
                        catch (Exception exception)
                        {
                            LoggingManager.LogException(exception.Message, exception.StackTrace, DateTime.Now, string.Empty, exception.Source);

                            tranScope.Rollback();
                        }
                    }
                }
            }

        }

        /////////////////////////
        [HttpPost]
        //[ResponseType(typeof(Submit))]

        public async Task<HttpResponseMessage> SubmitIncompleteApplication([FromBody] List<Submit> lstRequest)
        {
            IncompleteApplication incompleteApplication = null;
            ApplicationInfo applicationInfo;
            int insertedApplicationId = 0;
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("S9JNH6BMRZ-JGM9BLLLGJ-LN22TTZDKT");

            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                //Rollback the stored procedure call in case of any unexpected error occured.
                using (var tranScope = entities.Database.BeginTransaction())
                {
                    try
                    {
                        if (lstRequest != null)
                        {

                            Submit request = lstRequest[0];

                            incompleteApplication = entities.IncompleteApplications.FirstOrDefault(id => id.IncompleteApplicationId == request.IncompleteApplicationId);

                            if (incompleteApplication != null)
                            {
                                applicationInfo = ApplicationInfo.DeserializeApplicationInfo(incompleteApplication.ApplicationData);

                                if (applicationInfo != null)
                                {
                                    String uploadMsg = "";
                                    bool isPostgraduate = applicationInfo.PostgraduateInfo != null;

                                    if (isPostgraduate)
                                    {
                                        // PG specific mandatory attachment checks
                                        bool bachelorFound = AttachmentCheck(lstRequest, "BachelorDegreeCertificate");
                                        bool transcriptFound = AttachmentCheck(lstRequest, "AcademicTranscript");
                                        bool birthFound = AttachmentCheck(lstRequest, "BirthCertificate");
                                        bool cvFound = AttachmentCheck(lstRequest, "CV");
                                        bool psFound = AttachmentCheck(lstRequest, "PersonalStatement");
                                        bool lorFound = AttachmentCheck(lstRequest, "LetterOfRecommendation");
                                        bool personalPicFound = AttachmentCheck(lstRequest, "PersonalPicture");

                                        if (!bachelorFound) uploadMsg += "Official Bachelor's Degree Certificate is required, ";
                                        if (!transcriptFound) uploadMsg += "Official Academic Transcript is required, ";
                                        if (!birthFound) uploadMsg += "Birth Certificate is required, ";
                                        if (!cvFound) uploadMsg += "Résumé / CV is required, ";
                                        if (!psFound) uploadMsg += "Personal Statement is required, ";
                                        if (!lorFound) uploadMsg += "Letters of Recommendation are required, ";
                                        if (!personalPicFound) uploadMsg += "Personal Picture is required, ";

                                        // English Assessment (if taught in english No)
                                        bool englishAssessmentFound = AttachmentCheck(lstRequest, "EnglishAssessment");
                                        if ("false".Equals(applicationInfo.PostgraduateInfo.BachelorTaughtInEnglish, StringComparison.OrdinalIgnoreCase) && !englishAssessmentFound)
                                        {
                                            uploadMsg += "English Language Proficiency Certificate is required, ";
                                        }

                                        // Professional certificates
                                        bool profCertFound = AttachmentCheck(lstRequest, "ProfessionalCertificate");
                                        if ("true".Equals(applicationInfo.PostgraduateInfo.HasProfExam, StringComparison.OrdinalIgnoreCase) && !profCertFound)
                                        {
                                            uploadMsg += "Professional Certificates are required, ";
                                        }
                                        
                                        // Nationality checks for PG
                                        string nationalityConfig = ConfigurationManager.AppSettings["NationalityCheck"];
                                        int primaryCitizenship = applicationInfo.Demographic.PrimaryCitizenship;
                                        string nationality = entities.CODE_COUNTRY.FirstOrDefault(citizenShip => citizenShip.CountryId == primaryCitizenship).CODE_VALUE_KEY;

                                        bool nationalIDLabelFound = AttachmentCheck(lstRequest, "NationalID");
                                        bool passportLabelFound = AttachmentCheck(lstRequest, "Passport");

                                        if (nationality.Equals(nationalityConfig))
                                        {
                                            if (!nationalIDLabelFound) uploadMsg += "National ID is required, ";
                                        }
                                        else
                                        {
                                            if (!passportLabelFound) uploadMsg += "Passport is required, ";
                                        }
                                    }
                                    else
                                    {
                                        // UG specific mandatory attachment checks
                                        bool highSchoolTranscript = AttachmentCheck(lstRequest, "HighSchoolTranscript");
                                        if (!highSchoolTranscript) {
                                            uploadMsg += "High School Transcript or Report Card or Grade 12's Enrollment Letter is required, ";
                                        }

                                        bool transferUniFound = false;
                                        if (applicationInfo.PriorEducation != null && applicationInfo.PriorEducation.Count > 0)
                                        {
                                            foreach (EducationInfo edu in applicationInfo.PriorEducation)
                                            {
                                                string transferedInstitutionName = edu.TransferedInstitutionName;
                                                if (!string.IsNullOrEmpty(transferedInstitutionName))
                                                    transferUniFound = true;
                                            }
                                        }

                                        bool transferAttFound = false;
                                        bool courseDescriptionsAttFound = false;
                                        bool gradingSchemeAttFound = false;
                                        if (transferUniFound)
                                        {
                                            transferAttFound = AttachmentCheck(lstRequest, "UniversityTranscript");
                                            courseDescriptionsAttFound = AttachmentCheck(lstRequest, "CourseDescriptions");
                                            gradingSchemeAttFound = AttachmentCheck(lstRequest, "GradingScheme");
                                            if (!transferAttFound) 
                                            {
                                                uploadMsg += "Transfer students must submit their University Transcript to date or Enrollment Letter is required, ";
                                            }
                                            if (!courseDescriptionsAttFound) 
                                            {
                                                uploadMsg += "Transfer students must submit their Course Descriptions, ";
                                            }
                                            if (!gradingSchemeAttFound) 
                                            {
                                                uploadMsg += "Transfer students must submit their Grading Scheme is required, ";
                                            }
                                        }

                                        bool englishAssessmentFound = false;
                                        if (applicationInfo.Tests != null && applicationInfo.Tests.Count > 0)
                                        {
                                            englishAssessmentFound = AttachmentCheck(lstRequest, "EnglishAssessment");
                                            if (!englishAssessmentFound) 
                                            {
                                                uploadMsg += "English Assessment is required, "; 
                                            }
                                        }

                                        string nationalityConfig = ConfigurationManager.AppSettings["NationalityCheck"];
                                        int primaryCitizenship = applicationInfo.Demographic.PrimaryCitizenship;
                                        string nationality = entities.CODE_COUNTRY.FirstOrDefault(citizenShip => citizenShip.CountryId == primaryCitizenship).CODE_VALUE_KEY;

                                        bool nationalIDLabelFound = AttachmentCheck(lstRequest, "NationalID");
                                        bool passportLabelFound = AttachmentCheck(lstRequest, "Passport");

                                        if (nationality.Equals(nationalityConfig))
                                        {
                                            if (!nationalIDLabelFound)
                                            {
                                                uploadMsg += "National ID is required, ";
                                            }
                                        }
                                        else
                                        {
                                            if (!passportLabelFound)
                                            {
                                                uploadMsg += "Passport is required, ";
                                            }
                                        }

                                        bool personalPicFound = false;
                                        personalPicFound = AttachmentCheck(lstRequest, "PersonalPicture");

                                        if (!personalPicFound) 
                                        {
                                            uploadMsg += "Personal Picture is required, ";
                                        }
                                    }


                                    if (!string.IsNullOrEmpty(uploadMsg)) //English Assessment attachment not found
                                    {
                                        ErrorResponse errorResponse = new ErrorResponse()
                                        {
                                            StatusCode = (int)HttpStatusCode.NotFound,
                                            Status = "failure",
                                            Msg = uploadMsg + "please attach your missing files. "
                                        };
                                        return Request.CreateResponse(errorResponse);
                                    }
                                    else
                                    {
                                        insertedApplicationId = InsertApplication(lstRequest, applicationInfo);

                                        if (insertedApplicationId > 0)
                                        {
                                            //Delete the incomplete application after submission
                                            //TODO:Ali
                                            entities.IncompleteApplications.Remove(incompleteApplication);

                                            //Collect data before redirection to the payment gateway
                                            int applicationSettingId = Convert.ToInt32(ConfigurationManager.AppSettings["ApplicationFormSettings"]);
                                            //Retrieve the application fee amout
                                            decimal? amount = entities.ApplicationFormSettings.FirstOrDefault(id => id.ApplicationFormSettingId == applicationSettingId).FeeAmount;
                                            //Inset record in payment transaction
                                            ObjectParameter paymentTransactionId = new ObjectParameter("PaymentTransactionId", typeof(int));
                                            entities.spInsPaymentTransaction(paymentTransactionId, amount.Value, "Application Online Payment", 5, null, null, null, null, DateTime.Now, null, null);
                                            if (paymentTransactionId != null)
                                            {
                                                entities.spUpdApplicationPaymentTransaction(insertedApplicationId, Convert.ToInt32(paymentTransactionId.Value));
                                            }
                                            ///////////////////////

                                            PaymentObject payment = new PaymentObject();

                                            string orderId = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 10);
                                            string email = applicationInfo.Email;
                                            payment.PaymentTransactionId = Convert.ToInt32(paymentTransactionId.Value);
                                            payment.TransactionDescription = "Application Online Payment";
                                            payment.Amount = Convert.ToString(amount.Value);
                                            payment.Status = "Pending";
                                            payment.OrderId = orderId;
                                            payment.MerchantID = Convert.ToString(ConfigurationManager.AppSettings["merchantID"]);
                                            payment.Email = email;
                                            entities.ITB_InsertPGLOG(payment.PaymentTransactionId, payment.Amount, payment.TransactionDescription, payment.OrderId, payment.Status, payment.MerchantID, payment.Email);

                                            string callbackURL = ConfigurationManager.AppSettings["APIBaseURLcallback"];
                                            string cartDesc = paymentTransactionId.Value.ToString() + ' ' + insertedApplicationId;
                                            PayTabsRequest request1 = new PayTabsRequest
                                            {
                                                profile_id = Convert.ToInt32(ConfigurationManager.AppSettings["merchantID"]),
                                                tran_type = "sale",
                                                tran_class = "ecom",
                                                cart_description = cartDesc,
                                                cart_id = orderId,
                                                cart_amount = Convert.ToDouble(amount.Value),
                                                cart_currency = "EGP",
                                                callback = callbackURL + "/api/Submit/payTabsResponse",
                                                hide_shipping = true
                                                //callback = "https://webhook.site/7e4785b5-7188-4126-a261-9acb7a988bfe",
                                                //@return = "https://pcss.tkh.edu.eg/ApplicationFormTest/login"
                                            };
                                            tranScope.Commit();
                                            try
                                            {
                                                System.Net.ServicePointManager.SecurityProtocol =
                                    SecurityProtocolType.Tls12 |
            SecurityProtocolType.Tls11 |
            SecurityProtocolType.Tls;
                                                HttpResponseMessage response = await client.PostAsJsonAsync(
                                                    "https://secure-egypt.paytabs.com/payment/request", request1);
                                                string responseBody = await response.Content.ReadAsStringAsync();
                                                response.EnsureSuccessStatusCode();


                                                Root payTabsResponse = new Root();
                                                payTabsResponse = JsonConvert.DeserializeObject<Root>(responseBody);

                                                AdditionalResponse1 userFound = new AdditionalResponse1()
                                                {
                                                    StatusCode = (int)HttpStatusCode.OK,
                                                    Status = "Success",
                                                    Data = payTabsResponse
                                                };


                                                return Request.CreateResponse(userFound);
                                            }
                                            catch (System.Exception ex)
                                            {
                                                LoggingManager.LogException(ex.Message, ex.StackTrace, System.DateTime.Now, ex.ToString(), ex.Source);

                                                ErrorResponse reponse = new ErrorResponse()
                                                {
                                                    StatusCode = (int)HttpStatusCode.Conflict,
                                                    Status = "error",
                                                    Msg = "Error"
                                                };

                                                return Request.CreateResponse(reponse);

                                            }

                                            ///////////////////////////
                                            //entities.SaveChanges();
                                            tranScope.Commit();

                                            SuccessReponse successResponse = new SuccessReponse()
                                            {
                                                StatusCode = (int)HttpStatusCode.OK,
                                                Status = "success",
                                                Amount = EncryptDecrypt.Encrypt(amount.ToString()),
                                                PaymentTransactionId = paymentTransactionId.Value != null ? Convert.ToInt32(paymentTransactionId.Value) : 0,
                                                PaymentGatewayURL = ConfigurationManager.AppSettings["PaymentGatewayURL"]

                                            };

                                            return Request.CreateResponse(successResponse);
                                        }
                                        else
                                        {
                                            ErrorResponse reponse = new ErrorResponse()
                                            {
                                                StatusCode = (int)HttpStatusCode.Conflict,
                                                Status = "error",
                                                Msg = "Application already submitted"
                                            };

                                            return Request.CreateResponse(reponse);
                                        }
                                    }
                                    //}

                                    //else
                                    //{
                                    //    ErrorResponse errorResponse = new ErrorResponse()
                                    //    {
                                    //        StatusCode = (int)HttpStatusCode.NotFound,
                                    //        Status = "failure",
                                    //        Msg = "High School Transcript or Report Card or Grade 12's Enrollment Letter is required,National ID is required, Passport is required," +
                                    //        "English Assessment is required,University Transcript or Enrollment Letter is required, please attach your missing files."
                                    //    };
                                    //    return Request.CreateResponse(errorResponse);
                                    //}

                                }
                                else
                                {
                                    ErrorResponse errorResponse = new ErrorResponse()
                                    {
                                        StatusCode = (int)HttpStatusCode.NotFound,
                                        Status = "failure",
                                        Msg = "Application Info is null. Please contact your administrator"
                                    };
                                    return Request.CreateResponse(errorResponse);
                                }
                            }
                            else
                            {
                                ErrorResponse errorResponse = new ErrorResponse()
                                {
                                    StatusCode = (int)HttpStatusCode.NotFound,
                                    Status = "failure",
                                    Msg = "Incomplete Application is null. Please contact your administrator"
                                };
                                return Request.CreateResponse(errorResponse);
                            }

                        }

                        else
                        {
                            ErrorResponse errorResponse = new ErrorResponse()
                            {
                                StatusCode = (int)HttpStatusCode.NotFound,
                                Status = "failure",
                                Msg = "Request is null"
                            };
                            return Request.CreateResponse(errorResponse);
                        }
                    }
                    catch (Exception exception)
                    {
                        ErrorResponse errorResponse = new ErrorResponse()
                        {
                            StatusCode = (int)HttpStatusCode.NotFound,
                            Status = "failure",
                            Msg = "An error has occured. Please contact your administrator."
                        };

                        tranScope.Rollback();

                        if (incompleteApplication != null)
                            LoggingManager.LogException(exception.Message, exception.StackTrace, DateTime.Now, string.Empty, exception.Source);
                        else
                            LoggingManager.LogException(exception.Message, exception.StackTrace, DateTime.Now, string.Empty, exception.Source);
                        return Request.CreateResponse(errorResponse);
                    }
                }
            }
        }

        //public HttpResponseMessage Test(dynamic jObject)
        //{
        //    foreach (var item in jObject)
        //    {
        //        string test = item.ToString();
        //    }
        //    return Request.CreateResponse();
        //}


        private bool AttachmentCheck(List<Submit> lstRequest, string attachmentName)
        {
            bool attachmentFound = false;

            if (lstRequest[0].label != null)
            {
                Submit submit = lstRequest.Find(item => item.label.Equals(attachmentName));

                if (submit != null)
                {
                    attachmentFound = true;
                }
            }

            return attachmentFound;
        }

        private static int InsertApplication(List<Submit> lstRequest, ApplicationInfo applicationInfo)
        {
            //Insert main application data

            int insertedApplicationId = 0;

            if (applicationInfo.Demographic != null && applicationInfo.AcademicInterest != null)
            {
                //Check if the application already submiited , prevent multiple submission

                //TODO: Ali
                //bool isSubmitted = false;
                bool isSubmitted = Login.ApplicationSubmitted(applicationInfo.Email);

                if (!isSubmitted)
                {
                    using (ApplicationFormEntities entities = new ApplicationFormEntities())
                    {
                        //Rollback the stored procedure call in case of any unexpected error occured.
                        using (var tranScope = entities.Database.BeginTransaction())
                        {
                            try
                            {
                                insertedApplicationId = Submit.InsertApplication(applicationInfo, Convert.ToInt32(ConfigurationManager.AppSettings["ApplicationProgramSettings"]));

                                if (insertedApplicationId > 0)
                                {
                                    //Insert application phone data
                                    if (applicationInfo.PhoneNumber != null && applicationInfo.PhoneNumber.Count > 0)
                                        Submit.InsertApplicationPhone(applicationInfo.PhoneNumber, insertedApplicationId);

                                    //Insert application address data
                                    if (applicationInfo.Address != null && applicationInfo.Address.Count > 0)
                                        Submit.InsertApplicationAddress(applicationInfo.Address, insertedApplicationId);

                                    //Insert application source data
                                    if (applicationInfo.SourceInfo != null && applicationInfo.SourceInfo.Count > 0)
                                        Submit.InsertApplicationSource(applicationInfo.SourceInfo, insertedApplicationId);

                                    //Insert application test score
                                    if (applicationInfo.TestScores != null && applicationInfo.TestScores.Count > 0)
                                        Submit.InsertApplicationTestScore(applicationInfo.TestScores, insertedApplicationId);

                                    //Insert application tests
                                    if (applicationInfo.Tests != null && applicationInfo.Tests.Count > 0)
                                        Submit.InsertApplicationTests(applicationInfo.Tests, insertedApplicationId);

                                    //Insert application program
                                    if (applicationInfo.AcademicInterest != null)
                                        Submit.InsertApplicationProgram(applicationInfo.AcademicInterest, insertedApplicationId);


                                    //Added By Mohammad Farfour 
                                    //Insert application campus
                                    if (applicationInfo.AcademicInterest != null) {
                                        if (applicationInfo.AcademicInterest.PreferredCampus != "")
                                            Submit.InsertApplicationCampus(applicationInfo.AcademicInterest, insertedApplicationId);
                                    }
                                    //Insert application relationship and emergency contact
                                    if (applicationInfo.ApplicationRelations != null && applicationInfo.ApplicationRelations.Count > 0)
                                        Submit.InsertApplicationRelationShip(applicationInfo.ApplicationRelations, insertedApplicationId);

                                    //Insert application relationship sibling
                                    if (!string.IsNullOrEmpty(applicationInfo.PersonalInfo.SiblingFName) && !string.IsNullOrEmpty(applicationInfo.PersonalInfo.SiblingLName))
                                    {
                                        int siblingPrefixId = Convert.ToInt32(ConfigurationManager.AppSettings["SiblingCodeId"]);
                                        Submit.InsertApplicationSiblingRelationShip(applicationInfo.PersonalInfo, insertedApplicationId, siblingPrefixId);
                                    }

                                    //Insert application education
                                    if (applicationInfo.PriorEducation != null && applicationInfo.PriorEducation.Count > 0)
                                        Submit.InsertApplicationEducation(applicationInfo.PriorEducation, insertedApplicationId);

                                    //Insert application employment
                                    if (applicationInfo.Employment != null && applicationInfo.Employment.Count > 0)
                                        Submit.InsertApplicationEmployment(applicationInfo.Employment, insertedApplicationId);


                                    //Insert application attachments
                                    foreach (Submit attachment in lstRequest)
                                    {
                                        if (attachment != null && attachment.FileContent !=null && !string.IsNullOrEmpty(attachment.FileName) && !string.IsNullOrEmpty(attachment.FileExtension))
                                            Submit.InsertApplicationAttachment(attachment, insertedApplicationId);
                                    }

                                    //Insert Application User Defined
                                    List<ApplicationUserDefinedInfo> lstUserDefined = applicationInfo.UserDefined ?? new List<ApplicationUserDefinedInfo>();

                                    // ──────────────────────────────────────────────────────────────
                                    // PG Postgraduate Data → ApplicationUserDefined
                                    // These fields are stored in USERDEFINEDIND columns added for PG.
                                    // Column names must match USERDEFINEDIND column names exactly.
                                    // TODO: When dedicated PG tables are designed, move these inserts
                                    //       to their own methods and remove from UserDefined.
                                    // ──────────────────────────────────────────────────────────────
                                    if (applicationInfo.PostgraduateInfo != null)
                                    {
                                        var pg = applicationInfo.PostgraduateInfo;

                                        // -- PG: Military & Marital Status --
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "MilitaryStatus", ColumnValue = pg.MilitaryStatus ?? "", ColumnType = 1, ColumnLabel = "MilitaryStatus", IsUploading = true, Description = "PostgraduateData" });
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "MaritalStatus", ColumnValue = pg.MaritalStatus ?? "", ColumnType = 1, ColumnLabel = "MaritalStatus", IsUploading = true, Description = "PostgraduateData" });

                                        // -- PG: Employment --
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "EmploymentStatus", ColumnValue = pg.EmploymentStatus ?? "", ColumnType = 1, ColumnLabel = "EmploymentStatus", IsUploading = true, Description = "PostgraduateData" });
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "EmployerCompanyName", ColumnValue = pg.EmployerCompanyName ?? "", ColumnType = 1, ColumnLabel = "EmployerCompanyName", IsUploading = true, Description = "PostgraduateData" });
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "EmployerPosition", ColumnValue = pg.EmployerPosition ?? "", ColumnType = 1, ColumnLabel = "EmployerPosition", IsUploading = true, Description = "PostgraduateData" });
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "EmployerStartDate", ColumnValue = pg.EmployerStartDate ?? "", ColumnType = 1, ColumnLabel = "EmployerStartDate", IsUploading = true, Description = "PostgraduateData" });
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "EmployerDuties", ColumnValue = pg.EmployerDuties ?? "", ColumnType = 1, ColumnLabel = "EmployerDuties", IsUploading = true, Description = "PostgraduateData" });
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "EmployerIdNumber", ColumnValue = pg.EmployerIdNumber ?? "", ColumnType = 1, ColumnLabel = "EmployerIdNumber", IsUploading = true, Description = "PostgraduateData" });

                                        // -- PG: Coventry Alumni & English Proficiency --
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "CovAlumni", ColumnValue = pg.CovAlumni ?? "", ColumnType = 1, ColumnLabel = "CovAlumni", IsUploading = true, Description = "PostgraduateData" });
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "CoventryId", ColumnValue = pg.CoventryId ?? "", ColumnType = 1, ColumnLabel = "CoventryId", IsUploading = true, Description = "PostgraduateData" });
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "BachelorTaughtInEnglish", ColumnValue = pg.BachelorTaughtInEnglish ?? "", ColumnType = 1, ColumnLabel = "BachelorTaughtInEnglish", IsUploading = true, Description = "PostgraduateData" });
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "HasProfExam", ColumnValue = pg.HasProfExam ?? "", ColumnType = 1, ColumnLabel = "HasProfExam", IsUploading = true, Description = "PostgraduateData" });

                                        // -- PG: Emergency Contact --
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "PgEmergencyContactRelationship", ColumnValue = pg.PgEmergencyContactRelationship ?? "", ColumnType = 1, ColumnLabel = "PgEmergencyContactRelationship", IsUploading = true, Description = "PostgraduateData" });
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "PgEmergencyContactGivenName", ColumnValue = pg.PgEmergencyContactGivenName ?? "", ColumnType = 1, ColumnLabel = "PgEmergencyContactGivenName", IsUploading = true, Description = "PostgraduateData" });
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "PgEmergencyContactMiddleName", ColumnValue = pg.PgEmergencyContactMiddleName ?? "", ColumnType = 1, ColumnLabel = "PgEmergencyContactMiddleName", IsUploading = true, Description = "PostgraduateData" });
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "PgEmergencyContactFamilyName", ColumnValue = pg.PgEmergencyContactFamilyName ?? "", ColumnType = 1, ColumnLabel = "PgEmergencyContactFamilyName", IsUploading = true, Description = "PostgraduateData" });
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "PgEmergencyContactMobile", ColumnValue = pg.PgEmergencyContactMobile ?? "", ColumnType = 1, ColumnLabel = "PgEmergencyContactMobile", IsUploading = true, Description = "PostgraduateData" });
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "PgEmergencyContactEmail", ColumnValue = pg.PgEmergencyContactEmail ?? "", ColumnType = 1, ColumnLabel = "PgEmergencyContactEmail", IsUploading = true, Description = "PostgraduateData" });

                                        // -- PG: Bachelor Education Details --
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "PgSchoolName", ColumnValue = pg.PgSchoolName ?? "", ColumnType = 1, ColumnLabel = "PgSchoolName", IsUploading = true, Description = "PostgraduateData" });
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "PgBachelorUniversity", ColumnValue = pg.PgBachelorUniversity ?? "", ColumnType = 1, ColumnLabel = "PgBachelorUniversity", IsUploading = true, Description = "PostgraduateData" });
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "PgBachelorUniversityName", ColumnValue = pg.PgBachelorUniversityName ?? "", ColumnType = 1, ColumnLabel = "PgBachelorUniversityName", IsUploading = true, Description = "PostgraduateData" });
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "PgBachelorDegree", ColumnValue = pg.PgBachelorDegree ?? "", ColumnType = 1, ColumnLabel = "PgBachelorDegree", IsUploading = true, Description = "PostgraduateData" });
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "PgBachelorFieldOfStudy", ColumnValue = pg.PgBachelorFieldOfStudy ?? "", ColumnType = 1, ColumnLabel = "PgBachelorFieldOfStudy", IsUploading = true, Description = "PostgraduateData" });
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "PgBachelorYearOfGrad", ColumnValue = pg.PgBachelorYearOfGrad ?? "", ColumnType = 1, ColumnLabel = "PgBachelorYearOfGrad", IsUploading = true, Description = "PostgraduateData" });

                                        // -- PG: Academic Awards & Professional Exams (JSON strings) --
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "PgHasAcademicAward", ColumnValue = pg.PgHasAcademicAward ?? "", ColumnType = 1, ColumnLabel = "PgHasAcademicAward", IsUploading = true, Description = "PostgraduateData" });
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "AcademicAwards", ColumnValue = pg.AcademicAwards ?? "", ColumnType = 1, ColumnLabel = "AcademicAwards", IsUploading = true, Description = "PostgraduateData" });
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "PgAcademicAwards", ColumnValue = pg.PgAcademicAwards ?? "", ColumnType = 1, ColumnLabel = "PgAcademicAwards", IsUploading = true, Description = "PostgraduateData" });
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "PgProfExamsData", ColumnValue = pg.PgProfExamsData ?? "", ColumnType = 1, ColumnLabel = "PgProfExamsData", IsUploading = true, Description = "PostgraduateData" });
                                    }
                                    // ──────────────────────────────────────────────────────────────

                                    if (lstUserDefined.Count > 0)
                                        Submit.InsertApplicationUserDefined(lstUserDefined, insertedApplicationId);

                                    // Delete from the identity database
                                    using (PowerCampusIdentityEntities pcIdentity = new PowerCampusIdentityEntities())
                                    {
                                        IdentityUser identityUser = pcIdentity.IdentityUsers.FirstOrDefault(email => email.Email == applicationInfo.Email);

                                        if (identityUser != null)
                                        {
                                            //TODO:Ali
                                            pcIdentity.IdentityUsers.Remove(identityUser);
                                            pcIdentity.SaveChanges();
                                        }
                                    }
                                    entities.SaveChanges();
                                    tranScope.Commit();
                                }
                            }
                            catch (Exception exception)
                            {
                                tranScope.Rollback();
                                LoggingManager.LogException(exception.Message, exception.StackTrace, DateTime.Now, string.Empty, string.Empty);
                                throw;
                            }
                        }
                    }

                }
            }

            return insertedApplicationId;
        }
    }
}

