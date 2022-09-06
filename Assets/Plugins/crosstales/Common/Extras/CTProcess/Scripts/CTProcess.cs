﻿#if UNITY_STANDALONE || UNITY_EDITOR
using UnityEngine;

namespace Crosstales.Common.Util
{
   #region CTProcess

   /// <summary>Native process class for standalone IL2CPP-builds (mimicking the missing "System.Diagnostics.Process"-class with the most important properties, methods and events).</summary>
   public class CTProcess : System.IDisposable
   {
      #region Variables

      private uint exitCode = 123456;
      private CTProcessStartInfo startInfo = new CTProcessStartInfo();

      #endregion


      #region Properties

      /// <summary>Gets the native handle of the associated process.</summary>
      public System.IntPtr Handle { get; private set; }

      /// <summary>Gets the unique identifier for the associated process.</summary>
      public int Id { get; private set; }

      /// <summary>Gets or sets the properties to pass to the Start() method of the Process.</summary>
      public CTProcessStartInfo StartInfo
      {
         get => startInfo;

         set
         {
            if (value != null)
               startInfo = value;
         }
      }

      /// <summary>Gets a value indicating whether the associated process has been terminated.</summary>
      public bool HasExited { get; private set; }

      /// <summary>Gets the value that the associated process specified when it terminated.</summary>
      public uint ExitCode => exitCode;

      /// <summary>Gets the time that the associated process was started.</summary>
      public System.DateTime StartTime { get; private set; }

      /// <summary>Gets the time that the associated process exited.</summary>
      public System.DateTime ExitTime { get; private set; }

      /// <summary>Gets a stream used to read the textual output of the application.</summary>
      public System.IO.StreamReader StandardOutput { get; private set; }

      /// <summary>Gets a stream used to read the error output of the application.</summary>
      public System.IO.StreamReader StandardError { get; private set; }

      /// <summary>Gets a value indicating whether the associated process has been busy.</summary>
      public bool isBusy { get; private set; }

      #endregion


      #region Events

      public event System.EventHandler Exited;

//#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || ENABLE_IL2CPP
      public event System.Diagnostics.DataReceivedEventHandler OutputDataReceived;

      public event System.Diagnostics.DataReceivedEventHandler ErrorDataReceived;
//#endif

      #region Event-trigger methods

      private void onExited()
      {
         if (Crosstales.Common.Util.BaseConstants.DEV_DEBUG)
            Debug.Log($"onExited: {ExitCode}");

         Exited?.Invoke(this, System.EventArgs.Empty);
      }

      #endregion

      #endregion

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
//#if false
      #region Windows

      #region Variables

      private System.IntPtr threadHandle = System.IntPtr.Zero;

      private static readonly System.Reflection.FieldInfo[] eventFields =
         typeof(System.Diagnostics.DataReceivedEventArgs).GetFields(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.DeclaredOnly);

      private const uint Infinite = 0xffffffff;

      // Creation flags
      private const uint CREATE_NO_WINDOW = 0x08000000;

      #endregion


      #region Public methods

      /// <summary>Starts (or reuses) the process resource that is specified by the StartInfo property of this Process component and associates it with the component.</summary>
      public void Start()
      {
         cleanup();

         isBusy = true;
         HasExited = false;

         if (StartInfo.UseThread)
         {
            new System.Threading.Thread(createProcess).Start();

            System.Threading.Thread.Sleep(200);
         }
         else
         {
            createProcess();
         }
      }

      /// <summary>Starts the process resource that is specified by the parameter containing process start information (for example, the file name of the process to start) and associates the resource with a new Process component..</summary>
      public void Start(CTProcessStartInfo info)
      {
         if (info != null)
            StartInfo = info;

         Start();
      }

      /// <summary>Immediately stops the associated process.</summary>
      public void Kill()
      {
         if (Handle != System.IntPtr.Zero)
         {
            uint _exitCode = 99999; //killed
            NativeMethods.TerminateProcess(Handle, ref _exitCode);

            Dispose();
         }
      }

      public void WaitForExit(int milliseconds = 0)
      {
         if (milliseconds > 0)
         {
            NativeMethods.WaitForSingleObject(Handle, (uint)milliseconds);
         }
         else
         {
            NativeMethods.WaitForSingleObject(Handle, Infinite);
         }
      }

      public void BeginOutputReadLine()
      {
         //System.Threading.Thread.Sleep(100);
         new System.Threading.Thread(watchStdOut).Start();
      }

