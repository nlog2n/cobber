using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace Cobber.Core
{
    /// <summary>
    /// Interface for NameAnalyzer
    /// </summary>
    public abstract class Analyzer : IProgressProvider
    {
        protected Cobber Cobber { get; private set; }
        protected Logger Logger { get; private set; }
        protected IProgresser Progresser { get; private set; }
        public abstract void Analyze(IEnumerable<AssemblyDefinition> asms);

        internal void SetCobber(Cobber cr)
        {
            this.Cobber = cr;
            this.Logger = cr.Logger;
        }
        public void SetProgresser(IProgresser progresser)
        {
            this.Progresser = progresser;
        }
    }
}
