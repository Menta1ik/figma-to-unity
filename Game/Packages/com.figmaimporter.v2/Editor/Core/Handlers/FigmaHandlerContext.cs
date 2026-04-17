using System.Collections.Generic;
using FigmaImporter.V2.Data;
using TMPro;

namespace FigmaImporter.V2.Core.Handlers
{
    public class FigmaHandlerContext
    {
        public FigmaImporterSettings Settings { get; set; }
        public List<FigmaNode> ImageNodesToDownload { get; } = new List<FigmaNode>();
        
        // Font mapping for typography synchronization
        public List<FontMapping> FontMappings { get; set; } = new List<FontMapping>();
        public TMP_FontAsset GlobalFont { get; set; }
        public bool ForceUpdate { get; set; }
    }
}
