using System;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;

static class Encryptions
{
    static Assembly datAsm;
    static Assembly Resources(object sender, ResolveEventArgs args)
    {
        if (datAsm == null)
        {
            Stream str = typeof(Exception).Assembly.GetManifestResourceStream(Mutation.Key0S);
            byte[] dat = new byte[str.Length];
            str.Read(dat, 0, dat.Length);
            byte k = (byte)Mutation.Key0I;
            for (int i = 0; i < dat.Length; i++)
            {
                dat[i] = (byte)(dat[i] ^ k);
                k = (byte)((k * Mutation.Key1I) % 0x100);
            }

            using (BinaryReader rdr = new BinaryReader(new DeflateStream(new MemoryStream(dat), CompressionMode.Decompress)))
            {
                dat = rdr.ReadBytes(rdr.ReadInt32());
                datAsm = System.Reflection.Assembly.Load(dat);
                Buffer.BlockCopy(new byte[dat.Length], 0, dat, 0, dat.Length);
            }
        }
        if (Array.IndexOf(datAsm.GetManifestResourceNames(), args.Name) == -1)
            return null;
        else
            return datAsm;
    }

    //private static string SafeStrings(int id)
    //{
    //    Dictionary<int, string> hashTbl;
    //    if ((hashTbl = AppDomain.CurrentDomain.GetData("PADDINGPADDINGPADDING") as Dictionary<int, string>) == null)
    //    {
    //        AppDomain.CurrentDomain.SetData("PADDINGPADDINGPADDING", hashTbl = new Dictionary<int, string>());
    //        MemoryStream stream = new MemoryStream();
    //        Assembly asm = Assembly.GetCallingAssembly();
    //        using (DeflateStream str = new DeflateStream(asm.GetManifestResourceStream("PADDINGPADDINGPADDING"), CompressionMode.Decompress))
    //        {
    //            byte[] dat = new byte[0x1000];
    //            int read = str.Read(dat, 0, 0x1000);
    //            do
    //            {
    //                stream.Write(dat, 0, read);
    //                read = str.Read(dat, 0, 0x1000);
    //            }
    //            while (read != 0);
    //        }
    //        AppDomain.CurrentDomain.SetData("PADDINGPADDINGPADDINGPADDING", stream.ToArray());
    //    }
    //    string ret;
    //    int mdTkn = new StackFrame(1).GetMethod().MetadataToken;
    //    int pos = (mdTkn ^ id) - 12345678;
    //    if (!hashTbl.TryGetValue(pos, out ret))
    //    {
    //        using (BinaryReader rdr = new BinaryReader(new MemoryStream((byte[])AppDomain.CurrentDomain.GetData("PADDINGPADDINGPADDINGPADDING"))))
    //        {
    //            rdr.BaseStream.Seek(pos, SeekOrigin.Begin);
    //            int len = (int)((~rdr.ReadUInt32()) ^ 87654321);
    //            byte[] b = rdr.ReadBytes(len);

    //            ///////////////////

    //            uint seed = 88888888;
    //            ushort _m = (ushort)(seed >> 16);
    //            ushort _c = (ushort)(seed & 0xffff);
    //            ushort m = _c; ushort c = _m;
    //            byte[] k = new byte[b.Length];
    //            for (int i = 0; i < k.Length; i++)
    //            {
    //                k[i] = (byte)((seed * m + c) % 0x100);
    //                m = (ushort)((seed * m + _m) % 0x10000);
    //                c = (ushort)((seed * c + _c) % 0x10000);
    //            }

    //            int key = 0;
    //            for (int i = 0; i < b.Length; i++)
    //            {
    //                byte o = b[i];
    //                b[i] = (byte)(b[i] ^ (key / k[i]));
    //                key += o;
    //            }
    //            hashTbl[pos] = (ret = Encoding.UTF8.GetString(b));
    //            ///////////////////
    //        }
    //    }
    //    return ret;
    //}
    //private static string Strings(int id)
    //{
    //    Dictionary<int, string> hashTbl;
    //    if ((hashTbl = AppDomain.CurrentDomain.GetData("PADDINGPADDINGPADDING") as Dictionary<int, string>) == null)
    //    {
    //        AppDomain.CurrentDomain.SetData("PADDINGPADDINGPADDING", hashTbl = new Dictionary<int, string>());
    //        MemoryStream stream = new MemoryStream();
    //        Assembly asm = Assembly.GetCallingAssembly();
    //        using (DeflateStream str = new DeflateStream(asm.GetManifestResourceStream("PADDINGPADDINGPADDING"), CompressionMode.Decompress))
    //        {
    //            byte[] dat = new byte[0x1000];
    //            int read = str.Read(dat, 0, 0x1000);
    //            do
    //            {
    //                stream.Write(dat, 0, read);
    //                read = str.Read(dat, 0, 0x1000);
    //            }
    //            while (read != 0);
    //        }
    //        AppDomain.CurrentDomain.SetData("PADDINGPADDINGPADDINGPADDING", stream.ToArray());
    //    }
    //    string ret;
    //    int mdTkn = new StackFrame(1).GetMethod().MetadataToken;
    //    int pos = (mdTkn ^ id) - 12345678;
    //    if (!hashTbl.TryGetValue(pos, out ret))
    //    {
    //        using (BinaryReader rdr = new BinaryReader(new MemoryStream((byte[])AppDomain.CurrentDomain.GetData("PADDINGPADDINGPADDINGPADDING"))))
    //        {
    //            rdr.BaseStream.Seek(pos, SeekOrigin.Begin);
    //            int len = (int)((~rdr.ReadUInt32()) ^ 87654321);

    //            ///////////////////
    //            byte[] f = new byte[(len + 7) & ~7];

