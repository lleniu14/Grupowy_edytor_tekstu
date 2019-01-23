using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Serwer.Model
{
    [Table("Revisions", Schema = "CollaborativeTextEditor_Server")]
    public class Revision
    {
        public int RevisionId { get; set; }
        public int RevId { get; set; }
        public string Content { get; set; }
        public Document Document { get; set; }
        public virtual UpdateDto UpdateDto { get; set; }
    }
}
