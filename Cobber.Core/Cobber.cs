using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Metadata;
using Mono.Cecil.Rocks;

using Cobber.Core.Project;

namespace Cobber.Core
{
    public class Cobber
    {
        // simply read-in, and were used later in setting marker
        public static Dictionary<string, IObfuscation> Obfuscators = new Dictionary<string,IObfuscation>(); // key = obfuscator's ID
        public static Dictionary<string, Packer> Packers = new Dictionary<string,Packer>(); // key = packer's ID

        public CobberProject Project; // a copy of input project for obfuscation
        internal System.Reflection.StrongNameKeyPair SNKey;

        // will set new resolver for cobber
        CobberAssemblyResolver Resolver { get { return (CobberAssemblyResolver)GlobalAssemblyResolver.Instance; } }
 
        // for packing/compression
        internal MetadataProcessor.MetadataProcess PostProcessMetadata = null;
        internal MetadataProcessor.ImageProcess PostProcessImage = null;

        List<Phase> Phases;
        internal List<Analyzer> analyzers;

        int injectionCount; // the count of injected objects

        public NameHelper Namer; // helper for renaming
        public Random Random { get { return this.Namer.Random; } }

        public CobberDatabase Database;
        public Logger Logger = new Logger(); // log messages
        public void Log(string message) { Logger._Log(message); }

        static Cobber()
        {
            // load obfuscators from myself assembly
            LoadAssembly(typeof(Cobber).Assembly);

            // create default presettings for like normal, aggressive etc
            CreatePresetSettings();
        }


        // prerequisite:  project already marked!
        void Initialize()
        {
            this.SNKey = null;
            this.Phases = new List<Phase>();
            this.analyzers = new List<Analyzer>();
            this.injectionCount = 0;
            this.Database = new CobberDatabase();
            //this.Logger = new Logger();

            this.Namer = new NameHelper(this.Project.Seed, this.Database); // get random seed and create a name helper

            Database.Module("Global");
            var t = DateTime.Now;
            Database.AddEntry("Project", "Start", t);

            this.Logger._Phase("Initializing...");
            Log("Started at " + t.ToShortTimeString() + ".");

            Database.AddEntry("Project", "Seed", Namer.seed);
            Database.AddEntry("Project", "Debug", Project.Debug);

            ObfuscationSetting setting = ObfuscationSetting.GetSetting(this.Project.ObfSettingName); // any default presetting?
            string setting_name = (setting != null ? setting.Name : null);
            this.Project.Mark(setting_name);
            //bool applyToMembers = true; // supposed to apply to members

            // now all mono objects should be resolved.
            // and then register assemblies in resolver's cache for later use.
            AddResolverSearchDirectories();

            if (Project.Debug) // read symbols for later generating debug symbols
            {
                var provider = new Mono.Cecil.Pdb.PdbReaderProvider();
                Log(string.Format("Loading debug symbols..."));
                for (int i = 0; i < this.Project.Assemblies.Count; i++)
                {
                    foreach (var mod in this.Project.Assemblies[i].Object.Modules)
                    {
                        try
                        {
                            mod.ReadSymbols(provider.GetSymbolReader(mod, mod.FullyQualifiedName));
                        }
                        catch { }
                    }
                    //this.Logger._Progress(i + 1, this.Project.Assemblies.Count);
                }
            }


            // adjust module's references to point inside the project assemblies
            foreach (var asm in this.Project.Assemblies)
            {
                foreach (var mod in asm.Object.Modules)
                {
                    for (int i = 0; i < mod.AssemblyReferences.Count; i++)
                    {
                        AssemblyNameReference nameRef = mod.AssemblyReferences[i];
                        foreach (var asmRef in this.Project.Assemblies)
                        {
                            if (asmRef.Object.Name.Name == nameRef.Name)
                            {
                                nameRef = asmRef.Object.Name;
                                break;
                            }
                        }

                        mod.AssemblyReferences[i] = nameRef;
                    }
                }
            }


            // get new strong name
            SNKey = GetSNKey(Project.SNKeyPath, Project.SNKeyPassword);
            if ( SNKey == null )
            {
                Logger._Warn("Strong name key not found. Output assembly will not be signed.");
            }

            // remove previous sn tokens
            Dictionary<string, string> key_replacement = new Dictionary<string, string>(); // item1= original, item2 = replacement
            foreach (var i in this.Project.Assemblies)
            {
                AssemblyDefinition asm = i.Object;
                string o1 = ToString(asm.Name.PublicKeyToken);
                string o2 = ToString(asm.Name.PublicKey);
                if (SNKey != null)
                {
                    asm.Name.PublicKey = SNKey.PublicKey;
                    asm.MainModule.Attributes |= ModuleAttributes.StrongNameSigned;
                }
                else
                {
                    asm.Name.PublicKey = null;
                    asm.MainModule.Attributes &= ~ModuleAttributes.StrongNameSigned;
                }
                string n1 = ToString(asm.Name.PublicKeyToken);
                string n2 = ToString(asm.Name.PublicKey);
                if (o1 != n1 && !key_replacement.ContainsKey(o1)) { key_replacement.Add(o1, n1); }
                if (o2 != n2 && !key_replacement.ContainsKey(o2)) { key_replacement.Add(o2, n2); }
            }

            foreach (var item in key_replacement)
            {
                foreach (var asm in this.Project.Assemblies)
                {
                    UpdateCustomAttributeRef(asm.Object);
                    foreach (var mod in asm.Object.Modules)
                    {
                        UpdateCustomAttributeRef(mod);
                        foreach (var type in mod.Types)
                        {
                            UpdateAssemblyReference(type, item.Key, item.Value);
                        }
                    }
                }
            }
        }


