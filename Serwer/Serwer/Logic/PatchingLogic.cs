using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DiffMatchPatch;
using System.Security.Cryptography;
using System.Net;
using System.Net.Sockets;
using Serwer.Model;
using Serwer.Model.Interfaces;
using Serwer.Model.Database;
using Serwer.Model.Methods;

namespace Serwer.Logic
{
    class PatchingLogic
    {
        private const int SUPPORTED_NUM_OF_REACTIVE_UPDATES = 10;
        private const int FIRST_VALID_REVISON_ID = 1;

        private readonly HashSet<string> _pendingDocumentRequests = new HashSet<string>();
        private readonly SHA1 _sha1 = new SHA1CryptoServiceProvider();
        private readonly diff_match_patch _diffMatchPatch = new diff_match_patch();
        private readonly Program _program;
        private readonly IDocument _documentMethods;
        private readonly IEditor _editorMethods;
        private readonly IRevision _revisionMethods;
        private readonly IUpdateDto _updateMethods;
        private readonly object Lock = new object();

        public PatchingLogic(Program program,IDocument idoc,IEditor ied, IRevision irev,IUpdateDto iup)
        { 
            _program = program;
            _documentMethods = idoc;
            _editorMethods = ied;
            _revisionMethods = irev;
            _updateMethods = iup;
            program.CreateDocument += CreateDocument;
            program.UpdateRequest += UpdateRequest;
        }
        private void CreateDocument(object sender, DocToCreate doc)
        {
            AddDocument(new DocumentDto
            {
                Content = @"{\rtf1\ansi\ansicpg1252\uc1\htmautsp\deff2{\fonttbl{\f0\fcharset0 Times New Roman;}{\f2\fcharset0 Segoe UI;}}{\colortbl\red0\green0\blue0;\red255\green255\blue255;}\loch\hich\dbch\pard\plain\ltrpar\itap0{\lang1033\fs18\f2\cf0 \cf0\ql{\f2 \li0\ri0\sa0\sb0\fi0\ql\par}}}",
                RevisionId = 1,
                EditorCount = 1
            },doc);
        }
        private void AddDocument(DocumentDto dto,DocToCreate doc)
        {
            var hash = GetHash(dto.Content);
            Editor editor = _editorMethods.GetEditorById(doc.EditortId);
            var document = new Document
            {
                DocumentId = doc.DocumentId,
                Editor = editor,
                Name = doc.Name,
                CurrentRevisionId = dto.RevisionId,
                CurrentHash = hash,
                CurrentHashLength = hash.Length,
                Content = dto.Content,

            };
            _documentMethods.AddDocument(document);
            List<Patch> patches = new List<Patch>();
            _revisionMethods.AddRevision(new Revision
            {
                RevId = dto.RevisionId,
                Content = document.Content,
                Document = document,
                UpdateDto = new UpdateDto
                {
                    PreviousRevisionId = 0,
                    PreviousHash = new byte[] { },
                    NewHash = hash,
                    NewRevisionId = dto.RevisionId,
                    Patch = _diffMatchPatch.patch_toText(patches),
                }
            });
        }

        private byte[] GetHash(string content)
        {
            return _sha1.ComputeHash(Encoding.UTF8.GetBytes(content));
        }

