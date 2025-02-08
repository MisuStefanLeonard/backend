
using System.Security.Cryptography;
using System.Text;
using RandomDataGenerator.FieldOptions;
using RandomDataGenerator.Randomizers;

namespace E_Commerce_BackEnd.Services.Helpers.UserHelpers
{
    public class UserHelpers
    {
        private const decimal RONtoEUR = (decimal)0.2;
        private const decimal EURtoRON = 5;
        private const string Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        // private UserHelpers()
        // {
        //     
        // }
        //
        // static UserHelpers()
        // {
        //     
        // }
        //
        // public static UserHelpers Instance { get; } = new ();

        public static string Token(int size, int size2, string userEmail)
        {
            var seed = GenerateSeed(userEmail);
            var generateRandom = new Random(seed);

           
            var builder = new StringBuilder();

            
            for (var i = 0; i < size; i++)
            {
                var index = generateRandom.Next(Chars.Length);
                builder.Append(Chars[index]);
            }

            while (builder.Length < 60)
            {
                var nextNum = generateRandom.Next(size2);
                builder.Append(nextNum);
            }
            
           
            while (builder.Length < 100)
            {
                var index = generateRandom.Next(Chars.Length);
                builder.Append(Chars[index]);
                var nextNum = generateRandom.Next(size2);
                builder.Append(nextNum);
            }

            if (builder.Length > 100)
            {
                builder.Length = 100; 
            }

            return builder.ToString();
        }

        public static string GenerateRandomToken_50_Length()
        {
            var token = new StringBuilder();
            token.Append(Guid.NewGuid().ToString("N"));
            var random = new Random();
            while (token.Length < 50)
            {
                var chooseRandomIndexFromChars = random.Next(Chars.Length);
                token.Append(Chars[chooseRandomIndexFromChars]);
            }

            return token.ToString();
        }


        private static int GenerateSeed(string userEmail)
        {
            using var sha256 = SHA256.Create();
            var emailBytes = Encoding.UTF8.GetBytes(userEmail);
            var hashBytes = sha256.ComputeHash(emailBytes);
            var hashInt = BitConverter.ToInt32(hashBytes, 0);
            return hashInt ^ DateTime.Now.Ticks.GetHashCode();
        }

        public static string CryptPassword(string uncryptedPassword)
        {
            return BCrypt.Net.BCrypt.EnhancedHashPassword(uncryptedPassword, 13);
        }

        public static bool VerifyCryptedPassword(string plainTextPassword, string cryptedPassword)
        {
            return BCrypt.Net.BCrypt.EnhancedVerify(plainTextPassword, cryptedPassword);
        }
        

        /// <summary>
        /// Conversion method from RON to EUR
        /// Conversion method from EUR to RON
        /// </summary>
        /// <param name="currency1">First currency</param>
        /// <param name="currency2">Second currency </param>
        /// <param name="value1">First currency value</param>
        /// <param name="value2">Second currency value</param>
        /// <returns>The conversed value (double)</returns>>
        /// <returns></returns>
        public static decimal ConvertCurrency(string currency1, string currency2 , decimal value1 , decimal value2)
        {
            return currency1 switch
            {
                "RON" when currency2 == "EUR" => value1 * RONtoEUR,
                "EUR" when currency2 == "RON" => value2 * EURtoRON,
                _ => 0
            };
        }
        
        
       
    }
}