using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EventSwift.Models
{
    public class ClientDashboardViewModel
    {
        public int MyProposalsCount { get; set; }
        public int PendingApprovalsCount { get; set; }
        public int RejectedProposalsCount { get; set; }
        public List<EventCalendarViewModel> Events { get; set; }
    }
}