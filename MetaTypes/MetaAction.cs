using System;
using System.Collections.Generic;

namespace MetaTypes
{
    
    public class MetaAction
    {
        public MetaName Name { get; set; }
        public IEnumerable<MetaParameter> Parameters { get; set; }
        public Func<IEnumerable<MetaArgument>, MetaResult> Call { get; set; }
    }
}