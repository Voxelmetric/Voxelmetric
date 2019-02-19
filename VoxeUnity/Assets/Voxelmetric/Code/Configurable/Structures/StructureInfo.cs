﻿using System;
using System.Collections.Generic;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Configurable.Structures
{
    public class StructureInfo: IEquatable<StructureInfo>, IEqualityComparer<StructureInfo>
    {
        //! Total bounds in world coordinates
        public readonly AABBInt bounds;
        //! A chunk this structure belongs to
        public readonly Vector3Int chunkPos;
        //! Id of this structure comes from
        public readonly int id;

        public StructureInfo(int id, ref Vector3Int chunkPos, ref AABBInt bounds)
        {
            this.id = id;
            this.chunkPos = chunkPos;
            this.bounds = bounds;
        }

        #region Struct comparison

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = bounds.GetHashCode();
                hashCode = (hashCode*397)^chunkPos.GetHashCode();
                hashCode = (hashCode*397)^id;
                return hashCode;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            StructureInfo a = obj as StructureInfo;
            return a!=null && Equals(a);
        }

        public bool Equals(StructureInfo other)
        {
            return other!=null && id==other.id && chunkPos==other.chunkPos && bounds==other.bounds;
        }

        public bool Equals(StructureInfo x, StructureInfo y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(StructureInfo obj)
        {
            return obj.GetHashCode();
        }

        #endregion
    }
}
