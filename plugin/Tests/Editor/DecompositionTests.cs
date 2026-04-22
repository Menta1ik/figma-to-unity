using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using FigmaImporter.V2.Core;
using FigmaImporter.V2.Core.Handlers;
using FigmaImporter.V2.Data;
using FigmaImporter.V2.Runtime;
using FigmaImporter.V2;

namespace FigmaImporter.V2.Tests
{
    public class DecompositionTests
    {
        private List<GameObject> _created;

        [SetUp]
        public void Setup()
        {
            _created = new List<GameObject>();
        }

        [TearDown]
        public void Teardown()
        {
            foreach (var go in _created)
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
        }

        private GameObject Track(GameObject go)
        {
            _created.Add(go);
            return go;
        }

        private FigmaNode MakeNode(string id, string name, List<FigmaNode> children = null)
        {
            return new FigmaNode { id = id, name = name, children = children };
        }

        // ========================================================
        // FigmaTreeWalker Tests
        // ========================================================

        private FigmaTreeWalker CreateWalker(
            List<IFigmaComponentHandler> handlers = null,
            FigmaHandlerContext context = null,
            Dictionary<string, FigmaElement> existingCache = null,
            Dictionary<string, FigmaElement> sessionCache = null,
            HashSet<string> processedIds = null,
            List<(FigmaNode, FigmaElement, int)> deferredMasks = null)
        {
            return new FigmaTreeWalker(
                handlers ?? new List<IFigmaComponentHandler>(),
                context ?? new FigmaHandlerContext(),
                existingCache ?? new Dictionary<string, FigmaElement>(),
                sessionCache ?? new Dictionary<string, FigmaElement>(),
                processedIds ?? new HashSet<string>(),
                deferredMasks ?? new List<(FigmaNode, FigmaElement, int)>());
        }

        [Test]
        public void TreeWalker_CreatedCount_IncreasesForNewNodes()
        {
            var root = Track(new GameObject("Root"));
            var tree = MakeNode("r", "Root", new List<FigmaNode>
            {
                MakeNode("c1", "Child1"),
                MakeNode("c2", "Child2")
            });

            var walker = CreateWalker();
            walker.SyncAll(new List<FigmaNode> { tree }, root.transform, null, default);

            Assert.AreEqual(3, walker.CreatedCount);
            Assert.AreEqual(0, walker.UpdatedCount);
        }

        [Test]
        public void TreeWalker_UpdatedCount_IncreasesForExistingElements()
        {
            var root = Track(new GameObject("Root"));
            var child = new GameObject("ExistingChild");
            child.transform.SetParent(root.transform);
            child.AddComponent<RectTransform>();
            var element = child.AddComponent<FigmaElement>();
            element.FigmaNodeId = "existing_1";

            var existingCache = new Dictionary<string, FigmaElement> { { "existing_1", element } };
            var walker = CreateWalker(existingCache: existingCache);
            walker.SyncAll(new List<FigmaNode> { MakeNode("existing_1", "Updated") }, root.transform, null, default);

            Assert.AreEqual(1, walker.UpdatedCount);
            Assert.AreEqual(0, walker.CreatedCount);
        }

        [Test]
        public void TreeWalker_ProcessedIds_PopulatedAfterSync()
        {
            var root = Track(new GameObject("Root"));
            var processedIds = new HashSet<string>();
            var tree = MakeNode("a", "A", new List<FigmaNode>
            {
                MakeNode("b", "B"),
                MakeNode("c", "C")
            });

            var walker = CreateWalker(processedIds: processedIds);
            walker.SyncAll(new List<FigmaNode> { tree }, root.transform, null, default);

            Assert.AreEqual(3, processedIds.Count);
            Assert.IsTrue(processedIds.Contains("a"));
            Assert.IsTrue(processedIds.Contains("b"));
            Assert.IsTrue(processedIds.Contains("c"));
        }

        [Test]
        public void TreeWalker_DeferredMasks_CollectedForMaskNodes()
        {
            var root = Track(new GameObject("Root"));
            var deferredMasks = new List<(FigmaNode, FigmaElement, int)>();

            var tree = MakeNode("root", "Root", new List<FigmaNode>
            {
                new FigmaNode { id = "mask1", name = "Mask1", isMask = true },
                new FigmaNode { id = "clip1", name = "Clip1", clipsContent = true }
            });

            var walker = CreateWalker(deferredMasks: deferredMasks);
            walker.SyncAll(new List<FigmaNode> { tree }, root.transform, null, default);

            Assert.AreEqual(2, deferredMasks.Count);
            Assert.IsTrue(deferredMasks[0].Item1.isMask);
            Assert.IsTrue(deferredMasks[1].Item1.clipsContent);
        }

