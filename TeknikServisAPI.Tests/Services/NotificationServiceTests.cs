using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeknikServisAPI.Data;
using TeknikServisAPI.Models;
using TeknikServisAPI.Services;
using TeknikServisAPI.Tests.Helpers;

namespace TeknikServisAPI.Tests.Services
{
    /// <summary>
    /// Tests for NotificationService - bildirim yönetimi component
    /// </summary>
    public class NotificationServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;

        public NotificationServiceTests()
        {
            _context = InMemoryDbHelper.CreateContext($"NotificationServiceTest_{Guid.NewGuid()}");
            _notificationService = new NotificationService(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region CreateNotificationAsync Tests

        [Fact]
        public async Task CreateNotificationAsync_ValidData_CreatesNotification()
        {
            // Act
            var result = await _notificationService.CreateNotificationAsync(
                title: "Test Title",
                message: "Test Message",
                type: "info"
            );

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Test Title");
            result.Message.Should().Be("Test Message");
            result.Type.Should().Be("info");
            result.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task CreateNotificationAsync_WithOptionalParams_CreatesCorrectly()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            var user = await _context.Users.FirstAsync();

            // Act
            var result = await _notificationService.CreateNotificationAsync(
                title: "User Notification",
                message: "Message for user",
                type: "warning",
                link: "/faults/123",
                relatedId: 123,
                relatedType: "Fault",
                userId: user.Id
            );

            // Assert
            result.UserId.Should().Be(user.Id);
            result.Link.Should().Be("/faults/123");
            result.RelatedId.Should().Be(123);
            result.RelatedType.Should().Be("Fault");
        }

        [Fact]
        public async Task CreateNotificationAsync_WithTargetRole_SetsRoleCorrectly()
        {
            // Act
            var result = await _notificationService.CreateNotificationAsync(
                title: "Role Notification",
                message: "Message for technicians",
                type: "info",
                targetRole: "Teknisyen"
            );

            // Assert
            result.TargetRole.Should().Be("Teknisyen");
            result.UserId.Should().BeNull();
        }

        #endregion

        #region GetNotificationsAsync Tests

        [Fact]
        public async Task GetNotificationsAsync_ReturnsUserNotifications()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            var user = await _context.Users.FirstAsync();
            
            // Create notifications for this user
            await _notificationService.CreateNotificationAsync("N1", "M1", "info", userId: user.Id);
            await _notificationService.CreateNotificationAsync("N2", "M2", "warning", userId: user.Id);
            // Create notification for another user
            await _notificationService.CreateNotificationAsync("N3", "M3", "info", userId: 99);

            // Act
            var result = await _notificationService.GetNotificationsAsync(user.Id);

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetNotificationsAsync_IncludesRoleBasedNotifications()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            var user = await _context.Users.FirstAsync(u => u.Role == "Teknisyen");
            
            // Create user-specific notification
            await _notificationService.CreateNotificationAsync("User Notif", "M1", "info", userId: user.Id);
            // Create role-based notification
            await _notificationService.CreateNotificationAsync("Role Notif", "M2", "info", targetRole: "Teknisyen");
            // Create notification for different role
            await _notificationService.CreateNotificationAsync("Admin Notif", "M3", "info", targetRole: "Admin");

            // Act
            var result = await _notificationService.GetNotificationsAsync(user.Id);

            // Assert
            result.Should().HaveCount(2, because: "user should see their own and role-based notifications");
        }

        [Fact]
        public async Task GetNotificationsAsync_UnreadOnly_ReturnsOnlyUnread()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            var user = await _context.Users.FirstAsync();
            
            var notif1 = await _notificationService.CreateNotificationAsync("Unread", "M1", "info", userId: user.Id);
            var notif2 = await _notificationService.CreateNotificationAsync("Read", "M2", "info", userId: user.Id);
            
            // Mark one as read
            await _notificationService.MarkAsReadAsync(notif2.Id, user.Id);

            // Act
            var result = await _notificationService.GetNotificationsAsync(user.Id, unreadOnly: true);

            // Assert
            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Unread");
        }

        [Fact]
        public async Task GetNotificationsAsync_NoNotifications_ReturnsEmptyList()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            var user = await _context.Users.FirstAsync();

            // Act
            var result = await _notificationService.GetNotificationsAsync(user.Id);

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region MarkAsReadAsync Tests

        [Fact]
        public async Task MarkAsReadAsync_UnreadNotification_MarksAsRead()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            var user = await _context.Users.FirstAsync();
            
            var notification = await _notificationService.CreateNotificationAsync(
                "Unread Notification", 
                "Message", 
                "info",
                userId: user.Id
            );

