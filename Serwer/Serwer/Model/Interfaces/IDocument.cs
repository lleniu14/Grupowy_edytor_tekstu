using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serwer.Model.Interfaces
{
    public interface IDocument
    {
        int AddDocument(Document document);
        List<Document> GetAllDocuments();
        Document GetDocument(int documentId);
        void DeleteDocument(Document document);
        int UpdateDocument(Document document);
        int GetNewDocId();
        string GetEditorDocuments(int EditorId);
    }
}
