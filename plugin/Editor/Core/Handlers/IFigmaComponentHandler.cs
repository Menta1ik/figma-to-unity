using FigmaImporter.V2.Data;

namespace FigmaImporter.V2.Core.Handlers
{
    public interface IFigmaComponentHandler
    {
        /// <summary>
        /// Checks if this handler can process the specified Figma node.
        /// </summary>
        bool CanHandle(FigmaNode node);

        /// <summary>
        /// Applies Figma node properties to the Unity GameObject.
        /// </summary>
        void Apply(FigmaNode node, FigmaElement target, FigmaHandlerContext context);
    }
}
