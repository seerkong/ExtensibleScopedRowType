using System;
using System.Collections.Generic;
namespace Kon.Interpreter.Runtime;

/// <summary>
/// Interface implemented by nodes that can be stored in a <see cref="SingleEntryGraph{TNode, TKey}"/>.
/// </summary>
/// <typeparam name="TKey">Identifier type for the node.</typeparam>
public interface ISingleEntryGraphNode<out TKey>
{
    TKey GetVertexId();
}

/// <summary>
/// Directed graph with a single entry vertex. Ports the TypeScript implementation used by the original runtime.
/// </summary>
/// <typeparam name="TNode">Node detail type.</typeparam>
/// <typeparam name="TKey">Node identifier type.</typeparam>
public class SingleEntryGraph<TNode, TKey>
    where TNode : class, ISingleEntryGraphNode<TKey>
    where TKey : notnull
{
    private readonly Dictionary<TKey, TNode> _vertexDetailMap = new();
    private readonly HashSet<TKey> _vertexIds = new();
    private readonly Dictionary<TKey, HashSet<TKey>> _nextIdsMap = new();
    private readonly Dictionary<TKey, HashSet<TKey>> _prevIdsMap = new();

    private TKey? _entryVertexId;
    private bool _hasEntryVertex;

    /// <summary>
    /// Sets the entry vertex identifier for the graph.
    /// </summary>
    public void SetEntryVertexId(TKey rootId)
    {
        _entryVertexId = rootId;
        _hasEntryVertex = true;
    }

    /// <summary>
    /// Gets the entry vertex identifier.
    /// </summary>
    public TKey GetEntryVertexId()
    {
        if (!_hasEntryVertex)
        {
            throw new InvalidOperationException("Entry vertex has not been set.");
        }

        return _entryVertexId!;
    }

    /// <summary>
    /// Gets the entry vertex detail, if the entry has been set and exists.
    /// </summary>
    public TNode? GetEntryVertex()
    {
        return !_hasEntryVertex ? null : GetVertexDetail(_entryVertexId!);
    }

    /// <summary>
    /// Returns the mapping of each vertex to its outgoing edges.
    /// </summary>
    public IReadOnlyDictionary<TKey, HashSet<TKey>> GetNextIdsMap() => _nextIdsMap;

    /// <summary>
    /// Gets the outgoing vertex identifiers for the specified vertex.
    /// </summary>
    public HashSet<TKey> GetNextIds(TKey vertexId)
    {
        return CopySetValue(_nextIdsMap, vertexId);
    }

    /// <summary>
    /// Gets the node details of the outgoing vertices for the specified vertex.
    /// </summary>
    public HashSet<TNode> GetNextVertexDetails(TKey vertexId)
    {
        var nextIds = GetNextIds(vertexId);
        return GetVertexDetailsByIds(nextIds);
    }

    /// <summary>
    /// Adds the specified vertex to the graph.
    /// </summary>
    public virtual void AddVertex(TNode vertexDetail)
    {
        var vertexId = vertexDetail.GetVertexId();
        if (!_vertexIds.Add(vertexId))
        {
            return;
        }

        _vertexDetailMap[vertexId] = vertexDetail;
    }

    /// <summary>
    /// Removes the vertex and all edges to and from it.
    /// </summary>
    private void RemoveVertexAndNeighborEdges(TKey vertexId)
    {
        var vertexNextIds = CopySetValue(_nextIdsMap, vertexId);
        var vertexPrevIds = CopySetValue(_prevIdsMap, vertexId);

        foreach (var nextVertexId in vertexNextIds)
        {
            RemoveEdge(vertexId, nextVertexId);
        }

        foreach (var prevVertexId in vertexPrevIds)
        {
            RemoveEdge(prevVertexId, vertexId);
        }

        _vertexDetailMap.Remove(vertexId);
        _vertexIds.Remove(vertexId);
    }

    /// <summary>
    /// Removes the vertex and connects each of its predecessors to each of its successors.
    /// </summary>
    public void RemoveVertexAndConnectNeighborEdges(TKey vertexId)
    {
        var vertexNextIds = CopySetValue(_nextIdsMap, vertexId);
        var vertexPrevIds = CopySetValue(_prevIdsMap, vertexId);

        RemoveVertexAndNeighborEdges(vertexId);

        foreach (var prevId in vertexPrevIds)
        {
            foreach (var nextId in vertexNextIds)
            {
                AddEdge(prevId, nextId);
            }
        }
    }

    /// <summary>
    /// Adds a directed edge from <paramref name="prevId"/> to <paramref name="nextId"/>.
    /// </summary>
    public virtual void AddEdge(TKey prevId, TKey nextId)
    {
        if (!_vertexIds.Contains(prevId) || !_vertexIds.Contains(nextId))
        {
            return;
        }

        var nextIds = GetOrCreateSet(_nextIdsMap, prevId);
        var prevIds = GetOrCreateSet(_prevIdsMap, nextId);

        nextIds.Add(nextId);
        prevIds.Add(prevId);
    }

    /// <summary>
    /// Removes a directed edge from <paramref name="prevId"/> to <paramref name="nextId"/>.
    /// </summary>
    public void RemoveEdge(TKey prevId, TKey nextId)
    {
        var nextIds = GetOrCreateSet(_nextIdsMap, prevId);
        var prevIds = GetOrCreateSet(_prevIdsMap, nextId);

        nextIds.Remove(nextId);
        if (nextIds.Count == 0)
        {
            _nextIdsMap.Remove(prevId);
        }

        prevIds.Remove(prevId);
        if (prevIds.Count == 0)
        {
            _prevIdsMap.Remove(nextId);
        }
    }

    /// <summary>
    /// Appends a single-entry DAG after the specified vertices.
    /// </summary>
    public SingleEntryGraph<TNode, TKey>? AppendDAG(ISet<TKey> appendAfterVertexIds, SingleEntryGraph<TNode, TKey>? otherDag)
    {
        if (otherDag == null)
        {
            return null;
        }

        var otherEntryVertexId = otherDag.GetEntryVertexId();
        var otherReachableVertices = otherDag.GetReachableVertexes();
        var otherNextIdsMap = otherDag.GetNextIdsMap();

        foreach (var otherVertex in otherReachableVertices)
        {
            AddVertex(otherVertex);
        }

        foreach (var appendAfterVertexId in appendAfterVertexIds)
        {
            AddEdge(appendAfterVertexId, otherEntryVertexId);
        }

        foreach (var kvp in otherNextIdsMap)
        {
            foreach (var endId in kvp.Value)
            {
                AddEdge(kvp.Key, endId);
            }
        }

        return otherDag;
    }

    /// <summary>
    /// Gets all vertices reachable from the entry point.
    /// </summary>
    public HashSet<TNode> GetReachableVertexes()
    {
        var reachableVertexIds = GetReachableVertexIds();
        var result = new HashSet<TNode>();

        foreach (var vertexId in reachableVertexIds)
        {
            if (_vertexDetailMap.TryGetValue(vertexId, out var detail))
            {
                result.Add(detail);
            }
        }

        return result;
    }

    /// <summary>
    /// Gets all vertices, including those that are unreachable from the entry.
    /// </summary>
    public HashSet<TNode> GetVertexesIncludeUnreachable()
    {
        return new HashSet<TNode>(_vertexDetailMap.Values);
    }

    /// <summary>
    /// Gets all vertex identifiers, including those that are unreachable from the entry.
    /// </summary>
    public HashSet<TKey> GetVertexIdsIncludeUnreachable()
    {
        return new HashSet<TKey>(_vertexIds);
    }

    /// <summary>
    /// Gets the vertex identifiers that are unreachable from the entry.
    /// </summary>
    public HashSet<TKey> GetUnreachableVertexIds()
    {
        var reachableIds = GetReachableVertexIds();
        var unreachable = new HashSet<TKey>(_vertexIds);
        unreachable.ExceptWith(reachableIds);
        return unreachable;
    }

    /// <summary>
    /// Gets the vertex details that are unreachable from the entry.
    /// </summary>
    public HashSet<TNode> GetUnreachableVertexes()
    {
        var unreachableIds = GetUnreachableVertexIds();
        return GetVertexDetailsByIds(unreachableIds);
    }

    /// <summary>
    /// Returns the vertex identifiers reachable from the entry vertex.
    /// </summary>
    public HashSet<TKey> GetReachableVertexIds()
    {
        if (!_hasEntryVertex)
        {
            return new HashSet<TKey>();
        }

        return QueryReachableVertexIds(_nextIdsMap, _entryVertexId!, includeFromId: true);
    }

    /// <summary>
    /// Gets the vertex details from the entry to the specified vertex, optionally including the vertex itself.
    /// </summary>
    public HashSet<TNode> GetAllVertexDetailsFromEntryToVertex(TKey queryFromVertexId, bool includeSelf)
    {
        var ids = GetAllVertexIdsFromEntryToVertex(queryFromVertexId, includeSelf);
        return GetVertexDetailsByIds(ids);
    }

    /// <summary>
    /// Gets all vertex identifiers on every path from the entry to the specified vertex.
    /// </summary>
    public HashSet<TKey> GetAllVertexIdsFromEntryToVertex(TKey queryFromVertexId, bool includeSelf)
    {
        var result = new HashSet<TKey>();
        if (includeSelf)
        {
            result.Add(queryFromVertexId);
        }

        var queue = new Queue<TKey>();
        queue.Enqueue(queryFromVertexId);

        var visited = new HashSet<TKey>();

        while (queue.Count > 0)
        {
            var levelSize = queue.Count;
            for (var i = 0; i < levelSize; i++)
            {
                var vertexId = queue.Dequeue();

                if (!visited.Add(vertexId))
                {
                    continue;
                }

                var prevVertexIds = CopySetValue(_prevIdsMap, vertexId);
                foreach (var prevId in prevVertexIds)
                {
                    result.Add(prevId);
                    queue.Enqueue(prevId);
                }
            }
        }

        return result;
    }

    private HashSet<TKey> QueryReachableVertexIds(
        IReadOnlyDictionary<TKey, HashSet<TKey>> nextIdsMap,
        TKey fromId,
        bool includeFromId)
    {
        var result = new HashSet<TKey>();
        if (includeFromId)
        {
            result.Add(fromId);
        }

        var queue = new Queue<TKey>();
        queue.Enqueue(fromId);

        var visited = new HashSet<TKey>();

        while (queue.Count > 0)
        {
            var levelSize = queue.Count;
            for (var i = 0; i < levelSize; i++)
            {
                var vertexId = queue.Dequeue();
                if (!visited.Add(vertexId))
                {
                    continue;
                }

                var nextVertexIds = CopySetValue(_nextIdsMap, vertexId);
                foreach (var nextId in nextVertexIds)
                {
                    result.Add(nextId);
                    queue.Enqueue(nextId);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the identifiers of the terminal vertices (vertices without outgoing edges) reachable from the entry.
    /// </summary>
    public HashSet<TKey> GetEndVertexIds()
    {
        var result = new HashSet<TKey>();
        var reachableVertexIds = GetReachableVertexIds();

        foreach (var vertexId in reachableVertexIds)
        {
            var nextIds = CopySetValue(_nextIdsMap, vertexId);
            if (nextIds.Count == 0)
            {
                result.Add(vertexId);
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the identifiers of the predecessors of the specified vertex.
    /// </summary>
    public HashSet<TKey> GetPrevVertexIds(TKey vertexId)
    {
        return CopySetValue(_prevIdsMap, vertexId);
    }

    /// <summary>
    /// Gets the predecessor vertex details for the specified vertex.
    /// </summary>
    public HashSet<TNode> GetPrevVertexesById(TKey vertexId)
    {
        var prevIds = GetPrevVertexIds(vertexId);
        return GetVertexDetailsByIds(prevIds);
    }

    /// <summary>
    /// Gets the vertex details for the specified identifiers.
    /// </summary>
    public HashSet<TNode> GetVertexDetailsByIds(IEnumerable<TKey> vertexIds)
    {
        var result = new HashSet<TNode>();
        foreach (var id in vertexIds)
        {
            if (_vertexDetailMap.TryGetValue(id, out var detail))
            {
                result.Add(detail);
            }
        }
        return result;
    }

    /// <summary>
    /// Gets the vertex detail for the specified identifier.
    /// </summary>
    public TNode? GetVertexDetail(TKey vertexId)
    {
        return _vertexDetailMap.TryGetValue(vertexId, out var detail) ? detail : null;
    }

    /// <summary>
    /// Determines whether the graph currently contains the specified vertex.
    /// </summary>
    protected bool ContainsVertex(TKey vertexId) => _vertexIds.Contains(vertexId);

    private static HashSet<TKey> GetOrCreateSet(Dictionary<TKey, HashSet<TKey>> map, TKey key)
    {
        if (!map.TryGetValue(key, out var set))
        {
            set = new HashSet<TKey>();
            map[key] = set;
        }

        return set;
    }

    private static HashSet<TKey> CopySetValue(IReadOnlyDictionary<TKey, HashSet<TKey>> map, TKey key)
    {
        return map.TryGetValue(key, out var set)
            ? new HashSet<TKey>(set)
            : new HashSet<TKey>();
    }
}
