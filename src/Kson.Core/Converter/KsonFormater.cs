using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Kson.Core.Node;
using Kson.Core.Util;

namespace Kson.Core.Converter;

public static class KsonFormater
{
    private const string MultilineIndentStr = "  ";

    public static string Prettify(KsNode node) => Stringify(node, new FormatState(0, true));

    public static string SingleLine(KsNode node) => Stringify(node, new FormatState(0, false));

    public static string Stringify(KsNode node, FormatState formatState) =>
        NodeToString(node, formatState);

    public static string NodeToString(KsNode? node, FormatState formatState)
    {
        if (node is null)
        {
            return "null";
        }

        if (node is KsMap map)
        {
            return MapToString(map, formatState);
        }

        if (node is KsFuncInOutData)
        {
            return ArrayToStringCustom(
                      (node as KsFuncInOutData).GetItems(),
                      formatState,
                      "|",
                      "|",
                      false);
        }

        if (node is KsArray arr)
        {
            return ArrayToString(arr, formatState);
        }

        if (node is KsBlock block)
        {
            return ArrayToStringCustom(block.GetItems(), formatState, "`(", ")", false);
        }

        if (node is KsChainNode chainNode)
        {
            return ChainToString(chainNode, formatState);
        }

        if (node is KsWord word)
        {
            return WordToStringCustom(word, formatState, string.Empty);
        }

        if (node is KsSymbol symbol)
        {
            return $"`{symbol.Value}";
        }

        // if (node.IsStackOp())
        // {
        //     return $"`{node.AsStackOp().Value}";
        // }

        if (node is KsQuoteNode quoteNode)
        {
            return QuoteToString(quoteNode, formatState);
        }

        if (node is KsString str)
        {
            return $"\"{StringEscapeHelper.EscapeString(str.Value)}\"";
        }

        if (node is KsInt64 int64)
        {
            return int64.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (node is KsDouble @double)
        {
            return @double.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (node is KsBoolean boolean)
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

    public static string WordToStringCustom(KsWord node, FormatState formatState, string prefixNotation)
    {
        var containerIndent = string.Concat(Enumerable.Repeat(MultilineIndentStr, formatState.IndentLevel));
        var prefixPostfixStr = BuildPrefixPostfixStr(node, new FormatState(formatState.IndentLevel, false), containerIndent);
        var fullPath = node.GetFullNameList();
        return $"{prefixNotation}{prefixPostfixStr.Prefixes}{string.Join(".", fullPath)}{prefixPostfixStr.Postfixes}";
    }

    public static string MapToString(KsMap node, FormatState formatState) =>
        MapToStringCustom(node, formatState, "{", "}");

    public static string QuoteToString(KsQuoteNode node, FormatState formatState)
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

    public static string MapToStringCustom(KsMap node, FormatState formatState, string prefix, string suffix)
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

    public static string MapFormatInner(KsMap node, FormatState formatState, bool addElementSeparator)
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
                var quoteNode = component.Value as KsQuoteNode ?? new KsQuoteNode(QuoteType.UnquoteMap, component.Value);
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

    public static string ArrayToString(KsArray node, FormatState formatState) =>
        ArrayToStringCustom(node.GetItems(), formatState, "[", "]", false);

    public static string ArrayToStringCustom(
        IList<KsNode> items,
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

    public static string ArrayFormatInner(IList<KsNode> items, FormatState formatState, bool addElementSeparator)
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

    public static bool ShouldSingleLine(KsChainNode node)
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

    public static string ChainToString(KsChainNode node, FormatState formatState)
    {
        var containerIndent = string.Concat(Enumerable.Repeat(MultilineIndentStr, formatState.IndentLevel));
        var prefixPostfixStr = BuildPrefixPostfixStr(node, formatState, containerIndent);
        var chainSectionsStr = ChainToStringCustom(node, formatState, "(", ")");
        return $"{prefixPostfixStr.Prefixes}{chainSectionsStr}{prefixPostfixStr.Postfixes}";
    }

    public static string ChainToStringCustom(KsChainNode node, FormatState formatState, string prefix, string suffix)
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

    public static string ChainFormatInner(KsChainNode node, FormatState formatState)
    {
        var innerIndent = string.Concat(Enumerable.Repeat(MultilineIndentStr, formatState.IndentLevel + 1));
        var builder = new StringBuilder();
        var iter = node;
        KsChainNode? previous = null;
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

    public static (string, bool) ChainFormatSegment(KsChainNode node, FormatState formatState)
    {
        var innerIndent = string.Concat(Enumerable.Repeat(MultilineIndentStr, formatState.IndentLevel + 1));
        var sb = new StringBuilder();

        if (node.CallType != null)
        {
            sb.Append(node.CallType switch
            {
                KsCallType.PrefixCall => RenderPrefixCall(node, formatState),
                KsCallType.PostfixCall => RenderPostfixCall(node, formatState),
                KsCallType.InstanceCall => RenderInstanceCall(node, formatState),
                KsCallType.StaticSubscript => RenderStaticSubscript(node, formatState),
                KsCallType.ContainerSubscript => RenderContainerSubscript(node, formatState),
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

            if (node.CallParams is not null)
            {
                if (sb.Length > 0)
                {
                    sb.Append(' ');
                }

                var childrenStr = ArrayToStringCustom(
                    node.CallParams.GetItems(),
                    new FormatState(formatState.IndentLevel + 1, false),
                    "|",
                    "|",
                    false);
                sb.Append(childrenStr);
            }
        }

        if (node.Attr is not null)
        {
            foreach (var metadataKey in node.Attr.Keys)
            {
                sb.Append(formatState.IsMultiline && node.Core is not null ? $"\n{innerIndent}" : " ");
                var annotationVal = node.Attr[metadataKey];
                if (annotationVal == KsBoolean.True)
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
            node.CallType != KsCallType.InstanceCall
            && node.CallType != KsCallType.StaticSubscript

            && node.CallType != KsCallType.ContainerSubscript
            );
        return (segmentStr, shouldAddAfterSpacer);
    }

    private static string RenderPrefixCall(KsChainNode node, FormatState formatState)
    {
        var builder = new StringBuilder();
        if (node.Core is not null)
        {
            builder.Append(NodeToString(node.Core, new FormatState(formatState.IndentLevel + 1, false)));
        }
        if (node.CallParams != null)
        {
            // if (node.Core is not null)
            // {
            //     builder.Append(" ");
            // }
            builder.Append(":");
            string paramPart = ArrayToStringCustom(
                          node.CallParams.GetItems(),
                          formatState,
                          "|",
                          "|",
                          false);
            builder.Append(paramPart);
        }

        if (node.CallResults != null)
        {
            string resultPart = ArrayToStringCustom(
                                  node.CallResults.GetItems(),
                                  formatState,
                                  "<",
                                  ">",
                                  false);
            builder.Append(resultPart);
        }
        return builder.ToString();
    }

    private static string RenderPostfixCall(KsChainNode node, FormatState formatState)
    {
        var builder = new StringBuilder();
        builder.Append(':');
        if (node.Core is not null)
        {
            builder.Append(NodeToString(node.Core, new FormatState(formatState.IndentLevel + 1, false)));
        }

        var args = RenderCallArguments(node.CallParams, formatState);
        if (!string.IsNullOrEmpty(args))
        {
            builder.Append(' ');
            builder.Append(args);
        }

        builder.Append(';');
        return builder.ToString();
    }

    private static string RenderInstanceCall(KsChainNode node, FormatState formatState)
    {
        var builder = new StringBuilder();
        builder.Append('~');
        if (node.Core is not null)
        {
            builder.Append(NodeToString(node.Core, new FormatState(formatState.IndentLevel + 1, false)));
        }

        if (node.CallParams is not null)
        {
            var args = RenderCallArguments(node.CallParams, formatState);
            if (!string.IsNullOrEmpty(args))
            {
                builder.Append(' ');
                builder.Append(args);
            }

            builder.Append(';');
        }


        return builder.ToString();
    }

    private static string RenderStaticSubscript(KsChainNode node, FormatState formatState)
    {
        var builder = new StringBuilder();
        builder.Append(".:");
        if (node.Core is not null)
        {
            builder.Append(NodeToString(node.Core, new FormatState(formatState.IndentLevel + 1, false)));
        }
        return builder.ToString();
    }

    private static string RenderContainerSubscript(KsChainNode node, FormatState formatState)
    {
        var builder = new StringBuilder();
        builder.Append("::");
        if (node.Core is not null)
        {
            builder.Append(NodeToString(node.Core, new FormatState(formatState.IndentLevel + 1, false)));
        }
        return builder.ToString();
    }

    private static string RenderCallArguments(KsArray? callParams, FormatState formatState)
    {
        if (callParams is null || callParams.Size() == 0)
        {
            return string.Empty;
        }

        var parts = callParams.GetItems()
            .Select(item => NodeToString(item, new FormatState(formatState.IndentLevel + 1, false)));
        return string.Join(" ", parts);
    }
}
