namespace EventSwift.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TargetOfficeRole : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.EventProposals", "TargetOfficeRole", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.EventProposals", "TargetOfficeRole");
        }
    }
}
