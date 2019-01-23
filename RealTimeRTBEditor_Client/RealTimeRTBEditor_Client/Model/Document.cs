using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeRTBEditor_Client
{
    public class Document
    {
        public int Id { get; set; }
        public UpdateDto PendingUpdate { get; set; }
        public string Name { get; set; }
        public int CurrentRevisionId { get; set; }
        public string Content { get; set; }
        public byte[] CurrentHash { get; set; }
        public int Chl { get; set; }
        public int EditorCount { get; set; }
        public UpdateDto OutOfSyncUpdate { get; set; }
        public AcknowledgeDto OutOfSyncAcknowledge { get; set; }

        private readonly Dictionary<int, Revision> _revisions = new Dictionary<int, Revision>();

        public Revision GetCurrentRevision()
        {
            return GetRevision(CurrentRevisionId);
        }
        public Revision GetRevision(int revisionId)
        {
            if (_revisions.ContainsKey(revisionId))
            {
                return _revisions[revisionId];
            }
            return null;
        }

    }

    public class Revision
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public UpdateDto UpdateDto { get; set; }
    }
}
