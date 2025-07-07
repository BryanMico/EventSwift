using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EventSwift.Models
{
    public class ProposalFeedback
    {
        public int ProposalFeedbackId { get; set; }
        public int EventProposalId { get; set; }
        public virtual EventProposal EventProposal { get; set; }

        public string Office { get; set; }
        public string Feedback { get; set; }
        public DateTime FeedbackDate { get; set; }
    }
}