﻿// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ClipperLib;
using SixLabors.Memory;
using SixLabors.Primitives;

namespace SixLabors.Shapes
{
    /// <summary>
    /// Path extensions to generate outlines of paths.
    /// </summary>
    public static class Outliner
    {
        private const double MiterOffsetDelta = 20;
        private const float ScalingFactor = 1000.0f;

        /// <summary>
        /// Generates a outline of the path with alternating on and off segments based on the pattern.
        /// </summary>
        /// <param name="path">the path to outline</param>
        /// <param name="width">The final width outline</param>
        /// <param name="pattern">The pattern made of multiples of the width.</param>
        /// <returns>A new path representing the outline.</returns>
        public static IPath GenerateOutline(this IPath path, float width, float[] pattern)
            => path.GenerateOutline(width, new ReadOnlySpan<float>(pattern));

        /// <summary>
        /// Generates a outline of the path with alternating on and off segments based on the pattern.
        /// </summary>
        /// <param name="path">the path to outline</param>
        /// <param name="width">The final width outline</param>
        /// <param name="pattern">The pattern made of multiples of the width.</param>
        /// <returns>A new path representing the outline.</returns>
        public static IPath GenerateOutline(this IPath path, float width, ReadOnlySpan<float> pattern)
        {
            return path.GenerateOutline(width, pattern, false);
        }

        /// <summary>
        /// Generates a outline of the path with alternating on and off segments based on the pattern.
        /// </summary>
        /// <param name="path">the path to outline</param>
        /// <param name="width">The final width outline</param>
        /// <param name="pattern">The pattern made of multiples of the width.</param>
        /// <param name="startOff">Weather the first item in the pattern is on or off.</param>
        /// <returns>A new path representing the outline.</returns>
        public static IPath GenerateOutline(this IPath path, float width, float[] pattern, bool startOff)
            => path.GenerateOutline(width, new ReadOnlySpan<float>(pattern), startOff);

        /// <summary>
        /// Generates a outline of the path with alternating on and off segments based on the pattern.
        /// </summary>
        /// <param name="path">the path to outline</param>
        /// <param name="width">The final width outline</param>
        /// <param name="pattern">The pattern made of multiples of the width.</param>
        /// <param name="startOff">Weather the first item in the pattern is on or off.</param>
        /// <returns>A new path representing the outline.</returns>
        public static IPath GenerateOutline(this IPath path, float width, ReadOnlySpan<float> pattern, bool startOff)
            => GenerateOutline(path, width, pattern, startOff, JointStyle.Square, EndCapStyle.Butt);

        /// <summary>
        /// Generates a outline of the path with alternating on and off segments based on the pattern.
        /// </summary>
        /// <param name="path">the path to outline</param>
        /// <param name="width">The final width outline</param>
        /// <param name="pattern">The pattern made of multiples of the width.</param>
        /// <param name="startOff">Weather the first item in the pattern is on or off.</param>
        /// <param name="jointStyle">The style to render the joints.</param>
        /// <param name="patternSectionCapStyle">The style to render between sections of the specified pattern.</param>
        /// <returns>A new path representing the outline.</returns>
        public static IPath GenerateOutline(
            this IPath path, float width, ReadOnlySpan<float> pattern, bool startOff,
            JointStyle jointStyle = JointStyle.Square, EndCapStyle patternSectionCapStyle = EndCapStyle.Butt)
        {
            if (pattern.Length < 2)
                return path.GenerateOutline(width);

            var style = Convert(jointStyle);
            var patternSectionCap = Convert(patternSectionCapStyle);

            IEnumerable<ISimplePath> paths = path.Flatten();

            var offset = new ClipperOffset()
            {
                MiterLimit = MiterOffsetDelta
            };

            var buffer = new List<IntPoint>(3);
            foreach (ISimplePath p in paths)
            {
                var points = p.Points;
                bool online = !startOff;
                float targetLength = pattern[0] * width;
                int patternPos = 0;

                // Create a new list of points representing the new outline
                int pCount = points.Count;
                if (!p.IsClosed)
                    pCount--;

                int i = 0;
                Vector2 currentPoint = points[0];

                while (i < pCount)
                {
                    int next = (i + 1) % points.Count;
                    Vector2 targetPoint = points[next];
                    float distToNext = Vector2.Distance(currentPoint, targetPoint);
                    if (distToNext > targetLength)
                    {
                        // find a point between the 2
                        float t = targetLength / distToNext;

                        Vector2 point = (currentPoint * (1 - t)) + (targetPoint * t);
                        buffer.Add(currentPoint.ToPoint());
                        buffer.Add(point.ToPoint());

                        // we now inset a line joining
                        if (online)
                        {
                            offset.AddPath(buffer, style, patternSectionCap);
                        }

                        online = !online;

                        buffer.Clear();

                        currentPoint = point;

                        // next length
                        patternPos = (patternPos + 1) % pattern.Length;
                        targetLength = pattern[patternPos] * width;
                    }
                    else if (distToNext <= targetLength)
                    {
                        buffer.Add(currentPoint.ToPoint());
                        currentPoint = targetPoint;
                        i++;
                        targetLength -= distToNext;
                    }
                }

                if (buffer.Count > 0)
                {
                    if (p.IsClosed)
                        buffer.Add(points[0].ToPoint());
                    else
                        buffer.Add(points[points.Count - 1].ToPoint());

                    if (online)
                        offset.AddPath(buffer, style, patternSectionCap);

                    online = !online;

                    buffer.Clear();
                    patternPos = (patternPos + 1) % pattern.Length;
                    targetLength = pattern[patternPos] * width;
                }
            }

            return ExecuteOutliner(width, offset);
        }