        public void Process(CobberProject proj)
        {
            // save previous instance for GlobalAssemblyResolver
            var prevAsmResolver = GlobalAssemblyResolver.Instance; // save previous GlobalAssemblyResolver.Instance for recover
            GlobalAssemblyResolver.Instance = new CobberAssemblyResolver(); // use my assembly resolver for cobber instead

            // save this project temporarily a copy for modification
            XmlDocument xmlDoc = proj.Save();
            this.Project = new CobberProject();
            this.Project.Name = proj.Name;
            this.Project.Load(xmlDoc);

            try
            {
                Initialize();

                this.Logger._Progress(10, 100);
                this.Logger._Phase("Create static constructors...");
                foreach (var asm in this.Project.Assemblies)
                {
                    Log(string.Format("scan assembly {0}", asm.Object.FullName));

                    foreach (var mod in asm.Object.Modules)
                    {
                        // do not support multiple cobber obfuscations on same assembly
                        if (mod.GetType("CobberedByAttribute") != null)
                            throw new Exception("'" + mod.Name + "' was already obfuscated by Cobber!");

                        //global cctor which used in many obfuscators
                        MethodDefinition cctor = GetStaticConstructor(mod);
                        if (cctor == null)
                        {
                            cctor = CreateStaticConstructor(mod); // add cctor
                            Log("> create static constructor for module " + mod.Name);
                        }
                        else
                        {
                            ((IAnnotationProvider)cctor).Annotations.Clear(); // clear original annotations for cctor
                        }
                    }
                }

                this.Logger._Progress(20, 100);
                Log("Simplifying methods...");
                foreach (var type in this.Project.Assemblies.SelectMany(_ => _.Modules).SelectMany(_ => _.Object.GetAllTypes()))
                {
                    foreach (var method in type.Methods)
                    {
                        if (!method.HasBody) continue;

                        // simplify branch.short to br to avoid instruction overflow ( when after injecting instructions)
                        method.Body.SimplifyMacros();
                    }
                }


                this.Logger._Progress(30, 100);
                this.Logger._Phase("Analyzing names...");
                // get phases and name analyzers
                foreach (IObfuscation obf in Cobber.Obfuscators.Values)
                {
                    foreach (Phase phase in obf.Phases)
                    {
                        Phases.Add(phase);

                        Analyzer analyzer = phase.GetAnalyzer(); // sofar only renaming obfuscator has name analyzer.
                        if (analyzer != null)
                        {
                            analyzer.SetCobber(this);
                            analyzer.SetProgresser(this.Logger);

                            analyzers.Add(analyzer);

                            Log(string.Format("Analyzing {0}...", obf.ID));
                            analyzer.Analyze(this.Project.Assemblies.Select(_ => _.Object));
                        }
                    }
                }

                this.Logger._Progress(40, 100);
                this.Logger._Phase("Processing structural phases...");
                foreach ( CobberAssembly asm in this.Project.Assemblies)
                {
                    using (this.Logger._Assembly(asm))
                    {
                        foreach (CobberModule mod in asm.Modules)
                        {
                            if (mod.Injected)
                            {
                                Logger._Warn("do not apply on injected object " + mod);
                                continue;
                            }

                            //global cctor which used in many obfuscators
                            MethodDefinition cctor = GetStaticConstructor(mod.Object);
                            if (cctor != null)
                            {
                                // bug: for existing cctor, no need to inject except mark as no_encrypt
                                InjectObject(mod.Object.GetType("<Module>"), cctor, HelperAttribute.NoEncrypt);
                            }

                            ProcessStructuralPhases(mod, Phases);
                        }
                    }
                }
                Log(injectionCount.ToString() + " new objects injected.");

                this.Logger._Progress(50, 100);
                this.Logger._Phase("Optimizing methods...");
                // get all types and all methods
                foreach (var type in this.Project.Assemblies.SelectMany(_ => _.Modules).SelectMany(_ => _.Object.GetAllTypes()))
                {
                    foreach (var method in type.Methods)
                    {
                        if (!method.HasBody) continue;

                        method.Body.SimplifyMacros();
                        method.Body.OptimizeMacros(); // recover branch macros
                        method.Body.ComputeOffsets(); // update branch offset after modifying instructions
                    }
                }

                this.Logger._Progress(60, 100);
                this.Logger._Phase("Processing metadata pe phases...");
                var provider = new Mono.Cecil.Pdb.PdbWriterProvider();
                List<byte[]> pes = new List<byte[]>();
                List<byte[]> syms = new List<byte[]>();
                List<ModuleDefinition> mods = new List<ModuleDefinition>();
                foreach (CobberAssembly asm in this.Project.Assemblies)
                {
                    using (this.Logger._Assembly(asm))
                    {
                        foreach (var mod in asm.Modules)
                        {
                            MemoryStream final = new MemoryStream();
                            MemoryStream symbol = new MemoryStream();

                            WriterParameters writerParams = new WriterParameters();
                            if ((mod.Object.Attributes & ModuleAttributes.StrongNameSigned) != 0)
                            {
                                writerParams.StrongNameKeyPair = SNKey;
                            }
                            else
                            {
                                writerParams.StrongNameKeyPair = null;
                            }

                            if (Project.Debug) // write debug symbols
                            {
                                writerParams.WriteSymbols = true;
                                writerParams.SymbolWriterProvider = provider;
                                writerParams.SymbolStream = symbol;
                            }

                            Database.Module(Path.GetFileName(mod.Object.FullyQualifiedName));

                            ProcessMdPePhases(mod, Phases, final, writerParams); // process metadata

                            // output
                            pes.Add(final.ToArray());
                            syms.Add(symbol.ToArray());
                            mods.Add(mod.Object);
                        }
                    }
                }

                this.Logger._Progress(70, 100);
                this.Logger._Phase("Finalizing...");
                Finalize(mods.ToArray(), pes.ToArray(), syms.ToArray());

                this.Logger._Progress(100, 100);
            }
            catch (Exception ex)
            {
                this.Logger._Fatal(ex);
            }
            finally
            {
                // One cobber obfuscation operation will generated the following new stuff:
                //    injected objects,
                //    annotations for some objects
                //    new-added obfuscation settings
                // This function reload project for preparing next obfuscation operation

                // Settings: remove internal obfuscation settings
                List<string> setting_names = ObfuscationSetting.ObfSettings.Keys.ToList();
                foreach (var setting_name in setting_names)
                {
                    ObfuscationSetting setting = ObfuscationSetting.ObfSettings[setting_name];
                    if (setting.IsInternal)
                    {
                        ObfuscationSetting.ObfSettings.Remove(setting_name);
                    }
                }

                GlobalAssemblyResolver.Instance = prevAsmResolver; // recover previous resolver

                GC.Collect(); 
            }
        }

