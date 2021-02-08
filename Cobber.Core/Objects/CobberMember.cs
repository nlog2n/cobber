using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

using Mono.Cecil;

namespace Cobber.Core.Project
{
    public enum CobberMemberTypes
    {
        Method,
        Field,
        Property,
        Event
    }

    /// <summary>
    /// member definition for (method, field, property, and event).
    /// note that property and event have method members also.
    /// member name is from signature
    /// </summary>
    public class CobberMember : CobberObject
    {
        public CobberMemberTypes MemberType { get; set; }
        public IMemberDefinition Object; // mono object, may need to resolve

        public List<CobberMember> Methods;  // children members, only property and even can have methods.

        public CobberMember(CobberObject parent)
        {
            this.Parent = parent;
            this.Methods = new List<CobberMember>();
        }

        public override List<CobberObject> GetChildren()
        {
            List<CobberObject> ret = new List<CobberObject>();
            foreach (var m in Methods)
            {
                ret.Add(m);
            }
            return ret;
        }

        #region get icon
        public override CobberIcon XgetIcon()
        {
            if (MemberType == CobberMemberTypes.Event) 
                return CobberIcon.Event;

            if (MemberType == CobberMemberTypes.Field)
            {
                FieldDefinition field = (FieldDefinition)Object;
                if (field.IsStatic)
                {
                    if (field.DeclaringType.IsEnum)
                        return CobberIcon.Constant;
                }

                return CobberIcon.Field;
            }


            if (MemberType == CobberMemberTypes.Method)
            {
                if (!((Object as MethodReference).DeclaringType is ArrayType))
                {
                    MethodDefinition method = (MethodDefinition)Object;
                    string name = method.Name;
                    if ((name == ".ctor") || (name == ".cctor"))
                    {
                        return CobberIcon.Constructor;
                    }
                    else if (method.IsVirtual && !method.IsAbstract)
                    {
                        return CobberIcon.Omethod;
                    }
                }

                return CobberIcon.Method;
            }

            if (MemberType == CobberMemberTypes.Property)
            {
                PropertyDefinition prop = (PropertyDefinition)Object;
                MethodReference getMethod = prop.GetMethod;
                MethodDefinition getDecl = (getMethod == null) ? null : getMethod.Resolve();
                MethodReference setMethod = prop.SetMethod;
                MethodDefinition setDecl = (setMethod == null) ? null : setMethod.Resolve();
                if (getDecl != null && setDecl == null)
                {
                    return CobberIcon.Propget;
                }
                else if (setDecl != null && getDecl == null)
                {
                    return CobberIcon.Propset;
                }

                return CobberIcon.Property;
            }

            return CobberIcon.None; // failed to parse
        }
        #endregion

        #region get overlay for static members
        public override CobberIconOverlay XgetIconStatic()
        {
            CobberIconOverlay overlay = CobberIconOverlay.None;

            IMemberDefinition obj = this.Object;
            if (obj is FieldDefinition)
            {
                FieldDefinition field = (FieldDefinition)obj;
                if (field.IsStatic)
                {
                    if (!field.DeclaringType.IsEnum)
                        overlay = CobberIconOverlay.Static;
                }
            }
            else if (obj is MethodDefinition)
            {
                if (!((obj as MethodReference).DeclaringType is ArrayType))
                {
                    MethodDefinition method = (MethodDefinition)obj;
                    if (method.IsStatic)
                    {
                        overlay = CobberIconOverlay.Static;
                    }
                }
            }
            else if (obj is PropertyDefinition)
            {
                PropertyDefinition prop = (PropertyDefinition)obj;
                if (IsStatic(prop))
                {
                    overlay = CobberIconOverlay.Static;
                }
            }
            else if (obj is EventDefinition)
            {
                if (IsStatic(obj as EventReference))
                {
                    overlay = CobberIconOverlay.Static;
                }
            }

            return overlay;
        }

