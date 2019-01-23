using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Serwer.Model
{
    [Table("Update", Schema = "CollaborativeTextEditor_Server")]
    public class UpdateDto
    {
        [ForeignKey("Revision")]
        public int UpdateDtoId { get; set; }
        public String MemberName { get; set; }
        public string DocumentId { get; set; }
        public int PreviousRevisionId { get; set; }
        public byte[] PreviousHash { get; set; }
        public int PreviousHashLength { get; set; }
        public byte[] NewHash { get; set; }
        public int NewHashLength { get; set; }
        public int NewRevisionId { get; set; }
        public string Patch { get; set; }
        public int EditorCount { get; set; }
        public virtual Revision Revision { get; set; }
    }
}
