namespace EventSwift.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ApproveDate : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Events", "ApprovedDate", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Events", "ApprovedDate");
        }
    }
}
