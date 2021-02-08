using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

using Mono.Cecil;

namespace Cobber.Core.Project
{
    public class CobberModule : CobberObject
    {
        public ModuleDefinition Object; // mono object, may need to resolve

        public List<CobberNamespace> Namespaces; // children

        public CobberModule(CobberObject parent)
        {
            this.Parent = parent;
            this.Namespaces = new List<CobberNamespace>();
        }

        public override List<CobberObject> GetChildren()
        {
            List<CobberObject> ret = new List<CobberObject>();
            foreach (var m in Namespaces)
            {
                ret.Add(m);
            }
            return ret;
        }

        public override CobberIcon XgetIcon()
        {
            return CobberIcon.Module;
        }

        public override CobberIconOverlay XgetIconStatic()
        {
            return CobberIconOverlay.None;
        }

        public override CobberIconVisible XgetIconVisible()
        {
            return CobberIconVisible.Public;
        }



        public override void Resolve()
        {
            // resolve myself object
            if (this.Object == null)
            {
                this.Object = (this.Parent as CobberAssembly).Object.Modules.Single(_ => _.Name == this.Name);
            }

            // resolve my children, namespaces
            foreach (var t in this.Object.Types.Select(_ => _.Namespace).Distinct()) // no duplicates
            {
                CobberNamespace ns = this.Namespaces.SingleOrDefault(_ => _.Name == t);
                if (ns == null)
                {
                    ns = new CobberNamespace(this)
                    {
                        Name = t,
                    };

                    this.Namespaces.Add(ns); // add this new namespace
                }
            }
        }


        public override XmlElement Save(XmlDocument xmlDoc)
        {
            XmlElement elem = xmlDoc.CreateElement("module", CobberProject.Namespace);

            XmlAttribute nameAttr = xmlDoc.CreateAttribute("name");
            nameAttr.Value = Name;
            elem.Attributes.Append(nameAttr);

            this.SaveObfConfig(xmlDoc, elem);

            foreach (var i in this.Namespaces)
            {
                if (i.IsTreeInherited()) continue;  // 20140613

                elem.AppendChild(i.Save(xmlDoc));
            }

            return elem;
        }

        public override void Load(XmlElement elem)
        {
            this.Name = elem.Attributes["name"].Value;

            LoadObfConfig(elem);

            // go deep into children
            foreach (XmlElement i in elem.ChildNodes.OfType<XmlElement>())
            {
                CobberNamespace ns = new CobberNamespace(this);
                ns.Load(i);
                this.Namespaces.Add(ns);
            }
        }
    }
}