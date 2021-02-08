using System;
using System.Text;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;


static class Proxies
{
    private static void CtorProxy(RuntimeFieldHandle f)
    {
        FieldInfo fld = FieldInfo.GetFieldFromHandle(f);
        var m = fld.Module;
        byte[] dat = m.ResolveSignature(fld.MetadataToken); 

        uint x =
            ((uint)dat[dat.Length - 6] << 0) |
            ((uint)dat[dat.Length - 5] << 8) |
            ((uint)dat[dat.Length - 3] << 16) |
            ((uint)dat[dat.Length - 2] << 24);

        ConstructorInfo mtd = m.ResolveMethod(Mutation.Placeholder((int)x) | ((int)dat[dat.Length - 7] << 24)) as ConstructorInfo; 

        var args = mtd.GetParameters();
        Type[] arg = new Type[args.Length];
        for (int i = 0; i < args.Length; i++)
            arg[i] = args[i].ParameterType;

        DynamicMethod dm;
        if (mtd.DeclaringType.IsInterface || mtd.DeclaringType.IsArray)
            dm = new DynamicMethod("", mtd.DeclaringType, arg, fld.DeclaringType, true);
        else
            dm = new DynamicMethod("", mtd.DeclaringType, arg, mtd.DeclaringType, true);
        var gen = dm.GetILGenerator();
        for (int i = 0; i < arg.Length; i++)
            gen.Emit(System.Reflection.Emit.OpCodes.Ldarg_S, i);
        gen.Emit(System.Reflection.Emit.OpCodes.Newobj, mtd);
        gen.Emit(System.Reflection.Emit.OpCodes.Ret);

        fld.SetValue(null, dm.CreateDelegate(fld.FieldType));
    }

    private static void MtdProxy(RuntimeFieldHandle f)
    {
        var fld = FieldInfo.GetFieldFromHandle(f);
        var m = fld.Module;
        byte[] dat = m.ResolveSignature(fld.MetadataToken); 

        uint x =
            ((uint)dat[dat.Length - 6] << 0) |
            ((uint)dat[dat.Length - 5] << 8) |
            ((uint)dat[dat.Length - 3] << 16) |
            ((uint)dat[dat.Length - 2] << 24);

        var mtd = m.ResolveMethod(Mutation.Placeholder((int)x) | ((int)dat[dat.Length - 7] << 24)) as MethodInfo; 

        if (mtd.IsStatic)
            fld.SetValue(null, Delegate.CreateDelegate(fld.FieldType, mtd));
        else
        {
            string n = fld.Name; 

            var tmp = mtd.GetParameters();
            Type[] arg = new Type[tmp.Length + 1];
            arg[0] = typeof(object);
            for (int i = 0; i < tmp.Length; i++)
                arg[i + 1] = tmp[i].ParameterType;

            DynamicMethod dm;
            var decl = mtd.DeclaringType;
            var decl2 = fld.DeclaringType;
            if (decl.IsInterface || decl.IsArray)
                dm = new DynamicMethod("", mtd.ReturnType, arg, decl2, true); 
            else
                dm = new DynamicMethod("", mtd.ReturnType, arg, decl, true); 

            var gen = dm.GetILGenerator();
            for (int i = 0; i < arg.Length; i++)
            {
                gen.Emit(OpCodes.Ldarg, i);
                if (i == 0) gen.Emit(OpCodes.Castclass, decl); 
            }

            if (n[0] == Mutation.Key0I)
                gen.Emit(OpCodes.Callvirt, mtd);
            else
                gen.Emit(OpCodes.Call, mtd); 

            gen.Emit(OpCodes.Ret);

            fld.SetValue(null, dm.CreateDelegate(fld.FieldType));
        }
    }
}