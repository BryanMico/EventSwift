using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EventSwift.Models
{
    public class ProposalSummaryVM
    {
        public int EventProposalId { get; set; }
        public string Title { get; set; }
        public string EventTitle { get; set; }
        public string Status { get; set; }
        public DateTime SubmittedAt { get; set; } // Add this
        public int ProposalApprovalId { get; set; } // Add this property
    }
}