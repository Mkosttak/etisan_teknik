using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeknikServisAPI.Constants;
using TeknikServisAPI.Data;
using TeknikServisAPI.DTOs;
using TeknikServisAPI.Models;
using TeknikServisAPI.Services;
using TeknikServisAPI.Tests.Helpers;

namespace TeknikServisAPI.Tests.Services
{
    /// <summary>
    /// Tests for ShipmentService - sevkiyat yönetimi component
    /// </summary>
    public class ShipmentServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ShipmentService _shipmentService;

        public ShipmentServiceTests()
        {
            _context = InMemoryDbHelper.CreateContext($"ShipmentServiceTest_{Guid.NewGuid()}");
            _shipmentService = new ShipmentService(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region GetAllShipmentsAsync Tests

        [Fact]
        public async Task GetAllShipmentsAsync_ReturnsAllShipments()
        {
            // Arrange
            _context.Shipments.AddRange(
                new Shipment 
                { 
                    Id = 1, 
                    ShipmentType = "Giden",
                    TrackingNumber = "TRK001", 
                    Status = ShipmentStatuses.Preparing,
                    CargoCompany = "Yurtiçi Kargo",
                    CreatedAt = DateTime.UtcNow 
                },
                new Shipment 
                { 
                    Id = 2, 
                    ShipmentType = "Gelen",
                    TrackingNumber = "TRK002", 
                    Status = ShipmentStatuses.Delivered,
                    CargoCompany = "Aras Kargo",
                    CreatedAt = DateTime.UtcNow 
                }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _shipmentService.GetAllShipmentsAsync();

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAllShipmentsAsync_EmptyDatabase_ReturnsEmptyList()
        {
            // Act
            var result = await _shipmentService.GetAllShipmentsAsync();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllShipmentsAsync_IncludesItems()
        {
            // Arrange
            var shipment = new Shipment 
            { 
                Id = 1, 
                ShipmentType = "Giden",
                TrackingNumber = "TRK001", 
                Status = ShipmentStatuses.Preparing,
                CargoCompany = "Kargo",
                CreatedAt = DateTime.UtcNow,
                Items = new List<ShipmentItem>
                {
                    new ShipmentItem { Id = 1, ItemType = "Device", Description = "Test Device", Quantity = 1 },
                    new ShipmentItem { Id = 2, ItemType = "Stock", Description = "Test Stock", Quantity = 5 }
                }
            };
            _context.Shipments.Add(shipment);
            await _context.SaveChangesAsync();

            // Act
            var result = await _shipmentService.GetAllShipmentsAsync();

            // Assert
            result.Should().HaveCount(1);
            result.First().Items.Should().HaveCount(2);
        }

        #endregion

        #region GetShipmentByIdAsync Tests

        [Fact]
        public async Task GetShipmentByIdAsync_ExistingId_ReturnsShipment()
        {
            // Arrange
            var shipment = new Shipment 
            { 
                Id = 1, 
                ShipmentType = "Giden",
                TrackingNumber = "TRK001", 
                Status = ShipmentStatuses.Preparing,
                CargoCompany = "Yurtiçi Kargo",
                CreatedAt = DateTime.UtcNow 
            };
            _context.Shipments.Add(shipment);
            await _context.SaveChangesAsync();

            // Act
            var result = await _shipmentService.GetShipmentByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result!.TrackingNumber.Should().Be("TRK001");
        }

        [Fact]
        public async Task GetShipmentByIdAsync_NonExistingId_ReturnsNull()
        {
            // Act
            var result = await _shipmentService.GetShipmentByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region CreateShipmentAsync Tests

        [Fact]
        public async Task CreateShipmentAsync_ValidData_CreatesShipment()
        {
            // Arrange
            var createDto = new CreateShipmentDto
            {
                ShipmentType = "Giden",
                TrackingNumber = "NEW001",
                CargoCompany = "MNG Kargo",
                Notes = "Test açıklaması",
                Items = new List<CreateShipmentItemDto>
                {
                    new CreateShipmentItemDto { ItemType = "Device", Description = "Test Device", Quantity = 1 }
                }
            };

            // Act
            var result = await _shipmentService.CreateShipmentAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.TrackingNumber.Should().Be("NEW001");
            result.Status.Should().Be(ShipmentStatuses.Preparing);
            result.Items.Should().HaveCount(1);
            
            // Verify persisted
            var fromDb = await _context.Shipments.FindAsync(result.Id);
            fromDb.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateShipmentAsync_SetsCreatedAt()
        {
            // Arrange
            var createDto = new CreateShipmentDto
            {
                ShipmentType = "Giden",
                TrackingNumber = "NEW001",
                CargoCompany = "Kargo",
                Items = new List<CreateShipmentItemDto>()
            };
            var beforeCreate = DateTime.UtcNow;

            // Act
            var result = await _shipmentService.CreateShipmentAsync(createDto);

            // Assert
            result.CreatedAt.Should().BeAfter(beforeCreate.AddSeconds(-1));
            result.CreatedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
        }

        #endregion

        #region UpdateShipmentAsync Tests

        [Fact]
        public async Task UpdateShipmentAsync_ExistingShipment_UpdatesFields()
        {
            // Arrange
            var shipment = new Shipment 
            { 
                Id = 1, 
                ShipmentType = "Giden",
                TrackingNumber = "TRK001", 
                Status = ShipmentStatuses.Preparing,
                CargoCompany = "Yurtiçi Kargo",
                CreatedAt = DateTime.UtcNow 
            };
            _context.Shipments.Add(shipment);
            await _context.SaveChangesAsync();

            var updateDto = new UpdateShipmentDto
            {
                TrackingNumber = "UPDATED001",
                Notes = "Updated notes"
            };

            // Act
            var result = await _shipmentService.UpdateShipmentAsync(1, updateDto);

            // Assert
            result.Should().NotBeNull();
            result!.TrackingNumber.Should().Be("UPDATED001");
            result.Notes.Should().Be("Updated notes");
        }

        [Fact]
        public async Task UpdateShipmentAsync_NonExistingShipment_ReturnsNull()
        {
            // Arrange
            var updateDto = new UpdateShipmentDto { TrackingNumber = "Updated" };

            // Act
            var result = await _shipmentService.UpdateShipmentAsync(999, updateDto);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region UpdateShipmentStatusAsync Tests

        [Fact]
        public async Task UpdateShipmentStatusAsync_Shipped_SetsShippedAt()
        {
            // Arrange
            var shipment = new Shipment 
            { 
                Id = 1, 
                ShipmentType = "Giden",
                TrackingNumber = "TRK001", 
                Status = ShipmentStatuses.Preparing,
                CargoCompany = "Kargo",
                CreatedAt = DateTime.UtcNow 
            };
            _context.Shipments.Add(shipment);
            await _context.SaveChangesAsync();

            // Act
            var result = await _shipmentService.UpdateShipmentStatusAsync(1, ShipmentStatuses.Shipped);

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be(ShipmentStatuses.Shipped);
            result.ShippedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateShipmentStatusAsync_Delivered_SetsDeliveredAt()
        {
            // Arrange
            var shipment = new Shipment 
            { 
                Id = 1, 
                ShipmentType = "Giden",
                TrackingNumber = "TRK001", 
                Status = ShipmentStatuses.Shipped,
                CargoCompany = "Kargo",
                CreatedAt = DateTime.UtcNow,
                ShippedAt = DateTime.UtcNow.AddDays(-1)
            };
            _context.Shipments.Add(shipment);
            await _context.SaveChangesAsync();

            // Act
            var result = await _shipmentService.UpdateShipmentStatusAsync(1, ShipmentStatuses.Delivered);

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be(ShipmentStatuses.Delivered);
            result.DeliveredAt.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateShipmentStatusAsync_NonExistingShipment_ReturnsNull()
        {
            // Act
            var result = await _shipmentService.UpdateShipmentStatusAsync(999, ShipmentStatuses.Shipped);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region DeleteShipmentAsync Tests

        [Fact]
        public async Task DeleteShipmentAsync_ExistingShipment_SetsCancelled()
        {
            // Arrange
            var shipment = new Shipment 
            { 
                Id = 1, 
                ShipmentType = "Giden",
                TrackingNumber = "TRK001", 
                Status = ShipmentStatuses.Preparing,
                CargoCompany = "Kargo",
                CreatedAt = DateTime.UtcNow 
            };
            _context.Shipments.Add(shipment);
            await _context.SaveChangesAsync();

            // Act
            var result = await _shipmentService.DeleteShipmentAsync(1);

            // Assert
            result.Should().BeTrue();
            var fromDb = await _context.Shipments.FindAsync(1);
            fromDb!.Status.Should().Be(ShipmentStatuses.Cancelled, because: "soft delete sets status to cancelled");
        }

        [Fact]
        public async Task DeleteShipmentAsync_NonExistingShipment_ReturnsFalse()
        {
            // Act
            var result = await _shipmentService.DeleteShipmentAsync(999);

            // Assert
            result.Should().BeFalse();
        }

        #endregion
    }
}