        static bool IsStatic(EventReference prop)
        {
            bool flag = false;
            EventDefinition Definition = prop.Resolve();
            if (Definition != null)
            {
                MethodReference addMethod = Definition.AddMethod;
                MethodDefinition addDecl = (addMethod == null) ? null : addMethod.Resolve();
                MethodReference removeMethod = Definition.RemoveMethod;
                MethodDefinition removeDecl = (removeMethod == null) ? null : removeMethod.Resolve();
                MethodReference invokeMethod = Definition.InvokeMethod;
                MethodDefinition invokeDecl = (invokeMethod == null) ? null : invokeMethod.Resolve();
                flag |= (addDecl != null) && addDecl.IsStatic;
                flag |= (removeDecl != null) && removeDecl.IsStatic;
                flag |= (invokeDecl != null) && invokeDecl.IsStatic;
            }
            return flag;
        }

        static bool IsStatic(PropertyReference prop)
        {
            bool flag = false;
            PropertyDefinition Definition = prop.Resolve();
            if (Definition != null)
            {
                MethodReference setMethod = Definition.SetMethod;
                MethodDefinition addDecl = (setMethod == null) ? null : setMethod.Resolve();
                MethodReference getMethod = Definition.GetMethod;
                MethodDefinition getDecl = (getMethod == null) ? null : getMethod.Resolve();
                flag |= (addDecl != null) && addDecl.IsStatic;
                flag |= (getDecl != null) && getDecl.IsStatic;
            }
            return flag;
        }
        #endregion

        #region Get visible overlay for non-public members
        public override CobberIconVisible XgetIconVisible()
        {
            CobberIconVisible visible = CobberIconVisible.Public; // public
            
            IMemberDefinition obj = this.Object;
            if (obj is FieldDefinition)
            {
                FieldDefinition field = (FieldDefinition)obj;
                switch (field.Attributes & FieldAttributes.FieldAccessMask)
                {
                    case FieldAttributes.CompilerControlled:
                    case FieldAttributes.Private:
                        visible = CobberIconVisible.Private;
                        break;
                    case FieldAttributes.FamANDAssem:
                    case FieldAttributes.Assembly:
                        visible = CobberIconVisible.Internal;
                        break;
                    case FieldAttributes.Family:
                        visible = CobberIconVisible.Protected;
                        break;
                    case FieldAttributes.FamORAssem:
                        visible = CobberIconVisible.Famasm;
                        break;
                    case FieldAttributes.Public:
                        visible = CobberIconVisible.Public;
                        break;
                }
            }
            else if (obj is MethodDefinition)
            {
                if (!((obj as MethodReference).DeclaringType is ArrayType))
                {
                    MethodDefinition method = (MethodDefinition)obj;
                    switch (method.Attributes & MethodAttributes.MemberAccessMask)
                    {
                        case MethodAttributes.CompilerControlled:
                        case MethodAttributes.Private:
                            visible = CobberIconVisible.Private;
                            break;
                        case MethodAttributes.FamANDAssem:
                        case MethodAttributes.Assembly:
                            visible = CobberIconVisible.Internal;
                            break;
                        case MethodAttributes.Family:
                            visible = CobberIconVisible.Protected;
                            break;
                        case MethodAttributes.FamORAssem:
                            visible = CobberIconVisible.Famasm;
                            break;
                        case MethodAttributes.Public:
                            visible = CobberIconVisible.Public;
                            break;
                    }
                }
            }
            else if (obj is PropertyDefinition)
            {
                PropertyDefinition prop = (PropertyDefinition)obj;
                switch (GetPropVisibility(prop))
                {
                    case MethodAttributes.CompilerControlled:
                    case MethodAttributes.Private:
                        visible = CobberIconVisible.Private;
                        break;
                    case MethodAttributes.FamANDAssem:
                    case MethodAttributes.Assembly:
                        visible = CobberIconVisible.Internal;
                        break;
                    case MethodAttributes.Family:
                        visible = CobberIconVisible.Protected;
                        break;
                    case MethodAttributes.FamORAssem:
                        visible = CobberIconVisible.Famasm;
                        break;
                    case MethodAttributes.Public:
                        visible = CobberIconVisible.Public;
                        break;
                }
            }
            else if (obj is EventDefinition)
            {
                switch (GetEvtVisibility(obj as EventReference))
                {
                    case MethodAttributes.CompilerControlled:
                    case MethodAttributes.Private:
                        visible = CobberIconVisible.Private;
                        break;
                    case MethodAttributes.FamANDAssem:
                    case MethodAttributes.Assembly:
                        visible = CobberIconVisible.Internal;
                        break;
                    case MethodAttributes.Family:
                        visible = CobberIconVisible.Protected;
                        break;
                    case MethodAttributes.FamORAssem:
                        visible = CobberIconVisible.Famasm;
                        break;
                    case MethodAttributes.Public:
                        visible = CobberIconVisible.Public;
                        break;
                }
            }

            return visible;
        }

