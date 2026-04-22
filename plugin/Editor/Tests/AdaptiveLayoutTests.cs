using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using FigmaImporter.V2.Core;
using FigmaImporter.V2.Core.Handlers;
using FigmaImporter.V2.Data;
using FigmaImporter.V2.Runtime;
using System.Collections.Generic;
using UnityEditor;

namespace FigmaImporter.V2.Tests
{
    public class AdaptiveLayoutTests
    {
        private TransformHandler _transformHandler;
        private FigmaHandlerContext _context;

        [SetUp]
        public void Setup()
        {
            _transformHandler = new TransformHandler();
            _context = new FigmaHandlerContext
            {
                Settings = ScriptableObject.CreateInstance<FigmaImporterSettings>()
            };
            _context.Settings.enableConstraintsTranslation = true;
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_context.Settings);
        }

        [Test]
        public void TransformHandler_Stretch_CalculatesCorrectOffsets()
        {
            var parentNode = new FigmaNode
            {
                absoluteBoundingBox = new FigmaBoundingBox { x = 0, y = 0, width = 1000, height = 1000 }
            };
            var node = new FigmaNode
            {
                absoluteBoundingBox = new FigmaBoundingBox { x = 50, y = 50, width = 900, height = 900 },
                constraints = new FigmaConstraints { horizontal = "LEFT_RIGHT", vertical = "TOP_BOTTOM" }
            };

            var go = new GameObject("Test");
            var rt = go.AddComponent<RectTransform>();
            var element = go.AddComponent<FigmaElement>();
            
            _context.ParentNode = parentNode;

            try
            {
                _transformHandler.Apply(node, element, _context);

                Assert.AreEqual(new Vector2(0, 0), rt.anchorMin);
                Assert.AreEqual(new Vector2(1, 1), rt.anchorMax);
                
                // Left = 50, Right = 50 -> offsetMin.x = 50, offsetMax.x = -50
                Assert.AreEqual(50f, rt.offsetMin.x);
                Assert.AreEqual(-50f, rt.offsetMax.x);
                
                // Top = 50, Bottom = 50 -> offsetMax.y = -50, offsetMin.y = 50
                Assert.AreEqual(-50f, rt.offsetMax.y);
                Assert.AreEqual(50f, rt.offsetMin.y);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TransformHandler_Scale_CalculatesCorrectAnchors()
        {
            var parentNode = new FigmaNode
            {
                absoluteBoundingBox = new FigmaBoundingBox { x = 0, y = 0, width = 1000, height = 1000 }
            };
            var node = new FigmaNode
            {
                absoluteBoundingBox = new FigmaBoundingBox { x = 200, y = 100, width = 600, height = 200 },
                constraints = new FigmaConstraints { horizontal = "SCALE", vertical = "SCALE" }
            };

            var go = new GameObject("Test");
            var rt = go.AddComponent<RectTransform>();
            var element = go.AddComponent<FigmaElement>();
            
            _context.ParentNode = parentNode;

            try
            {
                _transformHandler.Apply(node, element, _context);

                // Width: 200/1000 = 0.2 to 800/1000 = 0.8
                Assert.AreEqual(0.2f, rt.anchorMin.x, 0.001f);
                Assert.AreEqual(0.8f, rt.anchorMax.x, 0.001f);
                
                // Height: 100/1000 = 0.1 (Top). In Unity Y: 1.0 - 0.1 = 0.9 (Max), 1.0 - 0.3 = 0.7 (Min)
                Assert.AreEqual(0.7f, rt.anchorMin.y, 0.001f);
                Assert.AreEqual(0.9f, rt.anchorMax.y, 0.001f);
                
                Assert.AreEqual(Vector2.zero, rt.offsetMin);
                Assert.AreEqual(Vector2.zero, rt.offsetMax);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Parser_ParentNodeContext_IsRestoredAfterRecursion()
        {
            var root = new GameObject("Root");

            var child2 = new FigmaNode { id = "child2", name = "Child 2" };
            var child1 = new FigmaNode
            {
                id = "child1",
                name = "Child 1",
                children = new List<FigmaNode> { child2 }
            };
            var rootNode = new FigmaNode
            {
                id = "root",
                name = "Root",
                children = new List<FigmaNode> { child1 }
            };

            var context = new FigmaHandlerContext();
            var processedIds = new HashSet<string>();
            var sessionCache = new Dictionary<string, FigmaElement>();
            var deferredMasks = new List<(FigmaNode, FigmaElement, int)>();

            var walker = new FigmaTreeWalker(
                new List<IFigmaComponentHandler>(),
                context,
                new Dictionary<string, FigmaElement>(),
                sessionCache,
                processedIds,
                deferredMasks);

            walker.SyncAll(new List<FigmaNode> { rootNode }, root.transform, null, default);

            // After full sync, context.ParentNode should be back to null (or previous value)
            Assert.IsNull(context.ParentNode, "ParentNode should be restored to null after root processing");

            Object.DestroyImmediate(root);
        }

        [Test]
        public void TransformHandler_AutoLayoutConflict_OverridesConstraints()
        {
            var node = new FigmaNode
            {
                layoutMode = "HORIZONTAL", // Auto Layout active
                constraints = new FigmaConstraints { horizontal = "LEFT_RIGHT", vertical = "TOP_BOTTOM" },
                absoluteBoundingBox = new FigmaBoundingBox { x = 0, y = 0, width = 100, height = 100 }
            };

            var go = new GameObject("Test");
            var rt = go.AddComponent<RectTransform>();
            var element = go.AddComponent<FigmaElement>();

            try
            {
                _transformHandler.Apply(node, element, _context);

                // Should fallback to Absolute (0.5, 0.5 anchors).
                // Wait, in my implementation it falls back to ApplyAbsolutePosition which sets (0.5, 0.5).
                Assert.AreEqual(new Vector2(0.5f, 0.5f), rt.anchorMin);
                Assert.AreEqual(new Vector2(0.5f, 0.5f), rt.anchorMax);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
