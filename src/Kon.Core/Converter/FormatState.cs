namespace Kon.Core.Converter;

public class FormatState
{
    public int IndentLevel { get; set; }
    public bool IsMultiline { get; set; }
    public bool IndentFirstCore { get; set; }

    public FormatState(int indentLevel, bool isMultiline)
    {
        IndentLevel = indentLevel;
        IsMultiline = isMultiline;
    }

    public FormatState(int indentLevel, bool isMultiline, bool indentFirstCore)
    {
        IndentLevel = indentLevel;
        IsMultiline = isMultiline;
        IndentFirstCore = indentFirstCore;
    }
}
