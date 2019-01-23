using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Serwer.Model
{
    [Table("Documents", Schema = "CollaborativeTextEditor_Server")]
    public class Document
    {
        public int DocumentId { get; set; }
        public string Name { get; set; }
        public int CurrentRevisionId { get; set; }
        public string Content { get; set; }
        public byte[] CurrentHash { get; set; }
        public int CurrentHashLength { get; set; }
        public int Editors_Count { get; set; }
        public ICollection<Revision> Revisions { get; set; }
        public Editor Editor { get; set; }
    }
}
