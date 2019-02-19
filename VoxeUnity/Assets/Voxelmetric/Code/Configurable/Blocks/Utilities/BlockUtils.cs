using UnityEngine;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Textures;
using Vector3Int = Voxelmetric.Code.Data_types.Vector3Int;

namespace Voxelmetric.Code.Configurable.Blocks.Utilities
{
    public static class BlockUtils
    {
        /// All faces in the engine are build in the following order:
        ///     1--2
        ///     |  |
        ///     |  |
        ///     0--3

        //Adding a tiny overlap between block meshes may solve floating point imprecision
        //errors causing pixel size gaps between blocks when looking closely
        public static readonly float blockPadding = Env.BlockFacePadding;
        
        public static readonly Vector3[][] PaddingOffsets =
        {
            new[]
            {
                // Direction.up
                new Vector3(-blockPadding, +blockPadding, -blockPadding),
                new Vector3(-blockPadding, +blockPadding, +blockPadding),
                new Vector3(+blockPadding, +blockPadding, +blockPadding),
                new Vector3(+blockPadding, +blockPadding, -blockPadding)
            },
            new[]
            {
                // Direction.down
                new Vector3(-blockPadding, -blockPadding, -blockPadding),
                new Vector3(-blockPadding, -blockPadding, +blockPadding),
                new Vector3(+blockPadding, -blockPadding, +blockPadding),
                new Vector3(+blockPadding, -blockPadding, -blockPadding)
            },

            new[]
            {
                // Direction.north
                new Vector3(-blockPadding, -blockPadding, +blockPadding),
                new Vector3(-blockPadding, +blockPadding, +blockPadding),
                new Vector3(+blockPadding, +blockPadding, +blockPadding),
                new Vector3(+blockPadding, -blockPadding, +blockPadding)
            },
            new[]
            {
                // Direction.south
                new Vector3(-blockPadding, -blockPadding, -blockPadding),
                new Vector3(-blockPadding, +blockPadding, -blockPadding),
                new Vector3(+blockPadding, +blockPadding, -blockPadding),
                new Vector3(+blockPadding, -blockPadding, -blockPadding)
            },

            new[]
            {
                // Direction.east
                new Vector3(+blockPadding, -blockPadding, -blockPadding),
                new Vector3(+blockPadding, +blockPadding, -blockPadding),
                new Vector3(+blockPadding, +blockPadding, +blockPadding),
                new Vector3(+blockPadding, -blockPadding, +blockPadding)
            },
            new[]
            {
                // Direction.west
                new Vector3(-blockPadding, -blockPadding, -blockPadding),
                new Vector3(-blockPadding, +blockPadding, -blockPadding),
                new Vector3(-blockPadding, +blockPadding, +blockPadding),
                new Vector3(-blockPadding, -blockPadding, +blockPadding)
            }
        };

        public static void PrepareColors(Chunk chunk, Color32[] data, BlockLightData light)
        {
            if (chunk.world.config.addAOToMesh)
                SetColorsAO(data, light, chunk.world.config.ambientOcclusionStrength);
            else
                SetColors(data, 1f, 1f, 1f, 1f, false);
        }

