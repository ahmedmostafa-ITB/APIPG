using SelfServiceAPI.Helper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Web;
using System.Web.Http;

namespace SelfServiceAPI.Classes
{
    public static class SendEmail
    {
        public static void ProcessSendEmail(string toEmail, string password)
        {
            try
            {
                string smtpValue = ConfigurationManager.AppSettings["Smtp"];
                string smtpPort = ConfigurationManager.AppSettings["Port"];
                string smtpEnableSSl = ConfigurationManager.AppSettings["EnableSSL"];
                string smtpUserName = ConfigurationManager.AppSettings["UserName"];
                string smtpPassword = ConfigurationManager.AppSettings["Password"];
                string smtpFromEmail = ConfigurationManager.AppSettings["FromEmail"];

                string htmlBody = "Dear " + toEmail + "<br><br/>" + "The password for your application account has been reset." + "<br><br/>" + "Your new password is: " + password + "<br><br/>"
                                    + "The password is case-sensitive, so make sure you enter it exactly as shown. Use this password to log into the application.";
                                    //+ "<br><br/>"
                                    //+ "If you did not initiate this password reset, please contact the System Administrator immediately.";

                AlternateView htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, new ContentType("text/html"));

                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(smtpFromEmail);
                    mail.Subject = "Reset Password";
                    mail.IsBodyHtml = true;
                    mail.To.Add(toEmail);
                    mail.Body = htmlBody;
                    mail.AlternateViews.Add(htmlView);

                    using (SmtpClient smtp = new SmtpClient(smtpValue, Convert.ToInt32(smtpPort)))
                    {
                        smtp.EnableSsl = Convert.ToBoolean(smtpEnableSSl);
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new NetworkCredential(smtpUserName, smtpPassword);
                        smtp.Send(mail);
                    };
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static void EmailValidation(PersonalInfo personnelInfo, string toEmail, int incompleteApplicationId)
        {
            try
            {
                string smtpValue = ConfigurationManager.AppSettings["Smtp"];
                string smtpPort = ConfigurationManager.AppSettings["Port"];
                string smtpEnableSSl = ConfigurationManager.AppSettings["EnableSSL"];
                string smtpUserName = ConfigurationManager.AppSettings["UserName"];
                string smtpPassword = ConfigurationManager.AppSettings["Password"];
                string smtpFromEmail = ConfigurationManager.AppSettings["FromEmail"];
                string verificationAccountURL = ConfigurationManager.AppSettings["VerificationURLAccount"];


                string htmlBody = "Dear " + string.Format("{0} {1}",personnelInfo.FirstName , personnelInfo.LastName) + "<br><br/>"
                                    + "Click  <a href=" + verificationAccountURL  + "?Id=" + EncryptDecrypt.Encrypt(incompleteApplicationId.ToString())  + "> here </a> to verify your account";

               
                AlternateView htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, new ContentType("text/html"));

                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(smtpFromEmail);
                    mail.Subject = "Account Verification";
                    mail.IsBodyHtml = true;
                    mail.To.Add(toEmail);
                    mail.Body = htmlBody;
                    mail.AlternateViews.Add(htmlView);

                    using (SmtpClient smtp = new SmtpClient(smtpValue, Convert.ToInt32(smtpPort)))
                    {
                        smtp.EnableSsl = Convert.ToBoolean(smtpEnableSSl);
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new NetworkCredential(smtpUserName, smtpPassword);
                        smtp.Send(mail);
                    };
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}