        static MethodAttributes GetPropVisibility(PropertyReference prop)
        {
            MethodAttributes ret = MethodAttributes.Public;
            PropertyDefinition Definition = prop.Resolve();
            if (Definition != null)
            {
                MethodReference setMethod = Definition.SetMethod;
                MethodDefinition setDecl = (setMethod == null) ? null : setMethod.Resolve();
                MethodReference getMethod = Definition.GetMethod;
                MethodDefinition getDecl = (getMethod == null) ? null : getMethod.Resolve();
                if ((setDecl != null) && (getDecl != null))
                {
                    if ((getDecl.Attributes & MethodAttributes.MemberAccessMask) == (setDecl.Attributes & MethodAttributes.MemberAccessMask))
                    {
                        ret = getDecl.Attributes & MethodAttributes.MemberAccessMask;
                    }
                    return ret;
                }
                if (setDecl != null)
                {
                    return setDecl.Attributes & MethodAttributes.MemberAccessMask;
                }
                if (getDecl != null)
                {
                    ret = getDecl.Attributes & MethodAttributes.MemberAccessMask;
                }
            }
            return ret;
        }

        static MethodAttributes GetEvtVisibility(EventReference evt)
        {
            MethodAttributes ret = MethodAttributes.Public;
            EventDefinition Definition = evt.Resolve();
            if (Definition != null)
            {
                MethodReference addMethod = Definition.AddMethod;
                MethodDefinition addDecl = (addMethod == null) ? null : addMethod.Resolve();
                MethodReference removeMethod = Definition.RemoveMethod;
                MethodDefinition removeDecl = (removeMethod == null) ? null : removeMethod.Resolve();
                MethodReference invokeMethod = Definition.InvokeMethod;
                MethodDefinition invokeDecl = (invokeMethod == null) ? null : invokeMethod.Resolve();
                if (((addDecl != null) && (removeDecl != null)) && (invokeDecl != null))
                {
                    if (((addDecl.Attributes & MethodAttributes.MemberAccessMask) == (removeDecl.Attributes & MethodAttributes.MemberAccessMask)) && ((addDecl.Attributes & MethodAttributes.MemberAccessMask) == (invokeDecl.Attributes & MethodAttributes.MemberAccessMask)))
                    {
                        return addDecl.Attributes & MethodAttributes.MemberAccessMask;
                    }
                }
                else if ((addDecl != null) && (removeDecl != null))
                {
                    if ((addDecl.Attributes & MethodAttributes.MemberAccessMask) == (removeDecl.Attributes & MethodAttributes.MemberAccessMask))
                    {
                        return addDecl.Attributes & MethodAttributes.MemberAccessMask;
                    }
                }
                else if ((addDecl != null) && (invokeDecl != null))
                {
                    if ((addDecl.Attributes & MethodAttributes.MemberAccessMask) == (invokeDecl.Attributes & MethodAttributes.MemberAccessMask))
                    {
                        return addDecl.Attributes & MethodAttributes.MemberAccessMask;
                    }
                }
                else if ((removeDecl != null) && (invokeDecl != null))
                {
                    if ((removeDecl.Attributes & MethodAttributes.MemberAccessMask) == (invokeDecl.Attributes & MethodAttributes.MemberAccessMask))
                    {
                        return removeDecl.Attributes & MethodAttributes.MemberAccessMask;
                    }
                }
                else
                {
                    if (addDecl != null)
                    {
                        return addDecl.Attributes & MethodAttributes.MemberAccessMask;
                    }
                    if (removeDecl != null)
                    {
                        return removeDecl.Attributes & MethodAttributes.MemberAccessMask;
                    }
                    if (invokeDecl != null)
                    {
                        return invokeDecl.Attributes & MethodAttributes.MemberAccessMask;
                    }
                }

            }
            return ret;
        }
        #endregion



