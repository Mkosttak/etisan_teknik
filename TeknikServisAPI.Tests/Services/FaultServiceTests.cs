using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
    /// Tests for FaultService - arıza yönetimi component
    /// </summary>
    public class FaultServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly FaultService _faultService;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<IAuditLogService> _auditLogServiceMock;

        public FaultServiceTests()
        {
            _context = InMemoryDbHelper.CreateContext($"FaultServiceTest_{Guid.NewGuid()}");
            _notificationServiceMock = new Mock<INotificationService>();
            _auditLogServiceMock = new Mock<IAuditLogService>();
            _faultService = new FaultService(_context, _notificationServiceMock.Object, _auditLogServiceMock.Object);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<(Fault fault, User user)> CreateTestFault(string status = "Beklemede")
        {
            InMemoryDbHelper.SeedTestData(_context);
            var user = await _context.Users.FirstAsync();
            var institution = await _context.Institutions.FirstAsync();

            var fault = new Fault
            {
                Id = 100,
                DeviceSerialNumber = "SN123456",
                InstitutionName = institution.Name,
                Description = "Test arıza",
                Status = status,
                AssignedToUserId = user.Id,
                CreatedByUserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };
            _context.Faults.Add(fault);
            await _context.SaveChangesAsync();
            return (fault, user);
        }

        #region GetAllFaultsAsync Tests

        [Fact]
        public async Task GetAllFaultsAsync_ReturnsAllFaults()
        {
            // Arrange
            await CreateTestFault();

            // Act
            var result = await _faultService.GetAllFaultsAsync();

            // Assert
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetAllFaultsAsync_EmptyDatabase_ReturnsEmptyList()
        {
            // Act
            var result = await _faultService.GetAllFaultsAsync();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region GetFaultByIdAsync Tests

        [Fact]
        public async Task GetFaultByIdAsync_ExistingId_ReturnsFault()
        {
            // Arrange
            var (fault, _) = await CreateTestFault();

            // Act
            var result = await _faultService.GetFaultByIdAsync(fault.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Description.Should().Be("Test arıza");
        }

        [Fact]
        public async Task GetFaultByIdAsync_NonExistingId_ReturnsNull()
        {
            // Act
            var result = await _faultService.GetFaultByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region CreateFaultAsync Tests

        [Fact]
        public async Task CreateFaultAsync_ValidData_CreatesFault()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            var user = await _context.Users.FirstAsync();
            var institution = await _context.Institutions.FirstAsync();

            var createDto = new CreateFaultDto
            {
                DeviceSerialNumber = "NEWSN123",
                Description = "Yeni arıza açıklaması",
                InstitutionName = institution.Name
            };

            // Act
            var result = await _faultService.CreateFaultAsync(createDto, user.Id);

            // Assert
            result.Should().NotBeNull();
            result.Description.Should().Be("Yeni arıza açıklaması");
            result.Status.Should().Be(FaultStatuses.Created);
        }

        #endregion

        #region UpdateFaultStatusAsync Tests

        [Fact]
        public async Task UpdateFaultStatusAsync_ValidTransition_UpdatesStatus()
        {
            // Arrange
            var (fault, user) = await CreateTestFault("Beklemede");

            // Act
            var result = await _faultService.UpdateFaultStatusAsync(
                fault.Id, 
                FaultStatuses.InProgress, 
                "İşleme alındı",
                null,
                user.Id
            );

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be(FaultStatuses.InProgress);
        }

        [Fact]
        public async Task UpdateFaultStatusAsync_NonExistingFault_ReturnsNull()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            var user = await _context.Users.FirstAsync();

            // Act
            var result = await _faultService.UpdateFaultStatusAsync(
                999, 
                FaultStatuses.InProgress, 
                null,
                null,
                user.Id
            );

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateFaultStatusAsync_CreatesHistoryEntry()
        {
            // Arrange
            var (fault, user) = await CreateTestFault("Beklemede");

            // Act
            await _faultService.UpdateFaultStatusAsync(
                fault.Id, 
                FaultStatuses.InProgress, 
                "Test yorum",
                null,
                user.Id
            );

            // Assert
            var history = await _context.FaultHistories
                .Where(h => h.FaultId == fault.Id)
                .ToListAsync();
            history.Should().NotBeEmpty();
        }

        #endregion

        #region DeleteFaultAsync Tests

        [Fact]
        public async Task DeleteFaultAsync_ExistingFault_ReturnsTrue()
        {
            // Arrange
            var (fault, _) = await CreateTestFault();

            // Act
            var result = await _faultService.DeleteFaultAsync(fault.Id);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteFaultAsync_NonExistingFault_ReturnsFalse()
        {
            // Act
            var result = await _faultService.DeleteFaultAsync(999);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region GetFaultHistoryAsync Tests

        [Fact]
        public async Task GetFaultHistoryAsync_ReturnsHistoryEntries()
        {
            // Arrange
            var (fault, user) = await CreateTestFault();

            // Add some history
            _context.FaultHistories.AddRange(
                new FaultHistory 
                { 
                    FaultId = fault.Id, 
                    Action = "Oluşturuldu", 
                    UserId = user.Id, 
                    CreatedAt = DateTime.UtcNow 
                },
                new FaultHistory 
                { 
                    FaultId = fault.Id, 
                    Action = "Güncellendi", 
                    UserId = user.Id, 
                    CreatedAt = DateTime.UtcNow.AddMinutes(5) 
                }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _faultService.GetFaultHistoryAsync(fault.Id);

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetFaultHistoryAsync_NoHistory_ReturnsEmptyList()
        {
            // Arrange
            var (fault, _) = await CreateTestFault();

            // Act
            var result = await _faultService.GetFaultHistoryAsync(fault.Id);

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region AddFaultCommentAsync Tests

        [Fact]
        public async Task AddFaultCommentAsync_AddsHistoryEntry()
        {
            // Arrange
            var (fault, user) = await CreateTestFault();

            // Act
            var result = await _faultService.AddFaultCommentAsync(
                fault.Id, 
                "Test yorum",
                user.Id
            );

            // Assert
            result.Should().NotBeNull();
            result.Notes.Should().Be("Test yorum");
            result.Action.Should().Be("Comment");
        }

        #endregion
    }
}
