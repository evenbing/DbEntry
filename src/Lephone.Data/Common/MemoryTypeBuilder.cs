﻿using System;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Collections;
using Lephone.Util;
using Lephone.Data.Definition;

namespace Lephone.Data.Common
{
    public enum FieldType
    {
        Normal,
        HasOne,
        HasMany,
        BelongsTo,
        HasAndBelongsToMany,
        LazyLoad
    }

    public class MemoryTypeBuilder
    {
        public delegate void EmitCode(ILBuilder il);

        private const MethodAttributes OverrideFlag = MethodAttributes.Public |
            MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual;

        public static readonly string MemberPrifix = "$";

        private readonly TypeBuilder InnerType;

        public MemoryTypeBuilder(TypeBuilder builder)
        {
            InnerType = builder;
        }

        public void DefineDefaultConstructor(MethodAttributes attr)
        {
            InnerType.DefineDefaultConstructor(attr);
        }

        public void DefineConstructor(MethodAttributes attr, ConstructorInfo ci, MethodInfo minit, List<MemberHandler> impRelations)
        {
            ParameterInfo[] pis = ci.GetParameters();

            var al = new ArrayList();
            foreach (ParameterInfo pi in pis)
            {
                al.Add(pi.ParameterType);
            }
            var ts = (Type[])al.ToArray(typeof(Type));

            ConstructorBuilder cb = InnerType.DefineConstructor(attr,
                CallingConventions.ExplicitThis | CallingConventions.HasThis, ts);
            var il = new ILBuilder(cb.GetILGenerator());
            // call base consructor
            il.LoadArg(0).LoadArgShort(1, ts.Length).Call(ci);
            // create relation fields.
            foreach (MemberHandler h in impRelations)
            {
                il.LoadArg(0).LoadArg(0);
                ConstructorInfo ci1;
                if (h.IsHasOne || h.IsHasMany || h.IsHasAndBelongsToMany)
                {
                    ci1 = h.FieldType.GetConstructor(new[] { typeof(object), typeof(string) });
                    if (string.IsNullOrEmpty(h.OrderByString))
                    {
                        il.LoadNull();
                    }
                    else
                    {
                        il.LoadString(h.OrderByString);
                    }
                }
                else
                {
                    ci1 = h.FieldType.GetConstructor(new[] { typeof(object) });
                }
                il.NewObj(ci1);
                h.MemberInfo.EmitSet(il);
            }
            // call m_InitUpdateColumns
            if (minit != null)
            {
                il.LoadArg(0).Call(minit);
            }
            // return
            il.Return();
        }

        public Type CreateType()
        {
            return InnerType.CreateType();
        }

        public static FieldType GetFieldType(PropertyInfo pi)
        {
            foreach (Attribute a in pi.GetCustomAttributes(true))
            {
                if (a is HasOneAttribute)
                {
                    return FieldType.HasOne;
                }
                if (a is BelongsToAttribute)
                {
                    return FieldType.BelongsTo;
                }
                if (a is HasManyAttribute)
                {
                    return FieldType.HasMany;
                }
                if (a is HasAndBelongsToManyAttribute)
                {
                    return FieldType.HasAndBelongsToMany;
                }
                if (a is LazyLoadAttribute)
                {
                    return FieldType.LazyLoad;
                }
            }
            return FieldType.Normal;
        }

        private static readonly ConstructorInfo CrossTableNameAttributeConstructor
            = typeof(CrossTableNameAttribute).GetConstructor(new[] { typeof(string) });

        private static readonly ConstructorInfo DbColumnAttributeConstructor
            = typeof(DbColumnAttribute).GetConstructor(new[] { typeof(string) });

        private static readonly ConstructorInfo AllowNullAttributeConstructor
            = typeof(AllowNullAttribute).GetConstructor(new Type[] {});

        private static readonly ConstructorInfo LengthAttributeConstructor
            = typeof(LengthAttribute).GetConstructor(new[] { typeof(int), typeof(int) });

        private static readonly ConstructorInfo SpecialNameAttributeConstructor
            = typeof(SpecialNameAttribute).GetConstructor(new Type[] {});

        private static CustomAttributeBuilder GetDbColumnBuilder(string Name)
        {
            return new CustomAttributeBuilder(DbColumnAttributeConstructor, new object[] { Name });
        }

        private static CustomAttributeBuilder GetAllowNullBuilder()
        {
            return new CustomAttributeBuilder(AllowNullAttributeConstructor, new object[] { });
        }

        private static CustomAttributeBuilder GetLengthBuilder(LengthAttribute o)
        {
            Type t = typeof (LengthAttribute);
            return new CustomAttributeBuilder(LengthAttributeConstructor, new object[] { o.Min, o.Max },
                new [] {t.GetField("ErrorMessage")}, new object[] { o.ErrorMessage } );
        }

