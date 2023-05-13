using System;
using System.Collections.Generic;

namespace HandheldCompanion.Managers
{
    public class InputsHotkey
    {
        [Flags]
        public enum InputsHotkeyType : ushort
        {
            Mute = 0,
            HC = 1,
            Windows = 2,
            Device = 3,
            Custom = 4,
            Embedded = 5,
        }

        public static SortedDictionary<ushort, InputsHotkey> InputsHotkeys = new()
        {
            // Handheld Companion hotkeys
            { 10, new InputsHotkey(InputsHotkeyType.HC,       "\uE7FC", "overlayGamepad",                false, true,  string.Empty,           false, true ) },
            { 11, new InputsHotkey(InputsHotkeyType.HC,       "\uEDA4", "overlayTrackpads",              false, true,  string.Empty,           false, false) },
            { 12, new InputsHotkey(InputsHotkeyType.HC,       "\uE7C4", "shortcutMainWindow",            false, true,  string.Empty,           false, true ) },
            { 13, new InputsHotkey(InputsHotkeyType.HC,       "\uEC7A", "shortcutQuickTools",            false, true,  string.Empty,           false, false) },
            { 14, new InputsHotkey(InputsHotkeyType.HC,       "\uE961", "DesktopLayoutEnabled",          false, true,  string.Empty,           true,  true ) },

            // Microsoft Windows hotkeys
            { 20, new InputsHotkey(InputsHotkeyType.Windows,  "\uE765", "shortcutKeyboard",              false, true,  string.Empty,           false, true ) },
            { 21, new InputsHotkey(InputsHotkeyType.Windows,  "\uE7FB", "shortcutDesktop",               false, true,  string.Empty,           false, true ) },
            { 22, new InputsHotkey(InputsHotkeyType.Windows,  "\uEA4F", "shortcutESC",                   false, true,  string.Empty,           false, false) },
            { 23, new InputsHotkey(InputsHotkeyType.Windows,  "\uEE49", "shortcutExpand",                false, true,  string.Empty,           false, false) },
            { 24, new InputsHotkey(InputsHotkeyType.Windows,  "\uEB91", "shortcutTaskView",              false, true,  string.Empty,           false, false) },
            { 25, new InputsHotkey(InputsHotkeyType.Windows,  "\uE71D", "shortcutTaskManager",           false, true,  string.Empty,           false, false) },
            { 26, new InputsHotkey(InputsHotkeyType.Windows,  "\uE8A0", "shortcutActionCenter",          false, true,  string.Empty,           false, true ) },
            { 27, new InputsHotkey(InputsHotkeyType.Windows,  "\uEBE8", "shortcutKillApp",               false, true,  string.Empty,           false, false) },

            // Device specific hotkeys
            { 30, new InputsHotkey(InputsHotkeyType.Device,   "\uE706", "increaseBrightness",            true,  false, "HasBrightnessSupport", false, false) },
            { 31, new InputsHotkey(InputsHotkeyType.Device,   "\uEC8A", "decreaseBrightness",            true,  false, "HasBrightnessSupport", false, false) },
            { 32, new InputsHotkey(InputsHotkeyType.Device,   "\uE995", "increaseVolume",                true,  false, "HasVolumeSupport",     false, false) },
            { 33, new InputsHotkey(InputsHotkeyType.Device,   "\uE993", "decreaseVolume",                true,  false, "HasVolumeSupport",     false, false) },
            { 34, new InputsHotkey(InputsHotkeyType.Device,   "\uEC4A", "increaseTDP",                   true,  false, "HasTDPSupport",        false, false) },
            { 35, new InputsHotkey(InputsHotkeyType.Device,   "\uEC48", "decreaseTDP",                   true,  false, "HasTDPSupport",        false, false) },
            { 36, new InputsHotkey(InputsHotkeyType.Device,   "\uE9CA", "FanControlEnabled",             false, true,  "HasFanControlSupport", true,  false) },

            // User customizable hotkeys
            { 40, new InputsHotkey(InputsHotkeyType.Custom,   "\uF146", "shortcutCustom0",               false, true,  string.Empty,           false, false) },
            { 41, new InputsHotkey(InputsHotkeyType.Custom,   "\uF147", "shortcutCustom1",               false, true,  string.Empty,           false, false) },
            { 42, new InputsHotkey(InputsHotkeyType.Custom,   "\uF148", "shortcutCustom2",               false, true,  string.Empty,           false, false) },
            { 43, new InputsHotkey(InputsHotkeyType.Custom,   "\uF149", "shortcutCustom3",               false, true,  string.Empty,           false, false) },
            { 44, new InputsHotkey(InputsHotkeyType.Custom,   "\uF14A", "shortcutCustom4",               false, true,  string.Empty,           false, false) },
            { 45, new InputsHotkey(InputsHotkeyType.Custom,   "\uF14B", "shortcutCustom5",               false, true,  string.Empty,           false, false) },
            { 46, new InputsHotkey(InputsHotkeyType.Custom,   "\uF14C", "shortcutCustom6",               false, true,  string.Empty,           false, false) },
            { 47, new InputsHotkey(InputsHotkeyType.Custom,   "\uF14D", "shortcutCustom7",               false, true,  string.Empty,           false, false) },
            { 48, new InputsHotkey(InputsHotkeyType.Custom,   "\uF14E", "shortcutCustom8",               false, true,  string.Empty,           false, false) },
            { 49, new InputsHotkey(InputsHotkeyType.Custom,   "\uF14F", "shortcutCustom9",               false, true,  string.Empty,           false, false) },

            // Special, UI hotkeys
            { 60, new InputsHotkey(InputsHotkeyType.Embedded, "\uEDE3", "shortcutProfilesPage@",         true,  true,  string.Empty,           false, false) },
            { 61, new InputsHotkey(InputsHotkeyType.Embedded, "\uEDE3", "shortcutProfilesPage@@",        true,  true,  string.Empty,           false, false) },
            { 62, new InputsHotkey(InputsHotkeyType.Embedded, "\uEDE3", "shortcutProfilesSettingsMode0", true,  true,  string.Empty,           false, false) },
        };

