using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

using Mono.Cecil;

namespace Cobber.Core.Project
{
    /// <summary>
    /// CobberAssembly, where its Name is full path for assembly file
    /// </summary>
    public class CobberAssembly : CobberObject  
    {
        public AssemblyDefinition Object; // mono assembly object, may need to resolve
        public bool IsMain; // only one exists in assemblies

        // or Name = (obj as AssemblyDefinition).Name.Name ?

        public List<CobberModule> Modules; // children

        public CobberAssembly()
        {
            IsMain = false;
            Modules = new List<CobberModule>();
        }

        public override List<CobberObject> GetChildren()
        {
            List<CobberObject> ret = new List<CobberObject>();
            foreach (var m in Modules)
            {
                ret.Add(m);
            }
            return ret;
        }


        public override CobberIcon XgetIcon()
        {
            return (IsMain ? CobberIcon.Main : CobberIcon.Assembly);
        }

        public override CobberIconOverlay XgetIconStatic()
        {
            return CobberIconOverlay.None;
        }
        public override CobberIconVisible XgetIconVisible()
        {
            return CobberIconVisible.Public;
        }




        // resolve current mono object and its children into cobber modules
        //here parent is null by default, and name is full path for assembly file
        public override void Resolve()
        {
            // resolve myself object
            if (this.Object == null)
            {
                //string path = (basePath == null ? Path : System.IO.Path.GetFullPath(System.IO.Path.Combine(basePath, Path)));
                this.Object = AssemblyDefinition.ReadAssembly(this.Name, new ReaderParameters(ReadingMode.Immediate));
            }
            
            this.IsMain = (this.Object.MainModule.EntryPoint != null);
            //mainAsm.Object.MainModule.Kind == ModuleKind.Dll || mainAsm.Object.MainModule.Kind == ModuleKind.NetModule)

            // resolve my children as cobber modules
            //foreach (ModuleDefinition m in from ModuleDefinition x in this.Object.Modules orderby x.Name select x)
            foreach (var m in this.Object.Modules)
            {
                CobberModule mod = this.Modules.SingleOrDefault(_ => _.Name == m.Name);
                if (mod == null)
                {
                    mod = new CobberModule(this)
                    {
                        Name = m.Name,
                    };

                    this.Modules.Add(mod);
                }
            }
        }


        public override XmlElement Save(XmlDocument xmlDoc)
        {
            XmlElement elem = xmlDoc.CreateElement("assembly", CobberProject.Namespace);

            XmlAttribute nameAttr = xmlDoc.CreateAttribute("path");
            nameAttr.Value = this.Name;
            elem.Attributes.Append(nameAttr);

            if (IsMain != false)
            {
                XmlAttribute mainAttr = xmlDoc.CreateAttribute("isMain");
                mainAttr.Value = IsMain.ToString().ToLower();
                elem.Attributes.Append(mainAttr);
            }

            this.SaveObfConfig(xmlDoc, elem);

            foreach (var mod in this.Modules)
            {
                if (mod.IsTreeInherited()) continue;  // 20140613

                elem.AppendChild(mod.Save(xmlDoc));
            }

            return elem;
        }

        public override void Load(XmlElement elem)
        {
            this.Name = elem.Attributes["path"].Value;
            if (elem.Attributes["isMain"] != null)
            {
                this.IsMain = bool.Parse(elem.Attributes["isMain"].Value);
            }

            LoadObfConfig(elem);

            // go deeper into module
            foreach (XmlElement i in elem.ChildNodes.OfType<XmlElement>())
            {
                CobberModule mod = new CobberModule(this);
                mod.Load(i);
                this.Modules.Add(mod);
            }
        }
    }
}