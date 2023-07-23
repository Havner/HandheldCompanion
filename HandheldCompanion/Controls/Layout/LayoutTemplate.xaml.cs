using GregsStack.InputSimulatorStandard.Native;
using HandheldCompanion.Actions;
using HandheldCompanion.Inputs;
using HandheldCompanion.Misc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace HandheldCompanion.Controls
{
    /// <summary>
    /// Logique d'interaction pour LayoutTemplate.xaml
    /// </summary>
    ///
    [JsonObject(MemberSerialization.OptIn)]
    public partial class LayoutTemplate : UserControl, IComparable
    {
        [JsonProperty]
        public string Author
        {
            get { return _Author.Text; }
            set { _Author.Text = value; }
        }
        [JsonProperty]
        public string Name
        {
            get { return _Name.Text; }
            set { _Name.Text = value; }
        }
        [JsonProperty]
        public string Description
        {
            get { return _Description.Text; }
            set { _Description.Text = value; }
        }
        [JsonProperty]
        public string Product
        {
            get { return _Product.Text; }
            set
            {
                _Product.Text = value;
                _Product.Visibility = string.IsNullOrEmpty(value) ?
                    System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
            }
        }
        [JsonProperty]
        public Guid Guid { get; set; } = Guid.NewGuid();
        [JsonProperty]
        public string Executable { get; set; } = string.Empty;
        [JsonProperty]
        public bool IsInternal { get; set; } = false;
        [JsonProperty]
        public Layout Layout { get; set; } = new();
        [JsonProperty]
        public Type ControllerType { get; set; }

        #region events
        public event UpdatedEventHandler Updated;
        public delegate void UpdatedEventHandler(LayoutTemplate layoutTemplate);
        #endregion

        public LayoutTemplate()
        {
            InitializeComponent();
        }

        public LayoutTemplate(Layout layout) : this()
        {
            this.Layout = layout;
            this.Layout.Updated += Layout_Updated;
        }

        private LayoutTemplate(string name, string description, string author, bool isInternal, Type deviceType = null) : this()
        {
            this.Name = name;
            this.Description = description;
            this.Author = author;
            this.Product = string.Empty;

            this.IsInternal = isInternal;
            this.ControllerType = deviceType;

            this.Layout = new(true);

            switch (this.Name)
            {
                case "Desktop":
                    {
                        this.Layout.AxisLayout = new()
                        {
                            { AxisLayoutFlags.LeftStick, new MouseActions() { MouseType = MouseActionsType.Scroll, Sensivity = 25.0f } },
                            { AxisLayoutFlags.RightStick, new MouseActions() { MouseType = MouseActionsType.Move, Sensivity = 25.0f } },
                            { AxisLayoutFlags.LeftPad, new MouseActions() { MouseType = MouseActionsType.Scroll, Sensivity = 25.0f } },
                            { AxisLayoutFlags.RightPad, new MouseActions() { MouseType = MouseActionsType.Move, Sensivity = 25.0f } },
                        };

                        this.Layout.ButtonLayout = new()
                        {
                            { ButtonFlags.B1, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.RETURN } } },
                            { ButtonFlags.B2, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.ESCAPE } } },
                            { ButtonFlags.B3, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.NEXT } } },
                            { ButtonFlags.B4, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.PRIOR } } },

                            { ButtonFlags.L1, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.BACK } } },
                            { ButtonFlags.R1, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.SPACE } } },

                            { ButtonFlags.Back, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.MENU } } },
                            { ButtonFlags.Start, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.TAB } } },

                            { ButtonFlags.DPadUp, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.UP } } },
                            { ButtonFlags.DPadDown, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.DOWN } } },
                            { ButtonFlags.DPadLeft, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.LEFT } } },
                            { ButtonFlags.DPadRight, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.RIGHT } } },

                            { ButtonFlags.L2Soft, new List<IActions> { new MouseActions() { MouseType = MouseActionsType.RightButton } } },
                            { ButtonFlags.R2Soft, new List<IActions> { new MouseActions() { MouseType = MouseActionsType.LeftButton } } },

                            { ButtonFlags.LeftPadClick, new List<IActions> { new MouseActions() { MouseType = MouseActionsType.RightButton } } },
                            { ButtonFlags.RightPadClick, new List<IActions> { new MouseActions() { MouseType = MouseActionsType.LeftButton } } },
                        };
                    }
                    break;

                case "Keyboard (WASD) and Mouse":
                    {
                        this.Layout.AxisLayout = new()
                        {
                            { AxisLayoutFlags.RightStick, new MouseActions() { MouseType = MouseActionsType.Move, Sensivity = 25.0f } },
                            { AxisLayoutFlags.RightPad, new MouseActions() { MouseType = MouseActionsType.Move, Sensivity = 25.0f } },
                        };

                        this.Layout.ButtonLayout = new()
                        {
                            { ButtonFlags.B1, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.SPACE } } },
                            { ButtonFlags.B2, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.VK_E } } },
                            { ButtonFlags.B3, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.VK_R } } },
                            { ButtonFlags.B4, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.VK_F } } },

                            { ButtonFlags.L1, new List<IActions> { new MouseActions() { MouseType = MouseActionsType.ScrollUp, Sensivity = 25.0f } } },
                            { ButtonFlags.R1, new List<IActions> { new MouseActions() { MouseType = MouseActionsType.ScrollDown, Sensivity = 25.0f } } },

                            { ButtonFlags.Back, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.ESCAPE } } },
                            { ButtonFlags.Start, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.TAB } } },

                            { ButtonFlags.DPadUp, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.VK_1 } } },
                            { ButtonFlags.DPadDown, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.VK_3 } } },
                            { ButtonFlags.DPadLeft, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.VK_4 } } },
                            { ButtonFlags.DPadRight, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.VK_2 } } },

                            { ButtonFlags.L2Soft, new List<IActions> { new MouseActions() { MouseType = MouseActionsType.RightButton } } },
                            { ButtonFlags.R2Soft, new List<IActions> { new MouseActions() { MouseType = MouseActionsType.LeftButton } } },

                            { ButtonFlags.LeftStickUp, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.VK_W } } },
                            { ButtonFlags.LeftStickDown, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.VK_S } } },
                            { ButtonFlags.LeftStickLeft, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.VK_A } } },
                            { ButtonFlags.LeftStickRight, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.VK_D } } },

                            { ButtonFlags.LeftStickClick, new List<IActions> { new KeyboardActions() { Key = VirtualKeyCode.LSHIFT } } },
                            { ButtonFlags.RightStickClick, new List<IActions> { new MouseActions() { MouseType = MouseActionsType.LeftButton } } },

                            { ButtonFlags.RightPadClick, new List<IActions> { new MouseActions() { MouseType = MouseActionsType.LeftButton } } },
                        };
                    }
                    break;
            }
        }

        private void Layout_Updated(Layout layout)
        {
            Updated?.Invoke(this);
        }

        public int CompareTo(object obj)
        {
            LayoutTemplate profile = (LayoutTemplate)obj;
            return profile.Name.CompareTo(Name);
        }

        public static readonly LayoutTemplate DesktopLayout = new("Desktop", "Layout for Desktop Browsing", "HandheldCompanion", true);
        public static readonly LayoutTemplate DefaultLayout = new("Gamepad", "This template is for games that already have built-in gamepad support. Intended for dual stick games such as twin-stick shooters, side-scrollers, etc.", "HandheldCompanion", true);
        public static readonly LayoutTemplate KeyboardLayout = new("Keyboard (WASD) and Mouse", "This template works great for the games that were designed with a keyboard and mouse in mind, without gamepad support. The controller will drive the game's keyboard based events with buttons, but will make assumptions about which buttons move you around (WASD for movement, space for jump, etc.). The right pad will emulate the movement of a mouse.", "HandheldCompanion", true);
    }
}
