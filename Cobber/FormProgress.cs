using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

using Cobber;
using Cobber.Core;
using Cobber.Core.Project;


// More information about background worker, please refer to:
//  http://www.cnblogs.com/jaxu/archive/2011/05/13/2045702.html
//  http://blog.csdn.net/lightlater/article/details/8092991


namespace Cobber
{
    public partial class FormProgress : Form
    {
        Thread thread; // background worker

        public FormProgress()
        {
            InitializeComponent();

            CheckForIllegalCrossThreadCalls = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "Cancel")
            {
                if (this.thread != null) { this.thread.Abort(); }
                label1.Text = "Aborted.";
                button1.Text = "OK";
            }
            else
            {
                Close();
            }
        }

        void SetProgress(double percentage)
        {
            this.progressBar1.Value = (int)(percentage * progressBar1.Maximum);  // not necessarily in %

            // display x% in progressbar
            try
            {
                /*
                int percent = (int)(percentage * 100);
                this.progressBar1.CreateGraphics().DrawString(percent.ToString() + "%",
                          new Font("Arial", (float)8.25, FontStyle.Regular),
                          Brushes.Black, new PointF(progressBar1.Width / 2 - 10,
                          progressBar1.Height / 2 - 7));
                */
            }
            catch (Exception)
            {
                // sometimes over 3 times will get "out of memory" exception
            }
        }


        // wrap cobber.Process(project) into a thread running in background
        public void ProcessAsync(Core.Cobber cobber, CobberProject proj)
        {
            //Core.Cobber cobber = new Cobber.Core.Cobber();
            {
                cobber.Logger.BeginAssembly += BeginAssembly;
                cobber.Logger.EndAssembly += EndAssembly;
                cobber.Logger.Phase += Phase;
                cobber.Logger.Log += Logging;
                cobber.Logger.Warn += Warning;
                cobber.Logger.Progress += Progressing;
                cobber.Logger.Error += Error;
                cobber.Logger.Finish += Finish;
            }

            thread = new Thread(delegate() { cobber.Process(proj); });
            thread.IsBackground = true;
            thread.Name = "Cobbering";
            thread.Start();
        }



        #region Logging

        public void BeginAssembly(object sender, AssemblyEventArgs e)
        {
            this.label1.Text = string.Format("Processing '{0}'...", e.Assembly.Name);
        }

        public void EndAssembly(object sender, AssemblyEventArgs e)
        {
            //
        }

        public void Phase(object sender, LogEventArgs e)
        {
            this.label1.Text = e.Message;
         }

        public void Progressing(object sender, ProgressEventArgs e)
        {
            double percentage = (e.Progress / (double)e.Total);
            SetProgress(percentage);
        }

        public void Logging(object sender, LogEventArgs e)
        {
            this.label1.Text = e.Message;
        }

        public void Warning(object sender, LogEventArgs e)
        {
            this.label1.Text = e.Message;
        }

        public void Finish(object sender, LogEventArgs e)
        {
            this.label1.Text = "Obfuscation is successful!";
            button1.Text = "OK";
            SetProgress(1);
        }

        public void Error(object sender, ExceptionEventArgs e)
        {
            this.label1.Text = "Obfuscation failed";
            button1.Text = "OK";
        }

        #endregion
    }
}
