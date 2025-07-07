using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EventSwift.Models
{
    public class ProposalCalendarViewModel
    {
        public int Id { get; set; }  // must be a property
        public string Title { get; set; }
        public DateTime? StartDate { get; set; } // ✅ Nullable to prevent casting errors
        public string Status { get; set; }
        public string Description { get; set; }
    }

}