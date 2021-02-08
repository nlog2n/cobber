using System;
using System.Collections.Generic;

using Mono.Cecil;

namespace Cobber.Core
{
    /// <summary>
    /// Assembly resolver for cobber. 
    /// All assemblies will be resolved from own cache by GetVersionName,
    /// which only takes name and version without strong key public token.
    /// Refer to: DefaultAssemblyResolver class, which uses FullName.
    /// </summary>
    class CobberAssemblyResolver : BaseAssemblyResolver
    {
        public readonly IDictionary<string, AssemblyDefinition> AssemblyCache;

        public CobberAssemblyResolver()
        {
            AssemblyCache = new Dictionary<string, AssemblyDefinition>();
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            AssemblyDefinition assembly;
            if (AssemblyCache.TryGetValue(name.GetVersionName(), out assembly))
                return assembly;

            assembly = base.Resolve(name);
            if (assembly != null)
            {
                AssemblyCache[name.GetVersionName()] = assembly;
            }
            else
            {
                throw new Exception("Cannot resolve '" + name.FullName + "'!");
            }

            return assembly;
        }

        public void RegisterAssembly(AssemblyDefinition assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            var name = assembly.GetVersionName();
            if (AssemblyCache.ContainsKey(name))
                return;

            AssemblyCache[name] = assembly;
        }

    }
}
