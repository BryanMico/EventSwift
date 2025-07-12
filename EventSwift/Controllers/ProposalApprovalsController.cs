using EventSwift.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using BCrypt.Net;

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

        public ActionResult VPAEventOverview(int id)
        {
            var currentUser = db.Users.FirstOrDefault(u => u.Username == User.Identity.Name);
            if (currentUser == null || currentUser.Role != "VPA")
                return HttpNotFound("Access denied. VPA access only.");

            var ev = db.Events
                .Include(e => e.Proposals.Select(p => p.Approvals))
                .FirstOrDefault(e => e.EventId == id);
            if (ev == null)
                return HttpNotFound();

            return View(ev);
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

            var currentUser = db.Users.FirstOrDefault(u => u.Username == User.Identity.Name);
            ViewBag.CurrentUser = currentUser;

            return View(approval);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Action(int id, string action, string feedbackMessage, DateTime? eventDate, string approvalPassword, string signatureData)
        {
            var approval = db.ProposalApprovals
                   .Include(a => a.EventProposal)
                   .Include(a => a.EventProposal.Client)
                   .Include(a => a.EventProposal.Event)
                   .FirstOrDefault(a => a.ProposalApprovalId == id);

            if (approval == null)
                return HttpNotFound();

            var currentUser = db.Users.FirstOrDefault(u => u.Username == User.Identity.Name);
            if (currentUser == null)
            {
                ModelState.AddModelError("", "User not found.");
                ViewBag.CurrentUser = null;
                return View(approval);
            }

            if (action == "Approve")
            {
                // Validate password
                if (string.IsNullOrEmpty(approvalPassword) || !BCrypt.Net.BCrypt.Verify(approvalPassword, currentUser.PasswordHash))
                {
                    ModelState.AddModelError("approvalPassword", "Incorrect password.");
                    ViewBag.CurrentUser = currentUser;
                    return View(approval);
                }
                // Validate signature
                if (string.IsNullOrEmpty(signatureData) || !signatureData.StartsWith("data:image/png;base64,"))
                {
                    ModelState.AddModelError("signatureData", "Signature is required.");
                    ViewBag.CurrentUser = currentUser;
                    return View(approval);
                }
                // Save signature image to temp file
                var base64 = signatureData.Substring("data:image/png;base64,".Length);
                byte[] imageBytes = Convert.FromBase64String(base64);
                string tempSignaturePath = Path.GetTempFileName() + ".png";
                System.IO.File.WriteAllBytes(tempSignaturePath, imageBytes);

                approval.Status = "Approved";
                approval.ActionDate = DateTime.Now;
                
                // Set proposal status based on office
                if (approval.Office == "VPA")
                {
                    approval.EventProposal.Status = "Approved";
                }
                else
                {
                    approval.EventProposal.Status = "Reviewed";
                }

                // If VPAA, VPF, or AMU approves, forward to VPA
                if (approval.Office == "VPAA" || approval.Office == "VPF" || approval.Office == "AMU")
                {
                    // Create new approval for VPA
                    var vpaApproval = new ProposalApproval
                    {
                        EventProposalId = approval.EventProposalId,
                        Office = "VPA",
                        Status = "Pending",
                        ActionDate = null
                    };
                    db.ProposalApprovals.Add(vpaApproval);

                    // Notify VPA users
                    var vpaUsers = db.Users.Where(u => u.Role == "VPA").ToList();
                    foreach (var user in vpaUsers)
                    {
                        db.Notifications.Add(new Notification
                        {
                            Username = user.Username,
                            Message = $"A document '{approval.EventProposal.Title}' has been approved by {approval.Office} and forwarded to you for review.",
                            IsRead = false,
                            CreatedAt = DateTime.Now
                        });
                    }
                }

                // If VPA is approving, set the event date
                if (approval.Office == "VPA" && eventDate.HasValue)
                {
                    approval.EventProposal.Event.ApprovedDate = eventDate.Value;
                }

                // Stamp signature and text on PDF if the file is a PDF
                var filePath = approval.EventProposal.FilePath;
                if (!string.IsNullOrEmpty(filePath) && Path.GetExtension(filePath).ToLower() == ".pdf")
                {
                    string pdfPath = Server.MapPath(filePath);
                    if (System.IO.File.Exists(pdfPath))
                    {
                        StampPdfWithSignature(pdfPath, tempSignaturePath, approval.Office);
                    }
                }
                // Delete temp signature file
                if (System.IO.File.Exists(tempSignaturePath))
                {
                    System.IO.File.Delete(tempSignaturePath);
                }

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
                    ViewBag.CurrentUser = currentUser;
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
                ViewBag.CurrentUser = currentUser;
                return View(approval);
            }

            db.SaveChanges();

            return RedirectToAction("ApprovalsIndex");
        }

        // Helper method to stamp signature image and text on PDF
        private void StampPdfWithSignature(string pdfPath, string signatureImagePath, string office)
        {
            string tempPath = pdfPath + ".tmp";
            using (var reader = new PdfReader(pdfPath))
            using (var stamper = new PdfStamper(reader, new FileStream(tempPath, FileMode.Create)))
            {
                int pageCount = reader.NumberOfPages;
                iTextSharp.text.Image signature = iTextSharp.text.Image.GetInstance(signatureImagePath);
                signature.ScaleAbsolute(120, 50); // Smaller signature size

                // Place on last page
                PdfContentByte over = stamper.GetOverContent(pageCount);

                // Get page dimensions
                var pageSize = reader.GetPageSize(pageCount);
                float pageWidth = pageSize.Width;
                float pageHeight = pageSize.Height;

                // Calculate position based on office (different positions for different offices)
                float xPos, yPos;
                BaseColor textColor;
                
                switch (office.ToUpper())
                {
                    case "VPAA":
                        xPos = 50;
                        yPos = pageHeight - 100;
                        textColor = BaseColor.BLUE;
                        break;
                    case "VPF":
                        xPos = pageWidth - 250;
                        yPos = pageHeight - 100;
                        textColor = BaseColor.GREEN;
                        break;
                    case "AMU":
                        xPos = 50;
                        yPos = 150;
                        textColor = BaseColor.ORANGE;
                        break;
                    case "VPA":
                        xPos = pageWidth - 250;
                        yPos = 150;
                        textColor = BaseColor.RED;
                        break;
                    default:
                        xPos = 50;
                        yPos = 50;
                        textColor = BaseColor.BLACK;
                        break;
                }

                // Draw the approval text
                BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                over.BeginText();
                over.SetFontAndSize(bf, 12);
                over.SetColorFill(textColor);
                
                // Add approval text with date
                string approvalText = $"APPROVED BY {office}";
                over.ShowTextAligned(Element.ALIGN_LEFT, approvalText, xPos, yPos + 60, 0);
                
                string dateText = DateTime.Now.ToString("MMM dd, yyyy");
                over.ShowTextAligned(Element.ALIGN_LEFT, dateText, xPos, yPos + 45, 0);
                
                over.EndText();

                // Draw the signature image
                signature.SetAbsolutePosition(xPos, yPos);
                over.AddImage(signature);

                stamper.Close();
            }
            System.IO.File.Delete(pdfPath);
            System.IO.File.Move(tempPath, pdfPath);
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

            // 🔔 Notify the new receiving office users (No Username since this is office-level)
            var officeUsers = db.Users.Where(u => u.Role == targetOffice).ToList();
            foreach (var user in officeUsers)
            {
                db.Notifications.Add(new Notification
                {
                    Username = null, // No Username for office notifications
                    Message = $"A document titled '{approval.EventProposal.Title}' has been forwarded to your office.",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }

            db.SaveChanges();

            TempData["Message"] = $"Document sent to {targetOffice}.";
            return RedirectToAction("ApprovalsIndex");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteEvent(int eventId)
        {
            var currentUser = db.Users.FirstOrDefault(u => u.Username == User.Identity.Name);
            if (currentUser == null)
                return HttpNotFound("User not found");

            var officeRole = currentUser.Role;

            var ev = db.Events
                .Include(e => e.Proposals.Select(p => p.Approvals))
                .Include(e => e.Client)
                .FirstOrDefault(e => e.EventId == eventId);

            if (ev == null)
            {
                TempData["Error"] = "Event not found.";
                return RedirectToAction("ApprovalsIndex");
            }

            // Check if this office has any documents for this event
var officeProposals = ev.Proposals.Where(p => p.Approvals.Any(a => a.Office == officeRole)).ToList();
if (!officeProposals.Any())
{
    TempData["Error"] = "You don't have permission to delete this event.";
    return RedirectToAction("ApprovalsIndex");
}

// Delete all documents for this office and their associated files
foreach (var proposal in officeProposals)
            {
                // Delete the file from server
                if (!string.IsNullOrEmpty(proposal.FilePath))
                {
                    string fullPath = Server.MapPath(proposal.FilePath);
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }

                // Delete related feedbacks
                var feedbacks = db.ProposalFeedbacks.Where(f => f.EventProposalId == proposal.EventProposalId).ToList();
                db.ProposalFeedbacks.RemoveRange(feedbacks);

                // Delete approvals for this proposal
                var approvals = db.ProposalApprovals.Where(a => a.EventProposalId == proposal.EventProposalId).ToList();
                db.ProposalApprovals.RemoveRange(approvals);

                // Delete the document
db.EventProposals.Remove(proposal);
            }

            // Notify the client
            if (ev.Client != null)
            {
                db.Notifications.Add(new Notification
                {
                    Username = ev.Client.Username,
                    Message = $"Your event '{ev.Title}' has been deleted by {officeRole}.",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }

            db.SaveChanges();

            TempData["Success"] = "Event and associated documents deleted successfully.";
            return RedirectToAction("ApprovalsIndex");
        }
    }
}
