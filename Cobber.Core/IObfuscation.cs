using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

using Mono.Cecil;

using Cobber.Core.Project;

namespace Cobber.Core
{
    public enum Priority
    {
        Safe = 1,
        CodeLevel = 2,
        FieldLevel = 3,
        MethodLevel = 4,
        TypeLevel = 5,
        AssemblyLevel = 6,
        MetadataLevel = 7,
        PELevel = 8
    }

    [Flags]
    public enum Target
    {
        Types = 1,
        Methods = 2,
        Fields = 4,
        Events = 8,
        Properties = 16,
        Module = 32,
        All = 63,
    }

    [Flags]
    public enum Behaviour
    {
        Inject = 1,
        AlterCode = 2,
        Encrypt = 4,
        AlterStructure = 8,
    }

    [Flags]
    public enum HelperAttribute
    {
        NoInjection = 1,
        NoAlter = 2,
        NoEncrypt = 4,
    }

    /// <summary>
    /// Interface for each obfuscator like ConstantEncryptObfuscation, ConrolFlowObfuscation, etc.
    /// </summary>
    public interface IObfuscation
    {
        string ID { get; }
        string Name { get; }
        string Description { get; }

        // parameter list with all possible values, and the first value is default
        Dictionary<string, List<string>> Parameters { get; } 

        Phase[] Phases { get; }
        Target Target { get; }
        Preset Preset { get; }
        bool StandardCompatible { get; }
        bool SupportLateAddition { get; }
        Behaviour Behaviour { get; }

        void Init();
        void Deinit();
    }
}