      public void BeginErrorReadLine()
      {
         //System.Threading.Thread.Sleep(100);
         new System.Threading.Thread(watchStdErr).Start();
      }

      public void Dispose()
      {
         if (Crosstales.Common.Util.BaseConstants.DEV_DEBUG)
            Debug.LogWarning("Dispose called!");

         if (Handle != System.IntPtr.Zero)
            NativeMethods.CloseHandle(Handle);

         if (threadHandle != System.IntPtr.Zero)
            NativeMethods.CloseHandle(threadHandle);

         Handle = System.IntPtr.Zero;
         threadHandle = System.IntPtr.Zero;
         Id = 0;

         isBusy = false;
         HasExited = true;

         StandardOutput?.Dispose();
         StandardError?.Dispose();
      }

      #endregion


      #region Private methods

      private void createProcess()
      {
         StartTime = System.DateTime.Now;

         string app = StartInfo.FileName;
         string args = StartInfo.Arguments;

         if (Crosstales.Common.Util.BaseConstants.DEV_DEBUG)
            Debug.Log($"createProcess: {StartTime}");

         //isBusy = true;
         //HasExited = false;

         NativeMethods.STARTUPINFOEX startupInfo = new NativeMethods.STARTUPINFOEX();

         try
         {
            if ((StartInfo.RedirectStandardOutput || StartInfo.RedirectStandardError || StartInfo.UseCmdExecute) &&
                !StartInfo.FileName.CTContains("cmd"))
            {
               app = Crosstales.Common.Util.BaseConstants.CMD_WINDOWS_PATH;
               args = $"/c call \"{StartInfo.FileName}\" {StartInfo.Arguments}";
            }

            if (StartInfo.RedirectStandardOutput)
            {
               string tempStdFile = System.IO.Path.GetTempFileName();
               args += $" > \"{tempStdFile}\"";

               if (Crosstales.Common.Util.BaseConstants.DEV_DEBUG)
                  Debug.Log($"tempStdFile: {tempStdFile}");

               StandardOutput = new System.IO.StreamReader(new System.IO.FileStream(tempStdFile, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite), StartInfo.StandardOutputEncoding);
            }
            else
            {
               StandardOutput =
                  new System.IO.StreamReader(new System.IO.MemoryStream(), StartInfo.StandardOutputEncoding);
            }

            if (StartInfo.RedirectStandardError)
            {
               string tempErrFile = System.IO.Path.GetTempFileName();
               args += $" 2> \"{tempErrFile}\"";

               if (Crosstales.Common.Util.BaseConstants.DEV_DEBUG)
                  Debug.Log($"tempErrFile: {tempErrFile}");

               StandardError = new System.IO.StreamReader(new System.IO.FileStream(tempErrFile, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite), StartInfo.StandardOutputEncoding);
            }
            else
            {
               StandardError = new System.IO.StreamReader(new System.IO.MemoryStream(), StartInfo.StandardOutputEncoding);
            }

            NativeMethods.SECURITY_ATTRIBUTES pSec = new NativeMethods.SECURITY_ATTRIBUTES();
            NativeMethods.SECURITY_ATTRIBUTES tSec = new NativeMethods.SECURITY_ATTRIBUTES();
            pSec.nLength = System.Runtime.InteropServices.Marshal.SizeOf(pSec);
            tSec.nLength = System.Runtime.InteropServices.Marshal.SizeOf(tSec);

            if (Crosstales.Common.Util.BaseConstants.DEV_DEBUG)
               Debug.Log($"application: {app}{System.Environment.NewLine}arguments: {args}");

            bool retValue =
               NativeMethods.CreateProcess(app, $" {args}", ref pSec, ref tSec, true,
                  StartInfo.CreateNoWindow ? CREATE_NO_WINDOW : 0x00000000, System.IntPtr.Zero,
                  StartInfo.WorkingDirectory, ref startupInfo, out NativeMethods.PROCESS_INFORMATION processInfo);

            if (retValue)
            {
               Handle = processInfo.hProcess;
               threadHandle = processInfo.hThread;
               Id = processInfo.dwProcessId;

               WaitForExit();
            }
            else
            {
               Debug.LogError($"Could not start process: '{StartInfo.FileName}'{System.Environment.NewLine}Arguments: '{StartInfo.Arguments}'{System.Environment.NewLine}Working dir: '{StartInfo.WorkingDirectory}'{System.Environment.NewLine}Last error: {NativeMethods.GetLastError()}");
            }
         }
         catch (System.Exception ex)
         {
            Debug.LogError($"Process threw an error: {ex}");

            Dispose();
         }
         finally
         {
            System.Threading.Thread.Sleep(200); //give the streams the chance to write out all events

            NativeMethods.GetExitCodeProcess(Handle, ref exitCode);

            ExitTime = System.DateTime.Now;

            if (Handle != System.IntPtr.Zero)
               NativeMethods.CloseHandle(Handle);

            if (threadHandle != System.IntPtr.Zero)
               NativeMethods.CloseHandle(threadHandle);

            Handle = System.IntPtr.Zero;
            threadHandle = System.IntPtr.Zero;
            Id = 0;

            if (!HasExited)
               onExited();

            isBusy = false;
            HasExited = true;
         }
      }

