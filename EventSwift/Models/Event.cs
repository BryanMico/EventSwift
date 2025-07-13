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
        public string Venue { get; set; } // Venue assigned by VPA

        public int ClientId { get; set; }
        public virtual User Client { get; set; }

        // File paths for each office
        public string VPAAFilePath { get; set; }
        public string VPFFilePath { get; set; }
        public string AMUFilePath { get; set; }
        public string VPAFilePath { get; set; }

        public virtual ICollection<EventProposal> Proposals { get; set; }
    }

    public class CreateEventViewModel
    {
        public string Title { get; set; }
        public System.Web.HttpPostedFileBase VPAAFile { get; set; }
        public System.Web.HttpPostedFileBase VPFFile { get; set; }
        public System.Web.HttpPostedFileBase AMUFile { get; set; }
        public System.Web.HttpPostedFileBase VPAFile { get; set; }
    }
}