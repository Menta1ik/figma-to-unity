using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FigmaImporter.V2.Data;

namespace FigmaImporter.V2.Core.Validation
{
    public enum Severity { Fatal, Recoverable, Warning }
    public enum FixAction { None, Clamp, SkipSubtree, Placeholder }

    public class TransformAuditIssue
    {
        public string NodePath;
        public Severity Severity;
        public string RuleCode;
        public string Message;
        public FixAction Action;

        public override string ToString() => 
            $"<color={(Severity == Severity.Fatal ? "red" : "yellow")}>[{Severity}] {RuleCode}</color> at <b>{NodePath}</b>: {Message} -> <i>Result: {Action}</i>";
    }

    public class TransformAuditReport
    {
        private List<TransformAuditIssue> _issues = new List<TransformAuditIssue>();

        public void AddIssue(TransformAuditIssue issue) => _issues.Add(issue);
        public bool HasFatal(string path) => _issues.Any(i => i.NodePath == path && i.Severity == Severity.Fatal);

        public void PrintReport()
        {
            if (_issues.Count == 0)
            {
                Debug.Log("<color=green>[Transform Audit]</color> Clean! No geometry issues detected.");
                return;
            }

            int fatals = _issues.Count(i => i.Severity == Severity.Fatal);
            int warnings = _issues.Count(i => i.Severity != Severity.Fatal);

            Debug.Log($"<color=orange>[Transform Audit Report]</color> Found {_issues.Count} issues ({fatals} Fatal, {warnings} Warnings).");

            foreach (var issue in _issues)
            {
                if (issue.Severity == Severity.Fatal)
                    Debug.LogError(issue.ToString());
                else
                    Debug.LogWarning(issue.ToString());
            }
        }
    }

    public static class GeometryValidator
    {
        public const float Epsilon = 0.001f;
        public const float MaxSafeCoord = 50000f;

        // STAGE 1: PREFLIGHT
        public static bool ValidatePreflight(FigmaNode node, string path, TransformAuditReport report)
        {
            if (node.absoluteBoundingBox == null) return true;
            var b = node.absoluteBoundingBox;

            // GROUP nodes typically don't have intrinsic bounds and we handle them in TransformHandler
            if (node.type == "GROUP") return true;

            if (IsInvalid(b.x) || IsInvalid(b.y) || IsInvalid(b.width) || IsInvalid(b.height))
            {
                report.AddIssue(new TransformAuditIssue { 
                    NodePath = path, Severity = Severity.Fatal, RuleCode = "INVALID_MATH_PREFLIGHT", 
                    Message = $"NaN bounds detected: {b.width}x{b.height} at ({b.x}, {b.y})", Action = FixAction.SkipSubtree 
                });
                return false;
            }

            if (b.width < Epsilon || b.height < Epsilon)
            {
                report.AddIssue(new TransformAuditIssue { 
                    NodePath = path, Severity = Severity.Warning, RuleCode = "ZERO_SIZE_PREFLIGHT", 
                    Message = $"Zero size detected: {b.width}x{b.height}. Object might be invisible, but continuing sync for children.", Action = FixAction.Clamp 
                });
                return true; // Do not interrupt branch
            }
            return true;
        }

        // STAGE 2: AFTER PARENTING
        public static bool ValidateAfterParenting(RectTransform rt, string path, TransformAuditReport report)
        {
            return ValidateRectTransform(rt, path, "AFTER_PARENTING", report);
        }

        // STAGE 3: AFTER LAYOUT
        public static bool ValidateAfterLayout(RectTransform rt, string path, TransformAuditReport report)
        {
            return ValidateRectTransform(rt, path, "AFTER_LAYOUT", report);
        }

        private static bool ValidateRectTransform(RectTransform rt, string path, string stage, TransformAuditReport report)
        {
            if (rt == null) return false;

            bool isFatal = IsInvalid(rt.anchoredPosition.x) || IsInvalid(rt.anchoredPosition.y) ||
                           IsInvalid(rt.sizeDelta.x) || IsInvalid(rt.sizeDelta.y) ||
                           IsInvalid(rt.localScale.x) || IsInvalid(rt.localScale.y) || IsInvalid(rt.localScale.z) ||
                           IsInvalid(rt.anchorMin.x) || IsInvalid(rt.anchorMax.x) || IsInvalid(rt.pivot.x);

            if (!isFatal)
            {
                Vector3[] corners = new Vector3[4];
                rt.GetWorldCorners(corners);
                foreach (var c in corners)
                {
                    if (IsInvalid(c.x) || IsInvalid(c.y) || Mathf.Abs(c.x) > MaxSafeCoord || Mathf.Abs(c.y) > MaxSafeCoord)
                    {
                        isFatal = true;
                        break;
                    }
                }
            }

            if (isFatal)
            {
                report.AddIssue(new TransformAuditIssue { 
                    NodePath = path, Severity = Severity.Fatal, RuleCode = stage + "_NAN", 
                    Message = "Critical NaN or World Coordinate Explosion detected", Action = FixAction.Placeholder 
                });
                ResetToSafe(rt);
                return false;
            }
            return true;
        }

