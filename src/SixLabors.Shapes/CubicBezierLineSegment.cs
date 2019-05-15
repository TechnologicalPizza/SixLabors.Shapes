﻿// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Numerics;
using SixLabors.Memory;
using SixLabors.Primitives;

namespace SixLabors.Shapes
{
    /// <summary>
    /// Represents a line segment that contains a lists of control points that will be rendered as a cubic bezier curve.
    /// </summary>
    /// <seealso cref="ILineSegment" />
    public sealed class CubicBezierLineSegment : ILineSegment
    {
        // code for this taken from <see href="http://devmag.org.za/2011/04/05/bzier-curves-a-tutorial/"/>
        private const float MinimumSqrDistance = 1.75f;
        private const float DivisionThreshold = -.9995f;

        /// <summary>
        /// The line points.
        /// </summary>
        private List<PointF> linePoints;
        private List<PointF> _controlPoints;

        /// <inheritdoc/>
        public bool IsDisposed { get; private set; }

        internal CubicBezierLineSegment(List<PointF> points)
        {
            _controlPoints = points ?? throw new ArgumentNullException(nameof(points));
            Guard.MustBeGreaterThanOrEqualTo(_controlPoints.Count, 4, nameof(points));

            int correctPointCount = (_controlPoints.Count - 1) % 3;
            if (correctPointCount != 0)
                throw new ArgumentOutOfRangeException(nameof(points), "Length must be a multiple of 3 plus 1.");

            linePoints = GetDrawingPoints(_controlPoints);
            EndPoint = _controlPoints[_controlPoints.Count - 1];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CubicBezierLineSegment"/> class.
        /// </summary>
        /// <param name="points">The points.</param>
        public CubicBezierLineSegment(PointF[] points) : this(PrimitiveListPools.PointF.Rent(points))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CubicBezierLineSegment"/> class.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="controlPoint1">The control point1.</param>
        /// <param name="controlPoint2">The control point2.</param>
        /// <param name="end">The end.</param>
        /// <param name="additionalPoints">The additional points.</param>
        public CubicBezierLineSegment(PointF start, PointF controlPoint1, PointF controlPoint2, PointF end, params PointF[] additionalPoints)
            : this(MergeConstructorValues(start, controlPoint1, controlPoint2, end, additionalPoints))
        {
        }

        private static List<PointF> MergeConstructorValues(
            PointF start, PointF controlPoint1, PointF controlPoint2, PointF end, params PointF[] additionalPoints)
        {
            var list = PrimitiveListPools.PointF.Rent(4 + (additionalPoints?.Length).GetValueOrDefault());
            list.Add(start);
            list.Add(controlPoint1);
            list.Add(controlPoint2);
            list.Add(end);

            if (additionalPoints != null)
                foreach (var item in additionalPoints)
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
        /// Returns the current <see cref="ILineSegment" /> a simple linear path.
        /// </summary>
        /// <returns>
        /// Returns the current <see cref="ILineSegment" /> as simple linear path.
        /// </returns>
        public IReadOnlyList<PointF> Flatten()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(CubicBezierLineSegment));
            return linePoints;
        }

        /// <summary>
        /// Transforms the current LineSegment using specified matrix.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <returns>A line segment with the matrix applied to it.</returns>
        public CubicBezierLineSegment Transform(Matrix3x2 matrix)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(CubicBezierLineSegment));

            if (matrix.IsIdentity)
            {
                // no transform to apply skip it
                return this;
            }