        /*public void UpdateRequest(object sender,UpReq update)
        {
            List<Revision> revisions = _revisionMethods.GetDocumentRevision(int.Parse(update.dto.DocumentId));
                var document = _documentMethods.GetDocument(int.Parse(update.dto.DocumentId));
                var updateDto = update.dto;
                var client = update.client;
                var currentRevision = revisions.SingleOrDefault(rev => rev.RevId == document.CurrentRevisionId);
                Console.WriteLine();
                var lastUpdate = _updateMethods.GetUpdateDto(currentRevision.UpdateDto.UpdateDtoId);
                var secondLastUpdate = new UpdateDto { NewRevisionId = -1 };
                if (document.CurrentRevisionId > FIRST_VALID_REVISON_ID)
                {
                    var tmp = revisions.SingleOrDefault(rev => rev.RevId == document.CurrentRevisionId - 1);
                    secondLastUpdate = _updateMethods.GetUpdateDto(tmp.RevisionId);
                }

                bool creationSucessfull = false;

                if (IsFirstPreviousOfSecond(lastUpdate, updateDto))
                {
                    var result = _diffMatchPatch.patch_apply(_diffMatchPatch.patch_fromText(updateDto.Patch), document.Content);
                    if (result.Item2.All(x => x))
                    {
                        document.Content = result.Item1;
                        creationSucessfull = true;
                    }
                }
                else
                {
                    var revision = revisions.SingleOrDefault(rev => rev.RevId == updateDto.PreviousRevisionId - 1);
                    if (revision.RevId + SUPPORTED_NUM_OF_REACTIVE_UPDATES >= currentRevision.RevId) //MOŻE NIE DZIAŁAĆ
                    {
                        var nextRevision = revisions.SingleOrDefault(rev => rev.RevId == revision.RevId + 1);
                        while (nextRevision.UpdateDto.PreviousHash.SequenceEqual(updateDto.PreviousHash)&& nextRevision.RevId < currentRevision.RevId)
                        {
                            revision = nextRevision;
                            nextRevision = revisions.SingleOrDefault(rev => rev.RevId == revision.RevId + 1);
                        }

                        var content = revision.Content;
                        var tmpRevision = revision;
                        var patch = updateDto.Patch;
                        while (tmpRevision.RevId <= currentRevision.RevId)
                        {
                            var result = _diffMatchPatch.patch_apply(_diffMatchPatch.patch_fromText(patch), content);
                            if (result.Item2.All(x => x))
                            {
                                content = result.Item1;
                                if (tmpRevision.RevId == currentRevision.RevId)
                                {
                                    break;
                                }
                                tmpRevision = revisions.SingleOrDefault(rev => rev.RevId == tmpRevision.RevId + 1);
                                patch = _updateMethods.GetUpdateDto(tmpRevision.RevisionId).Patch;
                            }
                        }
                        document.Content = content;
                        updateDto.Patch = _diffMatchPatch.patch_toText(_diffMatchPatch.patch_make(currentRevision.Content, content));
                        creationSucessfull = true;
                    }
                }

                if (creationSucessfull)
                {

                    document.CurrentRevisionId = currentRevision.RevId + 1;
                    document.CurrentHash = GetHash(document.Content);
                    updateDto.NewRevisionId = document.CurrentRevisionId;
                    updateDto.NewHash = document.CurrentHash;
                    _revisionMethods.AddRevision(new Revision
                    {
                        RevId = document.CurrentRevisionId,
                        Document = document,
                        Content = document.Content,
                        UpdateDto = updateDto
                    });


                    var acknowledgeDto = new AcknowledgeDto
                    {
                        PreviousRevisionId = updateDto.PreviousRevisionId,
                        PreviousHashLength = updateDto.PreviousHash.Length,
                        PreviousHash = updateDto.PreviousHash,
                        NewRevisionId = updateDto.NewRevisionId,
                        NewHashLenth = updateDto.PreviousHash.Length,
                        NewHash = updateDto.NewHash,
                        DocumentId = document.DocumentId
                    };

                    byte[] buffer = AckDtoToBytes(acknowledgeDto);
                    client.Send(buffer, 0, buffer.Length, SocketFlags.None);

                    var newUpdateDto = new UpdateDto
                    {
                        DocumentId = document.DocumentId.ToString(),
                        MemberName = updateDto.MemberName,
                        PreviousRevisionId = updateDto.PreviousRevisionId,
                        PreviousHashLength = updateDto.PreviousHash.Length,
                        PreviousHash = updateDto.PreviousHash,
                        NewRevisionId = updateDto.NewRevisionId,
                        NewHashLength = updateDto.NewHash.Length,
                        NewHash = updateDto.NewHash,
                        Patch = updateDto.Patch,
                        EditorCount = document.Editors_Count
                    };

                    foreach (var editor in _program.currentConected.Keys)
                    {
                        if (client != editor)
                        {
                            byte[] buff = UpdateDtoToBytes(newUpdateDto);
                            editor.Send(buff, 0, buff.Length, SocketFlags.None);
                        }
                    }

                    _documentMethods.UpdateDocument(document);
                }
        }*/

        public void UpdateRequest(object sender, UpReq update)
        {
            if (_program.documents.ContainsKey(int.Parse(update.dto.DocumentId))) ;
            {
                var document = _program.documents[int.Parse(update.dto.DocumentId)];
                CreatePatchForUpdate(document, update.dto, update.client);
            }
        }

