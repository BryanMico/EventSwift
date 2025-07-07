using EventSwift.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EventSwift.Controllers
{
    public class UsersController : Controller
    {
        private DefaultConnection db = new DefaultConnection();

        // GET: Users
        public ActionResult Index()
        {
            var users = db.Users.ToList();
            return View(users);
        }

        // GET: Users/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(User user)
        {
            if (ModelState.IsValid)
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                db.Users.Add(user);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(user);
        }

        // GET: Users/Edit/5
        public ActionResult Edit(int id)
        {
            var user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(User user)
        {
            if (ModelState.IsValid)
            {
                var existingUser = db.Users.Find(user.UserId);
                if (existingUser != null)
                {
                    existingUser.FullName = user.FullName;
                    existingUser.Username = user.Username;
                    existingUser.Role = user.Role;

                    if (!string.IsNullOrWhiteSpace(user.PasswordHash))
                    {
                        existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                    }

                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }

            return View(user);
        }

        // GET: Users/Delete/5
        public ActionResult Delete(int id)
        {
            var user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var user = db.Users.Find(id);
            if (user != null)
            {
                db.Users.Remove(user);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}
