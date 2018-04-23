using System.Collections.Generic;

namespace MetaTypes
{
    public class MetaObject
    {
        public MetaType Type { get; set; }
        public IEnumerable<MetaProperty> Properties { get; set; }
        public IEnumerable<MetaAction> Actions { get; set; }
    }
}
