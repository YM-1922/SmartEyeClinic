using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEyeClinic.Web.Services;

namespace SmartEyeClinic.Web.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly NotificationService _notificationService;

        public NotificationController(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // POST: /Notification/MarkAllAsRead | وضع علامة مقروء على كافة إشعارات المستخدم الحالي
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value);
                await _notificationService.MarkAllAsReadAsync(userId);
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "User not found" });
        }

        // POST: /Notification/MarkAsRead/{id} | وضع علامة مقروء على إشعار محدد
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value);
                var success = await _notificationService.MarkAsReadAsync(id, userId);
                return Json(new { success });
            }
            return Json(new { success = false, message = "User not found" });
        }
    }
}