            var transformed = PrimitiveListPools.PointF.Rent(_controlPoints.Count);
            for (int i = 0; i < _controlPoints.Count; i++)
                transformed.Add(PointF.Transform(_controlPoints[i], matrix));
            return new CubicBezierLineSegment(transformed);
        }

        /// <summary>
        /// Transforms the current LineSegment using specified matrix.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <returns>A line segment with the matrix applied to it.</returns>
        ILineSegment ILineSegment.Transform(Matrix3x2 matrix) => Transform(matrix);

        private static List<PointF> GetDrawingPoints(IList<PointF> controlPoints)
        {
            var drawingPoints = PrimitiveListPools.PointF.Rent();
            int curveCount = (controlPoints.Count - 1) / 3;

            var tmp = PrimitiveListPools.PointF.Rent();
            for (int curveIndex = 0; curveIndex < curveCount; curveIndex++)
            {
                FindDrawingPoints(curveIndex, controlPoints, tmp);

                if (curveIndex != 0)
                {
                    // remove the first point, as it coincides with the last point of the previous Bezier curve.
                    tmp.RemoveAt(0);
                }

                foreach (var p in tmp)
                    drawingPoints.Add(p);
                tmp.Clear();
            }
            PrimitiveListPools.PointF.Return(tmp);

            return drawingPoints;
        }

        private static void FindDrawingPoints(int curveIndex, IList<PointF> controlPoints, IList<PointF> output)
        {
            Vector2 left = CalculateBezierPoint(curveIndex, 0, controlPoints);
            Vector2 right = CalculateBezierPoint(curveIndex, 1, controlPoints);

            output.Add(left);
            output.Add(right);

            FindDrawingPoints(curveIndex, 0, 1, output, 1, controlPoints, 0);
        }

        private static int FindDrawingPoints(
            int curveIndex,
            float t0,
            float t1,
            IList<PointF> pointList,
            int insertionIndex,
            IList<PointF> controlPoints,
            int depth)
        {
            // max recursive depth for control points, means this is approx the max number of points discoverable
            if (depth > 999)
                return 0;

            Vector2 left = CalculateBezierPoint(curveIndex, t0, controlPoints);
            Vector2 right = CalculateBezierPoint(curveIndex, t1, controlPoints);

            if ((left - right).LengthSquared() < MinimumSqrDistance)
                return 0;

            float midT = (t0 + t1) / 2;
            Vector2 mid = CalculateBezierPoint(curveIndex, midT, controlPoints);

            Vector2 leftDirection = Vector2.Normalize(left - mid);
            Vector2 rightDirection = Vector2.Normalize(right - mid);

            if (Vector2.Dot(leftDirection, rightDirection) > DivisionThreshold || Math.Abs(midT - 0.5f) < 0.0001f)
            {
                int pointsAddedCount = 0;

                pointsAddedCount += FindDrawingPoints(curveIndex, t0, midT, pointList, insertionIndex, controlPoints, depth + 1);
                pointList.Insert(insertionIndex + pointsAddedCount, mid);
                pointsAddedCount++;
                pointsAddedCount += FindDrawingPoints(curveIndex, midT, t1, pointList, insertionIndex + pointsAddedCount, controlPoints, depth + 1);

                return pointsAddedCount;
            }

            return 0;
        }

        private static PointF CalculateBezierPoint(int curveIndex, float t, IList<PointF> controlPoints)
        {
            int nodeIndex = curveIndex * 3;

            Vector2 p0 = controlPoints[nodeIndex];
            Vector2 p1 = controlPoints[nodeIndex + 1];
            Vector2 p2 = controlPoints[nodeIndex + 2];
            Vector2 p3 = controlPoints[nodeIndex + 3];

            return CalculateBezierPoint(t, p0, p1, p2, p3);
        }

        /// <summary>
        /// Calculates the bezier point along the line.
        /// </summary>
        /// <param name="t">The position within the line.</param>
        /// <param name="p0">The p 0.</param>
        /// <param name="p1">The p 1.</param>
        /// <param name="p2">The p 2.</param>
        /// <param name="p3">The p 3.</param>
        /// <returns>
        /// The <see cref="Vector2"/>.
        /// </returns>
        private static Vector2 CalculateBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector2 p = uuu * p0; // first term

            p += 3 * uu * t * p1; // second term
            p += 3 * u * tt * p2; // third term
            p += ttt * p3; // fourth term

            return p;
        }

        /// <summary>
        /// Disposes the line, making it unusable.
        /// </summary>
        public void Dispose()
        {
            if (!IsDisposed)
            {
                PrimitiveListPools.PointF.Return(linePoints);
                linePoints = null;

                PrimitiveListPools.PointF.Return(_controlPoints);
                _controlPoints = null;

                IsDisposed = true;
            }
        }
    }
}