      private void cleanup()
      {
         Kill();
         Dispose();
      }

      private void watchStdOut()
      {
         using (System.IO.StreamReader streamReader = StandardOutput)
         {
            while (!streamReader.EndOfStream)
            {
               string reply = streamReader.ReadLine();

               if (Crosstales.Common.Util.BaseConstants.DEV_DEBUG)
                  Debug.Log($"watchStdOut: {reply}");

               OutputDataReceived?.Invoke(this, createMockDataReceivedEventArgs(reply));
            }
         }
      }

      private void watchStdErr()
      {
         using (System.IO.StreamReader streamReader = StandardError)
         {
            while (!streamReader.EndOfStream)
            {
               string reply = streamReader.ReadLine();

               if (Crosstales.Common.Util.BaseConstants.DEV_DEBUG)
                  Debug.Log($"watchStdErr: {reply}");

               ErrorDataReceived?.Invoke(this, createMockDataReceivedEventArgs(reply));
            }
         }
      }

      private static System.Diagnostics.DataReceivedEventArgs createMockDataReceivedEventArgs(string data)
      {
         if (string.IsNullOrEmpty(data))
            throw new System.ArgumentException("Data is null or empty.", nameof(data));

         System.Diagnostics.DataReceivedEventArgs mockEventArgs =
            (System.Diagnostics.DataReceivedEventArgs)System.Runtime.Serialization.FormatterServices
               .GetUninitializedObject(typeof(System.Diagnostics.DataReceivedEventArgs));

         if (eventFields.Length > 0)
         {
            eventFields[0].SetValue(mockEventArgs, data);
         }
         else
         {
            Debug.LogError("Could not create 'DataReceivedEventArgs'!");
         }

         return mockEventArgs;
      }

      #endregion

      #endregion

#else

      #region Unix

      #region Variables

      private System.Threading.Thread worker;

      #endregion


      #region Public methods

      /// <summary>Starts (or reuses) the process resource that is specified by the StartInfo property of this Process component and associates it with the component.</summary>
      public void Start()
      {
         isBusy = true;
         HasExited = false;

         if (StartInfo.UseThread)
         {
            worker = new System.Threading.Thread(() => createProcess());
            worker.Start();

            System.Threading.Thread.Sleep(200);
         }
         else
         {
            createProcess();
         }
      }

      /// <summary>Starts the process resource that is specified by the parameter containing process start information (for example, the file name of the process to start) and associates the resource with a new Process component..</summary>
      public void Start(CTProcessStartInfo info)
      {
         if (info != null)
            StartInfo = info;

         Start();
      }

      /// <summary>Immediately stops the associated process.</summary>
      public void Kill()
      {
         if (worker != null && worker.IsAlive)
         {
            worker.Abort();
         }

         Dispose();
      }

      public void WaitForExit(int milliseconds = 0)
      {
         //Debug.Log("Not implemented!");
      }

      public void BeginOutputReadLine()
      {
         //Debug.Log("Not implemented!");
      }

      public void BeginErrorReadLine()
      {
         //Debug.Log("Not implemented!");
      }

      public void Dispose()
      {
         if (BaseConstants.DEV_DEBUG)
            Debug.LogWarning("Dispose called!");

         Id = 0;
         isBusy = false;
         HasExited = true;

         if (StandardOutput != null)
            StandardOutput.Dispose();

         if (StandardError != null)
            StandardError.Dispose();
      }

      #endregion


      #region Private methods

