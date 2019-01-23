using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.ComponentModel.DataAnnotations.Schema;

namespace Serwer.Model
{
    [Table("Editors", Schema = "CollaborativeTextEditor_Server")]
    public class Editor
    {
        public int EditorId { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string EditableDocuments { get; set; }
        public ICollection<Document> Documents { get; set; }
    }
}
