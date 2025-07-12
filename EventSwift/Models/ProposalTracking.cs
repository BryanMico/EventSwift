using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventSwift.Models
{
    public class ProposalTracking
    {
        public int ProposalTrackingId { get; set; }
        
        public int EventProposalId { get; set; }
        public virtual EventProposal EventProposal { get; set; }
        
        [Required]
        public string FromOffice { get; set; }
        
        [Required]
        public string ToOffice { get; set; }
        
        [Required]
        public string Action { get; set; } // "Submitted", "Approved", "Rejected", "Forwarded"
        
        public string Status { get; set; } // "Pending", "Completed"
        
        public DateTime Timestamp { get; set; }
        
        public string Notes { get; set; }
        
        public int? ActionByUserId { get; set; }
        public virtual User ActionByUser { get; set; }
    }
} 