        // get children mono objects, which could be method only
        // required: myself object must be already resolved
        private List<MethodDefinition> get_children()
        {
            List<MethodDefinition> children = new List<MethodDefinition>();  // IAnnotationProvider

            IMemberDefinition parent = this.Object;

            if (parent is PropertyDefinition) // note: property could have methods
            {
                PropertyDefinition propDef = (PropertyDefinition)parent;
                if (propDef.GetMethod != null) { children.Add(propDef.GetMethod); }
                if (propDef.SetMethod != null) { children.Add(propDef.SetMethod); }
                if (propDef.HasOtherMethods)                
                {
                    foreach (var i in propDef.OtherMethods) { children.Add(i); }
                }
            }
            else if (parent is EventDefinition)  // event has methods
            {
                EventDefinition evtDef = (EventDefinition)parent;
                if (evtDef.AddMethod != null) { children.Add(evtDef.AddMethod); }
                if (evtDef.RemoveMethod != null) { children.Add(evtDef.RemoveMethod); }
                if (evtDef.InvokeMethod != null) { children.Add(evtDef.InvokeMethod); }
                if (evtDef.HasOtherMethods)
                {
                    foreach (var i in evtDef.OtherMethods) { children.Add(i); }
                }
            }

            return children;
        }


        // resolve this member in parent, according to signature and member type
        // Note: the implicit input is member type and name(signature)
        public override void Resolve()
        {
            if (this.Object != null)
            {
                // reset name and membertype, then return
                CobberMemberTypes t;
                this.Name = GetMemberSig(this.Object, out t);
                this.MemberType = t;
                return;
            }

            // otherwise search from parent's children
            CobberMemberTypes mtype = this.MemberType;
            string sig = this.Name;
            IMemberDefinition obj = null;  // output

            if (this.Parent is CobberType)
            {
                TypeDefinition type = (this.Parent as CobberType).Object;
                CobberMemberTypes x;
                switch (mtype)
                {
                    case CobberMemberTypes.Method:
                        foreach (var i in type.Methods) { if (GetMemberSig(i, out x) == sig && x == mtype) { obj = i; break; } }
                        break;
                    case CobberMemberTypes.Field:
                        foreach (var i in type.Fields) { if (GetMemberSig(i, out x) == sig && x == mtype) { obj = i; break; } }
                        break;
                    case CobberMemberTypes.Property:
                        foreach (var i in type.Properties) { if (GetMemberSig(i, out x) == sig && x == mtype) { obj = i; break; } }
                        break;
                    case CobberMemberTypes.Event:
                        foreach (var i in type.Events) { if (GetMemberSig(i, out x) == sig && x == mtype) { obj = i; break; } }
                        break;
                }
            }
            else  // parent is property or event
            {
                CobberMemberTypes x;
                foreach (var i in  (this.Parent as CobberMember).get_children()) 
                {
                    if (GetMemberSig((IMemberDefinition)i, out x) == sig && x == mtype) { obj = (IMemberDefinition)i; break; }
                }
            }


            if (obj == null)
            {
                throw new Exception("Failed to resolve " + sig + "!");
            }
            else
            {
                // myself object was resolved.

                this.MemberType = mtype;
                this.Name = sig;
                this.Object = obj;

                // further resolve my children
                foreach (MethodDefinition mdef in get_children()) // CobberChildren.GetChildren(obj))
                {
                    // parse name
                    CobberMemberTypes t;
                    string sname = GetMemberSig(mdef, out t);

                    // if not exist, create a new one
                    CobberMember method = this.Methods.SingleOrDefault(_ => _.Name == sname && _.MemberType == t);
                    if (method == null)
                    {
                        method = new CobberMember(this);
                        {
                            method.Name = sname;
                            method.MemberType = t;
                            //method.Object = mdef;  // only do shallow resolve for name
                        }

                        this.Methods.Add(method);
                    }
                }
            }
        }

