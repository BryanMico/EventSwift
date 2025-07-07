using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EventSwift.Models
{
    public class Event
    {
        public int EventId { get; set; }
        public string Title { get; set; }
        public string Status { get; set; } // Pending, ReadyForFinal, FinalPending, FinalApproved
        public DateTime? ApprovedDate { get; set; }

        public int ClientId { get; set; }
        public virtual User Client { get; set; }

        public virtual ICollection<EventProposal> Proposals { get; set; }
    }
}