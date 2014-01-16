using NiL.JS.Core.BaseTypes;
using NiL.JS.Core.Modules;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace NiL.JS.Core
{
    public sealed class TypeProxy : JSObject
    {
        private static object[] convertArgs(JSObject[] source)
        {
            if (source == null)
                return null;
            object[] res = new object[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == null)
                    continue;
                var obj = source[i];
                var v = obj.Value;
                if (v is Core.BaseTypes.Array)
                {
                    var arr = v as Core.BaseTypes.Array;
                    var arg = new object[arr.length];
                    for (var j = 0; j < arg.Length; j++)
                        arg[j] = arr[j].Value;
                    res[i] = arg;
                }
                else
                    res[i] = v;
            }
            return res;
        }

        private static object[] convertArgs(JSObject[] source, ParameterInfo[] targetTypes)
        {
            if (targetTypes.Length == 0)
                return null;
            if (targetTypes.Length == 1 && targetTypes[0].ParameterType == typeof(JSObject[]))
                return new object[] { source };
            int targetCount = targetTypes.Length;
            object[] res = new object[targetCount];
            if (source != null)
                targetCount = System.Math.Min(targetCount, source.Length);
            for (int i = 0; i < targetCount; i++)
            {
                if (source == null || source[i] == null)
                    continue;
                var obj = source[i];
                if (targetTypes[i].ParameterType == typeof(JSObject))
                    res[i] = obj;
                else
                {
                    var v = obj.Value;
                    if (v is Core.BaseTypes.Array)
                    {
                        var arr = v as Core.BaseTypes.Array;
                        var arg = new object[arr.length];
                        for (var j = 0; j < arg.Length; j++)
                            arg[j] = arr[j].Value;
                        res[i] = arg;
                    }
                    else
                        res[i] = v;
                }
            }
            return res;
        }

        private static readonly Dictionary<Type, TypeProxy> constructors = new Dictionary<Type, TypeProxy>();
        private static readonly Dictionary<Type, TypeProxy> prototypes = new Dictionary<Type, TypeProxy>();

        public static JSObject Proxy(object obj)
        {
            if (obj == null)
                return JSObject.Null;
            else if (obj is JSObject)
                return obj as JSObject;
            else if (obj is int)
                return (int)obj;
            else if (obj is long)
                return (long)obj;
            else if (obj is double)
                return (double)obj;
            else if (obj is string)
                return (string)obj;
            else if (obj is bool)
                return (bool)obj;
            else if (obj is ContextStatement)
                return (JSObject)(ContextStatement)obj;
            else
            {
                var type = obj.GetType();
                var res = new JSObject(false) { oValue = obj, ValueType = ObjectValueType.Object };
                res.GetField("constructor", false, true).Assign(GetConstructor(type));
                res.prototype = GetPrototype(type);
                return res;
            }
        }

        public static JSObject GetPrototype(Type type)
        {
            TypeProxy prot = null;
            if (!prototypes.TryGetValue(type, out prot))
            {
                new TypeProxy(type);
                prot = prototypes[type];
            }
            return prot;
        }

        public static JSObject GetConstructor(Type type)
        {
            TypeProxy constructor = null;
            if (!constructors.TryGetValue(type, out constructor))
                constructor = new TypeProxy(type);
            return constructor;
        }

        private Type hostedType;
        [NonSerialized]
        private Dictionary<string, JSObject> cache;

        private TypeProxy(Type type, bool fictive)
        {
            hostedType = type;
            oValue = type;
            ValueType = ObjectValueType.Object;
            assignCallback = ErrorAssignCallback;
            cache = new Dictionary<string, JSObject>();
            prototype = BaseObject.Prototype;
        }

        public TypeProxy(Type type)
        {
            ValueType = ObjectValueType.Statement;
            hostedType = type;
            JSObject proto = null;
            TypeProxy exconst = null;
            assignCallback = ErrorAssignCallback;
            cache = new Dictionary<string, JSObject>();
            prototype = BaseObject.Prototype;
            if (constructors.TryGetValue(type, out exconst))
            {
                oValue = exconst.oValue;
                DefaultFieldGetter("prototype", false, false).Assign(prototypes[type]);
            }
            else
            {
                oValue = new ExternalFunction((x, y) =>
                {
                    object[] args = null;
                    ConstructorInfo constructor = null;
                    if (y == null || y.Length == 0)
                        constructor = hostedType.GetConstructor(Type.EmptyTypes);
                    else
                    {
                        Type[] argtypes = new[] { y.GetType() };
                        if (y.Length == 1)
                        {
                            argtypes[0] = y[0].GetType();
                            constructor = hostedType.GetConstructor(argtypes);
                            if (constructor == null)
                            {
                                var arg = y[0].Value;
                                argtypes[0] = arg.GetType();
                                constructor = hostedType.GetConstructor(argtypes);
                                if (constructor != null)
                                    args = new object[] { arg };
                            }
                            else
                                args = new object[] { y[0] };
                        }
                        if (constructor == null || y.Length > 1)
                        {
                            constructor = hostedType.GetConstructor(Type.GetTypeArray(y));
                            if (constructor == null)
                            {
                                argtypes[0] = y.GetType();
                                constructor = hostedType.GetConstructor(argtypes);
                                if (constructor == null)
                                    constructor = hostedType.GetConstructor(Type.EmptyTypes);
                                else
                                    args = new object[] { y };
                            }
                            else
                                args = y;
                        }
                    }
                    var _this = x.thisBind;
                    bool bynew = _this != null && _this.GetField("__proto__", true) != null && _this.GetField("__proto__", true).ValueType == ObjectValueType.Object && _this.GetField("__proto__", true).oValue == type as object;
                    var obj = constructor.Invoke(args);
                    var res = obj is JSObject && !bynew ? obj as JSObject : new JSObject(false)
                    {
                        oValue = obj,
                        ValueType = ObjectValueType.Object
                    };
                    if (!(res is Core.BaseTypes.EmbeddedType))
                        res.GetField("constructor").Assign(this);
                    res.GetField("__proto__").Assign(proto);
                    if (bynew)
                        _this.firstContainer = res;
                    return res;
                });
                constructors[type] = this;
                prototypes[type] = new TypeProxy(type, true);
                proto = DefaultFieldGetter("prototype", false, false);
                proto.Assign(prototypes[type]);
                proto.attributes |= ObjectAttributes.DontDelete | ObjectAttributes.DontEnum;
            }
        }

        private JSObject convert(MethodInfo method)
        {
            JSObject result = null;
            if (method.IsStatic)
            {
                if (method.ReturnType == typeof(JSObject))
                {
                    if ((method.GetParameters().Length == 1) && (method.GetParameters()[0].ParameterType == typeof(JSObject[])))
                    {
                        var dinv = (Func<JSObject[], JSObject>)Delegate.CreateDelegate(typeof(Func<JSObject[], JSObject>), null, method);
                        result = new CallableField((th, args) =>
                        {
                            return dinv(args);
                        });
                        return result;
                    }
                    else if (method.GetParameters().Length == 0)
                    {
                        var dinv = (Func<JSObject>)Delegate.CreateDelegate(typeof(Func<JSObject>), null, method);
                        result = new CallableField((th, args) =>
                        {
                            return dinv();
                        });
                        return result;
                    }
                }
            }
            result = new CallableField((context, args) =>
            {
                try
                {
                    return Proxy(method.Invoke(getTargetObject(context), convertArgs(args, method.GetParameters())));
                }
                catch (Exception e)
                {
                    throw e.InnerException ?? e;
                }
            });
            return result;
        }

        private object getTargetObject(Context context)
        {
            if (ValueType == ObjectValueType.Statement)
                return null;
            object obj = context.thisBind.firstContainer ?? context.thisBind;
            obj = obj is Core.BaseTypes.EmbeddedType ? obj : (obj as JSObject).oValue;
            return obj;
        }

        public override JSObject GetField(string name, bool fast, bool own)
        {
            JSObject r = null;
            if (cache.TryGetValue(name, out r) && r.ValueType >= ObjectValueType.Undefined)
                return r;
            var m = hostedType.GetMember(name, BindingFlags.Public | (ValueType == ObjectValueType.Statement ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            if (m.Length > 1)
                throw new InvalidOperationException("Too many fields with name " + name);
            if (m.Length == 0 || m[0].GetCustomAttributes(typeof(HiddenAttribute), true).Length != 0)
            {
                r = DefaultFieldGetter(name, fast, own);
                return r;
            }
            switch (m[0].MemberType)
            {
                case MemberTypes.Constructor:
                    {
                        var method = (ConstructorInfo)m[0];
                        r = new CallableField((th, args) =>
                        {
                            var res = method.Invoke(args);
                            if (res is JSObject)
                                return res as JSObject;
                            else if (res is int)
                                return (int)res;
                            else if (res is double || res is long)
                                return (double)res;
                            else if (res is string)
                                return (string)res;
                            else if (res is bool)
                                return (bool)res;
                            else if (res is ContextStatement)
                                return (JSObject)(ContextStatement)res;
                            else
                                return TypeProxy.Proxy(res);
                        });
                        break;
                    }
                case MemberTypes.Method:
                    {
                        var method = (MethodInfo)m[0];
                        r = convert(method);
                        break;
                    }
                case MemberTypes.Field:
                    {
                        var field = (m[0] as FieldInfo);
                        if (!field.IsStatic)
                            throw new NotSupportedException("Fields for instances not supported. Use properties.");
                        object res = field.GetValue(null);
                        if (res is JSObject)
                            r = res as JSObject;
                        else
                        {
                            if (res is int)
                                r = (int)res;
                            else if (res is double || res is long)
                                r = (double)res;
                            else if (res is string)
                                r = (string)res;
                            else if (res is bool)
                                r = (bool)res;
                            else if (res is ContextStatement)
                                r = (JSObject)(ContextStatement)res;
                            else
                                r = TypeProxy.Proxy(res);
                            r.assignCallback = null;
                        }
                        break;
                    }
                case MemberTypes.Property:
                    {
                        var pinfo = (PropertyInfo)m[0];
                        r = new JSObject()
                        {
                            ValueType = ObjectValueType.Property,
                            oValue = new Statement[] { 
                                    pinfo.CanWrite ? convert(pinfo.GetSetMethod()).oValue as Statement : null,
                                    pinfo.CanRead ? convert(pinfo.GetGetMethod()).oValue as Statement : null 
                                }
                        };
                        break;
                    }
                default: throw new NotImplementedException("Convertion from " + m[0].MemberType + " not implemented");
            }
            if (m[0].GetCustomAttributes(typeof(ProtectedAttribute), false).Length != 0)
                r.Protect();
            cache[name] = r;
            r.attributes |= ObjectAttributes.DontDelete | ObjectAttributes.DontEnum;
            return r;
        }
    }
}