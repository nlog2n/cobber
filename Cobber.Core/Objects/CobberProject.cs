using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Xml;
using System.Xml.Schema;

namespace Cobber.Core.Project
{
    // Cobber project was saved in an XML file
    public class CobberProject : CobberObject
    {
        #region XML data
        public static readonly XmlSchema Schema = XmlSchema.Read(typeof(CobberProject).Assembly.GetManifestResourceStream("Cobber.Core.CobberPrj.xsd"), null);
        public const string Namespace = "http://www.nlog2n.com";

        public string Seed { get; set; } // random seed
        public bool Debug { get; set; } // generate debug symbols or not
        public bool RecognizeObfuscationAttributes {get; set;} // recognize existing obfuscation attributes, global option

        public string BasePath { get; set; }
        public string OutputPath { get; set; }  // relative to BasePath if base not null
        public string SNKeyPath { get; set; }  //For pfx, use "xxx.pfx|password" 
        public string SNKeyPassword { get; set; } // for pfx password

        public string XAPfilename { get; set; } // xap file name for windows phone app

        public List<CobberAssembly> Assemblies; // assemblies setting, need to mark. this part is cobber/mono.cecil related

        public string PackerID; // current packer ID for global
        public NameValueCollection PackerParameters; // packer parameters

        #endregion

        public CobberProject()
        {
            Assemblies = new List<CobberAssembly>();
            PackerID = null;
            PackerParameters = new NameValueCollection();

            ObfSettingName = "normal"; // global preset, see enum Preset
            Inherited = false;  // no parent
            Name = "Untitled.cobber";  // project name, //  @"C:\Users\fanghui\Desktop\Dropbox\Cobber\sample\sample.cobber";
        }

        public override List<CobberObject> GetChildren()
        {
            List<CobberObject> ret = new List<CobberObject>();
            foreach (var asm in Assemblies)
            {
                ret.Add(asm);
            }
            return ret;
        }

        public override CobberIcon XgetIcon()
        {
            return CobberIcon.Project;
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
            // TODO:
        }

        public CobberAssembly GetMainAssembly()
        {
            return this.Assemblies.SingleOrDefault(_ => _.IsMain);
        }

        #region Add or Remove Assembly
        public bool AddAssembly(string asmFilename)
        {
            bool isModified = false;

            if (this.Assemblies.SingleOrDefault(_ => _.Name == asmFilename) == null)
            {
                this.Assemblies.Add(new CobberAssembly() { Name = asmFilename });
                isModified = true;
            }

            return isModified;
        }

        public void RemoveAssembly( CobberAssembly asm)
        {
            this.Assemblies.Remove(asm);
        }
        #endregion

        #region Load and Save in XML file

        // only for override, do not use this
        public override XmlElement Save(XmlDocument xmlDoc)
        {
            return null;
        }

        // only for override, do not use this
        public override void Load(XmlElement elem)
        {
        }

        // save project to a file
        public bool Save(string xmlFile)
        {
            string filename = (string.IsNullOrEmpty(xmlFile) ? this.Name : xmlFile);
            XmlDocument xmlDoc = this.Save();
            xmlDoc.Save(filename);
            this.Name = filename;
            return true;
        }

        // save project to an XML document
        public XmlDocument Save()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Schemas.Add(Schema);

            XmlElement elem = xmlDoc.CreateElement("project", Namespace);

            XmlAttribute outputAttr = xmlDoc.CreateAttribute("outputDir");
            outputAttr.Value = OutputPath;
            elem.Attributes.Append(outputAttr);

            XmlAttribute snAttr = xmlDoc.CreateAttribute("snKey");
            snAttr.Value = SNKeyPath;
            elem.Attributes.Append(snAttr);

            if (ObfSettingName.ToLower() != "none") //Preset.None)
            {
                XmlAttribute presetAttr = xmlDoc.CreateAttribute("preset");
                presetAttr.Value = ObfSettingName.ToLower();
                elem.Attributes.Append(presetAttr);
            }

            if (Seed != null)
            {
                XmlAttribute seedAttr = xmlDoc.CreateAttribute("seed");
                seedAttr.Value = Seed;
                elem.Attributes.Append(seedAttr);
            }

            if (Debug != false)
            {
                XmlAttribute debugAttr = xmlDoc.CreateAttribute("debug");
                debugAttr.Value = Debug.ToString().ToLower();
                elem.Attributes.Append(debugAttr);
            }

            foreach (var i in ObfuscationSetting.GetSettings())
            {
                if (i.IsPreset()) continue; // only save user-defined setting

                if (i.IsInternal) continue; // do not save program-generated internal settings

                elem.AppendChild(i.Save(xmlDoc));
            }

            if ( !string.IsNullOrEmpty(PackerID) )
            {
                XmlElement sub = ObfuscationSetting.SaveItem(xmlDoc, "packer", PackerID, PackerParameters);
                elem.AppendChild(sub);
            }

            foreach (var asm in this.Assemblies)
            {
                elem.AppendChild(asm.Save(xmlDoc));
            }

            xmlDoc.AppendChild(elem);
            return xmlDoc;
        }

        public bool Load(string xmlFile)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlFile);
            Load(xmlDoc);
            this.Name = xmlFile;
            return true;
        }

        public void Load(XmlDocument doc)
        {
            doc.Schemas.Add(Schema);
            doc.Validate(null);

            XmlElement docElem = doc.DocumentElement;

            this.OutputPath = docElem.Attributes["outputDir"].Value;
            this.SNKeyPath = docElem.Attributes["snKey"].Value;

            if (docElem.Attributes["preset"] != null)
            {
                Preset p = (Preset)Enum.Parse(typeof(Preset), docElem.Attributes["preset"].Value, true);
                this.ObfSettingName = p.ToString().ToLower();
            }
            else
            {
                this.ObfSettingName = "none"; // Preset.None;
            }

            if (docElem.Attributes["seed"] != null)
                this.Seed = docElem.Attributes["seed"].Value;
            else
                this.Seed = null;

            if (docElem.Attributes["debug"] != null)
                this.Debug = bool.Parse(docElem.Attributes["debug"].Value);
            else
                this.Debug = false;

            foreach (XmlElement i in docElem.ChildNodes.OfType<XmlElement>())
            {
                if (i.Name == "obfsetting")
                {
                    ObfuscationSetting set = new ObfuscationSetting();
                    set.Load(i);
                    ObfuscationSetting.AddSetting(set);
                }
                else if (i.Name == "packer")
                {
                    NameValueCollection param;
                    PackerID = ObfuscationSetting.LoadItem(i, out param);
                    PackerParameters = param; // add
                }
                else if ( i.Name == "assembly" )
                {
                    CobberAssembly asm = new CobberAssembly();
                    asm.Load(i);
                    this.Assemblies.Add(asm);
                }
            }
        }
        #endregion
    }
}
