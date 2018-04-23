using OneOf;

namespace MetaTypes
{
    public class MetaArgument : ValueOf<OneOf<MetaObject, MetaValue, MetaArray>, MetaArgument> { }
}