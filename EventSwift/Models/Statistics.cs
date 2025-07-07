using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EventSwift.Models
{
    public class Statistics
    {
        public class ApprovalSpeedBucketsVM
        {
            public int Within1Day { get; set; }
            public int Within3Days { get; set; }
            public int Within7Days { get; set; }
            public int Within14Days { get; set; }
        }

        public class ApprovalsByOfficeVM
        {
            public string Office { get; set; }
            public int ApprovedCount { get; set; }
            public int PendingCount { get; set; }
            public int RejectedCount { get; set; }
        }

        public class MonthlyStatsVM
        {
            public string Month { get; set; }
            public int Submitted { get; set; }
            public int Approved { get; set; }
        }

        public class TopOfficeVM
        {
            public string Office { get; set; }
            public int ApprovedCount { get; set; }
        }

        public class TopClientVM
        {
            public string ClientUsername { get; set; }
            public int ProposalsCount { get; set; }
        }

        public class ProposalStatusBreakdownVM
        {
            public string Status { get; set; }
            public int Count { get; set; }
        }

        public class SuperAdminDashboardVM
        {
            public int TotalUsers { get; set; }
            public int TotalProposals { get; set; }
            public int PendingProposals { get; set; }
            public int ApprovedProposals { get; set; }
            public int RejectedProposals { get; set; }
            public double ApprovalPercentage { get; set; }
            public double AverageApprovalDays { get; set; }
            public ApprovalSpeedBucketsVM ApprovalSpeedBuckets { get; set; }
            public List<ApprovalsByOfficeVM> ApprovalsByOffice { get; set; }
            public List<MonthlyStatsVM> MonthlyStats { get; set; }
            public List<TopOfficeVM> TopOffices { get; set; }
            public List<TopClientVM> TopClients { get; set; }
            public List<ProposalStatusBreakdownVM> ProposalStatusBreakdown { get; set; }
        }

    }
}