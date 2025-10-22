using Kson.Core.Node;

public static class NodeValueHelper
{
    public static string GetInnerString(KsNode ksNode)
    {
        if (ksNode is KsString ksStr)
        {
            return ksStr.Value;
        }
        else if (ksNode is KsSymbol ksSymbol)
        {
            return ksSymbol.Value;
        }
        else if (ksNode is KsWord word)
        {
            return word.Value;
        }
        else
        {
            return null;
        }
    }
}