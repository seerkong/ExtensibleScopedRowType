using Kon.Core.Node;

public static class NodeValueHelper
{
    public static string GetInnerString(KnNode ksNode)
    {
        if (ksNode is KnString ksStr)
        {
            return ksStr.Value;
        }
        else if (ksNode is KnSymbol ksSymbol)
        {
            return ksSymbol.Value;
        }
        else if (ksNode is KnWord word)
        {
            return word.Value;
        }
        else
        {
            return null;
        }
    }
}