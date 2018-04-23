using OneOf;
using System.Collections.Generic;
using ValueOf;

namespace MetaTypes
{
    public class MetaArray : ValueOf<IEnumerable<OneOf<MetaObject, MetaValue, MetaArray>>, MetaArray> { }
}