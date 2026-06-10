namespace TeknikServisAPI.Constants
{
    /// <summary>
    /// Canonical rol listesi - TÜM katmanlarda bu değerler kullanılacak
    /// Backend, validator, frontend ve authorization handler'lar bu sabitleri kullanmalıdır
    /// </summary>
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Proje = "Proje";
        public const string Teknik = "Teknik";
        public const string SahaElemani = "SahaElemani";
        public const string Kullanici = "Kullanici";
        
        /// <summary>
        /// İzinli roller - Talep sahipliği kontrolünden muaf roller
        /// RequestOwnerHandler bu listeyi kullanarak ayrıcalıklı kullanıcıları belirler
        /// </summary>
        public static readonly string[] PrivilegedRoles = new[] 
        { 
            Admin, 
            Proje,
            Teknik,
            Kullanici
        };
        
        /// <summary>
        /// Tüm geçerli roller - Validasyon için kullanılır
        /// Bilinmeyen roller en düşük yetkiyle sınırlanır
        /// </summary>
        public static readonly string[] AllRoles = new[] 
        { 
            Admin, 
            Proje, 
            Teknik, 
            SahaElemani, 
            Kullanici 
        };
        public const string AdminOrTeknik = "Admin,Teknik";
        public const string DashboardRoles = "Admin,Teknik,Proje,Kullanici";
    }

    public static class AuthorizationPolicies
    {
        public const string RequestOwnerOrPrivileged = "RequestOwnerOrPrivileged";
    }

    public static class RequestStatuses
    {
        public const string Pending = "Pending";
        public const string InProgress = "InProgress";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
    }

    public static class FaultStatuses
    {
        public const string Pending = "Pending";
        public const string Created = "Created";
        public const string ReceivedByTechnician = "ReceivedByTechnician";
        public const string InProgress = "InProgress";
        public const string WaitingForParts = "WaitingForParts";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
        public const string Delivered = "Delivered";
        public const string SentToService = "SentToService";
        public const string SentToCustomer = "SentToCustomer";
        public const string ShippingToManufacturer = "ShippingToManufacturer";
        public const string ShippingToCustomer = "ShippingToCustomer";
    }

    public static class ServiceFormStatuses
    {
        public const string Draft = "DRAFT";
        public const string Submitted = "SUBMITTED";
    }

    public static class ShipmentStatuses
    {
        public const string Preparing = "Preparing";
        public const string Shipped = "Shipped";
        public const string Delivered = "Delivered";
        public const string Cancelled = "Cancelled";
    }

    public static class ShipmentTypes
    {
        public const string ToCustomer = "ToCustomer";
        public const string ToManufacturer = "ToManufacturer";
        public const string FromManufacturer = "FromManufacturer";
    }

    public static class StockMovementTypes
    {
        public const string In = "In";
        public const string Out = "Out";
        public const string Adjustment = "Adjustment";
    }

    public static class DeviceStatuses
    {
        public const string Available = "Available";
        public const string InUse = "InUse";
        public const string InRepair = "InRepair";
        public const string AtManufacturer = "AtManufacturer";
        public const string UnderMaintenance = "UnderMaintenance";
        public const string Retired = "Retired";
    }
}