        [Test]
        public void TreeWalker_HandlerError_DoesNotAbortSync()
        {
            var root = Track(new GameObject("Root"));
            var handlers = new List<IFigmaComponentHandler> { new ThrowingHandler() };
            var tree = MakeNode("a", "A", new List<FigmaNode> { MakeNode("b", "B") });

            var walker = CreateWalker(handlers: handlers);
            walker.SyncAll(new List<FigmaNode> { tree }, root.transform, null, default);

            Assert.AreEqual(2, walker.CreatedCount);
        }

        [Test]
        public void TreeWalker_Cancellation_ThrowsOperationCanceled()
        {
            var root = Track(new GameObject("Root"));
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var walker = CreateWalker();
            Assert.Throws<OperationCanceledException>(() =>
                walker.SyncAll(new List<FigmaNode> { MakeNode("a", "A") }, root.transform, null, cts.Token));
        }

        private class ThrowingHandler : IFigmaComponentHandler
        {
            public bool CanHandle(FigmaNode node) => true;
            public void Apply(FigmaNode node, FigmaElement target, FigmaHandlerContext context) =>
                throw new InvalidOperationException("Intentional test error");
        }

        // ========================================================
        // FigmaMaskResolver Tests
        // ========================================================

        [Test]
        public void MaskResolver_DismantleAll_ReparentsAndDestroysContainer()
        {
            var root = Track(new GameObject("Root"));

            var container = new GameObject("[Mask] MyMask");
            container.AddComponent<RectTransform>();
            container.transform.SetParent(root.transform);

            var item1 = new GameObject("Item1");
            item1.AddComponent<RectTransform>();
            item1.transform.SetParent(container.transform);

            var item2 = new GameObject("Item2");
            item2.AddComponent<RectTransform>();
            item2.transform.SetParent(container.transform);

            FigmaMaskResolver.DismantleAll(root.transform);

            Assert.IsNull(root.transform.Find("[Mask] MyMask"), "Container should be destroyed");
            Assert.AreEqual(2, root.transform.childCount, "Children should be reparented to root");
        }

        [Test]
        public void MaskResolver_DismantleAll_PreservesSiblingOrder()
        {
            var root = Track(new GameObject("Root"));

            var container = new GameObject("[Mask] M");
            container.AddComponent<RectTransform>();
            container.transform.SetParent(root.transform);

            var a = new GameObject("A");
            a.AddComponent<RectTransform>();
            a.transform.SetParent(container.transform);

            var b = new GameObject("B");
            b.AddComponent<RectTransform>();
            b.transform.SetParent(container.transform);

            var c = new GameObject("C");
            c.AddComponent<RectTransform>();
            c.transform.SetParent(root.transform);

            FigmaMaskResolver.DismantleAll(root.transform);

            Assert.AreEqual(3, root.transform.childCount);
            Assert.AreEqual("A", root.transform.GetChild(0).name);
            Assert.AreEqual("B", root.transform.GetChild(1).name);
            Assert.AreEqual("C", root.transform.GetChild(2).name);
        }

        [Test]
        public void MaskResolver_ApplyDeferred_ClipsContent_AddsRectMask2D()
        {
            var root = Track(new GameObject("Frame1"));
            root.AddComponent<RectTransform>();
            var element = root.AddComponent<FigmaElement>();
            element.FigmaNodeId = "f1";

            var node = new FigmaNode { id = "f1", name = "Frame1", clipsContent = true, isMask = false };
            var masks = new List<(FigmaNode, FigmaElement, int)> { (node, element, 0) };

            FigmaMaskResolver.ApplyDeferred(masks);

            Assert.IsNotNull(root.GetComponent<RectMask2D>(), "Should add RectMask2D for clipsContent");
        }

        [Test]
        public void MaskResolver_CleanupOrphaned_RemovesEmptyContainers()
        {
            var root = Track(new GameObject("Root"));

            var empty = new GameObject("[Mask] Empty");
            empty.AddComponent<RectTransform>();
            empty.transform.SetParent(root.transform);

            var active = new GameObject("[Mask] Active");
            active.AddComponent<RectTransform>();
            active.transform.SetParent(root.transform);
            var managedChild = new GameObject("Child");
            managedChild.AddComponent<FigmaElement>();
            managedChild.transform.SetParent(active.transform);

            FigmaMaskResolver.CleanupOrphaned(root.transform);

            Assert.IsNull(root.transform.Find("[Mask] Empty"), "Empty container should be removed");
            Assert.IsNotNull(root.transform.Find("[Mask] Active"), "Container with managed children should remain");
        }

