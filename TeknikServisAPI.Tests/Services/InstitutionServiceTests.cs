using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TeknikServisAPI.Data;
using TeknikServisAPI.Models;
using TeknikServisAPI.Services;
using TeknikServisAPI.Tests.Helpers;

namespace TeknikServisAPI.Tests.Services
{
    /// <summary>
    /// Tests for InstitutionService - Institution-based aggregation and reporting
    /// Coverage target: 70%+
    /// 
    /// Key Test Areas:
    /// - Institution summary calculation
    /// - Request/Fault/ServiceForm/WorkItem retrieval by institution
    /// - Pagination handling
    /// - URL-encoded institution names
    /// </summary>
    public class InstitutionServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<InstitutionService>> _loggerMock;
        private readonly InstitutionService _service;

        public InstitutionServiceTests()
        {
            _context = InMemoryDbHelper.CreateContext($"InstitutionServiceTest_{Guid.NewGuid()}");
            _loggerMock = new Mock<ILogger<InstitutionService>>();
            _service = new InstitutionService(_context, _loggerMock.Object);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region GetSummaryAsync Tests

        [Fact]
        public async Task GetSummaryAsync_NonExistentInstitution_ReturnsZeroStats()
        {
            // Act
            var result = await _service.GetSummaryAsync("Yok Kurum");

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Yok Kurum");
            result.TotalRequests.Should().Be(0);
            result.TotalFaults.Should().Be(0);
            result.TotalServiceForms.Should().Be(0);
            result.TotalWorkItems.Should().Be(0);
        }

        [Fact]
        public async Task GetSummaryAsync_ExistingInstitution_ReturnsCorrectStats()
        {
            // Arrange
            var institutionName = "Test Üniversitesi";
            await SeedInstitutionTestData(institutionName);

            // Act
            var result = await _service.GetSummaryAsync(institutionName);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be(institutionName);
            result.TotalRequests.Should().BeGreaterThan(0);
            result.TotalFaults.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetSummaryAsync_UrlEncodedName_DecodesCorrectly()
        {
            // Arrange
            var actualName = "İstanbul Üniversitesi";
            var encodedName = Uri.EscapeDataString(actualName);
            await SeedInstitutionTestData(actualName);

            // Act
            var result = await _service.GetSummaryAsync(encodedName);

            // Assert
            result.Name.Should().Be(actualName);
            result.TotalRequests.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetSummaryAsync_OtherInstitution_MatchesOtherName()
        {
            // Arrange - "DİĞER" kurumu ile farklı OtherName
            var otherName = "Özel Kurum";
            _context.Requests.Add(new Request
            {
                Description = "Test",
                Status = "Pending",
                InstitutionName = "DİĞER",
                InstitutionOtherName = otherName,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetSummaryAsync(otherName);

            // Assert
            result.TotalRequests.Should().Be(1);
        }

        #endregion

        #region GetRequestsAsync Tests

        [Fact]
        public async Task GetRequestsAsync_NonExistentInstitution_ReturnsEmptyList()
        {
            // Act
            var result = await _service.GetRequestsAsync("Yok Kurum");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetRequestsAsync_ExistingInstitution_ReturnsRequests()
        {
            // Arrange
            var institutionName = "Test Üniversitesi";
            await SeedInstitutionTestData(institutionName);

            // Act
            var result = await _service.GetRequestsAsync(institutionName);

            // Assert
            result.Should().NotBeEmpty();
            result.All(r => r.InstitutionName == institutionName).Should().BeTrue();
        }

        [Fact]
        public async Task GetRequestsAsync_Pagination_ReturnsCorrectPage()
        {
            // Arrange
            var institutionName = "Test Üniversitesi";
            for (int i = 0; i < 10; i++)
            {
                _context.Requests.Add(new Request
                {
                    Description = $"Talep {i}",
                    Status = "Pending",
                    InstitutionName = institutionName,
                    CreatedAt = DateTime.UtcNow.AddHours(-i)
                });
            }
            await _context.SaveChangesAsync();

            // Act
            var page1 = await _service.GetRequestsAsync(institutionName, page: 1, pageSize: 5);
            var page2 = await _service.GetRequestsAsync(institutionName, page: 2, pageSize: 5);

            // Assert
            page1.Should().HaveCount(5);
            page2.Should().HaveCount(5);
            page1.First().Id.Should().NotBe(page2.First().Id);
        }

        #endregion

        #region GetFaultsAsync Tests

        [Fact]
        public async Task GetFaultsAsync_ExistingInstitution_ReturnsFaults()
        {
            // Arrange
            var institutionName = "Test Üniversitesi";
            await SeedInstitutionTestData(institutionName);

            // Act
            var result = await _service.GetFaultsAsync(institutionName);

            // Assert
            result.Should().NotBeEmpty();
        }

        #endregion

        #region GetServiceFormsAsync Tests

        [Fact]
        public async Task GetServiceFormsAsync_ExistingInstitution_ReturnsForms()
        {
            // Arrange
            var institutionName = "Test Üniversitesi";
            _context.ServiceForms.Add(new ServiceForm
            {
                InstitutionName = institutionName,
                FormDate = DateTime.UtcNow,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetServiceFormsAsync(institutionName);

            // Assert
            result.Should().NotBeEmpty();
        }

        #endregion

        #region GetWorkItemsAsync Tests

        [Fact]
        public async Task GetWorkItemsAsync_ExistingInstitution_ReturnsWorkItems()
        {
            // Arrange
            var institutionName = "Test Üniversitesi";
            _context.WorkItems.Add(new WorkItem
            {
                Title = "Test İş",
                InstitutionName = institutionName,
                Kind = WorkItemKind.Request,
                Stage = WorkItemStage.Backlog,
                Priority = WorkItemPriority.Normal,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetWorkItemsAsync(institutionName);

            // Assert
            result.Should().NotBeEmpty();
            result.First().Title.Should().Be("Test İş");
        }

        #endregion

        #region GetStockMovementsAsync Tests

        [Fact]
        public async Task GetStockMovementsAsync_ExistingInstitution_ReturnsMovements()
        {
            // Arrange
            var institutionName = "Test Üniversitesi";
            var stockItem = new StockItem
            {
                ItemName = "Test Malzeme",
                CurrentStock = 100,
                MinimumStock = 10,
                Unit = "Adet",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.StockItems.Add(stockItem);
            await _context.SaveChangesAsync();

            _context.StockMovements.Add(new StockMovement
            {
                StockItemId = stockItem.Id,
                MovementType = "Out",
                Quantity = 5,
                InstitutionName = institutionName,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetStockMovementsAsync(institutionName);

            // Assert
            result.Should().NotBeEmpty();
        }

        #endregion

        #region Helper Methods

        private async Task SeedInstitutionTestData(string institutionName)
        {
            // Add requests
            _context.Requests.AddRange(
                new Request { Description = "Talep 1", Status = "Pending", InstitutionName = institutionName, CreatedAt = DateTime.UtcNow },
                new Request { Description = "Talep 2", Status = "Completed", InstitutionName = institutionName, CreatedAt = DateTime.UtcNow }
            );

            // Add faults
            _context.Faults.AddRange(
                new Fault { DeviceSerialNumber = "SN-001", Description = "Arıza 1", Status = "Pending", InstitutionName = institutionName, CreatedAt = DateTime.UtcNow },
                new Fault { DeviceSerialNumber = "SN-002", Description = "Arıza 2", Status = "InProgress", InstitutionName = institutionName, CreatedAt = DateTime.UtcNow }
            );

            await _context.SaveChangesAsync();
        }

        #endregion
    }
}
