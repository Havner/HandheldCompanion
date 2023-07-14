using ControllerCommon.Inputs;
using ControllerCommon.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ControllerCommon
{
    [Flags]
    public enum ProfileErrorCode
    {
        None = 0,
        MissingExecutable = 1,
        MissingPath = 2,
        MissingPermission = 3,
        Default = 4,
    }

    [Flags]
    public enum ProfileUpdateSource
    {
        Background = 0,
        ProfilesPage = 1,
        QuickProfilesPage = 2,
        Creation = 3,
        Deserializer = 4,
    }

    [Serializable]
    public class Profile : ICloneable, IComparable
    {
        // todo: move me out of here !
        public static readonly SortedDictionary<MotionInput, string> InputDescription = new()
        {
            { MotionInput.JoystickCamera, Properties.Resources.JoystickCameraDesc },
            { MotionInput.JoystickSteering, Properties.Resources.JoystickSteeringDesc },
            { MotionInput.PlayerSpace, Properties.Resources.PlayerSpaceDesc },
            { MotionInput.AutoRollYawSwap, Properties.Resources.AutoRollYawSwapDesc }
        };

        [JsonIgnore]
        public const int SensivityArraySize = 49;             // x + 1 (hidden)

        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;

        public Guid Guid { get; set; } = Guid.NewGuid();
        public string Executable { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public bool Default { get; set; }
        public Version Version { get; set; } = new();

        public string LayoutTitle { get; set; } = string.Empty;
        public Layout Layout { get; set; } = new();

        public int SteeringAxis { get; set; } = 0;                  // 0 = Roll, 1 = Yaw

        public MotionInput MotionInput { get; set; } = MotionInput.JoystickCamera;
        public MotionMode MotionMode { get; set; } = MotionMode.Off;
        public bool MotionInvertHorizontal { get; set; }
        public bool MotionInvertVertical { get; set; }
        public float MotionSensivityX { get; set; } = 1.0f;
        public float MotionSensivityY { get; set; } = 1.0f;
        public bool MotionSensivityArrayEnabled = false;
        public SortedDictionary<double, double> MotionSensivityArray { get; set; } = new();

        public ButtonState MotionTrigger { get; set; } = new();

        // steering
        public float SteeringMaxAngle { get; set; } = 30.0f;
        public float SteeringPower { get; set; } = 1.0f;
        public float SteeringDeadzone { get; set; } = 0.0f;

        // Aiming down sights
        public float AimingSightsMultiplier { get; set; } = 1.0f;
        public ButtonState AimingSightsTrigger { get; set; } = new();

        public ProfileErrorCode ErrorCode = ProfileErrorCode.None;

        public Profile()
        {
            // initialize aiming array
            if (MotionSensivityArray.Count == 0)
            {
                for (int i = 0; i < SensivityArraySize; i++)
                {
                    double value = (double)i / (double)(SensivityArraySize - 1);
                    MotionSensivityArray[value] = 0.5f;
                }
            }
        }

        public Profile(string path) : this()
        {
            Dictionary<string, string> AppProperties = ProcessUtils.GetAppProperties(path);

            string ProductName = AppProperties.ContainsKey("FileDescription") ? AppProperties["FileDescription"] : AppProperties["ItemFolderNameDisplay"];
            // string Version = AppProperties.ContainsKey("FileVersion") ? AppProperties["FileVersion"] : "1.0.0.0";
            // string Company = AppProperties.ContainsKey("Company") ? AppProperties["Company"] : AppProperties.ContainsKey("Copyright") ? AppProperties["Copyright"] : "Unknown";

            this.Executable = AppProperties["FileName"];
            this.Name = ProductName;
            this.Path = path;

            // enable the below variables when profile is created
            this.Enabled = true;
        }

        public float GetSensitivityX()
        {
            return MotionSensivityX * 1000.0f;
        }

        public float GetSensitivityY()
        {
            return MotionSensivityY * 1000.0f;
        }

        public string GetFileName()
        {
            string name = Name;

            if (!Default)
                name = System.IO.Path.GetFileNameWithoutExtension(Executable);

            return $"{name}.json";
        }

        public override string ToString()
        {
            return Name;
        }

        public object Clone()
        {
            string jsonString = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
            return JsonConvert.DeserializeObject<Profile>(jsonString, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
        }

        public int CompareTo(object obj)
        {
            Profile profile = (Profile)obj;
            return profile.Name.CompareTo(Name);
        }
    }
}
