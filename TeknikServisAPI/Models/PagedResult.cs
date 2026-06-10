namespace TeknikServisAPI.Models
{
    /// <summary>
    /// Generic paged result for server-side pagination
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        
        /// <summary>
        /// Current page number (1-indexed)
        /// </summary>
        public int Page { get; set; }
        
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        
        /// <summary>
        /// Backward compatibility - same as Page
        /// </summary>
        public int PageNumber 
        { 
            get => Page; 
            set => Page = value; 
        }
        
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
        
        /// <summary>
        /// Backward compatibility
        /// </summary>
        public bool HasPreviousPage => HasPrevious;
        
        /// <summary>
        /// Backward compatibility
        /// </summary>
        public bool HasNextPage => HasNext;
    }

    public class PaginationParams
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 10;

        public int PageNumber { get; set; } = 1;
        
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; } = "asc";
    }

    public class FaultFilterParams : PaginationParams
    {
        public string? Status { get; set; }
        public int? DeviceId { get; set; }
        public int? AssignedToUserId { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
    }

    public class DeviceFilterParams : PaginationParams
    {
        public string? Status { get; set; }
        public bool? IsSpareDevice { get; set; }
        public string? University { get; set; }
    }

    public class RequestFilterParams : PaginationParams
    {
        public string? Status { get; set; }
        public int? DeviceId { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
    }
}





