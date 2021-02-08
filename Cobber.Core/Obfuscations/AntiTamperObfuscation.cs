using System;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Cryptography;

using Mono.Cecil;
using Mono.Cecil.Metadata;
using Mono.Cecil.Cil;
using Mono.Cecil.PE;

namespace Cobber.Core.Obfuscations
{
    public partial class AntiTamperObfuscation : IObfuscation
    {
        interface IAntiTamper
        {
            Cobber cobber { get; set; }
            Action<object, IMemberDefinition, HelperAttribute> AddHelper { get; set; }
            void InitPhase1(ModuleDefinition mod);
            void Phase1(ModuleDefinition mod);
            void InitPhase2(ModuleDefinition mod);
            void Phase2(IProgresser progresser, ModuleDefinition mod);
            void Phase3(MetadataProcessor.MetadataAccessor accessor);
            void Phase4(MetadataProcessor.ImageAccessor accessor);
            void Phase5(Stream stream, MetadataProcessor.ImageAccessor accessor);
        }

        class Phase1 : StructurePhase
        {
            AntiTamperObfuscation cion;
            public Phase1(AntiTamperObfuscation cion) { this.cion = cion; }
            public override Priority Priority { get { return Priority.AssemblyLevel; } }
            public override IObfuscation Obfuscator { get { return cion; } }
            public override int PhaseID { get { return 1; } }
            public override bool WholeRun { get { return true; } }

            public override void Initialize(ModuleDefinition mod)
            {
                this.mod = mod;
            }
            public override void DeInitialize()
            {
                //
            }

            ModuleDefinition mod;
            public override void Process(object target, NameValueCollection parameters)
            {
                IAntiTamper ver;

                cobber.Database.AddEntry("AntiTamper", "Type", parameters["type"] ?? "normal");
                if (parameters["type"] == "jit")
                    ver = new JIT();
                else
                    ver = new Mem();
                ver.AddHelper = AddHelper;
                ver.cobber = cobber;
                cion.vers[mod] = ver;

                ver.InitPhase1(mod);
                ver.Phase1(mod);
            }
        }
        class Phase2 : StructurePhase, IProgressProvider
        {
            AntiTamperObfuscation cion;
            public Phase2(AntiTamperObfuscation cion) { this.cion = cion; }
            public override Priority Priority { get { return Priority.AssemblyLevel; } }
            public override IObfuscation Obfuscator { get { return cion; } }
            public override int PhaseID { get { return 2; } }
            public override bool WholeRun { get { return false; } }

            public override void Initialize(ModuleDefinition mod)
            {
                this.mod = mod;
                cion.vers[mod].InitPhase2(mod);
            }
            public override void DeInitialize()
            {
                //
            }

            ModuleDefinition mod;
            public override void Process(object target, NameValueCollection parameters)
            {
                cion.vers[mod].Phase2(progresser, mod);
            }

            IProgresser progresser;
            public void SetProgresser(IProgresser progresser) { this.progresser = progresser; }
        }
        class Phase3 : MetadataPhase
        {
            AntiTamperObfuscation cion;
            public Phase3(AntiTamperObfuscation cion) { this.cion = cion; }
            public override Priority Priority { get { return Priority.MetadataLevel; } }
            public override IObfuscation Obfuscator { get { return cion; } }
            public override int PhaseID { get { return 2; } }

            public override void Process(NameValueCollection parameters, MetadataProcessor.MetadataAccessor accessor)
            {
                cion.vers[accessor.Module].Phase3(accessor);
            }
        }
        class Phase4 : ImagePhase
        {
            AntiTamperObfuscation cion;
            public Phase4(AntiTamperObfuscation cion) { this.cion = cion; }
            public override Priority Priority { get { return Priority.MetadataLevel; } }
            public override IObfuscation Obfuscator { get { return cion; } }
            public override int PhaseID { get { return 4; } }

            public override void Process(NameValueCollection parameters, MetadataProcessor.ImageAccessor accessor)
            {
                cion.vers[accessor.Module].Phase4(accessor);
            }
        }
        class Phase5 : PePhase        
        {
            AntiTamperObfuscation cion;
            public Phase5(AntiTamperObfuscation cion) { this.cion = cion; }
            public override Priority Priority { get { return Priority.PELevel; } }
            public override IObfuscation Obfuscator { get { return cion; } }
            public override int PhaseID { get { return 5; } }

            public override void Process(NameValueCollection parameters, Stream stream, MetadataProcessor.ImageAccessor accessor)
            {
                cion.vers[accessor.Module].Phase5(stream, accessor);
            }
        }

        Dictionary<ModuleDefinition, IAntiTamper> vers = new Dictionary<ModuleDefinition, IAntiTamper>();
        Phase[] phases;

        public string Name { get { return "Anti Tampering"; } }
        public string Description { get { return "provide a better protection than strong name for maintain integration."; } }
        public string ID { get { return "anti tamper"; } }
        public bool StandardCompatible { get { return false; } }
        public Target Target { get { return Target.Module; } }
        public Preset Preset { get { return Preset.Maximum; } }
        public bool SupportLateAddition { get { return true; } }
        public Behaviour Behaviour { get { return Behaviour.Inject | Behaviour.AlterCode | Behaviour.Encrypt; } }

        public Phase[] Phases
        {
            get { if (phases == null)phases = new Phase[] { new Phase1(this), new Phase2(this), new Phase3(this), new Phase4(this), new Phase5(this) }; return phases; }
        }


        public Dictionary<string, List<string>> Parameters
        {
            get
            {
                return new Dictionary<string, List<string>>()
                {
                    {"type", new List<string> {"normal","jit"}}
                };
            }
        }


        public void Init() { vers.Clear(); }
        public void Deinit() { vers.Clear(); }
    }
}