      private void createProcess()
      {
         StartTime = System.DateTime.Now;

         string app = StartInfo.FileName;
         string args = StartInfo.Arguments;

         if (BaseConstants.DEV_DEBUG)
            Debug.LogWarning($"createProcess: {StartTime}");

         try
         {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
//#if true
            if (StartInfo.RedirectStandardOutput)
            {
               StandardOutput = new System.IO.StreamReader(new System.IO.MemoryStream(), StartInfo.StandardOutputEncoding);
            }

            if (StartInfo.RedirectStandardError)
            {
               string tempErrFile = System.IO.Path.GetTempFileName();
               args += $" 2> \"{tempErrFile}\"";

               if (BaseConstants.DEV_DEBUG)
                  Debug.Log($"tempErrFile: {tempErrFile}");

               StandardError = new System.IO.StreamReader(new System.IO.FileStream(tempErrFile, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite), StartInfo.StandardOutputEncoding);
            }
            else
            {
               StandardError = new System.IO.StreamReader(new System.IO.MemoryStream(), StartInfo.StandardOutputEncoding);
            }

            string result = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(NativeMethods.RunCommand($"{app} {args}"));

            if (StartInfo.RedirectStandardOutput && !string.IsNullOrEmpty(result))
            {
               byte[] byteArray = StartInfo.StandardOutputEncoding.GetBytes(result);
               System.IO.MemoryStream stream = new System.IO.MemoryStream(byteArray);
               StandardOutput = new System.IO.StreamReader(stream);
            }

            exitCode = 0;
#else
            if (StartInfo.RedirectStandardOutput)
            {
               string tempStdFile = System.IO.Path.GetTempFileName();
               args += $" > \"{tempStdFile}\"";

               if (BaseConstants.DEV_DEBUG)
                  Debug.Log($"tempStdFile: {tempStdFile}");

               StandardOutput = new System.IO.StreamReader(new System.IO.FileStream(tempStdFile, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite), StartInfo.StandardOutputEncoding);
            }
            else
            {
               StandardOutput = new System.IO.StreamReader(new System.IO.MemoryStream(), StartInfo.StandardOutputEncoding);
            }

            if (StartInfo.RedirectStandardError)
            {
               string tempErrFile = System.IO.Path.GetTempFileName();
               args += $" 2> \"{tempErrFile}\"";

               if (BaseConstants.DEV_DEBUG)
                  Debug.Log($"tempErrFile: {tempErrFile}");

               StandardError =
                  new System.IO.StreamReader(new System.IO.FileStream(tempErrFile, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite), StartInfo.StandardOutputEncoding);
            }
            else
            {
               StandardError = new System.IO.StreamReader(new System.IO.MemoryStream(), StartInfo.StandardOutputEncoding);
            }

            exitCode = (uint)NativeMethods.RunCommand($"{app} {args}");
#endif
         }
         catch (System.Threading.ThreadAbortException)
         {
            //Debug.LogWarning("Process killed!");
            exitCode = 123456;
         }
         catch (System.Exception ex)
         {
            Debug.LogError($"Process threw an error: {ex}");
            exitCode = 99;
         }
         finally
         {
            ExitTime = System.DateTime.Now;
            Id = 0;

            if (!HasExited)
               onExited();

            isBusy = false;
            HasExited = true;
         }
      }

      #endregion

      #endregion

#endif
   }

   #endregion

   #region Native methods

   /// <summary>Native methods (bridge to Windows).</summary>
   internal static class NativeMethods
   {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
//#if false

      #region Windows

