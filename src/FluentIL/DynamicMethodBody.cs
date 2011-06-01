﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq.Expressions;
using FluentIL.ExpressionInterpreter;
using System.Diagnostics;
using FluentIL.ExpressionParser;

namespace FluentIL
{
    public partial class DynamicMethodBody
    {
        DynamicMethodInfo _Info;
        internal DynamicMethodBody(DynamicMethodInfo info)
        {
            this._Info = info;
        }

        public DynamicMethod AsDynamicMethod
        {
            get
            {
                return _Info.AsDynamicMethod;
            }
        }

        public Type AsType
        {
            get
            {
                return _Info.DynamicTypeInfo.AsType;
            }
        }


        public DynamicMethodInfo WithMethod(string methodName)
        {
            return this._Info.DynamicTypeInfo.WithMethod(methodName);
        }

        public DynamicMethodBody Parse(string expression)
        {
            Parser.Parse(expression, this);
            return this;
        }


        public DynamicMethodBody Box(Type type)
        {
            if (type.IsSubclassOf(typeof(ValueType)))
                this.Emit(OpCodes.Box, type);
            return this;
        }

        public DynamicMethodBody Ldfld(FieldInfo fldInfo)
        {
            this.Emit(OpCodes.Ldfld, fldInfo);
            return this;
        }

        public DynamicMethodBody Ldfld(string fieldName)
        {
            var field = this._Info.DynamicTypeInfo.GetFieldInfo(fieldName);
            return this.Ldfld(field);
        }

        public DynamicMethodBody Stfld(FieldInfo fldInfo)
        {
            this.Emit(OpCodes.Stfld, fldInfo);
            return this;
        }

        public DynamicMethodBody Stfld(string fieldName)
        {
            var field = this._Info.DynamicTypeInfo.GetFieldInfo(fieldName);
            return this.Stfld(field);
        }


        public DynamicMethodBody IfEmptyString(bool not)
        {
            var stringEmptyField = typeof(string).GetField("Empty");
            var stringOp_EqualityMethod = typeof(string).GetMethod(
                "op_Equality", new[] { typeof(string), typeof(string) });

            var emitter = new IfEmitter(this);
            _IfEmitters.Push(emitter);
            this
                .Ldsfld(stringEmptyField)
                .Call(stringOp_EqualityMethod);

            emitter.EmitBranch(not);
            return this;
        }

        public DynamicMethodBody IfEmptyString()
        {
            return this.IfEmptyString(false);
        }

        public DynamicMethodBody IfNotEmptyString()
        {
            return this.IfEmptyString(true);
        }

        public DynamicMethodBody IfNull(bool not)
        {
            var emitter = new IfEmitter(this);
            _IfEmitters.Push(emitter);
            emitter.EmitBranch(!not);
            return this;
        }

        public DynamicMethodBody IfNull()
        {
            return this.IfNull(false);
        }

        public DynamicMethodBody IfNotNull()
        {
            return this.IfNull(true);
        }

        public DynamicMethodBody If(Expression expression)
        {
            var emitter = new IfEmitter(this);
            _IfEmitters.Push(emitter);
            this.Expression(expression);
            emitter.EmitBranch(false);
            return this;
        }

        public DynamicMethodBody If(string expression)
        {
            var emitter = new IfEmitter(this);
            _IfEmitters.Push(emitter);
            Parser.Parse(expression, this);
            emitter.EmitBranch(false);
            return this;
        }

        


        public DynamicMethodBody Throw<TException>(params Type[] types)
            where TException : Exception
        {
            return this
                .Newobj<TException>(types)
                .Throw();
        }

        public DynamicMethodBody Ldsfld(FieldInfo fieldInfo)
        {
            return this.Emit(OpCodes.Ldsfld, fieldInfo);
        }

        public DynamicMethodBody Newobj(ConstructorInfo ctorInfo)
        {
            return this.Emit(OpCodes.Newobj, ctorInfo);
        }

        public DynamicMethodBody Newarr(Type type)
        {
            return this.Emit(OpCodes.Newarr, type);
        }