        // mods: mono module objects
        // pes: a list of modified assemblies in byte arrays
        // syms: debug symbols
        void Finalize(ModuleDefinition[] mods, byte[][] pes, byte[][] syms)
        {
            Database.Module("Global");

            // get output directory, create it if null
            string output = Project.OutputPath;
            if (string.IsNullOrEmpty(output))
            {
                CobberAssembly asm = Project.GetMainAssembly();
                if (asm == null)
                {
                    asm = Project.Assemblies[0];
                }
                output = Path.Combine(Path.GetDirectoryName(asm.Name), "Obf");
            }
            if (!string.IsNullOrEmpty(Project.BasePath))
            {
                output = Path.Combine(Project.BasePath, output);
            }
            if (!Directory.Exists(output))
            {
                Directory.CreateDirectory(output);
            }


            if (!string.IsNullOrEmpty(this.Project.PackerID))  // with packing
            {
                Packer packer = Cobber.Packers[this.Project.PackerID];

                Log("Packing output assemblies...");
                packer.cobber = this;
                PackerSetting pParam = new PackerSetting();
                {
                    pParam.Assemblies = this.Project.Assemblies.ToArray();
                    pParam.Modules = mods;
                    pParam.PEs = pes;
                    pParam.Parameters = this.Project.PackerParameters;
                }
                string[] final = packer.Pack(pParam);

                for (int i = 0; i < final.Length; i++)
                {
                    string path = Path.Combine(output, Path.GetFileName(final[i]));
                    if (File.Exists(path)) { File.Delete(path); }

                    File.Move(final[i], path);
                }
            }
            else // no packing
            {
                Log("Writing outputs...");
                for (int i = 0; i < pes.Length; i++)
                {
                    string filename = Path.GetFileName(mods[i].FullyQualifiedName);
                    if (string.IsNullOrEmpty(filename)) { filename = mods[i].Name; }

                    string dest = Path.Combine(output, filename);
                    if (!Directory.Exists(Path.GetDirectoryName(dest)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    }
                    Stream dstStream = new FileStream(dest, FileMode.Create, FileAccess.Write);
                    try
                    {
                        dstStream.Write(pes[i], 0, pes[i].Length);
                    }
                    finally
                    {
                        dstStream.Dispose();
                    }
                }
            }

            if (Project.Debug)
            {
                Log("Writing symbols...");
                string ext = "pdb"; // Type.GetType("Mono.Runtime") != null ? "mdb" : "pdb"; // support both Pdb and Mdb
                for (int i = 0; i < mods.Length; i++)
                {
                    File.WriteAllBytes(Path.Combine(output, Path.ChangeExtension(mods[i].Name, ext)), syms[i]);
                }
            }

            var t = DateTime.Now;
            Database.AddEntry("Project", "End", t);
            this.Logger._Finish("Ended at " + t.ToShortTimeString() + ".");

            //Ya, finally done it. Now save the map into db file
            using (BinaryWriter wtr = new BinaryWriter(File.OpenWrite( Path.Combine(output, "cobber.map"))))
            {
                Database.Serialize(wtr);
            }
        }

        public static List<string> LoadAssembly(System.Reflection.Assembly asm)
        {
            List<string> added = new List<string>();

            foreach (Type t in asm.GetTypes())
            {
                if (typeof(IObfuscation).IsAssignableFrom(t) && t != typeof(IObfuscation))
                {
                    // create an obfuscator object and add in
                    IObfuscation obf = Activator.CreateInstance(t) as IObfuscation;
                    Cobber.Obfuscators[obf.ID] = obf;

                    added.Add("obfuscator/" + obf.ID);
                }
                if (typeof(Packer).IsAssignableFrom(t) && t != typeof(Packer))
                {
                    Packer p = Activator.CreateInstance(t) as Packer;
                    Cobber.Packers[p.ID] = p;

                    added.Add("packer/" + p.ID);
                }
            }

            return added;
        }

        // create those preset settings
        internal static void CreatePresetSettings()
        {
            foreach (Preset preset in Enum.GetValues(typeof(Preset)))
            {
                ObfuscationSetting setting = new ObfuscationSetting();
                setting.Name = preset.ToString().ToLower(); // rename to default

                foreach (IObfuscation obf in Cobber.Obfuscators.Values)
                {
                    if (preset >= obf.Preset) // included this obf
                    {
                        NameValueCollection param = new NameValueCollection();
                        foreach (string k in obf.Parameters.Keys)
                        {
                            param[k] = obf.Parameters[k][0]; // default value
                        }

                        setting.ObfParameters.Add(obf.ID, param); 
                    }
                }

                ObfuscationSetting.AddSetting(setting); // save
            }
        }



        void AddResolverSearchDirectories()
        {
            // register assembly objects, so later works on our own assembly caches.
            foreach (var i in this.Project.Assemblies)
            {
                this.Resolver.RegisterAssembly(i.Object);
            }

            var mainAsm = this.Project.GetMainAssembly();
            if (!string.IsNullOrEmpty(this.Project.PackerID) && mainAsm == null )
            {
                Logger._Warn("cannot pack a library or net module! set to no packing.");
                this.Project.PackerID = null;
            }

            if (!string.IsNullOrEmpty(this.Project.PackerID) && Project.Debug)
            {
                Logger._Warn("with packing, the debug symbols may not be loaded properly into debugger!");
            }

            // add detection for silverlight
            if (BitConverter.ToUInt64(
                ( mainAsm != null ? mainAsm.Object : this.Project.Assemblies[0].Object).MainModule.AssemblyReferences.First(
                    _ => _.Name == "mscorlib").PublicKeyToken, 0) == 0x8e79a7bed785ec7c)
            {
                Log("Silverlight assemblies!");
                var dir = Environment.ExpandEnvironmentVariables("%ProgramFiles%\\Microsoft Silverlight");
                if (Directory.Exists(dir))
                {
                    //Log("Silverlight Path detected!");
                    foreach (var i in Directory.GetDirectories(dir))
                    {
                        //resolver.AddSearchDirectory(i);
                    }
                }
                else
                    throw new Exception("Could not detect Silverlight installation path!");
            }


            // add searching directories
            /*
            HashSet<string> dirs = new HashSet<string>();
            if (Project.BasePath != null)
            {
                dirs.Add(Project.BasePath);
            }
            foreach (var asm in this.Project.Assemblies)
            {
                string path = Path.GetDirectoryName(asm.Object.MainModule.FullyQualifiedName);
                if (dirs.Add(path))
                {
                    foreach (var j in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
                    {
                        dirs.Add(j);
                    }
                }
            }
            foreach (var i in dirs)
            {
                resolver.AddSearchDirectory(i);
            }
            */
        }


        void UpdateCustomAttributeRef(ICustomAttributeProvider ca)
        {
            if (!ca.HasCustomAttributes) return;
            foreach (var i in ca.CustomAttributes)
            {
                foreach (var arg in i.ConstructorArguments)
                {
                    UpdateCustomAttributeArgs(arg);
                }
                foreach (var arg in i.Fields)
                {
                    UpdateCustomAttributeArgs(arg.Argument);
                }
                foreach (var arg in i.Properties)
                {
                    UpdateCustomAttributeArgs(arg.Argument);
                }
            }
        }

        // called by: UpdateCustomAttributeRef and itself only
        void UpdateCustomAttributeArgs(CustomAttributeArgument arg)
        {
            if (arg.Value is TypeReference)
            {
                TypeReference typeRef = arg.Value as TypeReference;
                if (typeRef.Scope is AssemblyNameReference)
                {
                    AssemblyNameReference nameRef = typeRef.Scope as AssemblyNameReference;
                    foreach (var i in this.Project.Assemblies)
                    {
                        if (i.Object.Name.Name == nameRef.Name)
                        {
                            typeRef.Scope = i.Object.Name;
                        }
                    }
                }
            }
            else if (arg.Value is CustomAttributeArgument[])
            {
                foreach (var i in arg.Value as CustomAttributeArgument[])
                {
                    UpdateCustomAttributeArgs(i);
                }
            }
        }

        void UpdateAssemblyReference(TypeDefinition typeDef, string from, string to)
        {
            UpdateCustomAttributeRef(typeDef);

            if (typeDef.HasGenericParameters)
            {
                foreach (var p in typeDef.GenericParameters) { UpdateCustomAttributeRef(p); }
            }

            foreach (var i in typeDef.Methods)
            {
                if (i.HasParameters)
                {
                    foreach (var p in i.Parameters) { UpdateCustomAttributeRef(p); }
                }
                if (i.HasGenericParameters)
                {
                    foreach (var p in i.GenericParameters) { UpdateCustomAttributeRef(p); }
                }
                UpdateCustomAttributeRef(i.MethodReturnType);
                UpdateCustomAttributeRef(i);
            }
            foreach (var i in typeDef.Fields) { UpdateCustomAttributeRef(i); }
            foreach (var i in typeDef.Properties) { UpdateCustomAttributeRef(i); }
            foreach (var i in typeDef.Events) { UpdateCustomAttributeRef(i); }

            foreach (var i in typeDef.NestedTypes)
            {
                UpdateAssemblyReference(i, from, to); // recursive
            }

            foreach (var i in typeDef.Methods)
            {
                if (!i.HasBody) continue;
                foreach (var inst in i.Body.Instructions)
                {
                    if (inst.Operand is string)
                    {
                        string op = (string)inst.Operand;
                        if (op.Contains(from))
                            op = op.Replace(from, to);
                        inst.Operand = op;
                    }
                }
            }
        }
        
        string ToString(byte[] arr)
        {
            if (arr == null || arr.Length == 0) return "null";
            return BitConverter.ToString(arr).Replace("-", "").ToLower();
        }

        // password is required for .pfx file
        static System.Reflection.StrongNameKeyPair GetSNKey(string filepath, string password)
        {
            if (string.IsNullOrEmpty(filepath)) return null;
            if (!File.Exists(filepath)) return null;

            if (filepath.Contains(".pfx|"))
            {
                //http://stackoverflow.com/questions/7556846/how-to-use-strongnamekeypair-with-a-password-protected-keyfile-pfx

                //string fileName = path.Substring(0, path.IndexOf(".pfx|") + 4);
                //string password = path.Substring(path.IndexOf(".pfx|") + 5);

                X509Certificate2Collection certs = new X509Certificate2Collection();
                certs.Import(filepath, password, X509KeyStorageFlags.Exportable);
                if (certs.Count == 0)
                    throw new ArgumentException(null, "pfx file");

                RSACryptoServiceProvider provider = certs[0].PrivateKey as RSACryptoServiceProvider;
                if (provider == null) // not a good pfx file
                    throw new ArgumentException(null, "pfx file");

                return new System.Reflection.StrongNameKeyPair(provider.ExportCspBlob(true));
            }
            else // snk file
            {
                return new System.Reflection.StrongNameKeyPair(new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read));
            }
        } 

