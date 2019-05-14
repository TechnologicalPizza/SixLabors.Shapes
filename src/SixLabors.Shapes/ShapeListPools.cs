using System.Collections.Generic;
using SixLabors.Memory;

namespace SixLabors.Shapes
{
    /// <summary>
    /// A static class with commonly used <see cref="List{T}"/>s.
    /// </summary>
    public static class ShapeListPools
    {
        /// <summary> A pool for <see cref="IPath"/>s.</summary>
        public readonly static ListPool<IPath> Path = new ListPool<IPath>(512);

        /// <summary> A pool for <see cref="ILineSegment"/>s.</summary>
        public readonly static ListPool<ILineSegment> Line = new ListPool<ILineSegment>(512);

        internal readonly static ListPool<InternalPath.PointData> PointData = new ListPool<InternalPath.PointData>(512);
    }
}
