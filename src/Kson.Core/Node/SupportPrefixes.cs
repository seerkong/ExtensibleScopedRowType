namespace Kson.Core.Node;

public interface SupportPrefixes
{
    KsArray? AnnotationPrefixes { get; set; }

    KsMap? WithEffectPrefix { get; set; }

    KsArray? TypePrefixes { get; set; }

    KsArray? UnboundTypes { get; set; }
}
