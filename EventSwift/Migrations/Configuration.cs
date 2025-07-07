namespace EventSwift.Migrations
{
    using EventSwift.Models;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<EventSwift.Models.DefaultConnection>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(EventSwift.Models.DefaultConnection context)
        {
            if (!context.Users.Any(u => u.Username == "superadmin"))
            {
                // Hash the password using BCrypt
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword("SuperAdmin123");

                // Create the Super Admin account
                context.Users.Add(new User
                {
                    Username = "superadmin",
                    PasswordHash = hashedPassword,
                    Role = "SuperAdmin"
                });

                context.SaveChanges();
            }
        }
    }
}
