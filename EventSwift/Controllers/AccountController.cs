using EventSwift.Models;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;

namespace EventSwift.Controllers
{
    public class AccountController : Controller
    {

        private DefaultConnection db = new DefaultConnection();

        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(LoginViewModel model)
        {
            // Check for empty username
            if (string.IsNullOrWhiteSpace(model.Username))
            {
                ViewBag.Error = "Username is required.";
                return View(model);
            }

            // Check for empty password
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ViewBag.Error = "Password is required.";
                return View(model);
            }

            if (!ModelState.IsValid)
            {   
                ViewBag.Error = "Please fill in all required fields correctly.";
                return View(model);
            }

            var user = db.Users.FirstOrDefault(u => u.Username == model.Username);
            if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                FormsAuthentication.SetAuthCookie(user.Username, false);

                // Role-based Redirection
                switch (user.Role)
                {
                    case "SuperAdmin":
                        return RedirectToAction("SuperAdminDashboard", "Dashboard");

                    case "Client":
                        return RedirectToAction("ClientDashboard", "Dashboard");

                    case "VPAA":
                    case "VPF":
                    case "AMU":
                    case "VPA":
                        return RedirectToAction("OfficeDashboard", "Dashboard");

                    case "President":
                        return RedirectToAction("PresidentDashboard", "Dashboard");

                    default:
                        ViewBag.Error = "User role is not recognized.";
                        return View(model);
                }
            }

            ViewBag.Error = "Invalid username or password.";
            return View(model);
        }

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }
    }
}