            // Act
            await _notificationService.MarkAsReadAsync(notification.Id, user.Id);

            // Assert
            var receipt = await _context.NotificationReceipts
                .FirstOrDefaultAsync(r => r.NotificationId == notification.Id && r.UserId == user.Id);
            receipt.Should().NotBeNull();
            receipt!.IsRead.Should().BeTrue();
            receipt.ReadAt.Should().BeAfter(DateTime.MinValue);
        }

        [Fact]
        public async Task MarkAsReadAsync_AlreadyRead_DoesNotUpdateReadAt()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            var user = await _context.Users.FirstAsync();
            
            var notification = await _notificationService.CreateNotificationAsync(
                "Notification", 
                "Message", 
                "info",
                userId: user.Id
            );
            
            // Mark as read first time
            await _notificationService.MarkAsReadAsync(notification.Id, user.Id);
            var receipt = await _context.NotificationReceipts
                .FirstAsync(r => r.NotificationId == notification.Id);
            var firstReadAt = receipt.ReadAt;

            // Act - Mark as read again
            await Task.Delay(10); // Small delay
            await _notificationService.MarkAsReadAsync(notification.Id, user.Id);

            // Assert
            await _context.Entry(receipt).ReloadAsync();
            receipt.ReadAt.Should().Be(firstReadAt, because: "ReadAt should not change if already read");
        }

        #endregion

        #region GetUnreadCountAsync Tests

        [Fact]
        public async Task GetUnreadCountAsync_ReturnsCorrectCount()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            var user = await _context.Users.FirstAsync();
            
            // Create 3 notifications
            await _notificationService.CreateNotificationAsync("N1", "M1", "info", userId: user.Id);
            await _notificationService.CreateNotificationAsync("N2", "M2", "info", userId: user.Id);
            await _notificationService.CreateNotificationAsync("N3", "M3", "info", userId: user.Id);

            // Act
            var result = await _notificationService.GetUnreadCountAsync(user.Id);

            // Assert
            result.Should().Be(3);
        }

        [Fact]
        public async Task GetUnreadCountAsync_AfterMarkingAsRead_DecreasesCount()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            var user = await _context.Users.FirstAsync();
            
            var notif1 = await _notificationService.CreateNotificationAsync("N1", "M1", "info", userId: user.Id);
            await _notificationService.CreateNotificationAsync("N2", "M2", "info", userId: user.Id);
            
            await _notificationService.MarkAsReadAsync(notif1.Id, user.Id);

            // Act
            var result = await _notificationService.GetUnreadCountAsync(user.Id);

            // Assert
            result.Should().Be(1);
        }

        [Fact]
        public async Task GetUnreadCountAsync_IncludesRoleBasedNotifications()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            var user = await _context.Users.FirstAsync(u => u.Role == "Teknisyen");
            
            // Create user notification
            await _notificationService.CreateNotificationAsync("User N", "M", "info", userId: user.Id);
            // Create role notification
            await _notificationService.CreateNotificationAsync("Role N", "M", "info", targetRole: "Teknisyen");

            // Act
            var result = await _notificationService.GetUnreadCountAsync(user.Id);

            // Assert
            result.Should().Be(2);
        }

        #endregion

        #region MarkAllAsReadAsync Tests

        [Fact]
        public async Task MarkAllAsReadAsync_MarksAllUserNotifications()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            var user = await _context.Users.FirstAsync();
            
            await _notificationService.CreateNotificationAsync("N1", "M1", "info", userId: user.Id);
            await _notificationService.CreateNotificationAsync("N2", "M2", "info", userId: user.Id);
            await _notificationService.CreateNotificationAsync("N3", "M3", "info", userId: user.Id);

            // Act
            await _notificationService.MarkAllAsReadAsync(user.Id);

            // Assert
            var unreadCount = await _notificationService.GetUnreadCountAsync(user.Id);
            unreadCount.Should().Be(0);
        }

        [Fact]
        public async Task MarkAllAsReadAsync_IncludesRoleBasedNotifications()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            var user = await _context.Users.FirstAsync(u => u.Role == "Teknisyen");
            
            await _notificationService.CreateNotificationAsync("User N", "M", "info", userId: user.Id);
            await _notificationService.CreateNotificationAsync("Role N", "M", "info", targetRole: "Teknisyen");

            // Act
            await _notificationService.MarkAllAsReadAsync(user.Id);

            // Assert
            var result = await _notificationService.GetNotificationsAsync(user.Id, unreadOnly: true);
            result.Should().BeEmpty();
        }

        #endregion
    }
}
