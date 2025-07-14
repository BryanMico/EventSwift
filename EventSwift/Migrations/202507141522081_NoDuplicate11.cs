namespace EventSwift.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class NoDuplicate11 : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Events", "CreatedAt", c => c.DateTime());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Events", "CreatedAt", c => c.DateTime(nullable: false));
        }
    }
}
