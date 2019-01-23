using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiffMatchPatch;

namespace RealTimeRTBEditor_Client
{
    public class AcknowledgeDto
    {
        public int DocumentId { get; set; }
        public int PreviousRevisionId { get; set; }
        public byte[] PreviousHash { get; set; }
        public int PreviousHashLength { get; set; }
        public byte[] NewHash { get; set; }
        public int NewHashLength { get; set; }
        public int NewRevisionId { get; set; }
    }
    public class UpdateDto
    {
        public String MemberName { get; set; }
        public int DocumentId { get; set; }
        public int PreviousRevisionId { get; set; }
        public byte[] PreviousHash { get; set; }
        public int PreviousHashLength { get; set; }
        public byte[] NewHash { get; set; }
        public int NewHashLength { get; set; }
        public int NewRevisionId { get; set; }
        public string Patch { get; set; }
        public int EditorCount { get; set; }
    }
}
