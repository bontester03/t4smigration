using Microsoft.EntityFrameworkCore;
using WebApit4s.DAL;
using WebApit4s.Models;

namespace WebApit4s.Services
{
    public class NotificationService
    {
        private readonly TimeContext _context;
        public NotificationService(TimeContext context)
        {
            _context = context;
        }

        // Send Notification to a Specific User
        public async Task SendNotification(int userId, string message)
        {
            var notification = new Notification
            {
                UserId = userId.ToString(),
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        // Send Notification to ALL Users
        public async Task SendNotificationToAllUsers(string message)
        {
            var users = await _context.Users.ToListAsync();

            foreach (var user in users)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = user.Id.ToString(),
                    Message = message,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
        }

        // Get Notifications for a Specific User
        public async Task<List<Notification>> GetUserNotifications(int userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId.ToString())
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        // Mark Notification as Read
        public async Task MarkAsRead(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}