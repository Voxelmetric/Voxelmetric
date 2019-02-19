﻿using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Builders
{
    public abstract class AMeshBuilder
    {
        //! Pallete used by this mesh builder
        public Color32[] Palette { get; set; }
        //! Side mask
        public Side SideMask { get; set; }

        public abstract void Build(Chunk chunk, out int minBounds, out int maxBounds);
    }
}
