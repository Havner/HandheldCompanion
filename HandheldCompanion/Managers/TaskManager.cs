using HandheldCompanion.Views;
using Microsoft.Win32.TaskScheduler;
using System;

namespace HandheldCompanion.Managers
{
    public static class TaskManager
    {
        // TaskManager vars
        private static Task task;
        private static TaskDefinition taskDefinition;
        private static TaskService TaskServ;

        private static string ServiceName, ServiceExecutable;

        private static bool IsInitialized;

        public static event InitializedEventHandler Initialized;
        public delegate void InitializedEventHandler();

        static TaskManager()
        {
            ServiceName = "HandheldCompanion";
            ServiceExecutable = MainWindow.CurrentExe;

            SettingsManager.SettingValueChanged += SettingsManager_SettingValueChanged;
        }

        private static void SettingsManager_SettingValueChanged(string name, object value)
        {
            switch (name)
            {
                case "RunAtStartup":
                    UpdateTask(Convert.ToBoolean(value));
                    break;
            }
        }

        public static void Start()
        {
            TaskServ = new TaskService();
            task = TaskServ.FindTask(ServiceName);

            try
            {
                if (task is not null)
                {
                    task.Definition.Actions.Clear();
                    task.Definition.Actions.Add(new ExecAction(ServiceExecutable));
                    task = TaskService.Instance.RootFolder.RegisterTaskDefinition(ServiceName, task.Definition);
                }
                else
                {
                    taskDefinition = TaskService.Instance.NewTask();
                    taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;
                    taskDefinition.Principal.LogonType = TaskLogonType.InteractiveToken;
                    taskDefinition.Settings.DisallowStartIfOnBatteries = false;
                    taskDefinition.Settings.StopIfGoingOnBatteries = false;
                    taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.Zero;
                    taskDefinition.Settings.Enabled = false;
                    taskDefinition.Triggers.Add(new LogonTrigger());
                    taskDefinition.Actions.Add(new ExecAction(ServiceExecutable));
                    task = TaskService.Instance.RootFolder.RegisterTaskDefinition(ServiceName, taskDefinition);
                }
            }
            catch { }

            IsInitialized = true;
            Initialized?.Invoke();

            LogManager.LogInformation("{0} has started", "TaskManager");
        }

        public static void Stop()
        {
            if (!IsInitialized)
                return;

            IsInitialized = false;

            LogManager.LogInformation("{0} has stopped", "TaskManager");
        }

        public static void UpdateTask(bool value)
        {
            if (task is null)
                return;

            try
            {
                task.Enabled = value;
            }
            catch { }
        }
    }
}
