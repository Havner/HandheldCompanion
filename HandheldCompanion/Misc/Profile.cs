using HandheldCompanion.Inputs;
using HandheldCompanion.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace HandheldCompanion.Misc
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

        public string Name = string.Empty;
        public string Path = string.Empty;

        public Guid Guid = Guid.NewGuid();
        public string Executable = string.Empty;
        public bool Enabled;
        public bool Default;
        public Version Version = new();
        public ProfileErrorCode ErrorCode = ProfileErrorCode.None;

        public string LayoutTitle = string.Empty;
        public Layout Layout = new();

        // generic gyro
        public int SteeringAxis = 0;                  // 0 = Roll, 1 = Yaw
        public bool MotionInvertHorizontal;
        public bool MotionInvertVertical;

        // mapped gyro
        public MotionInput MotionInput  = MotionInput.JoystickCamera;
        public MotionMode MotionMode = MotionMode.Off;
        public ButtonState MotionTrigger = new();

        // mode0
        public float MotionSensivityX = 1.0f;
        public float MotionSensivityY = 1.0f;
        public float AimingSightsMultiplier = 1.0f;
        public ButtonState AimingSightsTrigger = new();
        public bool MotionSensivityArrayEnabled = false;
        public SortedDictionary<double, double> MotionSensivityArray = new();

        // mode1
        public float SteeringMaxAngle = 30.0f;
        public float SteeringPower = 1.0f;
        public float SteeringDeadzone = 0.0f;

        public Profile()
        {
            // initialize aiming array
            if (MotionSensivityArray.Count == 0)
            {
                for (int i = 0; i < SensivityArraySize; i++)
                {
                    double value = i / (double)(SensivityArraySize - 1);
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

            Executable = AppProperties["FileName"];
            Name = ProductName;
            Path = path;

            // enable the below variables when profile is created
            Enabled = true;
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