        public static BlockLightData CalculateColors(Chunk chunk, int localPosIndex, Direction direction)
        {
            // With AO turned off, do not generate any fancy data
            if (!chunk.world.config.addAOToMesh)
                return new BlockLightData();

            // Side blocks
            bool n_Solid, _eSolid, s_Solid, _wSolid;
            // Corner blocks
            bool nwSolid, neSolid, seSolid, swSolid;

            ChunkBlocks blocks = chunk.Blocks;
            int index1, index2, index3;

            int sizeWithPadding = chunk.SideSize + Env.ChunkPadding2;
            int sizeWithPaddingPow2 = sizeWithPadding * sizeWithPadding;

            switch (direction)
            {
                case Direction.up:
                    index1 = localPosIndex + sizeWithPaddingPow2; // + (0,1,0)
                    index2 = index1 - sizeWithPadding; // - (0,0,1)
                    index3 = index1 + sizeWithPadding; // + (0,0,1)
                    
                    swSolid = blocks.Get(index2 - 1).Solid; // -1,1,-1
                    s_Solid = blocks.Get(index2).Solid;     //  0,1,-1
                    seSolid = blocks.Get(index2 + 1).Solid; //  1,1,-1
                    _wSolid = blocks.Get(index1 - 1).Solid; // -1,1, 0
                    _eSolid = blocks.Get(index1 + 1).Solid; //  1,1, 0
                    nwSolid = blocks.Get(index3 - 1).Solid; // -1,1, 1
                    n_Solid = blocks.Get(index3).Solid;     //  0,1, 1
                    neSolid = blocks.Get(index3 + 1).Solid; //  1,1, 1
                    break;
                case Direction.down:
                    index1 = localPosIndex - sizeWithPaddingPow2; // - (0,1,0)
                    index2 = index1 - sizeWithPadding; // - (0,0,1)
                    index3 = index1 + sizeWithPadding; // + (0,0,1)

                    swSolid = blocks.Get(index2 - 1).Solid; // -1,-1,-1
                    s_Solid = blocks.Get(index2).Solid;     //  0,-1,-1
                    seSolid = blocks.Get(index2 + 1).Solid; //  1,-1,-1
                    _wSolid = blocks.Get(index1 - 1).Solid; // -1,-1, 0
                    _eSolid = blocks.Get(index1 + 1).Solid; //  1,-1, 0
                    nwSolid = blocks.Get(index3 - 1).Solid; // -1,-1, 1
                    n_Solid = blocks.Get(index3).Solid;     //  0,-1, 1
                    neSolid = blocks.Get(index3 + 1).Solid; //  1,-1, 1
                    break;
                case Direction.north:
                    index1 = localPosIndex + sizeWithPadding; // + (0,0,1)
                    index2 = index1 - sizeWithPaddingPow2;   // - (0,1,0)
                    index3 = index1 + sizeWithPaddingPow2;   // + (0,1,0)

                    swSolid = blocks.Get(index2 - 1).Solid; // -1,-1,1
                    seSolid = blocks.Get(index2 + 1).Solid; //  1,-1,1
                    _wSolid = blocks.Get(index1 - 1).Solid; // -1, 0,1
                    _eSolid = blocks.Get(index1 + 1).Solid; //  1, 0,1
                    nwSolid = blocks.Get(index3 - 1).Solid; // -1, 1,1
                    s_Solid = blocks.Get(index2).Solid;     //  0,-1,1
                    n_Solid = blocks.Get(index3).Solid;     //  0, 1,1
                    neSolid = blocks.Get(index3 + 1).Solid; //  1, 1,1
                    break;
                case Direction.south:
                    index1 = localPosIndex - sizeWithPadding; // - (0,0,1)
                    index2 = index1 - sizeWithPaddingPow2;   // - (0,1,0)
                    index3 = index1 + sizeWithPaddingPow2;   // + (0,1,0)
                    
                    swSolid = blocks.Get(index2 - 1).Solid; // -1,-1,-1
                    seSolid = blocks.Get(index2 + 1).Solid; //  1,-1,-1
                    _wSolid = blocks.Get(index1 - 1).Solid; // -1, 0,-1
                    _eSolid = blocks.Get(index1 + 1).Solid; //  1, 0,-1
                    nwSolid = blocks.Get(index3 - 1).Solid; // -1, 1,-1
                    s_Solid = blocks.Get(index2).Solid;     //  0,-1,-1
                    n_Solid = blocks.Get(index3).Solid;     //  0, 1,-1
                    neSolid = blocks.Get(index3 + 1).Solid; //  1, 1,-1
                    break;
                case Direction.east:
                    index1 = localPosIndex+1; // + (1,0,0)
                    index2 = index1 - sizeWithPaddingPow2; // - (0,1,0)
                    index3 = index1 + sizeWithPaddingPow2; // + (0,1,0)
                    
                    swSolid = blocks.Get(index2 - sizeWithPadding).Solid; // 1,-1,-1
                    s_Solid = blocks.Get(index2).Solid;                   // 1,-1, 0
                    seSolid = blocks.Get(index2 + sizeWithPadding).Solid; // 1,-1, 1
                    _wSolid = blocks.Get(index1 - sizeWithPadding).Solid; // 1, 0,-1
                    _eSolid = blocks.Get(index1 + sizeWithPadding).Solid; // 1, 0, 1
                    nwSolid = blocks.Get(index3 - sizeWithPadding).Solid; // 1, 1,-1
                    n_Solid = blocks.Get(index3).Solid;                   // 1, 1, 0
                    neSolid = blocks.Get(index3 + sizeWithPadding).Solid; // 1, 1, 1
                    break;
                default://case Direction.west:
                    index1 = localPosIndex-1; // - (1,0,0)
                    index2 = index1 - sizeWithPaddingPow2; // - (0,1,0)
                    index3 = index1 + sizeWithPaddingPow2; // + (0,1,0)

                    swSolid = blocks.Get(index2 - sizeWithPadding).Solid; // -1,-1,-1
                    s_Solid = blocks.Get(index2).Solid;                   // -1,-1, 0
                    seSolid = blocks.Get(index2 + sizeWithPadding).Solid; // -1,-1, 1
                    _wSolid = blocks.Get(index1 - sizeWithPadding).Solid; // -1, 0,-1
                    _eSolid = blocks.Get(index1 + sizeWithPadding).Solid; // -1, 0, 1
                    nwSolid = blocks.Get(index3 - sizeWithPadding).Solid; // -1, 1,-1
                    n_Solid = blocks.Get(index3).Solid;                   // -1, 1, 0
                    neSolid = blocks.Get(index3 + sizeWithPadding).Solid; // -1, 1, 1
                    break;
            }

            return new BlockLightData(nwSolid, n_Solid, neSolid, _eSolid, seSolid, s_Solid, swSolid, _wSolid);
        }
        
