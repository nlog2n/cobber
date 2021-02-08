using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Security;
using System.Security.Permissions;
using System.Security.Cryptography;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Metadata;
using Mono.Cecil.PE;

using Cobber.Core.Project;

namespace Cobber.Core
{
    public struct Tuple<T1, T2>
    {
        public Tuple(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }
        public T1 Item1;
        public T2 Item2;
    }

    public struct Tuple<T1, T2, T3>
    {
        public Tuple(T1 item1, T2 item2, T3 item3)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
        }
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
    }


    public class Compressor : Packer
    {
        public override string ID { get { return "compress"; } }
        public override string Name { get { return "Compressing Packer"; } }
        public override string Description { get { return "Reduce the size of output"; } }
        public override bool StandardCompatible { get { return true; } }

        // saved for original main module with entry point
        uint origEntryPoint; 
        string origModName;
        ModuleKind origKind;

        // for storing all other assembly images
        List<Tuple<string, ManifestResourceAttributes, uint>> res;

        ByteBuffer hash;

        protected internal override void ProcessModulePhase3(ModuleDefinition mod, bool isMain)
        {
            if (isMain)
            {
                origModName = mod.Name;
                origEntryPoint = mod.Assembly.EntryPoint.MetadataToken.RID; // this got to be hidden later

                // change module name and reset entry point to null
                mod.Name = "___.netmodule";
                mod.Assembly.EntryPoint = null;

                res = new List<Tuple<string, ManifestResourceAttributes, uint>>();
            }
        }
        protected internal override void ProcessMetadataPhase2(MetadataProcessor.MetadataAccessor accessor, bool isMain)
        {
            if (isMain)
            {
                origKind = accessor.Module.Kind;
                accessor.Module.Kind = ModuleKind.NetModule;

                accessor.TableHeap.GetTable<AssemblyTable>(Table.Assembly).Clear();
                var resTbl = accessor.TableHeap.GetTable<ManifestResourceTable>(Table.ManifestResource);
                for (int i = 0; i < resTbl.Length; i++)
                    res.Add(new Tuple<string, ManifestResourceAttributes, uint>(
                        accessor.StringHeap.GetString(resTbl[i].Col3),
                        resTbl[i].Col2,
                        resTbl[i].Col1));
            }
        }

        protected internal override void PostProcessMetadata(MetadataProcessor.MetadataAccessor accessor)
        {
            accessor.StringHeap.Position = accessor.StringHeap.Length;
            accessor.BlobHeap.Position = accessor.BlobHeap.Length;
            int rid = accessor.TableHeap.GetTable<FileTable>(Table.File).AddRow(
                        new Row<Mono.Cecil.FileAttributes, uint, uint>(
                            Mono.Cecil.FileAttributes.ContainsMetaData,
                            accessor.StringHeap.GetStringIndex("___.netmodule"),
                            accessor.BlobHeap.GetBlobIndex(hash)));

            var resTbl = accessor.TableHeap.GetTable<ManifestResourceTable>(Table.ManifestResource);
            foreach (var i in res)
                resTbl.AddRow(new Row<uint, ManifestResourceAttributes, uint, uint>(
                    i.Item3, i.Item2, accessor.StringHeap.GetStringIndex(i.Item1),
                    CodedIndex.Implementation.CompressMetadataToken(
                        new MetadataToken(TokenType.File, rid))));
        }

        #region number arithmetic operations
        static bool isPrime(ulong n)
        {
            ulong max = (ulong)Math.Sqrt(n);
            for (ulong i = 2; i <= max; i++)
                if (n % i == 0)
                    return false;
            return true;
        }
        static ulong modPow(ulong bas, ulong pow, ulong mod)
        {
            ulong m = 1;
            while (pow > 0)
            {
                if ((pow & 1) != 0)
                    m = (m * bas) % mod;
                pow = pow >> 1;
                bas = (bas * bas) % mod;
            }
            return m;
        }
        static ulong modInv(ulong num, ulong mod)
        {
            ulong a = mod, b = num % mod;
            ulong p0 = 0, p1 = 1;
            while (b != 0)
            {
                if (b == 1) return p1;
                p0 += (a / b) * p1;
                a = a % b;

                if (a == 0) break;
                if (a == 1) return mod - p0;

                p1 += (b / a) * p0;
                b = b % a;
            }
            return 0;
        }
        #endregion

        public override string[] Pack(PackerSetting param)
        {
            ModuleDefinition originMain = param.Assemblies.Single(_ => _.IsMain).Object.MainModule;
            int originIndex = Array.IndexOf(param.Modules, originMain);

            // create a new assembly for packed program
            var asmOne = AssemblyDefinition.CreateAssembly(originMain.Assembly.Name, origModName, new ModuleParameters()
            {
                Architecture = originMain.Architecture,
                Kind = origKind,
                Runtime = originMain.Runtime
            });
            ModuleDefinition modOne = asmOne.MainModule;
            modOne.Attributes |= (originMain.Attributes & ModuleAttributes.Required32Bit); // added -- to prevent BadImageFormatException, Stub assembly need to set ModuleAttribute.Required32Bit if oringinMain has one.

            hash = new ByteBuffer(SHA1Managed.Create().ComputeHash(param.PEs[originIndex]));

            int key0 = Random.Next(0, 0xff);
            int key1 = Random.Next(0, 0xff);
            int key2 = Random.Next(0, 0xff);
            Database.AddEntry("Compressor", "Key0", key0);
            Database.AddEntry("Compressor", "Key1", key1);
            Database.AddEntry("Compressor", "Key2", key2);


            ulong e = 0x47;
            ulong p = (ulong)Random.Next(0x1000, 0x10000);
            while (!isPrime(p) || (p - 1) % e == 0) p = (ulong)Random.Next(0x1000, 0x10000);
            ulong q = (ulong)Random.Next(0x1000, 0x10000);
            while (!isPrime(q) || (q - 1) % e == 0) q = (ulong)Random.Next(0x1000, 0x10000);
            ulong n = p * q;
            ulong n_ = (p - 1) * (q - 1);
            ulong d = modInv(e, n_);
            Database.AddEntry("Compressor", "p", p);
            Database.AddEntry("Compressor", "q", q);
            Database.AddEntry("Compressor", "n", n);
            Database.AddEntry("Compressor", "d", d);

            // add all other assemblies into resources
            string mainResourceName = null;
            for (int i = 0; i < param.Modules.Length; i++)
            {
                //if (param.Modules[i].IsMain)
                if ( originIndex == i )
                {
                    //EmbeddedResource imageRes = new EmbeddedResource(GetNewResourceName(param.Modules[i].Assembly.Name.FullName, key2), ManifestResourceAttributes.Private, Encrypt(param.PEs[i], key0));
                    EmbeddedResource imageRes = new EmbeddedResource(GetNewResourceName(param.Modules[i].Name, key2), ManifestResourceAttributes.Private, Encrypt(param.PEs[i], key0));
                    modOne.Resources.Add(imageRes);

                    mainResourceName = imageRes.Name;
                }
                else
                {
                    EmbeddedResource imageRes = new EmbeddedResource(GetNewResourceName(param.Modules[i].Name, key2), ManifestResourceAttributes.Private, Encrypt(param.PEs[i], key1));
                    modOne.Resources.Add(imageRes);  
                    //TODO: Support for multi-module asssembly
                }
            }

            // inject a compression shell
            AssemblyDefinition ldrC = AssemblyDefinition.ReadAssembly(typeof(Iid).Assembly.Location);
            ldrC.MainModule.ReadSymbols();
            TypeDefinition t = CecilHelper.Inject(modOne, ldrC.MainModule.GetType("CompressShell")); // class name

            // lets do mutation on this class first
            Mutator mutator = new Mutator();
            {
                mutator.IntKeys = new int[] {
                    key0,
                    key1,
                    key2
                };
                mutator.LongKeys = new long[] {
                    (long)modPow(origEntryPoint, d, n),
                    (long)n
                };
                mutator.StringKeys = new string[] { 
                    mainResourceName // pass the resource name for main module to key0S
                };
            }
            mutator.Mutate(Random, t);


            t.Namespace = "";
            t.DeclaringType = null;
            t.IsNestedPrivate = false;
            t.IsNotPublic = true;

            modOne.Types.Add(t);  // injected!

            //MethodDefinition cctor = new MethodDefinition(".cctor", MethodAttributes.Private | MethodAttributes.HideBySig |
            //                                                MethodAttributes.SpecialName | MethodAttributes.RTSpecialName |
            //                                                MethodAttributes.Static, mod.TypeSystem.Void);
            //mod.GetType("<Module>").Methods.Add(cctor);
            //MethodBody bdy = cctor.Body = new MethodBody(cctor);
            //ILProcessor psr = bdy.GetILProcessor();
            //psr.Emit(OpCodes.Call, mod.Import(typeof(AppDomain).GetProperty("CurrentDomain").GetGetMethod()));
            //psr.Emit(OpCodes.Ldnull);
            //psr.Emit(OpCodes.Ldftn, t.Methods.FirstOrDefault(mtd => mtd.Name == "DecryptAsm"));
            //psr.Emit(OpCodes.Newobj, mod.Import(typeof(ResolveEventHandler).GetConstructor(new Type[] { typeof(object), typeof(IntPtr) })));
            //psr.Emit(OpCodes.Callvirt, mod.Import(typeof(AppDomain).GetEvent("AssemblyResolve").GetAddMethod()));
            //psr.Emit(OpCodes.Ret);


            // reset entry point to main method in CompressShell
            MethodDefinition main = t.Methods.FirstOrDefault(mtd => mtd.Name == "Main");
            modOne.EntryPoint = main;

            //asmOne.Write("cipherbox_xxx.exe");
            return ProtectStub(asmOne);
        }

        // change resource name into a new one. see CompressShell
        static string GetNewResourceName(string n, int key)
        {
            byte[] b = Encoding.UTF8.GetBytes(n);
            for (int i = 0; i < b.Length; i++)
            {
                b[i] = (byte)(b[i] ^ key ^ i);
            }
            return Encoding.UTF8.GetString(b);
        }

        // Accordingly, see CompressShell.Decrypt(byte[] asm)
        byte[] Encrypt(byte[] asm, int key0)
        {
            // Step 1: compress. set compression parameters
            int dictionary = 1 << 23;
            Int32 posStateBits = 2;
            Int32 litContextBits = 3; // for normal files
            // UInt32 litContextBits = 0; // for 32-bit data
            Int32 litPosBits = 0;
            // UInt32 litPosBits = 2; // for 32-bit data
            Int32 algorithm = 2;
            Int32 numFastBytes = 128;
            string mf = "bt4";

            SevenZip.CoderPropID[] propIDs = 
				{
					SevenZip.CoderPropID.DictionarySize,
					SevenZip.CoderPropID.PosStateBits,
					SevenZip.CoderPropID.LitContextBits,
					SevenZip.CoderPropID.LitPosBits,
					SevenZip.CoderPropID.Algorithm,
					SevenZip.CoderPropID.NumFastBytes,
					SevenZip.CoderPropID.MatchFinder,
					SevenZip.CoderPropID.EndMarker
				};
            object[] properties = 
				{
					(int)dictionary,
					(int)posStateBits,
					(int)litContextBits,
					(int)litPosBits,
					(int)algorithm,
					(int)numFastBytes,
					mf,
					false
				};

            // do compression
            MemoryStream final = new MemoryStream();
            var encoder = new SevenZip.Compression.LZMA.Encoder();
            encoder.SetCoderProperties(propIDs, properties);
            encoder.WriteCoderProperties(final);
            Int64 fileSize;
            fileSize = asm.Length;
            for (int i = 0; i < 8; i++)
            {
                final.WriteByte((Byte)(fileSize >> (8 * i)));
            }
            encoder.Code(new MemoryStream(asm), final, -1, -1, null);


            // Step 2: encryption
            var dat = new MemoryStream();
            RijndaelManaged rijn = Namer.CreateRijndael();
            using (var x = new CryptoStream(dat, rijn.CreateEncryptor(), CryptoStreamMode.Write))
            {
                x.Write(BitConverter.GetBytes(asm.Length), 0, 4); // number of original bytes
                x.Write(final.ToArray(), 0, (int)final.Length);
            }

            // do a little scramble on encryption key
            byte[] key = rijn.Key;
            for (int j = 0; j < key.Length; j += 4)
            {
                key[j + 0] ^= (byte)((key0 & 0x000000ff) >> 0);
                key[j + 1] ^= (byte)((key0 & 0x0000ff00) >> 8);
                key[j + 2] ^= (byte)((key0 & 0x00ff0000) >> 16);
                key[j + 3] ^= (byte)((key0 & 0xff000000) >> 24);
            }

            // Step 3: output, format (data, iv, key)
            MemoryStream str = new MemoryStream();
            using (BinaryWriter wtr = new BinaryWriter(str))
            {
                byte[] b = dat.ToArray();
                wtr.Write(b.Length);
                wtr.Write(b);  // data
                wtr.Write(rijn.IV.Length);
                wtr.Write(rijn.IV);  // IV
                wtr.Write(key.Length);
                wtr.Write(key);  // key
            }
            return str.ToArray();
        }
    }
}
