namespace EventSwift.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ActivityLogs",
                c => new
                    {
                        ActivityLogId = c.Int(nullable: false, identity: true),
                        Action = c.String(),
                        Username = c.String(),
                        ActionDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ActivityLogId);
            
            CreateTable(
                "dbo.EventProposals",
                c => new
                    {
                        EventProposalId = c.Int(nullable: false, identity: true),
                        Title = c.String(),
                        FilePath = c.String(),
                        Status = c.String(),
                        SubmittedAt = c.DateTime(nullable: false),
                        ClientId = c.Int(nullable: false),
                        Client_UserId = c.Int(),
                    })
                .PrimaryKey(t => t.EventProposalId)
                .ForeignKey("dbo.Users", t => t.Client_UserId)
                .Index(t => t.Client_UserId);
            
            CreateTable(
                "dbo.ProposalApprovals",
                c => new
                    {
                        ProposalApprovalId = c.Int(nullable: false, identity: true),
                        EventProposalId = c.Int(nullable: false),
                        Office = c.String(),
                        Status = c.String(),
                        ActionDate = c.DateTime(),
                    })
                .PrimaryKey(t => t.ProposalApprovalId)
                .ForeignKey("dbo.EventProposals", t => t.EventProposalId, cascadeDelete: true)
                .Index(t => t.EventProposalId);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        UserId = c.Int(nullable: false, identity: true),
                        FullName = c.String(),
                        Username = c.String(),
                        PasswordHash = c.String(),
                        Role = c.String(),
                    })
                .PrimaryKey(t => t.UserId);
            
            CreateTable(
                "dbo.ProposalFeedbacks",
                c => new
                    {
                        ProposalFeedbackId = c.Int(nullable: false, identity: true),
                        EventProposalId = c.Int(nullable: false),
                        Office = c.String(),
                        Feedback = c.String(),
                        FeedbackDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ProposalFeedbackId)
                .ForeignKey("dbo.EventProposals", t => t.EventProposalId, cascadeDelete: true)
                .Index(t => t.EventProposalId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ProposalFeedbacks", "EventProposalId", "dbo.EventProposals");
            DropForeignKey("dbo.EventProposals", "Client_UserId", "dbo.Users");
            DropForeignKey("dbo.ProposalApprovals", "EventProposalId", "dbo.EventProposals");
            DropIndex("dbo.ProposalFeedbacks", new[] { "EventProposalId" });
            DropIndex("dbo.ProposalApprovals", new[] { "EventProposalId" });
            DropIndex("dbo.EventProposals", new[] { "Client_UserId" });
            DropTable("dbo.ProposalFeedbacks");
            DropTable("dbo.Users");
            DropTable("dbo.ProposalApprovals");
            DropTable("dbo.EventProposals");
            DropTable("dbo.ActivityLogs");
        }
    }
}
