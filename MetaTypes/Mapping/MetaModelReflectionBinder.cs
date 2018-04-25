using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OneOf.Types;
using ValueOf;
using OneOf;

namespace MetaTypes.Mapping
{
    public class MetaModelReflectionBinder
    {
        class FromRuleArgs : ValueOf<MetaValue, FromRuleArgs> { }
        class FromMetaRule : ValueOf<Func<FromRuleArgs, OneOf<object, OneOf.Types.None>>, FromMetaRule> { }

        class ToRuleArgs : ValueOf<(Type targetType, object target), ToRuleArgs> { }
        class ToMetaRule : ValueOf<Func<ToRuleArgs, OneOf<MetaValue, OneOf.Types.None>>, ToMetaRule> { }

        List<ToMetaRule> rules = new List<ToMetaRule>();

        public void AddRule<T>(Func<T, MetaValue> map)
            => rules.Add(ToMetaRule.From(args => typeof(T).GetTypeInfo().IsAssignableFrom(args.Value.targetType.GetTypeInfo())
                ? (OneOf.OneOf<MetaValue, None>)map((T)args.Value.target)
                : new None()));

        public static MetaModelReflectionBinder CreateWithDefaultRules()
        {
            var binder = new MetaModelReflectionBinder();
            binder.AddRule(Rules.StringsAreMappedToMetaScalars());
            binder.AddRule(Rules.CollectionsAreMappedToMetaArrays(binder));
            binder.AddRule(Rules.ObjectsAreDecomposed(binder));
            return binder;
        }

        public MetaModelReflectionBinder()
        {
            
        }

        public OneOf<MetaValue, NoMapping> ToMetaValue(object target)
        {
            var targetType = target.GetType();
            var ruleArgs = ToRuleArgs.From((targetType, target));
            foreach (var rule in rules)
            {
                var ruleResult = rule.Value.Invoke(ruleArgs);
                if (ruleResult.TryPickT0(out var mv, out var none))
                    return mv;
            }

            return new NoMapping();


        }
    }

    public struct NoMapping
    {
    }
}
