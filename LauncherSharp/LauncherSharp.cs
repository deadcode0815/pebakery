﻿/*
    Copyright (C) 2016-2018 Hajin Jang
    Licensed under MIT License.

    MIT License

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PEBakeryLauncher
{
    public class Launcher
    {
        public static void Main(string[] args)
        {
            // Alert user to update .Net Framework to 4.7.1 if not installed.
            // The launcher itself runs in .Net Framework 4 Client Profile.
            // TODO: Write in C, for maximum compatibility.
            if (!CheckNetFrameworkVersion())
            {
                MessageBox.Show("PEBakery requires .Net Framework 4.7.1 or newer.", "Install .Net Framework 4.7.1", MessageBoxButton.OK, MessageBoxImage.Error);
                Process.Start("https://www.microsoft.com/en-us/download/details.aspx?id=56116");
                Environment.Exit(1);
            }

            // Launch PEBakery.exe using ShellExecute
            string absPath = GetProgramAbsolutePath();
            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    Verb = "Open",
                    FileName = Path.Combine(absPath, "Binary", "PEBakery.exe"),
                    WorkingDirectory = absPath
                }
            };

            StringBuilder b = new StringBuilder();
            foreach (string arg in args)
            {
                b.Append("\"");
                b.Append(arg);
                b.Append("\" ");
            }
            proc.StartInfo.Arguments = b.ToString();
            proc.Start();
        }

        public static string GetProgramAbsolutePath()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            if (Path.GetDirectoryName(path) != null)
                path = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return path;
        }

        private static bool CheckNetFrameworkVersion()
        { // https://docs.microsoft.com/ko-kr/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed#net_b
            const string ndpPath = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full";
            using (RegistryKey ndpKey = Registry.LocalMachine.OpenSubKey(ndpPath, false))
            {
                if (ndpKey == null)
                    return false;

                int revision = (int)ndpKey.GetValue("Release", 0);

                // PEBakery requires .Net Framework 4.7.1 or later
                return 461308 <= revision;
            }
        }
    }
}
