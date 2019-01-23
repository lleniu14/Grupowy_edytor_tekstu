using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serwer.Model.Interfaces
{
    interface IEditor
    {
        int AddEditor(Editor editor);
        List<Editor> GetAllEditors();
        Editor GetEditorByLogin(string username);
        Editor GetEditorById(int editorId);
        void DeleteEditor(Editor editor);
        int UpdateEditor(Editor editor);
        string GetEditorDocuments(Editor editor);
        void DeleteSharedDoc(int docID);
    }
}
