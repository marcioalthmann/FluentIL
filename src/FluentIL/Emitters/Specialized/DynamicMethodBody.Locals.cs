﻿using System.Linq;
using System.Reflection.Emit;
using FluentIL.Infos;

// ReSharper disable CheckNamespace
namespace FluentIL.Emitters
// ReSharper restore CheckNamespace
{
    partial class DynamicMethodBody
    {
        public int GetVariableIndex(string varname)
        {
            DynamicVariableInfo[] variables = Enumerable.ToArray<DynamicVariableInfo>(methodInfoField.Variables);

            for (int i = 0; i < variables.Length; i++)
                if (variables[i].Name == varname)
                    return i;

            return -1;
        }

        public int GetParameterIndex(string parametername)
        {
            DynamicVariableInfo[] parameters = Enumerable.ToArray<DynamicVariableInfo>(methodInfoField.Parameters);

            for (int i = 0; i < parameters.Length; i++)
                if (parameters[i].Name == parametername)
                    return i;

            return -1;
        }


        public DynamicMethodBody Ldloc(params uint[] args)
        {
            foreach (uint arg in args)
            {
                switch (arg)
                {
                    case 0:
                        Emit(OpCodes.Ldloc_0);
                        break;
                    case 1:
                        Emit(OpCodes.Ldloc_1);
                        break;
                    case 2:
                        Emit(OpCodes.Ldloc_2);
                        break;
                    case 3:
                        Emit(OpCodes.Ldloc_3);
                        break;
                    default:
                        Emit(OpCodes.Ldloc, (int) arg);
                        break;
                }
            }
            return this;
        }

        public DynamicMethodBody Stloc(params uint[] args)
        {
            foreach (uint arg in args)
            {
                switch (arg)
                {
                    case 0:
                        Emit(OpCodes.Stloc_0);
                        break;
                    case 1:
                        Emit(OpCodes.Stloc_1);
                        break;
                    case 2:
                        Emit(OpCodes.Stloc_2);
                        break;
                    case 3:
                        Emit(OpCodes.Stloc_3);
                        break;
                    default:
                        Emit(OpCodes.Stloc, (int) arg);
                        break;
                }
            }
            return this;
        }

        public DynamicMethodBody Ldloc(params string[] args)
        {
            foreach (string arg in args)
                Ldloc((uint) GetVariableIndex(arg));

            return this;
        }

        public DynamicMethodBody Stloc(params string[] args)
        {
            foreach (string arg in args)
                Stloc((uint) GetVariableIndex(arg));

            return this;
        }
    }
}