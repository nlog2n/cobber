using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

using Mono.Cecil;

using Cobber.Core.Project;

namespace Cobber.Core
{
    public abstract class Phase
    {
        public Cobber cobber;
        protected void Log(string message) { cobber.Log(message); }

        public abstract int PhaseID { get; }
        public abstract Priority Priority { get; }
        public abstract bool WholeRun { get; }

        protected Random Random { get { return cobber.Random; } }
        protected CobberDatabase Database { get { return cobber.Database; } }

        public abstract IObfuscation Obfuscator { get; }
        protected NameHelper Namer { get { return cobber.Namer; } }

        internal Phase() { }
        public abstract void Initialize(ModuleDefinition mod);
        public abstract void DeInitialize();
        
        public virtual Analyzer GetAnalyzer() { return null; }
        protected void AddHelper(object parent, IMemberDefinition helper, HelperAttribute attr) { cobber.InjectObject(parent, helper, attr); }
    }

    public abstract class StructurePhase : Phase
    {
        public abstract void Process(object target, NameValueCollection parameters);
    }

    public abstract class MetadataPhase : Phase
    {
        public abstract void Process(NameValueCollection parameters, MetadataProcessor.MetadataAccessor accessor);
        public override sealed void Initialize(ModuleDefinition mod) { }
        public override sealed void DeInitialize() { }
        public override sealed bool WholeRun { get { return true; } }
    }

    public abstract class PePhase : Phase
    {
        public abstract void Process(NameValueCollection parameters, Stream stream, MetadataProcessor.ImageAccessor accessor);
        public override sealed void Initialize(ModuleDefinition mod) { }
        public override sealed void DeInitialize() { }
        public override sealed bool WholeRun { get { return true; } }
    }

    public abstract class ImagePhase : Phase
    {
        public abstract void Process(NameValueCollection parameters, MetadataProcessor.ImageAccessor accessor);
        public override sealed void Initialize(ModuleDefinition mod) { }
        public override sealed void DeInitialize() { }
        public override sealed bool WholeRun { get { return true; } }
    }
}