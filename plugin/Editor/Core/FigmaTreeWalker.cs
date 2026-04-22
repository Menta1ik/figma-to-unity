using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEditor;
using FigmaImporter.V2.Data;
using FigmaImporter.V2.Runtime;
using FigmaImporter.V2.Core.Handlers;

namespace FigmaImporter.V2.Core
{
    /// <summary>
    /// Walks the Figma node tree and syncs each node to a Unity GameObject hierarchy.
    /// </summary>
    internal class FigmaTreeWalker
    {
        private readonly List<IFigmaComponentHandler> _handlers;
        private readonly FigmaHandlerContext _handlerContext;
        private readonly Dictionary<string, FigmaElement> _existingCache;
        private readonly Dictionary<string, FigmaElement> _sessionCache;
        private readonly HashSet<string> _processedIds;
        private readonly List<(FigmaNode node, FigmaElement element, int depth)> _deferredMasks;

        public int CreatedCount { get; private set; }
        public int UpdatedCount { get; private set; }

        public FigmaTreeWalker(
            List<IFigmaComponentHandler> handlers,
            FigmaHandlerContext handlerContext,
            Dictionary<string, FigmaElement> existingCache,
            Dictionary<string, FigmaElement> sessionCache,
            HashSet<string> processedIds,
            List<(FigmaNode node, FigmaElement element, int depth)> deferredMasks)
        {
            _handlers = handlers;
            _handlerContext = handlerContext;
            _existingCache = existingCache;
            _sessionCache = sessionCache;
            _processedIds = processedIds;
            _deferredMasks = deferredMasks;
        }

        public void SyncAll(List<FigmaNode> topNodes, Transform root, Action<int, int, string> onProgress, CancellationToken ct)
        {
            int total = 0;
            foreach (var n in topNodes) total += CountNodes(n);
            int current = 0;

            foreach (var node in topNodes)
                SyncRecursive(node, root, root.name, ref current, total, onProgress, ct, 0);
        }

        private void SyncRecursive(FigmaNode node, Transform parent, string path, ref int current, int total, Action<int, int, string> onProgress, CancellationToken ct, int depth)
        {
            if (node == null) return;
            ct.ThrowIfCancellationRequested();

            current++;
            onProgress?.Invoke(current, total, node.name);

            FigmaElement element = null;
            if (_existingCache != null && _existingCache.TryGetValue(node.id, out var cachedElement))
            {
                if (cachedElement != null)
                {
                    element = cachedElement;
                    
                    FigmaParserUtils.EnsureUnpacked(element.gameObject);
                    FigmaParserUtils.EnsureUnpacked(parent.gameObject);
                    
                    element.transform.SetParent(parent, false);
                    UpdatedCount++;
                }
                else
                {
                    _existingCache.Remove(node.id);
                }
            }

            if (element == null)
            {
                FigmaParserUtils.EnsureUnpacked(parent.gameObject);
                GameObject go = new GameObject(node.name);
                go.transform.SetParent(parent, false);
                element = go.AddComponent<FigmaElement>();
                if (go.GetComponent<RectTransform>() == null) go.AddComponent<RectTransform>();
                element.FigmaNodeId = node.id;
                CreatedCount++;
            }

            _processedIds.Add(node.id);
            _sessionCache[node.id] = element;

            if (_handlerContext.Settings == null || !_handlerContext.Settings.preserveUnityNames)
                element.name = node.name;

            foreach (var handler in _handlers)
            {
                try { if (handler.CanHandle(node)) handler.Apply(node, element, _handlerContext); }
                catch (Exception e) { FigmaLog.Error($"{FigmaLog.VersionPrefix}Error in {handler.GetType().Name} for {node.name}: {e.Message}"); }
            }

            if (node.isMask || node.clipsContent)
                _deferredMasks.Add((node, element, depth));

            if (node.children != null)
            {
                var previousParent = _handlerContext.ParentNode;
                _handlerContext.ParentNode = node;
                try
                {
                    foreach (var child in node.children)
                        SyncRecursive(child, element.transform, path + "/" + node.name, ref current, total, onProgress, ct, depth + 1);
                }
                finally
                {
                    _handlerContext.ParentNode = previousParent;
                }
            }
        }

        private int CountNodes(FigmaNode node)
        {
            int count = 1;
            if (node.children != null)
                foreach (var child in node.children) count += CountNodes(child);
            return count;
        }
    }
}
