using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SelfServiceAPI.Classes
{
    public static class PasswordGenerator
    {
        public static string GeneratePassword(int lowercase, int uppercase, int numerics , int specialCharacter)
        {
            string lowers = "abcdefghijklmnopqrstuvwxyz";
            string uppers = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string number = "0123456789";
            string special = "@!#$%^&*.-_";

            Random random = new Random();

            string generated = string.Empty;
            for (int i = 1; i <= lowercase; i++)
                generated = generated.Insert(
                    random.Next(generated.Length),
                    lowers[random.Next(lowers.Length - 1)].ToString()
                );

            for (int i = 1; i <= uppercase; i++)
                generated = generated.Insert(
                    random.Next(generated.Length),
                    uppers[random.Next(uppers.Length - 1)].ToString()
                );

            for (int i = 1; i <= numerics; i++)
                generated = generated.Insert(
                    random.Next(generated.Length),
                    number[random.Next(number.Length - 1)].ToString()
                );

            for (int i = 1; i <= specialCharacter; i++)
                generated = generated.Insert(
                    random.Next(generated.Length),
                    special[random.Next(special.Length - 1)].ToString()
                );
            return generated;
           // return generated.Replace("!", string.Empty);

        }
    }
}