      [System.Runtime.InteropServices.DllImport("Kernel32.dll", SetLastError = true,
         CharSet = System.Runtime.InteropServices.CharSet.Auto)]
      [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
      internal static extern bool CreateProcess(
         string lpApplicationName, string lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes,
         ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags,
         System.IntPtr lpEnvironment, string lpCurrentDirectory,
         [System.Runtime.InteropServices.In] ref STARTUPINFOEX lpStartupInfo,
         out PROCESS_INFORMATION lpProcessInformation);

      [System.Runtime.InteropServices.DllImport("Kernel32.dll", SetLastError = true)]
      [System.Runtime.ConstrainedExecution.ReliabilityContract(
         System.Runtime.ConstrainedExecution.Consistency.WillNotCorruptState,
         System.Runtime.ConstrainedExecution.Cer.MayFail)]
      internal static extern bool CloseHandle(System.IntPtr hObject);

      [System.Runtime.InteropServices.DllImport("Kernel32.dll", SetLastError = true)]
      internal static extern bool GetExitCodeProcess(System.IntPtr process, ref uint exitCode);

      [System.Runtime.InteropServices.DllImport("Kernel32.dll", SetLastError = true)]
      internal static extern uint WaitForSingleObject(System.IntPtr handle, uint milliseconds);

      [System.Runtime.InteropServices.DllImport("Kernel32.dll", SetLastError = true)]
      internal static extern bool TerminateProcess(System.IntPtr hProcess, ref uint exitCode);

      [System.Runtime.InteropServices.DllImport("Kernel32.dll")]
      internal static extern uint GetLastError();

      [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet =
         System.Runtime.InteropServices.CharSet.Unicode)]
      internal struct STARTUPINFOEX
      {
         public STARTUPINFO StartupInfo;
         public System.IntPtr lpAttributeList;
      }

      [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet =
         System.Runtime.InteropServices.CharSet.Unicode)]
      internal struct STARTUPINFO
      {
         public int cb;
         public string lpReserved;
         public string lpDesktop;
         public string lpTitle;
         public int dwX;
         public int dwY;
         public int dwXSize;
         public int dwYSize;
         public int dwXCountChars;
         public int dwYCountChars;
         public int dwFillAttribute;
         public int dwFlags;
         public short wShowWindow;
         public short cbReserved2;
         public System.IntPtr lpReserved2;
         public System.IntPtr hStdInput;
         public System.IntPtr hStdOutput;
         public System.IntPtr hStdError;
      }

      [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
      internal struct PROCESS_INFORMATION
      {
         public System.IntPtr hProcess;
         public System.IntPtr hThread;
         public int dwProcessId;
         public int dwThreadId;
      }

      [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
      internal struct SECURITY_ATTRIBUTES
      {
         public int nLength;
         public System.IntPtr lpSecurityDescriptor;
         public int bInheritHandle;
      }

      #endregion

#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
//#elif false
        #region macOS

        [System.Runtime.InteropServices.DllImport("ProcessStart")]
        internal static extern System.IntPtr RunCommand(string command);

        #endregion

#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
//#elif true

        #region Linux

        [System.Runtime.InteropServices.DllImport("libProcessStart")]
        internal static extern int RunCommand(string command);

        #endregion

#endif
   }

   #endregion


   #region CTProcessStartInfo

   /// <summary>Specifies a set of values that are used when you start a process (mimicking the "System.Diagnostics.ProcessStartInfo"-class with the most important properties).</summary>
   public class CTProcessStartInfo
   {
      public CTProcessStartInfo()
      {
         StandardErrorEncoding = StandardOutputEncoding = System.Text.Encoding.UTF8;
         UseThread = true;
      }

      /// <summary>Gets or sets the application to be threaded.</summary>
      public bool UseThread { get; set; }

      /// <summary>Gets or sets the application to be started in cmd (command prompt).</summary>
      public bool UseCmdExecute { get; set; }

      /// <summary>Gets or sets the application or document to start.</summary>
      public string FileName { get; set; }

      /// <summary>Gets or sets the set of command-line arguments to use when starting the application.</summary>
      public string Arguments { get; set; }

      /// <summary>Gets or sets a value indicating whether to start the process in a new window.</summary>
      public bool CreateNoWindow { get; set; }

      /// <summary>Gets or sets the working directory for the process to be started.</summary>
      public string WorkingDirectory { get; set; }

      /// <summary>Gets or sets a value that indicates whether the textual output of an application is written to the StandardOutput stream.</summary>
      public bool RedirectStandardOutput { get; set; }

      /// <summary>Gets or sets a value that indicates whether the error output of an application is written to the StandardError stream.</summary>
      public bool RedirectStandardError { get; set; }

      /// <summary>Gets or sets the preferred encoding for standard output (UTF8 per default).</summary>
      public System.Text.Encoding StandardOutputEncoding { get; set; }

      /// <summary>Gets or sets the preferred encoding for error output (UTF8 per default).</summary>
      public System.Text.Encoding StandardErrorEncoding { get; set; }

      /// <summary>Gets or sets a value indicating whether to use the operating system shell to start the process (ignored, always false).</summary>
      public bool UseShellExecute { get; set; }
   }

   #endregion
}
#endif
// © 2019-2022 crosstales LLC (https://www.crosstales.com)