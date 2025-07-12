using EventSwift.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EventSwift.Controllers
{
    public class EventProposalsController : BaseController
    {
        private DefaultConnection db = new DefaultConnection();

        // GET: EventProposals
        public ActionResult Index()
        {
            var currentUser = db.Users.FirstOrDefault(u => u.Username == User.Identity.Name);
            if (currentUser == null) return HttpNotFound();

            var events = db.Events
                           .Where(e => e.ClientId == currentUser.UserId)
                           .ToList();

            return View(events);
        }

        public ActionResult CreateEvent()
        {
            return View();
        }

        // POST: EventProposals/CreateEvent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateEvent(Event model)
        {
            if (ModelState.IsValid)
            {
                var currentUser = db.Users.FirstOrDefault(u => u.Username == User.Identity.Name);
                if (currentUser == null)
                {
                    ModelState.AddModelError("", "User not found.");
                    return View(model);
                }

                model.Status = "Pending";
                model.ClientId = currentUser.UserId;

                db.Events.Add(model);
                db.SaveChanges();

                // Redirect to the event details page, or to the proposal creation page for this event
                return RedirectToAction("Details", new { id = model.EventId });
            }

            return View(model);
        }

        // GET: EventProposals/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: EventProposals/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EventProposal proposal, HttpPostedFileBase uploadedFile, int eventId, string targetOffice)
        {
            using (var db = new DefaultConnection())
            {
                // Check if event has been sent to President
                var eventItem = db.Events.FirstOrDefault(e => e.EventId == eventId);
                if (eventItem != null && (eventItem.Status == "SentToPresident" || eventItem.Status == "ApprovedByPresident"))
                {
                    TempData["Error"] = "Cannot create new documents. This event has been sent to the President for approval.";
                    return RedirectToAction("Details", new { id = eventId });
                }
                if (uploadedFile != null && uploadedFile.ContentLength > 0)
                {
                    string fileExtension = Path.GetExtension(uploadedFile.FileName).ToLower();

                    if (fileExtension != ".pdf")
                    {
                        ModelState.AddModelError("uploadedFile", "Only PDF files are allowed.");
                        return RedirectToAction("Details", new { id = eventId });
                    }

                    string uploadFolder = Server.MapPath("~/Uploads");
                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    string originalFileName = Path.GetFileName(uploadedFile.FileName);
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
                    string fileExt = Path.GetExtension(originalFileName);
                    
                    // Create unique filename for this proposal
                    string uniqueFileName = $"{fileNameWithoutExt}_{eventId}_{DateTime.Now:yyyyMMddHHmmss}{fileExt}";
                    string path = Path.Combine(uploadFolder, uniqueFileName);
                    uploadedFile.SaveAs(path);
                    proposal.FilePath = "/Uploads/" + uniqueFileName;
                }
                else
                {
                    ModelState.AddModelError("uploadedFile", "Please insert a file to upload.");
                    return RedirectToAction("Details", new { id = eventId });
                }

                proposal.TargetOfficeRole = targetOffice;
                proposal.EventId = eventId; // ✅ Pass the EventId
                proposal.Status = "Pending";
                proposal.SubmittedAt = DateTime.Now;

                var client = db.Users.FirstOrDefault(u => u.Username == User.Identity.Name);
                if (client == null)
                {
                    ModelState.AddModelError("", "User not found.");
                    return RedirectToAction("Details", new { id = eventId });
                }
                proposal.ClientId = client.UserId;

                db.EventProposals.Add(proposal);
                db.SaveChanges();

                if (!string.IsNullOrEmpty(proposal.TargetOfficeRole))
                {
                    var approval = new ProposalApproval
                    {
                        EventProposalId = proposal.EventProposalId,
                        Office = proposal.TargetOfficeRole,
                        Status = "Pending",
                        ActionDate = null
                    };

                    db.ProposalApprovals.Add(approval);

                    var officeUsers = db.Users.Where(u => u.Role == proposal.TargetOfficeRole).ToList();

                    foreach (var user in officeUsers)
                    {
                        var notification = new Notification
                        {
                            Username = user.Username,
                            Message = $"A new proposal titled '{proposal.Title}' has been submitted.",
                            IsRead = false,
                            CreatedAt = DateTime.Now
                        };
                        db.Notifications.Add(notification);
                    }

                    db.SaveChanges();
                }

                return RedirectToAction("Details", new { id = eventId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SendToPresident(int eventId)
        {
            var ev = db.Events
                .Include(e => e.Proposals.Select(p => p.Approvals))
                .FirstOrDefault(e => e.EventId == eventId);

            if (ev == null) return HttpNotFound();

            // Check if all VPA approvals are completed
            var vpaApprovals = ev.Proposals.SelectMany(p => p.Approvals).Where(a => a.Office == "VPA").ToList();
            bool allVPAApproved = vpaApprovals.Any() && vpaApprovals.All(a => a.Status == "Approved");

            if (!allVPAApproved)
            {
                TempData["Error"] = "Cannot send to President. All documents must be approved by VPA first.";
                return RedirectToAction("Details", new { id = eventId });
            }

            // Mark event as sent to president
            ev.Status = "SentToPresident";



            db.SaveChanges();

            // Notify Presidents (assuming role = "President")
            var presidents = db.Users.Where(u => u.Role == "President").ToList();
            foreach (var pres in presidents)
            {
                db.Notifications.Add(new Notification
                {
                    Username = pres.Username,
                    Message = $"Event '{ev.Title}' has been sent for your final approval.",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }
            db.SaveChanges();

            TempData["Success"] = "Event sent to the President successfully.";
            return RedirectToAction("Details", new { id = eventId });
        }


        // GET: EventProposals/Delete/5
        public ActionResult Details(int id)
        {
            var ev = db.Events
                .Include(e => e.Proposals.Select(p => p.Approvals))
                .FirstOrDefault(e => e.EventId == id);
            if (ev == null) return HttpNotFound();

            return View(ev);
        }

        // GET: EventProposals/DetailProposal/5
        public ActionResult DetailProposal(int id)
        {
            var proposal = db.EventProposals
                .Include(p => p.Feedbacks)
                .FirstOrDefault(p => p.EventProposalId == id);

            if (proposal == null)
            {
                return HttpNotFound();
            }

            if (proposal.Feedbacks == null)
            {
                proposal.Feedbacks = new List<ProposalFeedback>();
            }

            return View(proposal);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var proposal = db.EventProposals.Find(id);
            if (proposal == null)
            {
                return HttpNotFound();
            }

            // Optional: Delete the file from the server
            string fullPath = Server.MapPath(proposal.FilePath);
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }

            db.EventProposals.Remove(proposal);
            db.SaveChanges();

            return RedirectToAction("Details", new { id = proposal.EventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteEvent(int eventId)
        {
            var ev = db.Events.Include(e => e.Proposals).FirstOrDefault(e => e.EventId == eventId);

            if (ev == null)
            {
                TempData["Error"] = "Event not found.";
                return RedirectToAction("Index");
            }

            // Optional: Delete associated proposals and files
            foreach (var proposal in ev.Proposals.ToList())
            {
                string fullPath = Server.MapPath(proposal.FilePath);
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                var approvals = db.ProposalApprovals.Where(a => a.EventProposalId == proposal.EventProposalId).ToList();
                db.ProposalApprovals.RemoveRange(approvals);

                db.EventProposals.Remove(proposal);
            }

            db.Events.Remove(ev);
            db.SaveChanges();

            return RedirectToAction("Index");
        }


        // GET: EventProposals/Feedback/5
        public ActionResult Feedback(int id)
        {
            var proposal = db.EventProposals.Find(id);
            if (proposal == null || proposal.Client.Username != User.Identity.Name)
            {
                return HttpNotFound();
            }

            return View(proposal);
        }

        // POST: EventProposals/Feedback/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Feedback(int id, string feedback)
        {
            var proposal = db.EventProposals.Include(p => p.Feedbacks).FirstOrDefault(p => p.EventProposalId == id);
            if (proposal != null && proposal.Client.Username == User.Identity.Name)
            {
                var feedbackEntry = new ProposalFeedback
                {
                    EventProposalId = proposal.EventProposalId,
                };

                proposal.Status = "Resubmitted";
                proposal.Feedbacks.Add(feedbackEntry);

                db.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(proposal);
        }

        // GET: EventProposals/Resubmit/5
        public ActionResult Resubmit(int id)
        {
            var proposal = db.EventProposals.Include(p => p.Feedbacks).FirstOrDefault(p => p.EventProposalId == id);
            var currentUser = db.Users.FirstOrDefault(u => u.Username == User.Identity.Name);

            if (proposal == null || proposal.ClientId != currentUser.UserId)
                return HttpNotFound();

            if (proposal.Status != "Rejected")
                return RedirectToAction("Index");

            return View(proposal);
        }

        // POST: EventProposals/Resubmit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Resubmit(int id, HttpPostedFileBase uploadedFile)
        {
            var proposal = db.EventProposals.Find(id);
            var currentUser = db.Users.FirstOrDefault(u => u.Username == User.Identity.Name);

            if (proposal == null || proposal.ClientId != currentUser.UserId)
                return HttpNotFound();

            if (uploadedFile != null && uploadedFile.ContentLength > 0)
            {
                string fileExtension = Path.GetExtension(uploadedFile.FileName).ToLower();

                if (fileExtension != ".pdf")
                {
                    ModelState.AddModelError("uploadedFile", "Only PDF files are allowed.");
                    return View(proposal);
                }

                string uploadFolder = Server.MapPath("~/Uploads");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                string originalFileName = Path.GetFileName(uploadedFile.FileName);
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
                string fileExt = Path.GetExtension(originalFileName);
                
                // Create unique filename for this resubmission
                string uniqueFileName = $"{fileNameWithoutExt}_resubmit_{proposal.EventProposalId}_{DateTime.Now:yyyyMMddHHmmss}{fileExt}";
                string path = Path.Combine(uploadFolder, uniqueFileName);
                uploadedFile.SaveAs(path);
                proposal.FilePath = "/Uploads/" + uniqueFileName;
            }
            else
            {
                ModelState.AddModelError("uploadedFile", "Please upload a file.");
                return View(proposal);
            }

            proposal.Status = "Resubmitted";
            proposal.SubmittedAt = DateTime.Now;

            var approvals = db.ProposalApprovals.Where(a => a.EventProposalId == proposal.EventProposalId).ToList();
            foreach (var approval in approvals)
            {
                approval.Status = "Pending";
                approval.ActionDate = null;

                // 🔔 Create notification for each office on resubmission
                // Find all users with the role matching the approval's Office
                var officeUsers = db.Users.Where(u => u.Role == approval.Office).ToList();

                foreach (var user in officeUsers)
                {
                    var notification = new Notification
                    {
                        Username = user.Username,
                        Message = $"A proposal titled '{proposal.Title}' has been resubmitted.",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    };
                    db.Notifications.Add(notification);
                }

            }

            db.SaveChanges();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult ApproveEvent(int eventId, DateTime approvedDate)
        {
            var ev = db.Events
                .Include(e => e.Proposals)
                .FirstOrDefault(e => e.EventId == eventId);

            if (ev == null)
            {
                TempData["Error"] = "Event not found.";
                return RedirectToAction("PresidentDashboard", "Dashboard");
            }

            ev.Status = "ApprovedByPresident";
            ev.ApprovedDate = approvedDate; // ✅ Use the new field



            db.SaveChanges();

            // Send notification to the client
            var client = db.Users.FirstOrDefault(u => u.UserId == ev.ClientId);
            if (client != null)
            {
                db.Notifications.Add(new Notification
                {
                    Username = client.Username,
                    Message = $"Your event '{ev.Title}' has been approved by the President and scheduled for {approvedDate:MMMM dd, yyyy}.",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
                db.SaveChanges();
            }

            TempData["Success"] = "Event approved successfully.";
            return RedirectToAction("PresidentDashboard", "Dashboard");
        }

        [HttpGet]
        public ActionResult FollowUp(int id)
        {
            var proposal = db.EventProposals.Find(id);
            var currentUser = db.Users.FirstOrDefault(u => u.Username == User.Identity.Name);
            if (proposal == null || proposal.ClientId != currentUser.UserId)
                return HttpNotFound();
            if (proposal.HasFollowedUp)
            {
                TempData["Error"] = "You have already used Follow Up for this proposal.";
                return RedirectToAction("Details", new { id = proposal.EventId });
            }
            // Mark as followed up
            proposal.HasFollowedUp = true;
            // Notify the office
            var officeUsers = db.Users.Where(u => u.Role == proposal.TargetOfficeRole).ToList();
            foreach (var user in officeUsers)
            {
                db.Notifications.Add(new Notification
                {
                    Username = user.Username,
                    Message = $"Follow up: Please review the proposal '{proposal.Title}'.",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }
            db.SaveChanges();
            TempData["Success"] = "Follow up sent to the office.";
            return RedirectToAction("Details", new { id = proposal.EventId });
        }

        public ActionResult PresidentEventDetails(int id)
        {
            var ev = db.Events.Include("Client").Include("Proposals").FirstOrDefault(e => e.EventId == id);
            if (ev == null) return HttpNotFound();
            return View("PresidentEventDetails", ev);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PresidentApprove(int eventId)
        {
            var ev = db.Events.FirstOrDefault(e => e.EventId == eventId);
            if (ev == null) return HttpNotFound();
            ev.Status = "ApprovedByPresident";
            db.SaveChanges();
            TempData["Success"] = "Event approved by President.";
            return RedirectToAction("PresidentDashboard", "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PresidentReject(int eventId)
        {
            var ev = db.Events.FirstOrDefault(e => e.EventId == eventId);
            if (ev == null) return HttpNotFound();
            ev.Status = "RejectedByPresident";
            db.SaveChanges();
            TempData["Success"] = "Event rejected by President.";
            return RedirectToAction("PresidentDashboard", "Dashboard");
        }

    }
}
