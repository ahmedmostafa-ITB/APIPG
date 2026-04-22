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
        private static string BuildSignature()
        {
            return @"
                <p style='margin-top:24px;'>
                    <b>Sincerely,</b><br/><br/>
                    <b>The Knowledge Hub Universities &mdash; Admissions Office</b><br/>
                    <b>19940 | admissions@tkh.edu.eg | New Administrative Capital, Residential Area 7, R7</b>
                </p>
                <p style='color:#888;font-size:12px;margin-top:16px;'>
                    (THIS IS AN AUTOMATED MESSAGE - PLEASE DO NOT REPLY DIRECTLY TO THIS EMAIL)
                </p>";
        }

        public static void ProcessSendEmail(string toEmail, string password, string firstName = "", string lastName = "")
        {
            try
            {
                string smtpValue = ConfigurationManager.AppSettings["Smtp"];
                string smtpPort = ConfigurationManager.AppSettings["Port"];
                string smtpEnableSSl = ConfigurationManager.AppSettings["EnableSSL"];
                string smtpUserName = ConfigurationManager.AppSettings["UserName"];
                string smtpPassword = ConfigurationManager.AppSettings["Password"];
                string smtpFromEmail = ConfigurationManager.AppSettings["FromEmail"];

                string greeting = !string.IsNullOrEmpty(firstName) ? firstName + " " + lastName : toEmail;
                string htmlBody = @"
                    <html>
                    <head><meta charset='utf-8'/></head>
                    <body style='font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#333;line-height:1.6;'>
                        <p>Dear " + greeting.Trim() + @",</p>
                        <p>The password for your application account has been reset.</p>
                        <p>Your new password is: <b style='font-size:16px;'>" + password + @"</b></p>
                        <p>The password is case-sensitive, so make sure you enter it exactly as shown. Use this password to log into the application.</p>"
                        + BuildSignature() + @"
                    </body>
                    </html>";

                AlternateView htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, "text/html");

                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(smtpFromEmail);
                    mail.Subject = "Reset Password";
                    mail.IsBodyHtml = true;
                    mail.BodyEncoding = Encoding.UTF8;
                    mail.SubjectEncoding = Encoding.UTF8;
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

                string fullName = string.Format("{0} {1}", personnelInfo.FirstName, personnelInfo.LastName);
                string verifyLink = verificationAccountURL + "?Id=" + EncryptDecrypt.Encrypt(incompleteApplicationId.ToString());

                string htmlBody = @"
                    <html>
                    <head><meta charset='utf-8'/></head>
                    <body style='font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#333;line-height:1.6;'>
                        <p>Dear " + fullName + @",</p>
                        <p><a href='" + verifyLink + @"' style='text-decoration:underline;text-transform:uppercase;font-weight:bold;color:#3168CE;'>CLICK HERE</a> to verify your account.</p>
                        <p>Kindly note that this step is mandatory in order to complete your application.</p>"
                        + BuildSignature() + @"
                    </body>
                    </html>";

                AlternateView htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, "text/html");

                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(smtpFromEmail);
                    mail.Subject = "Account Verification";
                    mail.IsBodyHtml = true;
                    mail.BodyEncoding = Encoding.UTF8;
                    mail.SubjectEncoding = Encoding.UTF8;
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