        void ProcessStructuralPhases(CobberModule mod, IEnumerable<Phase> phases)
        {
            Log(string.Format("Obfuscating structure of module {0}...", mod.Object.Name));
            Database.Module(Path.GetFileName(mod.Object.FullyQualifiedName));

            if (!string.IsNullOrEmpty(this.Project.PackerID))
            {
                Packer packer = Cobber.Packers[this.Project.PackerID];
                packer.ProcessModulePhase1(
                    mod.Object,
                    mod.Object.IsMain && this.Project.GetMainAssembly().Object == mod.Object.Assembly
                    );
            }

            bool end1 = false;
            foreach (StructurePhase sph in from i in phases 
                                           where (i is StructurePhase) 
                                           orderby (int)i.Priority + i.PhaseID * 10 ascending 
                                           select i)
            {
                if (!end1 && sph.PhaseID > 1)
                {
                    InjectCustomAttribute(mod);
                    CecilHelper.RefreshTokens(mod.Object);
                    end1 = true;
                }

                var mems = GetTargets(mod, sph.Obfuscator);
                if (mems.Count == 0) continue;

                sph.cobber = this;
                Log("> Executing " + sph.Obfuscator.Name + " Phase " + sph.PhaseID + "...");

                sph.Initialize(mod.Object);

                // choose parameters
                ObfuscationSetting setting = ObfuscationSetting.GetSetting(mod.ObfSettingName);
                NameValueCollection param = new NameValueCollection();
                if (setting.ObfParameters.ContainsKey(sph.Obfuscator.ID))
                {
                    param.Add(setting.ObfParameters[sph.Obfuscator.ID]);
                }

                if (sph.WholeRun == true)
                {
                    sph.Process(null, param);
                }
                else if (sph is IProgressProvider)
                {
                    (sph as IProgressProvider).SetProgresser(this.Logger);
                    sph.Process(mems, param);
                }
                else
                {
                    foreach (var mem in mems)
                    {
                        sph.Process(mem.Item1, mem.Item2); // each member has its parameters corresponding to the phase obfuscator
                    }
                }

                sph.DeInitialize();
            }

            if (!string.IsNullOrEmpty(this.Project.PackerID))
            {
                Packer packer = Cobber.Packers[this.Project.PackerID];
                packer.ProcessModulePhase3(
                    mod.Object,
                    mod.Object.IsMain && this.Project.GetMainAssembly().Object == mod.Object.Assembly
                    );
            }
        }

