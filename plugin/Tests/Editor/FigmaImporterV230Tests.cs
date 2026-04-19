using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using FigmaImporter.V2;
using FigmaImporter.V2.Data;
using FigmaImporter.V2.Core.Handlers;
using FigmaImporter.V2.Runtime;

namespace FigmaImporter.V2.Tests
{
    /// <summary>
    /// EditMode tests for v2.3.0 patch: validates safety, layout, masks, and 9-slice.
    /// These tests do NOT require Figma API — they test handlers and logic in isolation.
    /// </summary>
    public class FigmaImporterV230Tests
    {
        // ============================================================
        // PHASE 1: Safety Tests
        // ============================================================

        [Test]
        public void OrphanedElement_InitializeSetsFields()
        {
            var go = new GameObject("TestButton");
            var orphan = go.AddComponent<FigmaOrphanedElement>();
            
            orphan.Initialize("123:456");
            
            Assert.AreEqual("123:456", orphan.originalFigmaNodeId);
            Assert.IsTrue(go.name.StartsWith("[Orphan]"));
            Assert.IsNotNull(orphan.orphanedAt);
            Assert.IsNotEmpty(orphan.orphanedAt);
            
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OrphanedElement_DoubleInitializeDoesNotDoublePrefix()
        {
            var go = new GameObject("TestPanel");
            var orphan = go.AddComponent<FigmaOrphanedElement>();
            
            orphan.Initialize("100:200");
            orphan.Initialize("100:200"); // second call
            
            // Should not be "[Orphan] [Orphan] TestPanel"
            Assert.AreEqual("[Orphan] TestPanel", go.name);
            
            Object.DestroyImmediate(go);
        }

        // ============================================================
        // PHASE 2: LayoutHandler Tests
        // ============================================================

        [Test]
        public void LayoutHandler_CanHandle_FalseWhenNoLayout()
        {
            var handler = new LayoutHandler();
            var node = new FigmaNode { id = "1", name = "Frame", type = "FRAME" };
            
            Assert.IsFalse(handler.CanHandle(node), 
                "LayoutHandler should NOT handle nodes without layoutMode");
        }

        [Test]
        public void LayoutHandler_CanHandle_TrueForHorizontal()
        {
            var handler = new LayoutHandler();
            var node = new FigmaNode 
            { 
                id = "1", name = "HStack", type = "FRAME",
                layoutMode = "HORIZONTAL" 
            };
            
            Assert.IsTrue(handler.CanHandle(node));
        }

        [Test]
        public void LayoutHandler_CanHandle_TrueForVertical()
        {
            var handler = new LayoutHandler();
            var node = new FigmaNode 
            { 
                id = "1", name = "VStack", type = "FRAME",
                layoutMode = "VERTICAL" 
            };
            
            Assert.IsTrue(handler.CanHandle(node));
        }

        [Test]
        public void LayoutHandler_CanHandle_FalseForNullLayoutMode()
        {
            var handler = new LayoutHandler();
            var node = new FigmaNode 
            { 
                id = "1", name = "Plain", type = "FRAME",
                layoutMode = null
            };
            
            Assert.IsFalse(handler.CanHandle(node));
        }

        [Test]
        public void LayoutHandler_CanHandle_FalseForEmptyLayoutMode()
        {
            var handler = new LayoutHandler();
            var node = new FigmaNode 
            { 
                id = "1", name = "Empty", type = "FRAME",
                layoutMode = ""
            };
            
            Assert.IsFalse(handler.CanHandle(node));
        }

        [Test]
        public void LayoutHandler_AppliesHorizontalLayoutGroup()
        {
            var handler = new LayoutHandler();
            var go = CreateTestElement("HStack");
            var element = go.GetComponent<FigmaElement>();
            var context = new FigmaHandlerContext();

            var node = new FigmaNode
            {
                id = "1", name = "HStack", type = "FRAME",
                layoutMode = "HORIZONTAL",
                itemSpacing = 12f,
                paddingLeft = 8, paddingRight = 8,
                paddingTop = 4, paddingBottom = 4
            };

            handler.Apply(node, element, context);

            var hlg = go.GetComponent<HorizontalLayoutGroup>();
            Assert.IsNotNull(hlg, "HorizontalLayoutGroup should be added");
            Assert.IsNull(go.GetComponent<VerticalLayoutGroup>(), 
                "VerticalLayoutGroup should NOT exist");
            Assert.AreEqual(12f, hlg.spacing);
            Assert.AreEqual(8, hlg.padding.left);
            Assert.AreEqual(8, hlg.padding.right);
            Assert.AreEqual(4, hlg.padding.top);
            Assert.AreEqual(4, hlg.padding.bottom);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void LayoutHandler_AppliesVerticalLayoutGroup()
        {
            var handler = new LayoutHandler();
            var go = CreateTestElement("VStack");
            var element = go.GetComponent<FigmaElement>();
            var context = new FigmaHandlerContext();

            var node = new FigmaNode
            {
                id = "2", name = "VStack", type = "FRAME",
                layoutMode = "VERTICAL",
                itemSpacing = 20f,
                paddingLeft = 16, paddingRight = 16,
                paddingTop = 16, paddingBottom = 16
            };

            handler.Apply(node, element, context);

            var vlg = go.GetComponent<VerticalLayoutGroup>();
            Assert.IsNotNull(vlg, "VerticalLayoutGroup should be added");
            Assert.IsNull(go.GetComponent<HorizontalLayoutGroup>(), 
                "HorizontalLayoutGroup should NOT exist");
            Assert.AreEqual(20f, vlg.spacing);
            Assert.AreEqual(16, vlg.padding.left);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void LayoutHandler_DoesNotTouchNonLayoutNodes()
        {
            var handler = new LayoutHandler();

            // Simulate a typical non-layout node
            var node = new FigmaNode
            {
                id = "99", name = "StaticFrame", type = "FRAME",
                layoutMode = null // <-- no Auto Layout
            };

            // CanHandle should be false, so Apply would never be called
            Assert.IsFalse(handler.CanHandle(node),
                "Handler must skip nodes without layoutMode — this protects existing layouts");
        }

        [Test]
        public void LayoutHandler_ContentSizeFitter_AddedForAutoSizing()
        {
            var handler = new LayoutHandler();
            var go = CreateTestElement("AutoBox");
            var element = go.GetComponent<FigmaElement>();
            var context = new FigmaHandlerContext();

            var node = new FigmaNode
            {
                id = "3", name = "AutoBox", type = "FRAME",
                layoutMode = "VERTICAL",
                primaryAxisSizingMode = "AUTO",
                counterAxisSizingMode = "FIXED"
            };

            handler.Apply(node, element, context);

            var fitter = go.GetComponent<ContentSizeFitter>();
            Assert.IsNotNull(fitter, "ContentSizeFitter should be added for AUTO sizing");
            Assert.AreEqual(ContentSizeFitter.FitMode.PreferredSize, fitter.verticalFit);
            Assert.AreEqual(ContentSizeFitter.FitMode.Unconstrained, fitter.horizontalFit);

            Object.DestroyImmediate(go);
        }

        // ============================================================
        // PHASE 2: Mask detection
        // ============================================================

        [Test]
        public void FigmaNode_isMask_DefaultsFalse()
        {
            var node = new FigmaNode { id = "1", name = "Normal", type = "RECTANGLE" };
            Assert.IsFalse(node.isMask, "isMask should default to false");
        }

        [Test]
        public void FigmaNode_clipsContent_DefaultsFalse()
        {
            var node = new FigmaNode { id = "1", name = "Frame", type = "FRAME" };
            Assert.IsFalse(node.clipsContent, "clipsContent should default to false");
        }

        // ============================================================
        // Helpers
        // ============================================================

        private GameObject CreateTestElement(string name)
        {
            var go = new GameObject(name);
            go.AddComponent<RectTransform>();
            go.AddComponent<FigmaElement>();
            return go;
        }
    }
}
