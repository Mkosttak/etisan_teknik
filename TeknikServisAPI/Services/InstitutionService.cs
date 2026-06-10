using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeknikServisAPI.Constants;
using TeknikServisAPI.Data;
using TeknikServisAPI.DTOs;
using TeknikServisAPI.Models;

namespace TeknikServisAPI.Services
{
    public class InstitutionService : IInstitutionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InstitutionService> _logger;

        public InstitutionService(ApplicationDbContext context, ILogger<InstitutionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<InstitutionSummaryDto> GetSummaryAsync(string institutionName)
        {
            var decodedName = Uri.UnescapeDataString(institutionName);

            var summary = new InstitutionSummaryDto
            {
                Name = decodedName
            };

            var requestsQuery = _context.Requests
                .AsNoTracking()
                .Where(r => (r.InstitutionName == decodedName) || (r.InstitutionName == "DİĞER" && r.InstitutionOtherName == decodedName));
            
            summary.TotalRequests = await requestsQuery.CountAsync();
            summary.PendingRequests = await requestsQuery.CountAsync(r => r.Status == RequestStatuses.Pending);
            summary.InProgressRequests = await requestsQuery.CountAsync(r => r.Status == RequestStatuses.InProgress);
            summary.CompletedRequests = await requestsQuery.CountAsync(r => r.Status == RequestStatuses.Completed);

            var faultsQuery = _context.Faults
                .AsNoTracking()
                .Where(f => (f.InstitutionName == decodedName) || (f.InstitutionName == "DİĞER" && f.InstitutionOtherName == decodedName));
            
            summary.TotalFaults = await faultsQuery.CountAsync();
            summary.PendingFaults = await faultsQuery.CountAsync(f => f.Status == FaultStatuses.Pending || f.Status == FaultStatuses.Created);
            summary.InProgressFaults = await faultsQuery.CountAsync(f => f.Status == FaultStatuses.InProgress);
            summary.CompletedFaults = await faultsQuery.CountAsync(f => f.Status == FaultStatuses.Completed || f.Status == FaultStatuses.Delivered);

            var serviceFormsQuery = _context.ServiceForms
                .AsNoTracking()
                .Where(sf => (sf.InstitutionName == decodedName) || (sf.InstitutionName == "DİĞER" && sf.InstitutionOtherName == decodedName));
            
            summary.TotalServiceForms = await serviceFormsQuery.CountAsync();
            summary.DraftServiceForms = await serviceFormsQuery.CountAsync(sf => sf.Status == ServiceFormStatuses.Draft);
            summary.SubmittedServiceForms = await serviceFormsQuery.CountAsync(sf => sf.Status == ServiceFormStatuses.Submitted);

            var workItemsQuery = _context.WorkItems
                .AsNoTracking()
                .Where(wi => (wi.InstitutionName == decodedName) || (wi.InstitutionName == "DİĞER" && wi.InstitutionOtherName == decodedName));
            
            summary.TotalWorkItems = await workItemsQuery.CountAsync();
            summary.BacklogWorkItems = await workItemsQuery.CountAsync(wi => wi.Stage == WorkItemStage.Backlog || wi.Stage == (WorkItemStage)1);
            summary.DoneWorkItems = await workItemsQuery.CountAsync(wi => wi.Stage == WorkItemStage.Done);

            var stockMovementsQuery = _context.StockMovements
                .AsNoTracking()
                .Where(sm => (sm.InstitutionName == decodedName) && sm.MovementType == "Out");
            
            summary.TotalStockMovements = await stockMovementsQuery.CountAsync();
            summary.UniqueItemsUsed = await stockMovementsQuery.Select(sm => sm.StockItemId).Distinct().CountAsync();
            summary.TotalStockValue = await stockMovementsQuery.SumAsync(sm => sm.Quantity * (sm.StockItem != null ? (sm.StockItem.UnitPrice ?? 0m) : 0m));

            return summary;
        }

        public async Task<List<RequestDto>> GetRequestsAsync(string institutionName, int page = 1, int pageSize = 50)
        {
            var decodedName = Uri.UnescapeDataString(institutionName);

            return await _context.Requests
                .AsNoTracking()
                .Include(r => r.RequestStockItems)
                    .ThenInclude(rsi => rsi.StockItem)
                .Where(r => (r.InstitutionName == decodedName) || (r.InstitutionName == "DİĞER" && r.InstitutionOtherName == decodedName))
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new RequestDto
                {
                    Id = r.Id,
                    DeviceSerialNumber = r.DeviceSerialNumber,
                    Description = r.Description,
                    InstitutionName = (r.InstitutionName == "DİĞER" && !string.IsNullOrEmpty(r.InstitutionOtherName)) ? r.InstitutionOtherName : r.InstitutionName,
                    InstitutionOtherName = r.InstitutionOtherName,
                    Status = r.Status,
                    ContactPerson = r.ContactPerson,
                    ContactPhone = r.ContactPhone,
                    ContactEmail = r.ContactEmail,
                    ShippingAddressLine = r.ShippingAddressLine,
                    ShippingCity = r.ShippingCity,
                    ShippingDistrict = r.ShippingDistrict,
                    IsUrgent = r.IsUrgent,
                    ExtraItems = r.ExtraItems,
                    ShippedExtraItems = r.ShippedExtraItems,
                    StockItems = r.RequestStockItems.Select(rsi => new RequestStockItemDto
                    {
                        Id = rsi.Id,
                        StockItemId = rsi.StockItemId,
                        ItemName = rsi.StockItem.ItemName,
                        Quantity = rsi.Quantity,
                        IsDeducted = rsi.IsDeducted,
                        IsCancelled = rsi.IsCancelled,
                        IsShipped = rsi.IsShipped
                    }).ToList(),
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    CompletedAt = r.CompletedAt,
                    CreatedByUserId = r.CreatedByUserId
                })
                .ToListAsync();
        }

