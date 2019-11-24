using System.Diagnostics;
using UnityEngine;

namespace SEE
{

    public static class BatchFileUtil
    {
        public static readonly string WORKING_DIRECTORY = Application.dataPath.Replace('/', '\\') + '\\';
        public static readonly int EXIT_SUCCESS = 0;

        public static int Run(string path, string args = "")
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.Arguments = args;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.FileName = WORKING_DIRECTORY + path;
            processStartInfo.UseShellExecute = false;
            processStartInfo.WorkingDirectory = WORKING_DIRECTORY;

            Process process = Process.Start(processStartInfo);
            process.WaitForExit();

            int exitCode = process.ExitCode;
            process.Close();
            return exitCode;
        }

    }

}
