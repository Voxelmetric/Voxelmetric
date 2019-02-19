﻿using System;
using System.Collections.Generic;
using System.IO;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Common.IO;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Core.Serialization
{
    public class Save: IBinarizable
    {
        public static readonly short SaveVersion = 1;

        public Chunk Chunk { get; private set; }
        public bool IsDifferential { get; private set; }

        //! A list of modified positions
        private BlockPos[] m_positionsModified;
        //! A list of modified blocks
        private BlockData[] m_blocksModified;

        // Temporary structures
        private byte[] m_positionsBytes;
        private byte[] m_blocksBytes;

        public Save(Chunk chunk)
        {
            Chunk = chunk;
            IsDifferential = false;
        }

        public void Reset()
        {
            MarkAsProcessed();
            ResetTemporary();            
        }
        
        public void MarkAsProcessed()
        {
            // Release the memory allocated by temporary buffers
            m_positionsModified = null;
            m_blocksModified = null;
        }

        private void ResetTemporary()
        {
            // Reset temporary buffers
            m_positionsBytes = null;
            m_blocksBytes = null;
        }

        public bool IsBinarizeNecessary()
        {
            // When doing a pure differential serialization we need data
            if (
                Features.UseDifferentialSerialization &&
                !Features.UseDifferentialSerialization_ForceSaveHeaders &&
                m_blocksModified == null
                )
                return false;

            return true;
        }

        public bool Binarize(BinaryWriter bw)
        {
            bw.Write(SaveVersion);
            bw.Write((byte)(Features.UseDifferentialSerialization ? 1 : 0));
            bw.Write(Env.ChunkSizePow3);
            bw.Write(Chunk.Blocks.NonEmptyBlocks);

            int blockPosSize = StructSerialization.TSSize<BlockPos>.ValueSize;
            int blockDataSize = StructSerialization.TSSize<BlockData>.ValueSize;

            // Chunk data
            if (Features.UseDifferentialSerialization)
            {
                if(m_blocksModified==null)
                    bw.Write(0);
                else
                {
                    int posLenBytes = m_blocksModified.Length * blockPosSize;
                    int blkLenBytes = m_blocksModified.Length * blockDataSize;

                    bw.Write(m_blocksModified.Length);
                    bw.Write(m_positionsBytes, 0, posLenBytes);
                    bw.Write(m_blocksBytes, 0, blkLenBytes);
                }
            }
            else
            {
                // Write compressed data to file
                bw.Write(m_blocksBytes.Length);
                bw.Write(m_blocksBytes, 0, m_blocksBytes.Length);
            }

            ResetTemporary();
            return true;
        }

        public bool Debinarize(BinaryReader br)
        {
            bool success = true;

            // Read the version number
            int version = br.ReadInt16();
            if (version!=SaveVersion)
                return false;

            // 0/1 allowed for IsDifferential
            byte isDifferential = br.ReadByte();
            if (isDifferential!=0 && isDifferential!=1)
            {
                success = false;
                goto deserializeFail;
            }
            IsDifferential = isDifferential==1;

            // Current chunk size must match the saved chunk size
            int chunkBlocks = br.ReadInt32();
            if (chunkBlocks!=Env.ChunkSizePow3)
            {
                success = false;
                goto deserializeFail;
            }

            // NonEmptyBlocks must be a sane number in chunkBlocks range
            int nonEmptyBlocks = br.ReadInt32();
            if (nonEmptyBlocks<0 || nonEmptyBlocks>chunkBlocks)
            {
                success = false;
                goto deserializeFail;
            }
            Chunk.Blocks.NonEmptyBlocks = nonEmptyBlocks;
            
            while (true)
            {
                if (IsDifferential)
                {
                    int lenBlocks = br.ReadInt32();
                    if (lenBlocks > 0)
                    {
                        int blockPosSize = StructSerialization.TSSize<BlockPos>.ValueSize;
                        int blockDataSize = StructSerialization.TSSize<BlockData>.ValueSize;

                        int posLenBytes = lenBlocks * blockPosSize;
                        int blkLenBytes = lenBlocks * blockDataSize;

                        m_positionsBytes = new byte[posLenBytes];
                        int read = br.Read(m_positionsBytes, 0, posLenBytes);
                        if (read!=posLenBytes)
                        {
                            // Length must match
                            success = false;
                            goto deserializeFail;
                        }

                        m_blocksBytes = new byte[blkLenBytes];
                        read = br.Read(m_blocksBytes, 0, blkLenBytes);
                        if (read!=blkLenBytes)
                        {
                            // Length must match
                            success = false;
                            goto deserializeFail;
                        }
                    }
                    else
                    {
                        m_blocksBytes = null;
                        m_positionsBytes = null;
                    }
                }
                else
                {                    
                    int blkLenBytes = br.ReadInt32();
                    m_blocksBytes = new byte[blkLenBytes];

                    // Read raw data
                    int readLength = br.Read(m_blocksBytes, 0, blkLenBytes);
                    if (readLength!= blkLenBytes)
                    {
                        // Length must match
                        success = false;
                        goto deserializeFail;
                    }
                }

                break;
            }
        deserializeFail:
            if (!success)
            {
                // Revert any changes we performed on our chunk
                Chunk.Blocks.NonEmptyBlocks = -1;
                ResetTemporary();
            }

            return success;
        }

        public bool DoCompression()
        {
            if (Features.UseDifferentialSerialization)
            {
                var provider = Chunk.world.blockProvider;
                int blockPosSize = StructSerialization.TSSize<BlockPos>.ValueSize;
                int blockDataSize = StructSerialization.TSSize<BlockData>.ValueSize;

                int posLenBytes = m_blocksModified.Length * blockPosSize;
                int blkLenBytes = m_blocksModified.Length * blockDataSize;
                m_positionsBytes = new byte[posLenBytes];
                m_blocksBytes = new byte[blkLenBytes];

                unsafe
                {
                    // Pack positions to a byte array
                    fixed (byte* pDst = m_positionsBytes)
                    {
                        for (int i = 0, j = 0; i < m_blocksModified.Length; i++, j += blockPosSize)
                        {
                            *(BlockPos*)&pDst[j] = m_positionsModified[i];
                        }
                    }
                    // Pack block data to a byte array
                    fixed (BlockData* pBD = m_blocksModified)
                    fixed (byte* pDst = m_blocksBytes)
                    {
                        for (int i = 0, j = 0; i < m_blocksModified.Length; i++, j += blockDataSize)
                        {
                            BlockData* bd = &pBD[i];
                            // Convert block types from internal optimized version into global types
                            ushort typeInConfig = provider.GetConfig(bd->Type).typeInConfig;

                            *(BlockData*)&pDst[j] = new BlockData(typeInConfig, bd->Solid);
                        }
                    }
                }
            }
            else
            {
                var pools = Globals.WorkPool.GetPool(Chunk.ThreadID);
                var provider = Chunk.world.blockProvider;

                int blockDataSize = StructSerialization.TSSize<BlockData>.ValueSize;
                int requestedByteSize = Env.ChunkSizePow3 * blockDataSize;

                // Pop large enough buffers from the pool
                byte[] tmp = pools.ByteArrayPool.Pop(requestedByteSize);
                byte[] bytesCompressed = pools.ByteArrayPool.Pop(requestedByteSize);
                {
                    ChunkBlocks blocks = Chunk.Blocks;
                    int i = 0;

                    int index = Helpers.ZeroChunkIndex;
                    int yOffset = Env.ChunkSizeWithPaddingPow2- Env.ChunkSize*Env.ChunkSizeWithPadding;
                    int zOffset = Env.ChunkSizeWithPadding-Env.ChunkSize;

                    for (int y = 0; y<Env.ChunkSize; ++y, index+=yOffset)
                    {
                        for (int z = 0; z<Env.ChunkSize; ++z, index+=zOffset)
                        {
                            for (int x = 0; x<Env.ChunkSize; ++x, i += blockDataSize, ++index)
                            {
                                BlockData bd = blocks.Get(index);

                                // Convert block types from internal optimized version into global types
                                ushort typeInConfig = provider.GetConfig(bd.Type).typeInConfig;

                                // Write updated block data to destination buffer
                                unsafe
                                {
                                    fixed (byte* pDst = tmp)
                                    {
                                        *(BlockData*)&pDst[i] = new BlockData(typeInConfig, bd.Solid);
                                    }
                                }
                            }
                        }
                    }

                    // Compress bytes
                    int blkLenBytes = CLZF2.lzf_compress(tmp, requestedByteSize, ref bytesCompressed);
                    m_blocksBytes = new byte[blkLenBytes];

                    // Copy data from a temporary buffer to block buffer
                    Array.Copy(bytesCompressed, 0, m_blocksBytes, 0, blkLenBytes);
                }

                // Return our temporary buffer back to the pool
                pools.ByteArrayPool.Push(bytesCompressed);
                pools.ByteArrayPool.Push(tmp);
            }

            return true;
        }

        public bool CanDecompress()
        {
            if (IsDifferential)
                return m_positionsBytes != null && m_blocksBytes != null;
            else
                return m_blocksBytes != null;
        }

        public bool DoDecompression()
        {
            var pools = Globals.WorkPool.GetPool(Chunk.ThreadID);
            var provider = Chunk.world.blockProvider;

            if (IsDifferential)
            {
                int blockPosSize = StructSerialization.TSSize<BlockPos>.ValueSize;
                int blockDataSize = StructSerialization.TSSize<BlockData>.ValueSize;

                m_positionsModified = new BlockPos[m_positionsBytes.Length / blockPosSize];
                m_blocksModified = new BlockData[m_blocksBytes.Length / blockDataSize];

                int i, j;
                unsafe
                {
                    // Extract positions
                    fixed (byte* pSrc = m_positionsBytes)
                    {
                        for (i = 0, j = 0; j<m_positionsModified.Length; i += blockPosSize, j++)
                        {
                            m_positionsModified[j] = *(BlockPos*)&pSrc[i];
                        }
                    }
                    // Extract block data
                    fixed (byte* pSrc = m_blocksBytes)
                    {
                        for (i = 0, j = 0; j<m_blocksModified.Length; i += blockDataSize, j++)
                        {
                            BlockData* bd = (BlockData*)&pSrc[i];
                            // Convert global block types into internal optimized version
                            ushort type = provider.GetTypeFromTypeInConfig(bd->Type);

                            m_blocksModified[j] = new BlockData(type, bd->Solid);
                        }
                    }
                }
            }
            else
            {
                int blockDataSize = StructSerialization.TSSize<BlockData>.ValueSize;
                int requestedByteSize = Env.ChunkSizePow3 * blockDataSize;

                // Pop a large enough buffers from the pool
                var bytes = pools.ByteArrayPool.Pop(requestedByteSize);
                {
                    // Decompress data
                    int decompressedLength = CLZF2.lzf_decompress(m_blocksBytes, m_blocksBytes.Length, ref bytes);
                    if (decompressedLength!=Env.ChunkSizePow3 * blockDataSize)
                    {
                        m_blocksBytes = null;
                        return false;
                    }

                    // Fill chunk with decompressed data
                    ChunkBlocks blocks = Chunk.Blocks;
                    int i = 0;
                    unsafe
                    {
                        fixed (byte* pSrc = bytes)
                        {
                            int index = Helpers.ZeroChunkIndex;
                            int yOffset = Env.ChunkSizeWithPaddingPow2- Env.ChunkSize*Env.ChunkSizeWithPadding;
                            int zOffset = Env.ChunkSizeWithPadding-Env.ChunkSize;

                            for (int y = 0; y<Env.ChunkSize; ++y, index+=yOffset)
                            {
                                for (int z = 0; z<Env.ChunkSize; ++z, index+=zOffset)
                                {
                                    for (int x = 0; x<Env.ChunkSize; ++x, i += blockDataSize, ++index)
                                    {
                                        BlockData* bd = (BlockData*)&pSrc[i];

                                        // Convert global block type into internal optimized version
                                        ushort type = provider.GetTypeFromTypeInConfig(bd->Type);

                                        blocks.SetRaw(index, new BlockData(type, bd->Solid));
                                    }
                                }
                            }
                        }
                    }
                }
                // Return our temporary buffer back to the pool
                pools.ByteArrayPool.Push(bytes);
            }

            ResetTemporary();
            return true;
        }

        public bool ConsumeChanges()
        {
            ChunkBlocks blocks = Chunk.Blocks;

            if (!Features.UseDifferentialSerialization)
                return true;

            if (Features.UseDifferentialSerialization_ForceSaveHeaders)
            {
                if (blocks.modifiedBlocks.Count <= 0)
                    return true;
            }
            else
            {
                if (blocks.modifiedBlocks.Count <= 0)
                    return false;
            }
            
            Dictionary<BlockPos, BlockData> blocksDictionary = new Dictionary<BlockPos, BlockData>();
            
            // Create a map of modified blocks and their positions
            // TODO: Depending on the amount of changes this could become a performance bottleneck
            for (int i = 0; i < blocks.modifiedBlocks.Count; i++)
            {
                var pos = blocks.modifiedBlocks[i];
                // Remove any existing blocks in the dictionary. They come from the existing save and are overwritten
                blocksDictionary.Remove(pos);
                blocksDictionary.Add(pos, blocks.Get(Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, pos.z)));
            }

            int cnt = blocksDictionary.Keys.Count;
            if (cnt > 0)
            {
                m_blocksModified = new BlockData[cnt];
                m_positionsModified = new BlockPos[cnt];

                int index = 0;
                foreach (var pair in blocksDictionary)
                {
                    m_blocksModified[index] = pair.Value;
                    m_positionsModified[index] = pair.Key;
                    ++index;
                }
            }

            return true;
        }

        public void CommitChanges()
        {
            if (!IsDifferential)
                return;

            // Rewrite generated blocks with differential positions
            if (m_blocksModified!=null)
            {
                for (int i = 0; i<m_blocksModified.Length; i++)
                {
                    BlockPos pos = m_positionsModified[i];
                    Chunk.Blocks.SetRaw(Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, pos.z), m_blocksModified[i]);
                }
            }

            MarkAsProcessed();
        }
    }
}