        // Get display name for member
        // identification name for members(method, field, property,event) only
        public static string GetMemberSig(IMemberDefinition member, out CobberMemberTypes type)
        {
            StringBuilder sig = new StringBuilder();
            if (member is MethodReference)
            {
                type = CobberMemberTypes.Method;

                MethodReference method = member as MethodReference;

                // write return type
                if (method.Name != ".ctor" && method.Name != ".cctor") // do not display "Void" for constructors
                {
                    sig.Append(CobberType.GetDisplayName(method.ReturnType));
                    sig.Append(" ");
                }

                // write method name
                string name = method.Name;
                /*
                var genParamsCount = 0;
                if (method.HasGenericParameters)
                {
                    genParamsCount = method.GenericParameters.Count;
                    string str = "`" + genParamsCount.ToString();
                    if (method.Name.EndsWith(str)) name = method.Name.Substring(0, method.Name.Length - str.Length);
                }
                */
                sig.Append(name);

                // followed by generic parameters
                if (method.HasGenericParameters)
                {
                    sig.Append("<");
                    for (int i = 0; i < method.GenericParameters.Count; i++)
                    {
                        if (i != 0) sig.Append(", ");
                        sig.Append(method.GenericParameters[i].Name);
                    }
                    sig.Append(">");
                }

                // write parameters
                sig.Append("(");
                for (int i = 0; i < method.Parameters.Count; i++)
                {
                    if (i != 0) sig.Append(", ");
                    sig.Append(CobberType.GetDisplayName(method.Parameters[i].ParameterType) );
                }
                sig.Append(")");
            }
            else if (member is FieldReference)
            {
                type = CobberMemberTypes.Field;
                FieldReference field = member as FieldReference;

                sig.Append(CobberType.GetDisplayName(field.FieldType));
                sig.Append(" ");
                sig.Append(field.Name);
            }
            else if (member is PropertyDefinition)
            {
                type = CobberMemberTypes.Property;
                PropertyDefinition prop = member as PropertyDefinition;

                sig.Append(CobberType.GetDisplayName(prop.PropertyType));
                sig.Append(" ");
                sig.Append(prop.Name);

                // added by fanghui 20140704
                if (prop.HasParameters)
                {
                    sig.Append("[");
                    for (int i = 0; i < prop.Parameters.Count; i++)
                    {
                        if (i != 0) sig.Append(", ");
                        sig.Append(CobberType.GetDisplayName(prop.Parameters[i].ParameterType));
                    }
                    sig.Append("]");
                }
                // end
            }
            else if (member is EventReference)
            {
                type = CobberMemberTypes.Event;
                EventReference evt = member as EventReference;

                sig.Append(CobberType.GetDisplayName(evt.EventType));
                sig.Append(" ");
                sig.Append(evt.Name);
            }
            else
                throw new NotSupportedException();

            return sig.ToString();
        }

        #region XML save and load
        public override XmlElement Save(XmlDocument xmlDoc)
        {
            XmlElement elem = xmlDoc.CreateElement("member", CobberProject.Namespace);

            XmlAttribute sigAttr = xmlDoc.CreateAttribute("sig");
            sigAttr.Value = Name;
            elem.Attributes.Append(sigAttr);

            XmlAttribute typeAttr = xmlDoc.CreateAttribute("type");
            typeAttr.Value = MemberType.ToString().ToLower();
            elem.Attributes.Append(typeAttr);

            this.SaveObfConfig(xmlDoc, elem);

            foreach (var i in this.Methods)
            {
                if (i.IsTreeInherited()) continue;  // 20140613

                elem.AppendChild(i.Save(xmlDoc)); // members
            }

            return elem;
        }

        public override void Load(XmlElement elem)
        {
            this.Name = elem.Attributes["sig"].Value;
            this.MemberType = (CobberMemberTypes)Enum.Parse(typeof(CobberMemberTypes), elem.Attributes["type"].Value, true);

            LoadObfConfig(elem);

            // go further for its children member
            foreach (XmlElement i in elem.ChildNodes.OfType<XmlElement>())
            {
                CobberMember member = new CobberMember(this);
                member.Load(i);
                this.Methods.Add(member);
            }
        }
        #endregion
    }
}