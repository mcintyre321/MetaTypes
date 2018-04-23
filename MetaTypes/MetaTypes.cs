namespace MetaTypes
{
    using System.Collections.Generic;

    static public class MetaTypes
    {
        public static MetaType String => new MetaType
        {
            Name = MetaName.From("String")
        };

        public static MetaType List => new MetaType
        {
            Name = MetaName.From(typeof(List<>).FullName)
        };
    }
}