        private static CustomAttributeBuilder GetStringColumnBuilder(StringColumnAttribute o)
        {
            Type t = typeof(StringColumnAttribute);
            return new CustomAttributeBuilder(t.GetConstructor(new Type[] { }), new object[] { },
                new [] { t.GetField("IsUnicode"), t.GetField("Regular"), t.GetField("ErrorMessage") },
                new object[] { o.IsUnicode, o.Regular, o.ErrorMessage });
        }

        private static CustomAttributeBuilder GetIndexBuilder(IndexAttribute o)
        {
            Type t = typeof(IndexAttribute);
            return new CustomAttributeBuilder(t.GetConstructor(new Type[] { }), new object[] { },
                new [] { t.GetField("ASC"), t.GetField("IndexName"), t.GetField("UNIQUE"), t.GetField("UniqueErrorMessage") },
                new object[] { o.ASC, o.IndexName, o.UNIQUE, o.UniqueErrorMessage });
        }

        private static CustomAttributeBuilder GetSpecialNameBuilder()
        {
            return new CustomAttributeBuilder(SpecialNameAttributeConstructor, new object[] { });
        }

        private FieldInfo DefineField(string Name, Type PropertyType, FieldType ft, PropertyInfo pi)
        {
            if (ft == FieldType.Normal)
            {
                return InnerType.DefineField(Name, PropertyType, FieldAttributes.Private);
            }
            Type t = GetRealType(PropertyType, ft);
            FieldBuilder fb = InnerType.DefineField(Name, t, FieldAttributes.FamORAssem);
            var bs = (DbColumnAttribute[])pi.GetCustomAttributes(typeof(DbColumnAttribute), true);
            if (bs != null && bs.Length > 0)
            {
                fb.SetCustomAttribute(GetDbColumnBuilder(bs[0].Name));
            }
            else if (ft == FieldType.LazyLoad)
            {
                fb.SetCustomAttribute(GetDbColumnBuilder(pi.Name));
            }
            if (ft == FieldType.HasAndBelongsToMany)
            {
                var mm = ClassHelper.GetAttribute<HasAndBelongsToManyAttribute>(pi, false);
                if(!string.IsNullOrEmpty(mm.CrossTableName))
                {
                    fb.SetCustomAttribute(new CustomAttributeBuilder(CrossTableNameAttributeConstructor, new object[] { mm.CrossTableName }));
                }
            }
            ProcessCustomAttribute<AllowNullAttribute>(pi, o => fb.SetCustomAttribute(GetAllowNullBuilder()));
            ProcessCustomAttribute<LengthAttribute>(pi, o => fb.SetCustomAttribute(GetLengthBuilder(o)));
            ProcessCustomAttribute<StringColumnAttribute>(pi, o => fb.SetCustomAttribute(GetStringColumnBuilder(o)));
            ProcessCustomAttribute<IndexAttribute>(pi, o => fb.SetCustomAttribute(GetIndexBuilder(o)));
            if (ft == FieldType.LazyLoad)
            {
                if (ClassHelper.HasAttribute<SpecialNameAttribute>(pi, true))
                {
                    ProcessCustomAttribute<SpecialNameAttribute>(pi, o => fb.SetCustomAttribute(GetSpecialNameBuilder()));
                }
            }
            return fb;
        }

        private static Type GetRealType(Type PropertyType, FieldType ft)
        {
            Type t;
            switch (ft)
            {
                case FieldType.HasOne:
                    t = typeof(HasOne<>);
                    t = t.MakeGenericType(PropertyType);
                    break;
                case FieldType.HasMany:
                    t = typeof(HasMany<>);
                    t = t.MakeGenericType(PropertyType.GetGenericArguments()[0]);
                    break;
                case FieldType.BelongsTo:
                    t = typeof(BelongsTo<>);
                    t = t.MakeGenericType(PropertyType);
                    break;
                case FieldType.HasAndBelongsToMany:
                    t = typeof(HasAndBelongsToMany<>);
                    t = t.MakeGenericType(PropertyType.GetGenericArguments()[0]);
                    break;
                case FieldType.LazyLoad:
                    t = typeof(LazyLoadField<>);
                    t = t.MakeGenericType(PropertyType);
                    break;
                default:
                    throw new DataException("Impossible");
            }
            return t;
        }

        private static void ProcessCustomAttribute<T>(ICustomAttributeProvider pi, CallbackObjectHandler<T> callback)
        {
            object[] bs = pi.GetCustomAttributes(typeof(T), true);
            if (bs != null && bs.Length > 0)
            {
                foreach(var b in bs)
                {
                    callback((T)b);
                }
            }
        }

