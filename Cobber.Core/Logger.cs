using System;

using Cobber.Core.Project;

namespace Cobber.Core
{
    public class AssemblyEventArgs : EventArgs
    {
        public AssemblyEventArgs(CobberAssembly asmDef) { this.Assembly = asmDef; }
        public CobberAssembly Assembly { get; private set; }
    }

    public class LogEventArgs : EventArgs
    {
        public LogEventArgs(string msg) { this.Message = msg; }
        public string Message { get; private set; }
    }

    public class ProgressEventArgs : EventArgs
    {
        public ProgressEventArgs(int progress, int overall)
        {
            this.Progress = progress;
            this.Total = overall;
        }
        public int Progress { get; private set; }
        public int Total { get; private set; }
    }

    public class ExceptionEventArgs : EventArgs
    {
        public ExceptionEventArgs(Exception ex) { this.Exception = ex; }
        public Exception Exception { get; private set; }
    }


    public interface IProgresser
    {
        void SetProgress(int progress, int overall);
    }
    public interface IProgressProvider
    {
        void SetProgresser(IProgresser progresser);
    }

    public class Logger : IProgresser
    {
        public event EventHandler<AssemblyEventArgs> BeginAssembly;
        public event EventHandler<AssemblyEventArgs> EndAssembly;
        public event EventHandler<LogEventArgs> Phase;
        public event EventHandler<LogEventArgs> Log;
        public event EventHandler<LogEventArgs> Warn;
        public event EventHandler<ProgressEventArgs> Progress;
        public event EventHandler<ExceptionEventArgs> Error;
        public event EventHandler<LogEventArgs> Finish;

        class Asm : IDisposable
        {
            public CobberAssembly asmDef;
            public Logger logger;
            public void Dispose()
            {
                if (logger.EndAssembly != null)
                    logger.EndAssembly(logger, new AssemblyEventArgs(asmDef));
            }
        }

        internal void _Phase(string phase)
        {
            if (Phase != null)
                Phase(this, new LogEventArgs(phase));
        }
        internal IDisposable _Assembly(CobberAssembly asmDef)
        {
            if (BeginAssembly != null)
                BeginAssembly(this, new AssemblyEventArgs(asmDef));
            return new Asm() { asmDef = asmDef, logger = this };
        }
        public void _Log(string message)
        {
            if (Log != null)
                Log(this, new LogEventArgs(message));
        }
        public void _Warn(string message)
        {
            if (Warn != null)
                Warn(this, new LogEventArgs(message));
        }
        public void _Progress(int progress, int overall)
        {
            if (Progress != null)
                Progress(this, new ProgressEventArgs(progress, overall));
        }
        internal void _Fatal(Exception ex)
        {
            if (Error != null)
                Error(this, new ExceptionEventArgs(ex));
            else
                throw ex;
        }
        internal void _Finish(string message)
        {
            if (Finish != null)
                Finish(this, new LogEventArgs(message));
        }

        void IProgresser.SetProgress(int progress, int overall)
        {
            _Progress(progress, overall);
        }
    }
}