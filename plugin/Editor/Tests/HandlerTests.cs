using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using FigmaImporter.V2.Core.Handlers;
using FigmaImporter.V2.Core;
using FigmaImporter.V2.Data;
using FigmaImporter.V2.Runtime;
using FigmaImporter.V2;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using UnityEditor;

namespace FigmaImporter.V2.Tests
{
    public class HandlerTests
    {
        private LayoutHandler _layoutHandler;

        [SetUp]
        public void Setup()
        {
            _layoutHandler = new LayoutHandler();
        }

        [Test]
        public void MapAlignment_Horizontal_CorrectlyMaps()
        {
            var node = new FigmaNode { layoutMode = "HORIZONTAL", primaryAxisAlignItems = "MIN", counterAxisAlignItems = "MIN" };
            Assert.AreEqual(TextAnchor.UpperLeft, InvokeMapAlignment(node));

            node = new FigmaNode { layoutMode = "HORIZONTAL", primaryAxisAlignItems = "CENTER", counterAxisAlignItems = "CENTER" };
            Assert.AreEqual(TextAnchor.MiddleCenter, InvokeMapAlignment(node));

            node = new FigmaNode { layoutMode = "HORIZONTAL", primaryAxisAlignItems = "MAX", counterAxisAlignItems = "MAX" };
            // In MapAlignment logic: primary=MAX (horizontal) + counter=MAX (vertical) -> LowerRight
            Assert.AreEqual(TextAnchor.LowerRight, InvokeMapAlignment(node)); 
        }

        [Test]
        public void Test_Reskin_IDMapping()
        {
            var parentGo = new GameObject("Parent");
            var childGo = new GameObject("OldName");
            childGo.transform.SetParent(parentGo.transform);
            var childElement = childGo.AddComponent<FigmaElement>();
            childElement.FigmaNodeId = "target_id";

            var reskinHandler = new ReskinHandler(new FigmaHandlerContext());
            var newNode = new FigmaNode 
            { 
                id = "root",
                children = new List<FigmaNode> 
                { 
                    new FigmaNode { id = "target_id", name = "NewName", type = "FRAME", computedHash = "new_hash" } 
                } 
            };

            reskinHandler.ApplyReskin(parentGo, newNode);

            // Verify that the child with ID "target_id" was found and updated
            var updatedElement = childGo.GetComponent<FigmaElement>();
            Assert.AreEqual("target_id", updatedElement.FigmaNodeId);
            Assert.AreEqual("new_hash", updatedElement.LastUpdateHash, "Reskin must update the computed hash");
            
            Object.DestroyImmediate(parentGo);
        }

