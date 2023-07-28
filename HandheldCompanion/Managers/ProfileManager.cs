using HandheldCompanion.Controls;
using HandheldCompanion.Misc;
using HandheldCompanion.Utils;
using HandheldCompanion.Views;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HandheldCompanion.Managers
{
    public static class ProfileManager
    {
        private static readonly JsonSerializerSettings jsonSettings = new() { TypeNameHandling = TypeNameHandling.Auto };

        public const string DefaultName = "Default";

        public static Dictionary<string, Profile> profiles = new(StringComparer.InvariantCultureIgnoreCase);
        public static FileSystemWatcher profileWatcher { get; set; }

        public static event InitializedEventHandler Initialized;
        public delegate void InitializedEventHandler();
        public static event AppliedEventHandler Applied;
        public delegate void AppliedEventHandler(Profile profile, ProfileUpdateSource source);
        public static event UpdatedEventHandler Updated;
        public delegate void UpdatedEventHandler(Profile profile, ProfileUpdateSource source);
        public static event DeletedEventHandler Deleted;
        public delegate void DeletedEventHandler(Profile profile, ProfileUpdateSource source);

        private static Profile currentProfile;

        public static string ProfilesPath;
        private static bool IsInitialized;

        static ProfileManager()
        {
            // initialiaze path
            ProfilesPath = Path.Combine(MainWindow.SettingsPath, "profiles");
            if (!Directory.Exists(ProfilesPath))
                Directory.CreateDirectory(ProfilesPath);

            // This is the only event ProfileManager needs to work.
            ProcessManager.ForegroundChanged += ProcessManager_ForegroundChanged;
        }

        public static void Start()
        {
            // monitor profile files
            profileWatcher = new FileSystemWatcher()
            {
                Path = ProfilesPath,
                EnableRaisingEvents = true,
                IncludeSubdirectories = true,
                Filter = "*.json",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size
            };
            profileWatcher.Deleted += ProfileWatcher_Deleted;

            // process existing profiles
            string[] fileEntries = Directory.GetFiles(ProfilesPath, "*.json", SearchOption.AllDirectories);
            foreach (string fileName in fileEntries)
                ProcessProfile(fileName);

            // check for default profile
            if (GetDefault() is null)
            {
                Profile defaultProfile = new()
                {
                    Name = DefaultName,
                    Default = true,
                    Enabled = true,
                    Layout = LayoutTemplate.DefaultLayout.Layout.Clone() as Layout,
                    LayoutTitle = LayoutTemplate.DefaultLayout.Name,
                };

                UpdateOrCreateProfile(defaultProfile, ProfileUpdateSource.Creation);
            }
            else
            {
                ApplyProfile(GetDefault(), ProfileUpdateSource.Background);
            }

            IsInitialized = true;
            Initialized?.Invoke();

            LogManager.LogInformation("{0} has started", "ProfileManager");
        }

        public static void Stop()
        {
            if (!IsInitialized)
                return;

            IsInitialized = false;

            profileWatcher.Deleted -= ProfileWatcher_Deleted;
            profileWatcher.Dispose();

            LogManager.LogInformation("{0} has stopped", "ProfileManager");
        }

        public static Profile GetDefault()
        {
            return profiles.Values.Where(a => a.Default).FirstOrDefault();
        }

        public static Profile GetCurrent()
        {
            return currentProfile;
        }

        public static bool IsCurrent(Profile profile)
        {
            return currentProfile is not null && profile.Path.Equals(currentProfile.Path, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool Contains(string path)
        {
            Profile profile = GetProfileFromPath(path);
            return profile is not null;
        }

        public static Profile GetProfileFromPath(string path)
        {
            return profiles.Values.Where(a => a.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
        }

        private static void ApplyProfile(Profile profile, ProfileUpdateSource source)
        {
            currentProfile = profile;
            Applied?.Invoke(profile, source);

            LogManager.LogInformation("Profile {0} applied", profile.Name);
            ToastManager.SendToast($"Profile {profile.Name} applied");
        }

        private static void RefreshCurrentProfile(ProcessEx foregroundProcess, ProfileUpdateSource source)
        {
            Profile profile = null;
            if (foregroundProcess is not null)
                profile = GetProfileFromPath(foregroundProcess.Path);
            if (profile is null)
                profile = GetDefault();

            if (profile == currentProfile)
                return;

            ApplyProfile(profile, source);
        }

        public static void UpdateOrCreateProfile(Profile profile, ProfileUpdateSource source)
        {
            bool isCurrent = IsCurrent(profile);

            // refresh error code
            SanitizeProfile(profile);

            profiles[profile.Path] = profile;
            Updated?.Invoke(profile, source);

            if (source == ProfileUpdateSource.Deserializer)
                return;

            // serialize profile
            SerializeProfile(profile);

            // re-apply current profile
            if (isCurrent)
                ApplyProfile(profile, source);
            // if it's a new profile, update current, maybe the new one should be the current one
            else if (source == ProfileUpdateSource.Creation)
                RefreshCurrentProfile(ProcessManager.GetForegroundProcess(), source);
        }

        public static void DeleteProfile(Profile profile, ProfileUpdateSource source)
        {
            // just for testing, shouldn't happen
            if (profile.Default)
                return;

            string profilePath = Path.Combine(ProfilesPath, profile.GetFileName());

            if (profiles.ContainsKey(profile.Path))
            {
                bool isCurrent = IsCurrent(profile);

                profiles.Remove(profile.Path);
                Deleted?.Invoke(profile, source);

                LogManager.LogInformation("Profile {0} deleted", profile.Name);
                ToastManager.SendToast($"Profile {profile.Name} deleted");

                // choose another profile as current
                if (isCurrent)
                    RefreshCurrentProfile(ProcessManager.GetForegroundProcess(), source);
            }

            File.Delete(profilePath);
        }

        private static void ProcessManager_ForegroundChanged(ProcessEx process)
        {
            RefreshCurrentProfile(process, ProfileUpdateSource.Background);
        }

        private static void ProfileWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            // not ideal
            string ProfileName = e.Name.Replace(".json", "");
            Profile profile = profiles.Values.Where(p => p.Name.Equals(ProfileName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

            // couldn't find a matching profile
            if (profile is null)
                return;

            // you can't delete default profile !
            if (profile.Default)
            {
                SerializeProfile(profile);
                return;
            }

            DeleteProfile(profile, ProfileUpdateSource.Background);
        }

        private static void ProcessProfile(string fileName)
        {
            Profile profile = null;
            try
            {
                string outputraw = File.ReadAllText(fileName);
                profile = JsonConvert.DeserializeObject<Profile>(outputraw, jsonSettings);
            }
            catch (Exception ex)
            {
                LogManager.LogError("Could not parse profile {0}. {1}", fileName, ex.Message);
            }

            // failed to parse
            if (profile is null || profile.Name is null || profile.Path is null)
            {
                LogManager.LogError("Could not parse profile {0}", fileName);
                return;
            }

            UpdateOrCreateProfile(profile, ProfileUpdateSource.Deserializer);
        }

        public static void SerializeProfile(Profile profile)
        {
            // update profile version to current build
            profile.Version = new(MainWindow.fileVersionInfo.ProductVersion);

            string jsonString = JsonConvert.SerializeObject(profile, Formatting.Indented, jsonSettings);

            string profilePath = Path.Combine(ProfilesPath, profile.GetFileName());
            File.WriteAllText(profilePath, jsonString);
        }

        private static void SanitizeProfile(Profile profile)
        {
            string processpath = Path.GetDirectoryName(profile.Path);
            profile.ErrorCode = ProfileErrorCode.None;

            if (profile.Default)
                profile.ErrorCode = ProfileErrorCode.Default;
            else if (!Directory.Exists(processpath))
                profile.ErrorCode = ProfileErrorCode.MissingPath;
            else if (!File.Exists(profile.Path))
                profile.ErrorCode = ProfileErrorCode.MissingExecutable;
            else if (!CommonUtils.IsDirectoryWritable(processpath))
                profile.ErrorCode = ProfileErrorCode.MissingPermission;
        }
    }
}