        void ProcessMdPePhases(CobberModule mod, IEnumerable<Phase> phases, Stream stream, WriterParameters parameters)
        {
            ObfuscationSetting setting = ObfuscationSetting.GetSetting(mod.ObfSettingName);

            MetadataProcessor psr = new MetadataProcessor();
            int total1 = (from i in phases where (i is MetadataPhase) select i).Count();
            psr.BeforeBuildModule += new MetadataProcessor.MetadataProcess(delegate(MetadataProcessor.MetadataAccessor accessor)
            {
                foreach (MetadataPhase i in from i in phases where (i is MetadataPhase) && i.PhaseID == 1 orderby i.Priority ascending select i)
                {
                    if (GetTargets(mod, i.Obfuscator).Count == 0) continue;
                    Log("> Executing " + i.Obfuscator.Name + " Phase 1...");
                    i.cobber = this;
                    NameValueCollection param;
                    if (setting.ObfParameters.ContainsKey(i.Obfuscator.ID))
                        param = setting.ObfParameters[i.Obfuscator.ID];
                    else
                        param = new NameValueCollection();
                    i.Process(param, accessor);
                }

                if (!string.IsNullOrEmpty(this.Project.PackerID))
                {
                    Packer packer = Cobber.Packers[this.Project.PackerID];
                    packer.ProcessMetadataPhase1(accessor,
                        mod.Object.IsMain && this.Project.GetMainAssembly().Object == mod.Object.Assembly);
                }
            });
            psr.BeforeWriteTables += new MetadataProcessor.MetadataProcess(delegate(MetadataProcessor.MetadataAccessor accessor)
            {
                foreach (MetadataPhase i in from i in phases where (i is MetadataPhase) && i.PhaseID == 2 orderby i.Priority ascending select i)
                {
                    if (GetTargets(mod, i.Obfuscator).Count == 0) continue;
                    Log("> Executing " + i.Obfuscator.Name + " Phase 2...");
                    i.cobber = this;
                    NameValueCollection param;
                    if (setting.ObfParameters.ContainsKey(i.Obfuscator.ID))
                        param = setting.ObfParameters[i.Obfuscator.ID];
                    else
                        param = new NameValueCollection();
                    i.Process(param, accessor);
                }

                if (!string.IsNullOrEmpty(this.Project.PackerID))
                {
                    Packer packer = Cobber.Packers[this.Project.PackerID];
                    packer.ProcessMetadataPhase2(accessor,
                        mod.Object.IsMain && this.Project.GetMainAssembly().Object == mod.Object.Assembly);
                }
                if (this.PostProcessMetadata != null)
                {
                    this.PostProcessMetadata(accessor);
                }
            });
            psr.AfterWriteTables += new MetadataProcessor.MetadataProcess(delegate(MetadataProcessor.MetadataAccessor accessor)
            {
                foreach (MetadataPhase i in from i in phases where (i is MetadataPhase) && i.PhaseID == 3 orderby i.Priority ascending select i)
                {
                    if (GetTargets(mod, i.Obfuscator).Count == 0) continue;
                    Log("> Executing " + i.Obfuscator.Name + " Phase 3...");
                    i.cobber = this;
                    NameValueCollection param;
                    if (setting.ObfParameters.ContainsKey(i.Obfuscator.ID))
                        param = setting.ObfParameters[i.Obfuscator.ID];
                    else
                        param = new NameValueCollection();
                    i.Process(param, accessor);
                }
            });
            psr.ProcessImage += new MetadataProcessor.ImageProcess(delegate(MetadataProcessor.ImageAccessor accessor)
            {
                Log(string.Format("Obfuscating Image of module {0}...", mod.Object.Name));

                if (!string.IsNullOrEmpty(this.Project.PackerID))
                {
                    Packer packer = Cobber.Packers[this.Project.PackerID];
                    packer.ProcessImage(accessor,
                        mod.Object.IsMain && this.Project.GetMainAssembly().Object == mod.Object.Assembly);
                }
                if (this.PostProcessImage != null)
                {
                    this.PostProcessImage(accessor);
                }

                ImagePhase[] imgPhases = (from i in phases where (i is ImagePhase) orderby (int)i.Priority + i.PhaseID * 10 ascending select (ImagePhase)i).ToArray();
                for (int i = 0; i < imgPhases.Length; i++)
                {
                    if (GetTargets(mod, imgPhases[i].Obfuscator).Count == 0) continue;
                    Log("> Executing " + imgPhases[i].Obfuscator.Name + " Phase " + imgPhases[i].PhaseID + "...");
                    imgPhases[i].cobber = this;
                    NameValueCollection param;
                    if (setting.ObfParameters.ContainsKey(imgPhases[i].Obfuscator.ID))
                        param = setting.ObfParameters[imgPhases[i].Obfuscator.ID];
                    else
                        param = new NameValueCollection();
                    imgPhases[i].Process(param, accessor);
                }
            });
            psr.ProcessPe += new MetadataProcessor.PeProcess(delegate(Stream str, MetadataProcessor.ImageAccessor accessor)
            {
                Log(string.Format("Obfuscating PE of module {0}...", mod.Object.Name));
                PePhase[] pePhases = (from i in phases where (i is PePhase) orderby (int)i.Priority + i.PhaseID * 10 ascending select (PePhase)i).ToArray();
                for (int i = 0; i < pePhases.Length; i++)
                {
                    if (GetTargets(mod, pePhases[i].Obfuscator).Count == 0) continue;
                    Log("> Executing " + pePhases[i].Obfuscator.Name + " Phase " + pePhases[i].PhaseID + "...");
                    pePhases[i].cobber = this;
                    NameValueCollection param;
                    if (setting.ObfParameters.ContainsKey(pePhases[i].Obfuscator.ID))
                        param = setting.ObfParameters[pePhases[i].Obfuscator.ID];
                    else
                        param = new NameValueCollection();
                    pePhases[i].Process(param, str, accessor);
                }
            });

            Log(string.Format("Obfuscating metadata of module {0}...", mod.Object.Name));
            psr.Process(mod.Object, stream, parameters);
        }


