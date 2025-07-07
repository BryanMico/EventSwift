using EventSwift.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EventSwift.Controllers
{
    public class NotificationsController : Controller
    {
        private DefaultConnection db = new DefaultConnection();

        // GET: Notifications
        public ActionResult Index()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "Account");

            var username = User.Identity.Name;
            var notifications = db.Notifications
                .Where(n => n.Username == username)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

            return View(notifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkAllAsRead()
        {
            var username = User.Identity.Name;
            var notifications = db.Notifications.Where(n => n.Username == username && !n.IsRead).ToList();

            foreach (var notif in notifications)
            {
                notif.IsRead = true;
            }

            db.SaveChanges();

            return new HttpStatusCodeResult(200); // success
        }

    }
}