using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TeknikServisAPI.Data;
using TeknikServisAPI.DTOs;
using TeknikServisAPI.Models;
using TeknikServisAPI.Services;
using TeknikServisAPI.Tests.Helpers;

namespace TeknikServisAPI.Tests.Services
{
    /// <summary>
    /// Tests for StockService - kritik stok yönetimi component
    /// Gerçek servdis metodlarına göre yazılmış testler
    /// </summary>
    public class StockServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly StockService _stockService;
        private readonly Mock<INotificationService> _notificationServiceMock;

        public StockServiceTests()
        {
            _context = InMemoryDbHelper.CreateContext($"StockServiceTest_{Guid.NewGuid()}");
            _notificationServiceMock = new Mock<INotificationService>();
            _stockService = new StockService(_context, _notificationServiceMock.Object);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region GetAllStockItemsAsync Tests

        [Fact]
        public async Task GetAllStockItemsAsync_ReturnsAllActiveStockItems()
        {
            // Arrange
            _context.StockItems.AddRange(
                new StockItem { Id = 1, ItemName = "Item 1", CurrentStock = 10, MinimumStock = 5, Unit = "Adet", IsActive = true, CreatedAt = DateTime.UtcNow },
                new StockItem { Id = 2, ItemName = "Item 2", CurrentStock = 20, MinimumStock = 10, Unit = "Adet", IsActive = true, CreatedAt = DateTime.UtcNow },
                new StockItem { Id = 3, ItemName = "Inactive Item", CurrentStock = 5, MinimumStock = 2, Unit = "Adet", IsActive = false, CreatedAt = DateTime.UtcNow }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _stockService.GetAllStockItemsAsync();

            // Assert
            result.Should().HaveCount(2, because: "only active items should be returned");
        }

        [Fact]
        public async Task GetAllStockItemsAsync_EmptyDatabase_ReturnsEmptyList()
        {
            // Act
            var result = await _stockService.GetAllStockItemsAsync();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region GetStockItemByIdAsync Tests

        [Fact]
        public async Task GetStockItemByIdAsync_ExistingId_ReturnsItem()
        {
            // Arrange
            var stockItem = new StockItem 
            { 
                Id = 1, 
                ItemName = "Test Item", 
                CurrentStock = 50, 
                MinimumStock = 10, 
                Unit = "Adet", 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow 
            };
            _context.StockItems.Add(stockItem);
            await _context.SaveChangesAsync();

            // Act
            var result = await _stockService.GetStockItemByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result!.ItemName.Should().Be("Test Item");
            result.CurrentStock.Should().Be(50);
        }

        [Fact]
        public async Task GetStockItemByIdAsync_NonExistingId_ReturnsNull()
        {
            // Act
            var result = await _stockService.GetStockItemByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetStockItemByIdAsync_InactiveItem_ReturnsNull()
        {
            // Arrange
            var stockItem = new StockItem 
            { 
                Id = 1, 
                ItemName = "Inactive Item", 
                CurrentStock = 50, 
                MinimumStock = 10, 
                Unit = "Adet", 
                IsActive = false, 
                CreatedAt = DateTime.UtcNow 
            };
            _context.StockItems.Add(stockItem);
            await _context.SaveChangesAsync();

            // Act
            var result = await _stockService.GetStockItemByIdAsync(1);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region CreateStockItemAsync Tests

        [Fact]
        public async Task CreateStockItemAsync_ValidData_CreatesStockItem()
        {
            // Arrange
            var createDto = new CreateStockItemDto
            {
                ItemName = "New Item",
                CurrentStock = 100,
                MinimumStock = 10,
                Description = "Test Description"
            };

            // Act
            var result = await _stockService.CreateStockItemAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.ItemName.Should().Be("New Item");
            result.CurrentStock.Should().Be(100);
            result.IsActive.Should().BeTrue();
            
            // Verify it's persisted
            var fromDb = await _context.StockItems.FindAsync(result.Id);
            fromDb.Should().NotBeNull();
        }

        #endregion

        #region UpdateStockItemAsync Tests

        [Fact]
        public async Task UpdateStockItemAsync_ExistingItem_UpdatesSuccessfully()
        {
            // Arrange
            var stockItem = new StockItem 
            { 
                Id = 1, 
                ItemName = "Original Name", 
                CurrentStock = 50, 
                MinimumStock = 10, 
                Unit = "Adet", 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow 
            };
            _context.StockItems.Add(stockItem);
            await _context.SaveChangesAsync();

            var updateDto = new UpdateStockItemDto
            {
                ItemName = "Updated Name",
                MinimumStock = 15
            };

            // Act
            var result = await _stockService.UpdateStockItemAsync(1, updateDto);

            // Assert
            result.Should().NotBeNull();
            result!.ItemName.Should().Be("Updated Name");
            result.MinimumStock.Should().Be(15);
        }

        [Fact]
        public async Task UpdateStockItemAsync_NonExistingItem_ReturnsNull()
        {
            // Arrange
            var updateDto = new UpdateStockItemDto { ItemName = "Updated" };

            // Act
            var result = await _stockService.UpdateStockItemAsync(999, updateDto);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateStockItemAsync_LowStock_SendsNotification()
        {
            // Arrange
            var stockItem = new StockItem 
            { 
                Id = 1, 
                ItemName = "Low Stock Item", 
                CurrentStock = 50, 
                MinimumStock = 10, 
                Unit = "Adet", 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow 
            };
            _context.StockItems.Add(stockItem);
            await _context.SaveChangesAsync();

            var updateDto = new UpdateStockItemDto
            {
                CurrentStock = 5 // Below minimum
            };

            // Act
            await _stockService.UpdateStockItemAsync(1, updateDto);

            // Assert - Notification should be sent
            _notificationServiceMock.Verify(
                x => x.CreateNotificationAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<int?>(),
                    It.IsAny<string?>(),
                    It.IsAny<int?>(),
                    It.IsAny<string?>()
                ),
                Times.Once
            );
        }

        #endregion

        #region GetLowStockItemsAsync Tests

        [Fact]
        public async Task GetLowStockItemsAsync_ReturnsItemsBelowOrAtMinimum()
        {
            // Arrange
            _context.StockItems.AddRange(
                new StockItem { Id = 1, ItemName = "Low Stock Item", CurrentStock = 5, MinimumStock = 10, Unit = "Adet", IsActive = true, CreatedAt = DateTime.UtcNow },
                new StockItem { Id = 2, ItemName = "Normal Stock Item", CurrentStock = 50, MinimumStock = 10, Unit = "Adet", IsActive = true, CreatedAt = DateTime.UtcNow },
                new StockItem { Id = 3, ItemName = "At Minimum", CurrentStock = 20, MinimumStock = 20, Unit = "Adet", IsActive = true, CreatedAt = DateTime.UtcNow },
                new StockItem { Id = 4, ItemName = "Low but Inactive", CurrentStock = 1, MinimumStock = 10, Unit = "Adet", IsActive = false, CreatedAt = DateTime.UtcNow }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _stockService.GetLowStockItemsAsync();

            // Assert
            result.Should().HaveCount(2, because: "two active items are at or below minimum stock");
        }

        #endregion

        #region DeleteStockItemAsync Tests

        [Fact]
        public async Task DeleteStockItemAsync_ExistingItem_SetsInactive()
        {
            // Arrange
            var stockItem = new StockItem 
            { 
                Id = 1, 
                ItemName = "To Delete", 
                CurrentStock = 50, 
                MinimumStock = 10, 
                Unit = "Adet", 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow 
            };
            _context.StockItems.Add(stockItem);
            await _context.SaveChangesAsync();

            // Act
            var result = await _stockService.DeleteStockItemAsync(1);

            // Assert
            result.Should().BeTrue();
            var fromDb = await _context.StockItems.FindAsync(1);
            fromDb!.IsActive.Should().BeFalse("soft delete should set IsActive to false");
        }

        [Fact]
        public async Task DeleteStockItemAsync_NonExistingItem_ReturnsFalse()
        {
            // Act
            var result = await _stockService.DeleteStockItemAsync(999);

            // Assert
            result.Should().BeFalse();
        }

        #endregion
    }
}
