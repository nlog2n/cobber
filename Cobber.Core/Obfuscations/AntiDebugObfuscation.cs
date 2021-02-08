using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections.Specialized;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Cobber.Core.Obfuscations
{
    public class AntiDebugObfuscation : StructurePhase, IObfuscation
    {
        ModuleDefinition mod;

        public string Name { get { return "Anti Debug"; } }
        public string Description { get { return "prevent the assembly from debugging/profiling."; } }
        public string ID { get { return "anti debug"; } }
        public bool StandardCompatible { get { return true; } }
        public Target Target { get { return Target.Module; } }
        public Preset Preset { get { return Preset.Normal; } } // suggestion
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
                return new Dictionary<string, List<string>>()
                {
                    {"win32", new List<string> {"false","true"}}
                };
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

        /// <summary>
        /// process anti-debug obfuscation. when win32 set, it will
        /// use Windows API to detect debugger, which only
        /// works on windows 2000 and above
        /// </summary>
        /// <param name="parameter"></param>
        public override void Process(object target, NameValueCollection parameters)
        {
            AssemblyDefinition self = AssemblyDefinition.ReadAssembly(typeof(Iid).Assembly.Location);
            Database.AddEntry("AntiDebug", "Win32", Array.IndexOf(parameters.AllKeys, "win32") != -1);
            if (Array.IndexOf(parameters.AllKeys, "win32") != -1)
            {
                TypeDefinition type = CecilHelper.Inject(mod, self.MainModule.GetType("AntiDebugger"));
                type.Methods.Remove(type.Methods.FirstOrDefault(mtd => mtd.Name == "AntiDebugSafe"));
                type.Methods.Remove(type.Methods.FirstOrDefault(mtd => mtd.Name == "InitializeSafe"));
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
                Database.AddEntry("AntiDebug", "Helper", type.FullName);
            }
            else
            {
                TypeDefinition type = CecilHelper.Inject(mod, self.MainModule.GetType("AntiDebugger"));
                type.Methods.Remove(type.Methods.FirstOrDefault(mtd => mtd.Name == "AntiDebug"));
                type.Methods.Remove(type.Methods.FirstOrDefault(mtd => mtd.Name == "Initialize"));
                mod.Types.Add(type);
                TypeDefinition modType = mod.GetType("<Module>");
                ILProcessor psr = modType.GetStaticConstructor().Body.GetILProcessor();
                psr.InsertBefore(psr.Body.Instructions.Count - 1, Instruction.Create(OpCodes.Call, type.Methods.FirstOrDefault(mtd => mtd.Name == "InitializeSafe")));

                type.Name = Namer.GetRandomName();
                type.Namespace = "";
                AddHelper(mod, type, HelperAttribute.NoInjection);
                foreach (MethodDefinition mtdDef in type.Methods)
                {
                    mtdDef.Name = Namer.GetRandomName();
                    AddHelper(type, mtdDef, HelperAttribute.NoInjection);
                }
                Database.AddEntry("AntiDebug", "Helper", type.FullName);
            }
        }

        public void Init() { }
        public void Deinit() { }
    }
}
