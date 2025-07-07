namespace EventSwift.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class NotificationFix : DbMigration
    {
        public override void Up()
        {
            Sql("UPDATE EventProposals SET ClientId = 1 WHERE ClientId IS NULL");

        }

        public override void Down()
        {
            DropIndex("dbo.EventProposals", new[] { "ClientId" });
            AlterColumn("dbo.EventProposals", "ClientId", c => c.Int());
            RenameColumn(table: "dbo.EventProposals", name: "ClientId", newName: "Client_UserId");
            AddColumn("dbo.EventProposals", "ClientId", c => c.Int(nullable: false));
            CreateIndex("dbo.EventProposals", "Client_UserId");
        }
    }
}
