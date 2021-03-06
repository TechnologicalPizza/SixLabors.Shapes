﻿// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Shapes
{
    /// <summary>
    /// Describes the different type of paths.
    /// </summary>
    public enum PathType
    {
        /// <summary>
        /// Denotes a path containing a single simple open path
        /// </summary>
        Open,

        /// <summary>
        /// Denotes a path describing a single simple closed shape
        /// </summary>
        Closed,

        /// <summary>
        /// Denotes a path containing one or more child paths that could be open or closed.
        /// </summary>
        Mixed
    }
}
