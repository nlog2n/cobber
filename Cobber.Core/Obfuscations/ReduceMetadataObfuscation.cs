using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

using Mono.Cecil;

namespace Cobber.Core.Obfuscations
{
    public class ReduceMetadataObfuscation : StructurePhase, IObfuscation
    {
        public string Name { get { return "Reduce Metadata"; } }
        public string Description { get { return @"remove unnecessary metadata carried by the assembly. do not apply to application which relys on Reflection."; } }
        public string ID { get { return "reduce md"; } }
        public bool StandardCompatible { get { return true; } }
        public Target Target { get { return Target.Events | Target.Properties | Target.Types; } }
        public Preset Preset { get { return Preset.Maximum; } }
        public override Priority Priority { get { return Priority.TypeLevel; } }
        public override IObfuscation Obfuscator { get { return this; } }
        public override int PhaseID { get { return 1; } }
        public override bool WholeRun { get { return false; } }
        public bool SupportLateAddition { get { return true; } }
        public Behaviour Behaviour { get { return Behaviour.AlterStructure; } }
        public Phase[] Phases { get { return new Phase[] { this }; } }

        public Dictionary<string, List<string>> Parameters
        {
            get
            {
                return new Dictionary<string, List<string>>();
            }
        }


        public void Init() { }
        public void Deinit() { }


        public override void Initialize(ModuleDefinition mod)
        {
            //
        }
        public override void DeInitialize()
        {
            //
        }

        public override void Process(object target, NameValueCollection parameters)
        {
            IMemberDefinition def = target as IMemberDefinition;

            TypeDefinition t;
            if ((t = def as TypeDefinition) != null && !IsTypePublic(t))
            {
                if (t.IsEnum)
                {
                    int idx = 0;
                    while (t.Fields.Count != 1)
                        if (t.Fields[idx].Name != "value__")
                            t.Fields.RemoveAt(idx);
                        else
                            idx++;
                    Database.AddEntry("MdReduce", t.FullName, "Enum");
                }
            }
            else if (def is EventDefinition)
            {
                if (def.DeclaringType != null)
                {
                    Database.AddEntry("MdReduce", def.FullName, "Evt");
                    def.DeclaringType.Events.Remove(def as EventDefinition);
                }
            }
            else if (def is PropertyDefinition)
            {
                if (def.DeclaringType != null)
                {
                    Database.AddEntry("MdReduce", def.FullName, "Prop");
                    def.DeclaringType.Properties.Remove(def as PropertyDefinition);
                }
            }
        }

        bool IsTypePublic(TypeDefinition type)
        {
            do
            {
                if (!type.IsPublic && !type.IsNestedFamily && !type.IsNestedFamilyAndAssembly && !type.IsNestedFamilyOrAssembly && !type.IsNestedPublic && !type.IsPublic)
                    return false;
                type = type.DeclaringType;
            } while (type != null);
            return true;
        }
    }
}
