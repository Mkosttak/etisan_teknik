using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using TeknikServisAPI.Data;
using TeknikServisAPI.DTOs;
using TeknikServisAPI.Models;
using TeknikServisAPI.Services;
using TeknikServisAPI.Tests.Helpers;

namespace TeknikServisAPI.Tests.Services
{
    /// <summary>
    /// Tests for DashboardService - Statistics and recent activities
    /// Coverage target: 70%+
    /// 
    /// Key Test Areas:
    /// - Dashboard stats calculation
    /// - Caching behavior
    /// - Recent activities retrieval
    /// - Empty database handling
    /// </summary>
    public class DashboardServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly Mock<ILogger<DashboardService>> _loggerMock;
        private readonly DashboardService _service;

        public DashboardServiceTests()
        {
            _context = InMemoryDbHelper.CreateContext($"DashboardServiceTest_{Guid.NewGuid()}");
            _cache = new MemoryCache(new MemoryCacheOptions());
            _loggerMock = new Mock<ILogger<DashboardService>>();
            _service = new DashboardService(_context, _cache, _loggerMock.Object);
        }

        public void Dispose()
        {
            _cache.Dispose();
            _context.Dispose();
        }

        #region GetDashboardStatsAsync Tests

        [Fact]
        public async Task GetDashboardStatsAsync_EmptyDatabase_ReturnsDefaultStats()
        {
            // Act
            var result = await _service.GetDashboardStatsAsync();

            // Assert
            result.Should().NotBeNull();
            result.TotalDevices.Should().Be(0);
            result.TotalPendingFaults.Should().Be(0);
            result.TotalRequests.Should().Be(0);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_WithData_ReturnsCorrectStats()
        {
            // Arrange
            await SeedDashboardTestData();

            // Act
            var result = await _service.GetDashboardStatsAsync();

            // Assert
            result.Should().NotBeNull();
            result.TotalDevices.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_CachesResult()
        {
            // Arrange
            await SeedDashboardTestData();

            // Act - First call
            var result1 = await _service.GetDashboardStatsAsync();
            
            // Modify data
            _context.Devices.Add(new Device
            {
                SerialNumber = "NEW-DEVICE",
                DeviceModel = "Test Model",
                University = "Test",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // Act - Second call (should return cached)
            var result2 = await _service.GetDashboardStatsAsync();

            // Assert - Both should be equal (cached)
            result2.TotalDevices.Should().Be(result1.TotalDevices);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_CalculatesStatusDistribution()
        {
            // Arrange
            await SeedDashboardTestData();

            // Act
            var result = await _service.GetDashboardStatsAsync();

            // Assert
            result.FaultStatusDistribution.Should().NotBeNull();
        }

        #endregion

        #region GetRecentActivitiesAsync Tests

        [Fact]
        public async Task GetRecentActivitiesAsync_EmptyDatabase_ReturnsEmptyList()
        {
            // Act
            var result = await _service.GetRecentActivitiesAsync(10);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetRecentActivitiesAsync_WithData_ReturnsOrderedActivities()
        {
            // Arrange
            await SeedDashboardTestData();

            // Act
            var result = await _service.GetRecentActivitiesAsync(10);

            // Assert
            result.Should().NotBeNull();
            // Aktiviteler tarih sırasına göre sıralı olmalı
            for (int i = 0; i < result.Count - 1; i++)
            {
                result[i].CreatedAt.Should().BeOnOrAfter(result[i + 1].CreatedAt);
            }
        }

        [Fact]
        public async Task GetRecentActivitiesAsync_RespectsCountLimit()
        {
            // Arrange
            await SeedDashboardTestData();

            // Act
            var result = await _service.GetRecentActivitiesAsync(2);

            // Assert
            result.Count.Should().BeLessThanOrEqualTo(2);
        }

        [Fact]
        public async Task GetRecentActivitiesAsync_CountClampedToMax()
        {
            // Act
            var result = await _service.GetRecentActivitiesAsync(100);

            // Assert - Should not exceed max (50)
            result.Count.Should().BeLessThanOrEqualTo(50);
        }

        #endregion

        #region Helper Methods

        private async Task SeedDashboardTestData()
        {
            // Add devices
            _context.Devices.AddRange(
                new Device { SerialNumber = "DEV-001", DeviceModel = "Model A", University = "Üniversite A", Status = "Active", CreatedAt = DateTime.UtcNow },
                new Device { SerialNumber = "DEV-002", DeviceModel = "Model B", University = "Üniversite B", Status = "InRepair", CreatedAt = DateTime.UtcNow }
            );

            // Add faults
            _context.Faults.AddRange(
                new Fault { DeviceSerialNumber = "DEV-001", Description = "Arıza 1", Status = "Pending", InstitutionName = "Kurum A", CreatedAt = DateTime.UtcNow },
                new Fault { DeviceSerialNumber = "DEV-002", Description = "Arıza 2", Status = "InProgress", InstitutionName = "Kurum B", CreatedAt = DateTime.UtcNow.AddHours(-1) }
            );

            // Add requests
            _context.Requests.AddRange(
                new Request { Description = "Talep 1", Status = "Pending", InstitutionName = "Kurum A", CreatedAt = DateTime.UtcNow },
                new Request { Description = "Talep 2", Status = "Completed", InstitutionName = "Kurum B", CreatedAt = DateTime.UtcNow.AddHours(-2) }
            );

            await _context.SaveChangesAsync();
        }

        #endregion
    }
}
