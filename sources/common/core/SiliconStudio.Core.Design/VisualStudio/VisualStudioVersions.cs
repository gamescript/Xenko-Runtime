// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.VisualStudio.Setup.Configuration;

namespace SiliconStudio.Core.VisualStudio
{
    public class IDEInfo
    {
        public override string ToString() => DisplayName;
        public string DisplayName { get; internal set; }
        public string DevenvPath { get; internal set; }
        public string InstallationPath { get; internal set; }

        public VSIXInstallerVersion VsixInstallerVersion { get; internal set; }
        public string VsixInstallerPath { get; internal set; }

        public bool Complete { get; internal set; } = true;

        public Dictionary<string, string> PackageVersions { get; internal set; } = new Dictionary<string, string>();
    }

    public enum VSIXInstallerVersion
    {
        None,
        VS2015,
        VS2017AndFutureVersions,
    }

    public static class VisualStudioVersions
    {
        private const int REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);
        private static List<IDEInfo> ideInfos;

        public static IDEInfo DefaultIDE = new IDEInfo { DisplayName = "Default IDE", DevenvPath = null };

        private static void BuildIDEInfos()
        {
            if (ideInfos != null)
                return;

            ideInfos = new List<IDEInfo>();

            ideInfos.Add(DefaultIDE);

            // Visual Studio 14.0 (2015)
            var localMachine32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            using (var subkey = localMachine32.OpenSubKey(string.Format(@"SOFTWARE\Microsoft\{0}\{1}", "VisualStudio", "14.0")))
            {
                var path = (string)subkey?.GetValue("InstallDir");

                var vs14InstallPath = (path != null) ? Path.Combine(path, "devenv.exe") : null;
                if (vs14InstallPath != null && File.Exists(vs14InstallPath))
                {
                    var vsixInstallerPath = Path.Combine(path, "VSIXInstaller.exe");
                    if (!File.Exists(vsixInstallerPath))
                        vsixInstallerPath = null;

                    ideInfos.Add(new IDEInfo { DisplayName = "Visual Studio 2015", DevenvPath = vs14InstallPath, VsixInstallerVersion = VSIXInstallerVersion.VS2015, VsixInstallerPath = vsixInstallerPath });
                }
            }

            // Visual Studio 15.0 (2017) and later
            try
            {
                var configuration = new SetupConfiguration();

                var instances = configuration.EnumAllInstances();
                instances.Reset();
                var inst = new ISetupInstance[1];
                int pceltFetched;

                while (true)
                {
                    instances.Next(1, inst, out pceltFetched);
                    if (pceltFetched <= 0)
                        break;

                    try
                    {
                        var inst2 = inst[0] as ISetupInstance2;
                        if (inst2 == null)
                            continue;

                        var idePath = Path.Combine(inst2.GetInstallationPath(), "Common7\\IDE");
                        var path = Path.Combine(idePath, "devenv.exe");
                        if (File.Exists(path))
                        {
                            var vsixInstallerPath = Path.Combine(idePath, "VSIXInstaller.exe");
                            if (!File.Exists(vsixInstallerPath))
                                vsixInstallerPath = null;

                            var displayName = inst2.GetDisplayName();

                            // Try to append nickname (if any)
                            try
                            {
                                var nickname = inst2.GetProperties().GetValue("nickname") as string;
                                if (!string.IsNullOrEmpty(nickname))
                                    displayName = $"{displayName} ({nickname})";
                            }
                            catch (COMException)
                            {
                            }

                            var ideInfo = new IDEInfo { DisplayName = displayName, Complete = inst2.IsComplete(), InstallationPath = inst2.GetInstallationPath(), DevenvPath = path, VsixInstallerVersion = VSIXInstallerVersion.VS2017AndFutureVersions, VsixInstallerPath = vsixInstallerPath };

                            // Fill packages
                            foreach (var package in inst2.GetPackages())
                            {
                                ideInfo.PackageVersions[package.GetId()] = package.GetVersion();
                            }

                            ideInfos.Add(ideInfo);
                        }
                    }
                    catch (Exception)
                    {
                        // Something might have happened inside Visual Studio Setup code (had FileNotFoundException in GetInstallationPath() for example)
                        // Let's ignore this instance
                    }
                } 
            }
            catch (COMException comException) when (comException.HResult == REGDB_E_CLASSNOTREG)
            {
                // COM is not registered. Assuming no instances are installed.
            }
        }

        public static IEnumerable<IDEInfo> AvailableVisualStudioVersions
        {
            get
            {
                BuildIDEInfos();

                return ideInfos;
            }
        }
    }
}
