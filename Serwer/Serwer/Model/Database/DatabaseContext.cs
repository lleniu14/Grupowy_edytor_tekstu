using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

namespace Serwer.Model.Database
{
    class DatabaseContext : DbContext
    {
        public DatabaseContext() : base("name=CollaborativeTextEditor_Server") { }
        public DbSet<Document> Documents { get; set; }
        public DbSet<Editor> Editors { get; set; }
        public DbSet<Revision> Revisions { get; set; }
        public DbSet<UpdateDto> Updates { get; set; }

    }
}
