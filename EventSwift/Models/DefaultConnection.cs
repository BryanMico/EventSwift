using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

namespace EventSwift.Models
{
    public partial class DefaultConnection : DbContext
    {
        public DefaultConnection()
            : base("name=DefaultConnection")
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; } 
        public DbSet<EventProposal> EventProposals { get; set; }
        public DbSet<ProposalApproval> ProposalApprovals { get; set; }
        public DbSet<ProposalFeedback> ProposalFeedbacks { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Relationship: EventProposal -> User (Client)
            modelBuilder.Entity<EventProposal>()
                .HasRequired(e => e.Client)
                .WithMany()
                .HasForeignKey(e => e.ClientId)
                .WillCascadeOnDelete(false);

            // Relationship: EventProposal -> Event
            modelBuilder.Entity<EventProposal>()
                .HasRequired(e => e.Event)
                .WithMany(ev => ev.Proposals)
                .HasForeignKey(e => e.EventId)
                .WillCascadeOnDelete(false);

            // Relationship: Event -> User (Client)
            modelBuilder.Entity<Event>()
                .HasRequired(e => e.Client)
                .WithMany()
                .HasForeignKey(e => e.ClientId)
                .WillCascadeOnDelete(false);



            base.OnModelCreating(modelBuilder);
        }
    }
}
