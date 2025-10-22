using System.Text;

namespace Kon.Core.Util;

public static class StringEscapeHelper
{
    public static string EscapeString(string internalStr)
    {
        var sb = new StringBuilder();
        foreach (var ch in internalStr)
        {
            switch (ch)
            {
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                case '\b':
                    sb.Append("\\b");
                    break;
                case '\f':
                    sb.Append("\\f");
                    break;
                case '"':
                    sb.Append("\\\"");
                    break;
                case '/':
                    sb.Append("\\/");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                default:
                    sb.Append(ch);
                    break;
            }
        }

        return sb.ToString();
    }
}
