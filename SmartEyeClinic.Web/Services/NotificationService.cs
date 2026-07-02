using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;

namespace SmartEyeClinic.Web.Services
{
    public class NotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateNotificationAsync(int userId, string title, string message, string type)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                SentAt = DateTime.Now
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task NotifyAllReceptionistsAsync(string title, string message, string type)
        {
            var receptionists = await _context.Receptionists.Include(r => r.User).ToListAsync();
            foreach (var r in receptionists)
            {
                if (r.User != null)
                {
                    var notification = new Notification
                    {
                        UserId = r.User.Id,
                        Title = title,
                        Message = message,
                        Type = type,
                        IsRead = false,
                        SentAt = DateTime.Now
                    };
                    _context.Notifications.Add(notification);
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task NotifyAllAdminsAsync(string title, string message, string type)
        {
            var admins = await _context.Users.Include(u => u.Role).Where(u => u.Role.Name == "Admin").ToListAsync();
            foreach (var a in admins)
            {
                var notification = new Notification
                {
                    UserId = a.Id,
                    Title = title,
                    Message = message,
                    Type = type,
                    IsRead = false,
                    SentAt = DateTime.Now
                };
                _context.Notifications.Add(notification);
            }
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetNotificationsForUserAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.SentAt)
                .Take(15)
                .ToListAsync();
        }

        public async Task<int> GetUnreadNotificationsCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && (n.IsRead == null || !n.IsRead.Value));
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && (n.IsRead == null || !n.IsRead.Value))
                .ToListAsync();

            foreach (var n in unread)
            {
                n.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }
    }
}