        public static void AdjustColors(Chunk chunk, Color32[] data, BlockLightData light)
        {
            if (!chunk.world.config.addAOToMesh)
                return;

            AdjustColorsAO(data, light, chunk.world.config.ambientOcclusionStrength);
        }

        private static void PrepareTexture(Vector2[] data, ref Rect texture, bool rotated, bool backface)
        {
            if (!rotated)
            {
                if (backface)
                {
                    data[0] = new Vector2(texture.x + texture.width, texture.y);
                    data[1] = new Vector2(texture.x + texture.width, texture.y + texture.height);
                    data[2] = new Vector2(texture.x, texture.y + texture.height);
                    data[3] = new Vector2(texture.x, texture.y);
                }
                else
                {
                    data[0] = new Vector2(texture.x, texture.y);
                    data[1] = new Vector2(texture.x, texture.y + texture.height);
                    data[2] = new Vector2(texture.x + texture.width, texture.y + texture.height);
                    data[3] = new Vector2(texture.x + texture.width, texture.y);
                }
            }
            else
            {
                if (backface)
                {
                    data[0] = new Vector2(texture.x + texture.width, texture.y + texture.height);
                    data[1] = new Vector2(texture.x, texture.y + texture.height);
                    data[2] = new Vector2(texture.x, texture.y);
                    data[3] = new Vector2(texture.x + texture.width, texture.y);
                }
                else
                {
                    data[0] = new Vector2(texture.x, texture.y + texture.height);
                    data[1] = new Vector2(texture.x + texture.width, texture.y + texture.height);
                    data[2] = new Vector2(texture.x + texture.width, texture.y);
                    data[3] = new Vector2(texture.x, texture.y);
                }
            }
        }

