using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EventSwift.Models
{
    public class ProposalApproval
    {
        public int ProposalApprovalId { get; set; }
        public int EventProposalId { get; set; }
        public virtual EventProposal EventProposal { get; set; }

        public string Office { get; set; } // VPAA, VPF, AMI, President
        public string Status { get; set; } // Pending, Approved, Rejected
        public DateTime? ActionDate { get; set; }
    }
}