using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serwer.Model.Interfaces;
using Serwer.Model.Database;

namespace Serwer.Model.Methods
{
    class RevisionMethods : IRevision
    {
        private readonly DatabaseContext _databaseContext;
        public RevisionMethods(DatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
        }
        int IRevision.AddRevision(Revision Revision)
        {
            if (Revision == null)
            {
                throw new Exception("Revision object cannot be null");
            }


            using (DatabaseContext entities = new DatabaseContext())
            {
                _databaseContext.Revisions.Add(Revision);
                if(Revision.UpdateDto!=null) _databaseContext.Updates.Add(Revision.UpdateDto);
                _databaseContext.SaveChanges();
            }
            

            return Revision.RevisionId;
        }

        void IRevision.DeleteRevision(Revision Revision)
        {
            if (Revision == null)
            {
                throw new Exception("Revision object cannot be null");
            }
            _databaseContext.Updates.Remove(_databaseContext.Updates.SingleOrDefault(up => up.UpdateDtoId == Revision.RevisionId));
            _databaseContext.Revisions.Remove(Revision);
            _databaseContext.SaveChanges();
        }

        List<Revision> IRevision.GetAllRevisions()
        {
            return _databaseContext.Revisions.ToList();
        }

        Revision IRevision.GetRevision(int RevisionId)
        {
            if (RevisionId < 0)
            {
                throw new Exception("Id cannot be less than 0");
            }

            return _databaseContext.Revisions.FirstOrDefault(Revision => Revision.RevisionId == RevisionId);
        }

        ICollection<Revision> IRevision.GetRevisionbyDocument(Document document)
        {
            if(document == null)
            {
                throw new Exception("Document cannot be null");
            }
            return _databaseContext.Revisions.Where(rev => rev.Document.DocumentId == document.DocumentId).ToList();
        }

        ICollection<Revision> IRevision.GetDocumentRevision(int docId)
        {
            if (docId < 0)
            {
                throw new Exception("Id cannot be less than 0");
            }

            return _databaseContext.Revisions.Where(rev => rev.Document.DocumentId == docId).ToList();
        }

        int IRevision.GetNewRevId()
        {
            if (_databaseContext.Revisions.ToArray().Length > 0) return _databaseContext.Revisions.ToArray().Last().RevisionId + 1;
            else return 0;
        }

    }
}
