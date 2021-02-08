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
    /// namespace under specified module. 
    /// collected from TypeDefinition.Namespace. 
    /// Note that Mono.Cecil does not define Namespace
    /// </summary>
    public class CobberNamespace : CobberObject
    {
        // namespace does not contain an object. please refer to its parent's module
        // and its name is from TypeDefinition.Namespace

        public List<CobberType> Types; // children

        public CobberNamespace(CobberObject parent)
        {
            this.Parent = parent;
            this.Types = new List<CobberType>();
        }

        public override List<CobberObject> GetChildren()
        {
            List<CobberObject> ret = new List<CobberObject>();
            foreach (var m in Types)
            {
                ret.Add(m);
            }
            return ret;
        }

        public override CobberIcon XgetIcon()
        {
            return CobberIcon.Namespace;
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
            //this.Name = name;

            foreach (var t in (this.Parent as CobberModule).Object.Types.Where(_ => _.Namespace == this.Name)) // given name of namespace
            {
                CobberType type = this.Types.SingleOrDefault(_ => _.Name == t.Name);
                if (type == null)
                {
                    type = new CobberType(this)
                    {
                        Name = t.Name, // create a new type with parent = ns
                    };

                    this.Types.Add(type);
                }
            }
        }


        public override XmlElement Save(XmlDocument xmlDoc)
        {
            XmlElement elem = xmlDoc.CreateElement("namespace", CobberProject.Namespace);

            XmlAttribute fnAttr = xmlDoc.CreateAttribute("name");
            fnAttr.Value = Name;
            elem.Attributes.Append(fnAttr);

            this.SaveObfConfig(xmlDoc, elem);

            foreach (var i in this.Types)
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

            foreach (XmlElement i in elem.ChildNodes.OfType<XmlElement>())
            {
                CobberType type = new CobberType(this);
                type.Load(i);
                this.Types.Add(type);
            }
        }
    }
}
