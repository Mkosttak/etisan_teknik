using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeknikServisAPI.Constants;
using TeknikServisAPI.DTOs;
using TeknikServisAPI.Services;

namespace TeknikServisAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ShipmentsController : ControllerBase
    {
        private readonly IShipmentService _shipmentService;
        private readonly IFaultService _faultService;

        public ShipmentsController(IShipmentService shipmentService, IFaultService faultService)
        {
            _shipmentService = shipmentService;
            _faultService = faultService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var shipments = await _shipmentService.GetAllShipmentsAsync();
            return Ok(shipments);
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingShipments()
        {
            var allFaults = await _faultService.GetAllFaultsAsync();
            // Verifone'a ve müşteriye gönderilecek cihazları filtrele
            var pendingShipments = allFaults
                .Where(f => f.Status == FaultStatuses.ShippingToManufacturer || f.Status == FaultStatuses.ShippingToCustomer)
                .Select(f => new
                {
                    Id = f.Id,
                    FaultId = f.Id,
                    DeviceSerialNumber = f.DeviceSerialNumber,
                    Description = f.Description,
                    ShipmentType = f.Status == FaultStatuses.ShippingToManufacturer ? "Verifone'a Gönderilecek" : "Müşteriye Gönderilecek",
                    Status = f.Status,
                    ShippingAddressLine = f.ShippingAddressLine,
                    ShippingCity = f.ShippingCity,
                    ShippingDistrict = f.ShippingDistrict,
                    ContactPerson = f.ContactPerson,
                    ContactPhone = f.ContactPhone,
                    CreatedAt = f.CreatedAt
                })
                .ToList();
            
            return Ok(pendingShipments);
        }

        [HttpGet("verifone")]
        public async Task<IActionResult> GetVerifoneShipments()
        {
            var allFaults = await _faultService.GetAllFaultsAsync();
            var verifoneFaults = allFaults
                .Where(f => f.Status == FaultStatuses.ShippingToManufacturer)
                .ToList();

            var result = new List<object>();
            
            foreach (var fault in verifoneFaults)
            {
                // Hata kodunu history'den bul
                var history = await _faultService.GetFaultHistoryAsync(fault.Id);
                var statusChangeHistory = history
                    .Where(h => h.Action == "StatusChanged" && h.NewValue == FaultStatuses.ShippingToManufacturer)
                    .OrderByDescending(h => h.CreatedAt)
                    .FirstOrDefault();

                string errorCode = string.Empty;
                string errorDescription = string.Empty;

                if (statusChangeHistory?.Notes != null)
                {
                    // Notes formatı: "Hata Kodu: 000 - Onay Kodu xxxxxx" veya "Hata Kodu: 000 - Onay Kodu xxxxxx\nDiğer notlar"
                    var notes = statusChangeHistory.Notes;
                    if (notes.Contains("Hata Kodu:"))
                    {
                        // "Hata Kodu: 000 - Onay Kodu xxxxxx" formatından parse et
                        // "Hata Kodu: ... - ..." formatından parse et
                        // Regex'i esnekleştirdik (sadece 3 hane değil, tire işaretine kadar olan kısmı al)
                        var codeMatch = System.Text.RegularExpressions.Regex.Match(notes, @"Hata Kodu:\s*(.*?)\s*-\s*");
                        if (codeMatch.Success)
                        {
                            errorCode = codeMatch.Groups[1].Value.Trim();
                            
                            // Açıklamayı bul - "Hata Kodu: ... - " sonrasındaki kısmı al
                            var descMatch = System.Text.RegularExpressions.Regex.Match(notes, @"Hata Kodu:\s*.*?\s*-\s*([^\n]+)");
                            if (descMatch.Success)
                            {
                                errorDescription = descMatch.Groups[1].Value.Trim();
                            }
                        }
                    }
                }

                result.Add(new
                {
                    Id = fault.Id,
                    FaultId = fault.Id,
                    DeviceSerialNumber = fault.DeviceSerialNumber,
                    ErrorCode = errorCode,
                    ErrorDescription = errorDescription,
                    Description = fault.Description,
                    CreatedAt = fault.CreatedAt
                });
            }

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var shipment = await _shipmentService.GetShipmentByIdAsync(id);
            if (shipment == null)
                return NotFound(new { message = "Gönderim bulunamadı" });

            return Ok(shipment);
        }

        // Müşteri özelliği kaldırıldı
        // [HttpGet("customer/{customerId}")]
        // public async Task<IActionResult> GetByCustomerId(int customerId)
        // {
        //     var shipments = await _shipmentService.GetShipmentsByCustomerIdAsync(customerId);
        //     return Ok(shipments);
        // }

        [HttpPost]
        [Authorize(Roles = "Admin,Proje,Teknik")] // Admin/Proje/Teknik oluşturabilir
        public async Task<IActionResult> Create([FromBody] CreateShipmentDto dto)
        {
            var shipment = await _shipmentService.CreateShipmentAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = shipment.Id }, shipment);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Proje,Teknik")] // Admin/Proje/Teknik güncelleyebilir
        public async Task<IActionResult> Update(int id, [FromBody] UpdateShipmentDto dto)
        {
            var shipment = await _shipmentService.UpdateShipmentAsync(id, dto);
            if (shipment == null)
                return NotFound(new { message = "Gönderim bulunamadı" });

            return Ok(shipment);
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin,Proje,Teknik")] // Admin/Proje/Teknik durum güncelleyebilir
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateShipmentStatusDto dto)
        {
            var shipment = await _shipmentService.UpdateShipmentStatusAsync(id, dto.Status);
            if (shipment == null)
                return NotFound(new { message = "Gönderim bulunamadı" });

            return Ok(shipment);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Proje")] // Sadece Admin/Proje silebilir
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _shipmentService.DeleteShipmentAsync(id);
            if (!result)
                return NotFound(new { message = "Gönderim bulunamadı" });

            return Ok(new { message = "Gönderim iptal edildi" });
        }
    }

    public class UpdateShipmentStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}

