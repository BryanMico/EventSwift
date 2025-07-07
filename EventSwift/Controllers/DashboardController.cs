using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using EventSwift.Models;


namespace EventSwift.Controllers
{
    public class DashboardController : BaseController
    {
        private DefaultConnection db = new DefaultConnection();

        // GET: Dashboard
        public ActionResult SuperAdminDashboard()
        {
            var approvedProposalsData = db.ProposalApprovals
                .Where(pa => pa.Status == "Approved" && pa.ActionDate != null)
                .Select(pa => new
                {
                    DaysToApprove = DbFunctions.DiffDays(pa.EventProposal.SubmittedAt, pa.ActionDate)
                })
                .ToList();

            int totalUsers = db.Users.Count();
            int totalProposals = db.EventProposals.Count();
            int pendingProposals = db.EventProposals.Count(p => p.Status == "Pending");
            int approvedProposals = db.EventProposals.Count(p => p.Status == "Approved");
            int rejectedProposals = db.EventProposals.Count(p => p.Status == "Rejected");

            double approvalPercentage = totalProposals == 0 ? 0 : (double)approvedProposals / totalProposals * 100;

            double averageApprovalDays = approvedProposalsData.Any() ? approvedProposalsData.Average(x => x.DaysToApprove) ?? 0 : 0;

            // Approvals by Office
            var approvalsByOffice = db.ProposalApprovals
                .GroupBy(pa => pa.Office)
                .Select(g => new Statistics.ApprovalsByOfficeVM
                {
                    Office = g.Key,
                    ApprovedCount = g.Count(pa => pa.Status == "Approved"),
                    PendingCount = g.Count(pa => pa.Status == "Pending"),
                    RejectedCount = g.Count(pa => pa.Status == "Rejected")
                })
                .ToList();

            // Monthly Stats (last 6 months)
            DateTime sixMonthsAgo = DateTime.Now.AddMonths(-5);
            var monthlyStats = new List<Statistics.MonthlyStatsVM>();
            for (int i = 0; i < 6; i++)
            {
                DateTime month = sixMonthsAgo.AddMonths(i);
                int year = month.Year;
                int monthNum = month.Month;

                int submittedCount = db.EventProposals.Count(p => p.SubmittedAt.Year == year && p.SubmittedAt.Month == monthNum);
                int approvedCount = db.EventProposals.Count(p => p.Status == "Approved" && p.SubmittedAt.Year == year && p.SubmittedAt.Month == monthNum);

                monthlyStats.Add(new Statistics.MonthlyStatsVM
                {
                    Month = month.ToString("MMM yyyy"),
                    Submitted = submittedCount,
                    Approved = approvedCount
                });
            }

            // Approval Speed Buckets
            int totalApproved = approvedProposalsData.Count;
            var approvalSpeedBuckets = new Statistics.ApprovalSpeedBucketsVM
            {
                Within1Day = totalApproved > 0 ? approvedProposalsData.Count(x => x.DaysToApprove <= 1) * 100 / totalApproved : 0,
                Within3Days = totalApproved > 0 ? approvedProposalsData.Count(x => x.DaysToApprove <= 3) * 100 / totalApproved : 0,
                Within7Days = totalApproved > 0 ? approvedProposalsData.Count(x => x.DaysToApprove <= 7) * 100 / totalApproved : 0,
                Within14Days = totalApproved > 0 ? approvedProposalsData.Count(x => x.DaysToApprove <= 14) * 100 / totalApproved : 0,
            };

            // Top Offices
            var topOffices = db.ProposalApprovals
                .Where(pa => pa.Status == "Approved")
                .GroupBy(pa => pa.Office)
                .Select(g => new Statistics.TopOfficeVM
                {
                    Office = g.Key,
                    ApprovedCount = g.Count()
                })
                .OrderByDescending(x => x.ApprovedCount)
                .Take(5)
                .ToList();

            // Top Clients
            var topClients = db.EventProposals
                .GroupBy(p => p.Client.Username)
                .Select(g => new Statistics.TopClientVM
                {
                    ClientUsername = g.Key,
                    ProposalsCount = g.Count()
                })
                .OrderByDescending(x => x.ProposalsCount)
                .Take(5)
                .ToList();

            // Proposal Status Breakdown
            var proposalStatusBreakdown = db.EventProposals
                .GroupBy(p => p.Status)
                .Select(g => new Statistics.ProposalStatusBreakdownVM
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToList();

            var model = new Statistics.SuperAdminDashboardVM
            {
                TotalUsers = totalUsers,
                TotalProposals = totalProposals,
                PendingProposals = pendingProposals,
                ApprovedProposals = approvedProposals,
                RejectedProposals = rejectedProposals,
                ApprovalPercentage = approvalPercentage,
                AverageApprovalDays = averageApprovalDays,
                ApprovalSpeedBuckets = approvalSpeedBuckets,
                ApprovalsByOffice = approvalsByOffice,
                MonthlyStats = monthlyStats,
                TopOffices = topOffices,
                TopClients = topClients,
                ProposalStatusBreakdown = proposalStatusBreakdown
            };

            return View(model);
        }


        public ActionResult OfficeDashboard()
        {
            var currentUser = db.Users.FirstOrDefault(u => u.Username == User.Identity.Name);
            if (currentUser == null)
                return HttpNotFound("User not found");

            string officeRole = currentUser.Role;

            ViewBag.PendingProposals = db.ProposalApprovals.Count(pa => pa.Office == officeRole && pa.EventProposal.Status == "Pending");
            ViewBag.ApprovedProposals = db.ProposalApprovals.Count(pa => pa.Office == officeRole && pa.EventProposal.Status == "Approved");
            ViewBag.RejectedProposals = db.ProposalApprovals.Count(pa => pa.Office == officeRole && pa.EventProposal.Status == "Rejected");

            ViewBag.RecentProposals = db.ProposalApprovals
              .Where(pa => pa.Office == officeRole)
              .OrderByDescending(pa => pa.EventProposal.SubmittedAt)
              .Take(5)
              .Select(pa => new ProposalSummaryVM
              {
                  EventProposalId = pa.EventProposal.EventProposalId,
                  Title = pa.EventProposal.Title,
                  Status = pa.EventProposal.Status,
                  SubmittedAt = pa.EventProposal.SubmittedAt

              })
              .ToList();



            return View();

        }


        public ActionResult ClientDashboard()
        {
            var currentUser = db.Users.FirstOrDefault(u => u.Username == User.Identity.Name);
            if (currentUser == null)
                return HttpNotFound("User not found");

            var dashboard = new ClientDashboardViewModel
            {
                MyProposalsCount = db.EventProposals.Count(p => p.ClientId == currentUser.UserId),
                PendingApprovalsCount = db.EventProposals.Count(p => p.ClientId == currentUser.UserId && p.Status == "Pending"),
                RejectedProposalsCount = db.EventProposals.Count(p => p.ClientId == currentUser.UserId && p.Status == "Rejected"),

                // Get Events related to current user (via proposals or ClientId if applicable)
                Events = db.Events
                    .Where(e => e.ClientId == currentUser.UserId) // or other logic to get user’s events
                    .Select(e => new EventCalendarViewModel
                    {
                        Id = e.EventId,
                        Title = e.Title,
                        StartDate = (DateTime)e.ApprovedDate, // use approved date or creation date
                Status = e.Status
                    })
                    .ToList()
            };

            return View(dashboard);
        }



        public ActionResult PresidentDashboard()
        {
            var eventsForApproval = db.Events
                .Where(e => e.Status == "SentToPresident")
                .ToList();

            return View(eventsForApproval);
        }

    }
}

