using System;
using System.Collections.Generic;

namespace TanamSawit.NPC
{
    /// <summary>
    /// Runner stateless untuk mengelola alur dialog.
    /// Dipanggil oleh DialogueUI. Tidak bergantung pada MonoBehaviour.
    /// </summary>
    public class DialogueRunner
    {
        private DialogueAsset _asset;
        private int _currentIndex;
        private Func<int, string> _dynamicTextResolver;

        /// <summary>Dipicu saat sebuah actionId ditemukan di choice.</summary>
        public event Action<string> OnActionId;

        /// <summary>Dipicu saat dialog berakhir (end_dialogue atau selesai).</summary>
        public event Action OnDialogueEnd;

        /// <summary>Node saat ini, atau null jika dialog belum mulai/selesai.</summary>
        public DialogueAsset.DialogueNode Current =>
            (_asset != null && _currentIndex >= 0 && _currentIndex < _asset.nodes.Count)
                ? _asset.nodes[_currentIndex]
                : null;

        public NPCIdentity Speaker => _asset?.speaker;
        public bool IsActive => _asset != null && _currentIndex >= 0 && _currentIndex < _asset.nodes.Count;

        /// <summary>
        /// Memulai dialog. Resolver opsional untuk mengisi teks dinamis
        /// (mis. tingkat karma untuk Kepala Desa).
        /// </summary>
        public void Start(DialogueAsset dialogueAsset, Func<int, string> dynamicTextResolver = null)
        {
            _asset = dialogueAsset;
            _dynamicTextResolver = dynamicTextResolver;
            _currentIndex = 0;
        }

        /// <summary>Mendapatkan teks node saat ini, mengganti placeholder dinamis.</summary>
        public string GetCurrentText()
        {
            var node = Current;
            if (node == null) return "";

            if (node.isDynamicText && _dynamicTextResolver != null)
                return _dynamicTextResolver(_currentIndex);

            return node.text;
        }

        /// <summary>
        /// Melanjutkan dialog jika tidak ada pilihan (autoNext atau sekuensial).
        /// Jika node punya pilihan, Advance() tidak melakukan apa-apa (tunggu Choose()).
        /// </summary>
        public void Advance()
        {
            var current = Current;
            if (current == null) { EndDialogue(); return; }

            if (current.autoNext >= 0)
            {
                _currentIndex = current.autoNext;
            }
            else if (current.choices.Count == 0)
            {
                _currentIndex++;
                if (_currentIndex >= _asset.nodes.Count)
                    EndDialogue();
            }
            // Jika ada choices, tunggu Choose()
        }

        /// <summary>Memproses pilihan dari pemain.</summary>
        public void Choose(int choiceIndex)
        {
            var current = Current;
            if (current == null || choiceIndex < 0 || choiceIndex >= current.choices.Count) return;

            var choice = current.choices[choiceIndex];

            // Fire action sebelum navigasi
            if (!string.IsNullOrEmpty(choice.actionId))
                OnActionId?.Invoke(choice.actionId);

            if (choice.actionId == "end_dialogue")
            {
                EndDialogue();
                return;
            }

            if (choice.nextNodeIndex >= 0)
                _currentIndex = choice.nextNodeIndex;
            else
                Advance();
        }

        private void EndDialogue()
        {
            _currentIndex = -1;
            OnDialogueEnd?.Invoke();
        }
    }
}
