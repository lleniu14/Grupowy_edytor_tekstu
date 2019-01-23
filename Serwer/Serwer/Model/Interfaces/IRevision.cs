using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serwer.Model.Interfaces
{
    interface IRevision
    {
        int AddRevision(Revision Revision);
        List<Revision> GetAllRevisions();
        Revision GetRevision(int RevisionId);
        ICollection<Revision> GetRevisionbyDocument(Document document);
        ICollection<Revision> GetDocumentRevision(int docId);
        void DeleteRevision(Revision Revision);
        int GetNewRevId();
    }
}
