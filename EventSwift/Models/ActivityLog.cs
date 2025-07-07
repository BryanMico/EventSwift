using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EventSwift.Models
{
    public class ActivityLog
    {
        public int ActivityLogId { get; set; }
        public string Action { get; set; }
        public string Username { get; set; }
        public DateTime ActionDate { get; set; }
    }
}