using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using ValueOf;
using OneOf;

namespace MetaTypes.Mapping
{
    public class Mapper<TOut>
    {
        public class ToRuleArgs : ValueOf<(Type targetType, object target), ToRuleArgs>
        {
        }

        public class ToRule : ValueOf<Func<ToRuleArgs, OneOf<TOut, TOut[], NotApplicable>>, ToRule>
        {
        }

        public IList<ToRule> Rules { get; } = new List<ToRule>();

        public void AddRule<T>(Func<T, TOut> map)
            => Rules.Add(ToRule.From(args => typeof(T).GetTypeInfo()
                .IsAssignableFrom(args.Value.targetType.GetTypeInfo())
                ? (OneOf.OneOf<TOut, TOut[], NotApplicable>) map((T) args.Value.target)
                : new NotApplicable()));

        public TOut[] Map(object target) => To(target).Match(one => new[] {one}, many => many, none => new TOut[0]);


        OneOf<TOut, TOut[], NotApplicable> To(object target)
        {
            var targetType = target.GetType();
            var ruleArgs = ToRuleArgs.From((targetType, target));
            foreach (var rule in Rules)
            {
                var ruleResult = rule.Value.Invoke(ruleArgs);
                if (ruleResult.TryPickT0(out var mv, out var arrayOrNa))
                    return mv;
                if (arrayOrNa.TryPickT0(out var array, out NotApplicable notApplicable))
                    return array;
            }

            return new NotApplicable();
        }
    }
    public struct NotApplicable
    {
    }
}
