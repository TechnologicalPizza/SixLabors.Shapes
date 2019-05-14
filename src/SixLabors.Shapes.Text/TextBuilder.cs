// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using SixLabors.Fonts;
using SixLabors.Primitives;
using SixLabors.Shapes.Text;

namespace SixLabors.Shapes
{
    /// <summary>
    /// Text drawing extensions for a <see cref="PathBuilder"/>.
    /// </summary>
    public static class TextBuilder
    {
        /// <summary>
        /// Generates the shapes corresponding the glyphs described by the font and with the settings withing the FontSpan.
        /// </summary>
        /// <param name="builder">The renderer.</param>
        /// <param name="text">The text to generate glyphs for.</param>
        /// <param name="style">The style and settings to use while rendering the glyphs.</param>
        /// <returns>The <see cref="PathCollection"/>.</returns>
        public static IPathCollection GenerateGlyphs(IGlyphBuilder builder, ReadOnlySpan<char> text, RendererOptions style)
        {
            TextRenderer.RenderText(builder, text, style);
            return builder.BuildPath();
        }

        /// <summary>
        /// Generates the shapes corresponding the glyphs described by the font and with the settings withing the FontSpan.
        /// </summary>
        /// <param name="builder">The renderer.</param>
        /// <param name="text">The text to generate glyphs for.</param>
        /// <param name="style">The style and settings to use while rendering the glyphs.</param>
        /// <param name="output">The output for the path.</param>
        /// <returns>The bounds of the enclosing path.</returns>
        public static RectangleF GenerateGlyphs(IGlyphBuilder builder, ReadOnlySpan<char> text, RendererOptions style, ICollection<IPath> output)
        {
            TextRenderer.RenderText(builder, text, style);
            return builder.GetPath(output);
        }

        /// <summary>
        /// Generates the shapes corresponding the glyphs described by the font and with the settings withing the FontSpan.
        /// </summary>
        /// <param name="text">The text to generate glyphs for.</param>
        /// <param name="location">The location.</param>
        /// <param name="style">The style and settings to use while rendering the glyphs.</param>
        /// <returns>The <see cref="PathCollection"/></returns>
        public static IPathCollection GenerateGlyphs(ReadOnlySpan<char> text, PointF location, RendererOptions style)
        {
            var glyphBuilder = new GlyphBuilder(location);
            TextRenderer.RenderText(glyphBuilder, text, style);
            return glyphBuilder.BuildPath();
        }

        /// <summary>
        /// Generates the shapes corresponding the glyphs described by the font and with the settings withing the FontSpan.
        /// </summary>
        /// <param name="text">The text to generate glyphs for.</param>
        /// <param name="style">The style and settings to use while rendering the glyphs.</param>
        /// <returns>The <see cref="PathCollection"/></returns>
        public static IPathCollection GenerateGlyphs(ReadOnlySpan<char> text, RendererOptions style)
        {
            return GenerateGlyphs(text, PointF.Empty, style);
        }

        /// <summary>
        /// Generates the shapes corresponding the glyphs described by the font and with the setting in within the FontSpan along the described path.
        /// </summary>
        /// <param name="text">The text to generate glyphs for.</param>
        /// <param name="path">The path to draw the text in relation to.</param>
        /// <param name="style">The style and settings to use while rendering the glyphs.</param>
        /// <returns>The <see cref="PathCollection"/></returns>
        public static IPathCollection GenerateGlyphs(ReadOnlySpan<char> text, IPath path, RendererOptions style)
        {
            var glyphBuilder = new PathGlyphBuilder(path);
            TextRenderer.RenderText(glyphBuilder, text, style);
            return glyphBuilder.BuildPath();
        }
    }
}
