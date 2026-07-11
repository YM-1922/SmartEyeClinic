using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;

namespace SmartEyeClinic.Web.Services
{
    /// <summary>
    /// خدمة إدارة وإرسال الإشعارات للمستخدمين والموظفين والمديرين
    /// </summary>
    public class NotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        // إنشاء إشعار جديد لمستخدم معين وحفظه بقاعدة البيانات
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

        // إرسال إشعار لكافة موظفي الاستقبال الفعالين بالعيادة
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

        // إرسال إشعار لكافة مديري النظام (Admins) المسجلين
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

        // جلب قائمة الإشعارات الخاصة بمستخدم معين بترتيب تنازلي (آخر 15 إشعاراً)
        public async Task<List<Notification>> GetNotificationsForUserAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.SentAt)
                .Take(15)
                .ToListAsync();
        }

        // جلب عدد الإشعارات غير المقروءة الخاصة بمستخدم معين
        public async Task<int> GetUnreadNotificationsCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && (n.IsRead == null || !n.IsRead.Value));
        }

        // تعيين كافة إشعارات مستخدم معين كإشعارات مقروءة دفعة واحدة
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

        // تعيين إشعار محدد كإشعار مقروء بعد التحقق من ملكيته للمستخدم المحدد
        public async Task<bool> MarkAsReadAsync(int id, int userId)
        {
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}