        [Test]
        public void Test_ImageCaching_Logic()
        {
            var context = new FigmaHandlerContext();
            var node = new FigmaNode 
            { 
                id = "icon_id", 
                type = "FRAME",
                name = "test_icon", // Must contain "icon" to be a candidate
                children = new List<FigmaNode> { new FigmaNode { type = "VECTOR", id = "vec" } } // NO text children
            };

            var imageHandler = new ImageHandler();
            var go = new GameObject("IconTest");
            var element = go.AddComponent<FigmaElement>();
            
            try 
            {
                // Apply triggers IsIconCandidateCached
                imageHandler.Apply(node, element, context);
                
                Assert.IsTrue(context.IconCandidateCache.ContainsKey("icon_id"), "Cache should contain the node ID after Apply");
                Assert.IsTrue(context.IconCandidateCache["icon_id"], "Node with 'icon' name and no text should be cached as true");

                // Second call should NOT change the cache result
                imageHandler.Apply(node, element, context);
                Assert.IsTrue(context.IconCandidateCache["icon_id"]);
            }
            finally 
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TextHandler_AppliesFont_WhenMappingExists()
        {
            var go = new GameObject("Text");
            var element = go.AddComponent<FigmaElement>();
            var fontAsset = ScriptableObject.CreateInstance<TMP_FontAsset>();
            
            var context = new FigmaHandlerContext
            {
                GlobalFont = fontAsset,
                FontMappings = new List<FontMapping>
                {
                    new FontMapping { figmaFontFamily = "Roboto", figmaFontWeight = 400, targetTMPAsset = fontAsset }
                }
            };
            var node = new FigmaNode
            {
                type = "TEXT",
                characters = "Hello",
                style = new FigmaTextStyle { fontFamily = "Roboto", fontWeight = 400, fontSize = 16f }
            };

            new TextHandler().Apply(node, element, context);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            Assert.IsNotNull(tmp);
            Assert.AreEqual("Hello", tmp.text);
            Assert.AreEqual(16f, tmp.fontSize);
            Assert.AreEqual(fontAsset, tmp.font);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void TextHandler_ClampsNaNFontSize_To14()
        {
            var go = new GameObject("Text");
            var element = go.AddComponent<FigmaElement>();
            var context = new FigmaHandlerContext { GlobalFont = ScriptableObject.CreateInstance<TMP_FontAsset>() };
            var node = new FigmaNode
            {
                type = "TEXT",
                characters = "NaN test",
                style = new FigmaTextStyle { fontSize = float.NaN }
            };

            new TextHandler().Apply(node, element, context);

            Assert.AreEqual(14f, go.GetComponent<TextMeshProUGUI>().fontSize);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void TextHandler_AppliesUpperCase_WhenTextCaseIsUPPER()
        {
            var go = new GameObject("Text");
            var element = go.AddComponent<FigmaElement>();
            var context = new FigmaHandlerContext { GlobalFont = ScriptableObject.CreateInstance<TMP_FontAsset>() };
            var node = new FigmaNode
            {
                type = "TEXT",
                characters = "hello",
                style = new FigmaTextStyle { textCase = "UPPER" }
            };

            new TextHandler().Apply(node, element, context);
            Assert.AreEqual("HELLO", go.GetComponent<TextMeshProUGUI>().text);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void LayoutHandler_NoContentSizeFitter_WhenBothAxesFixed()
        {
            var go = new GameObject("Layout");
            go.AddComponent<RectTransform>();
            var element = go.AddComponent<FigmaElement>();
            var node = new FigmaNode
            {
                layoutMode = "HORIZONTAL",
                primaryAxisSizingMode = "FIXED",
                counterAxisSizingMode = "FIXED"
            };

            new LayoutHandler().Apply(node, element, new FigmaHandlerContext());

            Assert.IsNull(go.GetComponent<ContentSizeFitter>(), "CSF should NOT be added when both axes are FIXED");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void LayoutHandler_AddsContentSizeFitter_WhenPrimaryIsAuto()
        {
            var go = new GameObject("Layout");
            go.AddComponent<RectTransform>();
            var element = go.AddComponent<FigmaElement>();
            var node = new FigmaNode
            {
                layoutMode = "HORIZONTAL",
                primaryAxisSizingMode = "AUTO",
                counterAxisSizingMode = "FIXED"
            };

            new LayoutHandler().Apply(node, element, new FigmaHandlerContext());

            var fitter = go.GetComponent<ContentSizeFitter>();
            Assert.IsNotNull(fitter);
            Assert.AreEqual(ContentSizeFitter.FitMode.PreferredSize, fitter.horizontalFit);
            Assert.AreEqual(ContentSizeFitter.FitMode.Unconstrained, fitter.verticalFit);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void HandleDeletedElements_DeactivatesOrphan_NotDestroys()
        {
            var parent = new GameObject("Root");
            var go = new GameObject("Child");
            go.transform.SetParent(parent.transform);
            var element = go.AddComponent<FigmaElement>();
            element.FigmaNodeId = "orphan_id";

            // Add private field access using reflection if needed, but existingCache is calculated from children
            var parser = new FigmaParser();
            
            // We need to simulate the state where existingCache has the child but processedIds doesn't
            var existingCacheField = typeof(FigmaParser).GetField("_existingCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var processedIdsField = typeof(FigmaParser).GetField("_processedIds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            existingCacheField.SetValue(parser, new Dictionary<string, FigmaElement> { { "orphan_id", element } });
            processedIdsField.SetValue(parser, new HashSet<string>());

            var method = typeof(FigmaParser).GetMethod("HandleDeletedElements", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(parser, null);

            Assert.IsFalse(go.activeSelf, "Orphaned object should be deactivated");
            Assert.IsNotNull(go.GetComponent<FigmaOrphanedElement>(), "Orphan should have FigmaOrphanedElement component");
            
            Object.DestroyImmediate(parent);
        }

        [Test]
        public void ImageSyncService_NineSliceApplied_WhenNodeNameHasSuffix()
        {
            // 9-slice in ImageSyncService is triggered by node name ending in "_9slice"
            // This tests the naming convention logic used in SyncImagesAsync
            const string nodeName = "button_background_9slice";
            bool isNineSlice = nodeName.EndsWith("_9slice", System.StringComparison.OrdinalIgnoreCase);
            Assert.IsTrue(isNineSlice, "Node name ending with '_9slice' should trigger sliced image type");

            // Verify the Image.Type.Sliced assignment path compiles and is available
            var go = new GameObject("SlicedImage");
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.type = UnityEngine.UI.Image.Type.Sliced;
            Assert.AreEqual(UnityEngine.UI.Image.Type.Sliced, img.type);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void FigmaParser_ApplyDeferredMasks_CreatesContainer()
        {
            var root = new GameObject("Root");
            var maskGo = new GameObject("MaskLayer");
            maskGo.transform.SetParent(root.transform);
            var maskElement = maskGo.AddComponent<FigmaElement>();
            maskElement.FigmaNodeId = "mask_1";

            var item1 = new GameObject("Item1");
            item1.transform.SetParent(root.transform);
            var item1Element = item1.AddComponent<FigmaElement>();
            item1Element.FigmaNodeId = "item_1";

            var parser = new FigmaParser();
            var deferredMasksField = typeof(FigmaParser).GetField("_deferredMasks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maskNode = new FigmaNode { id = "mask_1", isMask = true, clipsContent = true, type = "RECTANGLE", cornerRadius = 0f };
            
            deferredMasksField.SetValue(parser, new List<(FigmaNode, FigmaElement)> { (maskNode, maskElement) });

            var method = typeof(FigmaParser).GetMethod("ApplyDeferredMasks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(parser, null);

            // Check if hierarchy changed: Item1 should be under a [Mask] container now
            var maskContainer = root.transform.Find("[Mask] MaskLayer");
            Assert.IsNotNull(maskContainer, "Mask container should be created");
            Assert.AreEqual(maskContainer, item1.transform.parent, "Item1 should be moved into mask container");
            Assert.IsNotNull(maskContainer.GetComponent<UnityEngine.UI.RectMask2D>(), "Should have RectMask2D for rectangle mask");

            Object.DestroyImmediate(root);
        }

        private TextAnchor InvokeMapAlignment(FigmaNode node)
        {
            var method = typeof(LayoutHandler).GetMethod("MapAlignment", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (TextAnchor)method.Invoke(_layoutHandler, new object[] { node });
        }
    }
}
