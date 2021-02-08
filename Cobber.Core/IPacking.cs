using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.IO;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.PE;

using Cobber.Core.Project;

namespace Cobber.Core
{
    public class PackerSetting
    {
        public CobberAssembly[] Assemblies { get; internal set; }
        public ModuleDefinition[] Modules { get; internal set; }
        public byte[][] PEs { get; internal set; }
        public NameValueCollection Parameters { get; internal set; }
    }

    public abstract class Packer
    {
        public abstract string ID { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract bool StandardCompatible { get; }

        public Cobber cobber;
        protected void Log(string message) { cobber.Log(message); }

        protected NameHelper Namer { get { return cobber.Namer; } }
        protected Random Random { get { return cobber.Random; } }
        protected CobberDatabase Database { get { return cobber.Database; } }

        internal protected virtual void ProcessModulePhase1(ModuleDefinition mod, bool isMain) { }
        internal protected virtual void ProcessModulePhase3(ModuleDefinition mod, bool isMain) { }
        internal protected virtual void ProcessMetadataPhase1(MetadataProcessor.MetadataAccessor accessor, bool isMain) { }
        internal protected virtual void ProcessMetadataPhase2(MetadataProcessor.MetadataAccessor accessor, bool isMain) { }
        internal protected virtual void ProcessImage(MetadataProcessor.ImageAccessor accessor, bool isMain) { }
        internal protected virtual void PostProcessMetadata(MetadataProcessor.MetadataAccessor accessor) { }
        internal protected virtual void PostProcessImage(MetadataProcessor.ImageAccessor accessor) { }

        public abstract string[] Pack(PackerSetting param);
        
        // called by Pack(param) at the end, to obfuscate compressshell additionally
        protected string[] ProtectStub(AssemblyDefinition asmOne)
        {
            string tmp = Path.GetTempPath() + "\\" + Path.GetRandomFileName() + "\\";
            Directory.CreateDirectory(tmp);

            ModuleDefinition modDef = this.cobber.Project.Assemblies.Single(_ => _.IsMain).Object.MainModule;
            asmOne.MainModule.TimeStamp = modDef.TimeStamp;
            byte[] mvid = new byte[0x10];
            Random.NextBytes(mvid);
            asmOne.MainModule.Mvid = new Guid(mvid);

            string asmOneFileName = Path.GetFileName(modDef.FullyQualifiedName);

            // handle resource section
            MetadataProcessor psr = new MetadataProcessor();
            Section oldRsrc = null;
            foreach (Section s in modDef.GetSections())
            {
                if (s.Name == ".rsrc") { oldRsrc = s; break; }
            }
            if (oldRsrc != null)
            {
                psr.ProcessImage += accessor =>
                {
                    Section sect = null;
                    foreach (Section s in accessor.Sections)
                    {
                        if (s.Name == ".rsrc") { sect = s; break; }
                    }
                    if (sect == null)
                    {
                        sect = new Section()
                        {
                            Name = ".rsrc",
                            Characteristics = 0x40000040
                        };
                        foreach (Section s in accessor.Sections)
                        {
                            if (s.Name == ".text") { accessor.Sections.Insert(accessor.Sections.IndexOf(s) + 1, sect); break; }
                        }
                    }
                    sect.VirtualSize = oldRsrc.VirtualSize;
                    sect.SizeOfRawData = oldRsrc.SizeOfRawData;
                    int idx = accessor.Sections.IndexOf(sect);
                    sect.VirtualAddress = accessor.Sections[idx - 1].VirtualAddress + ((accessor.Sections[idx - 1].VirtualSize + 0x2000U - 1) & ~(0x2000U - 1));
                    sect.PointerToRawData = accessor.Sections[idx - 1].PointerToRawData + accessor.Sections[idx - 1].SizeOfRawData;
                    for (int i = idx + 1; i < accessor.Sections.Count; i++)
                    {
                        accessor.Sections[i].VirtualAddress = accessor.Sections[i - 1].VirtualAddress + ((accessor.Sections[i - 1].VirtualSize + 0x2000U - 1) & ~(0x2000U - 1));
                        accessor.Sections[i].PointerToRawData = accessor.Sections[i - 1].PointerToRawData + accessor.Sections[i - 1].SizeOfRawData;
                    }
                    ByteBuffer buff = new ByteBuffer(oldRsrc.Data);
                    PatchResourceDirectoryTable(buff, oldRsrc, sect);
                    sect.Data = buff.GetBuffer();
                };
            }
            psr.Process(asmOne.MainModule, tmp + asmOneFileName, new WriterParameters()
            {
                StrongNameKeyPair = this.cobber.SNKey,
                WriteSymbols = this.cobber.Project.Debug
            });

            // prepare an obf setting for packed project
            ObfuscationSetting setting;
            {
                string anySetting = null;
                if (this.cobber.Project.ObfSettingName != null)
                {
                    anySetting = this.cobber.Project.ObfSettingName;
                }
                else
                {
                    CobberAssembly main = this.cobber.Project.Assemblies.Single(_ => _.IsMain);
                    if (main.ObfSettingName != null)
                    {
                        anySetting = main.ObfSettingName;
                    }
                    else if (main.Modules.Count != 0 && main.Modules[0].ObfSettingName != null)
                    {
                        anySetting = main.Modules[0].ObfSettingName;
                    }
                }

                setting = new ObfuscationSetting(ObfuscationSetting.GetSetting(anySetting));
                setting.Name = "packerSettings";

                ObfuscationSetting.AddSetting(setting); // add
                setting.IsInternal = true; // mark this setting as program generated
            }

            // create a new project for packed project
            CobberProject proj = new CobberProject();
            {
                // copy from existing project
                proj.ObfSettingName = this.cobber.Project.ObfSettingName;
                proj.SNKeyPath = this.cobber.Project.SNKeyPath;
                proj.SNKeyPassword = this.cobber.Project.SNKeyPassword;
                proj.Seed = Random.Next().ToString();
                proj.Debug = this.cobber.Project.Debug;
                proj.OutputPath = tmp;

                proj.Assemblies.Add(new CobberAssembly()
                {
                    Name = tmp + asmOneFileName,
                    ObfSettingName = setting.Name, // "packerSettings",
                    ApplyToMembers = true,
                    IsMain = true //(this.cobber.Project.GetMainAssembly() != null)  // added on 20140519
                });
            }

            // create a new cobber to do obfuscation and packing
            Cobber cr = new Cobber();
            {
                cr.PostProcessMetadata = PostProcessMetadata;
                cr.PostProcessImage = PostProcessImage;
            }
            cr.Process(proj);

            return Directory.GetFiles(tmp);
        }


        static void PatchResourceDirectoryTable(ByteBuffer resources, Section old, Section @new)
        {
            resources.Advance(12);
            int num = resources.ReadUInt16() + resources.ReadUInt16();
            for (int i = 0; i < num; i++)
            {
                PatchResourceDirectoryEntry(resources, old, @new);
            }
        }
        static void PatchResourceDirectoryEntry(ByteBuffer resources, Section old, Section @new)
        {
            resources.Advance(4);
            uint num = resources.ReadUInt32();
            int position = resources.Position;
            resources.Position = ((int)num) & 0x7fffffff;
            if ((num & 0x80000000) != 0)
            {
                PatchResourceDirectoryTable(resources, old, @new);
            }
            else
            {
                PatchResourceDataEntry(resources, old, @new);
            }
            resources.Position = position;
        }
        static void PatchResourceDataEntry(ByteBuffer resources, Section old, Section @new)
        {
            uint num = resources.ReadUInt32();
            resources.Position -= 4;
            resources.WriteUInt32(num - old.VirtualAddress + @new.VirtualAddress);
        }
    }
}