        public MemberHandler ImplProperty(Type OriginType, MethodInfo mupdate, PropertyInfo pi)
        {
            string PropertyName = pi.Name;
            Type PropertyType = pi.PropertyType;

            string GetPropertyName = "get_" + PropertyName;
            string SetPropertyName = "set_" + PropertyName;

            FieldType ft = GetFieldType(pi);
            FieldInfo fi = DefineField(MemberPrifix + PropertyName, PropertyType, ft, pi);

            OverrideMethod(OverrideFlag, GetPropertyName, OriginType, PropertyType, null, delegate(ILBuilder il)
            {
                il.LoadField(fi);
                if (ft == FieldType.BelongsTo || ft == FieldType.HasOne || ft == FieldType.LazyLoad)
                {
                    MethodInfo getValue = fi.FieldType.GetMethod("get_Value", ClassHelper.InstancePublic);
                    il.CallVirtual(getValue);
                }
            });

            OverrideMethod(OverrideFlag, SetPropertyName, OriginType, null, new[] { PropertyType }, delegate(ILBuilder il)
            {
                if (ft == FieldType.HasOne || ft == FieldType.BelongsTo || ft == FieldType.LazyLoad)
                {
                    il.LoadField(fi).LoadArg(1);
                    MethodInfo setValue = fi.FieldType.GetMethod("set_Value", ClassHelper.InstancePublic);
                    il.CallVirtual(setValue);
                }
                else if (ft == FieldType.Normal)
                {
                    il.LoadArg(1).SetField(fi);
                }
                if (ft == FieldType.LazyLoad || ft == FieldType.Normal)
                {
                    if (mupdate != null)
                    {
                        string cn = ObjectInfo.GetColumuName(new MemberAdapter.PropertyAdapter(pi));
                        il.LoadArg(0).LoadString(cn).Call(mupdate);
                    }
                }
            });
            if (ft != FieldType.Normal)
            {
                return MemberHandler.NewObject(fi, ft, pi);
            }
            return null;
        }

        public void OverrideMethod(MethodAttributes flag, string MethodName, Type OriginType, Type returnType, Type[] paramTypes, EmitCode emitCode)
        {
            MethodBuilder mb = DefineMethod(flag, MethodName, returnType, paramTypes, emitCode);
            MethodInfo mi = (paramTypes == null) ? OriginType.GetMethod(MethodName) : OriginType.GetMethod(MethodName, paramTypes);
            InnerType.DefineMethodOverride(mb, mi);
        }

        public void OverrideMethodDirect(MethodAttributes flag, string MethodName, Type OriginType, Type returnType, Type[] paramTypes, EmitCode emitCode)
        {
            MethodBuilder mb = DefineMethodDirect(flag, MethodName, returnType, paramTypes, emitCode);
            MethodInfo mi = OriginType.GetMethod(MethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            InnerType.DefineMethodOverride(mb, mi);
        }

        public MethodBuilder DefineMethod(MethodAttributes flag, string MethodName, Type returnType, Type[] paramTypes, EmitCode emitCode)
        {
            return DefineMethodDirect(flag, MethodName, returnType, paramTypes, delegate(ILBuilder il)
            {
                il.LoadArg(0);
                emitCode(il);
            });
        }

        public MethodBuilder DefineMethodDirect(MethodAttributes flag, string MethodName, Type returnType, Type[] paramTypes, EmitCode emitCode)
        {
            MethodBuilder mb = InnerType.DefineMethod(MethodName, flag, returnType, paramTypes);
            var il = new ILBuilder(mb.GetILGenerator());
            emitCode(il);
            il.Return();
            return mb;
        }

        public void ImplInitialize(Type type, MethodInfo info)
        {
            var ptypes = new List<Type>();
            var pis = info.GetParameters();
            foreach (var parameter in pis)
            {
                ptypes.Add(parameter.ParameterType);
            }

            OverrideMethod(OverrideFlag, info.Name, type, info.ReturnType, ptypes.ToArray(),
               delegate(ILBuilder il)
               {
                   if(ptypes.Count == 1 && ptypes[0] == info.ReturnType)
                   {
                       var ps = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                       for (int i = 0; i < ps.Length; i++)
                       {
                           var pi = ps[i];
                           il.LoadArg(0);
                           il.LoadArg(1);
                           il.CallVirtual(pi.GetGetMethod());
                           il.CallVirtual(pi.GetSetMethod());
                       }
                   }
                   else
                   {
                       for (int i = 0; i < pis.Length; i++)
                       {
                           var pi = type.GetProperty(pis[i].Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                           il.LoadArg(0);
                           il.LoadArg(i + 1);
                           il.CallVirtual(pi.GetSetMethod());
                       }
                   }
                   il.Return();
               });
        }
    }
}
