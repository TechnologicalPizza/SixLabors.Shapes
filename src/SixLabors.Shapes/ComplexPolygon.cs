﻿// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Numerics;
using SixLabors.Primitives;

namespace SixLabors.Shapes
{
    /// <summary>
    /// Represents a complex polygon made up of one or more shapes overlayed on each other, where overlaps causes holes.
    /// </summary>
    /// <seealso cref="IPath" />
    public sealed class ComplexPolygon : IPath
    {
        /// <summary>
        /// A complex polygon with no paths.
        /// </summary>
        public static readonly ComplexPolygon Empty = new ComplexPolygon(new List<IPath>(0));

        private List<IPath> _paths;

        /// <inheritdoc/>
        public bool IsDisposed { get; private set; }

        internal ComplexPolygon(List<IPath> paths)
        {
            this._paths = paths ?? throw new ArgumentNullException(nameof(paths));

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            float length = 0;
            int intersections = 0;

            foreach (IPath s in this._paths)
            {
                length += s.Length;
                if (s.Bounds.Left < minX)
                    minX = s.Bounds.Left;

                if (s.Bounds.Right > maxX)
                    maxX = s.Bounds.Right;

                if (s.Bounds.Top < minY)
                    minY = s.Bounds.Top;

                if (s.Bounds.Bottom > maxY)
                    maxY = s.Bounds.Bottom;

                intersections += s.MaxIntersections;
            }

            this.MaxIntersections = intersections;
            this.Length = length;
            this.Bounds = new RectangleF(minX, minY, maxX - minX, maxY - minY);
            this.PathType = PathTypes.Mixed;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComplexPolygon" /> class.
        /// </summary>
        /// <param name="paths">The paths.</param>
        public ComplexPolygon(IEnumerable<IPath> paths) : this(ShapeListPools.Path.Rent(paths))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComplexPolygon" /> class.
        /// </summary>
        /// <param name="paths">The paths.</param>
        public ComplexPolygon(params IPath[] paths) : this(ShapeListPools.Path.Rent(paths))
        {
        }

        /// <summary>
        /// Gets the length of the path.
        /// </summary>
        public float Length { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is closed, open or a composite path with a mixture of open and closed figures.
        /// </summary>
        public PathTypes PathType { get; }

        /// <summary>
        /// Gets the paths that make up this shape
        /// </summary>
        /// <value>
        /// The paths.
        /// </value>
        public IEnumerable<IPath> Paths => this._paths;

        /// <summary>
        /// Gets the bounding box of this shape.
        /// </summary>
        /// <value>
        /// The bounds.
        /// </value>
        public RectangleF Bounds { get; }

        /// <summary>
        /// Gets the maximum number intersections that a shape can have when testing a line.
        /// </summary>
        /// <value>
        /// The maximum intersections.
        /// </value>
        public int MaxIntersections { get; }

        /// <summary>
        /// the distance of the point from the outline of the shape, if the value is negative it is inside the polygon bounds
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>
        /// Returns the distance from thr shape to the point
        /// </returns>
        /// <remarks>
        /// Due to the clipping we did during construction we know that out shapes do not overlap at there edges
        /// therefore for a point to be in more that one we must be in a hole of another, theoretically this could
        /// then flip again to be in a outline inside a hole inside an outline :)
        /// </remarks>
        public PointInfo Distance(PointF point)
        {
            float dist = float.MaxValue;
            PointInfo pointInfo = default;
            bool inside = false;
            foreach (IPath shape in this._paths)
            {
                PointInfo d = shape.Distance(point);

                if (d.DistanceFromPath <= 0)
                {
                    // we are inside a poly
                    d.DistanceFromPath = -d.DistanceFromPath;  // flip the sign
                    inside ^= true; // flip the inside flag
                }

                if (d.DistanceFromPath < dist)
                {
                    dist = d.DistanceFromPath;
                    pointInfo = d;
                }
            }

            if (inside)
            {
                pointInfo.DistanceFromPath = -pointInfo.DistanceFromPath;
            }

            return pointInfo;
        }

        /// <summary>
        /// Based on a line described by <paramref name="start"/> and <paramref name="end"/>
        /// populate a buffer for all points on all the polygons, that make up this complex shape,
        /// that the line intersects.
        /// </summary>
        /// <param name="start">The start point of the line.</param>
        /// <param name="end">The end point of the line.</param>
        /// <param name="buffer">The buffer that will be populated with intersections.</param>
        /// <param name="offset">The offset within the buffer</param>
        /// <returns>
        /// The number of intersections populated into the buffer.
        /// </returns>
        public int FindIntersections(PointF start, PointF end, PointF[] buffer, int offset)
        {
            Span<PointF> subBuffer = buffer.AsSpan(offset);
            return this.FindIntersections(start, end, subBuffer);
        }

        /// <inheritdoc />
        public int FindIntersections(PointF start, PointF end, Span<PointF> buffer)
        {
            int totalAdded = 0;
            for (int i = 0; i < this._paths.Count; i++)
            {
                Span<PointF> subBuffer = buffer.Slice(totalAdded);
                int added = this._paths[i].FindIntersections(start, end, subBuffer);
                totalAdded += added;
            }

            // TODO we should sort by distance from start
            return totalAdded;
        }

        /// <summary>
        /// Determines whether the <see cref="IPath" /> contains the specified point
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>
        ///   <c>true</c> if the <see cref="IPath" /> contains the specified point; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(PointF point)
        {
            bool inside = false;
            foreach (IPath shape in this._paths)
            {
                if (shape.Contains(point))
                    inside ^= true; // flip the inside flag
            }

            return inside;
        }

        /// <summary>
        /// Transforms the shape using the specified matrix.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <returns>
        /// A new shape with the matrix applied to it.
        /// </returns>
        public IPath Transform(Matrix3x2 matrix)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(ComplexPolygon));

            if (matrix.IsIdentity)
            {
                // no transform to apply skip it
                return this;
            }

            var shapes = ShapeListPools.Path.Rent(this._paths.Count);
            for (int i = 0; i < this._paths.Count; i++)
                shapes[i] = shapes[i].Transform(matrix);
            return new ComplexPolygon(shapes);
        }

