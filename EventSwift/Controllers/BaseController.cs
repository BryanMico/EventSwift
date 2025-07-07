using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using EventSwift.Models;

public class BaseController : Controller
{
    protected DefaultConnection db = new DefaultConnection();

    protected override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        base.OnActionExecuting(filterContext);

        if (User.Identity.IsAuthenticated)
        {
            var currentUsername = User.Identity.Name;
            var notifications = db.Notifications
                                  .Where(n => n.Username == currentUsername && !n.IsRead)
                                  .OrderByDescending(n => n.CreatedAt)
                                  .ToList();

            ViewBag.Notifications = notifications;
            ViewBag.Role = db.Users.FirstOrDefault(u => u.Username == currentUsername)?.Role ?? "";
        }
        else
        {
            ViewBag.Notifications = new List<Notification>();
            ViewBag.Role = "";
        }
    }
}
