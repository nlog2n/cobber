﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Cobber.Core
{
    /// <summary>
    /// Instruction mutation, including OpCode, Operand (int, long, string)
    /// </summary>
    class Mutator
    {
        public int[] IntKeys { get; set; }
        public long[] LongKeys { get; set; }
        public string[] StringKeys { get; set; }
        public int[] DelayedKeys { get; set; }
        public Instruction Placeholder { get; private set; }
        public Instruction Delayed0 { get; private set; }
        public Instruction Delayed1 { get; private set; }
        public bool IsDelayed { get; set; }

        public void Mutate(Random rand, TypeDefinition typeDef)
        {
            foreach (var i in typeDef.NestedTypes)
            {
                Mutate(rand, i);
            }
            foreach (var i in typeDef.Methods)
            {
                if (i.HasBody)
                {
                    Mutate(rand, i.Body);
                }
            }
        }
        public void Mutate(Random rand, MethodBody body)
        {
            body.SimplifyMacros();
            foreach (var i in body.Instructions)
            {
                FieldReference field = i.Operand as FieldReference;
                if (field != null && field.DeclaringType.FullName == "Mutation")
                {
                    switch (field.Name)
                    {
                        case "Key0I":
                            i.Operand = IntKeys[0]; goto case "I";
                        case "Key1I":
                            i.Operand = IntKeys[1]; goto case "I";
                        case "Key2I":
                            i.Operand = IntKeys[2]; goto case "I";
                        case "Key3I":
                            i.Operand = IntKeys[3]; goto case "I";
                        case "Key4I":
                            i.Operand = IntKeys[4]; goto case "I";
                        case "Key5I":
                            i.Operand = IntKeys[5]; goto case "I";
                        case "Key6I":
                            i.Operand = IntKeys[6]; goto case "I";
                        case "Key7I":
                            i.Operand = IntKeys[7]; goto case "I";

                        case "Key0L":
                            i.Operand = LongKeys[0]; goto case "L";
                        case "Key1L":
                            i.Operand = LongKeys[1]; goto case "L";
                        case "Key2L":
                            i.Operand = LongKeys[2]; goto case "L";
                        case "Key3L":
                            i.Operand = LongKeys[3]; goto case "L";
                        case "Key4L":
                            i.Operand = LongKeys[4]; goto case "L";
                        case "Key5L":
                            i.Operand = LongKeys[5]; goto case "L";
                        case "Key6L":
                            i.Operand = LongKeys[6]; goto case "L";
                        case "Key7L":
                            i.Operand = LongKeys[7]; goto case "L";

                        case "Key0S":
                            i.Operand = StringKeys[0]; goto case "S";
                        case "Key1S":
                            i.Operand = StringKeys[1]; goto case "S";
                        case "Key2S":
                            i.Operand = StringKeys[2]; goto case "S";
                        case "Key3S":
                            i.Operand = StringKeys[3]; goto case "S";

                        case "Key0Delayed":
                            if (IsDelayed)
                            {
                                i.Operand = DelayedKeys[0];
                                goto case "I";
                            }
                            else
                                Delayed0 = i;
                            break;
                        case "Key1Delayed":
                            if (IsDelayed)
                            {
                                i.Operand = DelayedKeys[1];
                                goto case "I";
                            }
                            else
                                Delayed1 = i;
                            break;

                        case "I":
                            i.OpCode = OpCodes.Ldc_I4; break;
                        case "L":
                            i.OpCode = OpCodes.Ldc_I8; break;
                        case "S":
                            i.OpCode = OpCodes.Ldstr; break;
                    }
                }
                MethodReference method = i.Operand as MethodReference;
                if (method != null && method.DeclaringType.FullName == "Mutation")
                {
                    if (method.Name == "Placeholder")
                        Placeholder = i;
                    else if (method.Name == "DeclaringType")
                    {
                        i.OpCode = OpCodes.Ldtoken;
                        i.Operand = body.Method.DeclaringType;
                    }
                }
            }

            for (int i = 0; i < body.Variables.Count; i++)
            {
                int x = rand.Next(0, body.Variables.Count);
                var tmp = body.Variables[i];
                body.Variables[i] = body.Variables[x];
                body.Variables[x] = tmp;
            }

            int iteration = rand.Next(20, 35);
            while (iteration > 0)
            {
                MutateCode(rand, body);
                iteration--;
            }
        }
        public void MutateCode(Random rand, MethodBody body)
        {
            bool modified = false;
            int i = 10;
            do
            {
                int idx = rand.Next(0, body.Instructions.Count);
                Instruction inst = body.Instructions[idx];
                switch (inst.OpCode.Code)
                {
                    case Code.Beq:
                        CecilHelper.Replace(body, inst, new Instruction[]
                        {
                            Instruction.Create(OpCodes.Ceq),
                            Instruction.Create(OpCodes.Brtrue, (Instruction)inst.Operand)
                        });
                        modified = true; break;
                    case Code.Bgt:
                        CecilHelper.Replace(body, inst, new Instruction[]
                        {
                            Instruction.Create(OpCodes.Cgt),
                            Instruction.Create(OpCodes.Brtrue, (Instruction)inst.Operand)
                        });
                        modified = true; break;
                    case Code.Bgt_Un:
                        CecilHelper.Replace(body, inst, new Instruction[]
                        {
                            Instruction.Create(OpCodes.Cgt_Un),
                            Instruction.Create(OpCodes.Brtrue, (Instruction)inst.Operand)
                        });
                        modified = true; break;
                    case Code.Blt:
                        CecilHelper.Replace(body, inst, new Instruction[]
                        {
                            Instruction.Create(OpCodes.Clt),
                            Instruction.Create(OpCodes.Brtrue, (Instruction)inst.Operand)
                        });
                        modified = true; break;
                    case Code.Blt_Un:
                        CecilHelper.Replace(body, inst, new Instruction[]
                        {
                            Instruction.Create(OpCodes.Clt_Un),
                            Instruction.Create(OpCodes.Brtrue, (Instruction)inst.Operand)
                        });
                        modified = true; break;
                    case Code.Ldc_I4:
                        {
                            int x = (int)inst.Operand;
                            if (x > 0x10)
                            {
                                int y = rand.Next();
                                switch (rand.Next(0, 3))
                                {
                                    case 0:
                                        x = x - y;
                                        CecilHelper.Replace(body, inst, new Instruction[]
                                        {
                                            Instruction.Create(OpCodes.Ldc_I4, x),
                                            Instruction.Create(OpCodes.Ldc_I4, y),
                                            Instruction.Create(OpCodes.Add)
                                        }); break;
                                    case 1:
                                        x = x + y;
                                        CecilHelper.Replace(body, inst, new Instruction[]
                                        {
                                            Instruction.Create(OpCodes.Ldc_I4, x),
                                            Instruction.Create(OpCodes.Ldc_I4, y),
                                            Instruction.Create(OpCodes.Sub)
                                        }); break;
                                    case 2:
                                        x = x ^ y;
                                        CecilHelper.Replace(body, inst, new Instruction[]
                                        {
                                            Instruction.Create(OpCodes.Ldc_I4, x),
                                            Instruction.Create(OpCodes.Ldc_I4, y),
                                            Instruction.Create(OpCodes.Xor)
                                        }); break;
                                }
                                modified = true;
                            }
                        } break;
                    case Code.Ldc_I8:
                        {
                            long x = (long)inst.Operand;
                            if (x > 0x10)
                            {
                                long y = rand.Next() * rand.Next();
                                switch (rand.Next(0, 3))
                                {
                                    case 0:
                                        x = x - y;
                                        CecilHelper.Replace(body, inst, new Instruction[]
                                        {
                                            Instruction.Create(OpCodes.Ldc_I8, x),
                                            Instruction.Create(OpCodes.Ldc_I8, y),
                                            Instruction.Create(OpCodes.Add)
                                        }); break;
                                    case 1:
                                        x = x + y;
                                        CecilHelper.Replace(body, inst, new Instruction[]
                                        {
                                            Instruction.Create(OpCodes.Ldc_I8, x),
                                            Instruction.Create(OpCodes.Ldc_I8, y),
                                            Instruction.Create(OpCodes.Sub)
                                        }); break;
                                    case 2:
                                        x = x ^ y;
                                        CecilHelper.Replace(body, inst, new Instruction[]
                                        {
                                            Instruction.Create(OpCodes.Ldc_I8, x),
                                            Instruction.Create(OpCodes.Ldc_I8, y),
                                            Instruction.Create(OpCodes.Xor)
                                        }); break;
                                }
                                modified = true;
                            }
                        } break;
                }
                i--;
            } while (!modified && i > 0);
        }
    }
}