        /// <summary>
        /// Converts the <see cref="IPath" /> into a simple linear path.
        /// </summary>
        /// <returns>
        /// Returns the current <see cref="IPath" /> as simple linear path.
        /// </returns>
        public IEnumerable<ISimplePath> Flatten()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(ComplexPolygon));
            
            var paths = new List<ISimplePath>();

            foreach (IPath path in this._paths)
                paths.AddRange(path.Flatten());

            return paths;
        }

        /// <summary>
        /// Converts a path to a closed path.
        /// </summary>
        /// <returns>
        /// Returns the path as a closed path.
        /// </returns>
        public IPath AsClosedPath()
        {
            if(IsDisposed)
                throw new ObjectDisposedException(nameof(ComplexPolygon));

            if (this.PathType == PathTypes.Closed)
            {
                return this;
            }
            else
            {
                var paths = ShapeListPools.Path.Rent(this._paths.Count);
                for (int i = 0; i < this._paths.Count; i++)
                    paths.Add(this._paths[i].AsClosedPath());
                return new ComplexPolygon(paths);
            }
        }

        /// <summary>
        /// Calculates the the point a certain distance a path.
        /// </summary>
        /// <param name="distanceAlongPath">The distance along the path to find details of.</param>
        /// <returns>
        /// Returns details about a point along a path.
        /// </returns>
        public SegmentInfo PointAlongPath(float distanceAlongPath)
        {
            distanceAlongPath %= this.Length;

            foreach (IPath p in this._paths)
            {
                if (p.Length >= distanceAlongPath)
                    return p.PointAlongPath(distanceAlongPath);

                // reduce it before trying the next path
                distanceAlongPath -= p.Length;
            }

            throw new InvalidOperationException("Should not be possible to reach this line");
        }

        /// <summary>
        /// Disposes the polygon, making it unusable.
        /// </summary>
        public void Dispose()
        {
            if (!IsDisposed)
            {
                foreach (var path in _paths)
                    path.Dispose();

                ShapeListPools.Path.Return(_paths);
                _paths = null;

                IsDisposed = true;
            }
        }
    }
}