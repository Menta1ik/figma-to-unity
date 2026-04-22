using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FigmaImporter.V2.Data;
using FigmaImporter.V2.Runtime;

namespace FigmaImporter.V2.Core
{
    /// <summary>
    /// Handles deferred mask application and mask container lifecycle.
    /// Figma masks affect subsequent siblings; Unity masks affect children.
    /// This class bridges the gap by creating container GameObjects.
    /// </summary>
    internal static class FigmaMaskResolver
    {
        /// <summary>
        /// Dismantles all existing [Mask] containers, moving children back to the original parent.
        /// Call before sync to prevent recursive nesting from previous runs.
        /// </summary>
        public static void DismantleAll(Transform root)
        {
            var all = root.GetComponentsInChildren<RectTransform>(true);
            for (int i = all.Length - 1; i >= 0; i--)
            {
                var rt = all[i];
                if (rt != null && rt.name.StartsWith("[Mask]"))
                {
                    Transform parent = rt.parent;
                    if (parent != null)
                    {
                        int siblingIndex = rt.GetSiblingIndex();
                        var children = new List<Transform>();
                        foreach (Transform child in rt) children.Add(child);

                        foreach (Transform child in children)
                        {
                            child.SetParent(parent, true);
                            child.SetSiblingIndex(siblingIndex++);
                        }
                    }
                    Object.DestroyImmediate(rt.gameObject);
                }
            }
        }

        /// <summary>
        /// Applies deferred masks after the full hierarchy has been synced.
        /// </summary>
        public static void ApplyDeferred(List<(FigmaNode node, FigmaElement element, int depth)> deferredMasks)
        {
            if (deferredMasks == null || deferredMasks.Count == 0) return;

            foreach (var (maskNode, maskElement, depth) in deferredMasks)
            {
                if (maskElement == null || maskElement.gameObject == null) continue;

                var maskGo = maskElement.gameObject;
                var maskTransform = maskGo.transform;
                var parentTransform = maskTransform.parent;

                if (parentTransform == null) continue;

                // Handle standard Frame clipping (clipsContent)
                if (!maskNode.isMask && maskNode.clipsContent)
                {
                    if (maskGo.GetComponent<RectMask2D>() == null && maskGo.GetComponent<Mask>() == null)
                        maskGo.AddComponent<RectMask2D>();
                    continue;
                }

                // Handle Figma Mask (isMask: true)
                if (maskNode.isMask)
                {
                    int maskSiblingIndex = maskTransform.GetSiblingIndex();

                    var containerGo = new GameObject($"[Mask] {maskGo.name}");
                    var containerRect = containerGo.AddComponent<RectTransform>();
                    containerGo.transform.SetParent(parentTransform, false);
                    containerGo.transform.SetSiblingIndex(maskSiblingIndex);

                    var maskRect = maskGo.GetComponent<RectTransform>();
                    if (maskRect != null)
                    {
                        containerRect.anchorMin        = maskRect.anchorMin;
                        containerRect.anchorMax        = maskRect.anchorMax;
                        containerRect.pivot            = maskRect.pivot;
                        containerRect.sizeDelta        = maskRect.sizeDelta;
                        containerRect.anchoredPosition = maskRect.anchoredPosition;
                    }

                    int currentStencilDepth = 0;
                    Transform t = containerGo.transform.parent;
                    while (t != null)
                    {
                        if (t.GetComponent<Mask>() != null) currentStencilDepth++;
                        t = t.parent;
                    }

                    bool isComplex = (maskNode.type == "VECTOR" || maskNode.type == "BOOLEAN_OPERATION" || maskNode.type == "STAR");
                    bool forceRectMask = (currentStencilDepth >= 3) || !isComplex;

                    if (forceRectMask)
                    {
                        if (containerGo.GetComponent<Mask>() != null) Object.DestroyImmediate(containerGo.GetComponent<Mask>());
                        if (containerGo.GetComponent<Image>() != null) containerGo.GetComponent<Image>().enabled = false;

                        if (containerGo.GetComponent<RectMask2D>() == null)
                            containerGo.AddComponent<RectMask2D>();

                        string reason = !isComplex ? "Simple Shape" : "Depth Limit Reached";
                        FigmaLog.Verbose($"[Mask Optimization] '{maskGo.name}' (type: {maskNode.type}) using RectMask2D (Reason: {reason}, Current Depth: {currentStencilDepth})");
                    }
                    else
                    {
                        if (containerGo.GetComponent<RectMask2D>() != null) Object.DestroyImmediate(containerGo.GetComponent<RectMask2D>());
                        if (maskGo.GetComponent<RectMask2D>() != null) Object.DestroyImmediate(maskGo.GetComponent<RectMask2D>());
                        if (maskGo.GetComponent<Mask>() != null) Object.DestroyImmediate(maskGo.GetComponent<Mask>());

                        var maskImage = containerGo.GetComponent<Image>() ?? containerGo.AddComponent<Image>();
                        maskImage.enabled = true;
                        maskImage.color = new Color(1, 1, 1, 0.01f);
                        maskImage.raycastTarget = false;

                        var sourceImage = maskGo.GetComponent<Image>();
                        if (sourceImage != null && sourceImage.sprite != null)
                        {
                            maskImage.sprite = sourceImage.sprite;
                            maskImage.type = sourceImage.type;
                        }

                        if (containerGo.GetComponent<Mask>() == null)
                            containerGo.AddComponent<Mask>().showMaskGraphic = false;

                        FigmaLog.Verbose($"[Mask Optimization] '{maskGo.name}' using STENCIL Mask (Complex Shape and Depth: {currentStencilDepth})");
                    }

                    // Move original element and siblings into container
                    maskTransform.SetParent(containerRect, false);

                    var siblingsToMask = new List<Transform>();
                    for (int i = maskSiblingIndex + 1; i < parentTransform.childCount; i++)
                    {
                        var sibling = parentTransform.GetChild(i);
                        if (sibling != containerRect.transform)
                            siblingsToMask.Add(sibling);
                    }

                    foreach (var sibling in siblingsToMask)
                    {
                        FigmaParserUtils.EnsureUnpacked(sibling.gameObject);
                        sibling.SetParent(containerRect, true);
                    }
                }
            }
        }

        /// <summary>
        /// Removes orphaned [Mask] containers that have no managed FigmaElement children.
        /// </summary>
        public static void CleanupOrphaned(Transform root)
        {
            var allCandidates = root.GetComponentsInChildren<RectTransform>(true);
            for (int i = allCandidates.Length - 1; i >= 0; i--)
            {
                var rt = allCandidates[i];
                if (rt != null && rt.name.StartsWith("[Mask]") && rt.GetComponent<FigmaElement>() == null)
                {
                    bool anyManagedChild = false;
                    foreach (Transform child in rt)
                        if (child.GetComponent<FigmaElement>() != null) anyManagedChild = true;
                    if (!anyManagedChild) Object.DestroyImmediate(rt.gameObject);
                }
            }
        }
    }
}
