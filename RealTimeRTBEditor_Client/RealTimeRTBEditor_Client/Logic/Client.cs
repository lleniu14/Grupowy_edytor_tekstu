using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Windows;

namespace RealTimeRTBEditor_Client
{
    public class Client
    {
        private static readonly Socket ClientSocket = new Socket
        (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static bool recivedId = false;
        private const int PORT = 8080;
        private const int BUFFER = 104857600;
        public int DocToShareId;
        public Client()
        {
            ConnectToServer();
            ReciveData();
        }

        private static void ConnectToServer()
        {
            int attempts = 0;
            while (!ClientSocket.Connected)
            {
                try
                {
                    attempts++;
                    ClientSocket.Connect(IPAddress.Loopback, PORT);
                }
                catch (SocketException)
                {
                    return;
                }
            }
        }
        public async void ReciveData()
        {
            EditorData editorData;
            string docList;
            string editorsList;
            Document document;
            await Task.Run(() =>
            {
                while (ClientSocket.Connected)
                {
                    var buffer = new byte[BUFFER];
                    int received = ClientSocket.Receive(buffer, SocketFlags.None);
                    var data = new byte[received];
                    Array.Copy(buffer, data, received);
                    string code;
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        BinaryReader br = new BinaryReader(ms);
                        code = br.ReadString();
                    }
                    switch (code)
                    {
                        case "01":
                            editorData = EditorDataFromBytes(data);
                            LogIn?.Invoke(this, editorData);
                            break;
                        case "02":
                            editorData = EditorDataFromBytes(data);
                            Register?.Invoke(this, editorData);
                            break;
                        case "03":
                            docList = DocumentListFromBytes(data);
                            ReciveList?.Invoke(this, docList);
                            break;
                        case "04":
                            docList = DocumentListFromBytes(data);
                            ReciveSharedList?.Invoke(this,docList);
                            break;
                        case "05":
                            document = DocFromBytes(data);
                            ReciveDocument?.Invoke(this, document);
                            break;
                        case "06":
                            UpdateDto dto;
                            dto = UpdateDtoFromBytes(data);
                            UpdateRequest?.Invoke(this, dto);
                            break;
                        case "07":
                            AcknowledgeDto acknowledgeDto;
                            acknowledgeDto = AckDtoFromBytes(data);
                            AckRequest?.Invoke(this, acknowledgeDto);
                            break;
                        case "08":
                            editorsList = DocumentListFromBytes(data);
                            ReciveUsersList?.Invoke(this, editorsList);
                            break;
                        default:
                            break;
                    }
                }
            });
        }

