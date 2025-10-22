using System;
using System.Collections.Generic;
using System.Linq;
using Kson.Core.Converter;

namespace Kson.Core.Node;

public class KsFuncInOutData : KsArray
{

    public KsFuncInOutData()
    {
        _items = new List<KsNode>();
    }

    public KsFuncInOutData(IEnumerable<KsNode> value)
    {
        _items = value?.ToList() ?? new List<KsNode>();
    }

    public KsFuncInOutData(params KsNode[] children)
        : this((IEnumerable<KsNode>)children)
    {
    }
}
