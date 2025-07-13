namespace EventSwift.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class EventMultiUpload : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Events", "VPAAFilePath", c => c.String());
            AddColumn("dbo.Events", "VPFFilePath", c => c.String());
            AddColumn("dbo.Events", "AMUFilePath", c => c.String());
            AddColumn("dbo.Events", "VPAFilePath", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Events", "VPAFilePath");
            DropColumn("dbo.Events", "AMUFilePath");
            DropColumn("dbo.Events", "VPFFilePath");
            DropColumn("dbo.Events", "VPAAFilePath");
        }
    }
}
