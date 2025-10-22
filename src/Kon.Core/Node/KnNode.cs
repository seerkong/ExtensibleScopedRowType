using System.Collections.Generic;

namespace Kon.Core.Node;

public interface KnNode
{
    bool ToBoolean();

    bool IsBoolean();

    KnBoolean AsBoolean();

    KnString AsString();
}
