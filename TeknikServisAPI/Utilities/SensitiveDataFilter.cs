using System.Text.RegularExpressions;

namespace TeknikServisAPI.Utilities
{
    /// <summary>
    /// KVKK/GDPR uyumluluğu için hassas verileri loglardan temizler
    /// Production ortamında kişisel verilerin loglanmasını önler
    /// </summary>
    public static class SensitiveDataFilter
    {
        /// <summary>
        /// Hassas verileri maskeleyerek güvenli log mesajı oluşturur
        /// </summary>
        public static string Sanitize(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            
            var sanitized = text;
            
            // 1. ŞİFRELERİ GİZLE (çeşitli formatlar)
            sanitized = Regex.Replace(sanitized, 
                @"password[\""\s]*[:=][\""\s]*([^,\};\n""]+)", 
                "password=***", 
                RegexOptions.IgnoreCase);
            
            sanitized = Regex.Replace(sanitized,
                @"(pwd|pass)[\""\s]*[:=][\""\s]*([^,\};\n""]+)",
                "$1=***",
                RegexOptions.IgnoreCase);
            
            // 2. JWT TOKEN'LARI GİZLE
            sanitized = Regex.Replace(sanitized,
                @"Bearer\s+[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]*",
                "Bearer ***");
            
            sanitized = Regex.Replace(sanitized,
                @"token[\""\s]*[:=][\""\s]*[A-Za-z0-9\-_\.]+",
                "token=***",
                RegexOptions.IgnoreCase);
            
            // 3. EMAIL ADRESLERİNİ KISMİ GİZLE (KVKK uyumlu)
            // ornek@domain.com -> or***@domain.com
            sanitized = Regex.Replace(sanitized,
                @"([a-zA-Z0-9]{2})[a-zA-Z0-9._%+-]*@([a-zA-Z0-9.-]+\.[a-zA-Z]{2,})",
                "$1***@$2");
            
            // 4. TELEFON NUMARALARINI GİZLE
            // +905551234567 -> +9055***4567
            // 05551234567 -> 055***4567
            sanitized = Regex.Replace(sanitized,
                @"(\+90|0)?(\d{3})(\d{3})(\d{2})(\d{2})",
                "$1$2***$4$5");
            
            // 5. KREDİ KARTI NUMARALARINI GİZLE (varsa)
            sanitized = Regex.Replace(sanitized,
                @"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b",
                "****-****-****-****");
            
            // 6. TC KİMLİK NUMARALARINI GİZLE
            sanitized = Regex.Replace(sanitized,
                @"\b[1-9]\d{10}\b",
                "***********");
            
            // 7. CONNECTION STRING'LERİ GİZLE
            sanitized = Regex.Replace(sanitized,
                @"(server|host|database|user id|password)\s*=\s*[^;]+",
                "$1=***",
                RegexOptions.IgnoreCase);
            
            // 8. SECRET KEY'LERİ GİZLE
            sanitized = Regex.Replace(sanitized,
                @"(secret|apikey|api_key)[\""\s]*[:=][\""\s]*([^,\};\n""]+)",
                "$1=***",
                RegexOptions.IgnoreCase);
            
            return sanitized;
        }
        
        /// <summary>
        /// Exception mesajlarını sanitize eder
        /// Stack trace'lerde hassas veri varsa temizler
        /// </summary>
        public static string SanitizeException(Exception ex)
        {
            if (ex == null)
                return string.Empty;
            
            var exceptionText = ex.ToString();
            return Sanitize(exceptionText);
        }
        
        /// <summary>
        /// SQL query'leri için özel sanitization
        /// Parametre değerlerini gizler
        /// </summary>
        public static string SanitizeSqlQuery(string query)
        {
            if (string.IsNullOrEmpty(query))
                return query;
            
            // SQL parametrelerindeki değerleri gizle
            var sanitized = Regex.Replace(query,
                @"@\w+\s*=\s*'[^']+'",
                "@param=***");
            
            return sanitized;
        }
    }
}
