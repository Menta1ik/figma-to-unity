using System;
using System.Collections.Generic;
using UnityEngine;

namespace FigmaImporter.V2.Data
{
    [Serializable]
    public class FigmaColor
    {
        public float r, g, b, a;
        public Color ToUnityColor(float opacity = 1f) => new Color(r, g, b, a * opacity);
    }

    [Serializable]
    public class FigmaVector
    {
        public float x, y;
        public Vector2 ToUnityVector() => new Vector2(x, y);
    }

    [Serializable]
    public class FigmaBoundingBox
    {
        public float x, y, width, height;
    }

    [Serializable]
    public class FigmaNode
    {
        public string id;
        public string name;
        public string type;
        public bool visible = true;
        public float opacity = 1f;
        
        public FigmaBoundingBox absoluteBoundingBox;
        public FigmaBoundingBox absoluteRenderBounds;
        
        public bool clipsContent;
        
        public string lastModified; 
        public string componentId; 

        public List<FigmaNode> children;
        
        public string characters;
        public FigmaTextStyle style;
        
        public List<FigmaFill> fills;
        public List<FigmaFill> strokes;
        public float strokeWeight;
        public float cornerRadius;
        
        [NonSerialized] public string computedHash;
    }

    [Serializable]
    public class FigmaTextStyle
    {
        public string fontFamily;
        public string fontPostScriptName;
        public int fontWeight;
        public float fontSize;
        public string textAlignHorizontal;
        public string textAlignVertical;
        public float lineHeightPx;
        public string textCase; 
    }

    [Serializable]
    public class FigmaFill
    {
        public string type; 
        public FigmaColor color;
        public float opacity = 1f;
        public string imageRef;
        public bool visible = true;
    }

    [Serializable]
    public class FigmaFileResponse
    {
        public string name;
        public FigmaNode document;
        public Dictionary<string, FigmaNodeContainer> nodes;
    }

    [Serializable]
    public class FigmaNodeContainer
    {
        public FigmaNode document;
    }
}
