using EventSwift.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using System.Collections.Generic;

namespace EventSwift.Controllers
{
    public class ProposalApprovalsController : BaseController
    {
        private DefaultConnection db = new DefaultConnection();

        public class OfficeEventProposalsVM
        {
            public Event Event { get; set; }
            public List<EventProposal> Proposals { get; set; }
        }

        public ActionResult ApprovalsIndex()
        {
            var currentUser = db.Users.FirstOrDefault(u => u.Username == User.Identity.Name);
            if (currentUser == null)
                return HttpNotFound("User not found");

            var officeRole = currentUser.Role;

            // Get all events that have at least one proposal for this office
            var events = db.Events
                .Include(e => e.Proposals.Select(p => p.Approvals))
                .Where(ev => ev.Proposals.Any(p => p.Approvals.Any(a => a.Office == officeRole)))
                .ToList();

            return View(events);
        }

        public ActionResult EventDetails(int id)
        {
            var currentUser = db.Users.FirstOrDefault(u => u.Username == User.Identity.Name);
            if (currentUser == null)
                return HttpNotFound("User not found");

            var officeRole = currentUser.Role;

            var ev = db.Events
                .Include(e => e.Proposals.Select(p => p.Approvals))
                .FirstOrDefault(e => e.EventId == id);
            if (ev == null)
                return HttpNotFound();

            // Only proposals for this office
            var proposals = ev.Proposals
                .Where(p => p.Approvals.Any(a => a.Office == officeRole))
                .ToList();

            ViewBag.Role = officeRole;
            ViewBag.EventTitle = ev.Title;
            ViewBag.EventStatus = ev.Status;
            ViewBag.EventId = ev.EventId;
            return View(proposals);
        }

        public ActionResult Details(int id)
        {
            var approval = db.ProposalApprovals.Find(id);
            if (approval == null)
                return HttpNotFound();

            return View(approval);
        }

        public ActionResult Action(int id)
        {
            var approval = db.ProposalApprovals.Find(id);
            if (approval == null)
                return HttpNotFound();

            return View(approval);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Action(int id, string action, string feedbackMessage)
        {
            var approval = db.ProposalApprovals
                   .Include(a => a.EventProposal)
                   .Include(a => a.EventProposal.Client)
                   .FirstOrDefault(a => a.ProposalApprovalId == id);

            if (approval == null)
                return HttpNotFound();

            if (action == "Approve")
            {
                approval.Status = "Approved";
                approval.ActionDate = DateTime.Now;
                approval.EventProposal.Status = "Approved";

                // 🔔 Notify Client about approval (Username should only be for Clients)
                db.Notifications.Add(new Notification
                {
                    Username = approval.EventProposal.Client.Username, // Client username only
                    Message = $"Your proposal '{approval.EventProposal.Title}' has been approved by {approval.Office}.",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }
            else if (action == "Reject")
            {
                if (string.IsNullOrWhiteSpace(feedbackMessage))
                {
                    ModelState.AddModelError("feedbackMessage", "Feedback message is required when rejecting.");
                    return View(approval);
                }

                approval.Status = "Rejected";
                approval.ActionDate = DateTime.Now;
                approval.EventProposal.Status = "Rejected";

                var feedback = new ProposalFeedback
                {
                    EventProposalId = approval.EventProposalId,
                    Office = approval.Office,
                    Feedback = feedbackMessage,
                    FeedbackDate = DateTime.Now
                };
                db.ProposalFeedbacks.Add(feedback);

                // 🔔 Notify Client about rejection (Username should only be for Clients)
                db.Notifications.Add(new Notification
                {
                    Username = approval.EventProposal.Client.Username, // Client username only
                    Message = $"Your proposal '{approval.EventProposal.Title}' has been rejected by {approval.Office}.",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }
            else
            {
                ModelState.AddModelError("", "Invalid action.");
                return View(approval);
            }

            db.SaveChanges();

            return RedirectToAction("ApprovalsIndex");
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SendToOtherOffice(int proposalApprovalId, string targetOffice)
        {
            var approval = db.ProposalApprovals
                             .Include(a => a.EventProposal.Client)
                             .FirstOrDefault(a => a.ProposalApprovalId == proposalApprovalId);

            if (approval == null)
                return HttpNotFound();

            // Update office and reset approval status and date
            approval.Office = targetOffice;
            approval.Status = "Pending";
            approval.ActionDate = null;

            // 🔔 Notify the new target office users (No Username since this is office-level)
            var officeUsers = db.Users.Where(u => u.Role == targetOffice).ToList();
            foreach (var user in officeUsers)
            {
                db.Notifications.Add(new Notification
                {
                    Username = null, // No Username for office notifications
                    Message = $"A proposal titled '{approval.EventProposal.Title}' has been forwarded to your office.",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }

            db.SaveChanges();

            TempData["Message"] = $"Proposal sent to {targetOffice}.";
            return RedirectToAction("ApprovalsIndex");
        }
    }
}
