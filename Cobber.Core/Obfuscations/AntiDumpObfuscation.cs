﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Cobber.Core.Obfuscations
{
    public class AntiDumpObfuscation : StructurePhase, IObfuscation
    {
        public string Name { get { return "Anti Dumping"; } }
        public string Description { get { return "prevent the assembly from dumping from memory."; } }
        public string ID { get { return "anti dump"; } }
        public bool StandardCompatible { get { return false; } }
        public Target Target { get { return Target.Module; } }
        public Preset Preset { get { return Preset.Aggressive; } }
        public Phase[] Phases { get { return new Phase[] { this }; } }
        public bool SupportLateAddition { get { return false; } }
        public Behaviour Behaviour { get { return Behaviour.Inject; } }

        public override Priority Priority { get { return Priority.AssemblyLevel; } }
        public override IObfuscation Obfuscator { get { return this; } }
        public override int PhaseID { get { return 1; } }
        public override bool WholeRun { get { return true; } }


        public Dictionary<string, List<string>> Parameters
        {
            get
            {
                return new Dictionary<string, List<string>>();
            }
        }


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
            AssemblyDefinition self = AssemblyDefinition.ReadAssembly(typeof(Iid).Assembly.Location);
            TypeDefinition type = CecilHelper.Inject(mod, self.MainModule.GetType("AntiDumping"));
            mod.Types.Add(type);
            TypeDefinition modType = mod.GetType("<Module>");
            ILProcessor psr = modType.GetStaticConstructor().Body.GetILProcessor();
            psr.InsertBefore(psr.Body.Instructions.Count - 1, Instruction.Create(OpCodes.Call, type.Methods.FirstOrDefault(mtd => mtd.Name == "Initialize")));

            type.Name = Namer.GetRandomName();
            type.Namespace = "";
            AddHelper(mod, type, HelperAttribute.NoInjection);
            foreach (MethodDefinition mtdDef in type.Methods)
            {
                mtdDef.Name = Namer.GetRandomName();
                AddHelper(type, mtdDef, HelperAttribute.NoInjection);
            }
            Database.AddEntry("AntiDump", "Helper", type.FullName);
        }

        public void Init() { }
        public void Deinit() { }
    }
}