﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;

using Mono.Cecil;

namespace Cobber.Core.Obfuscations
{
    public class AntiILDasmObfuscation : StructurePhase, IObfuscation
    {
        public string Name { get { return "Anti IL Dasm"; } }
        public string Description { get { return "mark the assembly with a attribute that prevent ILDasm disassemble the assembly."; } }
        public string ID { get { return "anti ildasm"; } }
        public bool StandardCompatible { get { return true; } }
        public Target Target { get { return Target.Module; } }
        public Preset Preset { get { return Preset.Minimum; } }
        public override Priority Priority { get { return Priority.AssemblyLevel; } }
        public override IObfuscation Obfuscator { get { return this; } }
        public override int PhaseID { get { return 1; } }
        public override bool WholeRun { get { return true; } }
        public bool SupportLateAddition { get { return false; } }
        public Behaviour Behaviour { get { return Behaviour.AlterStructure; } }

        public Phase[] Phases { get { return new Phase[] { this }; } }

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
            MethodReference ctor = mod.Import(typeof(SuppressIldasmAttribute).GetConstructor(Type.EmptyTypes));
            bool has = false;
            foreach (CustomAttribute att in mod.CustomAttributes)
                if (att.Constructor.ToString() == ctor.ToString())
                {
                    has = true;
                    break;
                }

            if (!has)
                mod.CustomAttributes.Add(new CustomAttribute(ctor));
        }

        public void Init() { }
        public void Deinit() { }
    }
}
