using System;
using System.Collections.Generic;
namespace Kon.Interpreter.Runtime;

/// <summary>
/// Graph structure that tracks lexical environments for the interpreter.
/// </summary>
public class EnvTree : SingleEntryGraph<Env, int>
{
    /// <summary>
    /// Returns the direct parent environment for the specified environment identifier.
    /// </summary>
    public Env? GetParentEnv(int envId)
    {
        var prevIds = GetPrevVertexIds(envId);
        foreach (var prevId in prevIds)
        {
            return GetVertexDetail(prevId);
        }

        return null;
    }

    /// <summary>
    /// Looks up the environment where a variable is declared, walking the lexical parent chain.
    /// </summary>
    public Env LookupDeclareEnv(Env fromEnv, string key)
    {
        var lookupIter = fromEnv;
        while (lookupIter != null)
        {
            if (lookupIter.ContainsVar(key))
            {
                return lookupIter;
            }

            var parent = lookupIter.ParentEnv ?? GetParentEnv(lookupIter.Id);
            lookupIter = parent;
        }

        return fromEnv;
    }

    /// <summary>
    /// Creates a lexical child environment, attaches it to the graph, and returns it.
    /// </summary>
    public Env CreateLexicalScope(Env parentEnv, Env.EnvType subEnvType, string name)
    {
        if (parentEnv == null)
        {
            throw new ArgumentNullException(nameof(parentEnv));
        }

        Env lexicalEnv = subEnvType switch
        {
            Env.EnvType.Global => Env.CreateGlobalEnv(parentEnv, name),
            Env.EnvType.Process => Env.CreateProcessEnv(parentEnv, name),
            Env.EnvType.Local => Env.CreateLocalEnv(parentEnv, name),
            Env.EnvType.BuiltIn => throw new ArgumentException("Cannot create built-in scope as a child environment."),
            _ => throw new ArgumentOutOfRangeException(nameof(subEnvType), subEnvType, null)
        };

        AddVertex(lexicalEnv);
        AddEdge(parentEnv.GetVertexId(), lexicalEnv.GetVertexId());

        return lexicalEnv;
    }

    /// <summary>
    /// Gets the immediate child environments of the specified environment.
    /// </summary>
    public new IList<Env> GetNextVertexDetails(int vertexId)
    {
        var nextIds = GetNextIds(vertexId);
        var result = new List<Env>(nextIds.Count);

        foreach (var id in nextIds)
        {
            var env = GetVertexDetail(id);
            if (env != null)
            {
                result.Add(env);
            }
        }

        return result;
    }

    /// <summary>
    /// Adds a vertex and ensures the parent link remains in sync with the graph.
    /// </summary>
    public override void AddVertex(Env vertexDetail)
    {
        base.AddVertex(vertexDetail);

        if (vertexDetail.ParentEnv != null && !ReferenceEquals(vertexDetail.ParentEnv, GetParentEnv(vertexDetail.Id)))
        {
            var parent = vertexDetail.ParentEnv;
            if (parent != null && !ReferenceEquals(GetVertexDetail(parent.Id), parent))
            {
                base.AddVertex(parent);
            }

            if (parent != null)
            {
                AddEdge(parent.Id, vertexDetail.Id);
            }
        }
    }

    public override void AddEdge(int prevId, int nextId)
    {
        base.AddEdge(prevId, nextId);
    }
}
