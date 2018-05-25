using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Transmuter;

namespace MetaTypes.Mapping
{

    public static class Rules
    {
        public static Mapper<MetaValue> CreateMapper()
        {
            var binder = new Mapper<MetaValue>();
            binder.Rules.Add((nameof(MetaScalarsAreMapped), MetaScalarsAreMapped()));
            binder.Rules.Add((nameof(CollectionsAreMappedToMetaArrays), CollectionsAreMappedToMetaArrays(binder)));
            binder.Rules.Add((nameof(ObjectsAreDecomposed), ObjectsAreDecomposed(binder)));
            return binder;
        }

        public static Mapper<MetaValue>.ToRule MetaScalarsAreMapped() => o =>
            o == null ? (Mapper<MetaValue>.RuleOutput) (MetaValue) MetaScalar.From(new MetaNull()) :
            (o is string str) ? ((Mapper<MetaValue>.RuleOutput)(MetaValue)MetaScalar.From(str)) :
            (o is DateTimeOffset dt) ? ((Mapper<MetaValue>.RuleOutput)(MetaValue)MetaScalar.From(dt)) :
            (o is decimal m) ? ((Mapper<MetaValue>.RuleOutput)(MetaValue)MetaScalar.From(m)) :
            (o is float f) ? ((Mapper<MetaValue>.RuleOutput)(MetaValue)MetaScalar.From(f)) :
            (o is double d) ? ((Mapper<MetaValue>.RuleOutput)(MetaValue)MetaScalar.From(d)) :
            (o is int i) ? ((Mapper<MetaValue>.RuleOutput) (MetaValue) MetaScalar.From(i)) : new NA();

        public static Mapper<MetaValue>.ToRule CollectionsAreMappedToMetaArrays(Mapper<MetaValue> binder) => o =>
        {
            if (o is ICollection c)
            {
                var items = c.Cast<object>().Select(binder.Map).Where(ob => ob.IsT0).Select(x => x.Match(mv => mv, na => null)).ToArray();
                return ((Mapper<MetaValue>.RuleOutput)(MetaValue)MetaArray.From(items));
            }
            return new NA();
        };


        public static Mapper<MetaValue>.ToRule ObjectsAreDecomposed(Mapper<MetaValue> binder)
        {
            object FromMeta(MetaValue target)
            {
                return target.Value.Match<object>(
                    metaObject => throw new NotImplementedException(),
                    metaValue => metaValue.Value.Value,
                    metaArray => throw new NotImplementedException()
                );
            }

            MetaObject ToMetaObject(object target)
            {
                MetaType ToMetaType(Type type)
                {
                    if (type == typeof(object))
                        throw new Exception();

                    return new MetaType
                    {
                        Name = type.GetTypeInfo().IsGenericType && !type.IsConstructedGenericType ? null : MetaName.From(type.FullName),
                        GenericType = type.GetTypeInfo().IsGenericType && !type.IsConstructedGenericType ? ToMetaType(type.GetGenericTypeDefinition()) : null,
                        GenericArguments = type.GetTypeInfo().IsGenericType && !type.IsConstructedGenericType ? type.GetTypeInfo().GenericTypeArguments.Select(ToMetaType).ToArray() : null
                    };
                }
                MetaParameter ToParam(ParameterInfo arg) => new MetaParameter()
                {
                    Name = MetaName.From(arg.Name),
                    Type = ToMetaType(arg.ParameterType)
                };
                MetaResult ToResult(Func<object> execute)
                {
                    try
                    {
                        var result = execute();
                        return MetaResult.From(binder.Map(result).AsT0);
                    }
                    catch (Exception ex)
                    {
                        return MetaResult.From(MetaError.From(ex.Message));
                    }
                }
                MetaAction ToAction(MethodInfo method) => new MetaAction()
                {
                    Name = MetaName.From(method.Name),
                    Call = args =>
                        ToResult(() => method.Invoke(target, args.Select(arg => FromMeta(arg.Value)).ToArray())),
                    Parameters = method.GetParameters().Select(ToParam)
                };
                MetaProperty ToProperty(PropertyInfo propertyInfo) => new MetaProperty()
                {
                    Name = MetaName.From(propertyInfo.Name),
                    Type = ToMetaType(propertyInfo.PropertyType),
                    GetValue = !propertyInfo.CanRead ? null as Func<MetaValue> : () =>
                    {
                        var value = propertyInfo.GetValue(target);
                        return value == null ? MetaScalar.From(new MetaNull()) : binder.Map(value).AsT0;
                    },
                    SetValue = !propertyInfo.CanWrite ? null as Action<MetaValue> : (arg) =>
                    {
                        propertyInfo.SetValue(target, FromMeta(arg.Value));
                    },
                    
                };
                return new MetaObject()
                {
                    Type = ToMetaType(target.GetType()),
                    Properties = target.GetType().GetTypeInfo().DeclaredProperties.Where(t => !t.IsSpecialName && t.DeclaringType == target.GetType()).Select(ToProperty).ToArray(),
                    Actions = target.GetType().GetTypeInfo().DeclaredMethods.Where(t => !t.Name.StartsWith("<") && !t.Name.StartsWith("get_") && !t.Name.StartsWith("set_") && t.DeclaringType == target.GetType()).Select(ToAction).ToArray()
                };
            }
            return target => (Mapper<MetaValue>.RuleOutput)(MetaValue)ToMetaObject(target);
        }
    }
}
