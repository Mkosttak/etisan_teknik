using Ganss.Xss;

namespace TeknikServisAPI.Utilities
{
    /// <summary>
    /// XSS saldırılarına karşı input temizleme utility class'ı
    /// Tüm kullanıcı girdileri bu class üzerinden sanitize edilmeli
    /// </summary>
    public static class InputSanitizer
    {
        private static readonly HtmlSanitizer _sanitizer = new();

        static InputSanitizer()
        {
            // Varsayılan ayarlar yeterli, gerekirse özelleştirilebilir:
            // _sanitizer.AllowedTags.Add("iframe"); // Örnek: iframe izin ver
            // _sanitizer.AllowedAttributes.Add("class"); // Örnek: class attribute izin ver
        }

        /// <summary>
        /// HTML içeriğini temizler, zararlı script ve tag'leri kaldırır
        /// </summary>
        /// <param name="input">Temizlenecek HTML içeriği</param>
        /// <returns>Temizlenmiş güvenli HTML içeriği</returns>
        public static string SanitizeHtml(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return input ?? string.Empty;

            return _sanitizer.Sanitize(input);
        }

        /// <summary>
        /// Tüm HTML tag'lerini kaldırarak düz metin döndürür
        /// </summary>
        /// <param name="input">Temizlenecek içerik</param>
        /// <returns>HTML tag'leri olmayan düz metin</returns>
        public static string StripHtmlTags(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return input ?? string.Empty;

            // Tüm tag'leri kaldır
            return System.Text.RegularExpressions.Regex.Replace(input, "<[^>]*>", string.Empty);
        }

        /// <summary>
        /// Script injection'a karşı string temizleme
        /// HTML encode yaparak tüm özel karakterleri güvenli hale getirir
        /// </summary>
        /// <param name="input">Temizlenecek string</param>
        /// <returns>HTML encoded güvenli string</returns>
        public static string HtmlEncode(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return input ?? string.Empty;

            return System.Net.WebUtility.HtmlEncode(input);
        }

        /// <summary>
        /// DTO alanlarını toplu temizleme (Description, Notes vb.)
        /// </summary>
        public static void SanitizeDescriptionFields<T>(T dto) where T : class
        {
            var type = typeof(T);
            var stringProperties = type.GetProperties()
                .Where(p => p.PropertyType == typeof(string) && p.CanRead && p.CanWrite);

            var descriptionFields = new[] { "Description", "Notes", "Comment", "Content", "Message", "Remarks" };

            foreach (var prop in stringProperties)
            {
                if (descriptionFields.Any(f => prop.Name.Contains(f, StringComparison.OrdinalIgnoreCase)))
                {
                    var value = (string?)prop.GetValue(dto);
                    if (!string.IsNullOrEmpty(value))
                    {
                        prop.SetValue(dto, SanitizeHtml(value));
                    }
                }
            }
        }
    }
}
