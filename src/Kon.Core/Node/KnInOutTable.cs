using System;
using System.Collections.Generic;
using System.Linq;
using Kon.Core.Converter;
using Kon.Core.Node.Inner;

namespace Kon.Core.Node;

public class KnInOutTable : KnContainerNode
{
    public KnInOutTableType Type = KnInOutTableType.NoOutput;
    public List<KnInOutTableItem> Inputs = new();
    public List<KnInOutTableItem> Outputs = new();

    public static KnInOutTable MakeByInputNodes(List<KnNode> items)
    {
        List<KnInOutTableItem> inputs = new();
        for (int i = 0; i < items.Count; i++)
        {
            inputs.Add(new KnInOutTableItem
            {
                Value = items[i]
            });
        }
        return new KnInOutTable
        {
            Inputs = inputs
        };
    }

    public List<KnNode> GetInputNodes()
    {
        KnInOutTable table = this;
        List<KnNode> r = new();
        for (int i = 0; i < table.Inputs.Count; i++)
        {
            r.Add(table.Inputs[i].Value);
        }
        return r;
    }

    public override string ToString() => KonFormater.SingleLine(this);
}
