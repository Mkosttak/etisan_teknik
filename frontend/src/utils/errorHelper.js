/**
 * Backendden gelen hata objelerini (özellikle ASP.NET Core ProblemDetails) 
 * kullanıcıya gösterilebilir anlamlı string mesajlara dönüştüren yardımcı fonksiyon.
 * 
 * @param {Error} error - Axios veya standart Error objesi
 * @returns {string} - Kullanıcıya gösterilecek hata mesajı
 */
export const getErrorMessage = (error) => {
    // 1. Axios Error kontrolü
    if (error?.response?.data) {
        const data = error.response.data;

        // ASP.NET Core Identity veya Custom API Error ( { message: "..." } )
        if (data.message) return data.message;

        // ASP.NET Core ProblemDetails ( { title: "...", status: 400, ... } )
        if (data.title) {
            // Eğer detaylı validation hataları varsa ( { errors: { Field: ["error"] } } )
            if (data.errors) {
                const firstErrorField = Object.keys(data.errors)[0];
                const firstErrorMessage = data.errors[firstErrorField][0];
                return `${data.title}: ${firstErrorMessage}`;
            }
            return data.title;
        }

        // Eğer data direkt string ise
        if (typeof data === 'string') return data;
    }

    // 2. Standart Error message
    if (error.message) return error.message;

    // 3. Fallback
    return 'İşlem sırasında beklenmedik bir hata oluştu.';
};
