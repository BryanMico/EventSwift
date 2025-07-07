using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EventSwift.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; } // SuperAdmin, Client, VPAA, VPF, AMI, President
    }

}