namespace EventSwift.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Followup : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.EventProposals", "HasFollowedUp", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.EventProposals", "HasFollowedUp");
        }
    }
}
