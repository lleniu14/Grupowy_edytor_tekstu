using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serwer.Model.Interfaces;
using Serwer.Model.Database;

namespace Serwer.Model.Methods
{
    class EditorMethods : IEditor
    {
        private readonly DatabaseContext _databaseContext;
        public EditorMethods(DatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
        }
        int IEditor.AddEditor(Editor editor)
        {
            if (editor == null)
            {
                throw new Exception("editor object cannot be null");
            }

            editor.EditorId = 0;

            _databaseContext.Editors.Add(editor);
            _databaseContext.SaveChanges();

            return editor.EditorId;
        }

        void IEditor.DeleteEditor(Editor editor)
        {
            if (editor == null)
            {
                throw new Exception("Editor object cannot be null");
            }

            _databaseContext.Editors.Remove(editor);
            _databaseContext.SaveChanges();
        }

        List<Editor> IEditor.GetAllEditors()
        {
            return _databaseContext.Editors.ToList();
        }

        Editor IEditor.GetEditorByLogin(string username)
        {
            if (username == null)
            {
                return null;
            }

            return _databaseContext.Editors.FirstOrDefault(editor => editor.Login == username);
        }

        Editor IEditor.GetEditorById(int editorId)
        {
            if (editorId < 0)
            {
                return null;
            }

            return _databaseContext.Editors.FirstOrDefault(editor => editor.EditorId == editorId);
        }

        int IEditor.UpdateEditor(Editor editor)
        {
            if (editor == null)
            {
                throw new Exception("editor object cannot be null");
            }
            using (var db = new DatabaseContext())
            {
                var result = db.Editors.SingleOrDefault(edit => edit.EditorId == editor.EditorId);
                if (result != null)
                {
                    result.Login = editor.Login;
                    result.Password = editor.Password;
                    result.EditableDocuments = editor.EditableDocuments;
                    db.SaveChanges();
                }
            }

            return editor.EditorId;
        }

        string IEditor.GetEditorDocuments(Editor editor)
        {
            string EditorDocuments = "";
            if (editor.Documents != null)
            {
                foreach (Document doc in editor.Documents)
                {
                    EditorDocuments = doc.DocumentId.ToString() + "." + doc.Name + ",";
                }
            }
            return EditorDocuments;
        }

        void IEditor.DeleteSharedDoc(int docID)
        {
            ICollection<Editor> editors = _databaseContext.Editors.ToList();
            foreach(Editor e in editors)
            {
                string list = e.EditableDocuments;
                if (list == null) list = "";
                int id = -1;
                int idx = 0;
                string name;
                for (int i = 0; i < list.Length; i++)
                {
                    if (list[i] == '.')
                    {
                        id = int.Parse(list.Substring(idx, i - idx));
                    }
                    if (list[i] == ',')
                    {
                        name = list.Substring(idx, i - idx + 1);
                        if (id == docID)
                        {
                            e.EditableDocuments = e.EditableDocuments.Remove(idx, i - idx + 1);
                            list = list.Remove(idx, i - idx + 1);
                            using (var db = new DatabaseContext())
                            {
                                var result = e;
                                if (result != null)
                                {
                                    result.Login = e.Login;
                                    result.Password = e.Password;
                                    result.EditableDocuments = e.EditableDocuments;
                                    db.SaveChanges();
                                }
                            }
                            i = idx;
                            id = -1;
                            name = "";
                        }
                        else
                        {
                            idx = i + 1;
                            id = -1;
                            name = "";
                        }
                    }
                }
            }
        }
    }
}
