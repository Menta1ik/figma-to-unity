using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using FigmaImporter.V2.Core.Handlers;
using FigmaImporter.V2.Data;
using FigmaImporter.V2.Runtime;
using System.Collections.Generic;

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

        private TextAnchor InvokeMapAlignment(FigmaNode node)
        {
            var method = typeof(LayoutHandler).GetMethod("MapAlignment", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (TextAnchor)method.Invoke(_layoutHandler, new object[] { node });
        }
    }
}
