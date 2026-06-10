using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeknikServisAPI.Constants;
using TeknikServisAPI.DTOs;
using TeknikServisAPI.Services;

namespace TeknikServisAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RequestsController : ControllerBase
    {
        private readonly IRequestService _requestService;

        public RequestsController(IRequestService requestService)
        {
            _requestService = requestService;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            var requests = await _requestService.GetAllRequestsAsync(userId, userRole);
            return Ok(requests);
        }

        /// <summary>
        /// Sayfalanmış talep listesi (server-side pagination)
        /// </summary>
        /// <param name="page">Sayfa numarası (default: 1)</param>
        /// <param name="pageSize">Sayfa başına kayıt (default: 20, max: 100)</param>
        /// <param name="status">Durum filtresi (opsiyonel)</param>
        /// <param name="priority">Öncelik filtresi: "urgent" veya "normal" (opsiyonel)</param>
        /// <returns>Sayfalanmış talep listesi</returns>
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] string? priority = null)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            
            var result = await _requestService.GetPagedRequestsAsync(
                page, 
                pageSize, 
                status, 
                priority, 
                userId, 
                userRole);
            
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = AuthorizationPolicies.RequestOwnerOrPrivileged)]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            var request = await _requestService.GetRequestByIdAsync(id, userId, userRole);
            if (request == null)
                return NotFound(new { message = "Talep bulunamadı" });


            return Ok(request);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var request = await _requestService.CreateRequestAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = request.Id }, request);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = AuthorizationPolicies.RequestOwnerOrPrivileged)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            var request = await _requestService.UpdateRequestAsync(id, dto, userId, userRole);
            if (request == null)
                return NotFound(new { message = "Talep bulunamadı" });
            
            

            return Ok(request);
        }

        [HttpPut("{id}/status")]
        [Authorize(Policy = AuthorizationPolicies.RequestOwnerOrPrivileged)]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateRequestStatusDto dto)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            var request = await _requestService.UpdateRequestStatusAsync(id, dto.Status, userId, userRole);
            if (request == null)
                return NotFound(new { message = "Talep bulunamadı" });

            return Ok(request);
        }

        [HttpPost("{id}/ship")]
        [Authorize(Policy = AuthorizationPolicies.RequestOwnerOrPrivileged)]
        public async Task<IActionResult> ShipItems(int id, [FromBody] ShipRequestItemsDto dto)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            var request = await _requestService.ShipRequestItemsAsync(id, dto.RequestStockItemIds, dto.ExtraItems, userId, userRole);
            if (request == null)
                return NotFound(new { message = "Talep bulunamadı" });

            return Ok(request);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = AuthorizationPolicies.RequestOwnerOrPrivileged)]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            var result = await _requestService.DeleteRequestAsync(id, userId, userRole);
            if (!result)
                return NotFound(new { message = "Talep bulunamadı" });

            return Ok(new { message = "Talep silindi" });
        }
    }
}
