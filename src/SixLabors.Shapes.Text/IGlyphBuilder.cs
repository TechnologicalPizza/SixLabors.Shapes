using SixLabors.Fonts;

namespace SixLabors.Shapes.Text
{
    /// <summary>
    /// A version of <see cref="IGlyphRenderer"/> that stores it's result.
    /// </summary>
    public interface IGlyphBuilder : IGlyphRenderer
    {
        /// <summary>
        /// Build the current paths.
        /// </summary>
        /// <returns>The <see cref="IPathCollection"/> path.</returns>
        IPathCollection BuildPath();
    }
}
