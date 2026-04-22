using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using FigmaImporter.V2.Core;
using FigmaImporter.V2.Core.Handlers;
using FigmaImporter.V2.Data;

namespace FigmaImporter.V2.Tests
{
    public class ConstraintsMappingTests
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

        private (GameObject, RectTransform, FigmaElement) CreateMockStructure(float pw, float ph, float x, float y, float w, float h)
        {
            var parentGo = new GameObject("Parent");
            var parentRt = parentGo.AddComponent<RectTransform>();
            parentRt.sizeDelta = new Vector2(pw, ph);

            var childGo = new GameObject("Child");
            childGo.transform.SetParent(parentGo.transform);
            var childRt = childGo.AddComponent<RectTransform>();
            var childElement = childGo.AddComponent<FigmaElement>();

            _context.ParentNode = new FigmaNode
            {
                absoluteBoundingBox = new FigmaBoundingBox { x = 0, y = 0, width = pw, height = ph }
            };

            return (childGo, childRt, childElement);
        }

        [Test]
        public void Constraints_Center_PositionsCorrectly()
        {
            // Parent: 1000x1000
            // Child: 200x200, positioned exactly in the center (x=400, y=400)
            var (go, rt, element) = CreateMockStructure(1000, 1000, 400, 400, 200, 200);
            var node = new FigmaNode
            {
                absoluteBoundingBox = new FigmaBoundingBox { x = 400, y = 400, width = 200, height = 200 },
                constraints = new FigmaConstraints { horizontal = "CENTER", vertical = "CENTER" }
            };

            try
            {
                _transformHandler.Apply(node, element, _context);

                Assert.AreEqual(new Vector2(0.5f, 0.5f), rt.anchorMin);
                Assert.AreEqual(new Vector2(0.5f, 0.5f), rt.anchorMax);
                
                // In Unity, if anchors are at 0.5, and object is 200x200 centered:
                // offsetMin = (-100, -100), offsetMax = (100, 100)
                Assert.AreEqual(-100f, rt.offsetMin.x, 0.01f);
                Assert.AreEqual(100f, rt.offsetMax.x, 0.01f);
                Assert.AreEqual(-100f, rt.offsetMin.y, 0.01f);
                Assert.AreEqual(100f, rt.offsetMax.y, 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(go.transform.parent.gameObject);
            }
        }

        [Test]
        public void Constraints_Right_Bottom_PositionsCorrectly()
        {
            // Parent: 1000x1000
            // Child: 100x100, placed at the very bottom right (x=900, y=900)
            var (go, rt, element) = CreateMockStructure(1000, 1000, 900, 900, 100, 100);
            var node = new FigmaNode
            {
                absoluteBoundingBox = new FigmaBoundingBox { x = 900, y = 900, width = 100, height = 100 },
                constraints = new FigmaConstraints { horizontal = "RIGHT", vertical = "BOTTOM" }
            };

            try
            {
                _transformHandler.Apply(node, element, _context);

                Assert.AreEqual(new Vector2(1f, 0f), rt.anchorMin);
                Assert.AreEqual(new Vector2(1f, 0f), rt.anchorMax);
                
                // Right = 900 -> distance from right edge is 0.
                // Unity right anchor is at 1000. offsetMax.x = (900+100) - 1000 = 0.
                // offsetMin.x = 900 - 1000 = -100.
                Assert.AreEqual(-100f, rt.offsetMin.x, 0.01f);
                Assert.AreEqual(0f, rt.offsetMax.x, 0.01f);

                // Bottom = 900 (Figma y). distance from top is 900. distance from bottom is 0.
                // Unity bottom anchor is at 0. node bottom is at 0 (absolute). 
                // parentHeight = 1000. Figma y=900, h=100 -> Bottom is at 1000 (top-down) -> 0 (bottom-up).
                Assert.AreEqual(0f, rt.offsetMin.y, 0.01f);
                Assert.AreEqual(100f, rt.offsetMax.y, 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(go.transform.parent.gameObject);
            }
        }
    }
}
