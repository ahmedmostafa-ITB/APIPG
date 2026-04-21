using DataAccess;
using SelfServiceAPI.Classes;
using SelfServiceAPI.Helper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SelfServiceAPI
{
    public partial class EmailVerification : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            int emailValidityTime = Convert.ToInt32(ConfigurationManager.AppSettings["EmailValidityTimeInHrs"]);

            DateTime currentDateTime = DateTime.Now;

            string incompleteApplicationId = string.Empty; ;

            string strDateTime = string.Empty;

            if (!string.IsNullOrEmpty(HttpContext.Current.Request.QueryString["Id"]))
            {
                incompleteApplicationId = EncryptDecrypt.Decrypt(HttpContext.Current.Request.QueryString["Id"]);
            }

            try
            {
                if (!string.IsNullOrEmpty(incompleteApplicationId))
                {

                    int incApplicationId = Convert.ToInt32(incompleteApplicationId);

                    using (ApplicationFormEntities entities = new ApplicationFormEntities())
                    {

                        IncompleteApplication incompleteApplication = entities.IncompleteApplications.FirstOrDefault(app => app.IncompleteApplicationId == incApplicationId);

                        if (incompleteApplication != null)
                        {
                            DateTime incompleteApplicationDateTime = incompleteApplication.CreateDatetime;

                            double ts = (currentDateTime - incompleteApplicationDateTime).TotalHours;

                            if (ts < emailValidityTime)
                            {
                                entities.ITB_EmailVerified(incApplicationId);
                                lblMsg.Visible = true;
                                lblMsg.Text = "Your email has been verified.";
                                redirection.Visible = true;
                                appLink.HRef = ConfigurationManager.AppSettings["ApplicationLink"].ToString();
                            }
                            else
                            {
                                lblMsg.Visible = true;
                                lblMsg.Text = string.Format("Your verification link has been expired, please visit <a href={0}>{1}</a> to create a new application, you can use the same email you have used before to create your application.",ConfigurationManager.AppSettings["ApplicationLink"], ConfigurationManager.AppSettings["ApplicationLink"]);
                                //Delete the application in order to allow new application application with the same email.
                                entities.IncompleteApplications.Remove(incompleteApplication);
                                entities.SaveChanges();

                                // Delete from the identity database
                                using (PowerCampusIdentityEntities pcIdentity = new PowerCampusIdentityEntities())
                                {
                                    IdentityUser identityUser = pcIdentity.IdentityUsers.FirstOrDefault(email => email.Email == incompleteApplication.Email);

                                    if (identityUser != null)
                                    {
                                        pcIdentity.IdentityUsers.Remove(identityUser);
                                        pcIdentity.SaveChanges();
                                    }
                                }


                            }
                        }
                        else
                        {
                            lblMsg.Visible = true;
                            //lblMsg.Text = "Your account has been already verified, click here to access your application.";
                            lblMsg.Text = string.Format("Your account has been already verified,<a href={0}>{1}</a> to access your application.", ConfigurationManager.AppSettings["ApplicationLink"], " click here");
                        }
                    }
                }
                else
                {
                    lblMsg.Visible = true;
                    lblMsg.Text = "You have reached this page is a wrong way.";
                }
            }

            catch (Exception exception)
            {
                LoggingManager.LogException(exception.Message, exception.StackTrace, DateTime.Now, string.Empty, exception.Source);
            }
        }
    }
}