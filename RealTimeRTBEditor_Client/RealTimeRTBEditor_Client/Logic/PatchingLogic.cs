using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiffMatchPatch;
using System.Security.Cryptography;
using System.Windows;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace RealTimeRTBEditor_Client
{
    public class PatchingLogic
    {
        private readonly SHA1 _sha1 = new SHA1CryptoServiceProvider();
        private readonly diff_match_patch _diffMatchPatch = new diff_match_patch();
        public string _memberName = "01";
        private readonly TextEditor _editor;

        public PatchingLogic(TextEditor editor)
        {
            _editor = editor;
            _editor.CreateDocument += CreateDocument;
            _editor.UpdateDocument += UpdateDocument;
        }

        private void CreateDocument(object sender, Document doc)
        {
            _editor.document = doc;
        }

        private void UpdateDocument(object sender, UpdateDocumentRequest request)
        {
            var document = _editor.document;
            if (document.PendingUpdate == null)
            {
                var updateDto = CreateUpdateDto(document, request.NewContent);
                document.PendingUpdate = updateDto;
                SendUpdateToDocumentOwner(document, updateDto);
            }
        }

        private UpdateDto CreateUpdateDto(Document document, string content)
        {
            var updateDto = new UpdateDto
            {
                DocumentId = document.Id,
                PreviousRevisionId = document.CurrentRevisionId,
                PreviousHashLength = document.CurrentHash.Length,
                PreviousHash = document.CurrentHash,
                Patch = _diffMatchPatch.patch_toText(_diffMatchPatch.patch_make(document.Content, content)),
                MemberName = _memberName,
            };
            return updateDto;
        }

        private void SendUpdateToDocumentOwner(Document document, UpdateDto dto)
        {
            _editor.client.SendUpdate(dto);
        }

        public void AckRequest(AcknowledgeDto dto)
        {
            if (_editor.document.Id == dto.DocumentId)
            {
                var document = _editor.document;
                if (document.PendingUpdate != null)
                {
                    if (document.PendingUpdate.PreviousRevisionId == dto.PreviousRevisionId && document.PendingUpdate.PreviousHash.SequenceEqual(dto.PreviousHash))
                    {
                        ConfirmPendingUpdate(document, dto);
                    }
                    else if (document.OutOfSyncAcknowledge == null)
                    {
                        document.OutOfSyncAcknowledge = dto;
                    }
                    else
                    {
                        ReloadDocument(document.Id);
                    }
                }
            }
        }
        private void ReloadDocument(int documentId)
        {
            _editor.ReloadDocument(documentId);
        }

        private void ConfirmPendingUpdate(Document document, AcknowledgeDto dto)
        {
            var result = _diffMatchPatch.patch_apply(_diffMatchPatch.patch_fromText(document.PendingUpdate.Patch), document.Content);
            if (CheckResultIsValidOtherwiseReOpen(result, dto.DocumentId))
            {
                document.PendingUpdate.NewRevisionId = dto.NewRevisionId;
                document.PendingUpdate.NewHash = dto.NewHash;
                UpdateDocument(document, document.PendingUpdate, result);
            }

            document.PendingUpdate = null;
            if (document.OutOfSyncUpdate != null && (document.CurrentRevisionId == document.OutOfSyncUpdate.PreviousRevisionId && document.CurrentHash.SequenceEqual(document.OutOfSyncUpdate.PreviousHash)))
            {
                var outOfSynUpdate = document.OutOfSyncUpdate;
                document.OutOfSyncUpdate = null;
                MergeUpdate(document, outOfSynUpdate);
            }

            var outOfSyncAcknowledge = document.OutOfSyncAcknowledge;
            if (document.OutOfSyncAcknowledge != null && (document.CurrentRevisionId == document.OutOfSyncAcknowledge.PreviousRevisionId && document.CurrentHash.SequenceEqual(document.OutOfSyncAcknowledge.PreviousHash)))
            {
                ConfirmPendingUpdate(document, outOfSyncAcknowledge);
                document.OutOfSyncAcknowledge = null;
            }
            var currentText = _editor.GetText();
            if (document.Content != currentText)
            {
                var updateDto = CreateUpdateDto(document, currentText);
                document.PendingUpdate = updateDto;

                SendUpdateToDocumentOwner(document, updateDto);
            }
            else
            {
                document.PendingUpdate = null;
            }
        }

        private void UpdateDocument(Document document, UpdateDto updateDto, Tuple<string, bool[]> resultAppliedGivenUpdate)
        {
            document.Content = resultAppliedGivenUpdate.Item1;
            document.CurrentRevisionId = updateDto.NewRevisionId;
            document.CurrentHash = GetHash(document.Content);
            if (!document.CurrentHash.SequenceEqual(updateDto.NewHash))
            {
                ReloadDocument(document.Id);
            }
        }

        private byte[] GetHash(string content)
        {
            return _sha1.ComputeHash(Encoding.UTF8.GetBytes(content));
        }

        private bool CheckResultIsValidOtherwiseReOpen(Tuple<string, bool[]> result, int documentId)
        {
            bool ok = result.Item2.All(x => x);
            if (!ok)
            {
                ReloadDocument(documentId);
            }
            return ok;
        }

        public void UpdateRequest(UpdateDto dto)
        {
            if (_editor.document.Id == dto.DocumentId)
            {
                var document = _editor.document;
                ApplyUpdate(document, dto);
            }
        }

        private void ApplyUpdate(Document document, UpdateDto dto)
        {
            if (document.CurrentRevisionId == dto.PreviousRevisionId && document.CurrentHash.SequenceEqual(dto.PreviousHash))
            {
                MergeUpdate(document, dto);
            }
            else if (document.OutOfSyncUpdate == null)
            {
                document.OutOfSyncUpdate = dto;
            }
            else
            {
                ReloadDocument(dto.DocumentId);
            }
        }

        private void MergeUpdate(Document document, UpdateDto updateDto)
        {
            var resultAppliedGivenUpdate = _diffMatchPatch.patch_apply(_diffMatchPatch.patch_fromText(updateDto.Patch), document.Content);
            if (CheckResultIsValidOtherwiseReOpen(resultAppliedGivenUpdate, updateDto.DocumentId))
            {
                if (MergePendingUpdate(document, updateDto, resultAppliedGivenUpdate))
                {
                    UpdateDocument(document, updateDto, resultAppliedGivenUpdate);
                }
            }
            if (document.OutOfSyncUpdate != null && IsFirstPreviousOfSecond(updateDto, document.OutOfSyncUpdate))
            {
                var outOfSynUpdate = document.OutOfSyncUpdate;
                document.OutOfSyncUpdate = null;
                MergeUpdate(document, outOfSynUpdate);
            }

            var outOfSyncAcknowledge = document.OutOfSyncAcknowledge;
            if (outOfSyncAcknowledge != null && IsFirstPreviousOfSecond(updateDto, document.PendingUpdate))
            {
                ConfirmPendingUpdate(document, outOfSyncAcknowledge);
                document.OutOfSyncAcknowledge = null;
            }
        }

        private bool MergePendingUpdate(Document document, UpdateDto updateDto, Tuple<string, bool[]> resultAppliedGivenUpdate)
        {
            var everythingOk = true;
            var pendingUpdate = document.PendingUpdate;

            if (pendingUpdate != null)
            {
                if (CompareRevisions(pendingUpdate, updateDto))
                {
                    everythingOk = MergePendingUpdateAfterGivenUpdate(updateDto, pendingUpdate);
                }
                else
                {
                    everythingOk = MergePendingUpdateBeforeGivenUpdate(document, updateDto, pendingUpdate, resultAppliedGivenUpdate);
                }
            }
            else
            {
                _editor.UpdateText(resultAppliedGivenUpdate.Item1);
            }

            return everythingOk;
        }

        private static bool CompareRevisions(UpdateDto firstUpdateDto, UpdateDto secondUpdateDto)
        {
            return firstUpdateDto.PreviousRevisionId - secondUpdateDto.PreviousRevisionId <= 0;
        }

        private bool MergePendingUpdateAfterGivenUpdate(UpdateDto updateDto, UpdateDto pendingUpdate)
        {
            pendingUpdate.PreviousHash = updateDto.NewHash;

            var documentId = updateDto.DocumentId;
            var result = _diffMatchPatch.patch_apply(_diffMatchPatch.patch_fromText(updateDto.Patch), _editor.GetText());
            var everythingOk = CheckResultIsValidOtherwiseReOpen(result, documentId);
            if (everythingOk)
            {
                _editor.UpdateText(result.Item1);
            }
            return everythingOk;
        }

        private bool MergePendingUpdateBeforeGivenUpdate(Document document, UpdateDto updateDto, UpdateDto pendingUpdate, Tuple<string, bool[]> resultAppliedGivenUpdate)
        {
            pendingUpdate.PreviousHash = updateDto.NewHash;
            var documentId = updateDto.DocumentId;
            var resultAppliedPendingUpdate = _diffMatchPatch.patch_apply(_diffMatchPatch.patch_fromText(pendingUpdate.Patch), document.Content);
            bool everythingOk = CheckResultIsValidOtherwiseReOpen(resultAppliedPendingUpdate, documentId);
            if (everythingOk)
            {
                var resultAfterPatches = _diffMatchPatch.patch_apply(_diffMatchPatch.patch_fromText(updateDto.Patch), resultAppliedPendingUpdate.Item1);
                everythingOk = CheckResultIsValidOtherwiseReOpen(resultAfterPatches, documentId);
                if (everythingOk)
                {
                    pendingUpdate.Patch = _diffMatchPatch.patch_toText(_diffMatchPatch.patch_make(resultAppliedGivenUpdate.Item1, resultAfterPatches.Item1));
                    var mergePatch = _diffMatchPatch.patch_make(resultAppliedPendingUpdate.Item1, _editor.GetText());
                    var resultMerge = _diffMatchPatch.patch_apply(mergePatch, resultAfterPatches.Item1);
                    everythingOk = CheckResultIsValidOtherwiseReOpen(resultMerge, documentId);
                    if (everythingOk)
                    {
                        _editor.UpdateText(resultMerge.Item1);
                    }
                }
            }
            return everythingOk;
        }

        private static bool IsFirstPreviousOfSecond(UpdateDto lastUpdate, UpdateDto updateDto)
        {
            return lastUpdate.NewRevisionId == updateDto.PreviousRevisionId &&
                   lastUpdate.NewHash.SequenceEqual(updateDto.PreviousHash);
        }
    }
}