        // search in the module for all objects that contain the given obfuscation, and return parameters.
        List<Tuple<IAnnotationProvider, NameValueCollection>> GetTargets(CobberModule mod, IObfuscation obf)
        {
            List<Tuple<CobberObject, NameValueCollection>> result = GetObfTargets(mod, obf);

            List<Tuple<IAnnotationProvider, NameValueCollection>> mems = new List<Tuple<IAnnotationProvider, NameValueCollection>>();

            foreach (var one in result)
            {
                CobberObject cobj = one.Item1;
                NameValueCollection param = one.Item2;

                //if (cobj.Injected) continue;

                IAnnotationProvider obj = null;
                if (cobj is CobberAssembly) { obj = (cobj as CobberAssembly).Object; }
                else if (cobj is CobberModule) { obj = (cobj as CobberModule).Object; }
                else if (cobj is CobberType) { obj = (cobj as CobberType).Object; }
                else if (cobj is CobberMember) { obj = (cobj as CobberMember).Object; }
                else { continue; }

                mems.Add(new Tuple<IAnnotationProvider, NameValueCollection>(obj, param));
            }

            return mems;
        }


        // get targets filtered by obfuscation id
        List<Tuple<CobberObject, NameValueCollection>> GetObfTargets(CobberModule mod, IObfuscation obf)
        {
            List<Tuple<CobberObject, NameValueCollection>> result = new List<Tuple<CobberObject, NameValueCollection>>();

            // test myself
            if ( (obf.Target & Target.Module) != 0 )
            {
                ObfuscationSetting setting = ObfuscationSetting.GetSetting(mod.ObfSettingName);
                if (setting.ObfParameters.ContainsKey(obf.ID) )
                {
                    result.Add(new Tuple<CobberObject, NameValueCollection>(mod, setting.ObfParameters[obf.ID]));
                }
            }

            // test my children. note that namespace is not mono object
            foreach (CobberType type in mod.Namespaces.SelectMany(_ => _.Types))
            {
                GetObfTargets(type, result, obf); 
            }
            
            return result;
        }


