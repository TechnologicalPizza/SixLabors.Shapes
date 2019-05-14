using System.Collections.Generic;
using SixLabors.Fonts;
using SixLabors.Primitives;

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

        /// <summary>
        /// Adds the current paths to the list.
        /// </summary>
        /// <param name="output">The output list.</param>
        /// <returns>The bounds of the enclosing path.</returns>
        RectangleF GetPath(ICollection<IPath> output);
    }
}
