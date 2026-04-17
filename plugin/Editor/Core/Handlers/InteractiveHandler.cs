using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FigmaImporter.V2.Data;

namespace FigmaImporter.V2.Core.Handlers
{
    public class InteractiveHandler : IFigmaComponentHandler
    {
        public bool CanHandle(FigmaNode node)
        {
            // Handle any nodes, checking for markers in the name
            return true;
        }

        public void Apply(FigmaNode node, FigmaElement target, FigmaHandlerContext context)
        {
            var settings = context.Settings;
            if (settings == null) return;

            string name = node.name;
            bool isInteractive = false;

            // 1. Button Handling [Btn]
            if (name.Contains(settings.buttonMarker))
            {
                if (target.GetComponent<Button>() == null)
                {
                    target.gameObject.AddComponent<Button>();
                }
                isInteractive = true;
            }

            // 2. Input Field Handling [Input]
            if (name.Contains(settings.inputMarker))
            {
                if (target.GetComponent<TMP_InputField>() == null)
                {
                    var input = target.gameObject.AddComponent<TMP_InputField>();
                    // Input field will handle its own children if needed, 
                    // but here we just add the component.
                }
                isInteractive = true;
            }

            // 3. Scroll Handling [Scroll]
            if (name.Contains(settings.scrollMarker))
            {
                if (target.GetComponent<ScrollRect>() == null)
                {
                    target.gameObject.AddComponent<ScrollRect>();
                }
                isInteractive = true;
            }

            // 4. Toggle Handling [Toggle]
            if (name.Contains(settings.toggleMarker))
            {
                if (target.GetComponent<Toggle>() == null)
                {
                    target.gameObject.AddComponent<Toggle>();
                }
                isInteractive = true;
            }

            // 5. Raycast Target Optimization
            if (settings.disableRaycastByDefault)
            {
                // Graphic includes Image, RawImage, TextMeshProUGUI
                var graphic = target.GetComponent<Graphic>();
                if (graphic != null)
                {
                    // If it is TEXT, check if it's part of an interactive element.
                    // To avoid "Raycast Hell", text is disabled by default unless it's a marker node.
                    graphic.raycastTarget = isInteractive;
                }
            }
        }
    }
}
