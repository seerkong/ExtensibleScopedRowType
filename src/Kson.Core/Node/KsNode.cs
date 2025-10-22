using System.Collections.Generic;

namespace Kson.Core.Node;

public interface KsNode
{
    bool ToBoolean();

    bool IsBoolean();

    KsBoolean AsBoolean();

    KsString AsString();
}
