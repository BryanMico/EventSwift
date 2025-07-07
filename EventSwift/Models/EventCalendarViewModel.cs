using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EventSwift.Models
{
    public class EventCalendarViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime? StartDate { get; set; }
        public string Status { get; set; }
    }
}