        // get targets and their obfuscatin parameters
        // called by GetTargets(mod,def) above only, and itself is recursive
        void GetObfTargets(CobberObject member, List<Tuple<CobberObject, NameValueCollection>> result, IObfuscation obf)
        {
            if (member is CobberType)
            {
                // test this type
                ObfuscationSetting setting = ObfuscationSetting.GetSetting(member.ObfSettingName);
                if (setting.ObfParameters.ContainsKey(obf.ID))
                {
                    NameValueCollection param = setting.ObfParameters[obf.ID];
                    if ((obf.Target & Target.Types) != 0)
                    {
                        result.Add(new Tuple<CobberObject, NameValueCollection>(member, param));
                    }
                }

                // test children
                foreach (var submember in ((CobberType)member).Members)
                {
                    GetObfTargets(submember, result, obf);
                }
            }
            else // it is member
            {
                // test this member
                ObfuscationSetting setting = ObfuscationSetting.GetSetting(member.ObfSettingName);
                if (setting.ObfParameters.ContainsKey(obf.ID))
                {
                    NameValueCollection param = setting.ObfParameters[obf.ID];
                    IMemberDefinition obj = ((CobberMember)member).Object;
                    if (   (obj is MethodDefinition   && (obf.Target & Target.Methods) != 0)
                        || (obj is FieldDefinition    && (obf.Target & Target.Fields) != 0)
                        || (obj is PropertyDefinition && (obf.Target & Target.Properties) != 0)
                        || (obj is EventDefinition    && (obf.Target & Target.Events) != 0)
                        )
                    {
                        result.Add(new Tuple<CobberObject, NameValueCollection>(member, param));
                    }
                }

                // test children
                foreach (var submember in ((CobberMember)member).Methods)
                {
                    GetObfTargets(submember, result, obf);
                }
            }
        }