        private void CreatePatchForUpdate(Document document, UpdateDto updateDto, Socket client)
        {
            var currentRevision = document.Revisions.Where(rev => rev.RevId == document.CurrentRevisionId).FirstOrDefault();
            var lastUpdate = currentRevision.UpdateDto;
            var secondLastUpdate = new UpdateDto { NewRevisionId = -1 };
            if (document.CurrentRevisionId > FIRST_VALID_REVISON_ID)
            {
                secondLastUpdate = document.Revisions.Where(rev => rev.RevId == document.CurrentRevisionId-1).FirstOrDefault().UpdateDto;
            }

            bool creationSucessfull = false;

            if (IsFirstPreviousOfSecond(lastUpdate, updateDto))
            {
                var result = _diffMatchPatch.patch_apply(_diffMatchPatch.patch_fromText(updateDto.Patch), document.Content);
                if (result.Item2.All(x => x))
                {
                    document.Content = result.Item1;
                    creationSucessfull = true;
                }
            }
            else
            {
                try
                {
                    var revision = document.Revisions.Where(rev => rev.RevId == updateDto.PreviousRevisionId).FirstOrDefault();
                    if (revision.RevId + SUPPORTED_NUM_OF_REACTIVE_UPDATES >= currentRevision.RevId)
                    {
                        var nextRevision = document.Revisions.Where(rev => rev.RevId == revision.RevId + 1).FirstOrDefault();
                        while (
                            nextRevision.UpdateDto.PreviousHash.SequenceEqual(updateDto.PreviousHash)
                            && nextRevision.RevId < currentRevision.RevId)
                        {
                            revision = nextRevision;
                            nextRevision = document.Revisions.Where(rev => rev.RevId == nextRevision.RevId + 1).FirstOrDefault();
                        }

                        var content = revision.Content;
                        var tmpRevision = revision;
                        var patch = updateDto.Patch;
                        while (tmpRevision.RevId <= currentRevision.RevId)
                        {
                            var result = _diffMatchPatch.patch_apply(_diffMatchPatch.patch_fromText(patch), content);
                            if (result.Item2.All(x => x))
                            {
                                content = result.Item1;
                                if (tmpRevision.RevId == currentRevision.RevId)
                                {
                                    break;
                                }
                                tmpRevision = document.Revisions.Where(rev => rev.RevId == tmpRevision.RevId + 1).FirstOrDefault(); ;
                                patch = tmpRevision.UpdateDto.Patch;
                            }
                        }
                        document.Content = content;
                        updateDto.Patch = _diffMatchPatch.patch_toText(_diffMatchPatch.patch_make(currentRevision.Content, content));
                        creationSucessfull = true;
                    }
                }
                catch
                {
                    creationSucessfull = true;
                }
            }

            if (creationSucessfull)
            {

                document.CurrentRevisionId = currentRevision.RevId + 1;
                document.CurrentHash = GetHash(document.Content);
                updateDto.NewRevisionId = document.CurrentRevisionId;
                updateDto.NewHash = document.CurrentHash;
                Revision rev = (new Revision
                {
                    RevId = document.CurrentRevisionId,
                    Document = document,
                    Content = document.Content,
                    UpdateDto = updateDto
                });
                /*lock (Lock)
                {
                    _revisionMethods.AddRevision(rev);
                }*/
                document.Revisions.Add(rev);
                var acknowledgeDto = new AcknowledgeDto
                {
                    PreviousRevisionId = updateDto.PreviousRevisionId,
                    PreviousHashLength = updateDto.PreviousHash.Length,
                    PreviousHash = updateDto.PreviousHash,
                    NewRevisionId = updateDto.NewRevisionId,
                    NewHashLenth = updateDto.PreviousHash.Length,
                    NewHash = updateDto.NewHash,
                    DocumentId = document.DocumentId
                };

                byte[] buffer = AckDtoToBytes(acknowledgeDto);
                client.Send(buffer, 0, buffer.Length, SocketFlags.None);

                var newUpdateDto = new UpdateDto
                {
                    DocumentId = document.DocumentId.ToString(),
                    MemberName = updateDto.MemberName,
                    PreviousRevisionId = updateDto.PreviousRevisionId,
                    PreviousHashLength = updateDto.PreviousHash.Length,
                    PreviousHash = updateDto.PreviousHash,
                    NewRevisionId = updateDto.NewRevisionId,
                    NewHashLength = updateDto.NewHash.Length,
                    NewHash = updateDto.NewHash,
                    Patch = updateDto.Patch,
                    EditorCount = document.Editors_Count
                };

                foreach (var editor in _program.currentConected.Keys)
                {
                    if (client != editor)
                    {
                        byte[] buff = UpdateDtoToBytes(newUpdateDto);
                        editor.Send(buff, 0, buff.Length, SocketFlags.None);
                    }
                }
                //_documentMethods.UpdateDocument(document);
            }
        }

        private bool IsFirstPreviousOfSecond(UpdateDto lastUpdate, UpdateDto updateDto)
        {
            return lastUpdate.NewRevisionId == updateDto.PreviousRevisionId &&
                   lastUpdate.NewHash.SequenceEqual(updateDto.PreviousHash);
        }

        public byte[] AckDtoToBytes(AcknowledgeDto document)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write("07");
                bw.Write(document.PreviousRevisionId);
                bw.Write(document.PreviousHashLength);
                bw.Write(document.PreviousHash);
                bw.Write(document.NewRevisionId);
                bw.Write(document.NewHashLenth);
                bw.Write(document.NewHash);
                bw.Write(document.DocumentId);
                return ms.ToArray();
            }
        }

        public byte[]  UpdateDtoToBytes(UpdateDto document)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write("06");
                bw.Write(document.DocumentId);
                bw.Write(document.MemberName);
                bw.Write(document.PreviousRevisionId);
                bw.Write(document.PreviousHashLength);
                bw.Write(document.PreviousHash);
                bw.Write(document.NewRevisionId);
                bw.Write(document.NewHashLength);
                bw.Write(document.NewHash);
                bw.Write(document.Patch);
                bw.Write(document.EditorCount);
                return ms.ToArray();
            }
        }
    }
}