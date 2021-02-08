using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using Ref = System.Reflection;

using Mono.Cecil;

namespace Cobber.Core.Project
{
    /// <summary>
    /// interface for CobberAssembly, CobberModule, CobberNamespace, CobberType, CobberMember
    /// refer to Mono.Cecil AssemblyDefinition, ModuleDefinition, TypeDefinition, and IMemberDefinition 
    /// </summary>
    public abstract class CobberObject
    {
        #region Obfuscation Attributes for Cobber
        // obf setting name, and the obfuscation setting will be from CobberProject
        public string ObfSettingName = null; // obfuscation setting name
        public bool ApplyToMembers = true; // default
        public bool Inherited = true; // if inherited from parent, no need to save obf setting name in XML
        #endregion


        public CobberObject Parent = null;
        public string Name; // object ID name
        public abstract List<CobberObject> GetChildren();


        // how to get namespaces from module?
        //   from module.types.namespace.

        // how to find namespace for a nested type?
        //   while (type.DeclaringType != null)
        //   {
        //        type = type.DeclaringType;
        //    }
        //
        //   // root type now
        //   get roottype.Namespace, roottype.Module

        // how to get pure members, i.e., not types ?
        //  its parent is member.DeclaringType

        public abstract CobberIcon XgetIcon();
        public abstract CobberIconOverlay XgetIconStatic();
        public abstract CobberIconVisible XgetIconVisible();


        public bool Injected = false; // for UI display disable only. default is from native dll

        public override string ToString() { return Name; }

        // Func: resolve myself's name properties, and children objects
        // Prerequisite:  either (parent,myname) or (myobject) was assigned
        //     if object is not null, do; else from parent
        public abstract void Resolve();


        // determine whether itself and its children all have inherited setting
        // for writing XML in simple way.
        public bool IsTreeInherited()
        {
            if (!this.Inherited) return false;
            foreach (var c in this.GetChildren())
            {
                if (!c.IsTreeInherited()) return false;
            }

            return true;
        }

        #region Mark obfuscation setting
        public void Mark(string setting)
        {
            Resolve();

            string next_setting = ApplyRules(setting);

            if (true) //RecognizeObfuscationAttributes)
            {
                // disable current setting if recognizing other exclusion attributes
                ApplyObfuscationAttributeRules(); 
            }

            // mark obfuscation setting for all members
            foreach (CobberObject member in this.GetChildren())
            {
                member.Mark(next_setting);  // recursive marking
            }
        }

        // Func:  apply parent obfuscation rules to current object
        // Called by: 
        //    MarkAssembly(CobberAssembly)
        //    MarkModule(CobberModule)
        //    MarkNamespace(CobberNamespace)
        //    MarkType(CobberType)
        //    MarkMember(CobberMember)
        // Input:   IProjectObject obj, Marking mark
        // Changed:  mark
        // Return:   next setting applied to members
        public string ApplyRules(string parent_setting)
        {
            string next_setting = null;

            if (parent_setting == null)  // no inherited setting
            {
                this.Inherited = false;
                if (this.ApplyToMembers)
                {
                    next_setting = this.ObfSettingName;
                }
            }
            else // parent setting is here
            {
                // the existing setting will persist
                if (!string.IsNullOrEmpty(this.ObfSettingName) && this.ObfSettingName != parent_setting)
                {
                    this.Inherited = false;
                    if (this.ApplyToMembers)
                    {
                        next_setting = this.ObfSettingName;
                    }
                    else
                    {
                        next_setting = parent_setting;
                    }
                }
                else
                {
                    this.Inherited = true;
                    this.ObfSettingName = parent_setting;  // changed
                    this.ApplyToMembers = true;
                    next_setting = parent_setting;
                }
            }

            return next_setting;
        }


        // recognize .NET obfuscation attributes
        // obj could be: AssemblyDefinition, ModuleDefinition, TypeDefinition, IMemberDefinition
        // so far applies to CobberMember's IMemberDefinition (for type and member) only
        public void ApplyObfuscationAttributeRules()
        {
            IMemberDefinition obj = null;
            if (this is CobberType)
            {
                obj = (this as CobberType).Object;
            }
            if (this is CobberMember)
            {
                obj = (this as CobberMember).Object;
            }
            if (obj == null) return;


            //ObfuscationSetting mark = from my current setting;

            var attributes = GetObfuscationAttributes(obj);
            if (attributes == null) return;

            foreach (var x in attributes)
            {
                if (x.Exclude)
                {
                    if (String.IsNullOrEmpty(x.Feature)) // Feature empty => all obfuscations
                    {
                        this.ObfSettingName = null;  // disable our obfuscation setting
                    }
                    else
                    {
                        // TODO: disable particular obfuscation setting
                        //  need to modify current obf setting or create new one
                        /*
                        var obf = cobber.Obfuscators[x.Feature];  
                        if (obf != null)
                        {
                            mark.ObfParameters.Remove(obf.ID);
                        }
                        */
                    }
                }
            }
        }

