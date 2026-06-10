using System.Text.RegularExpressions;

namespace TeknikServisAPI.Validators
{
    /// <summary>
    /// Parola güvenlik politikası validator
    /// Production-grade password security için OWASP önerileri uygulanmıştır
    /// </summary>
    public static class PasswordValidator
    {
        public const int MinLength = 12;
        
        private static readonly string[] CommonPasswords = new[]
        {
            "password123!",
            "admin123!",
            "qwerty123!",
            "welcome123!",
            "changeme123!",
            "letmein123!",
            "p@ssw0rd",
            "p@ssword123",
            "admin@123",
            "root@123"
        };
        
        /// <summary>
        /// Şifre güvenlik kontrolü yapar
        /// </summary>
        /// <param name="password">Kontrol edilecek şifre</param>
        /// <returns>Geçerlilik durumu ve hata mesajları</returns>
        public static (bool IsValid, List<string> Errors) Validate(string password)
        {
            var errors = new List<string>();
            
            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Şifre boş olamaz");
                return (false, errors);
            }
            
            // Uzunluk kontrolü
            if (password.Length < MinLength)
            {
                errors.Add($"Şifre en az {MinLength} karakter olmalı");
            }
            
            // Büyük harf kontrolü
            if (!password.Any(char.IsUpper))
            {
                errors.Add("En az 1 büyük harf içermeli (A-Z)");
            }
            
            // Küçük harf kontrolü
            if (!password.Any(char.IsLower))
            {
                errors.Add("En az 1 küçük harf içermeli (a-z)");
            }
            
            // Rakam kontrolü
            if (!password.Any(char.IsDigit))
            {
                errors.Add("En az 1 rakam içermeli (0-9)");
            }
            
            // Özel karakter kontrolü
            if (!password.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c)))
            {
                errors.Add("En az 1 özel karakter içermeli (!@#$%^&*()_+-=[]{}|;:,.<>?)");
            }
            
            // Yaygın şifre kontrolü
            if (CommonPasswords.Any(cp => password.Equals(cp, StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add("Bu şifre çok yaygın kullanılıyor, lütfen daha güçlü bir şifre seçin");
            }
            
            // Tekrarlayan karakterler (aaa, 111 gibi)
            if (Regex.IsMatch(password, @"(.)\1{2,}"))
            {
                errors.Add("Şifrede 3 veya daha fazla tekrarlayan karakter olmamalı");
            }
            
            // Ardışık karakterler kontrolü (abc, 123 gibi)
            if (HasSequentialChars(password))
            {
                errors.Add("Şifrede ardışık karakterler (abc, 123) kullanmayın");
            }
            
            return (errors.Count == 0, errors);
        }
        
        /// <summary>
        /// Ardışık karakter kontrolü (abc, 123, xyz gibi)
        /// </summary>
        private static bool HasSequentialChars(string password)
        {
            for (int i = 0; i < password.Length - 2; i++)
            {
                var char1 = password[i];
                var char2 = password[i + 1];
                var char3 = password[i + 2];
                
                // Sadece harf ve rakamlar için kontrol et
                if ((char.IsLetterOrDigit(char1) && char.IsLetterOrDigit(char2) && char.IsLetterOrDigit(char3)))
                {
                    // Artan sıra kontrol (abc, 123)
                    if (char2 == char1 + 1 && char3 == char2 + 1)
                        return true;
                    
                    // Azalan sıra kontrolü (cba, 321)
                    if (char2 == char1 - 1 && char3 == char2 - 1)
                        return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Kullanıcı dostu hata mesajı oluşturur
        /// </summary>
        public static string GetUserFriendlyErrorMessage(List<string> errors)
        {
            if (errors.Count == 0)
                return string.Empty;
            
            return "Şifre gereksinimleri:\n" + string.Join("\n• ", errors.Prepend(""));
        }
    }
}
