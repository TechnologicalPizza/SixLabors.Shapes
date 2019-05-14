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
    /// Represents a series of control points that will be joined by straight lines
    /// </summary>
    /// <seealso cref="ILineSegment" />
    public sealed class LinearLineSegment : ILineSegment
    {
        /// <summary>
        /// The collection of points.
        /// </summary>
        private List<PointF> _points;

        /// <inheritdoc/>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearLineSegment"/> class.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        public LinearLineSegment(PointF start, PointF end) : this(MergeConstructorValues(start, end, null))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearLineSegment" /> class.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="additionalPoints">Additional points</param>
        public LinearLineSegment(PointF start, PointF end, params PointF[] additionalPoints) :
            this(MergeConstructorValues(start, end, additionalPoints))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearLineSegment"/> class.
        /// </summary>
        /// <param name="points">The points.</param>
        public LinearLineSegment(PointF[] points) : this(PrimitiveListPools.PointF.Rent(points))
        {
        }

        internal LinearLineSegment(List<PointF> points)
        {
            Guard.MustBeGreaterThanOrEqualTo(points.Count, 2, nameof(points));

            this._points = points ?? throw new ArgumentNullException(nameof(points));
            this.EndPoint = this._points[this._points.Count - 1];
        }

        private static List<PointF> MergeConstructorValues(PointF p1, PointF p2, params PointF[] additional)
        {
            var list = PrimitiveListPools.PointF.Rent(2 + (additional?.Length).GetValueOrDefault());
            list.Add(p1);
            list.Add(p2);

            if (additional != null)
                foreach (var item in additional)
                    list.Add(item);

            return list;
        }

        /// <summary>
        /// Gets the end point.
        /// </summary>
        /// <value>
        /// The end point.
        /// </value>
        public PointF EndPoint { get; }

        /// <summary>
        /// Converts the <see cref="ILineSegment" /> into a simple linear path..
        /// </summary>
        /// <returns>
        /// Returns the current <see cref="ILineSegment" /> as simple linear path.
        /// </returns>
        public IReadOnlyList<PointF> Flatten()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(LinearLineSegment));
            return this._points;
        }

        /// <summary>
        /// Transforms the current LineSegment using specified matrix.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <returns>
        /// A line segment with the matrix applied to it.
        /// </returns>
        public LinearLineSegment Transform(Matrix3x2 matrix)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(LinearLineSegment));

            if (matrix.IsIdentity)
            {
                // no transform to apply skip it
                return this;
            }

            var transformedPoints = PrimitiveListPools.PointF.Rent(this._points.Count);
            for (int i = 0; i < this._points.Count; i++)
                transformedPoints[i] = PointF.Transform(this._points[i], matrix);

            return new LinearLineSegment(transformedPoints);
        }

        /// <summary>
        /// Transforms the current LineSegment using specified matrix.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <returns>A line segment with the matrix applied to it.</returns>
        ILineSegment ILineSegment.Transform(Matrix3x2 matrix) => this.Transform(matrix);

        /// <summary>
        /// Disposes the path, making it unusable.
        /// </summary>
        public void Dispose()
        {
            if (!IsDisposed)
            {
                PrimitiveListPools.PointF.Return(_points);
                _points = null;

                IsDisposed = true;
            }
        }
    }
}