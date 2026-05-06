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

        /// <summary>
        /// Verifies a PayTabs transaction reference. Called by payment-success page.
        /// Returns: status = "success" | "failed" | "invalid"
        /// If callback was missed but PayTabs confirms success, runs acceptance procedures.
        /// </summary>
        [HttpGet]
        public HttpResponseMessage VerifyPayment(string tranRef = null, int? ptId = null)
        {
            // No identifier at all = direct URL access
            if (string.IsNullOrEmpty(tranRef) && (!ptId.HasValue || ptId.Value <= 0))
                return Request.CreateResponse(HttpStatusCode.OK, new { status = "invalid" });

            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                // Detect test mode from connection string (PCDB_TESTING)
                string connString = entities.Database.Connection.ConnectionString;
                bool isTestMode = connString.IndexOf("PCDB_TESTING", StringComparison.OrdinalIgnoreCase) >= 0;

                // Resolve tranRef from ptId if tranRef not provided (test mode: PayTabs doesn't append tranRef)
                if (string.IsNullOrEmpty(tranRef) && ptId.HasValue && ptId.Value > 0)
                {
                    var ptRecord = entities.PaymentTransactions.FirstOrDefault(pt => pt.PaymentTransactionId == ptId.Value);
                    if (ptRecord == null)
                        return Request.CreateResponse(HttpStatusCode.OK, new { status = "invalid" });
                    if (ptRecord.IsSuccessful)
                        return Request.CreateResponse(HttpStatusCode.OK, new { status = "success" });
                    // Get tranRef from the record to query PayTabs
                    tranRef = ptRecord.AuthorizationNumber;
                    if (string.IsNullOrEmpty(tranRef))
                    {
                        // No tranRef stored yet (callback hasn't fired) — query PayTabs by cart_id
                        return Request.CreateResponse(HttpStatusCode.OK, new { status = "failed" });
                    }
                }

                if (string.IsNullOrEmpty(tranRef))
                    return Request.CreateResponse(HttpStatusCode.OK, new { status = "invalid" });

                // Check if already marked successful in our DB
                var transaction = entities.PaymentTransactions
                    .FirstOrDefault(pt => pt.AuthorizationNumber == tranRef);

                if (transaction != null && transaction.IsSuccessful)
                    return Request.CreateResponse(HttpStatusCode.OK, new { status = "success" });

                // Give the PayTabs callback time to complete before we query PayTabs ourselves.
                // The callback (payTabsResponse) and this endpoint race — without this delay,
                // both would run acceptance procedures, creating duplicate student IDs.
                // See known-issues.md #8.
                System.Threading.Thread.Sleep(3000);

                // Re-check DB after delay — callback may have completed by now
                // Use AsNoTracking to bypass EF cache and get fresh DB value
                var updatedTransaction = entities.PaymentTransactions
                    .AsNoTracking()
                    .FirstOrDefault(pt => pt.AuthorizationNumber == tranRef);
                if (updatedTransaction != null && updatedTransaction.IsSuccessful)
                    return Request.CreateResponse(HttpStatusCode.OK, new { status = "success" });

                // Still not successful — verify with PayTabs directly
                try
                {
                    string payTabsServerKey = ConfigurationManager.AppSettings["PayTabsServerKey"];
                    using (HttpClient client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(15);
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(payTabsServerKey);
                        System.Net.ServicePointManager.SecurityProtocol =
                            SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                        int profileId = Convert.ToInt32(ConfigurationManager.AppSettings["merchantID"]);
                        var payload = new { profile_id = profileId, tran_ref = tranRef };
                        HttpResponseMessage response = client.PostAsJsonAsync(
                            "https://secure-egypt.paytabs.com/payment/query", payload).Result;
                        string responseBody = response.Content.ReadAsStringAsync().Result;

                        // Log full PayTabs response for debugging
                        LoggingManager.LogException(
                            "VerifyPayment PayTabs query response: " + responseBody,
                            null, DateTime.Now, "tranRef: " + tranRef + " | isTestMode: " + isTestMode, "SubmitController");

                        if (response.IsSuccessStatusCode)
                        {
                            dynamic result = JsonConvert.DeserializeObject<dynamic>(responseBody);
                            string paymentStatus = (string)(result.payment_result?.response_status ?? "");

                            if (paymentStatus == "A") // Approved
                            {
                                // PayTabs confirms success. Only run recovery if the callback
                                // genuinely hasn't processed this yet (re-read from DB to be sure).
                                var freshCheck = entities.PaymentTransactions
                                    .AsNoTracking()
                                    .FirstOrDefault(pt => pt.AuthorizationNumber == tranRef);
                                if (freshCheck != null && !freshCheck.IsSuccessful)
                                {
                                    string cartDesc = (string)(result.cart_description ?? "");
                                    var split = cartDesc.Split(' ');
                                    if (split.Length >= 2)
                                    {
                                        int paymentId = Convert.ToInt32(split[0]);
                                        int applicationId = Convert.ToInt32(split[1]);
                                        string amount = (string)(result.cart_amount ?? "0");
                                        string cardScheme = (string)(result.payment_info?.card_scheme ?? "");
                                        string merchantId = Convert.ToString(ConfigurationManager.AppSettings["merchantID"]);
                                        string orderId = (string)(result.cart_id ?? "");

                                        entities.spUpdPaymentTransaction(paymentId, Convert.ToDecimal(amount), "EGP", true, null, cardScheme, tranRef, merchantId, orderId);
                                        entities.ITB_UpdatePGLOG(paymentId, "Success");
                                        entities.spUpdApplicationStatus(applicationId, 1, paymentId);
                                    }
                                }
                                return Request.CreateResponse(HttpStatusCode.OK, new { status = "success" });
                            }
                            else
                            {
                                return Request.CreateResponse(HttpStatusCode.OK, new { status = "failed" });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggingManager.LogException("VerifyPayment error: " + ex.Message, ex.StackTrace, DateTime.Now, "tranRef: " + tranRef + " | isTestMode: " + isTestMode, "SubmitController");
                }

                // PayTabs query failed — in test mode, check DB callback result; in prod, fail safe
                if (isTestMode && transaction != null)
                    return Request.CreateResponse(HttpStatusCode.OK, new { status = transaction.IsSuccessful ? "success" : "failed" });

                return Request.CreateResponse(HttpStatusCode.OK, new { status = "failed" });
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
            client.Timeout = TimeSpan.FromSeconds(30);
            string payTabsServerKey = ConfigurationManager.AppSettings["PayTabsServerKey"];
            client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue(payTabsServerKey);

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

                                        // English Assessment (required if NOT taught in English AND NOT Coventry alumni)
                                        bool englishAssessmentFound = AttachmentCheck(lstRequest, "EnglishAssessment");
                                        bool isCovAlumni = "Yes".Equals(applicationInfo.PostgraduateInfo.CovAlumni, StringComparison.OrdinalIgnoreCase);
                                        if ("false".Equals(applicationInfo.PostgraduateInfo.BachelorTaughtInEnglish, StringComparison.OrdinalIgnoreCase) && !isCovAlumni && !englishAssessmentFound)
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
                                                @return = ConfigurationManager.AppSettings["ApplicationLink"] + "/payment-success?ptId=" + paymentTransactionId.Value,
                                                hide_shipping = true
                                            };

                                            // ─── PayTabs Payment Gateway (ACTIVE) ───────────────────────
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

                                                // Application was submitted successfully but payment gateway failed.
                                                ErrorResponse reponse = new ErrorResponse()
                                                {
                                                    StatusCode = (int)HttpStatusCode.OK,
                                                    Status = "warning",
                                                    Msg = "Your application has been submitted successfully. However, the payment step could not be completed. Please contact the admissions office for payment instructions."
                                                };

                                                return Request.CreateResponse(reponse);
                                            }
                                            // ─── END PayTabs ────────────────────────────────────────────

                                            /* ─── SIMULATION: Bypass PayTabs (uncomment to simulate payment locally) ───
                                            //ITB - Ahmed Mostafa 2026/04/22
                                            tranScope.Commit();
                                            try
                                            {
                                                using (ApplicationFormEntities entities2 = new ApplicationFormEntities())
                                                {
                                                    using (var tranScope2 = entities2.Database.BeginTransaction())
                                                    {
                                                        entities2.spUpdPaymentTransaction(
                                                            Convert.ToInt32(paymentTransactionId.Value),
                                                            amount.Value,
                                                            "EGP",
                                                            true,
                                                            null,
                                                            "TEST",
                                                            Guid.NewGuid().ToString(),
                                                            payment.MerchantID,
                                                            orderId
                                                        );

                                                        entities2.ITB_UpdatePGLOG(Convert.ToInt32(paymentTransactionId.Value), "Success");

                                                        entities2.spUpdApplicationStatus(
                                                            insertedApplicationId,
                                                            1, // submitted/paid
                                                            Convert.ToInt32(paymentTransactionId.Value)
                                                        );

                                                        tranScope2.Commit();
                                                    }
                                                }

                                                ErrorResponse successResponse = new ErrorResponse()
                                                {
                                                    StatusCode = (int)HttpStatusCode.OK,
                                                    Status = "warning",
                                                    Msg = "Your application has been submitted successfully! Please proceed to the login page."
                                                };

                                                return Request.CreateResponse(successResponse);
                                            }
                                            catch (Exception ex)
                                            {
                                                LoggingManager.LogException(ex.Message, ex.StackTrace, DateTime.Now, ex.ToString(), ex.Source);

                                                ErrorResponse response = new ErrorResponse()
                                                {
                                                    StatusCode = (int)HttpStatusCode.InternalServerError,
                                                    Status = "error",
                                                    Msg = "Application saved but payment failed"
                                                };

                                                return Request.CreateResponse(response);
                                            }
                                            ─── END SIMULATION ─────────────────────────────────────────── */
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
            Submit submit = lstRequest.Find(item => item.label != null && item.label.Equals(attachmentName));
            return submit != null;
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
                                insertedApplicationId = Submit.InsertApplication(entities, applicationInfo, Convert.ToInt32(ConfigurationManager.AppSettings["ApplicationProgramSettings"]));

                                if (insertedApplicationId > 0)
                                {
                                    //Insert application phone data
                                    if (applicationInfo.PhoneNumber != null && applicationInfo.PhoneNumber.Count > 0)
                                        Submit.InsertApplicationPhone(entities, applicationInfo.PhoneNumber, insertedApplicationId);

                                    //Insert application address data
                                    if (applicationInfo.Address != null && applicationInfo.Address.Count > 0)
                                        Submit.InsertApplicationAddress(entities, applicationInfo.Address, insertedApplicationId);

                                    //Insert application source data
                                    if (applicationInfo.SourceInfo != null && applicationInfo.SourceInfo.Count > 0)
                                        Submit.InsertApplicationSource(entities, applicationInfo.SourceInfo, insertedApplicationId);

                                    //Insert application test score
                                    if (applicationInfo.TestScores != null && applicationInfo.TestScores.Count > 0)
                                        Submit.InsertApplicationTestScore(entities, applicationInfo.TestScores, insertedApplicationId);

                                    //Insert application tests
                                    if (applicationInfo.Tests != null && applicationInfo.Tests.Count > 0)
                                        Submit.InsertApplicationTests(entities, applicationInfo.Tests, insertedApplicationId);

                                    //Insert application program
                                    if (applicationInfo.AcademicInterest != null)
                                        Submit.InsertApplicationProgram(entities, applicationInfo.AcademicInterest, insertedApplicationId);


                                    //Insert application campus
                                    if (applicationInfo.AcademicInterest != null)
                                    {
                                        if (!string.IsNullOrEmpty(applicationInfo.AcademicInterest.PreferredCampus))
                                        {
                                            // UG flow: campus from user selection
                                            Submit.InsertApplicationCampus(entities, applicationInfo.AcademicInterest, insertedApplicationId);
                                        }
                                        else if (applicationInfo.AcademicInterest.ProgramOfStudy > 0)
                                        {
                                            // PG flow: campus from PROGRAMOFSTUDY.CampusId based on selected major
                                            var campusId = entities.Database.SqlQuery<int?>(
                                                "SELECT CampusId FROM PROGRAMOFSTUDY WHERE ProgramOfStudyId = @p0",
                                                applicationInfo.AcademicInterest.ProgramOfStudy).FirstOrDefault();
                                            if (campusId.HasValue && campusId.Value > 0)
                                            {
                                                ObjectParameter appCampusId = new ObjectParameter("ApplicationCampusId", typeof(int));
                                                entities.spInsApplicationCampus(appCampusId, insertedApplicationId, campusId.Value);
                                            }
                                        }
                                    }
                                    //Insert application relationship and emergency contact
                                    if (applicationInfo.ApplicationRelations != null && applicationInfo.ApplicationRelations.Count > 0)
                                        Submit.InsertApplicationRelationShip(entities, applicationInfo.ApplicationRelations, insertedApplicationId);

                                    //Insert application relationship sibling
                                    if (!string.IsNullOrEmpty(applicationInfo.PersonalInfo.SiblingFName) && !string.IsNullOrEmpty(applicationInfo.PersonalInfo.SiblingLName))
                                    {
                                        int siblingPrefixId = Convert.ToInt32(ConfigurationManager.AppSettings["SiblingCodeId"]);
                                        Submit.InsertApplicationSiblingRelationShip(entities, applicationInfo.PersonalInfo, insertedApplicationId, siblingPrefixId);
                                    }

                                    //Insert application education
                                    if (applicationInfo.PriorEducation != null && applicationInfo.PriorEducation.Count > 0)
                                        Submit.InsertApplicationEducation(entities, applicationInfo.PriorEducation, insertedApplicationId);

                                    //Insert application employment
                                    if (applicationInfo.Employment != null && applicationInfo.Employment.Count > 0)
                                        Submit.InsertApplicationEmployment(entities, applicationInfo.Employment, insertedApplicationId);

                                    // ── PG Employment → ApplicationEmployment table ──────────────
                                    // Moved from UserDefined: EmployerCompanyName, EmployerPosition, EmployerStartDate
                                    // Kept in UserDefined: EmploymentStatus, EmployerDuties, EmployerIdNumber
                                    // TODO: When full PG tables are designed, consolidate with above
                                    if (applicationInfo.PostgraduateInfo != null)
                                    {
                                        var pgEmp = applicationInfo.PostgraduateInfo;
                                        string empStatus = pgEmp.EmploymentStatus ?? "";

                                        if (empStatus == "Employed" && !string.IsNullOrEmpty(pgEmp.EmployerCompanyName))
                                        {
                                            ObjectParameter pgEmploymentId = new ObjectParameter("ApplicationEmploymentId", typeof(int));
                                            DateTime? pgStartDate = null;
                                            if (!string.IsNullOrEmpty(pgEmp.EmployerStartDate))
                                            {
                                                DateTime parsed;
                                                if (DateTime.TryParse(pgEmp.EmployerStartDate, out parsed))
                                                    pgStartDate = parsed < new DateTime(1900, 1, 1) ? new DateTime(1900, 1, 1) : parsed;
                                            }
                                            // Brief of Duties → Remarks
                                            entities.spInsApplicationEmployment(pgEmploymentId, insertedApplicationId,
                                                pgEmp.EmployerCompanyName, pgEmp.EmployerPosition ?? "",
                                                pgStartDate, (DateTime?)null, pgEmp.EmployerDuties ?? "");
                                        }
                                        else if (empStatus == "TKHStaff")
                                        {
                                            // Default employer/position for TKH Staff, Employer ID → Remarks
                                            ObjectParameter pgEmploymentId = new ObjectParameter("ApplicationEmploymentId", typeof(int));
                                            entities.spInsApplicationEmployment(pgEmploymentId, insertedApplicationId,
                                                "The Knowledge Hub", "TKH Staff",
                                                (DateTime?)null, (DateTime?)null, pgEmp.EmployerIdNumber ?? "");
                                        }
                                    }
                                    // ── END PG Employment ────────────────────────────────────────

                                    // ── PG Professional Exams → ApplicationTestScore ─────────────
                                    // GRE (TestId=16) and GMAT (TestId=17) mapped to ApplicationTestScore.
                                    // "Other" exams stay in UserDefined as PgProfExamsData JSON.
                                    // TestTypeId=1013 (Overall) for both GRE and GMAT.
                                    if (applicationInfo.PostgraduateInfo != null && "true".Equals(applicationInfo.PostgraduateInfo.HasProfExam, StringComparison.OrdinalIgnoreCase)
                                        && !string.IsNullOrEmpty(applicationInfo.PostgraduateInfo.PgProfExamsData))
                                    {
                                        try
                                        {
                                            var profExams = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(applicationInfo.PostgraduateInfo.PgProfExamsData);
                                            ObjectParameter profTestScoreId = new ObjectParameter("ApplicationTestScoreId", typeof(int));
                                            foreach (var exam in profExams)
                                            {
                                                string examType = (string)(exam.examType ?? "");
                                                int testId = 0;
                                                // Frontend sends TestId (int) from the dropdown
                                                int.TryParse(examType, out testId);
                                                // Legacy support: text-based exam types — look up by CODE_VALUE
                                                if (testId == 0 && !string.IsNullOrEmpty(examType))
                                                {
                                                    var testCode = entities.CODE_TEST.FirstOrDefault(t => t.CODE_VALUE == examType && t.STATUS == "A");
                                                    if (testCode != null) testId = testCode.TestId;
                                                }

                                                if (testId > 0)
                                                {
                                                    decimal score = 0;
                                                    decimal.TryParse((string)(exam.score ?? "0"), out score);
                                                    DateTime? dateTaken = null;
                                                    string dateStr = (string)(exam.dateTaken ?? "");
                                                    if (!string.IsNullOrEmpty(dateStr))
                                                    {
                                                        DateTime parsed;
                                                        if (DateTime.TryParse(dateStr, out parsed)) dateTaken = parsed;
                                                    }
                                                    // Look up the default TestTypeId (e.g. "Overall") for this test
                                                    var testTypes = entities.Database.SqlQuery<int>(
                                                        "SELECT TOP 1 tt.TestTypeId FROM CODE_TESTTYPE tt " +
                                                        "JOIN Code_TestLink tl ON tt.CODE_VALUE = tl.Type " +
                                                        "WHERE tl.Test = (SELECT CODE_VALUE FROM CODE_TEST WHERE TestId = @p0)",
                                                        testId).ToList();
                                                    int testTypeId = testTypes.Count > 0 ? testTypes[0] : 0;

                                                    entities.spInsApplicationTestScore(profTestScoreId, insertedApplicationId,
                                                        testId, testTypeId > 0 ? testTypeId : (int?)null, dateTaken, score, string.Empty, null, null, null);
                                                }
                                                // "Other" exams: insert score/date into ApplicationTestScore using "OTHER" test code
                                                if (testId == 0)
                                                {
                                                    var otherTest = entities.CODE_TEST.FirstOrDefault(t => t.CODE_VALUE_KEY == "OTHER" && t.STATUS == "A");
                                                    if (otherTest != null)
                                                    {
                                                        decimal otherScore = 0;
                                                        decimal.TryParse((string)(exam.score ?? "0"), out otherScore);
                                                        DateTime? otherDate = null;
                                                        string otherDateStr = (string)(exam.dateTaken ?? "");
                                                        if (!string.IsNullOrEmpty(otherDateStr))
                                                        {
                                                            DateTime parsed;
                                                            if (DateTime.TryParse(otherDateStr, out parsed)) otherDate = parsed;
                                                        }
                                                        var otherTestTypes = entities.Database.SqlQuery<int>(
                                                            "SELECT TOP 1 tt.TestTypeId FROM CODE_TESTTYPE tt " +
                                                            "JOIN Code_TestLink tl ON tt.CODE_VALUE = tl.Type " +
                                                            "WHERE tl.Test = (SELECT CODE_VALUE FROM CODE_TEST WHERE TestId = @p0)",
                                                            otherTest.TestId).ToList();
                                                        int otherTestTypeId = otherTestTypes.Count > 0 ? otherTestTypes[0] : 0;

                                                        entities.spInsApplicationTestScore(profTestScoreId, insertedApplicationId,
                                                            otherTest.TestId, otherTestTypeId > 0 ? otherTestTypeId : (int?)null, otherDate, otherScore, string.Empty, null, null, null);
                                                    }
                                                }
                                            }
                                        }
                                        catch { /* JSON parse failure — data stays in UserDefined */ }
                                    }
                                    // ── END PG Professional Exams ─────────────────────────────────

                                    // ── PG Professional Exam Notes → ITB_ApplicationNotes ─────────
                                    // Each exam's "Additional Notes" field gets a note row (Office=ADMSS, NoteType=EXMNOT)
                                    if (applicationInfo.PostgraduateInfo != null
                                        && "true".Equals(applicationInfo.PostgraduateInfo.HasProfExam, StringComparison.OrdinalIgnoreCase)
                                        && !string.IsNullOrEmpty(applicationInfo.PostgraduateInfo.PgProfExamsData))
                                    {
                                        try
                                        {
                                            var profExamsForNotes = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(applicationInfo.PostgraduateInfo.PgProfExamsData);
                                            int examNoteIndex = 0;
                                            foreach (var exam in profExamsForNotes)
                                            {
                                                examNoteIndex++;
                                                string examType = (string)(exam.examType ?? "");
                                                string examOtherName = (string)(exam.examOtherName ?? "");
                                                string examScore = (string)(exam.score ?? "");
                                                string examDate = (string)(exam.dateTaken ?? "");
                                                string examNotes = (string)(exam.notes ?? "");

                                                // Resolve display name
                                                int testIdCheck = 0;
                                                int.TryParse(examType, out testIdCheck);
                                                string displayName = examOtherName;
                                                if (string.IsNullOrEmpty(displayName) && testIdCheck > 0)
                                                {
                                                    var testRecord = entities.CODE_TEST.FirstOrDefault(t => t.TestId == testIdCheck);
                                                    displayName = testRecord != null ? testRecord.LONG_DESC : examType;
                                                }
                                                else if (string.IsNullOrEmpty(displayName))
                                                {
                                                    displayName = examType;
                                                }

                                                bool isOtherExam = testIdCheck == 0;

                                                // "Other" exams: full details (name, score, date, notes) since score/date
                                                // go to ApplicationTestScore under generic "OTHER" code
                                                // Standard exams (GRE/GMAT): only additional notes (score/date already in ApplicationTestScore)
                                                if (isOtherExam && (!string.IsNullOrEmpty(displayName) || !string.IsNullOrEmpty(examScore)))
                                                {
                                                    string noteText = "Exam " + examNoteIndex;
                                                    if (!string.IsNullOrEmpty(displayName)) noteText += " | Type: " + displayName;
                                                    if (!string.IsNullOrEmpty(examScore)) noteText += " | Score: " + examScore;
                                                    if (!string.IsNullOrEmpty(examDate)) noteText += " | Date: " + examDate;
                                                    if (!string.IsNullOrEmpty(examNotes)) noteText += " | Notes: " + examNotes;

                                                    entities.Database.ExecuteSqlCommand(
                                                        @"INSERT INTO ITB_ApplicationNotes (ApplicationId, Office, NoteType, Notes)
                                                          VALUES (@p0, @p1, @p2, @p3)",
                                                        insertedApplicationId, "ADMSS", "EXMNOT", noteText);
                                                }
                                                else if (!isOtherExam && !string.IsNullOrEmpty(examNotes))
                                                {
                                                    string noteText = "Exam " + examNoteIndex;
                                                    if (!string.IsNullOrEmpty(displayName)) noteText += " | Type: " + displayName;
                                                    noteText += " | Notes: " + examNotes;

                                                    entities.Database.ExecuteSqlCommand(
                                                        @"INSERT INTO ITB_ApplicationNotes (ApplicationId, Office, NoteType, Notes)
                                                          VALUES (@p0, @p1, @p2, @p3)",
                                                        insertedApplicationId, "ADMSS", "EXMNOT", noteText);
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            LoggingManager.LogException(
                                                "Failed to insert PG Professional Exam Notes: " + ex.Message,
                                                ex.StackTrace, DateTime.Now, "ApplicationId: " + insertedApplicationId, "SubmitController");
                                        }
                                    }
                                    // ── END PG Professional Exam Notes ────────────────────────────

                                    // ── PG Bachelor Education → ApplicationEducation + Enrollment ─
                                    // PgBachelorUniversity (ID), PgBachelorDegree (DegreeId),
                                    // PgBachelorFieldOfStudy (free text), PgBachelorYearOfGrad (date)
                                    // Moved from UserDefined.
                                    if (applicationInfo.PostgraduateInfo != null && !string.IsNullOrEmpty(applicationInfo.PostgraduateInfo.PgBachelorUniversity))
                                    {
                                        var pgBach = applicationInfo.PostgraduateInfo;
                                        ObjectParameter pgEduId = new ObjectParameter("ApplicationEducationId", typeof(int));
                                        ObjectParameter pgEduEnrollId = new ObjectParameter("ApplicationEducationEnrollmentId", typeof(int));

                                        // Resolve bachelor university name from OrganizationId
                                        string bachelorUniName = pgBach.PgBachelorUniversityName ?? "";
                                        if (string.IsNullOrEmpty(bachelorUniName) && !string.IsNullOrEmpty(pgBach.PgBachelorUniversity))
                                        {
                                            int uniOrgId;
                                            if (int.TryParse(pgBach.PgBachelorUniversity, out uniOrgId))
                                            {
                                                var name = entities.Database.SqlQuery<string>(
                                                    "SELECT ORG_NAME_1 FROM ORGANIZATION WHERE OrganizationId = @p0", uniOrgId).FirstOrDefault();
                                                if (!string.IsNullOrEmpty(name)) bachelorUniName = name;
                                            }
                                        }

                                        // Insert education record
                                        entities.spInsApplicationEducation(pgEduId, insertedApplicationId,
                                            bachelorUniName, // InstitutionName (resolved from ORGANIZATION)
                                            null, null, null, null, null,
                                            string.Empty, // GPA
                                            pgBach.PgBachelorFieldOfStudy ?? "", // OtherInstitutionName (field of study)
                                            null, null, null, "1", string.Empty); // isTransfer=1 to distinguish from high school

                                        int pgEducationId = entities.ApplicationEducations.Max(p => p.ApplicationEducationId);
                                        if (pgEducationId > 0)
                                        {
                                            // Insert enrollment with degree and graduation date
                                            int? degreeId = null;
                                            int parsedDegree;
                                            if (int.TryParse(pgBach.PgBachelorDegree, out parsedDegree) && parsedDegree > 0)
                                                degreeId = parsedDegree;

                                            DateTime? gradDate = null;
                                            if (!string.IsNullOrEmpty(pgBach.PgBachelorYearOfGrad))
                                            {
                                                DateTime parsed;
                                                if (DateTime.TryParse(pgBach.PgBachelorYearOfGrad, out parsed))
                                                    gradDate = parsed;
                                            }

                                            entities.spInsApplicationEducationEnrollment(pgEduEnrollId, pgEducationId,
                                                null, null, gradDate, degreeId, null, null);
                                        }
                                    }
                                    // ── END PG Bachelor Education ─────────────────────────────────

                                    // ── PG Academic Awards → ITB_ApplicationNotes ────────────────
                                    // Each award with content gets a note row (Office=ADMSS, NoteType=AWDEC)
                                    // Removed from UserDefined — now stored in dedicated notes table.
                                    if (applicationInfo.PostgraduateInfo != null
                                        && "Yes".Equals(applicationInfo.PostgraduateInfo.PgHasAcademicAward, StringComparison.OrdinalIgnoreCase)
                                        && !string.IsNullOrEmpty(applicationInfo.PostgraduateInfo.PgAcademicAwards))
                                    {
                                        try
                                        {
                                            var awards = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(applicationInfo.PostgraduateInfo.PgAcademicAwards);
                                            int awardIndex = 0;
                                            foreach (var award in awards)
                                            {
                                                awardIndex++;
                                                string awardType = (string)(award.type ?? "");
                                                string institution = (string)(award.institution ?? "");
                                                string year = (string)(award.year ?? "");
                                                string description = (string)(award.description ?? "");

                                                if (!string.IsNullOrEmpty(description) || !string.IsNullOrEmpty(institution))
                                                {
                                                    string noteText = "Award " + awardIndex;
                                                    if (!string.IsNullOrEmpty(awardType)) noteText += " | Type: " + awardType;
                                                    if (!string.IsNullOrEmpty(institution)) noteText += " | Institution: " + institution;
                                                    if (!string.IsNullOrEmpty(year)) noteText += " | Year: " + year;
                                                    if (!string.IsNullOrEmpty(description)) noteText += " | Description: " + description;

                                                    entities.Database.ExecuteSqlCommand(
                                                        @"INSERT INTO ITB_ApplicationNotes (ApplicationId, Office, NoteType, Notes)
                                                          VALUES (@p0, @p1, @p2, @p3)",
                                                        insertedApplicationId, "ADMSS", "AWDEC", noteText);

                                                    // TODO: Re-enable ApplicationEducation insert once a unique DegreeId/CurriculumId
                                                    // is defined for awards (currently causes EDUCATION PK violation on accept
                                                    // because multiple "Other" rows share the same empty Degree/Curriculum key).
                                                    // ObjectParameter awardEduId = new ObjectParameter("ApplicationEducationId", typeof(int));
                                                    // entities.spInsApplicationEducation(awardEduId, insertedApplicationId,
                                                    //     "Other", null, null, null, null, null,
                                                    //     string.Empty, institution, null, null, null, "0", string.Empty);
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            LoggingManager.LogException(
                                                "Failed to parse/insert PG Academic Awards: " + ex.Message,
                                                ex.StackTrace, DateTime.Now, "ApplicationId: " + insertedApplicationId, "SubmitController");
                                        }
                                    }
                                    // ── END PG Academic Awards ────────────────────────────────────

                                    //Insert application attachments
                                    foreach (Submit attachment in lstRequest)
                                    {
                                        if (attachment != null && attachment.FileContent !=null && !string.IsNullOrEmpty(attachment.FileName) && !string.IsNullOrEmpty(attachment.FileExtension))
                                            Submit.InsertApplicationAttachment(entities, attachment, insertedApplicationId);
                                    }

                                    //Insert Application User Defined
                                    List<ApplicationUserDefinedInfo> lstUserDefined = applicationInfo.UserDefined ?? new List<ApplicationUserDefinedInfo>();

                                    // ──────────────────────────────────────────────────────────────
                                    // PG Postgraduate Data
                                    // Fields are mapped to dedicated tables where possible.
                                    // Remaining fields stay in ApplicationUserDefined.
                                    // ──────────────────────────────────────────────────────────────
                                    if (applicationInfo.PostgraduateInfo != null)
                                    {
                                        var pg = applicationInfo.PostgraduateInfo;

                                        // ── PG: Marital Status → Application.MaritalStatus (FK ID from frontend dropdown) ──
                                        // Moved from UserDefined. Frontend sends CODE_MARITALSTATUS.MaritalStatusId.
                                        if (!string.IsNullOrEmpty(pg.MaritalStatus))
                                        {
                                            int maritalId;
                                            if (int.TryParse(pg.MaritalStatus, out maritalId) && maritalId > 0)
                                            {
                                                entities.Database.ExecuteSqlCommand(
                                                    "UPDATE Application SET MaritalStatus = @p0 WHERE ApplicationId = @p1",
                                                    maritalId, insertedApplicationId);
                                            }
                                        }

                                        // ── PG: Emergency Contact → ApplicationEmergencyContact table ──
                                        // Moved from UserDefined. Frontend sends CODE_RELATIONSHIP.RelationTypeId.
                                        if (!string.IsNullOrEmpty(pg.PgEmergencyContactGivenName))
                                        {
                                            ObjectParameter pgEmergencyId = new ObjectParameter("ApplicationEmergencyContactId", typeof(int));
                                            int? relationTypeId = null;
                                            if (!string.IsNullOrEmpty(pg.PgEmergencyContactRelationship))
                                            {
                                                int relId;
                                                if (int.TryParse(pg.PgEmergencyContactRelationship, out relId))
                                                    relationTypeId = relId;
                                            }
                                            entities.spInsApplicationEmergencyContact(pgEmergencyId, insertedApplicationId,
                                                null, // prefix
                                                pg.PgEmergencyContactGivenName,
                                                pg.PgEmergencyContactMiddleName,
                                                null, // lastNamePrefix
                                                pg.PgEmergencyContactFamilyName,
                                                null, // suffix
                                                relationTypeId,
                                                pg.PgEmergencyContactMobile,
                                                pg.PgEmergencyContactEmail,
                                                null, // address
                                                null  // profession
                                            );
                                        }

                                        // -- PG: Military Status (stays in UserDefined — no dedicated table) --
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "MilitaryStatus", ColumnValue = pg.MilitaryStatus ?? "", ColumnType = 1, ColumnLabel = "MilitaryStatus", IsUploading = true, Description = "PostgraduateData" });

                                        // -- PG: Employment (EmployerCompanyName, EmployerPosition, EmployerStartDate moved to ApplicationEmployment table above) --
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "EmploymentStatus", ColumnValue = pg.EmploymentStatus ?? "", ColumnType = 1, ColumnLabel = "EmploymentStatus", IsUploading = true, Description = "PostgraduateData" });
                                        // EmployerDuties removed — stored in ApplicationEmployment.Remarks
                                        // EmployerIdNumber removed — stored in ApplicationEmployment.Remarks (TKH Staff)

                                        // -- PG: Coventry Alumni & English Proficiency --
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "CovAlumni", ColumnValue = pg.CovAlumni ?? "", ColumnType = 1, ColumnLabel = "CovAlumni", IsUploading = true, Description = "PostgraduateData" });
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "CoventryId", ColumnValue = pg.CoventryId ?? "", ColumnType = 1, ColumnLabel = "CoventryId", IsUploading = true, Description = "PostgraduateData" });
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "BachelorTaughtInEnglish", ColumnValue = pg.BachelorTaughtInEnglish ?? "", ColumnType = 1, ColumnLabel = "BachelorTaughtInEnglish", IsUploading = true, Description = "PostgraduateData" });
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "HasProfExam", ColumnValue = pg.HasProfExam ?? "", ColumnType = 1, ColumnLabel = "HasProfExam", IsUploading = true, Description = "PostgraduateData" });

                                        // -- PG: Bachelor Education (moved to ApplicationEducation + Enrollment above) --
                                        // PgSchoolName stays in UserDefined (no education table column for it)
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "PgSchoolName", ColumnValue = pg.PgSchoolName ?? "", ColumnType = 1, ColumnLabel = "PgSchoolName", IsUploading = true, Description = "PostgraduateData" });

                                        // -- PG: Academic Awards & Professional Exams --
                                        // GRE/GMAT scores moved to ApplicationTestScore above.
                                        // PgProfExamsData kept for "Other" exams (no CODE_TEST entry).
                                        // PgHasAcademicAward stays in UserDefined as a flag
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "PgHasAcademicAward", ColumnValue = pg.PgHasAcademicAward ?? "", ColumnType = 1, ColumnLabel = "PgHasAcademicAward", IsUploading = true, Description = "PostgraduateData" });
                                        // AcademicAwards and PgAcademicAwards moved to ITB_ApplicationNotes (above)
                                        // PgProfExamsData removed — GRE/GMAT in ApplicationTestScore, "Other" in ApplicationTestScore (OTHER code) + ITB_ApplicationNotes (EXMNOT)
                                        // -- PG: Academic Support --
                                        // Map AcademicSupport → DISABILITIES, AcademicSupportDetails → DISABILITIES_DESC
                                        // Override the existing DISABILITIES entry (sent as "False" from frontend XML)
                                        var disabilitiesEntry = lstUserDefined.FirstOrDefault(u => u.ColumnName == "DISABILITIES");
                                        if (disabilitiesEntry != null)
                                        {
                                            disabilitiesEntry.ColumnValue = pg.AcademicSupport ?? "No";
                                        }
                                        lstUserDefined.Add(new ApplicationUserDefinedInfo { ColumnName = "DISABILITIES_DESC", ColumnValue = pg.AcademicSupportDetails ?? "", ColumnType = 1, ColumnLabel = "DISABILITIES_DESC", IsUploading = true, Description = "DISABILITIES" });
                                    }
                                    // ──────────────────────────────────────────────────────────────

                                    if (lstUserDefined.Count > 0)
                                        Submit.InsertApplicationUserDefined(entities, lstUserDefined, insertedApplicationId);

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