        public string Glyph { get; set; }
        public string Listener { get; set; }
        public string Description { get; set; }
        public InputsHotkeyType hotkeyType { get; set; }
        public bool OnKeyDown { get; set; }
        public bool OnKeyUp { get; set; }
        public string Settings { get; set; }
        public bool DefaultPinned { get; set; }
        public bool IsToggle { get; set; }

        public InputsHotkey(InputsHotkeyType hotkeyType, string glyph, string listener, bool onKeyDown, bool onKeyUp, string settings, bool isToggle, bool defaultPinned)
        {
            this.hotkeyType = hotkeyType;
            this.Glyph = glyph;
            this.Listener = listener;
            this.OnKeyDown = onKeyDown;
            this.OnKeyUp = onKeyUp;

            this.Settings = settings;
            this.DefaultPinned = defaultPinned;
            this.IsToggle = isToggle;
        }

        public InputsHotkey()
        {
        }

        public string GetName()
        {
            // return localized string if available
            string listener = Listener;

            switch (hotkeyType)
            {
                case InputsHotkeyType.Custom:
                    listener = "shortcutCustom";
                    break;
            }

            string root = Properties.Resources.ResourceManager.GetString($"InputsHotkey_{listener}");

            if (!string.IsNullOrEmpty(root))
                return root;

            return Listener;
        }

        public string GetDescription()
        {
            // return localized string if available
            string listener = Listener;

            switch (hotkeyType)
            {
                case InputsHotkeyType.Custom:
                    listener = "shortcutCustom";
                    break;
            }

            string root = Properties.Resources.ResourceManager.GetString($"InputsHotkey_{listener}Desc");

            if (!string.IsNullOrEmpty(root))
                return root;

            return string.Empty;
        }
    }
}
