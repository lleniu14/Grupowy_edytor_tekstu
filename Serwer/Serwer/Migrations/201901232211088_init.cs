namespace Serwer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class init : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "CollaborativeTextEditor_Server.Documents",
                c => new
                    {
                        DocumentId = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        CurrentRevisionId = c.Int(nullable: false),
                        Content = c.String(),
                        CurrentHash = c.Binary(),
                        CurrentHashLength = c.Int(nullable: false),
                        Editors_Count = c.Int(nullable: false),
                        Editor_EditorId = c.Int(),
                    })
                .PrimaryKey(t => t.DocumentId)
                .ForeignKey("CollaborativeTextEditor_Server.Editors", t => t.Editor_EditorId)
                .Index(t => t.Editor_EditorId);
            
            CreateTable(
                "CollaborativeTextEditor_Server.Editors",
                c => new
                    {
                        EditorId = c.Int(nullable: false, identity: true),
                        Login = c.String(),
                        Password = c.String(),
                        EditableDocuments = c.String(),
                    })
                .PrimaryKey(t => t.EditorId);
            
            CreateTable(
                "CollaborativeTextEditor_Server.Revisions",
                c => new
                    {
                        RevisionId = c.Int(nullable: false, identity: true),
                        RevId = c.Int(nullable: false),
                        Content = c.String(),
                        Document_DocumentId = c.Int(),
                    })
                .PrimaryKey(t => t.RevisionId)
                .ForeignKey("CollaborativeTextEditor_Server.Documents", t => t.Document_DocumentId)
                .Index(t => t.Document_DocumentId);
            
            CreateTable(
                "CollaborativeTextEditor_Server.Update",
                c => new
                    {
                        UpdateDtoId = c.Int(nullable: false),
                        MemberName = c.String(),
                        DocumentId = c.String(),
                        PreviousRevisionId = c.Int(nullable: false),
                        PreviousHash = c.Binary(),
                        PreviousHashLength = c.Int(nullable: false),
                        NewHash = c.Binary(),
                        NewHashLength = c.Int(nullable: false),
                        NewRevisionId = c.Int(nullable: false),
                        Patch = c.String(),
                        EditorCount = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.UpdateDtoId)
                .ForeignKey("CollaborativeTextEditor_Server.Revisions", t => t.UpdateDtoId)
                .Index(t => t.UpdateDtoId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("CollaborativeTextEditor_Server.Update", "UpdateDtoId", "CollaborativeTextEditor_Server.Revisions");
            DropForeignKey("CollaborativeTextEditor_Server.Revisions", "Document_DocumentId", "CollaborativeTextEditor_Server.Documents");
            DropForeignKey("CollaborativeTextEditor_Server.Documents", "Editor_EditorId", "CollaborativeTextEditor_Server.Editors");
            DropIndex("CollaborativeTextEditor_Server.Update", new[] { "UpdateDtoId" });
            DropIndex("CollaborativeTextEditor_Server.Revisions", new[] { "Document_DocumentId" });
            DropIndex("CollaborativeTextEditor_Server.Documents", new[] { "Editor_EditorId" });
            DropTable("CollaborativeTextEditor_Server.Update");
            DropTable("CollaborativeTextEditor_Server.Revisions");
            DropTable("CollaborativeTextEditor_Server.Editors");
            DropTable("CollaborativeTextEditor_Server.Documents");
        }
    }
}
