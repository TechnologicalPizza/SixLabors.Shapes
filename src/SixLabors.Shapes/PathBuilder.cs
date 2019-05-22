// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Numerics;
using SixLabors.Memory;
using SixLabors.Primitives;

namespace SixLabors.Shapes
{
    /// <summary>
    /// Allow you to derivatively build shapes and paths.
    /// </summary>
    public class PathBuilder : IDisposable
    {
        private readonly List<Figure> _figures;
        private readonly Matrix3x2 defaultTransform;

        private Figure currentFigure;
        private Matrix3x2 currentTransform;
        private Matrix3x2 setTransform;

        /// <summary>
        /// Gets the object validity.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PathBuilder" /> class.
        /// </summary>
        public PathBuilder() : this(Matrix3x2.Identity)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PathBuilder"/> class.
        /// </summary>
        /// <param name="defaultTransform">The default transform.</param>
        public PathBuilder(Matrix3x2 defaultTransform)
        {
            _figures = new List<Figure>();
            this.defaultTransform = defaultTransform;
            this.currentFigure = new Figure();
            this.Clear();
            this.ResetTransform();
        }

        /// <summary>
        /// Sets the translation to be applied to all items to follow being applied to the <see cref="PathBuilder"/>.
        /// </summary>
        /// <param name="translation">The translation.</param>
        /// <returns>The <see cref="PathBuilder"/></returns>
        public PathBuilder SetTransform(Matrix3x2 translation)
        {
            this.setTransform = translation;
            this.currentTransform = this.setTransform * this.defaultTransform;
            return this;
        }

        /// <summary>
        /// Sets the origin all subsequent point should be relative to.
        /// </summary>
        /// <param name="origin">The origin.</param>
        /// <returns>The <see cref="PathBuilder"/></returns>
        public PathBuilder SetOrigin(PointF origin)
        {
            // the new origin should be transofrmed based on the default transform
            this.setTransform.Translation = origin;
            this.currentTransform = this.setTransform * this.defaultTransform;
            return this;
        }

        /// <summary>
        /// Resets the translation to the default.
        /// </summary>
        /// <returns>The <see cref="PathBuilder"/></returns>
        public PathBuilder ResetTransform()
        {
            this.setTransform = Matrix3x2.Identity;
            this.currentTransform = this.setTransform * this.defaultTransform;
            return this;
        }

        /// <summary>
        /// Resets the origin to the default.
        /// </summary>
        /// <returns>The <see cref="PathBuilder"/></returns>
        public PathBuilder ResetOrigin()
        {
            this.setTransform.Translation = Vector2.Zero;
            this.currentTransform = this.setTransform * this.defaultTransform;
            return this;
        }

        /// <summary>
        /// Adds the line connecting the current point to the new point.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <returns>The <see cref="PathBuilder"/></returns>
        public PathBuilder AddLine(PointF start, PointF end)
        {
            end = PointF.Transform(end, this.currentTransform);
            start = PointF.Transform(start, this.currentTransform);
            this.currentFigure.AddSegment(new LinearLineSegment(start, end));
            return this;
        }

        /// <summary>
        /// Adds the line connecting the current point to the new point.
        /// </summary>
        /// <param name="x1">The x1.</param>
        /// <param name="y1">The y1.</param>
        /// <param name="x2">The x2.</param>
        /// <param name="y2">The y2.</param>
        /// <returns>The <see cref="PathBuilder"/></returns>
        public PathBuilder AddLine(float x1, float y1, float x2, float y2)
        {
            this.AddLine(new PointF(x1, y1), new PointF(x2, y2));
            return this;
        }

        /// <summary>
        /// Adds a series of line segments connecting the current point to the new points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>The <see cref="PathBuilder"/></returns>
        public PathBuilder AddLines(IEnumerable<PointF> points)
        {
            if (points is null)
                throw new ArgumentNullException(nameof(points));

            using (var segment = new LinearLineSegment(PrimitiveListPools.PointF.Rent(points)))
                AddSegment(segment);
            return this;
        }

        /// <summary>
        /// Adds a series of line segments connecting the current point to the new points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>The <see cref="PathBuilder"/></returns>
        public PathBuilder AddLines(params PointF[] points)
        {
            using (var segment = new LinearLineSegment(points))
                AddSegment(segment);
            return this;
        }

        /// <summary>
        /// Adds the segment.
        /// </summary>
        /// <param name="segment">The segment.</param>
        /// <returns>The <see cref="PathBuilder"/></returns>
        public PathBuilder AddSegment(ILineSegment segment)
        {
            this.currentFigure.AddSegment(segment.Transform(this.currentTransform));
            return this;
        }

