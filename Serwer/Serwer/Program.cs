using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using Serwer.Logic;
using Serwer.Model;
using Serwer.Model.Interfaces;
using Serwer.Model.Database;
using Serwer.Model.Methods;
using System.Linq;


namespace Serwer
{
    class Program
    {
        private readonly object Lock = new object();
        private readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public Dictionary<int, Document> documents = new Dictionary<int, Document>();
        private readonly List<Socket> clientSockets = new List<Socket>();
        private const int BUFFER_SIZE = 10485760;
        private const int PORT = 8080;
        private readonly byte[] buffer = new byte[BUFFER_SIZE];
        public Dictionary<Socket,EditedDocs> currentConected = new Dictionary<Socket, EditedDocs>();
        private readonly DatabaseContext _databaseContext = new DatabaseContext();
        private IDocument _documentMethods;
        private IEditor _editorMethods;
        private IRevision _revisionMethods;
        private IUpdateDto _updateMethods;
        PatchingLogic patchingClientLogic;
        static void Main()
        {
            Console.Title = "Server";
            Program program = new Program();
            program.SetupServer();
            Console.ReadLine(); 
            program.CloseAllSockets();
        }

        private void SetupServer()
        {
            _documentMethods = new DocumentMethods(_databaseContext);
            _editorMethods = new EditorMethods(_databaseContext);
            _revisionMethods = new RevisionMethods(_databaseContext);
            _updateMethods = new UpdateDtoMethods(_databaseContext);
            Console.WriteLine("Setting up server...");
            patchingClientLogic = new PatchingLogic(this, _documentMethods, _editorMethods, _revisionMethods, _updateMethods);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, PORT));
            serverSocket.Listen(0);
            serverSocket.BeginAccept(AcceptCallback, null);
            Console.WriteLine("Server setup complete");
        }

        private void CloseAllSockets()
        {
            foreach (Socket socket in clientSockets)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            serverSocket.Close();
        }

        private void AcceptCallback(IAsyncResult AR)
        {
            Socket socket;
            try
            {
                socket = serverSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            clientSockets.Add(socket);
            ReciveData(socket);
            Console.WriteLine("Client connected, waiting for request...");
            serverSocket.BeginAccept(AcceptCallback, null);
        }

        public async void ReciveData(Socket current)
        {
            AuthorizationData authorizationData;
            EditorData editorData;
            Editor editor;
            Document document;
            int id;
            List<Editor> editors;
            byte[] message;
            await Task.Run(() =>
            {
                while (current.Connected)
                {
                    int received = -1;
                    var buffer = new byte[BUFFER_SIZE];
                    try
                    {
                        received = current.Receive(buffer, SocketFlags.None);
                    }
                    catch
                    {
                        break;
                    }
                    var recBuf = new byte[received];
                    Array.Copy(buffer, recBuf, received);
                    string code;
                    try
                    {
                        using (MemoryStream ms = new MemoryStream(recBuf))
                        {
                            BinaryReader br = new BinaryReader(ms);
                            code = br.ReadString();
                        }
                    }
                    catch
                    {
                        code = "-1";
                    }
                    switch(code)
                    {
                        case "01":  //logowanie
                            authorizationData = AuthorizationDataFromBytes(recBuf);
                            editor = _editorMethods.GetEditorByLogin(authorizationData.username);
                            editorData = new EditorData();
                            if(editor != null && editor.Password == authorizationData.password)
                            {
                                editorData.Authorized = true;
                                editorData.EditorId = editor.EditorId;
                                editorData.SharedDocuments = editor.EditableDocuments;
                                editorData.MyDocuments = _editorMethods.GetEditorDocuments(editor);
                                if (!currentConected.ContainsKey(current))
                                {
                                    currentConected.Add(current, new EditedDocs {EditorId=editor.EditorId, DocId = -1 });
                                    message = EditorDataToBytes(editorData, "01");
                                    current.Send(message, 0, message.Length, SocketFlags.None);
                                }
                            }
                            else
                            {
                                editorData.Authorized = false;
                                editorData.EditorId = -1;
                                editorData.SharedDocuments = "";
                                editorData.MyDocuments = "";
                                message = EditorDataToBytes(editorData, "01");
                                current.Send(message, 0, message.Length, SocketFlags.None);
                            }
                            break;
                        case "02":  //rejestracja
                            editorData = new EditorData();
                            authorizationData = AuthorizationDataFromBytes(recBuf);
                            editors = _editorMethods.GetAllEditors();
                            bool loginExist = false;
                            for (int i = 0; i < editors.Count; i++) 
                            {
                                if (editors[i].Login == authorizationData.username) loginExist = true;
                            }
                            if(!loginExist)
                            {
                                int _EditorId;
                                if (editors.Count == 0) _EditorId = 0;
                                else _EditorId = editors[editors.Count - 1].EditorId + 1;
                                editor = new Editor
                                {
                                    EditorId = _EditorId,
                                    Login = authorizationData.username,
                                    Password = authorizationData.password
                                };
                                _editorMethods.AddEditor(editor);
                                editorData.Authorized = true;
                                editorData.EditorId = editor.EditorId;
                                editorData.MyDocuments = "";
                                editorData.SharedDocuments = "";
                            }
                            else
                            {
                                editorData.Authorized = false;
                                editorData.EditorId = -1;
                                editorData.MyDocuments = "";
                                editorData.SharedDocuments = "";
                            }
                            message = EditorDataToBytes(editorData, "02");
                            current.Send(message, 0, message.Length, SocketFlags.None);
                            break;
                        case "03": //Lista dokumentów użytkownika
                            string result = _documentMethods.GetEditorDocuments(currentConected[current].EditorId);
                            message = DocumentListToByte(result, "03");
                            current.Send(message, 0, message.Length, SocketFlags.None);
                            break;
                        case "04": //Lista dokumentów udostępnionych użytkownikowi
                            editor = _editorMethods.GetEditorById(currentConected[current].EditorId);
                            message = DocumentListToByte(editor.EditableDocuments, "04");
                            current.Send(message, 0, message.Length, SocketFlags.None);
                            break;
                        case "05": //Utwórz nowy dokument
                            string name = CreateDocumentFromBytes(recBuf);
                            int DocId = _documentMethods.GetNewDocId();
                            CreateDocument?.Invoke(this, new DocToCreate {DocumentId = DocId, Name = name, EditortId = currentConected[current].EditorId });
                            string newDoclist = _documentMethods.GetEditorDocuments(currentConected[current].EditorId);
                            message = DocumentListToByte(newDoclist, "03");
                            current.Send(message, 0, message.Length, SocketFlags.None);
                            break;
                        case "06": //Usuń dokument
                            id = RemoveDocumentFromBytes(recBuf);
                            if (documents.ContainsKey(id)) documents.Remove(id);
                            _editorMethods.DeleteSharedDoc(id);
                            _documentMethods.DeleteDocument(_documentMethods.GetDocument(id));
                            break;
                        case "07": //Pobierz dokument
                            id = ChooseDocumentFromBytes(recBuf);
                            lock (Lock)
                            {
                                if (!documents.ContainsKey(id))
                                {
                                    document = _documentMethods.GetDocument(id);
                                    document.Revisions = _revisionMethods.GetDocumentRevision(id);
                                    documents.Add(id, document);
                                    if(currentConected[current].DocId!=-1) SaveDoc(id);
                                    currentConected[current].DocId = id;
                                }
                                else
                                {
                                    document = documents[id];
                                }
                                message = DocumentToBytes(document);
                                current.Send(message, 0, message.Length, SocketFlags.None);
                            }
                            break;
                        case "08": //Pobierz listę użytkowników
                            editors = _editorMethods.GetAllEditors();
                            string users = "";
                            for (int i = 0; i < editors.Count; i++) 
                            {
                                if(editors[i].EditorId!=currentConected[current].EditorId) users = users + editors[i].EditorId + "." + editors[i].Login + ",";
                            }
                            message = DocumentListToByte(users, "08");
                            current.Send(message, 0, message.Length, SocketFlags.None);
                            break;
                        case "09": //udostępnij dokument
                            DocToShare toShare = ChooseUserFromBytes(recBuf);
                            editor = _editorMethods.GetEditorById(toShare.UserId);
                            editor.EditableDocuments = editor.EditableDocuments + toShare.DocId + "." + _documentMethods.GetDocument(toShare.DocId).Name + ",";
                            _editorMethods.UpdateEditor(editor);
                            break;
                        case "10":
                            UpdateDto update = UpdateDtoFromBytes(recBuf);
                            lock (Lock)
                            {
                                patchingClientLogic.UpdateRequest(this, new UpReq { client = current, dto = update });
                            }
                            Console.WriteLine(update.Patch);
                            break;
                        default:
                            Console.WriteLine("Nieznany kod");
                            break;
                    }
                }
                });
            Console.WriteLine("Client disconnected");
            id = currentConected[current].DocId;
            currentConected.Remove(current);
            lock (Lock)
            {
                if (id != -1) SaveDoc(id);
            }
        }

        public byte[] DocumentToBytes(Document document)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write("05");
                bw.Write(document.DocumentId);
                bw.Write(document.Name);
                bw.Write(document.CurrentRevisionId);
                bw.Write(document.Content);
                bw.Write(document.CurrentHashLength);
                bw.Write(document.CurrentHash);
                return ms.ToArray();
            }
        }
        public static UpdateDto UpdateDtoFromBytes(byte[] buffer)
        {
            UpdateDto retVal = new UpdateDto();

            using (MemoryStream ms = new MemoryStream(buffer))
            {
                BinaryReader br = new BinaryReader(ms);
                br.ReadString();
                retVal.DocumentId = br.ReadInt32().ToString();
                retVal.PreviousRevisionId = br.ReadInt32();
                retVal.PreviousHashLength = br.ReadInt32();
                retVal.PreviousHash = br.ReadBytes(retVal.PreviousHashLength);
                retVal.Patch = br.ReadString();
                retVal.MemberName = br.ReadString();
            }

            return retVal;
        }

        private void SaveDoc(int id)
        {
            ICollection<EditedDocs> editedDocs = currentConected.Values;
            foreach (EditedDocs e in editedDocs)
            {
                if (e.DocId == id) return;
            }
            List<Revision> revisions = documents[id].Revisions.OrderByDescending(rev => rev.RevId).ToList();
            int max = revisions.Count;
            if (max > 10) max = 10;
            for(int i=0;i<max;i++)
            {
                _revisionMethods.AddRevision(revisions[i]);
            }
            _documentMethods.UpdateDocument(documents[id]);
        }

        public byte[] UpdateDtoToBytes(UpdateDto document)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write(document.DocumentId);
                bw.Write(document.MemberName);
                bw.Write(document.PreviousRevisionId);
                bw.Write(document.Patch);
                return ms.ToArray();
            }
        }
        public static AuthorizationData AuthorizationDataFromBytes(byte[] buffer)
        {
            AuthorizationData retVal = new AuthorizationData();
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                BinaryReader br = new BinaryReader(ms);
                br.ReadString();
                retVal.username = br.ReadString();
                retVal.password = br.ReadString();
            }
            return retVal;
        }

        public byte[] EditorDataToBytes(EditorData data,string code)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write(code);
                bw.Write(data.Authorized);
                bw.Write(data.EditorId);
                bw.Write(data.MyDocuments);
                if (data.SharedDocuments != null) bw.Write(data.SharedDocuments);
                else bw.Write("");
                return ms.ToArray();
            }
        }

        public byte[] DocumentListToByte(string list, string code)
        {
            if (list == null) list = "";
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write(code);
                bw.Write(list);
                return ms.ToArray();
            }
        }

        public string CreateDocumentFromBytes(byte[] buffer)
        {
            string retVal = "";
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                BinaryReader br = new BinaryReader(ms);
                br.ReadString();
                retVal = br.ReadString();
            }
            return retVal;
        }

        public int RemoveDocumentFromBytes(byte[] buffer)
        {
            int retVal = -1;
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                BinaryReader br = new BinaryReader(ms);
                br.ReadString();
                retVal = br.ReadInt32();
            }
            return retVal;
        }

        public int ChooseDocumentFromBytes(byte[] buffer)
        {
            int retVal = -1;
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                BinaryReader br = new BinaryReader(ms);
                br.ReadString();
                retVal = br.ReadInt32();
            }
            return retVal;
        }

        public DocToShare ChooseUserFromBytes(byte[] buffer)
        {
            DocToShare retVal = new DocToShare();
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                BinaryReader br = new BinaryReader(ms);
                br.ReadString();
                retVal.UserId = br.ReadInt32();
                retVal.DocId = br.ReadInt32();
            }
            return retVal;
        }


        public event EventHandler<DocToCreate> CreateDocument;
        public event EventHandler<UpReq> UpdateRequest;
    }

    public class DocToCreate
    {
        public int DocumentId { get; set; }
        public int EditortId { get; set; }
        public string Name { get; set; }
    }

    public class UpReq
    {
        public Socket client;
        public UpdateDto dto;
    }

    public class AuthorizationData
    {
        public string username { get; set; }
        public string password { get; set; }
    }

    public class EditorData
    {
        public bool Authorized { get; set; }
        public int EditorId { get; set; }
        public string MyDocuments { get; set; }
        public string SharedDocuments { get; set; }
    }
    
    public class DocToShare
    {
        public int DocId { get; set; }
        public int UserId { get; set; }
    }

    public class EditedDocs
    {
        public int EditorId { get; set; }
        public int DocId { get; set; }
    }
}