        public static void PrepareTexture(Chunk chunk, ref Vector3Int localPos, Vector2[] data, Direction direction, TextureCollection textureCollection, bool rotated)
        {
            Rect texture = textureCollection.GetTexture(chunk, ref localPos, direction);
            bool backface = DirectionUtils.IsBackface(direction);
            PrepareTexture(data, ref texture, rotated, backface);
        }        

        public static void PrepareTexture(Chunk chunk, ref Vector3Int localPos, Vector2[] data, Direction direction, TextureCollection[] textureCollections, bool rotated)
        {
            Rect texture = textureCollections[(int)direction].GetTexture(chunk, ref localPos, direction);
            bool backface = DirectionUtils.IsBackface(direction);
            PrepareTexture(data, ref texture, rotated, backface);
        }

        private static void SetColorsAO(Color32[] data, BlockLightData light, float strength)
        {
            // 0.33f for there are 3 degrees of AO darkening (0.33f * 3 =~ 1f)
            float str = 0.33f * strength;
            float ne = 1f - light.neAO * str;
            float se = 1f - light.seAO * str;
            float sw = 1f - light.swAO * str;
            float nw = 1f - light.nwAO * str;

            SetColors(data, sw, nw, ne, se, light.FaceRotationNecessary);
        }
        
        private static void AdjustColorsAO(Color32[] data, BlockLightData light, float strength)
        {
            // 0.33f for there are 3 degrees of AO darkening (0.33f * 3 =~ 1f)
            float str = 0.33f * strength;
            float ne = 1f - light.neAO * str;
            float se = 1f - light.seAO * str;
            float sw = 1f - light.swAO * str;
            float nw = 1f - light.nwAO * str;

            AdjustColors(data, sw, nw, ne, se, light.FaceRotationNecessary);
        }

        public static void SetColors(Color32[] data, float sw, float nw, float ne, float se, bool rotated)
        {
            float _sw = (sw*255.0f).Clamp(0f,255f);
            float _nw = (nw*255.0f).Clamp(0f,255f);
            float _ne = (ne*255.0f).Clamp(0f,255f);
            float _se = (se*255.0f).Clamp(0f,255f);

            byte sw_ = (byte)_sw;
            byte nw_ = (byte)_nw;
            byte ne_ = (byte)_ne;
            byte se_ = (byte)_se;

            if (!rotated)
            {
                data[0] = new Color32(sw_, sw_, sw_, 255);
                data[1] = new Color32(nw_, nw_, nw_, 255);
                data[2] = new Color32(ne_, ne_, ne_, 255);
                data[3] = new Color32(se_, se_, se_, 255);
            }
            else
            {
                data[0] = new Color32(nw_, nw_, nw_, 255);
                data[1] = new Color32(ne_, ne_, ne_, 255);
                data[2] = new Color32(se_, se_, se_, 255);
                data[3] = new Color32(sw_, sw_, sw_, 255);
            }
        }
        
        private static Color32 ToColor32(ref Color32 col, float coef)
        {
            return new Color32(
                (byte)(col.r * coef),
                (byte)(col.g * coef),
                (byte)(col.b * coef),
                col.a
                );
        }
        
        public static void AdjustColors(Color32[] data, float sw, float nw, float ne, float se, bool rotated)
        {
            sw = sw.Clamp(0f, 1f);
            nw = nw.Clamp(0f, 1f);
            ne = ne.Clamp(0f, 1f);
            se = se.Clamp(0f, 1f);

            if (!rotated)
            {
                data[0] = ToColor32(ref data[0], sw);
                data[1] = ToColor32(ref data[1], nw);
                data[2] = ToColor32(ref data[2], ne);
                data[3] = ToColor32(ref data[3], se);
            }
            else
            {
                data[0] = ToColor32(ref data[0], nw);
                data[1] = ToColor32(ref data[1], ne);
                data[2] = ToColor32(ref data[2], se);
                data[3] = ToColor32(ref data[3], sw);
            }
        }
    }
}