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
    /// definition for types and nested types
    /// its parent could be either CobberNamespace or CobberType
    /// </summary>
    public class CobberType : CobberObject
    {
        public TypeDefinition Object;

        public List<CobberObject> Members; // children, including both nested types and pure members

        // appended with parent's name
        public string LongName 
        {
            get
            {
                if (Parent is CobberNamespace)
                {
                    if (string.IsNullOrEmpty((Parent as CobberNamespace).Name))
                        return Name;
                    else
                        return Parent.ToString() + "::" + Name;
                }
                else return Parent.ToString() + "." + Name;
            }
        }


        public CobberType(CobberObject parent)
        {
            this.Parent = parent;
            this.Members = new List<CobberObject>();
        }

        public override List<CobberObject> GetChildren()
        {
            List<CobberObject> ret = new List<CobberObject>();
            foreach (var m in Members)
            {
                ret.Add(m);
            }
            return ret;
        }

        private static bool IsDelegate(TypeReference typeRef)
        {
            if (typeRef == null) return false;
            TypeDefinition typeDecl = typeRef.Resolve();
            if (typeDecl == null || typeDecl.BaseType == null) return false;
            return typeDecl.BaseType.Name == "MulticastDelegate" && typeDecl.BaseType.Namespace == "System";
        }

        public override CobberIcon XgetIcon()
        {
            TypeDefinition typeDef = (TypeDefinition)Object;
            if (typeDef.IsInterface)
            {
                return CobberIcon.Interface;
            }
            else if (typeDef.BaseType != null)
            {
                if (typeDef.IsEnum)
                {
                    return CobberIcon.Enum;
                }
                else if (typeDef.IsValueType && !typeDef.IsAbstract)
                {
                    return CobberIcon.Valuetype;
                }
                else if (IsDelegate(typeDef))
                {
                    return CobberIcon.Delegate;
                }
            }

            return CobberIcon.Type;
        }

        public override CobberIconOverlay XgetIconStatic()
        {
            return CobberIconOverlay.None;
        }

        // Get visible overlay for non-public classes
        public override CobberIconVisible XgetIconVisible()
        {
            CobberIconVisible visible = CobberIconVisible.Public; // public

            TypeDefinition typeDef = this.Object;
            switch (typeDef.Attributes & TypeAttributes.VisibilityMask)
            {
                case TypeAttributes.NotPublic:
                case TypeAttributes.NestedAssembly:
                case TypeAttributes.NestedFamANDAssem:
                    visible = CobberIconVisible.Internal;
                    break;
                case TypeAttributes.Public:
                case TypeAttributes.NestedPublic:
                    visible = CobberIconVisible.Public;
                    break;
                case TypeAttributes.NestedPrivate:
                    visible = CobberIconVisible.Private;
                    break;
                case TypeAttributes.NestedFamily:
                    visible = CobberIconVisible.Protected;
                    break;
                case TypeAttributes.NestedFamORAssem:
                    visible = CobberIconVisible.Famasm;
                    break;
            }
            return visible;
        }

        #region Get display name for types shown in a method
        static void WriteTypeReference(StringBuilder sb, TypeReference typeRef, bool isGenericInstance, bool full)
        {
            if (typeRef is TypeSpecification)
            {
                TypeSpecification typeSpec = typeRef as TypeSpecification;
                if (typeSpec is ArrayType)
                {
                    WriteTypeReference(sb, typeSpec.ElementType, full);
                    sb.Append("[");
                    var dims = (typeSpec as ArrayType).Dimensions;
                    for (int i = 0; i < dims.Count; i++)
                    {
                        if (i != 0) sb.Append(", ");
                        if (dims[i].IsSized)
                        {
                            sb.Append(dims[i].LowerBound.HasValue ?
                                            dims[i].LowerBound.ToString() : ".");
                            sb.Append("..");
                            sb.Append(dims[i].UpperBound.HasValue ?
                                            dims[i].UpperBound.ToString() : ".");
                        }
                    }
                    sb.Append("]");
                }
                else if (typeSpec is ByReferenceType)
                {
                    WriteTypeReference(sb, typeSpec.ElementType, full);
                    sb.Append("&");
                }
                else if (typeSpec is PointerType)
                {
                    WriteTypeReference(sb, typeSpec.ElementType, full);
                    sb.Append("*");
                }
                else if (typeSpec is OptionalModifierType)
                {
                    WriteTypeReference(sb, typeSpec.ElementType, full);
                    sb.Append(" ");
                    sb.Append("modopt");
                    sb.Append("(");
                    WriteTypeReference(sb, (typeSpec as OptionalModifierType).ModifierType, full);
                    sb.Append(")");
                }
                else if (typeSpec is RequiredModifierType)
                {
                    WriteTypeReference(sb, typeSpec.ElementType, full);
                    sb.Append(" ");
                    sb.Append("modreq");
                    sb.Append("(");
                    WriteTypeReference(sb, (typeSpec as RequiredModifierType).ModifierType, full);
                    sb.Append(")");
                }
                else if (typeSpec is FunctionPointerType)
                {
                    FunctionPointerType funcPtr = typeSpec as FunctionPointerType;
                    WriteTypeReference(sb, funcPtr.ReturnType, full);
                    sb.Append(" *(");
                    for (int i = 0; i < funcPtr.Parameters.Count; i++)
                    {
                        if (i != 0) sb.Append(", ");
                        WriteTypeReference(sb, funcPtr.Parameters[i].ParameterType, full);
                    }
                    sb.Append(")");
                }
                else if (typeSpec is SentinelType)
                {
                    sb.Append("...");
                }
                else if (typeSpec is GenericInstanceType)
                {
                    WriteTypeReference(sb, typeSpec.ElementType, true);
                    sb.Append("<");
                    var args = (typeSpec as GenericInstanceType).GenericArguments;
                    for (int i = 0; i < args.Count; i++)
                    {
                        if (i != 0) sb.Append(", ");
                        WriteTypeReference(sb, args[i], full);
                    }
                    sb.Append(">");
                }
            }
            else if (typeRef is GenericParameter)
            {
                sb.Append((typeRef as GenericParameter).Name);
            }
            else
            {
                string name = typeRef.Name;
                var genParamsCount = 0;
                if (typeRef.HasGenericParameters)
                {
                    genParamsCount = typeRef.GenericParameters.Count - (typeRef.DeclaringType == null ? 0 : typeRef.DeclaringType.GenericParameters.Count);
                    string str = "`" + genParamsCount.ToString();
                    if (typeRef.Name.EndsWith(str)) name = typeRef.Name.Substring(0, typeRef.Name.Length - str.Length);
                }

                if (typeRef.IsNested)
                {
                    WriteTypeReference(sb, typeRef.DeclaringType, full);
                    sb.Append(".");
                    sb.Append(name);
                }
                else
                {
                    if (full)
                    {
                        sb.Append(typeRef.Namespace);
                        if (!string.IsNullOrEmpty(typeRef.Namespace)) sb.Append(".");
                    }
                    sb.Append(name);
                }
                if (typeRef.HasGenericParameters && genParamsCount != 0 && !isGenericInstance)
                {
                    sb.Append("<");
                    for (int i = typeRef.GenericParameters.Count - genParamsCount; i < typeRef.GenericParameters.Count; i++)
                    {
                        if (i != 0) sb.Append(", ");
                        WriteTypeReference(sb, typeRef.GenericParameters[i], full);
                    }
                    sb.Append(">");
                }
            }
        }

        static void WriteTypeReference(StringBuilder sb, TypeReference typeRef, bool full)
        {
            WriteTypeReference(sb, typeRef, false, full);
        }

        // Get display name for class
        public static string GetDisplayName(TypeReference typeRef)
        {
            StringBuilder ret = new StringBuilder();
            WriteTypeReference(ret, typeRef, false);
            return ret.ToString();
        }
        #endregion

        // get children mono objects, which could be nested types or members
        // required: myself object must be already resolved
        // note: all mono objects have IAnnotationProvider interface. and particularly type+member have IMemberDefinition interface.
        private List<IMemberDefinition> get_children()
        {
            List<IMemberDefinition> children = new List<IMemberDefinition>(); //IAnnotationProvider

            TypeDefinition parent = this.Object;

            //List<IMemberDefinition> children = new List<IMemberDefinition>(
            //    parent.NestedTypes.OfType<IMemberDefinition>().Concat(
            //    parent.Methods.OfType<IMemberDefinition>().Concat(
            //    parent.Fields.OfType<IMemberDefinition>()).Concat(
            //    parent.Properties.OfType<IMemberDefinition>()).Concat(
            //    parent.Events.OfType<IMemberDefinition>()))
            //    );

            // add nested types
            foreach (TypeDefinition nested in from TypeDefinition x in parent.NestedTypes orderby x.Name select x)
            {
                children.Add(nested);
            }

            // add fields
            foreach (FieldDefinition field in from FieldDefinition x in parent.Fields orderby x.Name select x)
            {
                children.Add(field);
            }

            // add methods, FILTERINGg those methods for property and event (as their children instead)
            foreach (MethodDefinition method in from MethodDefinition x in parent.Methods
                                                orderby x.Name
                                                orderby x.Name == ".cctor" ? 0 : (x.Name == ".ctor" ? 1 : 2)
                                                where x.SemanticsAttributes == MethodSemanticsAttributes.None
                                                select x)
            {
                children.Add(method);
            }

            // add properties
            foreach (PropertyDefinition prop in from PropertyDefinition x in parent.Properties orderby x.Name select x)
            {
                children.Add(prop);
            }

            // add events
            foreach (EventDefinition evt in from EventDefinition x in parent.Events orderby x.Name select x)
            {
                children.Add(evt);
            }

            return children;
        }

        public override void Resolve()
        {
            // firstly find object for myself
            TypeDefinition typeDef = null;
            if (this.Object != null)  // already resolved
            {
                typeDef = this.Object;
                this.Name = this.Object.Name;
            }
            else if (this.Parent != null)
            {
                if (this.Parent is CobberNamespace)
                {
                    CobberModule mod = ((this.Parent as CobberNamespace).Parent as CobberModule);
                    typeDef = mod.Object.Types.Single(_ => _.Namespace == (Parent as CobberNamespace).Name && _.Name == this.Name);
                }
                else if (Parent is CobberType)  // me is a nested type
                {
                    typeDef = (Parent as CobberType).Object.NestedTypes.Single(_ => _.Name == this.Name);
                }
            }

            if (typeDef == null)
            {
                throw new Exception("Failed to resolve " + this.ToString() + "!!!");
            }
            else
            {
                this.Object = typeDef;
                this.Name = this.Object.Name;

                // resolve all members in current type
                {
                    List<IMemberDefinition> memDefs = get_children();

                    // create new members
                    foreach (var memdef in memDefs)
                    {
                        if (memdef is TypeDefinition)
                        {
                            // remove duplicated ones that already in current type
                            CobberType subtype = this.Members.SingleOrDefault(_ => (_ is CobberType) && (_.Name == memdef.Name)) as CobberType;
                            if (subtype == null)
                            {
                                subtype = new CobberType(this);
                                {
                                    subtype.Name = (memdef as TypeDefinition).Name;
                                    //subtype.Object = (memdef as TypeDefinition); // only do shallow resolve on children
                                }
                                this.Members.Add(subtype);
                            }
                        }
                        else
                        {
                            CobberMemberTypes t;
                            string sName = CobberMember.GetMemberSig(memdef, out t);
                            CobberMember member = this.Members.SingleOrDefault(_ =>
                                (_ is CobberMember) && (_.Name == sName) && ((_ as CobberMember).MemberType == t)) as CobberMember;
                            if (member == null)
                            {
                                member = new CobberMember(this);  // memdef
                                {
                                    member.Name = sName;
                                    member.MemberType = t;
                                    //member.Object = memdef;  // only parse name of children
                                }

                                this.Members.Add(member);
                            }
                        }
                    }
                }
            }
        }

        #region XML save and load
        public override  XmlElement Save(XmlDocument xmlDoc)
        {
            XmlElement elem = xmlDoc.CreateElement("type", CobberProject.Namespace);

            XmlAttribute fnAttr = xmlDoc.CreateAttribute("name");
            fnAttr.Value = Name;
            elem.Attributes.Append(fnAttr);

            this.SaveObfConfig(xmlDoc, elem);

            foreach (var i in this.Members)
            {
                if (i.IsTreeInherited()) continue; // 20140613

                elem.AppendChild(i.Save(xmlDoc)); // nested types or pure members
            }

            return elem;
        }

        public override void Load(XmlElement elem)
        {
            this.Name = elem.Attributes["name"].Value;

            LoadObfConfig(elem);

            foreach (XmlElement i in elem.ChildNodes.OfType<XmlElement>())
            {
                if (i.Name == "type")  // for nested types
                {
                    CobberType type = new CobberType(this);
                    type.Load(i);
                    this.Members.Add(type);
                }
                else // for child members
                {
                    CobberMember member = new CobberMember(this);
                    member.Load(i);
                    this.Members.Add(member);
                }
            }
        }
        #endregion
    }
}