        // called by: InjectObject() only
        CobberObject FindCobberObject(object obj)
        {
            foreach (CobberAssembly asm in this.Project.Assemblies)
            {
                if (obj is AssemblyDefinition)
                {
                    if (asm.Object == obj)  return asm;
                }
                else
                {
                    foreach (CobberModule mod in asm.Modules)
                    {
                        if (obj is ModuleDefinition)
                        {
                            if (mod.Object == obj) return mod;
                        }
                        else
                        {
                            foreach (CobberNamespace ns in mod.Namespaces)
                            {
                                foreach (CobberType type in ns.Types)
                                {
                                    var result = FindCobberObject(type, obj);
                                    if (result != null) return result;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        // obj is a mono object, either TypeDefinition or IMemberDefinition
        CobberObject FindCobberObject(CobberType rootType, object obj)
        {
            if (rootType.Object == obj) return rootType;

            foreach (CobberObject member in rootType.Members)
            {
                if (member is CobberType)
                {
                    var result = FindCobberObject((member as CobberType), obj);
                    if (result != null) return result;
                }
                else
                {
                    if ((member as CobberMember).Object == obj) return member;
                }
            }

            return null;
        }


        // Called by: ProcessStructuralPhases()
        void InjectCustomAttribute( CobberModule cmod )
        {
            ModuleDefinition mod = cmod.Object;

            // create an attribute class (type) for injection
            TypeDefinition att = new TypeDefinition("", "CobberedByAttribute",
                TypeAttributes.Class | TypeAttributes.NotPublic, mod.Import(typeof(Attribute)));

            // there is a constructor method in this attribute class
            MethodDefinition ctor = new MethodDefinition(".ctor",
                MethodAttributes.RTSpecialName | MethodAttributes.SpecialName | MethodAttributes.Public, mod.TypeSystem.Void);
            ctor.Parameters.Add(new ParameterDefinition(mod.TypeSystem.String));

            ILProcessor psr = (ctor.Body = new MethodBody(ctor)).GetILProcessor();
            psr.Emit(OpCodes.Ldarg_0);
            psr.Emit(OpCodes.Call, mod.Import(typeof(Attribute).GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, Type.EmptyTypes, null)));
            psr.Emit(OpCodes.Ret);

            att.Methods.Add(ctor);

            // add to module
            mod.Types.Add(att); 

            // apply this custom attribute to the module
            CustomAttribute ca = new CustomAttribute(ctor);
            ca.ConstructorArguments.Add(new CustomAttributeArgument(mod.TypeSystem.String, string.Format("Cobber v" + typeof(Cobber).Assembly.GetName().Version.ToString())));
            mod.CustomAttributes.Add(ca);
        }


        // mark obfuscation setting for new member injected into this module
        void MarkNewObject(CobberObject parent, CobberObject member, HelperAttribute attr)
        {
            // determine member's applied target
            Target target = 0;
            if (member is CobberType) target = Target.Types;
            else
            {
                if ((member as CobberMember).MemberType == CobberMemberTypes.Method) target = Target.Methods;
                else if ((member as CobberMember).MemberType == CobberMemberTypes.Field) target = Target.Fields;
                else if ((member as CobberMember).MemberType == CobberMemberTypes.Event) target = Target.Events;
                else if ((member as CobberMember).MemberType == CobberMemberTypes.Property) target = Target.Properties;
            }


            // find an existing obf setting 
            ObfuscationSetting setting = ObfuscationSetting.GetSetting(parent.ObfSettingName);

            // create a new obf setting for this helper
            ObfuscationSetting sub_setting = new ObfuscationSetting();
            foreach (string obf_id in setting.ObfParameters.Keys)
            {
                IObfuscation obf = Cobber.Obfuscators[obf_id];
                NameValueCollection param = setting.ObfParameters[obf_id];

                // if obfuscation target does not apply here, or obfuscation behaviour collides with required, just skip on.
                if ((obf.Target & target) == 0 || (obf.Behaviour & (Behaviour)attr) != 0)
                    continue;

                // if there is an obfuscation phase that starts at beginning but does not support later addition,
                // then do not apply this obfuscation.
                bool ok = true;
                foreach (Phase phase in obf.Phases)
                {
                    if ( phase.PhaseID == 1 && (!phase.Obfuscator.SupportLateAddition) && !(phase is MetadataPhase) )
                    {
                        ok = false;
                        break;
                    }
                }
                if (!ok) continue;

                // Ok for add
                sub_setting.ObfParameters.Add(obf_id, param);
            }

            // save this obf setting into project
            ObfuscationSetting.AddSetting(sub_setting);
            sub_setting.IsInternal = true; // mark as program generated

            member.ObfSettingName = sub_setting.Name;

            // refresh to avoid name mismatch caused by renaming obfuscation
            //member.Mark(sub_setting.Name); // mark its children, added on 20140528
        }


        // Input: parent is mono object, could be module, type/nestedtype
        //        member helper and its attribute, member could be type and puremember
        //        member attribute indicates injection,alter,or encryption for modules
        // Possible injections:  < module, type>, <type,nestedType>, <type, member>
        public void InjectObject(object parent, IMemberDefinition member, HelperAttribute attr)
        {
            // firstly find the position of parent
            CobberObject cp = FindCobberObject(parent);
            if (cp == null)
            {
                Logger._Warn("parent object not found: " + parent);
                return;
            }

            // create cobber object for the member
            CobberObject one = FindCobberObject(member);
            if (one != null) // already exists, for example cctor
            {
                // do nothign except marking the setting again
                Logger._Warn("the object to be injected already exists: " + one);
            }
            else  // create a new one
            {
                if (member is TypeDefinition)
                {
                    one = new CobberType(cp)
                    {
                        Object = (member as TypeDefinition),
                        Name = (member as TypeDefinition).Name,
                        ObfSettingName = cp.ObfSettingName
                    };
                }
                else if (member is IMemberDefinition)
                {
                    CobberMemberTypes t;
                    string n = CobberMember.GetMemberSig(member, out t);
                    one = new CobberMember(cp)
                    {
                        Name = n,
                        MemberType = t,
                        Object = member,
                        ObfSettingName = cp.ObfSettingName
                    };
                }
                else
                {
                    Logger._Warn("unknown object found! " + member);
                    return;
                }

                // add the member in and mark obfuscation setting, by helperattribute
                if (one != null)
                {
                    one.Injected = true;

                    if (cp is CobberModule)
                    {
                        CobberType memType = (one as CobberType);
                        string ns_name = memType.Object.Namespace;
                        CobberNamespace ns = (cp as CobberModule).Namespaces.SingleOrDefault(_ => _.Name == ns_name);
                        if (ns == null)
                        {
                            ns = new CobberNamespace(cp)
                           {
                               Name = ns_name,
                           };
                            (cp as CobberModule).Namespaces.Add(ns);
                        }

                        ns.Types.Add(memType);
                        memType.Parent = ns;
                    }
                    else if (cp is CobberType)
                    {
                        (cp as CobberType).Members.Add(one);
                    }
                    else
                    {
                        Logger._Warn("wrong parent found! " + cp);
                        return;
                    }
                }
            }

            MarkNewObject(cp, one, attr);

            injectionCount++;
            //Log("Inject [" + one + "] into " + cp + " successfully.(" + injectionCount.ToString() + ")");
        }


        // add helper assembly, UnUsed!
        public static void InjectNewAssembly(AssemblyDefinition asm, string phase_obf_id, Cobber cobber)
        {
            // get main assembly
            CobberAssembly main = cobber.Project.GetMainAssembly();
            if (main == null) { main = cobber.Project.Assemblies[0]; }

            // get obfuscation setting for this assembly
            ObfuscationSetting s = ObfuscationSetting.GetSetting(main.ObfSettingName);
            // copy this setting
            ObfuscationSetting setting = new ObfuscationSetting(s);
            setting.IsInternal = true; // mark as program generated setting

            // exclude the obfuscator in current phase
            setting.ObfParameters.Remove(phase_obf_id);

            MarkNewAssembly(asm, setting, cobber);

            foreach (Analyzer analyzer in cobber.analyzers)
            {
                analyzer.Analyze(new AssemblyDefinition[] { asm });
            }
        }

        // called by: Phase.InjectNewAssembly(), unused!
        internal static void MarkNewAssembly(AssemblyDefinition asmDef, ObfuscationSetting settings, Cobber cobber)
        {
            CobberAssembly asm = new CobberAssembly();
            {
                asm.Object = asmDef;
                asm.IsMain = false;
                asm.ObfSettingName = settings.Name; // use same obf setting
                asm.ApplyToMembers = true;

                foreach (var mod in asmDef.Modules)
                {
                    CreateStaticConstructor(mod);

                    CobberModule m = new CobberModule(asm);
                    {
                        m.Name = mod.Name;
                        m.Object = mod;
                        m.ObfSettingName = null;
                    }
                    asm.Modules.Add(m);
                }
            }

            cobber.Project.Assemblies.Add(asm);
        }

        static MethodDefinition GetStaticConstructor(ModuleDefinition mod)
        {
            MethodDefinition cctor = mod.GetType("<Module>").GetStaticConstructor();
            return cctor;
        }

        static MethodDefinition CreateStaticConstructor(ModuleDefinition mod)
        {
            MethodDefinition cctor = mod.GetType("<Module>").GetStaticConstructor();
            if (cctor == null)
            {
                cctor = new MethodDefinition(".cctor",
                    MethodAttributes.Private | MethodAttributes.HideBySig |
                    MethodAttributes.SpecialName | MethodAttributes.RTSpecialName |
                    MethodAttributes.Static, mod.TypeSystem.Void);
                cctor.Body = new MethodBody(cctor);
                cctor.Body.GetILProcessor().Emit(OpCodes.Ret);

                mod.GetType("<Module>").Methods.Add(cctor);
            }

            return cctor;
        }

    }
}