        /// <summary>
        /// Adds a quadratic bezier curve to the current figure joining the last point to the endPoint.
        /// </summary>
        /// <param name="startPoint">The start point.</param>
        /// <param name="controlPoint">The control point1.</param>
        /// <param name="endPoint">The end point.</param>
        /// <returns>The <see cref="PathBuilder"/></returns>
        public PathBuilder AddBezier(PointF startPoint, PointF controlPoint, PointF endPoint)
        {
            Vector2 startPointVector = startPoint;
            Vector2 controlPointVector = controlPoint;
            Vector2 endPointVector = endPoint;

            Vector2 c1 = (((controlPointVector - startPointVector) * 2) / 3) + startPointVector;
            Vector2 c2 = (((controlPointVector - endPointVector) * 2) / 3) + endPointVector;

            this.AddBezier(startPointVector, c1, c2, endPoint);
            return this;
        }

        /// <summary>
        /// Adds a cubic bezier curve to the current figure joining the last point to the endPoint.
        /// </summary>
        /// <param name="startPoint">The start point.</param>
        /// <param name="controlPoint1">The control point1.</param>
        /// <param name="controlPoint2">The control point2.</param>
        /// <param name="endPoint">The end point.</param>
        /// <returns>The <see cref="PathBuilder"/></returns>
        public PathBuilder AddBezier(PointF startPoint, PointF controlPoint1, PointF controlPoint2, PointF endPoint)
        {
            this.currentFigure.AddSegment(new CubicBezierLineSegment(
                PointF.Transform(startPoint, this.currentTransform),
                PointF.Transform(controlPoint1, this.currentTransform),
                PointF.Transform(controlPoint2, this.currentTransform),
                PointF.Transform(endPoint, this.currentTransform)));

            return this;
        }

        /// <summary>
        /// Starts a new figure but leaves the previous one open.
        /// </summary>
        /// <returns>The <see cref="PathBuilder"/></returns>
        public PathBuilder StartFigure()
        {
            if (!this.currentFigure.IsEmpty)
            {
                this.currentFigure = new Figure();
                this._figures.Add(this.currentFigure);
            }
            else
            {
                this.currentFigure.IsClosed = false;
            }
            return this;
        }

        /// <summary>
        /// Closes the current figure.
        /// </summary>
        /// <returns>The <see cref="PathBuilder"/></returns>
        public PathBuilder CloseFigure()
        {
            this.currentFigure.IsClosed = true;
            this.StartFigure();
            return this;
        }

        /// <summary>
        /// Closes the current figure.
        /// </summary>
        /// <returns>The <see cref="PathBuilder"/></returns>
        public PathBuilder CloseAllFigures()
        {
            foreach (Figure f in this._figures)
                f.IsClosed = true;
            
            this.CloseFigure();
            return this;
        }

        /// <summary>
        /// Builds a complex polygon from the current set of operations.
        /// </summary>
        /// <returns>
        /// The current set of operations as a complex polygon 
        /// or an empty path if no operations were performed.
        /// </returns>
        public IPath Build()
        {
            int count = 0;
            foreach (var x in _figures)
                if (!x.IsEmpty)
                    count++;

            if (count == 0)
                return Path.Empty;

            if (count == 1)
                return _figures[0].Build();

            var paths = ShapeListPools.Path.Rent(count);
            foreach (var x in _figures)
                if (!x.IsEmpty)
                    paths.Add(x.Build());
            return new ComplexPolygon(paths);
        }

        /// <summary>
        /// Resets this instance, clearing any drawn paths and reseting any transforms.
        /// </summary>
        /// <returns>The <see cref="PathBuilder"/></returns>
        public PathBuilder Reset()
        {
            this.Clear();
            this.ResetTransform();
            return this;
        }

        /// <summary>
        /// Clears and disposes all drawn paths (making them unusable), leaving any applied transforms.
        /// </summary>
        public void Clear()
        {
            _figures.Clear();
            currentFigure.Clear();
            this._figures.Add(this.currentFigure);
        }

        /// <summary>
        /// Dispose the object, making it unusable.
        /// </summary>
        public void Dispose()
        {
            if (!IsDisposed)
            {
                Clear();
                IsDisposed = true;
            }
        }

        private class Figure : IDisposable
        {
            private List<ILineSegment> _segments;

            public bool IsDisposed { get; private set; }
            public bool IsClosed { get; set; } = false;
            public bool IsEmpty => this._segments.Count == 0;

            public Figure()
            {
                _segments = ShapeListPools.Line.Rent();
            }

            public void AddSegment(ILineSegment segment)
            {
                this._segments.Add(segment);
            }

            public Path Build()
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(Figure));

                var path = this.IsClosed ?
                    new Polygon(this._segments) : new Path(this._segments);

                return path;
            }

            public void Clear()
            {
                _segments.Clear();
            }

            public void Dispose()
            {
                if (!IsDisposed)
                {
                    Clear();
                    ShapeListPools.Line.Return(_segments);
                    _segments = null;

                    IsDisposed = true;
                }
            }
        }
    }
}
