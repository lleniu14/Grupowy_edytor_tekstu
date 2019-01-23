using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Serwer.Model
{
    [NotMapped]
    class AcknowledgeDto
    {
        public int DocumentId { get; set; }
        public int PreviousRevisionId { get; set; }
        public byte[] PreviousHash { get; set; }
        public int PreviousHashLength { get; set; }
        public byte[] NewHash { get; set; }
        public int NewHashLenth { get; set; }
        public int NewRevisionId { get; set; }
    }
}