        public DynamicMethodBody Newarr(Type type, Number size)
        {
            return this
                .Emit(size)
                .Emit(OpCodes.Newarr, type);
        }

        public DynamicMethodBody Newobj<T>(params Type[] types)
        {
            var ci = typeof(T).GetConstructor(types);
            return this.Newobj(ci);
        }

        public DynamicMethodBody Call(MethodInfo methodInfo)
        {
            return this.Emit(OpCodes.Call, methodInfo);
        }

        public DynamicMethodBody Call<T>(string methodName, params Type[] types)
        {
            var mi = typeof(T).GetMethod(methodName, types);
            return this.Call(mi);
        }

        #region Basic Math Operations
        private void MultipleOperations(Func<DynamicMethodBody> action, params Number[] args)
        {
            this.Emit(args);
            if (args.Length == 1)
                action();
            else
                for (int i = 0; i < args.Length - 1; i++)
                    action();
        }


        public DynamicMethodBody Ret(bool returnValue)
        {
            return this
                .Ldc(returnValue ? 1 : 0)
                .Ret();
        }

        public DynamicMethodBody Rem(params Number[] args)
        {
            this.MultipleOperations(this.Rem, args);
            return this;
        }


        public DynamicMethodBody Add(params Number[] args)
        {
            this.MultipleOperations(this.Add, args);
            return this;
        }

        public DynamicMethodBody Add(Number arg)
        {
            return this.Emit(arg).Add();
        }



        public DynamicMethodBody Mul(params Number[] args)
        {
            if (args.Length == 1 && args[0] is ConstantDoubleNumber)
            {
                double factor = (args[0] as ConstantDoubleNumber).Value;
                if (factor == 1)
                    return this;
                if (factor == -1)
                    return this.Neg();
                return
                    this.LdcR8(factor).Mul();
            }
            else
            {
                this.MultipleOperations(this.Mul, args);
                return this;
            }
        }


        public DynamicMethodBody Div(params Number[] args)
        {
            this.MultipleOperations(this.Div, args);
            return this;
        }


        public DynamicMethodBody Sub(params Number[] args)
        {
            this.MultipleOperations(this.Sub, args);
            return this;
        }
        #endregion

        #region Locals (variables)

        public int GetVariableIndex(string varname)
        {
            var variables = _Info.Variables.ToArray();

            for (int i = 0; i < variables.Length; i++)
                if (variables[i].Name == varname)
                    return i;

            return -1;
        }

        public int GetParameterIndex(string parametername)
        {
            var parameters = _Info.Parameters.ToArray();

            for (int i = 0; i < parameters.Length; i++)
                if (parameters[i].Name == parametername)
                    return i;

            return -1;
        }


