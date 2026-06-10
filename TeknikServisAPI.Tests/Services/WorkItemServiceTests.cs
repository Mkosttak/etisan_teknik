using FluentAssertions;
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
    /// Tests for WorkItemService - Kanban board and work item management
    /// Coverage target: 70%+
    /// 
    /// Key Test Areas:
    /// - CRUD operations
    /// - Stage transitions
    /// - Filtering and pagination
    /// - Comments, attachments, checklist management
    /// - History tracking
    /// </summary>
    public class WorkItemServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<WorkItemService>> _loggerMock;
        private readonly Mock<INotificationService> _notificationMock;
        private readonly Mock<IAuditLogService> _auditLogMock;
        private readonly WorkItemService _service;

        public WorkItemServiceTests()
        {
            _context = InMemoryDbHelper.CreateContext($"WorkItemServiceTest_{Guid.NewGuid()}");
            _loggerMock = new Mock<ILogger<WorkItemService>>();
            _notificationMock = new Mock<INotificationService>();
            _auditLogMock = new Mock<IAuditLogService>();
            
            _service = new WorkItemService(
                _context,
                _loggerMock.Object,
                _notificationMock.Object,
                _auditLogMock.Object);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region Create Tests

        [Fact]
        public async Task CreateAsync_ValidData_CreatesWorkItem()
        {
            // Arrange
            var dto = new CreateWorkItemDto
            {
                Title = "Test İş Kalemi",
                Description = "Test açıklaması",
                Kind = "Request",
                Priority = "Normal",
                Stage = "Backlog",
                InstitutionName = "Test Kurumu"
            };

            // Act
            var result = await _service.CreateAsync(dto, userId: 1);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Test İş Kalemi");
            result.Stage.Should().Be("Backlog");
            result.Priority.Should().Be("Normal");
        }

        [Fact]
        public async Task CreateAsync_WithTags_CreatesWithTags()
        {
            // Arrange
            var dto = new CreateWorkItemDto
            {
                Title = "Tagged Item",
                Tags = new List<string> { "acil", "önemli" }
            };

            // Act
            var result = await _service.CreateAsync(dto, userId: 1);

            // Assert
            result.TagCount.Should().Be(2);
        }

        [Fact]
        public async Task CreateAsync_CriticalPriority_SendsNotification()
        {
            // Arrange
            var dto = new CreateWorkItemDto
            {
                Title = "Kritik İş",
                Priority = "Critical"
            };
            
            string? capturedTitle = null;
            _notificationMock.Setup(n => n.CreateNotificationAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<int?>(),
                    It.IsAny<string?>(),
                    It.IsAny<int?>(),
                    It.IsAny<string?>()))
                .ReturnsAsync(new Notification());

            // Act
            await _service.CreateAsync(dto, userId: 1);

            // Assert - Kritik öncelikli iş oluşturulduğunda bildirim gönderilmeli
            _notificationMock.Verify(n => n.CreateNotificationAsync(
                It.Is<string>(title => title.Contains("KRİTİK")),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<string?>()), Times.Once);
        }

        #endregion

        #region GetById Tests

        [Fact]
        public async Task GetByIdAsync_ExistingItem_ReturnsItem()
        {
            // Arrange
            var created = await CreateTestWorkItem();

            // Act
            var result = await _service.GetByIdAsync(created.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(created.Id);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistentItem_ReturnsNull()
        {
            // Act
            var result = await _service.GetByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region Update Tests

        [Fact]
        public async Task UpdateAsync_ValidData_UpdatesItem()
        {
            // Arrange
            var created = await CreateTestWorkItem();
            var updateDto = new UpdateWorkItemDto
            {
                Title = "Güncellenmiş Başlık"
            };

            // Act
            var result = await _service.UpdateAsync(created.Id, updateDto, userId: 1);

            // Assert
            result.Should().NotBeNull();
            result!.Title.Should().Be("Güncellenmiş Başlık");
        }

        [Fact]
        public async Task UpdateAsync_NonExistentItem_ReturnsNull()
        {
            // Arrange
            var updateDto = new UpdateWorkItemDto { Title = "New Title" };

            // Act
            var result = await _service.UpdateAsync(999, updateDto, userId: 1);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task DeleteAsync_ExistingItem_ReturnsTrue()
        {
            // Arrange
            var created = await CreateTestWorkItem();

            // Act
            var result = await _service.DeleteAsync(created.Id, userId: 1);

            // Assert
            result.Should().BeTrue();

            // Verify deleted
            var deleted = await _service.GetByIdAsync(created.Id);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_NonExistentItem_ReturnsFalse()
        {
            // Act
            var result = await _service.DeleteAsync(999, userId: 1);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Stage Transition Tests

        [Fact]
        public async Task UpdateStageAsync_ToDone_SetsCompletedAt()
        {
            // Arrange
            var created = await CreateTestWorkItem();
            var stageDto = new UpdateStageDto { Stage = "Done" };

            // Act
            var result = await _service.UpdateStageAsync(created.Id, stageDto, userId: 1);

            // Assert
            result.Should().NotBeNull();
            result!.Stage.Should().Be("Done");
            result.CompletedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateStageAsync_InvalidStage_ThrowsException()
        {
            // Arrange
            var created = await CreateTestWorkItem();
            var stageDto = new UpdateStageDto { Stage = "InvalidStage" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.UpdateStageAsync(created.Id, stageDto, userId: 1));
        }

        #endregion

        #region Filtering Tests

        [Fact]
        public async Task GetAllAsync_WithStageFilter_ReturnsFilteredItems()
        {
            // Arrange
            await CreateTestWorkItem("Backlog Item 1", stage: "Backlog");
            await CreateTestWorkItem("Done Item", stage: "Done");

            var filter = new WorkItemFilterDto { Stage = "Backlog" };

            // Act
            var result = await _service.GetAllAsync(filter);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items.First().Stage.Should().Be("Backlog");
        }

        [Fact]
        public async Task GetAllAsync_Pagination_ReturnsCorrectPage()
        {
            // Arrange
            for (int i = 0; i < 10; i++)
            {
                await CreateTestWorkItem($"Item {i}");
            }

            var filter = new WorkItemFilterDto { Page = 1, PageSize = 5 };

            // Act
            var result = await _service.GetAllAsync(filter);

            // Assert
            result.Items.Should().HaveCount(5);
            result.TotalCount.Should().Be(10);
            result.PageNumber.Should().Be(1);
        }

        #endregion

        #region Kanban Board Tests

        [Fact]
        public async Task GetKanbanBoardAsync_ReturnsGroupedItems()
        {
            // Arrange
            await CreateTestWorkItem("Backlog 1", stage: "Backlog");
            await CreateTestWorkItem("Backlog 2", stage: "Backlog");
            await CreateTestWorkItem("Done 1", stage: "Done");

            var filter = new WorkItemFilterDto();

            // Act
            var result = await _service.GetKanbanBoardAsync(filter);

            // Assert
            result.Should().NotBeNull();
            result.Backlog.Should().HaveCount(2);
            result.Done.Should().HaveCount(1);
            result.TotalCount.Should().Be(3);
        }

        #endregion

        #region Comments Tests

        [Fact]
        public async Task AddCommentAsync_AddsComment()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context); // For user
            var created = await CreateTestWorkItem();
            var commentDto = new CreateCommentDto { Content = "Test yorumu" };

            // Act
            var result = await _service.AddCommentAsync(created.Id, commentDto, userId: 1);

            // Assert
            result.Should().NotBeNull();
            result.Content.Should().Be("Test yorumu");
        }

        [Fact]
        public async Task GetCommentsAsync_ReturnsComments()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            var created = await CreateTestWorkItem();
            await _service.AddCommentAsync(created.Id, new CreateCommentDto { Content = "Comment 1" }, userId: 1);
            await _service.AddCommentAsync(created.Id, new CreateCommentDto { Content = "Comment 2" }, userId: 1);

            // Act
            var result = await _service.GetCommentsAsync(created.Id);

            // Assert
            result.Should().HaveCount(2);
        }

        #endregion

        #region Checklist Tests

        [Fact]
        public async Task AddChecklistItemAsync_AddsItem()
        {
            // Arrange
            var created = await CreateTestWorkItem();
            var checklistDto = new CreateChecklistItemDto { Title = "Todo 1" };

            // Act
            var result = await _service.AddChecklistItemAsync(created.Id, checklistDto, userId: 1);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Todo 1");
            result.IsCompleted.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateChecklistItemAsync_CompleteItem_SetsCompletedAt()
        {
            // Arrange
            var created = await CreateTestWorkItem();
            var item = await _service.AddChecklistItemAsync(created.Id, new CreateChecklistItemDto { Title = "Todo" }, userId: 1);
            var updateDto = new UpdateChecklistItemDto { IsCompleted = true };

            // Act
            var result = await _service.UpdateChecklistItemAsync(created.Id, item.Id, updateDto, userId: 1);

            // Assert
            result.Should().NotBeNull();
            result!.IsCompleted.Should().BeTrue();
            result.CompletedAt.Should().NotBeNull();
            result.CompletedByUserId.Should().Be(1);
        }

        #endregion

        #region History Tests

        [Fact]
        public async Task GetHistoryAsync_ReturnsHistory()
        {
            // Arrange
            var created = await CreateTestWorkItem();
            
            // Update to create history
            await _service.UpdateStageAsync(created.Id, new UpdateStageDto { Stage = "Done" }, userId: 1);

            // Act
            var result = await _service.GetHistoryAsync(created.Id);

            // Assert
            result.Should().NotBeEmpty();
        }

        #endregion

        #region Helper Methods

        private async Task<WorkItemDto> CreateTestWorkItem(string title = "Test İş", string stage = "Backlog")
        {
            var dto = new CreateWorkItemDto
            {
                Title = title,
                Kind = "Request",
                Priority = "Normal",
                Stage = stage
            };
            return await _service.CreateAsync(dto, userId: 1);
        }

        #endregion
    }
}