        public async Task<List<FaultDto>> GetFaultsAsync(string institutionName, int page = 1, int pageSize = 50)
        {
            var decodedName = Uri.UnescapeDataString(institutionName);

            return await _context.Faults
                .AsNoTracking()
                .Where(f => (f.InstitutionName == decodedName) || (f.InstitutionName == "DİĞER" && f.InstitutionOtherName == decodedName))
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new FaultDto
                {
                    Id = f.Id,
                    DeviceSerialNumber = f.DeviceSerialNumber,
                    Description = f.Description,
                    InstitutionName = (f.InstitutionName == "DİĞER" && !string.IsNullOrEmpty(f.InstitutionOtherName)) ? f.InstitutionOtherName : f.InstitutionName,
                    InstitutionOtherName = f.InstitutionOtherName,
                    Status = f.Status,
                    AssignedToUserId = f.AssignedToUserId,
                    ShippingAddressLine = f.ShippingAddressLine,
                    ShippingCity = f.ShippingCity,
                    ShippingDistrict = f.ShippingDistrict,
                    ContactPerson = f.ContactPerson,
                    ContactPhone = f.ContactPhone,
                    ContactEmail = f.ContactEmail,
                    CreatedAt = f.CreatedAt,
                    CompletedAt = f.CompletedAt,
                    CreatedByUserId = f.CreatedByUserId,
                    RelatedServiceFormId = f.RelatedServiceFormId
                })
                .ToListAsync();
        }

        public async Task<List<ServiceFormDto>> GetServiceFormsAsync(string institutionName, int page = 1, int pageSize = 50)
        {
            var decodedName = Uri.UnescapeDataString(institutionName);

            return await _context.ServiceForms
                .AsNoTracking()
                .Where(sf => (sf.InstitutionName == decodedName) || (sf.InstitutionName == "DİĞER" && sf.InstitutionOtherName == decodedName))
                .OrderByDescending(sf => sf.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(sf => new ServiceFormDto
                {
                    Id = sf.Id,
                    InstitutionName = (sf.InstitutionName == "DİĞER" && !string.IsNullOrEmpty(sf.InstitutionOtherName)) ? sf.InstitutionOtherName : sf.InstitutionName,
                    InstitutionOtherName = sf.InstitutionOtherName,
                    FormDate = sf.FormDate,
                    Status = sf.Status,
                    CreatedAt = sf.CreatedAt,
                    UpdatedAt = sf.UpdatedAt,
                    CreatedByUserId = sf.CreatedByUserId
                })
                .ToListAsync();
        }

        public async Task<List<WorkItemDto>> GetWorkItemsAsync(string institutionName, int page = 1, int pageSize = 50)
        {
            var decodedName = Uri.UnescapeDataString(institutionName);

            return await _context.WorkItems
                .AsNoTracking()
                .Where(wi => (wi.InstitutionName == decodedName) || (wi.InstitutionName == "DİĞER" && wi.InstitutionOtherName == decodedName))
                .OrderByDescending(wi => wi.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(wi => new WorkItemDto
                {
                    Id = wi.Id,
                    Title = wi.Title,
                    Description = wi.Description,
                    InstitutionName = (wi.InstitutionName == "DİĞER" && !string.IsNullOrEmpty(wi.InstitutionOtherName)) ? wi.InstitutionOtherName : wi.InstitutionName,
                    InstitutionOtherName = wi.InstitutionOtherName,
                    Stage = wi.Stage.ToString(),
                    Kind = wi.Kind.ToString(),
                    Priority = wi.Priority.ToString(),
                    CreatedAt = wi.CreatedAt,
                    UpdatedAt = wi.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<List<StockMovementDto>> GetStockMovementsAsync(string institutionName, int page = 1, int pageSize = 50)
        {
            var decodedName = Uri.UnescapeDataString(institutionName);

            return await _context.StockMovements
                .AsNoTracking()
                .Include(sm => sm.StockItem)
                .Where(sm => sm.InstitutionName == decodedName)
                .OrderByDescending(sm => sm.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(sm => new StockMovementDto
                {
                    Id = sm.Id,
                    StockItemId = sm.StockItemId,
                    StockItemName = sm.StockItem != null ? sm.StockItem.ItemName : "Bilinmiyor",
                    MovementType = sm.MovementType,
                    Quantity = sm.Quantity,
                    Reference = sm.Reference,
                    InstitutionName = sm.InstitutionName,
                    Notes = sm.Notes,
                    CreatedAt = sm.CreatedAt
                })
                .ToListAsync();
        }
    }
}