        public DynamicMethodBody Ldloc(params uint[] args)
        {
            foreach (var arg in args)
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
                        Emit(OpCodes.Ldloc, (int)arg);
                        break;

                }
            }
            return this;
        }

        public DynamicMethodBody Stloc(params uint[] args)
        {
            foreach (var arg in args)
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
                        Emit(OpCodes.Stloc, (int)arg);
                        break;

                }
            }
            return this;
        }

        public DynamicMethodBody Ldloc(params string[] args)
        {
            foreach (var arg in args)
                Ldloc((uint)GetVariableIndex(arg));

            return this;
        }

        public DynamicMethodBody Stloc(params string[] args)
        {
            var variables = _Info.Variables.ToArray();

            foreach (var arg in args)
                Stloc((uint)GetVariableIndex(arg));

            return this;
        }
        #endregion

        #region Arguments (Parameters)

        public DynamicMethodBody Ldarg(params uint[] args)
        {
            foreach (var arg in args)
            {
                switch (arg)
                {
                    case 0:
                        Emit(OpCodes.Ldarg_0);
                        break;
                    case 1:
                        Emit(OpCodes.Ldarg_1);
                        break;
                    case 2:
                        Emit(OpCodes.Ldarg_2);
                        break;
                    case 3:
                        Emit(OpCodes.Ldarg_3);
                        break;
                    default:
                        Emit(OpCodes.Ldarg_S, (int)arg);
                        break;

                }
            }
            return this;
        }

        public DynamicMethodBody Ldarg(params string[] args)
        {
            var parameters = _Info.Parameters.ToArray();
            uint offset = (uint)(_Info.DynamicTypeInfo != null ? 1 : 0);

            foreach (var arg in args)
                for (uint i = 0; i < parameters.Length; i++)
                    if (parameters[i].Name == arg)
                        Ldarg(i + offset);

            return this;
        }
        #endregion

        #region Constants
        public DynamicMethodBody Ldc(params string[] args)
        {
            return this.Ldstr(args);
        }

        public DynamicMethodBody Ldstr(params string[] args)
        {
            foreach (var arg in args)
            {
                Emit(OpCodes.Ldstr, arg);
            }
            return this;
        }

        public DynamicMethodBody Ldc(params double[] args)
        {
            return this.LdcR8(args);
        }

        public DynamicMethodBody LdcR8(params double[] args)
        {
            for (int i = 0; i < args.Length; i++)
                Emit(OpCodes.Ldc_R8, args[i]);

            return this;
        }

        public DynamicMethodBody Ldc(params float[] args)
        {
            return this.LdcR4(args);
        }

        public DynamicMethodBody LdcR4(params float[] args)
        {
            for (int i = 0; i < args.Length; i++)
                Emit(OpCodes.Ldc_R4, args[i]);

            return this;
        }

        public DynamicMethodBody Ldc(params int[] args)
        {
            return this.LdcI4(args);
        }

        public DynamicMethodBody LdLocOrArg(string name)
        {
            if (GetVariableIndex(name) > -1)
                return this.Ldloc(name);
            else if (GetParameterIndex(name) > -1)
                return this.Ldarg(name);
            else
            {
                return this
                    .Ldarg(0)
                    .Ldfld(name);
            }
        }

        public DynamicMethodBody LdArgOrLoc(string name)
        {
            return this.LdLocOrArg(name);
        }

        public DynamicMethodBody LdcI4(params int[] args)
        {
            foreach (var arg in args)
            {
                switch (arg)
                {
                    case 0:
                        Emit(OpCodes.Ldc_I4_0);
                        break;
                    case 1:
                        Emit(OpCodes.Ldc_I4_1);
                        break;
                    case 2:
                        Emit(OpCodes.Ldc_I4_2);
                        break;
                    case 3:
                        Emit(OpCodes.Ldc_I4_3);
                        break;
                    case 4:
                        Emit(OpCodes.Ldc_I4_4);
                        break;
                    case 5:
                        Emit(OpCodes.Ldc_I4_5);
                        break;
                    case 6:
                        Emit(OpCodes.Ldc_I4_6);
                        break;
                    case 7:
                        Emit(OpCodes.Ldc_I4_7);
                        break;
                    case 8:
                        Emit(OpCodes.Ldc_I4_8);
                        break;
                    case -1:
                        Emit(OpCodes.Ldc_I4_M1);
                        break;
                    default:
                        Emit(OpCodes.Ldc_I4, arg);
                        break;

                }
            }
            return this;
        }
        #endregion

        #region Labels
        public DynamicMethodBody MarkLabel(Label label)
        {
            Debug.Print("IL_{0}:", label.GetHashCode());

            _Info.GetILGenerator()
                .MarkLabel(label);

            return this;
        }

        public DynamicMethodBody MarkLabel(string label)
        {
            var lbl = GetLabel(label);
            Debug.Print("IL_{0}:", lbl.GetHashCode());

            _Info.GetILGenerator()
                .MarkLabel(GetLabel(label));

            return this;
        }

        readonly Dictionary<string, Label> _Labels = new Dictionary<string, Label>();
        Label GetLabel(string label)
        {
            if (!_Labels.ContainsKey(label))
                _Labels.Add(label, _Info.GetILGenerator().DefineLabel());

            return _Labels[label];
        }
        #endregion

        #region For..Next
        readonly Stack<ForInfo> _Fors = new Stack<ForInfo>();

        public DynamicMethodBody Emit(params Number[] numbers)
        {
            foreach (var number in numbers)
                number.Emit(this);
            return this;
        }


        public DynamicMethodBody For(string variable, Number from, Number to, int step = 1)
        {

            var ilgen = this._Info.GetILGenerator();
            var beginLabel = ilgen.DefineLabel();
            var comparasionLabel = ilgen.DefineLabel();

            _Fors.Push(new ForInfo(variable, from, to, step,
                beginLabel, comparasionLabel));
            if (GetVariableIndex(variable) == -1)
            {
                this._Info.WithVariable(typeof(int), variable);
                ilgen.DeclareLocal(typeof(int));
            }

            this
                .Emit(from)
                .Stloc(variable)
                .Br(comparasionLabel)
                .MarkLabel(beginLabel);

            return this;
        }

        public DynamicMethodBody Next()
        {
            var f = _Fors.Pop();
            this
                .Ldloc(f.Variable)
                .Ldc(f.Step)
                .Add()
                .Stloc(f.Variable)
                .MarkLabel(f.ComparasionLabel)
                .Ldloc(f.Variable)
                .Emit(f.To);

            if (f.Step > 0)
                this.Ble(f.BeginLabel);
            else
                this.Bge(f.BeginLabel);

            return this;
        }
        #endregion

        #region Abs
        public DynamicMethodBody AbsR8()
        {
            return this
                .Dup()
                .Iflt(0.0)
                    .Neg()
                .EndIf();
        }

        public DynamicMethodBody AbsI4()
        {
            return this
                .Dup()
                .Iflt(0)
                    .Neg()
                .EndIf();
        }
        #endregion

        public DynamicMethodBody Expression(Expression expression)
        {
            expression = new ExpressionSimplifierVisitor().Visit(expression);
            new ILEmitterVisitor(this).Visit(
                expression
                );
            return this;
        }

        #region extended Stloc
        public DynamicMethodBody Stloc(Number value, params string[] variables)
        {
            this.Emit(value);

            for (int i = 1; i < variables.Length; i++)
                this.Dup();

            this.Stloc(variables);

            return this;
        }

        #endregion

        #region AddToVar
        public DynamicMethodBody AddToVar(string varname, Number constant)
        {
            return this
                .Ldloc(varname)
                .Add(constant)
                .Stloc(varname);
        }

        public DynamicMethodBody AddToVar(string varname)
        {
            return this
                .Ldloc(varname)
                .Add()
                .Stloc(varname);
        }
        #endregion

        #region EnsureLimits
        public DynamicMethodBody EnsureLimits(Number min, Number max)
        {
            return this
                .Dup()
                .Emit(min)
                .Iflt()
                    .Pop()
                    .Emit(min)
                .Else()
                    .Dup()
                    .Emit(max)
                    .Ifgt()
                        .Pop()
                        .Emit(max)
                    .EndIf()
                .EndIf();
        }
        #endregion

        #region static
        public static implicit operator DynamicMethod(DynamicMethodBody body)
        {
            return body._Info;
        }

        public static implicit operator DynamicMethodInfo(DynamicMethodBody body)
        {
            return body._Info;
        }
        #endregion

        public DynamicMethodBody Repeater(int from, int to, int step,
            Action<int, DynamicMethodBody> action
            )
        {
            for (int i = from; i <= to; i += step)
                action(i, this);

            return this;
        }

        public DynamicMethodBody Repeater(int from, int to, int step,
            Func<int, DynamicMethodBody, bool> precondition,
            Action<int, DynamicMethodBody> action
            )
        {
            for (int i = from; i <= to; i += step)
                if (precondition(i, this))
                    action(i, this);

            return this;
        }


        public DynamicMethodBody EmitIf(bool condition, Action<DynamicMethodBody> action)
        {
            if (condition)
                action(this);

            return this;
        }

        public object Invoke(params object[] args)
        {
            return _Info.AsDynamicMethod.Invoke(null, args);
        }


    }
}
