namespace MetaTypes
{
    using System.Collections.Generic;

    public class MetaType
    {
        public MetaName Name { get; set; }
        public MetaType GenericType { get; set; }
        public IEnumerable<MetaType> GenericArguments { get; set; }
    }
}