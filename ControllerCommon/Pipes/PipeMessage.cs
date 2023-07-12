using ControllerCommon.Controllers;
using ControllerCommon.Managers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace ControllerCommon.Pipes
{
    [Serializable]
    public abstract class PipeMessage
    {
        public PipeCode code;
    }

    #region serverpipe
    [Serializable]
    public partial class PipeServerToast : PipeMessage
    {
        public string title;
        public string content;
        public string image = "Toast";

        public PipeServerToast()
        {
            code = PipeCode.SERVER_TOAST;
        }
    }

    [Serializable]
    public partial class PipeServerPing : PipeMessage
    {
        public PipeServerPing()
        {
            code = PipeCode.SERVER_PING;
        }
    }

    [Serializable]
    public partial class PipeServerSettings : PipeMessage
    {
        public Dictionary<string, string> settings = new();

        public PipeServerSettings()
        {
            code = PipeCode.SERVER_SETTINGS;
        }

        public PipeServerSettings(string key, string value) : this()
        {
            this.settings.Add(key, value);
        }
    }
    #endregion

    #region clientpipe
    [Serializable]
    public partial class PipeClientProfile : PipeMessage
    {
        // public Profile profile;
        public string jsonString;

        public PipeClientProfile()
        {
            code = PipeCode.CLIENT_PROFILE;
        }

        public PipeClientProfile(Profile profile) : this()
        {
            this.jsonString = JsonConvert.SerializeObject(profile, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            });
        }

        public Profile GetProfile()
        {
            try
            {
                return JsonConvert.DeserializeObject<Profile>(jsonString, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All
                });
            }
            catch (Exception e)
            {
                LogManager.LogInformation("Failed to deserialize profile: {0}", e.Message);
                return new Profile();
            }
        }
    }

    [Serializable]
    public partial class PipeClientSettings : PipeMessage
    {
        public Dictionary<string, object> settings = new();

        public PipeClientSettings()
        {
            code = PipeCode.CLIENT_SETTINGS;
        }

        public PipeClientSettings(string key, object value) : this()
        {
            this.settings.Add(key, value);
        }
    }

    [Serializable]
    public enum CursorAction
    {
        CursorUp = 0,
        CursorDown = 1,
        CursorMove = 2
    }

    [Serializable]
    public enum CursorButton
    {
        None = 0,
        TouchLeft = 1,
        TouchRight = 2
    }

    [Serializable]
    public partial class PipeClientInputs : PipeMessage
    {
        public ControllerState Inputs;

        public PipeClientInputs()
        {
            code = PipeCode.CLIENT_INPUT;
        }

        public PipeClientInputs(ControllerState inputs) : this()
        {
            Inputs = inputs;
        }
    }

    [Serializable]
    public partial class PipeClientVibration : PipeMessage
    {
        public byte LargeMotor;
        public byte SmallMotor;

        public PipeClientVibration()
        {
            code = PipeCode.SERVER_VIBRATION;
        }
    }

    [Serializable]
    public enum SensorType
    {
        Girometer = 0,
        Accelerometer = 1,
        Inclinometer = 2,
        Quaternion = 3
    }
    #endregion
}
