
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using E_Commerce_BackEnd.Models.UserRelatedModels;
using RandomDataGenerator.FieldOptions;
using RandomDataGenerator.Randomizers;

namespace E_Commerce_BackEnd.Services.Helpers.UserHelpers
{
    public class UserHelpers
    {
        private static readonly UserHelpers _userHelpers =  new UserHelpers();
        
        private UserHelpers()
        {
            
        }

        static UserHelpers()
        {
            
        }

        public static UserHelpers Instance
        {
            get => _userHelpers;
        }
        
        public string Token(int size, int size2, string userEmail)
        {
            DateTime dateTime = DateTime.Now;
            int seed = GenerateSeed(userEmail);
            Random generateRandom = new Random(seed);

            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            StringBuilder builder = new StringBuilder();

            
            for (int i = 0; i < size; i++)
            {
                int index = generateRandom.Next(chars.Length);
                builder.Append(chars[index]);
            }

            while (builder.Length < 60)
            {
                int nextNum = generateRandom.Next(size2);
                builder.Append(nextNum);
            }
            
           
            while (builder.Length < 100)
            {
                int index = generateRandom.Next(chars.Length);
                builder.Append(chars[index]);
                int nextNum = generateRandom.Next(size2);
                builder.Append(nextNum);
            }

             if (builder.Length > 100)
            {
                builder.Length = 100; 
            }

            return builder.ToString();
        }


        private int GenerateSeed(string userEmail)
        {
            using var sha256 = SHA256.Create();
            byte[] emailBytes = Encoding.UTF8.GetBytes(userEmail);
            byte[] hashBytes = sha256.ComputeHash(emailBytes);
            int hashInt = BitConverter.ToInt32(hashBytes, 0);
            return hashInt ^ DateTime.Now.Ticks.GetHashCode();
        }

        public string CryptPassword(string uncryptedPassword)
        {
            return BCrypt.Net.BCrypt.EnhancedHashPassword(uncryptedPassword, 13);
        }

        public bool VerifyCryptedPassword(string plainTextPassword, string cryptedPassword)
        {
            return BCrypt.Net.BCrypt.EnhancedVerify(plainTextPassword, cryptedPassword);
        }

        public string GenerateRandomPassword()
        {
            var randomizer = RandomizerFactory.GetRandomizer(new FieldOptionsText
            {
                UseUppercase = true,
                UseLowercase = true,
                UseNumber = true,
                UseSpecial = true,
                Min = 16,
                Max = 16
            });

            var randomPassword = randomizer.Generate();

            while (randomPassword == null)
            {
                randomPassword = randomizer.Generate();
            }
            
            return randomPassword;
        }
        
       
    }
}