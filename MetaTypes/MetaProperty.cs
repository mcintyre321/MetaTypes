using OneOf;
using System;

namespace MetaTypes
{
    public class MetaProperty
    {
        public MetaName Name { get; set; }
        public Func<OneOf<MetaObject, MetaValue, MetaArray>> GetValue { get; set; }
        public MetaType Type { get; set; }
    }
}