using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeknikServisAPI.Constants;
using Microsoft.EntityFrameworkCore;
using TeknikServisAPI.Data;
using TeknikServisAPI.DTOs;
using TeknikServisAPI.Models;
using TeknikServisAPI.Services;

namespace TeknikServisAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class InstitutionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InstitutionsController> _logger;
        private readonly IInstitutionService _institutionService;

        public InstitutionsController(ApplicationDbContext context, ILogger<InstitutionsController> logger, IInstitutionService institutionService)
        {
            _context = context;
            _logger = logger;
            _institutionService = institutionService;
        }

        // GET: api/institutions
        [HttpGet]
        public async Task<ActionResult<PagedResult<InstitutionListDto>>> GetAll(
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // Optimization: Use AsNoTracking for read-only query
                var requestInstitutions = _context.Requests.AsNoTracking()
                    .Select(r => (r.InstitutionName == "DİĞER" && !string.IsNullOrEmpty(r.InstitutionOtherName)) ? r.InstitutionOtherName : r.InstitutionName);
                
                var faultInstitutions = _context.Faults.AsNoTracking()
                    .Select(f => (f.InstitutionName == "DİĞER" && !string.IsNullOrEmpty(f.InstitutionOtherName)) ? f.InstitutionOtherName : f.InstitutionName);
                
                var serviceFormInstitutions = _context.ServiceForms.AsNoTracking()
                    .Select(sf => (sf.InstitutionName == "DİĞER" && !string.IsNullOrEmpty(sf.InstitutionOtherName)) ? sf.InstitutionOtherName : sf.InstitutionName);
                
                var workItemInstitutions = _context.WorkItems.AsNoTracking()
                    .Select(wi => (wi.InstitutionName == "DİĞER" && !string.IsNullOrEmpty(wi.InstitutionOtherName)) ? wi.InstitutionOtherName : wi.InstitutionName);

                // Hepsini birleştir
                var allInstitutionsQuery = requestInstitutions
                    .Union(faultInstitutions)
                    .Union(serviceFormInstitutions)
                    .Union(workItemInstitutions)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Distinct();

                // Arama filtresi
                var searchValue = search?.Trim();
                if (!string.IsNullOrEmpty(searchValue))
                {
                    var searchPattern = $"%{searchValue.ToLower()}%";
                    allInstitutionsQuery = allInstitutionsQuery
                        .Where(name => name != null && EF.Functions.Like(name.ToLower(), searchPattern));
                }

                var totalCount = await allInstitutionsQuery.CountAsync();

                if (pageSize > 100) pageSize = 100;

                // Pagination
                var pagedInstitutions = await allInstitutionsQuery
                    .OrderBy(n => n)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                if (!pagedInstitutions.Any())
                {
                    return Ok(new PagedResult<InstitutionListDto> 
                    { 
                        Items = new List<InstitutionListDto>(), 
                        TotalCount = totalCount, 
                        PageNumber = page, 
                        PageSize = pageSize 
                    });
                }

                // Batch queries for statistics (Optimized with AsNoTracking)
                var requestCountsList = await _context.Requests.AsNoTracking()
                    .Where(r => pagedInstitutions.Contains(r.InstitutionName == "DİĞER" ? r.InstitutionOtherName : r.InstitutionName))
                    .GroupBy(r => r.InstitutionName == "DİĞER" ? r.InstitutionOtherName : r.InstitutionName)
                    .Select(g => new { Name = g.Key ?? string.Empty, Count = g.Count() })
                    .ToListAsync();
                var requestCounts = requestCountsList.ToDictionary(x => x.Name, x => x.Count);

                var faultCountsList = await _context.Faults.AsNoTracking()
                    .Where(f => pagedInstitutions.Contains(f.InstitutionName == "DİĞER" ? f.InstitutionOtherName : f.InstitutionName))
                    .GroupBy(f => f.InstitutionName == "DİĞER" ? f.InstitutionOtherName : f.InstitutionName)
                    .Select(g => new { Name = g.Key ?? string.Empty, Count = g.Count() })
                    .ToListAsync();
                var faultCounts = faultCountsList.ToDictionary(x => x.Name, x => x.Count);

                var serviceFormCountsList = await _context.ServiceForms.AsNoTracking()
                    .Where(sf => pagedInstitutions.Contains(sf.InstitutionName == "DİĞER" ? sf.InstitutionOtherName : sf.InstitutionName))
                    .GroupBy(sf => sf.InstitutionName == "DİĞER" ? sf.InstitutionOtherName : sf.InstitutionName)
                    .Select(g => new { Name = g.Key ?? string.Empty, Count = g.Count() })
                    .ToListAsync();
                var serviceFormCounts = serviceFormCountsList.ToDictionary(x => x.Name, x => x.Count);

                var workItemCountsList = await _context.WorkItems.AsNoTracking()
                    .Where(wi => pagedInstitutions.Contains(wi.InstitutionName == "DİĞER" ? wi.InstitutionOtherName : wi.InstitutionName))
                    .GroupBy(wi => wi.InstitutionName == "DİĞER" ? wi.InstitutionOtherName : wi.InstitutionName)
                    .Select(g => new { Name = g.Key ?? string.Empty, Count = g.Count() })
                    .ToListAsync();
                var workItemCounts = workItemCountsList.ToDictionary(x => x.Name, x => x.Count);

                var stockMovementCountsList = await _context.StockMovements.AsNoTracking()
                    .Where(sm => pagedInstitutions.Contains(sm.InstitutionName) && sm.MovementType == "Out")
                    .GroupBy(sm => sm.InstitutionName)
                    .Select(g => new { Name = g.Key ?? string.Empty, Count = g.Count() })
                    .ToListAsync();
                var stockMovementCounts = stockMovementCountsList.ToDictionary(x => x.Name, x => x.Count);

                // Construct result list
                var result = pagedInstitutions.Select(n => 
                {
                    var name = n ?? string.Empty;
                    return new InstitutionListDto
                    {
                        Name = name,
                        RequestCount = requestCounts.GetValueOrDefault(name, 0),
                        FaultCount = faultCounts.GetValueOrDefault(name, 0),
                        ServiceFormCount = serviceFormCounts.GetValueOrDefault(name, 0),
                        WorkItemCount = workItemCounts.GetValueOrDefault(name, 0),
                        StockMovementCount = stockMovementCounts.GetValueOrDefault(name, 0)
                    };
                }).ToList();

                return Ok(new PagedResult<InstitutionListDto>
                {
                    Items = result,
                    TotalCount = totalCount,
                    PageNumber = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting institutions list");
                return StatusCode(500, "Kurumlar listesi alınırken hata oluştu");
            }
        }

        // GET: api/institutions/names
        [HttpGet("names")]
        public async Task<ActionResult<List<string>>> GetInstitutionNames(
            [FromQuery] string? search = null,
            [FromQuery] bool includeOther = true,
            [FromQuery] int limit = 200)
        {
            try
            {
                // Institutions tablosundan aktif kurumları çek
                var query = _context.Institutions
                    .Where(i => i.IsActive);

                // Arama filtresi
                var searchValue = search?.Trim();
                if (!string.IsNullOrEmpty(searchValue))
                {
                    var searchPattern = $"%{searchValue.ToLower()}%";
                    query = query.Where(i => EF.Functions.Like(i.Name.ToLower(), searchPattern));
                }

                // DİĞER seçeneğini hariç tut (ayrıca ekleneceği için)
                if (!includeOther)
                {
                    query = query.Where(i => i.Name != "DİĞER");
                }

                if (limit < 1) limit = 1;
                if (limit > 500) limit = 500;

                var names = await query
                    .OrderBy(i => i.Name)
                    .Take(limit)
                    .Select(i => i.Name)
                    .ToListAsync();

                // DİĞER her zaman en sonda olsun
                if (includeOther)
                {
                    var otherOption = "DİĞER";
                    var shouldIncludeOther = string.IsNullOrEmpty(searchValue) ||
                                             otherOption.ToLower().Contains(searchValue!.ToLower());
                    
                    // Listeden DİĞER'i çıkar (varsa) ve sona ekle
                    names.Remove(otherOption);
                    if (shouldIncludeOther)
                    {
                        names.Add(otherOption);
                    }
                }

                return Ok(names);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting institution names");
                return StatusCode(500, "Kurum listesi alınırken hata oluştu");
            }
        }

        // GET: api/institutions/{name}/summary
        [HttpGet("{name}/summary")]
        public async Task<ActionResult<InstitutionSummaryDto>> GetSummary(string name)
        {
            try
            {
                var summary = await _institutionService.GetSummaryAsync(name);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting institution summary for {Name}", name);
                return StatusCode(500, "Kurum özeti alınırken hata oluştu");
            }
        }

        // GET: api/institutions/{name}/requests
        [HttpGet("{name}/requests")]
        public async Task<ActionResult<PagedResult<object>>> GetRequests(
            string name,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var decodedName = Uri.UnescapeDataString(name);
                if (page < 1) page = 1;
                if (pageSize > 100) pageSize = 100;

                var query = _context.Requests
                    .Include(r => r.CreatedByUser)
                    .Where(r => (r.InstitutionName == decodedName) || (r.InstitutionName == "DİĞER" && r.InstitutionOtherName == decodedName));

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(r => r.Status == status);

                if (startDate.HasValue)
                    query = query.Where(r => r.CreatedAt >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(r => r.CreatedAt <= endDate.Value);

                var totalCount = await query.CountAsync();

                var requests = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new
                    {
                        r.Id,
                        InstitutionName = (r.InstitutionName == "DİĞER" && !string.IsNullOrEmpty(r.InstitutionOtherName)) ? r.InstitutionOtherName : r.InstitutionName,
                        r.Status,
                        r.IsUrgent,
                        r.CreatedAt,
                        CreatedByUserName = r.CreatedByUser != null
                            ? $"{r.CreatedByUser.FirstName} {r.CreatedByUser.LastName}"
                            : null
                    })
                    .ToListAsync();

                return Ok(new PagedResult<object>
                {
                    Items = requests.Cast<object>().ToList(),
                    TotalCount = totalCount,
                    PageNumber = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting requests for institution {Name}", name);
                return StatusCode(500, "Kurum talepleri alınırken hata oluştu");
            }
        }

        // GET: api/institutions/{name}/faults
        [HttpGet("{name}/faults")]
        public async Task<ActionResult<PagedResult<object>>> GetFaults(
            string name,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var decodedName = Uri.UnescapeDataString(name);
                if (page < 1) page = 1;
                if (pageSize > 100) pageSize = 100;

                var query = _context.Faults
                    .Include(f => f.CreatedByUser)
                    .Where(f => (f.InstitutionName == decodedName) || (f.InstitutionName == "DİĞER" && f.InstitutionOtherName == decodedName));

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(f => f.Status == status);

                if (startDate.HasValue)
                    query = query.Where(f => f.CreatedAt >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(f => f.CreatedAt <= endDate.Value);

                var totalCount = await query.CountAsync();

                var faults = await query
                    .OrderByDescending(f => f.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(f => new
                    {
                        id = f.Id,
                        institutionName = (f.InstitutionName == "DİĞER" && !string.IsNullOrEmpty(f.InstitutionOtherName)) ? f.InstitutionOtherName : f.InstitutionName,
                        deviceSerialNumber = f.DeviceSerialNumber,
                        status = f.Status,
                        description = f.Description,
                        createdAt = f.CreatedAt,
                        createdByUserName = f.CreatedByUser != null
                            ? $"{f.CreatedByUser.FirstName} {f.CreatedByUser.LastName}"
                            : null
                    })
                    .ToListAsync();

                return Ok(new PagedResult<object>
                {
                    Items = faults.Cast<object>().ToList(),
                    TotalCount = totalCount,
                    PageNumber = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting faults for institution {Name}", name);
                return StatusCode(500, "Kurum arızaları alınırken hata oluştu");
            }
        }

        // GET: api/institutions/{name}/service-forms
        [HttpGet("{name}/service-forms")]
        public async Task<ActionResult<PagedResult<object>>> GetServiceForms(
            string name,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var decodedName = Uri.UnescapeDataString(name);
                if (page < 1) page = 1;
                if (pageSize > 100) pageSize = 100;

                var query = _context.ServiceForms
                    .Include(sf => sf.CreatedByUser)
                    .Where(sf => (sf.InstitutionName == decodedName) || (sf.InstitutionName == "DİĞER" && sf.InstitutionOtherName == decodedName));

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(sf => sf.Status == status);

                if (startDate.HasValue)
                    query = query.Where(sf => sf.CreatedAt >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(sf => sf.CreatedAt <= endDate.Value);

                var totalCount = await query.CountAsync();

                var serviceForms = await query
                    .OrderByDescending(sf => sf.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(sf => new
                    {
                        sf.Id,
                        InstitutionName = (sf.InstitutionName == "DİĞER" && !string.IsNullOrEmpty(sf.InstitutionOtherName)) ? sf.InstitutionOtherName : sf.InstitutionName,
                        sf.Status,
                        sf.ServiceType,
                        sf.FormDate,
                        sf.CreatedAt,
                        CreatedByUserName = sf.CreatedByUser != null
                            ? $"{sf.CreatedByUser.FirstName} {sf.CreatedByUser.LastName}"
                            : null
                    })
                    .ToListAsync();

                return Ok(new PagedResult<object>
                {
                    Items = serviceForms.Cast<object>().ToList(),
                    TotalCount = totalCount,
                    PageNumber = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service forms for institution {Name}", name);
                return StatusCode(500, "Kurum servis formları alınırken hata oluştu");
            }
        }

        // GET: api/institutions/{name}/work-items
        [HttpGet("{name}/work-items")]
        public async Task<ActionResult<PagedResult<object>>> GetWorkItems(
            string name,
            [FromQuery] string? stage = null,
            [FromQuery] string? priority = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var decodedName = Uri.UnescapeDataString(name);
                if (page < 1) page = 1;
                if (pageSize > 100) pageSize = 100;

                var query = _context.WorkItems
                    .Include(wi => wi.CreatedByUser)
                    .Where(wi => (wi.InstitutionName == decodedName) || (wi.InstitutionName == "DİĞER" && wi.InstitutionOtherName == decodedName));

                if (!string.IsNullOrEmpty(stage) && Enum.TryParse<WorkItemStage>(stage, out var stageEnum))
                    query = query.Where(wi => wi.Stage == stageEnum);

                if (!string.IsNullOrEmpty(priority) && Enum.TryParse<WorkItemPriority>(priority, out var priorityEnum))
                    query = query.Where(wi => wi.Priority == priorityEnum);

                if (startDate.HasValue)
                    query = query.Where(wi => wi.CreatedAt >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(wi => wi.CreatedAt <= endDate.Value);

                var totalCount = await query.CountAsync();

                var workItems = await query
                    .OrderByDescending(wi => wi.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(wi => new
                    {
                        wi.Id,
                        wi.Title,
                        InstitutionName = (wi.InstitutionName == "DİĞER" && !string.IsNullOrEmpty(wi.InstitutionOtherName)) ? wi.InstitutionOtherName : wi.InstitutionName,
                        Stage = wi.Stage.ToString(),
                        Priority = wi.Priority.ToString(),
                        Kind = wi.Kind.ToString(),
                        wi.CreatedAt,
                        CreatedByUserName = wi.CreatedByUser != null
                            ? $"{wi.CreatedByUser.FirstName} {wi.CreatedByUser.LastName}"
                            : null
                    })
                    .ToListAsync();

                return Ok(new PagedResult<object>
                {
                    Items = workItems.Cast<object>().ToList(),
                    TotalCount = totalCount,
                    PageNumber = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting work items for institution {Name}", name);
                return StatusCode(500, "Kurum iş kartları alınırken hata oluştu");
            }
        }

        // GET: api/institutions/{name}/stock-movements
        [HttpGet("{name}/stock-movements")]
        public async Task<ActionResult<PagedResult<InstitutionStockUsageDto>>> GetStockMovements(
            string name,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var decodedName = Uri.UnescapeDataString(name);
                if (page < 1) page = 1;
                if (pageSize > 100) pageSize = 100;

                var query = _context.StockMovements
                    .Where(sm => sm.InstitutionName == decodedName && sm.MovementType == "Out");

                if (startDate.HasValue)
                    query = query.Where(sm => sm.MovementDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(sm => sm.MovementDate <= endDate.Value);

                var groupedMovements = await query
                    .GroupBy(sm => sm.StockItemId)
                    .Select(g => new InstitutionStockUsageDto
                    {
                        StockItemId = g.Key,
                        ItemName = g.Select(sm => sm.StockItem != null ? sm.StockItem.ItemName : "Bilinmeyen").FirstOrDefault() ?? "Bilinmeyen",
                        TotalQuantityUsed = g.Sum(sm => sm.Quantity),
                        UnitPrice = g.Select(sm => sm.StockItem != null ? (sm.StockItem.UnitPrice ?? 0m) : 0m).FirstOrDefault(),
                        TotalValue = g.Sum(sm => sm.Quantity * (sm.StockItem != null ? (sm.StockItem.UnitPrice ?? 0m) : 0m)),
                        LastUsedDate = g.Max(sm => sm.MovementDate),
                        UsageCount = g.Count()
                    })
                    .OrderByDescending(dto => dto.TotalValue)
                    .ToListAsync();

                var totalCount = groupedMovements.Count;

                var pagedResult = groupedMovements
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new PagedResult<InstitutionStockUsageDto>
                {
                    Items = pagedResult,
                    TotalCount = totalCount,
                    PageNumber = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock movements for institution {Name}", name);
                return StatusCode(500, "Kurum stok hareketleri alınırken hata oluştu");
            }
        }

        // ============================================
        // CRUD Operations for Institution Management
        // ============================================

        // GET: api/institutions/manage
        [HttpGet("manage")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<ActionResult<List<InstitutionManageDto>>> GetAllForManagement([FromQuery] string? search = null)
        {
            try
            {
                var query = _context.Institutions.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(i => i.Name.ToLower().Contains(searchLower));
                }

                var institutions = await query
                    .OrderBy(i => i.Name)
                    .Select(i => new InstitutionManageDto
                    {
                        Id = i.Id,
                        Name = i.Name,
                        IsActive = i.IsActive,
                        AddressLine = i.AddressLine,
                        City = i.City,
                        District = i.District,
                        PostalCode = i.PostalCode,
                        ContactPerson = i.ContactPerson,
                        Phone = i.Phone,
                        Email = i.Email,
                        CreatedAt = i.CreatedAt,
                        UpdatedAt = i.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(institutions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting institutions for management");
                return StatusCode(500, "Kurumlar alınırken hata oluştu");
            }
        }

        // POST: api/institutions/manage
        [HttpPost("manage")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<ActionResult<InstitutionManageDto>> Create([FromBody] CreateInstitutionDto dto)
        {
            try
            {
                // Check if name already exists
                var exists = await _context.Institutions.AnyAsync(i => i.Name == dto.Name);
                if (exists)
                {
                    return BadRequest("Bu isimde bir kurum zaten mevcut");
                }

                var institution = new Institution
                {
                    Name = dto.Name,
                    IsActive = dto.IsActive,
                    AddressLine = dto.AddressLine,
                    City = dto.City,
                    District = dto.District,
                    PostalCode = dto.PostalCode,
                    ContactPerson = dto.ContactPerson,
                    Phone = dto.Phone,
                    Email = dto.Email,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Institutions.Add(institution);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Kurum oluşturuldu: {Name} (ID: {Id})", institution.Name, institution.Id);

                return Ok(new InstitutionManageDto
                {
                    Id = institution.Id,
                    Name = institution.Name,
                    IsActive = institution.IsActive,
                    AddressLine = institution.AddressLine,
                    City = institution.City,
                    District = institution.District,
                    PostalCode = institution.PostalCode,
                    ContactPerson = institution.ContactPerson,
                    Phone = institution.Phone,
                    Email = institution.Email,
                    CreatedAt = institution.CreatedAt,
                    UpdatedAt = institution.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating institution");
                return StatusCode(500, "Kurum oluşturulurken hata oluştu");
            }
        }

        // PUT: api/institutions/manage/{id}
        [HttpPut("manage/{id}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<ActionResult<InstitutionManageDto>> Update(int id, [FromBody] UpdateInstitutionDto dto)
        {
            try
            {
                var institution = await _context.Institutions.FindAsync(id);
                if (institution == null)
                {
                    return NotFound("Kurum bulunamadı");
                }

                // Check if new name conflicts with another institution
                if (!string.IsNullOrEmpty(dto.Name) && dto.Name != institution.Name)
                {
                    var exists = await _context.Institutions.AnyAsync(i => i.Name == dto.Name && i.Id != id);
                    if (exists)
                    {
                        return BadRequest("Bu isimde bir kurum zaten mevcut");
                    }
                    institution.Name = dto.Name;
                }

                if (dto.IsActive.HasValue)
                    institution.IsActive = dto.IsActive.Value;

                if (dto.AddressLine != null)
                    institution.AddressLine = dto.AddressLine;

                if (dto.City != null)
                    institution.City = dto.City;

                if (dto.District != null)
                    institution.District = dto.District;

                if (dto.PostalCode != null)
                    institution.PostalCode = dto.PostalCode;

                if (dto.ContactPerson != null)
                    institution.ContactPerson = dto.ContactPerson;

                if (dto.Phone != null)
                    institution.Phone = dto.Phone;

                if (dto.Email != null)
                    institution.Email = dto.Email;

                institution.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Kurum güncellendi: {Name} (ID: {Id})", institution.Name, institution.Id);

                return Ok(new InstitutionManageDto
                {
                    Id = institution.Id,
                    Name = institution.Name,
                    IsActive = institution.IsActive,
                    AddressLine = institution.AddressLine,
                    City = institution.City,
                    District = institution.District,
                    PostalCode = institution.PostalCode,
                    ContactPerson = institution.ContactPerson,
                    Phone = institution.Phone,
                    Email = institution.Email,
                    CreatedAt = institution.CreatedAt,
                    UpdatedAt = institution.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating institution {Id}", id);
                return StatusCode(500, "Kurum güncellenirken hata oluştu");
            }
        }

        // DELETE: api/institutions/manage/{id}
        [HttpDelete("manage/{id}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var institution = await _context.Institutions.FindAsync(id);
                if (institution == null)
                {
                    return NotFound("Kurum bulunamadı");
                }

                // Prevent deleting "DİĞER"
                if (institution.Name == "DİĞER")
                {
                    return BadRequest("DİĞER kurumu silinemez");
                }

                _context.Institutions.Remove(institution);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Kurum silindi: {Name} (ID: {Id})", institution.Name, id);

                return Ok(new { message = "Kurum silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting institution {Id}", id);
                return StatusCode(500, "Kurum silinirken hata oluştu");
            }
        }

        // ============================================
        // Institution Address CRUD Operations
        // ============================================

        // GET: api/institutions/{id}/addresses
        [HttpGet("{id}/addresses")]
        public async Task<ActionResult<List<InstitutionAddressDto>>> GetAddresses(int id)
        {
            try
            {
                var institution = await _context.Institutions.FindAsync(id);
                if (institution == null)
                {
                    return NotFound("Kurum bulunamadı");
                }

                var addresses = await _context.InstitutionAddresses
                    .Where(a => a.InstitutionId == id)
                    .OrderByDescending(a => a.IsDefault)
                    .ThenBy(a => a.Label)
                    .Select(a => new InstitutionAddressDto
                    {
                        Id = a.Id,
                        InstitutionId = a.InstitutionId,
                        Label = a.Label,
                        City = a.City,
                        District = a.District,
                        AddressLine = a.AddressLine,
                        ContactPerson = a.ContactPerson,
                        Phone = a.Phone,
                        Email = a.Email,
                        IsDefault = a.IsDefault,
                        CreatedAt = a.CreatedAt
                    })
                    .ToListAsync();

                return Ok(addresses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting addresses for institution {Id}", id);
                return StatusCode(500, "Adresler alınırken hata oluştu");
            }
        }

        // POST: api/institutions/{id}/addresses
        [HttpPost("{id}/addresses")]
        public async Task<ActionResult<InstitutionAddressDto>> CreateAddress(int id, [FromBody] CreateInstitutionAddressDto dto)
        {
            try
            {
                var institution = await _context.Institutions.FindAsync(id);
                if (institution == null)
                {
                    return NotFound("Kurum bulunamadı");
                }

                // If this is set as default, remove default from others
                if (dto.IsDefault)
                {
                    var existingDefaults = await _context.InstitutionAddresses
                        .Where(a => a.InstitutionId == id && a.IsDefault)
                        .ToListAsync();
                    foreach (var addr in existingDefaults)
                    {
                        addr.IsDefault = false;
                    }
                }

                var address = new InstitutionAddress
                {
                    InstitutionId = id,
                    Label = dto.Label,
                    City = dto.City,
                    District = dto.District,
                    AddressLine = dto.AddressLine,
                    ContactPerson = dto.ContactPerson,
                    Phone = dto.Phone,
                    Email = dto.Email,
                    IsDefault = dto.IsDefault,
                    CreatedAt = DateTime.UtcNow
                };

                _context.InstitutionAddresses.Add(address);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Adres oluşturuldu: {Label} (Institution: {InstitutionId})", address.Label, id);

                return Ok(new InstitutionAddressDto
                {
                    Id = address.Id,
                    InstitutionId = address.InstitutionId,
                    Label = address.Label,
                    City = address.City,
                    District = address.District,
                    AddressLine = address.AddressLine,
                    ContactPerson = address.ContactPerson,
                    Phone = address.Phone,
                    Email = address.Email,
                    IsDefault = address.IsDefault,
                    CreatedAt = address.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating address for institution {Id}", id);
                return StatusCode(500, "Adres oluşturulurken hata oluştu");
            }
        }

        // PUT: api/institutions/addresses/{id}
        [HttpPut("addresses/{id}")]
        public async Task<ActionResult<InstitutionAddressDto>> UpdateAddress(int id, [FromBody] UpdateInstitutionAddressDto dto)
        {
            try
            {
                var address = await _context.InstitutionAddresses.FindAsync(id);
                if (address == null)
                {
                    return NotFound("Adres bulunamadı");
                }

                // If setting as default, remove default from others
                if (dto.IsDefault == true && !address.IsDefault)
                {
                    var existingDefaults = await _context.InstitutionAddresses
                        .Where(a => a.InstitutionId == address.InstitutionId && a.IsDefault && a.Id != id)
                        .ToListAsync();
                    foreach (var addr in existingDefaults)
                    {
                        addr.IsDefault = false;
                    }
                }

                if (dto.Label != null) address.Label = dto.Label;
                if (dto.City != null) address.City = dto.City;
                if (dto.District != null) address.District = dto.District;
                if (dto.AddressLine != null) address.AddressLine = dto.AddressLine;
                if (dto.ContactPerson != null) address.ContactPerson = dto.ContactPerson;
                if (dto.Phone != null) address.Phone = dto.Phone;
                if (dto.Email != null) address.Email = dto.Email;
                if (dto.IsDefault.HasValue) address.IsDefault = dto.IsDefault.Value;

                address.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Adres güncellendi: {Id}", id);

                return Ok(new InstitutionAddressDto
                {
                    Id = address.Id,
                    InstitutionId = address.InstitutionId,
                    Label = address.Label,
                    City = address.City,
                    District = address.District,
                    AddressLine = address.AddressLine,
                    ContactPerson = address.ContactPerson,
                    Phone = address.Phone,
                    Email = address.Email,
                    IsDefault = address.IsDefault,
                    CreatedAt = address.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating address {Id}", id);
                return StatusCode(500, "Adres güncellenirken hata oluştu");
            }
        }

        // DELETE: api/institutions/addresses/{id}
        [HttpDelete("addresses/{id}")]
        public async Task<ActionResult> DeleteAddress(int id)
        {
            try
            {
                var address = await _context.InstitutionAddresses.FindAsync(id);
                if (address == null)
                {
                    return NotFound("Adres bulunamadı");
                }

                _context.InstitutionAddresses.Remove(address);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Adres silindi: {Id}", id);

                return Ok(new { message = "Adres silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting address {Id}", id);
                return StatusCode(500, "Adres silinirken hata oluştu");
            }
        }

        // GET: api/institutions/by-name/{name}/addresses
        // Get addresses by institution name (for form auto-fill)
        [HttpGet("by-name/{name}/addresses")]
        public async Task<ActionResult<List<InstitutionAddressDto>>> GetAddressesByName(string name)
        {
            try
            {
                var decodedName = Uri.UnescapeDataString(name);
                
                var institution = await _context.Institutions
                    .FirstOrDefaultAsync(i => i.Name == decodedName);
                    
                if (institution == null)
                {
                    return Ok(new List<InstitutionAddressDto>());
                }

                var addresses = await _context.InstitutionAddresses
                    .Where(a => a.InstitutionId == institution.Id)
                    .OrderByDescending(a => a.IsDefault)
                    .ThenBy(a => a.Label)
                    .Select(a => new InstitutionAddressDto
                    {
                        Id = a.Id,
                        InstitutionId = a.InstitutionId,
                        Label = a.Label,
                        City = a.City,
                        District = a.District,
                        AddressLine = a.AddressLine,
                        ContactPerson = a.ContactPerson,
                        Phone = a.Phone,
                        Email = a.Email,
                        IsDefault = a.IsDefault,
                        CreatedAt = a.CreatedAt
                    })
                    .ToListAsync();

                return Ok(addresses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting addresses by institution name {Name}", name);
                return StatusCode(500, "Adresler alınırken hata oluştu");
            }
        }

        // POST: api/institutions/by-name/{name}/addresses
        // Save new address by institution name (for form auto-fill save)
        [HttpPost("by-name/{name}/addresses")]
        public async Task<ActionResult<InstitutionAddressDto>> CreateAddressByName(string name, [FromBody] CreateInstitutionAddressDto dto)
        {
            try
            {
                var decodedName = Uri.UnescapeDataString(name);
                
                var institution = await _context.Institutions
                    .FirstOrDefaultAsync(i => i.Name == decodedName);
                    
                if (institution == null)
                {
                    return NotFound("Kurum bulunamadı");
                }

                if (dto.IsDefault)
                {
                    var existingDefaults = await _context.InstitutionAddresses
                        .Where(a => a.InstitutionId == institution.Id && a.IsDefault)
                        .ToListAsync();
                    foreach (var addr in existingDefaults)
                    {
                        addr.IsDefault = false;
                    }
                }

                var address = new InstitutionAddress
                {
                    InstitutionId = institution.Id,
                    Label = dto.Label,
                    City = dto.City,
                    District = dto.District,
                    AddressLine = dto.AddressLine,
                    ContactPerson = dto.ContactPerson,
                    Phone = dto.Phone,
                    Email = dto.Email,
                    IsDefault = dto.IsDefault,
                    CreatedAt = DateTime.UtcNow
                };

                _context.InstitutionAddresses.Add(address);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Adres oluşturuldu (by-name): {Label} (Institution: {Name})", address.Label, decodedName);

                return Ok(new InstitutionAddressDto
                {
                    Id = address.Id,
                    InstitutionId = address.InstitutionId,
                    Label = address.Label,
                    City = address.City,
                    District = address.District,
                    AddressLine = address.AddressLine,
                    ContactPerson = address.ContactPerson,
                    Phone = address.Phone,
                    Email = address.Email,
                    IsDefault = address.IsDefault,
                    CreatedAt = address.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating address by institution name {Name}", name);
                return StatusCode(500, "Adres oluşturulurken hata oluştu");
            }
        }
    }
}
