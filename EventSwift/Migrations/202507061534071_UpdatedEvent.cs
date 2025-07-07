namespace EventSwift.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class UpdatedEvent : DbMigration
    {
        public override void Up()
        {
            // 1. Create Events table
            CreateTable(
                "dbo.Events",
                c => new
                {
                    EventId = c.Int(nullable: false, identity: true),
                    Title = c.String(),
                    Status = c.String(),
                    ClientId = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.EventId)
                .ForeignKey("dbo.Users", t => t.ClientId)
                .Index(t => t.ClientId);

            // 2. Add the new EventId column to EventProposals (temporarily nullable)
            AddColumn("dbo.EventProposals", "EventId", c => c.Int(nullable: true));

            // 3. Seed a dummy Event (assuming UserId = 1 exists)
            Sql("INSERT INTO Events (Title, Status, ClientId) VALUES ('Temporary Event', 'Pending', 1)");

            // 4. Update all existing EventProposals to link to this dummy Event
            Sql("UPDATE EventProposals SET EventId = 1 WHERE EventId IS NULL");

            // 5. Alter the column to NOT NULL after fixing the data
            AlterColumn("dbo.EventProposals", "EventId", c => c.Int(nullable: false));

            // 6. Now create the index AFTER the column is NOT NULL
            CreateIndex("dbo.EventProposals", "EventId");

            // 7. Add the foreign key AFTER the column is NOT NULL
            AddForeignKey("dbo.EventProposals", "EventId", "dbo.Events", "EventId");
        }

        public override void Down()
        {
            DropForeignKey("dbo.EventProposals", "EventId", "dbo.Events");
            DropIndex("dbo.EventProposals", new[] { "EventId" });
            DropColumn("dbo.EventProposals", "EventId");
            DropTable("dbo.Events");
        }
    }


}
