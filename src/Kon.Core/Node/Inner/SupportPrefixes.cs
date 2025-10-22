namespace Kon.Core.Node.Inner;

public interface SupportPrefixes
{
    KnArray? AnnotationPrefixes { get; set; }

    KnMap? WithEffectPrefix { get; set; }

    KnArray? TypePrefixes { get; set; }

    KnArray? UnboundTypes { get; set; }
}