        #region Send
        public void SendRequest(string code)
        {
            byte[] buffer = CodeToBytes(code);
            ClientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        public void SendUpdate(UpdateDto dto)
        {
            byte[] buffer = UpdateDtoToBytes(dto);
            ClientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }
        public void SendAuthorizationData(AuthorizationData authorizationData,string code)
        {
            byte[] buffer = AuthToBytes(authorizationData,code);
            try
            {
                ClientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
            }
            catch { MessageBox.Show("Nie można połączyć z serwerm"); }
        }

        public void SendCreateDocument(string name)
        {
            byte[] buffer = CreateDocumentToBytes(name, "05");
            ClientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        public void SendChooseDoc(int id)
        {
            byte[] buffer = ChooseDocumentToBytes(id, "07");
            ClientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }
        public void SendChooseUser(int userId)
        {
            byte[] buffer = ChooseUserToBytes(userId,DocToShareId, "09");
            ClientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        public void SendRemoveDocument(int DocId)
        {
            byte[] buffer = RemoveDocumentToBytes(DocId, "06");
            ClientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        #endregion

        #region FromBytes
        public static UpdateDto UpdateDtoFromBytes(byte[] buffer)
        {
            UpdateDto retVal = new UpdateDto();
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                BinaryReader br = new BinaryReader(ms);
                br.ReadString();
                retVal.DocumentId = int.Parse(br.ReadString());
                retVal.MemberName = br.ReadString();
                retVal.PreviousRevisionId = br.ReadInt32();
                retVal.PreviousHashLength = br.ReadInt32();
                retVal.PreviousHash = br.ReadBytes(retVal.PreviousHashLength);
                retVal.NewRevisionId = br.ReadInt32();
                retVal.NewHashLength = br.ReadInt32();
                retVal.NewHash = br.ReadBytes(retVal.NewHashLength);
                retVal.Patch = br.ReadString();
                retVal.EditorCount = br.ReadInt32();
            }

            return retVal;
        }

        public static Document DocFromBytes(byte[] buffer)
        {
            Document retVal = new Document();
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                BinaryReader br = new BinaryReader(ms);
                br.ReadString();
                retVal.Id = br.ReadInt32();
                retVal.Name = br.ReadString();
                retVal.CurrentRevisionId = br.ReadInt32();
                retVal.Content = br.ReadString();
                retVal.Chl = br.ReadInt32();
                retVal.CurrentHash = br.ReadBytes(retVal.Chl);
            }
            return retVal;
        }

        public static AcknowledgeDto AckDtoFromBytes(byte[] buffer)
        {
            AcknowledgeDto retVal = new AcknowledgeDto();
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                BinaryReader br = new BinaryReader(ms);
                br.ReadString();
                retVal.PreviousRevisionId = br.ReadInt32();
                retVal.PreviousHashLength = br.ReadInt32();
                retVal.PreviousHash = br.ReadBytes(retVal.PreviousHashLength);
                retVal.NewRevisionId = br.ReadInt32();
                retVal.NewHashLength = br.ReadInt32();
                retVal.NewHash = br.ReadBytes(retVal.NewHashLength);
                retVal.DocumentId = br.ReadInt32();
            }

            return retVal;
        }
        public static EditorData EditorDataFromBytes(byte[] buffer)
        {
            EditorData retVal = new EditorData();
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                BinaryReader br = new BinaryReader(ms);
                br.ReadString();
                retVal.Authorized = br.ReadBoolean();
                retVal.EditorId = br.ReadInt32();
                retVal.MyDocuments = br.ReadString();
                retVal.SharedDocuments = br.ReadString();
            }
            return retVal;
        }

        public static string DocumentListFromBytes(byte[] buffer)
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

        #endregion

        #region ToBytes


        public static byte[] UpdateDtoToBytes(UpdateDto document)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write("10");
                bw.Write(document.DocumentId);
                bw.Write(document.PreviousRevisionId);
                bw.Write(document.PreviousHashLength);
                bw.Write(document.PreviousHash);
                bw.Write(document.Patch);
                bw.Write(document.MemberName);
                return ms.ToArray();
            }
        }

        

        public static byte[] AuthToBytes(AuthorizationData authorization,string code)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write(code);
                bw.Write(authorization.username);
                bw.Write(authorization.password);
                return ms.ToArray();
            }
        }

        public static byte[] CreateDocumentToBytes(string name, string code)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write(code);
                bw.Write(name);
                return ms.ToArray();
            }
        }
        public static byte[] RemoveDocumentToBytes(int DocId, string code)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write(code);
                bw.Write(DocId);
                return ms.ToArray();
            }
        }

        public static byte[] ChooseDocumentToBytes(int DocId, string code)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write(code);
                bw.Write(DocId);
                return ms.ToArray();
            }
        }

        public static byte[] ChooseUserToBytes(int userId,int docId, string code)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write(code);
                bw.Write(userId);
                bw.Write(docId);
                return ms.ToArray();
            }
        }

        public static byte[] CodeToBytes(string code)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write(code);
                return ms.ToArray();
            }
        }

        #endregion


        public event EventHandler<UpdateDto> UpdateRequest;
        public event EventHandler<string> ReciveList;
        public event EventHandler<string> ReciveUsersList;
        public event EventHandler<string> ReciveSharedList;
        public event EventHandler<Document> ReciveDocument;
        public event EventHandler<AcknowledgeDto> AckRequest;
        public event EventHandler<EditorData> LogIn;
        public event EventHandler<EditorData> Register;
    }

    public class EditorData
    {
        public bool Authorized { get; set; }
        public int EditorId { get; set; }
        public string MyDocuments { get; set; }
        public string SharedDocuments { get; set; }
    }
}