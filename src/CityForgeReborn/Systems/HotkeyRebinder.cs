using System.Collections.Generic;
using System.Linq;
using Game.Input;
using UnityEngine.InputSystem;

namespace CityForgeReborn.Systems
{
    /// <summary>
    /// Lets the panel set the hotkey: after <see cref="Start"/> the next key press
    /// (with any Ctrl/Shift/Alt held) becomes the new keyboard binding. Keys are read
    /// straight from the keyboard, because the game's UI only receives key presses
    /// while a text box has focus. The same binding shows in Options, so either place
    /// can change it.
    /// </summary>
    internal sealed class HotkeyRebinder
    {
        private static readonly HashSet<Key> kModifierKeys = new HashSet<Key>
        {
            Key.LeftShift, Key.RightShift, Key.LeftCtrl, Key.RightCtrl, Key.LeftAlt, Key.RightAlt,
            Key.LeftMeta, Key.RightMeta, Key.ContextMenu,
        };

        private readonly ProxyAction m_Action;

        public HotkeyRebinder(ProxyAction action)
        {
            m_Action = action;
            Refresh();
        }

        public bool Listening { get; private set; }

        /// <summary>The binding as people read it, e.g. "Ctrl+Shift+C"; empty when unbound.</summary>
        public string Label { get; private set; } = "";

        public void Start() => Listening = true;

        public void Cancel() => Listening = false;

        /// <summary>Call every frame. Returns true on the frame a new key was bound.</summary>
        public bool Update()
        {
            if (!Listening)
                return false;

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return false;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Listening = false;
                return false;
            }

            foreach (var key in keyboard.allKeys)
            {
                if (key == null || !key.wasPressedThisFrame || kModifierKeys.Contains(key.keyCode))
                    continue;

                Listening = false;
                return Bind(InputManager.GeneratePathForControl(key), keyboard.ctrlKey.isPressed, keyboard.shiftKey.isPressed, keyboard.altKey.isPressed);
            }

            return false;
        }

        /// <summary>Re-reads the label; the binding can also change in Options.</summary>
        public void Refresh()
        {
            if (!TryGetKeyboardBinding(out var binding) || !binding.isSet)
            {
                Label = "";
                return;
            }

            var parts = binding.modifiers.Select(m => Pretty(m.m_Path)).ToList();
            parts.Add(Pretty(binding.path));
            Label = string.Join("+", parts);
        }

        private bool Bind(string path, bool ctrl, bool shift, bool alt)
        {
            if (!TryGetKeyboardBinding(out var binding))
            {
                Mod.Log.Warn("No keyboard binding found for the panel hotkey.");
                return false;
            }

            var modifiers = new List<ProxyModifier>();
            void Add(bool held, string modifierPath)
            {
                if (held)
                    modifiers.Add(new ProxyModifier { m_Component = binding.component, m_Name = InputManager.GetModifierName(binding.component), m_Path = modifierPath });
            }

            Add(ctrl, InputManager.kCtrlName);
            Add(shift, InputManager.kShiftName);
            Add(alt, InputManager.kAltName);

            if (!InputManager.instance.SetBinding(binding.WithPath(path).WithModifiers(modifiers), out _))
            {
                Mod.Log.Warn($"Could not set the panel hotkey to {path}.");
                return false;
            }

            Refresh();
            Mod.Log.Info($"Panel hotkey set to {Label}.");
            return true;
        }

        private bool TryGetKeyboardBinding(out ProxyBinding binding)
        {
            foreach (var b in m_Action.bindings)
            {
                if (b.isKeyboard)
                {
                    binding = b;
                    return true;
                }
            }

            binding = default;
            return false;
        }

        /// <summary>"&lt;Keyboard&gt;/ctrl" → "Ctrl", "&lt;Keyboard&gt;/f5" → "F5", "&lt;Keyboard&gt;/c" → "C".</summary>
        private static string Pretty(string path)
        {
            var name = path.Substring(path.LastIndexOf('/') + 1);
            if (name.Length <= 1 || (name[0] == 'f' && name.Length <= 3 && char.IsDigit(name[1])))
                return name.ToUpperInvariant();
            return char.ToUpperInvariant(name[0]) + name.Substring(1);
        }
    }
}
