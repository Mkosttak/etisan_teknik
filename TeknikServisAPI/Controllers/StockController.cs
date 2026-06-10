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
    public class StockController : ControllerBase
    {
        private readonly IStockService _stockService;
        private readonly ILogger<StockController> _logger;

        public StockController(IStockService stockService, ILogger<StockController> logger)
        {
            _stockService = stockService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _stockService.GetAllStockItemsAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _stockService.GetStockItemByIdAsync(id);
            if (item == null)
                return NotFound(new { message = "Stok ürünü bulunamadı" });

            return Ok(item);
        }

        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStock()
        {
            var items = await _stockService.GetLowStockItemsAsync();
            return Ok(items);
        }

        [HttpGet("{id}/movements")]
        public async Task<IActionResult> GetMovements(int id)
        {
            var movements = await _stockService.GetStockMovementsAsync(id);
            return Ok(movements);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Proje")] // Sadece Admin/Proje ekleyebilir
        public async Task<IActionResult> Create([FromBody] CreateStockItemDto dto)
        {
            var item = await _stockService.CreateStockItemAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Proje,Teknik")] // Admin/Proje/Teknik güncelleyebilir
        public async Task<IActionResult> Update(int id, [FromBody] UpdateStockItemDto dto)
        {
            var item = await _stockService.UpdateStockItemAsync(id, dto);
            if (item == null)
                return NotFound(new { message = "Stok ürünü bulunamadı" });

            return Ok(item);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Proje")] // Sadece Admin/Proje silebilir
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _stockService.DeleteStockItemAsync(id);
            if (!result)
                return NotFound(new { message = "Stok ürünü bulunamadı" });

            return Ok(new { message = "Stok ürünü pasif hale getirildi" });
        }

        [HttpPost("movements")]
        [Authorize(Roles = "Admin,Proje,Teknik")] // Admin/Proje/Teknik hareket ekleyebilir
        public async Task<IActionResult> AddMovement([FromBody] CreateStockMovementDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var movement = await _stockService.AddStockMovementAsync(dto, userId);
                return Ok(movement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok hareketi eklenirken hata olu?Ytu");
                return StatusCode(500, new { message = "Stok hareketi eklenirken hata olu?Ytu" });
            }
        }
    }
}

