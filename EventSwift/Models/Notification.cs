using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EventSwift.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public string Username { get; set; } // Who will receive this notification
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}