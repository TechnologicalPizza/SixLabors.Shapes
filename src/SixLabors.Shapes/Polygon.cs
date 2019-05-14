// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Numerics;

namespace SixLabors.Shapes
{
    /// <summary>
    /// A shape made up of a single path made up of one of more <see cref="ILineSegment"/>s
    /// </summary>
    public class Polygon : Path
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Polygon"/> class.
        /// </summary>
        /// <param name="segments">The segments.</param>
        public Polygon(params ILineSegment[] segments) : base(segments)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Polygon"/> class.
        /// </summary>
        /// <param name="segments">The segments.</param>
        public Polygon(IEnumerable<ILineSegment> segments) : base(segments)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Polygon"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        internal Polygon(Path path) : base(path)
        {
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a closed path.
        /// </summary>
        protected override bool IsClosed => true;

        /// <summary>
        /// Transforms the rectangle using specified matrix.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <returns>
        /// A new shape with the matrix applied to it.
        /// </returns>
        public override IPath Transform(Matrix3x2 matrix)
        {
            if (matrix.IsIdentity)
                return this;

            var segments = ShapeListPools.Line.Rent(this.LineSegments.Count);

            for (int i = 0; i < LineSegments.Count; i++)
                segments[i] = LineSegments[i].Transform(matrix);

            return new Polygon(segments);
        }
    }
}
