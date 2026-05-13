using System;
using System.Security.Cryptography;
using System.Text;

namespace LashAccountingSystem.Security
{
    public static class PasswordHasher
    {
        // Генерация соли
        public static string GenerateSalt()
        {
            byte[] saltBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        // Хеширование пароля с солью
        public static string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                string saltedPassword = password + salt;
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
                return Convert.ToBase64String(bytes);
            }
        }

        // Проверка пароля
        public static bool VerifyPassword(string enteredPassword, string storedHash, string salt)
        {
            string hashOfEntered = HashPassword(enteredPassword, salt);
            return hashOfEntered == storedHash;
        }
    }
}