        [Test]
        public void MaskResolver_ApplyDeferred_NullOrEmpty_NoOp()
        {
            Assert.DoesNotThrow(() => FigmaMaskResolver.ApplyDeferred(null));
            Assert.DoesNotThrow(() => FigmaMaskResolver.ApplyDeferred(new List<(FigmaNode, FigmaElement, int)>()));
        }

        // ========================================================
        // FigmaOrphanManager Tests
        // ========================================================

        [Test]
        public void OrphanManager_NullCache_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => FigmaOrphanManager.MarkOrphans(null, new HashSet<string>()));
        }

        [Test]
        public void OrphanManager_ProcessedElement_NotDeactivated()
        {
            var go = Track(new GameObject("Keep"));
            var element = go.AddComponent<FigmaElement>();
            element.FigmaNodeId = "keep_me";

            var cache = new Dictionary<string, FigmaElement> { { "keep_me", element } };
            var processed = new HashSet<string> { "keep_me" };

            FigmaOrphanManager.MarkOrphans(cache, processed, false);

            Assert.IsTrue(go.activeSelf, "Processed element should stay active");
            Assert.IsNull(go.GetComponent<FigmaOrphanedElement>(), "Should not be marked as orphan");
        }

        [Test]
        public void OrphanManager_NullElementInCache_Skipped()
        {
            var cache = new Dictionary<string, FigmaElement> { { "gone_id", null } };
            Assert.DoesNotThrow(() => FigmaOrphanManager.MarkOrphans(cache, new HashSet<string>(), false));
        }

        // ========================================================
        // FigmaFontAuditor Tests
        // ========================================================

        private FigmaFileResponse MakeResponseWithTextNode(string fontFamily, int fontWeight)
        {
            return new FigmaFileResponse
            {
                document = new FigmaNode
                {
                    id = "doc", name = "Doc", type = "DOCUMENT",
                    children = new List<FigmaNode>
                    {
                        new FigmaNode
                        {
                            id = "t1", name = "Text1", type = "TEXT",
                            style = new FigmaTextStyle
                            {
                                fontFamily = fontFamily,
                                fontPostScriptName = fontFamily.Replace(" ", "-"),
                                fontWeight = fontWeight
                            }
                        }
                    }
                }
            };
        }

        [Test]
        public void FontAuditor_EmptyResponse_NoException()
        {
            var table = ScriptableObject.CreateInstance<FontMappingTable>();
            try
            {
                var auditor = new FigmaFontAuditor(table);
                Assert.DoesNotThrow(() => auditor.Audit(new FigmaFileResponse()));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(table);
            }
        }

        [Test]
        public void FontAuditor_MappedFont_DoesNotThrow()
        {
            var table = ScriptableObject.CreateInstance<FontMappingTable>();
            table.Mappings.Add(new FontMapping { figmaFontFamily = "Roboto", figmaFontWeight = 400 });
            try
            {
                var auditor = new FigmaFontAuditor(table);
                Assert.DoesNotThrow(() => auditor.Audit(MakeResponseWithTextNode("Roboto", 400)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(table);
            }
        }

        [Test]
        public void FontAuditor_MissingFont_DoesNotThrow()
        {
            var table = ScriptableObject.CreateInstance<FontMappingTable>();
            try
            {
                var auditor = new FigmaFontAuditor(table);
                Assert.DoesNotThrow(() => auditor.Audit(MakeResponseWithTextNode("UnknownFont", 700)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(table);
            }
        }

        [Test]
        public void FontAuditor_NullFontMapTable_DoesNotThrow()
        {
            var auditor = new FigmaFontAuditor(null);
            Assert.DoesNotThrow(() => auditor.Audit(MakeResponseWithTextNode("AnyFont", 400)));
        }

        // ========================================================
        // FigmaParserUtils Tests
        // ========================================================

        [Test]
        public void ParserUtils_EnsureUnpacked_NullInput_NoException()
        {
            Assert.DoesNotThrow(() => FigmaParserUtils.EnsureUnpacked(null));
        }

        [Test]
        public void ParserUtils_EnsureUnpacked_NonPrefab_NoOp()
        {
            var go = Track(new GameObject("PlainObject"));
            string originalName = go.name;

            Assert.DoesNotThrow(() => FigmaParserUtils.EnsureUnpacked(go));
            Assert.AreEqual(originalName, go.name, "Non-prefab object should be unchanged");
        }
    }
}
