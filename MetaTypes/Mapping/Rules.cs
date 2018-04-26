using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace MetaTypes.Mapping
{
    public static class Rules
    {
        public static Mapper<MetaValue> CreateWithDefaultRules()
        {
            var binder = new Mapper<MetaValue>();
            binder.AddRule(Rules.StringsAreMappedToMetaScalars());
            binder.AddRule(Rules.CollectionsAreMappedToMetaArrays(binder));
            binder.AddRule(Rules.ObjectsAreDecomposed(binder));
            return binder;
        }

        public static Func<string, MetaValue> StringsAreMappedToMetaScalars() => c => MetaScalar.From(c);

        public static Func<ICollection, MetaValue> CollectionsAreMappedToMetaArrays(Mapper<MetaValue> binder) =>
            c => MetaArray.From(c.Cast<object>().Select(binder.Map).SelectMany(x => x));


        public static Func<object, MetaValue> ObjectsAreDecomposed(Mapper<MetaValue> binder)
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
                    GetValue = () => binder.Map(propertyInfo.GetValue(target)).Single()
                };
                return new MetaObject()
                {
                    Type = ToMetaType(target.GetType()),
                    Properties = target.GetType().GetTypeInfo().DeclaredProperties.Where(t => !t.IsSpecialName && t.DeclaringType == target.GetType()).Select(ToProperty).ToArray(),
                    Actions = target.GetType().GetTypeInfo().DeclaredMethods.Where(t => !t.IsSpecialName && t.DeclaringType == target.GetType()).Select(ToAction).ToArray()
                };
            }
            return target => ToMetaObject(target);
        }
    }
}