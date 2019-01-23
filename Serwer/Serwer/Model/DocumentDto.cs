using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Serwer.Model
{
    [NotMapped]
    class DocumentDto
    {
        public int DocumentId { get; set; }
        public int RevisionId { get; set; }
        public string Content { get; set; }
        public int EditorCount { get; set; }
    }
}
