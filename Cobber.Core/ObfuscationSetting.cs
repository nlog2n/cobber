using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using Cobber.Core.Project;

namespace Cobber.Core
{
    public enum Preset
    {
        None = 0,
        Minimum = 1,
        Normal = 2, // normal includes minimum, and such
        Aggressive = 3,
        Maximum = 4,
    }

    /// <summary>
    /// define one setting of obfuscations with parameters and provide XML functions
    /// </summary>
    public class ObfuscationSetting
    {
        #region static funcitons
        static int _setting_id = 0; // for naming the setting objects
        static string CreateNewSettingName()
        {
            _setting_id++;
            return "setting" + _setting_id.ToString(); 
        }

        // a list of obfuscation settings, for both predefined and customized settings
        public static Dictionary<string, ObfuscationSetting> ObfSettings = new Dictionary<string, ObfuscationSetting>(); // key = setting name

        public static void AddSetting(ObfuscationSetting setting)
        {
            ObfSettings[setting.Name] = setting;
        }

        public static ObfuscationSetting GetSetting(string setting_name)
        {
            if (ObfSettings.ContainsKey(setting_name))
            {
                return ObfSettings[setting_name];
            }

            return null;
        }

        public static List<ObfuscationSetting> GetSettings()
        {
            return ObfSettings.Values.ToList();
        }
        #endregion

        public string Name { get; set; }  // setting name, like "setting1"
        public Dictionary<string, NameValueCollection> ObfParameters; // key = obfuscator's ID

        internal bool IsInternal = false; // true if program generated; false if user-defined

        public bool IsPreset()
        {
            foreach (Preset preset in Enum.GetValues(typeof(Preset)) )
            {
                if ( string.Compare(Name, preset.ToString(), true) == 0 )
                    return true;
            }

            return false;
        }

        // create a new setting
        public ObfuscationSetting()
        {
            this.Name = CreateNewSettingName();
            this.ObfParameters = new Dictionary<string, NameValueCollection>();
        }

        // create a new setting and copy values from an existing one
        public ObfuscationSetting(ObfuscationSetting setting)
        {
            this.Name = CreateNewSettingName();
            this.ObfParameters = new Dictionary<string, NameValueCollection>();
            if (setting != null)
            {
                foreach (var i in setting.ObfParameters)
                {
                    this.ObfParameters.Add(i.Key, new NameValueCollection(i.Value));
                }
            }
        }

        // clone an existing setting (with same name also)
        public static ObfuscationSetting Clone(ObfuscationSetting setting)
        {
            ObfuscationSetting result = new ObfuscationSetting(setting);
            result.Name = setting.Name;
            _setting_id--; // do not consume setting ids.
            return result;
        }

        #region XML load and save

        //example:
        //        
        //  <settings name="settings1">
        //    <obfuscation id="invalid md" />
        //    <obfuscation id="anti dump" />
        //    <obfuscation id="anti tamper" />
        //  </settings>
        //

        public XmlElement Save(XmlDocument xmlDoc)
        {
            XmlElement elem = xmlDoc.CreateElement("obfsetting", CobberProject.Namespace);

            XmlAttribute nAttr = xmlDoc.CreateAttribute("name");
            nAttr.Value = Name;
            elem.Attributes.Append(nAttr);

            foreach (string id in ObfParameters.Keys)
            {
                NameValueCollection param = ObfParameters[id];

                elem.AppendChild(SaveItem(xmlDoc, "obfuscation", id, param));
            }

            return elem;
        }

        // item examples:
        // <obfuscation id="res encrypt" />, or
        // <packer id="compress" />
        public static XmlElement SaveItem(XmlDocument xmlDoc, string pack_or_obf,
            string Id, NameValueCollection param)
        {
            XmlElement elem = xmlDoc.CreateElement(
                pack_or_obf,
                //typeof(T) == typeof(Packer) ? "packer" : "obfuscation", 
                CobberProject.Namespace);

            XmlAttribute idAttr = xmlDoc.CreateAttribute("id");
            idAttr.Value = Id;
            elem.Attributes.Append(idAttr);

            foreach (var name in param.AllKeys)
            {
                XmlElement arg = xmlDoc.CreateElement("argument", CobberProject.Namespace);

                XmlAttribute nameAttr = xmlDoc.CreateAttribute("name");
                nameAttr.Value = name;
                arg.Attributes.Append(nameAttr);
                XmlAttribute valAttr = xmlDoc.CreateAttribute("value");
                valAttr.Value = param[name];
                arg.Attributes.Append(valAttr);

                elem.AppendChild(arg);
            }

            return elem;
        }

        public void Load(XmlElement elem)
        {
            this.Name = elem.Attributes["name"].Value;

            foreach (XmlElement sub in elem.ChildNodes.OfType<XmlElement>())
            {
                NameValueCollection param;
                string id = LoadItem(sub, out param);

                this.ObfParameters[id] = param; // add
            }
        }

        public static string LoadItem(XmlElement elem, out NameValueCollection param)
        {
            string Id = elem.Attributes["id"].Value; ; // obf id
            
            param = new NameValueCollection();
            foreach (XmlElement arg in elem.ChildNodes.OfType<XmlElement>())
            {
                param.Add(arg.Attributes["name"].Value, arg.Attributes["value"].Value);
            }

            return Id;
        }
        #endregion
    }
}