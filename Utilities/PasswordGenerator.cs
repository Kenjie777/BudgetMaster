using System.Security.Cryptography;
using System.Text;

namespace BudgetMasterFinal.Utilities
{
    public static class PasswordGenerator
    {
        private const string UppercaseChars = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // Removed I, O for clarity
        private const string LowercaseChars = "abcdefghijkmnopqrstuvwxyz"; // Removed l for clarity
        private const string DigitChars = "23456789"; // Removed 0, 1 for clarity
        private const string SpecialChars = "!@#$%^&*";

        /// <summary>
        /// Generates a secure random temporary password
        /// </summary>
        /// <param name="length">Length of the password (minimum 12)</param>
        /// <returns>A secure random password</returns>
        public static string GenerateTemporaryPassword(int length = 16)
        {
            if (length < 12)
                length = 12;

            var password = new StringBuilder();
            
            // Ensure at least one character from each category
            password.Append(GetRandomChar(UppercaseChars));
            password.Append(GetRandomChar(LowercaseChars));
            password.Append(GetRandomChar(DigitChars));
            password.Append(GetRandomChar(SpecialChars));

            // Fill the rest with random characters from all categories
            var allChars = UppercaseChars + LowercaseChars + DigitChars + SpecialChars;
            for (int i = 4; i < length; i++)
            {
                password.Append(GetRandomChar(allChars));
            }

            // Shuffle the password to avoid predictable patterns
            return ShuffleString(password.ToString());
        }

        private static char GetRandomChar(string chars)
        {
            var randomIndex = RandomNumberGenerator.GetInt32(0, chars.Length);
            return chars[randomIndex];
        }

        private static string ShuffleString(string input)
        {
            var array = input.ToCharArray();
            int n = array.Length;
            
            for (int i = n - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(0, i + 1);
                // Swap
                var temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }
            
            return new string(array);
        }
    }
}
