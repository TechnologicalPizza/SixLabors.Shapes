// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using SixLabors.Fonts;
using SixLabors.Primitives;

namespace SixLabors.Shapes.Text
{
    /// <summary>
    /// Rendering surface that Fonts can use to generate Shapes.
    /// </summary>
    public class BaseGlyphBuilder : IGlyphBuilder
    {
        private readonly List<IPath> _paths;
        private PointF _currentPoint;

        /// <summary>
        /// The underlying path builder.
        /// </summary>
        protected PathBuilder Builder { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseGlyphBuilder"/> class.
        /// </summary>
        public BaseGlyphBuilder()
        {
            // glyphs are renderd realative to bottom left so invert the Y axis to allow it to render on top left origin surface
            this.Builder = new PathBuilder();
            _paths = new List<IPath>();
            _currentPoint = default;
        }

        /// <inheritdoc/>
        public IPathCollection BuildPath()
        {
            return new PathCollection(this._paths);
        }

        /// <inheritdoc/>
        public RectangleF GetPath(ICollection<IPath> output)
        {
            foreach (var path in _paths)
                output.Add(path);
            return PathCollection.GetBounds(_paths);
        }

        /// <inheritdoc/>
        void IGlyphRenderer.EndText()
        {
        }

        /// <inheritdoc/>
        void IGlyphRenderer.BeginText(RectangleF bounds)
        {
            this.BeginText(bounds);
        }

        /// <inheritdoc/>
        bool IGlyphRenderer.BeginGlyph(RectangleF bounds, GlyphRendererParameters paramaters)
        {
            this.Builder.Clear();
            this.BeginGlyph(bounds);
            return true;
        }

        /// <summary>
        /// Begins the figure.
        /// </summary>
        void IGlyphRenderer.BeginFigure()
        {
            this.Builder.StartFigure();
        }

        /// <summary>
        /// Draws a cubic bezier from the current point to the <paramref name="point"/>.
        /// </summary>
        /// <param name="secondControlPoint">The second control point.</param>
        /// <param name="thirdControlPoint">The third control point.</param>
        /// <param name="point">The point.</param>
        void IGlyphRenderer.CubicBezierTo(PointF secondControlPoint, PointF thirdControlPoint, PointF point)
        {
            this.Builder.AddBezier(this._currentPoint, secondControlPoint, thirdControlPoint, point);
            this._currentPoint = point;
        }

        /// <summary>
        /// Ends the glyph.
        /// </summary>
        void IGlyphRenderer.EndGlyph()
        {
            this._paths.Add(this.Builder.Build());
        }

        /// <summary>
        /// Ends the figure.
        /// </summary>
        void IGlyphRenderer.EndFigure()
        {
            this.Builder.CloseFigure();
        }

        /// <summary>
        /// Draws a line from the current point  to the <paramref name="point"/>.
        /// </summary>
        /// <param name="point">The point.</param>
        void IGlyphRenderer.LineTo(PointF point)
        {
            this.Builder.AddLine(this._currentPoint, point);
            this._currentPoint = point;
        }

        /// <summary>
        /// Moves to current point to the supplied vector.
        /// </summary>
        /// <param name="point">The point.</param>
        void IGlyphRenderer.MoveTo(PointF point)
        {
            this.Builder.StartFigure();
            this._currentPoint = point;
        }

        /// <summary>
        /// Draws a quadratics bezier from the current point to the <paramref name="point"/>.
        /// </summary>
        /// <param name="secondControlPoint">The second control point.</param>
        /// <param name="point">The point.</param>
        void IGlyphRenderer.QuadraticBezierTo(PointF secondControlPoint, PointF point)
        {
            this.Builder.AddBezier(this._currentPoint, secondControlPoint, point);
            this._currentPoint = point;
        }

        /// <summary>Called before any glyphs have been rendered.</summary>
        /// <param name="rect">The bounds the text will be rendered at and at whats size.</param>
        protected virtual void BeginText(RectangleF rect)
        {
        }

        /// <summary>Begins the glyph.</summary>
        /// <param name="rect">The bounds the glyph will be rendered at and at what size.</param>
        protected virtual void BeginGlyph(RectangleF rect)
        {
        }

        /// <summary>
        /// Clears the current path.
        /// </summary>
        public virtual void Clear()
        {
            Builder.Clear();
            _paths.Clear();
            _currentPoint = default;
        }
    }
}
