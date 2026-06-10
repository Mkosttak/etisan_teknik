using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MediatR;
using Moq;
using TeknikServisAPI.Constants;
using TeknikServisAPI.Data;
using TeknikServisAPI.DTOs;
using TeknikServisAPI.Models;
using TeknikServisAPI.Services;
using TeknikServisAPI.Tests.Helpers;

namespace TeknikServisAPI.Tests.Services
{
    /// <summary>
    /// Tests for RequestService - talep yönetimi component
    /// </summary>
    public class RequestServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly RequestService _requestService;
        private readonly Mock<ILogger<RequestService>> _loggerMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<IAuditLogService> _auditLogServiceMock;
        private readonly Mock<IMediator> _mediatorMock;

        public RequestServiceTests()
        {
            _context = InMemoryDbHelper.CreateContext($"RequestServiceTest_{Guid.NewGuid()}");
            _loggerMock = new Mock<ILogger<RequestService>>();
            _notificationServiceMock = new Mock<INotificationService>();
            _auditLogServiceMock = new Mock<IAuditLogService>();
            _mediatorMock = new Mock<IMediator>();
            _requestService = new RequestService(
                _context, 
                _loggerMock.Object, 
                _notificationServiceMock.Object,
                _auditLogServiceMock.Object,
                _mediatorMock.Object
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<(Request request, User user)> CreateTestRequest(string status = "Beklemede")
        {
            InMemoryDbHelper.SeedTestData(_context);
            var user = await _context.Users.FirstAsync();
            var institution = await _context.Institutions.FirstAsync();

            var request = new Request
            {
                Id = 100,
                InstitutionName = institution.Name,
                Description = "Test talep",
                Status = status,
                CreatedByUserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };
            _context.Requests.Add(request);
            await _context.SaveChangesAsync();
            return (request, user);
        }

        #region GetAllRequestsAsync Tests

        [Fact]
        public async Task GetAllRequestsAsync_ReturnsAllRequests()
        {
            // Arrange
            await CreateTestRequest();

            // Act
            var result = await _requestService.GetAllRequestsAsync();

            // Assert
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetAllRequestsAsync_EmptyDatabase_ReturnsEmptyList()
        {
            // Act
            var result = await _requestService.GetAllRequestsAsync();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region GetRequestByIdAsync Tests

        [Fact]
        public async Task GetRequestByIdAsync_ExistingId_ReturnsRequest()
        {
            // Arrange
            var (request, _) = await CreateTestRequest();

            // Act
            var result = await _requestService.GetRequestByIdAsync(request.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Description.Should().Be("Test talep");
        }

        [Fact]
        public async Task GetRequestByIdAsync_NonExistingId_ReturnsNull()
        {
            // Act
            var result = await _requestService.GetRequestByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region CreateRequestAsync Tests

        [Fact]
        public async Task CreateRequestAsync_ValidData_CreatesRequest()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            var user = await _context.Users.FirstAsync();
            var institution = await _context.Institutions.FirstAsync();

            var createDto = new CreateRequestDto
            {
                Description = "Yeni talep açıklaması",
                InstitutionName = institution.Name,
                ContactPerson = "Test Kişi",
                ContactPhone = "5551234567",
                ExtraItems = "Test Parça"
            };

            // Act
            var result = await _requestService.CreateRequestAsync(createDto, user.Id);

            // Assert
            result.Should().NotBeNull();
            result.Description.Should().Be("Yeni talep açıklaması");
            result.Status.Should().Be(RequestStatuses.Pending);
        }

        [Fact]
        public async Task CreateRequestAsync_UrgentRequest_SetsIsUrgent()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            var user = await _context.Users.FirstAsync();
            var institution = await _context.Institutions.FirstAsync();

            var createDto = new CreateRequestDto
            {
                Description = "Acil talep",
                InstitutionName = institution.Name,
                IsUrgent = true
            };

            // Act
            var result = await _requestService.CreateRequestAsync(createDto, user.Id);

            // Assert
            result.Should().NotBeNull();
            result.IsUrgent.Should().BeTrue();
        }

        [Fact]
        public async Task CreateRequestAsync_WithStockItems_AddsStockItemsToRequest()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            var user = await _context.Users.FirstAsync();
            var stockItem = await _context.StockItems.FirstAsync();

            var createDto = new CreateRequestDto
            {
                Description = "Malzeme talebi",
                InstitutionName = "Test Kurum",
                StockItems = new List<CreateRequestStockItemDto>
                {
                    new CreateRequestStockItemDto { StockItemId = stockItem.Id, Quantity = 5 }
                }
            };

            // Act
            var result = await _requestService.CreateRequestAsync(createDto, user.Id);

            // Assert
            result.Should().NotBeNull();
            result.StockItems.Should().HaveCount(1);
            result.StockItems.First().Quantity.Should().Be(5);
        }

        #endregion

        #region UpdateRequestStatusAsync Tests

        [Fact]
        public async Task UpdateRequestStatusAsync_ValidTransition_UpdatesStatus()
        {
            // Arrange
            var (request, user) = await CreateTestRequest("Beklemede");

            // Act
            var result = await _requestService.UpdateRequestStatusAsync(
                request.Id, 
                RequestStatuses.InProgress, 
                user.Id
            );

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be(RequestStatuses.InProgress);
        }

        [Fact]
        public async Task UpdateRequestStatusAsync_NonExistingRequest_ReturnsNull()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            var user = await _context.Users.FirstAsync();

            // Act
            var result = await _requestService.UpdateRequestStatusAsync(
                999, 
                RequestStatuses.InProgress, 
                user.Id
            );

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateRequestStatusAsync_Completed_SetsCompletedAt()
        {
            // Arrange
            var (request, user) = await CreateTestRequest("Devam Ediyor");

            // Act
            var result = await _requestService.UpdateRequestStatusAsync(
                request.Id, 
                RequestStatuses.Completed, 
                user.Id
            );

            // Assert
            result.Should().NotBeNull();
            result!.CompletedAt.Should().NotBeNull();
        }

        #endregion

        #region DeleteRequestAsync Tests

        [Fact]
        public async Task DeleteRequestAsync_ExistingRequest_ReturnsTrue()
        {
            // Arrange
            var (request, user) = await CreateTestRequest();

            // Act
            var result = await _requestService.DeleteRequestAsync(request.Id, user.Id);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteRequestAsync_NonExistingRequest_ReturnsFalse()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            var user = await _context.Users.FirstAsync();

            // Act
            var result = await _requestService.DeleteRequestAsync(999, user.Id);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteRequestAsync_RemovesRequest()
        {
            // Arrange
            var (request, user) = await CreateTestRequest();

            // Act
            await _requestService.DeleteRequestAsync(request.Id, user.Id);

            // Assert
            var fromDb = await _context.Requests.FindAsync(request.Id);
            fromDb.Should().BeNull();
        }

        #endregion
    }
}
