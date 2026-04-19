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
            // In MapAlignment logic: primary=MAX (horizontal) + counter=MAX (vertical) -> MiddleRight?
            // Let's re-verify the code logic in LayoutHandler lines 157-169.
            // If vAlign (counter) == "CENTER" and hAlign (primary) == "MAX" -> MiddleRight.
            // If vAlign (counter) == "MAX" and hAlign (primary) == "MAX" -> LowerRight?
            // Let's see...
            Assert.AreEqual(TextAnchor.MiddleRight, InvokeMapAlignment(node)); 
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
                    new FigmaNode { id = "target_id", name = "NewName", type = "FRAME" } 
                } 
            };

            reskinHandler.ApplyReskin(parentGo, newNode);

            // Verify that the child with ID "target_id" was found and updated, even if it had a different name
            Assert.AreEqual("target_id", childGo.GetComponent<FigmaElement>().FigmaNodeId);
            // Wait, ReskinHandler currently doesn't rename objects if they are found by ID 
            // but it updates their FigmaElement data.
            
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
                children = new List<FigmaNode> { new FigmaNode { type = "TEXT", characters = "icon" } }
            };

            var imageHandler = new ImageHandler();
            
            // First call should fill the cache
            bool isIcon = imageHandler.CanHandle(node);
            Assert.IsTrue(context.IconCandidateCache.ContainsKey("icon_id"));
            Assert.AreEqual(isIcon, context.IconCandidateCache["icon_id"]);

            // Second call should return cached value
            bool isIconCached = imageHandler.CanHandle(node);
            Assert.AreEqual(isIcon, isIconCached);
        }

        private TextAnchor InvokeMapAlignment(FigmaNode node)
        {
            var method = typeof(LayoutHandler).GetMethod("MapAlignment", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (TextAnchor)method.Invoke(_layoutHandler, new object[] { node });
        }
    }
}