    //            for (int i = 0; i < f.Length; i++)
    //            {
    //                Poly.PolyStart();
    //                int count = 0;
    //                int shift = 0;
    //                byte b;
    //                do
    //                {
    //                    b = rdr.ReadByte();
    //                    count |= (b & 0x7F) << shift;
    //                    shift += 7;
    //                } while ((b & 0x80) != 0);

    //                f[i] = (byte)Poly.PlaceHolder(count);
    //            }

    //            hashTbl[pos] = (ret = Encoding.Unicode.GetString(f, 0, len));
    //            ///////////////////
    //        }
    //    }
    //    return ret;
    //}

    public static int PlaceHolder(int val) { return 0; }

    static Dictionary<uint, object> constTbl;
    static byte[] constBuffer;
    static void Initialize()
    {
        constTbl = new Dictionary<uint, object>();
        var s = new MemoryStream();
        Assembly asm = Assembly.GetExecutingAssembly();
        var x = asm.GetManifestResourceStream(Encoding.UTF8.GetString(BitConverter.GetBytes(Mutation.Key0I)));

        var method = MethodBase.GetCurrentMethod();
        var key = method.Module.ResolveSignature((int)(Mutation.Key0Delayed ^ method.MetadataToken));

        var str = new DeflateStream(new CryptoStream(x,
            new RijndaelManaged().CreateDecryptor(key, MD5.Create().ComputeHash(key)), CryptoStreamMode.Read)
            , CompressionMode.Decompress);
        {
            byte[] dat = new byte[0x1000];
            int read = str.Read(dat, 0, 0x1000);
            do
            {
                s.Write(dat, 0, read);
                read = str.Read(dat, 0, 0x1000);
            }
            while (read != 0);
        }
        str.Dispose();

        s.Position = 0;
        byte[] b = s.ToArray();

        // fanghui: do we need compression on buffer b here?

        s = new MemoryStream();
        BinaryWriter wtr = new BinaryWriter(s);
        {
            int i = 0;
            while (i < b.Length)
            {
                int count = 0;
                int shift = 0;
                byte c;
                do
                {
                    c = b[i++];
                    count |= (c & 0x7F) << shift;
                    shift += 7;
                } while ((c & 0x80) != 0);

                count = Mutation.Placeholder(count);
                wtr.Write((byte)count);
            }
        }
        s.Dispose();

        constBuffer = s.ToArray();
    }
    static void InitializeSafe()
    {
        constTbl = new Dictionary<uint, object>();
        var s = new MemoryStream();
        Assembly asm = Assembly.GetExecutingAssembly();
        var x = asm.GetManifestResourceStream(Encoding.UTF8.GetString(BitConverter.GetBytes(Mutation.Key0I)));
        byte[] buff = new byte[x.Length];
        x.Read(buff, 0, buff.Length);

        var method = MethodBase.GetCurrentMethod();
        var key = method.Module.ResolveSignature((int)(Mutation.Key0Delayed ^ method.MetadataToken));

        uint seed = BitConverter.ToUInt32(key, 0xc) * (uint)Mutation.Key0I;
        ushort _m = (ushort)(seed >> 16);
        ushort _c = (ushort)(seed & 0xffff);
        ushort m = _c; ushort c = _m;
        for (int i = 0; i < buff.Length; i++)
        {
            buff[i] ^= (byte)((seed * m + c) % 0x100);
            m = (ushort)((seed * m + _m) % 0x10000);
            c = (ushort)((seed * c + _c) % 0x10000);
        }

        var str = new DeflateStream(new CryptoStream(new MemoryStream(buff),
            new RijndaelManaged().CreateDecryptor(key, MD5.Create().ComputeHash(key)), CryptoStreamMode.Read)
            , CompressionMode.Decompress);
        {
            byte[] dat = new byte[0x1000];
            int read = str.Read(dat, 0, 0x1000);
            do
            {
                s.Write(dat, 0, read);
                read = str.Read(dat, 0, 0x1000);
            }
            while (read != 0);
        }
        str.Dispose();

        constBuffer = s.ToArray();
    }
    static T Constants<T>(uint a, ulong b)
    {
        object ret;
        uint x = (uint)(Type.GetTypeFromHandle(Mutation.DeclaringType()).MetadataToken * a);
        ulong h = (ulong)Mutation.Key0L * x;
        ulong h1 = (ulong)Mutation.Key1L;
        ulong h2 = (ulong)Mutation.Key2L;
        h1 = h1 * h;
        h2 = h2 * h;
        h = h * h;

        ulong hash = 0xCBF29CE484222325;
        while (h != 0)
        {
            hash *= 0x100000001B3;
            hash = (hash ^ h) + (h1 ^ h2) * (uint)Mutation.Key0I;
            h1 *= 0x811C9DC5;
            h2 *= 0xA2CEBAB2;
            h >>= 8;
        }
        ulong dat = hash ^ b;
        uint pos = (uint)(dat >> 32);
        uint len = (uint)dat;
        lock (constTbl)
        {
            if (!constTbl.TryGetValue(pos, out ret))
            {
                byte[] bs = new byte[len];
                Array.Copy(constBuffer, (int)pos, bs, 0, len);
                var method = MethodBase.GetCurrentMethod();
                byte[] key = BitConverter.GetBytes(method.MetadataToken ^ Mutation.Key0Delayed);
                for (int i = 0; i < bs.Length; i++)
                    bs[i] ^= key[(pos + i) % 4];

                if (typeof(T) == typeof(string))
                    ret = Encoding.UTF8.GetString(bs);
                else
                {
                    var t = new T[1];
                    Buffer.BlockCopy(bs, 0, t, 0, Marshal.SizeOf(default(T)));
                    ret = t[0];
                }
                constTbl[pos] = ret;
            }
        }
        return (T)ret;
    }
}
