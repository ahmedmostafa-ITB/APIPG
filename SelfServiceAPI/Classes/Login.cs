using DataAccess;
using System;
using System.Linq;

namespace SelfServiceAPI.Classes
{
    public class Login
    {
        public string Email { get; set; }

        public string Password { get; set; }

        public static bool UserLogin(string email, string password)
        {
            using (PowerCampusIdentityEntities pcEntities = new PowerCampusIdentityEntities())
            {
                string encrypyrdPassword = EncryptDecryptText.Encryptword(password);

                return pcEntities.IdentityUsers.Any(user => user.Email.Equals(email, StringComparison.CurrentCultureIgnoreCase) && user.Password == encrypyrdPassword);
            }
        }

        public static bool ApplicationSubmitted(string email)
        {
            using (ApplicationFormEntities entities = new ApplicationFormEntities())
            {
                return entities.Applications.Any(user => user.Email.Equals(email, StringComparison.CurrentCultureIgnoreCase));
            }
        }
    }

    public class OnLogin
    {
        public int IncompleteApplicationId { get; set; }
        public Guid Token { get; set; }
    }

}