        // Cobber respects .NET Obfuscation Attributes
        // Any Attribute with the name "ObfuscationAttribute" is recognized (not only System.Reflection)
        // Supported Properties:
        //    ApplyToMembers
        //    Feature (intepreted as obfuscator id)
        //    Exclude
        //  
        //  The patch supports
        // * Disable/Enable specific obfuscation on attribute target and members
        // * Disable entire obfuscation on attribute target and members
        //        
        //  Not supported:
        // * Attribute stripping
        // * Enable entire obfuscation
        private static IEnumerable<Ref.ObfuscationAttribute> GetObfuscationAttributes(object obj)
        {
            // resolve ApplyToMembers
            var memberDef = obj as IMemberDefinition;
            if (memberDef != null)
            {
                var parent = memberDef.DeclaringType;
                var parentAttributes = GetObfuscationAttributes(parent);
                if (parentAttributes != null)
                    foreach (var x in parentAttributes)
                        if (x.ApplyToMembers)
                            yield return x;
            }

            var attProv = obj as ICustomAttributeProvider; // defined in Mono.Cecil
            if (attProv != null)
            {
                foreach (var x in attProv.CustomAttributes)
                    if (x.AttributeType.Name == "ObfuscationAttribute")
                        yield return new Ref.ObfuscationAttribute()
                        {
                            ApplyToMembers = (from p in x.Properties where p.Name == "ApplyToMembers" select (bool)p.Argument.Value).FirstOrDefault(),
                            Exclude = (from p in x.Properties where p.Name == "Exclude" select (bool)p.Argument.Value).FirstOrDefault(),
                            Feature = (from p in x.Properties where p.Name == "Feature" select (string)p.Argument.Value).FirstOrDefault(),
                            StripAfterObfuscation = (from p in x.Properties where p.Name == "StripAfterObfuscation" select (bool)p.Argument.Value).FirstOrDefault(),
                        };
            }
        }
        #endregion

        #region XML save and load

        public abstract XmlElement Save(XmlDocument xmlDoc);
        public abstract void Load(XmlElement elem);

        // specifiy one setting of obfuscations in XML format
        // example:  
        //    <assembly path="C:\Cobber\sample\OleStorage.dll" config="normal">  means not inherited, default apply to members
        //    <assembly path="C:\Cobber\sample\OleStorage.dll" inherit="true">   means  inherited
        //    <assembly path="C:\Cobber\sample\OleStorage.dll" config="normal" applytomembers="false">  means not inherited, not apply to members
        public void SaveObfConfig(XmlDocument xmlDoc, XmlElement elem)
        {
            if (Inherited)  // mark as inherited
            {
                XmlAttribute attr = xmlDoc.CreateAttribute("inherit");
                attr.Value = Inherited.ToString().ToLower();
                elem.Attributes.Append(attr);
            }
            else // not inherited, should save my config
            {
                if (ObfSettingName != null)
                {
                    XmlAttribute configAttr = xmlDoc.CreateAttribute("config");
                    configAttr.Value = ObfSettingName;
                    elem.Attributes.Append(configAttr);
                }
            }

            if (ApplyToMembers != true)  // is not default value
            {
                XmlAttribute attr = xmlDoc.CreateAttribute("applytomembers");
                attr.Value = ApplyToMembers.ToString().ToLower();
                elem.Attributes.Append(attr);
            }
        }

        public void LoadObfConfig(XmlElement elem)
        {
            if (elem.Attributes["config"] != null)
                this.ObfSettingName = elem.Attributes["config"].Value;
            else
                this.ObfSettingName = null; // default

            if (elem.Attributes["inherit"] != null)
                this.Inherited = bool.Parse(elem.Attributes["inherit"].Value);
            else
                this.Inherited = false; // default

            if (elem.Attributes["applytomembers"] != null)
                this.ApplyToMembers = bool.Parse(elem.Attributes["applytomembers"].Value);
            else
                this.ApplyToMembers = true; // default
        }

        #endregion
    }
}
