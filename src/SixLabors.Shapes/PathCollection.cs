﻿// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using SixLabors.Primitives;

namespace SixLabors.Shapes
{
    /// <summary>
    /// A aggregate of <see cref="IPath"/>s to apply common operations to them.
    /// </summary>
    /// <seealso cref="IPath" />
    public class PathCollection : IReadOnlyList<IPath>, IPathCollection
    {
        internal static readonly Func<IPath, float> GetLeft = x => x.Bounds.Left;
        internal static readonly Func<IPath, float> GetRight = x => x.Bounds.Right;
        internal static readonly Func<IPath, float> GetTop = x => x.Bounds.Top;
        internal static readonly Func<IPath, float> GetBottom = x => x.Bounds.Bottom;

        private List<IPath> _paths;

        /// <inheritdoc />
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PathCollection"/> class.
        /// </summary>
        /// <param name="paths">The collection of paths</param>
        public PathCollection(IEnumerable<IPath> paths) : this(ShapeListPools.Path.Rent(paths))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PathCollection"/> class.
        /// </summary>
        /// <param name="paths">The collection of paths</param>
        public PathCollection(params IPath[] paths) : this(ShapeListPools.Path.Rent(paths))
        {
        }

        private PathCollection(List<IPath> paths)
        {
            _paths = paths ?? throw new ArgumentNullException(nameof(paths));
            Bounds = GetBounds(_paths);
        }

        /// <summary>
        /// Creates a <see cref="PathCollection"/> with a list as it's underlying list.
        /// </summary>
        /// <param name="paths">The path list to consume.</param>
        /// <returns>The new collection.</returns>
        public static PathCollection FromList(List<IPath> paths)
        {
            return new PathCollection(paths);
        }

        /// <inheritdoc />
        public RectangleF Bounds { get; }

        /// <summary>
        /// Gets the number of paths in the collection.
        /// </summary>
        public int Count => _paths.Count;

        /// <summary>
        /// Returns a path at an index.
        /// </summary>
        /// <param name="index">The index of the path.</param>
        /// <returns>The path at the index.</returns>
        public IPath this[int index] => _paths[index];

        /// <inheritdoc />
        public IPathCollection Transform(Matrix3x2 matrix)
        {
            List<IPath> result = ShapeListPools.Path.Rent(_paths.Count);
            for (int i = 0; i < _paths.Count; i++)
                result.Add(_paths[i].Transform(matrix));
            return PathCollection.FromList(result);
        }

        /// <summary>
        /// Gets the bounds enclosing the path.
        /// </summary>
        /// <param name="paths">The given paths.</param>
        /// <returns>The bounds of the paths.</returns>
        public static RectangleF GetBounds(IReadOnlyCollection<IPath> paths)
        {
            if (paths.Count == 0)
                return RectangleF.Empty;

            float minX = paths.FastMin(GetLeft);
            float maxX = paths.FastMax(GetRight);
            float minY = paths.FastMin(GetTop);
            float maxY = paths.FastMax(GetBottom);
            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="PathCollection"/>.
        /// </summary>
        /// <returns></returns>
        public List<IPath>.Enumerator GetEnumerator() => _paths.GetEnumerator();
    
        /// <inheritdoc />
        IEnumerator<IPath> IEnumerable<IPath>.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        /// <summary>
        /// Disposes the collection, making it unusable.
        /// </summary>
        public void Dispose()
        {
            if (!IsDisposed)
            {
                ShapeListPools.Path.Return(_paths);
                _paths = null;

                IsDisposed = true;
            }
        }
    }
}