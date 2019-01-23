using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serwer.Model.Interfaces;
using Serwer.Model.Database;

namespace Serwer.Model.Methods
{
    class DocumentMethods : IDocument
    {
        private readonly IRevision _revisionMethods;
        private readonly DatabaseContext _databaseContext;
        public DocumentMethods(DatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
            _revisionMethods = new RevisionMethods(_databaseContext);
        }
        int IDocument.AddDocument(Document document)
        {
            if (document == null)
            {
                throw new Exception("Document object cannot be null");
            }

            document.DocumentId = 0;

            _databaseContext.Documents.Add(document);
            _databaseContext.SaveChanges();

            return document.DocumentId;
        }

        void IDocument.DeleteDocument(Document document)
        {
            if (document == null)
            {
                throw new Exception("Document object cannot be null");
            }
            ICollection<Revision> revisions = _revisionMethods.GetRevisionbyDocument(document);
            foreach (Revision rev in revisions)
            {
                _revisionMethods.DeleteRevision(rev);
            }
            _databaseContext.Documents.Remove(document);
            _databaseContext.SaveChanges();
        }

        List<Document> IDocument.GetAllDocuments()
        {
            return _databaseContext.Documents.ToList();
        }

        Document IDocument.GetDocument(int documentId)
        {
            if (documentId < 0)
            {
                throw new Exception("Id cannot be less than 0");
            }

            return _databaseContext.Documents.FirstOrDefault(document => document.DocumentId == documentId);
        }

        int IDocument.UpdateDocument(Document document)
        {
            if (document == null)
            {
                throw new Exception("Document object cannot be null");
            }
            using (var db = new DatabaseContext())
            {
                var result = db.Documents.SingleOrDefault(doc => doc.DocumentId == document.DocumentId);
                if (result != null)
                {
                    result.Name = document.Name;
                    result.Content = document.Content;
                    result.CurrentHash = document.CurrentHash;
                    result.CurrentHashLength = document.CurrentHashLength;
                    result.CurrentRevisionId = document.CurrentRevisionId;
                    result.Editors_Count = document.Editors_Count;
                    db.SaveChanges();
                }
            }
            

            return document.DocumentId;
        }

        int IDocument.GetNewDocId()
        {
            if (_databaseContext.Documents.ToArray().Length > 0) return _databaseContext.Documents.ToArray().Last().DocumentId + 1;
            else return 0;
        }

        string IDocument.GetEditorDocuments(int EditorId)
        {
            string result = "";
            var documents = _databaseContext.Documents.Where(doc => doc.Editor.EditorId == EditorId).ToList();
            for (int i = 0; i < documents.Count; i++) 
            {
                result = result + documents[i].DocumentId + "." + documents[i].Name + ",";
            }
            return result;
        }
    }
}
