using System;
using OneOf;
using System.Collections.Generic;
using ValueOf;

namespace MetaTypes
{
    public class MetaObject
    {
        public MetaType Type { get; set; }
        public IEnumerable<MetaProperty> Properties { get; set; }
        public IEnumerable<MetaAction> Actions { get; set; }
    }

    public class MetaProperty
    {
        public MetaName Name { get; set; }
        public Func<MetaValue> GetValue { get; set; }
        public MetaType Type { get; set; }
    }

    public class MetaScalar : ValueOf<OneOf<string, int, double, float>, MetaScalar>
    {
    }

    public class MetaArray : ValueOf.ValueOf<IEnumerable<MetaValue>, MetaArray>
    {
    }

    public class MetaType
    {
        public MetaName Name { get; set; }
        public MetaType GenericType { get; set; }
        public IEnumerable<MetaType> GenericArguments { get; set; }
    }

    static public class MetaTypes
    {
        public static MetaType String => new MetaType
        {
            Name = MetaName.From("System.String")
        };

        public static MetaType List => new MetaType
        {
            Name = MetaName.From(typeof(List<>).FullName)
        };
    }

    public class MetaName : ValueOf<string, MetaName>
    {
    }

    public class MetaAction
    {
        public MetaName Name { get; set; }
        public IEnumerable<MetaParameter> Parameters { get; set; }
        public Func<IEnumerable<MetaValue>, MetaResult> Call { get; set; }
    }

    public class MetaParameter
    {
        public MetaName Name { get; set; }
        public MetaType Type { get; set; }
    }

    public class MetaValue : ValueOf.ValueOf<OneOf<MetaObject, MetaScalar, MetaArray>, MetaValue>
    {
        public static implicit operator MetaValue(OneOf<MetaObject, MetaScalar, MetaArray> x) => MetaValue.From(x);
        public static implicit operator MetaValue(MetaObject x) => MetaValue.From(x);
        public static implicit operator MetaValue(MetaScalar x) => MetaValue.From(x);
        public static implicit operator MetaValue(MetaArray x) => MetaValue.From(x);
    }

    public class MetaResult : ValueOf.ValueOf<bool, MetaResult>
    {
    }
}