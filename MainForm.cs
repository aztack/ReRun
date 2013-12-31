using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Runtime.InteropServices;

namespace Rerun
{
    public partial class MainForm : Form
    {
        private string ServerScript;
        private Process proc = null;
        private string md5OfServerScript = "";
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            fileSystemWatcher.Path = System.IO.Path.GetDirectoryName(Application.ExecutablePath);
        }

        private void fileSystemWatcher_Changed(object sender, System.IO.FileSystemEventArgs e)
        {
            if (ServerScript != null && e.FullPath.ToLower().IndexOf(ServerScript.ToLower()) >= 0)
            {
                Rerun(ServerScript);
            }   
        }

        private void log_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void log_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            string script = files[0];
            fileSystemWatcher.Path = System.IO.Path.GetDirectoryName(script);
            ServerScript = script;
            log.AppendText(script + "\n");
            Rerun(script);
        }

        const uint CTRL_C_EVENT = 0;
        const uint CTRL_BREAK_EVENT = 1;

        [DllImport("kernel32.dll")]
        static extern bool GenerateConsoleCtrlEvent(
            uint dwCtrlEvent,
            uint dwProcessGroupId);

        private void Rerun(string path)
        {
            string md5 = "";
            try
            {
                md5 = GetMD5HashFromFile(path);
            }catch(Exception){
                return;
            }
            if (md5 != md5OfServerScript)
            {
                md5OfServerScript = md5;
                if (proc != null)
                {
                    try
                    {
                        GenerateConsoleCtrlEvent(CTRL_C_EVENT, (uint)proc.Id);
                        proc.Kill();
                        proc.CloseMainWindow();
                        log.AppendText(String.Format("Process#{0} killed\n", proc.Id));
                    }
                    catch (Exception e) {
                        log.AppendText(e.Message);
                    }
                }
                RunScript(path);
            }
        }

        protected string GetMD5HashFromFile(string fileName)
        {
            System.IO.FileStream file = new System.IO.FileStream(fileName, FileMode.Open);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(file);
            file.Close();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }

        public void RunScript(string script)
        {
            Process process = new Process();

            //process.OutputDataReceived += process_OutputDataReceived;
            ProcessStartInfo info = new ProcessStartInfo(script);
            //info.Arguments = String.Join(" ", arguments);
            info.UseShellExecute = true;
            //info.RedirectStandardError = true;
            //info.RedirectStandardOutput = true;
            info.WindowStyle = ProcessWindowStyle.Normal;
            process.StartInfo = info;
            process.EnableRaisingEvents = true;
            process.Start();
            //process.BeginOutputReadLine();
            proc = process;
            //errorMessage = process.StandardError.ReadToEnd();
            log.AppendText(String.Format("Process#{0} created\n", proc.Id));
        }
        private void process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    log.AppendText(e.Data + "\n");
                }
            }
        }
    }
}