        /// <summary>
        /// Generates a solid outline of the path.
        /// </summary>
        /// <param name="path">the path to outline</param>
        /// <param name="width">The final width outline</param>
        /// <returns>A new path representing the outline.</returns>
        public static IPath GenerateOutline(this IPath path, float width) => GenerateOutline(path, width, JointStyle.Square, EndCapStyle.Butt);

        /// <summary>
        /// Generates a solid outline of the path.
        /// </summary>
        /// <param name="path">the path to outline</param>
        /// <param name="width">The final width outline</param>
        /// <param name="jointStyle">The style to render the joints.</param>
        /// <param name="endCapStyle">The style to render the end caps of open paths (ignored on closed paths).</param>
        /// <returns>A new path representing the outline.</returns>
        public static IPath GenerateOutline(this IPath path, float width, JointStyle jointStyle = JointStyle.Square, EndCapStyle endCapStyle = EndCapStyle.Butt)
        {
            var offset = new ClipperOffset()
            {
                MiterLimit = MiterOffsetDelta
            };

            var style = Convert(jointStyle);
            var openEndCapStyle = Convert(endCapStyle);

            // Pattern can be applied to the path by cutting it into segments
            IEnumerable<ISimplePath> paths = path.Flatten();
            foreach (ISimplePath p in paths)
            {
                IReadOnlyList<PointF> vectors = p.Points;
                var points = new List<IntPoint>(vectors.Count);
                for (int i = 0; i < vectors.Count; i++)
                {
                    var v = vectors[i];
                    points.Add(new IntPoint(v.X * ScalingFactor, v.Y * ScalingFactor));
                }
                EndType type = p.IsClosed ? EndType.etClosedLine : openEndCapStyle;
                offset.AddPath(points, style, type);
            }

            return ExecuteOutliner(width, offset);
        }

        private static IPath ExecuteOutliner(float width, ClipperOffset offset)
        {
            var tree = new List<List<IntPoint>>();
            offset.Execute(ref tree, width * ScalingFactor / 2);

            var polygons = ShapeListPools.Path.Rent();
            var tmp = PrimitiveListPools.PointF.Rent();
            try
            {
                foreach (List<IntPoint> pt in tree)
                {
                    foreach (var point in pt)
                        tmp.Add(new PointF(point.X / ScalingFactor, point.Y / ScalingFactor));

                    polygons.Add(new Polygon(new LinearLineSegment(PrimitiveListPools.PointF.Rent(tmp))));
                    tmp.Clear();
                }
            }
            catch
            {
                foreach (var polygon in polygons)
                    polygon.Dispose();
                ShapeListPools.Path.Return(polygons);
                throw;
            }
            finally
            {
                PrimitiveListPools.PointF.Return(tmp);
            }
            return new ComplexPolygon(polygons);
        }

        private static IntPoint ToPoint(this PointF vector)
        {
            return new IntPoint(vector.X * ScalingFactor, vector.Y * ScalingFactor);
        }

        private static IntPoint ToPoint(this Vector2 vector)
        {
            return new IntPoint(vector.X * ScalingFactor, vector.Y * ScalingFactor);
        }

        private static JoinType Convert(JointStyle style)
        {
            switch (style)
            {
                case JointStyle.Round: return JoinType.jtRound;
                case JointStyle.Miter: return JoinType.jtMiter;

                case JointStyle.Square:
                default:
                    return JoinType.jtSquare;
            }
        }

        private static EndType Convert(EndCapStyle style)
        {
            switch (style)
            {
                case EndCapStyle.Round: return EndType.etOpenRound;
                case EndCapStyle.Square: return EndType.etOpenSquare;

                case EndCapStyle.Butt:
                default:
                    return EndType.etOpenButt;
            }
        }
    }
}
