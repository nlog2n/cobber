using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using System.Security.Cryptography;

static class AntiTamperMem
{
    [DllImportAttribute("kernel32.dll")]
    static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

    public static unsafe void Initalize()
    {
        Module mod = typeof(AntiTamperMem).Module;
        IntPtr modPtr = Marshal.GetHINSTANCE(mod);
        if (modPtr == (IntPtr)(-1)) Environment.FailFast("Module error");
        bool mapped = mod.FullyQualifiedName[0] != '<'; //<Unknown>
        Stream stream;
        stream = new UnmanagedMemoryStream((byte*)modPtr.ToPointer(), 0xfffffff, 0xfffffff, FileAccess.ReadWrite);

        byte[] buff;
        int checkSumOffset;
        ulong checkSum;
        byte[] iv;
        byte[] dats;
        int sn;
        int snLen;
        using (BinaryReader rdr = new BinaryReader(stream))
        {
            stream.Seek(0x3c, SeekOrigin.Begin);
            uint offset = rdr.ReadUInt32();
            stream.Seek(offset, SeekOrigin.Begin);
            stream.Seek(0x6, SeekOrigin.Current);
            uint sections = rdr.ReadUInt16();
            stream.Seek(0xC, SeekOrigin.Current);
            uint optSize = rdr.ReadUInt16();
            stream.Seek(offset = offset + 0x18, SeekOrigin.Begin);  //Optional hdr
            bool pe32 = (rdr.ReadUInt16() == 0x010b);
            stream.Seek(0x3e, SeekOrigin.Current);
            checkSumOffset = (int)stream.Position;
            uint md = rdr.ReadUInt32() ^ (uint)Mutation.Key0I;
            if (md == (uint)Mutation.Key0I)
                Environment.FailFast("Broken file");

            stream.Seek(offset = offset + optSize, SeekOrigin.Begin);  //sect hdr
            uint datLoc = 0;
            for (int i = 0; i < sections; i++)
            {
                int h = 0;
                for (int j = 0; j < 8; j++)
                {
                    byte chr = rdr.ReadByte();
                    if (chr != 0) h += chr;
                }
                uint vSize = rdr.ReadUInt32();
                uint vLoc = rdr.ReadUInt32();
                uint rSize = rdr.ReadUInt32();
                uint rLoc = rdr.ReadUInt32();
                if (h == Mutation.Key1I)
                    datLoc = mapped ? vLoc : rLoc;
                if (!mapped && md > vLoc && md < vLoc + vSize)
                    md = md - vLoc + rLoc;
                stream.Seek(0x10, SeekOrigin.Current);
            }

            stream.Seek(md, SeekOrigin.Begin);
            using (MemoryStream str = new MemoryStream())
            {
                stream.Position += 12;
                stream.Position += rdr.ReadUInt32() + 4;
                stream.Position += 2;

                ushort streams = rdr.ReadUInt16();

                for (int i = 0; i < streams; i++)
                {
                    uint pos = rdr.ReadUInt32() + md;
                    uint size = rdr.ReadUInt32();

                    int c = 0;
                    while (rdr.ReadByte() != 0) c++;
                    long ori = stream.Position += (((c + 1) + 3) & ~3) - (c + 1);

                    stream.Position = pos;
                    str.Write(rdr.ReadBytes((int)size), 0, (int)size);
                    stream.Position = ori;
                }

                buff = str.ToArray();
            }

            stream.Seek(datLoc, SeekOrigin.Begin);
            checkSum = rdr.ReadUInt64() ^ (ulong)Mutation.Key0L;
            sn = rdr.ReadInt32();
            snLen = rdr.ReadInt32();
            iv = rdr.ReadBytes(rdr.ReadInt32() ^ Mutation.Key2I);
            dats = rdr.ReadBytes(rdr.ReadInt32() ^ Mutation.Key3I);
        }

        byte[] md5 = MD5.Create().ComputeHash(buff);
        ulong tCs = BitConverter.ToUInt64(md5, 0) ^ BitConverter.ToUInt64(md5, 8);
        if (tCs != checkSum)
            Environment.FailFast("Broken file");

        byte[] b = Decrypt(buff, iv, dats);
        Buffer.BlockCopy(new byte[buff.Length], 0, buff, 0, buff.Length);
        if (b[0] != 0xd6 || b[1] != 0x6f)
            Environment.FailFast("Broken file");
        byte[] tB = new byte[b.Length - 2];
        Buffer.BlockCopy(b, 2, tB, 0, tB.Length);
        using (BinaryReader rdr = new BinaryReader(new MemoryStream(tB)))
        {
            uint len = rdr.ReadUInt32();
            int[] codeLens = new int[len];
            IntPtr[] ptrs = new IntPtr[len];
            for (int i = 0; i < len; i++)
            {
                uint pos = rdr.ReadUInt32() ^ (uint)Mutation.Key4I;
                if (pos == 0) continue;
                uint rva = rdr.ReadUInt32() ^ (uint)Mutation.Key5I;
                byte[] cDat = rdr.ReadBytes(rdr.ReadInt32());
                uint old;
                IntPtr ptr = (IntPtr)((uint)modPtr + (mapped ? rva : pos));
                VirtualProtect(ptr, (uint)cDat.Length, 0x04, out old);
                Marshal.Copy(cDat, 0, ptr, cDat.Length);
                VirtualProtect(ptr, (uint)cDat.Length, old, out old);
                codeLens[i] = cDat.Length;
                ptrs[i] = ptr;
            }
            //for (int i = 0; i < len; i++)
            //{
            //    if (codeLens[i] == 0) continue;
            //    RuntimeHelpers.PrepareMethod(mod.ModuleHandle.GetRuntimeMethodHandleFromMetadataToken(0x06000000 + i + 1));
            //}
            //for (int i = 0; i < len; i++)
            //{
            //    if (codeLens[i] == 0) continue;
            //    uint old;
            //    VirtualProtect(ptrs[i], (uint)codeLens[i], 0x04, out old);
            //    Marshal.Copy(new byte[codeLens[i]], 0, ptrs[i], codeLens[i]);
            //    VirtualProtect(ptrs[i], (uint)codeLens[i], old, out old);
            //}
        }
    }

    static byte[] Decrypt(byte[] buff, byte[] iv, byte[] dat)
    {
        RijndaelManaged ri = new RijndaelManaged();
        byte[] ret = new byte[dat.Length];
        MemoryStream ms = new MemoryStream(dat);
        using (CryptoStream cStr = new CryptoStream(ms, ri.CreateDecryptor(SHA256.Create().ComputeHash(buff), iv), CryptoStreamMode.Read))
        { cStr.Read(ret, 0, dat.Length); }

        SHA512 sha = SHA512.Create();
        byte[] c = sha.ComputeHash(buff);
        for (int i = 0; i < ret.Length; i += 64)
        {
            int len = ret.Length <= i + 64 ? ret.Length : i + 64;
            for (int j = i; j < len; j++)
                ret[j] ^= (byte)(c[j - i] ^ Mutation.Key6I);
            c = sha.ComputeHash(ret, i, len - i);
        }
        return ret;
    }
}