        public static void ResetToSafe(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(100, 100);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        public static bool ShouldApplyMask(FigmaNode node, out bool useAlphaMask)
        {
            useAlphaMask = false;
            if (!node.clipsContent) return false;
            if (node.children == null || node.children.Count == 0) return false;

            // Determine mask type: if circle/star/vector - complex Alpha Mask needed
            string type = node.type.ToUpper();
            if (type == "ELLIPSE" || type == "STAR" || type == "VECTOR" || type == "REGULAR_POLYGON")
            {
                useAlphaMask = true;
            }

            bool hasChildrenOutside = false;
            var parentBox = new Rect(node.absoluteBoundingBox.x, node.absoluteBoundingBox.y, node.absoluteBoundingBox.width, node.absoluteBoundingBox.height);
            
            foreach (var child in node.children)
            {
                if (child.absoluteBoundingBox == null) continue;
                var childBox = new Rect(child.absoluteBoundingBox.x, child.absoluteBoundingBox.y, child.absoluteBoundingBox.width, child.absoluteBoundingBox.height);
                
                if (childBox.xMin < parentBox.xMin - 0.1f || childBox.yMin < parentBox.yMin - 0.1f || 
                    childBox.xMax > parentBox.xMax + 0.1f || childBox.yMax > parentBox.yMax + 0.1f)
                {
                    hasChildrenOutside = true;
                    break;
                }
            }
            return hasChildrenOutside;
        }

        // STAGE 4: GRAPHIC VALIDATION (Render crash protection)
        public static void ValidateGraphic(GameObject go, string path, TransformAuditReport report)
        {
            // 0. Protection from NaN coords across whole object (Fallback Handler)
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                if (IsInvalid(rt.anchoredPosition.x) || IsInvalid(rt.sizeDelta.x) || IsInvalid(rt.localScale.x))
                {
                    report.AddIssue(new TransformAuditIssue { 
                        NodePath = path, Severity = Severity.Fatal, RuleCode = "FATAL_GRAPHIC_NAN", 
                        Message = "Coordinate explosion reached Graphic validation. Disabling rendering.", Action = FixAction.Placeholder 
                    });
                    ResetToSafe(rt);
                    var canvasRenderer = go.GetComponent<CanvasRenderer>();
                    if (canvasRenderer != null) canvasRenderer.cull = true;
                }
            }

            // 1. TextMeshPro Check (Most frequent cause of Invalid AABB)
            var tmp = go.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmp != null)
            {
                if (tmp.fontSize <= 0 || IsInvalid(tmp.fontSize) || IsInvalid(tmp.margin.x))
                {
                    report.AddIssue(new TransformAuditIssue { 
                        NodePath = path, Severity = Severity.Fatal, RuleCode = "INVALID_TMP", 
                        Message = $"Critical TMP geometry: fontSize is {tmp.fontSize}.", Action = FixAction.Placeholder 
                    });
                    tmp.fontSize = 14; 
                    tmp.margin = Vector4.zero;
                    if (string.IsNullOrEmpty(tmp.text)) tmp.text = " "; // Empty text with AutoSize breaks AABB
                }
                tmp.overflowMode = TMPro.TextOverflowModes.Overflow; // Extra protection
            }

            // 2. Image Check
            var img = go.GetComponent<UnityEngine.UI.Image>();
            if (img != null && img.sprite != null)
            {
                // Check texture dimensions inside sprite
                if (img.sprite.rect.width <= 0 || img.sprite.rect.height <= 0)
                {
                    report.AddIssue(new TransformAuditIssue { 
                        NodePath = path, Severity = Severity.Fatal, RuleCode = "INVALID_SPRITE", 
                        Message = "Attempted to draw a 0x0 sprite.", Action = FixAction.Placeholder 
                    });
                    img.sprite = null; // Remove corrupted sprite, leave transparent background
                }
            }
        }

        public static bool IsInvalid(float v) => float.IsNaN(v) || float.IsInfinity(v);
    }
}
