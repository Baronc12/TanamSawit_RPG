using UnityEngine;

namespace TanamSawit.Core
{
    /// <summary>
    /// Deteksi key press (edge up→down) yang andal terlepas dari timing
    /// update Input System. Menggunakan isPressed (continuous state) dan
    /// melacak transisi frame-ke-frame, menggantikan wasPressedThisFrame
    /// yang dapat miss akibat timing update pada konfigurasi tertentu.
    /// </summary>
    public static class InputEdgeDetection
    {
        private static int _frame = -1;
        private static bool _e, _space, _esc, _tab, _mouseLeft;
        private static bool _prevE, _prevSpace, _prevEsc, _prevTab, _prevMouseLeft;
        private static bool _eEdge, _spaceEdge, _escEdge, _tabEdge, _mouseLeftEdge;

        private static void Poll()
        {
            if (_frame == Time.frameCount) return;
            _frame = Time.frameCount;

            bool e = false, sp = false, esc = false, tab = false, ml = false;

#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                e = kb.eKey.isPressed;
                sp = kb.spaceKey.isPressed;
                esc = kb.escapeKey.isPressed;
                tab = kb.tabKey.isPressed;
            }
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null)
                ml = mouse.leftButton.isPressed;
#endif
            _eEdge = e && !_prevE;
            _spaceEdge = sp && !_prevSpace;
            _escEdge = esc && !_prevEsc;
            _tabEdge = tab && !_prevTab;
            _mouseLeftEdge = ml && !_prevMouseLeft;
            _prevE = e; _prevSpace = sp; _prevEsc = esc; _prevTab = tab; _prevMouseLeft = ml;
        }

        /// <summary>E atau Space baru saja ditekan (edge-triggered).</summary>
        public static bool EOrSpace()
        {
            Poll();
            if (_eEdge || _spaceEdge) return true;
#if !ENABLE_INPUT_SYSTEM
            try { return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space); }
            catch { return false; }
#else
            return false;
#endif
        }

        /// <summary>Space baru saja ditekan (edge-triggered).</summary>
        public static bool Space()
        {
            Poll();
            if (_spaceEdge) return true;
#if !ENABLE_INPUT_SYSTEM
            try { return Input.GetKeyDown(KeyCode.Space); }
            catch { return false; }
#else
            return false;
#endif
        }

        /// <summary>Escape baru saja ditekan (edge-triggered).</summary>
        public static bool Escape()
        {
            Poll();
            if (_escEdge) return true;
#if !ENABLE_INPUT_SYSTEM
            try { return Input.GetKeyDown(KeyCode.Escape); }
            catch { return false; }
#else
            return false;
#endif
        }

        /// <summary>Tab baru saja ditekan (edge-triggered).</summary>
        public static bool Tab()
        {
            Poll();
            if (_tabEdge) return true;
#if !ENABLE_INPUT_SYSTEM
            try { return Input.GetKeyDown(KeyCode.Tab); }
            catch { return false; }
#else
            return false;
#endif
        }

        /// <summary>Klik kiri mouse baru saja ditekan (edge-triggered).</summary>
        public static bool MouseLeft()
        {
            Poll();
            if (_mouseLeftEdge) return true;
#if !ENABLE_INPUT_SYSTEM
            try { return Input.GetMouseButtonDown(0); }
            catch { return false; }
#else
            return false;
#endif
        }

        /// <summary>E, Space, atau klik kiri mouse baru saja ditekan (untuk advance dialog).</summary>
        public static bool AdvanceDialogue()
        {
            Poll();
            if (_eEdge || _spaceEdge || _mouseLeftEdge) return true;
#if !ENABLE_INPUT_SYSTEM
            try { return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0); }
            catch { return false; }
#else
            return false;
#endif
        }
    }
}
