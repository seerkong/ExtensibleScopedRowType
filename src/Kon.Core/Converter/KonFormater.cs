using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Kon.Core.Node;
using Kon.Core.Node.Inner;
using Kon.Core.Util;

namespace Kon.Core.Converter;

public static class KonFormater
{
    private const string MultilineIndentStr = "  ";

    public static string Prettify(KnNode node) => Stringify(node, new FormatState(0, true));

    public static string SingleLine(KnNode node) => Stringify(node, new FormatState(0, false));

    public static string Stringify(KnNode node, FormatState formatState) =>
        NodeToString(node, formatState);

    public static string NodeToString(KnNode? node, FormatState formatState)
    {
        if (node is null)
        {
            return "null";
        }

        if (node is KnMap map)
        {
            return MapToString(map, formatState);
        }

        if (node is KnInOutTable knInOutTable)
        {
            return RenderKnInOutTable(formatState, knInOutTable);
        }

        if (node is KnArray arr)
        {
            return ArrayToString(arr, formatState);
        }

        if (node is KnBlock block)
        {
            return ArrayToStringCustom(block.GetItems(), formatState, "`(", ")", false);
        }

        if (node is KnInOutTable inOutTable)
        {
            return RenderKnInOutTable(formatState, inOutTable);
        }

        if (node is KnChainNode chainNode)
        {
            return ChainToString(chainNode, formatState);
        }

        if (node is KnWord word)
        {
            return WordToStringCustom(word, formatState, string.Empty);
        }

        if (node is KnSymbol symbol)
        {
            return $"`{symbol.Value}";
        }

        // if (node.IsStackOp())
        // {
        //     return $"`{node.AsStackOp().Value}";
        // }

        if (node is KnQuoteNode quoteNode)
        {
            return QuoteToString(quoteNode, formatState);
        }

        if (node is KnString str)
        {
            return $"\"{StringEscapeHelper.EscapeString(str.Value)}\"";
        }

        if (node is KnInt64 int64)
        {
            return int64.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (node is KnDouble @double)
        {
            return @double.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (node is KnBoolean boolean)
        {
            return boolean.Value ? "true" : "false";
        }

        return "null";
    }

    public static (string Prefixes, string Postfixes) BuildPrefixPostfixStr(
        SupportPrefixesPostfixes node,
        FormatState formatState,
        string containerIndent)
    {
        var afterPrefixSeparator = formatState.IsMultiline ? $"\n{containerIndent}" : " ";
        var prefixesBuilder = new StringBuilder();
        var postfixesBuilder = new StringBuilder();

        if (node.AnnotationPrefixes is { } annotationPrefixes)
        {
            prefixesBuilder.Append(ArrayToStringCustom(
                annotationPrefixes.GetItems(),
                formatState,
                "@(",
                ")",
                false));
            prefixesBuilder.Append(afterPrefixSeparator);
        }

        if (node.WithEffectPrefix is { } withEffectPrefix)
        {
            prefixesBuilder.Append(MapToStringCustom(
                withEffectPrefix,
                new FormatState(formatState.IndentLevel, formatState.IsMultiline),
                "@{",
                "}"));
            prefixesBuilder.Append(afterPrefixSeparator);
        }

        if (node.UnboundTypes is { } unboundTypes)
        {
            prefixesBuilder.Append(ArrayToStringCustom(
                unboundTypes.GetItems(),
                new FormatState(formatState.IndentLevel, false),
                "@[",
                "]",
                false));
            prefixesBuilder.Append(afterPrefixSeparator);
        }

        if (node.TypePrefixes is { } typePrefixes)
        {
            foreach (var item in typePrefixes.GetItems())
            {
                prefixesBuilder.Append("!");
                prefixesBuilder.Append(NodeToString(
                    item,
                    new FormatState(formatState.IndentLevel + 1, false)));
                prefixesBuilder.Append(afterPrefixSeparator);
            }
        }

        if (node.Postfixes is { } postfixes && postfixes.GetItems().Count > 0)
        {
            postfixesBuilder.Append(ArrayToStringCustom(
                postfixes.GetItems(),
                formatState,
                " %(",
                ")",
                false));
        }

        return (prefixesBuilder.ToString(), postfixesBuilder.ToString());
    }

    public static string WordToStringCustom(KnWord node, FormatState formatState, string prefixNotation)
    {
        var containerIndent = string.Concat(Enumerable.Repeat(MultilineIndentStr, formatState.IndentLevel));
        var prefixPostfixStr = BuildPrefixPostfixStr(node, new FormatState(formatState.IndentLevel, false), containerIndent);
        var fullPath = node.GetFullNameList();
        return $"{prefixNotation}{prefixPostfixStr.Prefixes}{string.Join(".", fullPath)}{prefixPostfixStr.Postfixes}";
    }

    public static string MapToString(KnMap node, FormatState formatState) =>
        MapToStringCustom(node, formatState, "{", "}");

    public static string QuoteToString(KnQuoteNode node, FormatState formatState)
    {
        var prefix = node.Kind switch
        {
            QuoteType.QuasiQuote => "`",
            QuoteType.Unquote => ",",
            QuoteType.UnquoteSplice => ",@",
            QuoteType.UnquoteMap => ",%",
            _ => string.Empty
        };

        var innerState = new FormatState(formatState.IndentLevel, formatState.IsMultiline);
        var inner = NodeToString(node.Value, innerState);
        return prefix + inner;
    }

    public static string MapToStringCustom(KnMap node, FormatState formatState, string prefix, string suffix)
    {
        var inner = MapFormatInner(node, formatState, false);
        if (formatState.IsMultiline)
        {
            var containerIndent = string.Concat(Enumerable.Repeat(MultilineIndentStr, formatState.IndentLevel));
            var builder = new StringBuilder();
            builder.Append(prefix);
            if (node.Components.Count > 0)
            {
                builder.Append('\n');
                builder.Append(inner);
                builder.Append('\n');
                builder.Append(containerIndent);
            }

            builder.Append(suffix);
            return builder.ToString();
        }

        return $"{prefix}{inner}{suffix}";
    }

    public static string MapFormatInner(KnMap node, FormatState formatState, bool addElementSeparator)
    {
        var elementSeparator = addElementSeparator ? "," : string.Empty;
        var pairsJoiner = addElementSeparator ? ", " : " ";
        var keyValueJoiner = ":";
        var valueTagAndValueJoiner = " ";
        var keyIndent = string.Empty;

        if (formatState.IsMultiline)
        {
            pairsJoiner = $"{elementSeparator}\n";
            keyIndent = string.Concat(Enumerable.Repeat(MultilineIndentStr, formatState.IndentLevel + 1));
        }

        var innerStrings = new List<string>();
        foreach (var component in node.Components)
        {
            if (component.IsSpread)
            {
                var quoteNode = component.Value as KnQuoteNode ?? new KnQuoteNode(QuoteType.UnquoteMap, component.Value);
                var spreadStr = QuoteToString(
                    quoteNode,
                    new FormatState(formatState.IndentLevel + 1, formatState.IsMultiline, true));
                innerStrings.Add(keyIndent + spreadStr);
                continue;
            }

            if (component.Key is null)
            {
                continue;
            }

            var value = component.Value;
            if (value is null)
            {
                continue;
            }

            var innerValStr = NodeToString(
                value,
                new FormatState(formatState.IndentLevel + 1, formatState.IsMultiline, true));
            var pairStr = $"{keyIndent}{component.Key}{keyValueJoiner}{valueTagAndValueJoiner}{innerValStr}";
            innerStrings.Add(pairStr);
        }

        return string.Join(pairsJoiner, innerStrings);
    }

    public static string ArrayToString(KnArray node, FormatState formatState) =>
        ArrayToStringCustom(node.GetItems(), formatState, "[", "]", false);

    public static string ArrayToStringCustom(
        IList<KnNode> items,
        FormatState formatState,
        string prefix,
        string suffix,
        bool addElementSeparator)
    {
        var inner = ArrayFormatInner(items, formatState, addElementSeparator);
        if (formatState.IsMultiline)
        {
            var containerIndent = string.Concat(Enumerable.Repeat(MultilineIndentStr, formatState.IndentLevel));
            var builder = new StringBuilder();
            builder.Append(prefix);
            if (items.Count > 0)
            {
                builder.Append('\n');
                builder.Append(inner);
                builder.Append('\n');
                builder.Append(containerIndent);
            }

            builder.Append(suffix);
            return builder.ToString();
        }

        return $"{prefix}{inner}{suffix}";
    }

    public static string ArrayFormatInner(IList<KnNode> items, FormatState formatState, bool addElementSeparator)
    {
        var joiner = addElementSeparator ? ", " : " ";
        var innerIndent = string.Empty;

        if (formatState.IsMultiline)
        {
            joiner = addElementSeparator ? ",\n" : "\n";
            innerIndent = string.Concat(Enumerable.Repeat(MultilineIndentStr, formatState.IndentLevel + 1));
        }

        var innerStrings = new List<string>();
        for (var i = 0; i < items.Count; i++)
        {
            var innerStr = NodeToString(
                items[i],
                new FormatState(formatState.IndentLevel + 1, formatState.IsMultiline));
            innerStrings.Add(innerIndent + innerStr);
        }

        return string.Join(joiner, innerStrings);
    }

    public static bool ShouldSingleLine(KnChainNode node)
    {
        var isCurrentCoreOrParamOnly = node.IsCoreOrParamOnlyNode();
        var isCurrentCoreNotContainer = !node.IsCoreContainerType();
        if (node.HasNext())
        {
            var nextNode = node.Next!;
            var isNextCoreOrParamOnly = nextNode.IsCoreOrParamOnlyNode();
            var isNextCoreNotContainer = !nextNode.IsCoreContainerType();
            return isCurrentCoreOrParamOnly && isCurrentCoreNotContainer && isNextCoreOrParamOnly && isNextCoreNotContainer;
        }

        return isCurrentCoreOrParamOnly && isCurrentCoreNotContainer;
    }

    public static string ChainToString(KnChainNode node, FormatState formatState)
    {
        var containerIndent = string.Concat(Enumerable.Repeat(MultilineIndentStr, formatState.IndentLevel));
        var prefixPostfixStr = BuildPrefixPostfixStr(node, formatState, containerIndent);
        var chainSectionsStr = ChainToStringCustom(node, formatState, "(", ")");
        return $"{prefixPostfixStr.Prefixes}{chainSectionsStr}{prefixPostfixStr.Postfixes}";
    }

    public static string ChainToStringCustom(KnChainNode node, FormatState formatState, string prefix, string suffix)
    {
        var containerIndent = string.Concat(Enumerable.Repeat(MultilineIndentStr, formatState.IndentLevel));
        var innerIndent = string.Concat(Enumerable.Repeat(MultilineIndentStr, formatState.IndentLevel + 1));

        var afterChainStartTokenStr = string.Empty;
        var beforeChainEndTokenStr = string.Empty;
        string inner;
        var isMultiline = formatState.IsMultiline;
        if (isMultiline)
        {
            if (formatState.IndentLevel > 0 && formatState.IndentFirstCore)
            {
                afterChainStartTokenStr = $"\n{innerIndent}";
            }

            inner = ChainFormatInner(node, new FormatState(formatState.IndentLevel, true));
            beforeChainEndTokenStr = $"\n{containerIndent}";
        }
        else
        {
            inner = ChainFormatInner(node, new FormatState(formatState.IndentLevel, false));
        }

        return $"{prefix}{afterChainStartTokenStr}{inner}{beforeChainEndTokenStr}{suffix}";
    }

    public static string ChainFormatInner(KnChainNode node, FormatState formatState)
    {
        var innerIndent = string.Concat(Enumerable.Repeat(MultilineIndentStr, formatState.IndentLevel + 1));
        var builder = new StringBuilder();
        var iter = node;
        KnChainNode? previous = null;
        while (iter is not null)
        {
            var (segment, shouldAddAfterSpacer) = ChainFormatSegment(iter, formatState);
            if (previous is not null)
            {
                if (formatState.IsMultiline)
                {
                    builder.Append($"\n{innerIndent}");
                }
                else if (shouldAddAfterSpacer)
                {
                    builder.Append(" ");
                }
            }

            builder.Append(segment);
            previous = iter;
            iter = iter.Next;
        }

        return builder.ToString();
    }

    public static (string, bool) ChainFormatSegment(KnChainNode node, FormatState formatState)
    {
        var innerIndent = string.Concat(Enumerable.Repeat(MultilineIndentStr, formatState.IndentLevel + 1));
        var sb = new StringBuilder();

        if (node.CallType != null)
        {
            sb.Append(node.CallType switch
            {
                KnCallType.PrefixCall => RenderPrefixCall(node, formatState),
                KnCallType.PostfixCall => RenderPostfixCall(node, formatState),
                KnCallType.InstanceCall => RenderInstanceCall(node, formatState),
                KnCallType.StaticSubscript => RenderStaticSubscript(node, formatState),
                KnCallType.ContainerSubscript => RenderContainerSubscript(node, formatState),
                _ => string.Empty
            });
        }
        else
        {
            if (node.Core is not null)
            {
                var coreStr = NodeToString(
                    node.Core,
                    new FormatState(formatState.IndentLevel + 1, false));
                sb.Append(coreStr);
            }

            if (node.Name is not null)
            {
                if (sb.Length > 0)
                {
                    sb.Append(' ');
                }
                sb.Append($"#{node.Name.GetFullNameStr()}");
            }

            if (node.InOutTable is not null)
            {
                if (sb.Length > 0)
                {
                    sb.Append(' ');
                }

                string childrenStr = RenderKnInOutTable(
                    new FormatState(formatState.IndentLevel + 1, false),
                    node.InOutTable);
                sb.Append(childrenStr);
            }
        }

        if (node.Attr is not null)
        {
            foreach (var metadataKey in node.Attr.Keys)
            {
                sb.Append(formatState.IsMultiline && node.Core is not null ? $"\n{innerIndent}" : " ");
                var annotationVal = node.Attr[metadataKey];
                if (annotationVal == KnBoolean.True)
                {
                    sb.Append($"@{metadataKey}");
                }
                else
                {
                    var extVal = Stringify(annotationVal, new FormatState(formatState.IndentLevel + 1, formatState.IsMultiline, true));
                    sb.Append($"@{metadataKey}: {extVal}");
                }
            }
        }

        if (node.Conf is not null)
        {
            sb.Append(formatState.IsMultiline ? $"\n{innerIndent}" : " ");
            var confStr = MapToStringCustom(
                node.Conf,
                new FormatState(formatState.IndentLevel + 1, formatState.IsMultiline),
                "%{",
                "}");
            sb.Append(confStr);
        }

        if (node.NamedConf is not null)
        {
            foreach (var confKey in node.NamedConf.Keys)
            {
                sb.Append(formatState.IsMultiline ? $"\n{innerIndent}" : " ");
                var confStr = MapToStringCustom(
                    node.NamedConf[confKey],
                    new FormatState(formatState.IndentLevel + 1, formatState.IsMultiline),
                    $"%{confKey}: {{",
                    "}");
                sb.Append(confStr);
            }
        }

        if (node.Body is not null)
        {
            sb.Append(formatState.IsMultiline ? $"\n{innerIndent}" : " ");
            var blockStr = ArrayToStringCustom(
                node.Body.GetItems(),
                new FormatState(formatState.IndentLevel + 1, formatState.IsMultiline),
                "%[",
                "]",
                false);
            sb.Append(blockStr);
        }

        if (node.Sections is not null)
        {
            foreach (var entry in node.Sections)
            {
                sb.Append(formatState.IsMultiline ? $"\n{innerIndent}" : " ");
                var blockStr = ArrayToStringCustom(
                    entry.Value.GetItems(),
                    new FormatState(formatState.IndentLevel + 1, formatState.IsMultiline),
                    $"%{entry.Key}: [",
                    "]",
                    false);
                sb.Append(blockStr);
            }
        }

        if (node.Slots is not null)
        {
            foreach (var slotKey in node.Slots.Keys)
            {
                sb.Append(formatState.IsMultiline ? $"\n{innerIndent}" : " ");
                var slotStr = ChainToStringCustom(
                    node.Slots[slotKey],
                    new FormatState(formatState.IndentLevel + 1, formatState.IsMultiline, true),
                    $"%{slotKey}: (",
                    ")");
                sb.Append(slotStr);
            }
        }

        string segmentStr = sb.ToString();
        bool shouldAddAfterSpacer = (
            node.CallType != KnCallType.InstanceCall
            && node.CallType != KnCallType.StaticSubscript

            && node.CallType != KnCallType.ContainerSubscript
            );
        return (segmentStr, shouldAddAfterSpacer);
    }

    private static string RenderPrefixCall(KnChainNode node, FormatState formatState)
    {
        var builder = new StringBuilder();
        if (node.Core is not null)
        {
            builder.Append(NodeToString(node.Core, new FormatState(formatState.IndentLevel + 1, false)));
        }
        if (node.InOutTable != null)
        {
            // if (node.Core is not null)
            // {
            //     builder.Append(" ");
            // }
            builder.Append(":");
            KnInOutTable table = node.InOutTable;
            string paramPart = RenderKnInOutTable(formatState, table);
            builder.Append(paramPart);
        }

        if (node.GenericParams != null)
        {
            string resultPart = ArrayToStringCustom(
                                  node.GenericParams.GetItems(),
                                  formatState,
                                  "<",
                                  ">",
                                  false);
            builder.Append(resultPart);
        }
        return builder.ToString();
    }

    private static string RenderKnInOutTable(
        FormatState formatState, KnInOutTable table
    )
    {
        List<KnNode> items = new();
        for (int i = 0; i < table.Inputs.Count; i++)
        {
            items.Add(table.Inputs[i].Value);
        }
        bool hasResult = (table.Type != KnInOutTableType.NoOutput);
        if (table.Type == Node.Inner.KnInOutTableType.OnlyTypeOutput)
        {
            items.Add(new KnWord("->"));
        }
        if (table.Type == Node.Inner.KnInOutTableType.NameAndTypeOutput)
        {
            items.Add(new KnWord("--"));
        }
        if (hasResult && table.Outputs != null)
        {
            for (int i = 0; i < table.Outputs.Count; i++)
            {
                items.Add(table.Outputs[i].Value);
            }
        }
        string paramPart = ArrayToStringCustom(
                      items,
                      formatState,
                      "|",
                      "|",
                      false);
        return paramPart;
    }

    private static string RenderPostfixCall(KnChainNode node, FormatState formatState)
    {
        var builder = new StringBuilder();
        builder.Append(':');
        if (node.Core is not null)
        {
            builder.Append(NodeToString(node.Core, new FormatState(formatState.IndentLevel + 1, false)));
        }

        var args = RenderCallArguments(node.InOutTable, formatState);
        if (!string.IsNullOrEmpty(args))
        {
            builder.Append(' ');
            builder.Append(args);
        }

        builder.Append(';');
        return builder.ToString();
    }

    private static string RenderInstanceCall(KnChainNode node, FormatState formatState)
    {
        var builder = new StringBuilder();
        builder.Append('~');
        if (node.Core is not null)
        {
            builder.Append(NodeToString(node.Core, new FormatState(formatState.IndentLevel + 1, false)));
        }

        if (node.InOutTable is not null)
        {
            var args = RenderCallArguments(node.InOutTable, formatState);
            if (!string.IsNullOrEmpty(args))
            {
                builder.Append(' ');
                builder.Append(args);
            }

            builder.Append(';');
        }


        return builder.ToString();
    }

    private static string RenderStaticSubscript(KnChainNode node, FormatState formatState)
    {
        var builder = new StringBuilder();
        builder.Append(".:");
        if (node.Core is not null)
        {
            builder.Append(NodeToString(node.Core, new FormatState(formatState.IndentLevel + 1, false)));
        }
        return builder.ToString();
    }

    private static string RenderContainerSubscript(KnChainNode node, FormatState formatState)
    {
        var builder = new StringBuilder();
        builder.Append("::");
        if (node.Core is not null)
        {
            builder.Append(NodeToString(node.Core, new FormatState(formatState.IndentLevel + 1, false)));
        }
        return builder.ToString();
    }

    private static string RenderCallArguments(KnInOutTable? inOutTable, FormatState formatState)
    {
        List<KnNode> items = new();
        if (inOutTable != null)
        {
            for (int i = 0; i < inOutTable.Inputs.Count; i++)
            {
                items.Add(inOutTable.Inputs[i].Value);
            }
        }

        if (items.Count == 0)
        {
            return string.Empty;
        }

        var parts = items
            .Select(item => NodeToString(item, new FormatState(formatState.IndentLevel + 1, false)));
        return string.Join(" ", parts);
    }
}
