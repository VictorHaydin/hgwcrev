using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace hgwcrev
{
    class Program
    {
        private static string ms_HgPath;
        private static string ms_TemplatePath;
        private static string ms_OutputPath;
        private static int ms_Revision;

        static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Usage();
                return 1;
            }
            Console.WriteLine("Args:");
            foreach (var s in args)
            {
                Console.WriteLine(s);
            }

            ms_HgPath = args[0];
            ms_TemplatePath = args[1];
            ms_OutputPath = args[2];

            LoadVariables();
            
            ProcessTemplate();
            return 0;
        }

        private static void ProcessTemplate()
        {
            using (var template = File.OpenText(ms_TemplatePath))
            {
                using (var output = File.CreateText(ms_OutputPath))
                {
                    string templateStr;
                    while ((templateStr = template.ReadLine()) != null)
                    {
                        output.WriteLine(ReplaceVariables(templateStr));
                    }
                }
            }
        }

        static void LoadVariables()
        {
            var revisionStr = GetProcessOutputFirstLine(ms_HgPath, "id -n");
            if (revisionStr.EndsWith("+"))
            {
                revisionStr = revisionStr.Substring(0, revisionStr.Length - 1);
            }
            ms_Revision = int.Parse(revisionStr);
        }

        private static string GetProcessOutputFirstLine(string filename, string arguments)
        {
            //Log.LogWarning(filename);
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(filename, arguments)
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            const int processWaitTime = 1000;
            string result = null;

            // we should get output asynchronous to avoid freezing
            process.OutputDataReceived += (sendingProcess, outLine) =>
            {
                if (result == null)   // get first line only
                {
                    result = outLine.Data;
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit(processWaitTime);
            if (!process.HasExited)
            {
                process.Kill();
            }

            return result;
        }

        static string ReplaceVariables(string input)
        {
            return input.Replace("$HGREV$", ms_Revision.ToString());
        }

        static void Usage()
        {
            Console.WriteLine(@"
Usage:
hgwcrev.exe <hgpath> <template_file> <output_file>
For example:
hgwcrev.exe ""C:\Program Files\TortoiseHg\hg.exe"" AssemblyInfo.cs.template AssemblyInfo.cs
Variables:
$HGREV$ - numerical revision (hg id -n)
");
        }
    }
}
