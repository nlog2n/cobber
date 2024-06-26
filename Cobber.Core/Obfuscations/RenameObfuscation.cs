﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

using Mono.Cecil;
using Mono.Cecil.Cil;

using Cobber.Core.Analyzers;

namespace Cobber.Core.Obfuscations
{
    public class RenameObfuscation : StructurePhase, IObfuscation
    {
        public string ID { get { return "rename"; } }
        public string Name { get { return "Renaming"; } }
        public string Description { get { return "rename members to unprintable name thus the decompiled source code can neither be compiled nor read."; } }
        public Target Target { get { return Target.Types | Target.Fields | Target.Methods | Target.Properties | Target.Events; } }
        public Preset Preset { get { return Preset.Minimum; } }
        public bool StandardCompatible { get { return true; } }
        public bool SupportLateAddition { get { return true; } }
        public Behaviour Behaviour { get { return Behaviour.AlterStructure; } }
        public Phase[] Phases { get { return new Phase[] { this }; } }
        public override int PhaseID { get { return 1; } }
        public override Priority Priority { get { return Priority.Safe; } }
        public override bool WholeRun { get { return false; } }
        public override Analyzer GetAnalyzer() { return new NameAnalyzer(); }
        public override IObfuscation Obfuscator { get { return this; } }


        public Dictionary<string, List<string>> Parameters
        {
            get
            {
                return new Dictionary<string, List<string>>()
                {
                    {"type", new List<string> {"Letters","Unreadable","ASCII"}}
                };
            }
        }


        public void Init() { }
        public void Deinit() { }


        ModuleDefinition mod;
        public override void Initialize(ModuleDefinition mod) { this.mod = mod; }

        public override void DeInitialize()
        {
            foreach (Resource res in mod.Resources)
            {
                if (!res.Name.EndsWith(".resources")) continue;
                string cult = mod.Assembly.Name.Culture;
                Identifier id = new Identifier()
                {
                    scope = string.IsNullOrEmpty(cult) ? res.Name.Substring(0, res.Name.LastIndexOf('.')) : res.Name.Substring(0, res.Name.LastIndexOf('.', res.Name.LastIndexOf('.') - 1)),
                    name = string.IsNullOrEmpty(cult) ? res.Name.Substring(res.Name.LastIndexOf('.') + 1) : res.Name.Substring(res.Name.LastIndexOf('.', res.Name.LastIndexOf('.') - 1) + 1)
                };
                foreach (IReference refer in (res as IAnnotationProvider).Annotations[NameAnalyzer.RenRef] as List<IReference>)
                {
                    refer.UpdateReference(id, id);
                }
            }
        }

        bool GetRenOk(IAnnotationProvider provider)
        {
            if (provider.Annotations[NameAnalyzer.RenOk] != null)
                return (bool)provider.Annotations[NameAnalyzer.RenOk];
            else
                return false;
        }
        bool GetCancel(IAnnotationProvider provider)
        {
            if (provider.Annotations[NameAnalyzer.RenRef] == null)
                return false;
            foreach (IReference refer in provider.Annotations[NameAnalyzer.RenRef] as List<IReference>)
                if (refer.QueryCancellation())
                    return true;
            return false;
        }

        public override void Process(object target, NameValueCollection parameters)
        {
            IMemberDefinition mem = target as IMemberDefinition;
            if (GetCancel(mem))
                return;

            // get renaming mode from parameter or by default.
            var mode = (NameMode)(mem.Module as IAnnotationProvider).Annotations[NameAnalyzer.RenMode];
            {
                NameMode? specMode = null;
                string xxx = parameters["type"];
                if (!string.IsNullOrEmpty(xxx))
                {
                    try
                    {
                        specMode = (NameMode)Enum.Parse(typeof(NameMode), xxx, true);
                    }
                    catch (Exception)
                    {
                    }
                }
                if (specMode != null && specMode.Value != mode)
                {
                    mode = specMode.Value;
                }
            }

            if (mem is TypeDefinition)
            {
                TypeDefinition type = mem as TypeDefinition;
                if (GetRenOk(type))
                {
                    string originalName = type.Name;
                    string originalFName = TypeParser.ToParseable(type);
                    type.Name = Namer.GetNewName(originalFName, mode);
                    switch (mode)
                    {
                        case NameMode.Unreadable:
                            type.Namespace = ""; break;
                        case NameMode.ASCII:
                            type.Namespace = " "; break;
                        case NameMode.Letters:
                            type.Namespace = "COBBER"; break;
                    }
                    Identifier id = (Identifier)(type as IAnnotationProvider).Annotations[NameAnalyzer.RenId];
                    Identifier n = id;
                    n.name = CecilHelper.GetName(type);
                    n.scope = CecilHelper.GetNamespace(type);
                    foreach (IReference refer in (type as IAnnotationProvider).Annotations[NameAnalyzer.RenRef] as List<IReference>)
                    {
                        refer.UpdateReference(id, n);
                    }
                    Database.AddEntry("Rename", originalName, type.Name);
                }
            }
            else if (mem is MethodDefinition)
            {
                MethodDefinition mtd = mem as MethodDefinition;
                PerformMethod(mtd, mode);
            }
            else if (GetRenOk(mem as IAnnotationProvider))
            {
                mem.Name = Namer.GetNewName(mem.Name, mode);
                Identifier id = (Identifier)(mem as IAnnotationProvider).Annotations[NameAnalyzer.RenId];
                Identifier n = id;
                n.scope = mem.DeclaringType.FullName;
                n.name = mem.Name;
                foreach (IReference refer in (mem as IAnnotationProvider).Annotations[NameAnalyzer.RenRef] as List<IReference>)
                {
                    refer.UpdateReference(id, n);
                }
            }
        }

        void PerformMethod(MethodDefinition mtd, NameMode mode)
        {
            if (GetRenOk(mtd))
            {
                mtd.Name = Namer.GetNewName(mtd.Name, mode);
                Identifier id = (Identifier)(mtd as IAnnotationProvider).Annotations[NameAnalyzer.RenId];
                Identifier n = id;
                n.scope = mtd.DeclaringType.FullName;
                n.name = mtd.Name;
                foreach (IReference refer in (mtd as IAnnotationProvider).Annotations[NameAnalyzer.RenRef] as List<IReference>)
                {
                    refer.UpdateReference(id, n);
                }

                foreach (ParameterDefinition para in mtd.Parameters)
                {
                    para.Name = Namer.GetNewName(para.Name, mode);
                }
            }

            //Variable names are stored in pdb, so no need to rename
            //if (mtd.HasBody)
            //{
            //    foreach (VariableDefinition var in mtd.Body.Variables)
            //    {
            //        var.Name = Namer.GetNewName(var.Name, mode);
            //    }
            //}
        }



        static string GetSig(MethodReference mtd)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(mtd.ReturnType.FullName);
            sb.Append(" ");
            sb.Append(mtd.Name);
            sb.Append("(");
            if (mtd.HasParameters)
            {
                for (int i = 0; i < mtd.Parameters.Count; i++)
                {
                    ParameterDefinition param = mtd.Parameters[i];
                    if (i > 0)
                    {
                        sb.Append(",");
                    }
                    if (param.ParameterType.IsSentinel)
                    {
                        sb.Append("...,");
                    }
                    sb.Append(param.ParameterType.FullName);
                }
            }
            sb.Append(")");
            return sb.ToString();
        }
        static string GetSig(FieldReference fld)
        {
            return fld.FieldType.FullName + " " + fld.Name;
        }
    }
}
