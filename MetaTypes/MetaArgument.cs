using OneOf;
using ValueOf;

namespace MetaTypes
{
    public class MetaArgument : ValueOf<OneOf<MetaObject, MetaValue, MetaArray>, MetaArgument> { }
}