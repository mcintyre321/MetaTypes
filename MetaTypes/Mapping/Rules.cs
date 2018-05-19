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
            binder.Rules.Add((nameof(StringsAreMappedToMetaScalars), StringsAreMappedToMetaScalars()));
            binder.Rules.Add((nameof(CollectionsAreMappedToMetaArrays), CollectionsAreMappedToMetaArrays(binder)));
            binder.Rules.Add((nameof(ObjectsAreDecomposed), ObjectsAreDecomposed(binder)));
            return binder;
        }

        public static Mapper<MetaValue>.ToRule StringsAreMappedToMetaScalars() => o =>
              (o is string str) ? ((Mapper<MetaValue>.RuleOutput)(MetaValue)MetaScalar.From(str)) : new NA();

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
                        execute();
                        return MetaResult.From(true);
                    }
                    catch (Exception ex)
                    {
                        return MetaResult.From(false);
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
                    GetValue = () =>
                    {
                        var value = propertyInfo.GetValue(target);
                        if (value is string str) return MetaScalar.From(str);
                        return null;
                    }
                };
                return new MetaObject()
                {
                    Type = ToMetaType(target.GetType()),
                    Properties = target.GetType().GetTypeInfo().DeclaredProperties.Where(t => !t.IsSpecialName && t.DeclaringType == target.GetType()).Select(ToProperty).ToArray(),
                    Actions = target.GetType().GetTypeInfo().DeclaredMethods.Where(t => !t.IsSpecialName && t.DeclaringType == target.GetType()).Select(ToAction).ToArray()
                };
            }
            return target => (Mapper<MetaValue>.RuleOutput)(MetaValue)ToMetaObject(target);
        }
    }
}