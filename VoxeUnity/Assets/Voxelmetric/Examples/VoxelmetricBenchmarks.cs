﻿using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Voxelmetric.Code.Utilities;
using UnityEngine;
using UnityEngine.Assertions;
using Voxelmetric.Code;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Common.IO;
using Voxelmetric.Code.Common.Memory;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Utilities.Noise;
using Random = System.Random;
using Vector3Int = Voxelmetric.Code.Data_types.Vector3Int;

namespace Voxelmetric.Examples
{
    class VoxelmetricBenchmarks : MonoBehaviour
    {
        void Awake()
        {
            Globals.InitWorkPool();
            Globals.InitIOPool();

            Benchmark_Modulus3();
            Benchmark_AbsValue();
            Benchmark_3D_to_1D_Index();
            Benchmark_1D_to_3D_Index();
            Benchmark_Noise();
            Benchmark_Noise_Dowsampling();
            Benchmark_Compression();
            Benchmark_MemCopy();
            Application.Quit();
        }

        private double t, t2;
        private string output;

        void Benchmark_Modulus3()
        {
            const int iters = 1000000;

            Debug.Log("Bechmark - mod3");
            using (StreamWriter writer = File.CreateText("perf_mod3.txt"))
            {
                uint[] number = { 0 };
                double t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        number[0] = number[0] % 3;
                    }, iters
                    );
                t2 = t / iters;
                output = string.Format("Mod3\nout:{0}\ntime:{1} | {2} ms",
                    number[0], t.ToString(CultureInfo.InvariantCulture), t2.ToString(CultureInfo.InvariantCulture));
                Debug.Log(output);
                foreach (string s in output.Split('\n')) writer.WriteLine(s);

                number[0] = 0;
                t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        number[0] = Helpers.Mod3(number[0]);
                    }, iters
                    );
                t2 = t / iters;
                output = string.Format("Mod3 mersenne\nout:{0}\ntime:{1} | {2} ms",
                    number[0], t.ToString(CultureInfo.InvariantCulture), t2.ToString(CultureInfo.InvariantCulture));
                Debug.Log(output);
                foreach (string s in output.Split('\n')) writer.WriteLine(s);
            }
        }

        void Benchmark_AbsValue()
        {
            const int iters = 1000000;

            Debug.Log("Bechmark - abs");
            using (StreamWriter writer = File.CreateText("perf_abs.txt"))
            {
                int[] number = { 0 };
                double t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        number[0] = Mathf.Abs(number[0]);
                    }, iters
                    );
                t2 = t / iters;
                output = string.Format("Mathf.abs\nout:{0}\ntime:{1} | {2} ms",
                    number[0], t.ToString(CultureInfo.InvariantCulture), t2.ToString(CultureInfo.InvariantCulture));
                Debug.Log(output);
                foreach (string s in output.Split('\n')) writer.WriteLine(s);

                number[0] = 0;
                t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        number[0] = Math.Abs(number[0]);
                    }, iters
                    );
                t2 = t / iters;
                output = string.Format("Math.abs\nout:{0}\ntime:{1} | {2} ms",
                    number[0], t.ToString(CultureInfo.InvariantCulture), t2.ToString(CultureInfo.InvariantCulture));
                Debug.Log(output);
                foreach (string s in output.Split('\n')) writer.WriteLine(s);

                number[0] = 0;
                t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        number[0] = Helpers.Abs(number[0]);
                    }, iters
                    );
                t2 = t / iters;
                output = string.Format("Helpers.Abs\nout:{0}\ntime:{1} | {2} ms",
                    number[0], t.ToString(CultureInfo.InvariantCulture), t2.ToString(CultureInfo.InvariantCulture));
                Debug.Log(output);
                foreach (string s in output.Split('\n')) writer.WriteLine(s);

                number[0] = 0;
                t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        number[0] = number[0] < 0 ? -number[0] : number[0];
                    }, iters
                    );
                t2 = t / iters;
                output = string.Format("i < 0 ? -i : i\nout:{0}\ntime:{1} | {2} ms",
                    number[0], t.ToString(CultureInfo.InvariantCulture), t2.ToString(CultureInfo.InvariantCulture));
                Debug.Log(output);
                foreach (string s in output.Split('\n')) writer.WriteLine(s);

                number[0] = 0;
                t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        int mask = number[0] >> 31;
                        number[0] = (number[0] + mask) ^ mask;
                    }, iters
                    );
                t2 = t / iters;
                output = string.Format("(i + mask) ^ mask\nout:{0}\ntime:{1} | {2} ms",
                    number[0], t.ToString(CultureInfo.InvariantCulture), t2.ToString(CultureInfo.InvariantCulture));
                Debug.Log(output);
                foreach (string s in output.Split('\n')) writer.WriteLine(s);

                number[0] = 0;
                t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        number[0] = (number[0] + (number[0] >> 31)) ^ (number[0] >> 31);
                    }, iters
                    );
                t2 = t / iters;
                output = string.Format("(i + (i >> 31)) ^ (i >> 31)\nout:{0}\ntime:{1} | {2} ms",
                    number[0], t.ToString(CultureInfo.InvariantCulture), t2.ToString(CultureInfo.InvariantCulture));
                Debug.Log(output);
                foreach (string s in output.Split('\n')) writer.WriteLine(s);
            }
        }

        void Benchmark_3D_to_1D_Index()
        {
            const int iters = 1000000;

            Debug.Log("Bechmark - 3D to 1D index calculation");
            using (StreamWriter writer = File.CreateText("perf_3d_to_1d_index.txt"))
            {
                int[] number = { 0, 0, 0, 0 };
                double t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        Helpers.GetIndex3DFrom1D(number[0], out number[1], out number[2],
                                                 out number[3], Env.ChunkSize, Env.ChunkSize);
                    }, iters
                    );
                t2 = t / iters;
                output = string.Format("GetIndex3DFrom1D\nout:{0}, x:{1},y:{2},z:{3}\ntime:{4} | {5} ms",
                    number[0], number[1], number[2], number[3], t.ToString(CultureInfo.InvariantCulture), t2.ToString(CultureInfo.InvariantCulture));
                Debug.Log(output);
                foreach (string s in output.Split('\n')) writer.WriteLine(s);

                number[0] = 0;
                number[1] = 0;
                number[2] = 0;
                number[3] = 0;
                t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        Helpers.GetIndex3DFrom1D(number[0], out number[1], out number[2],
                                                 out number[3], 33, 33);
                    }, iters
                    );
                t2 = t / iters;
                output = string.Format("GetIndex3DFrom1D non_pow_of_2\nout:{0}, x:{1},y:{2},z:{3}\ntime:{4} | {5} ms",
                    number[0], number[1], number[2], number[3], t.ToString(CultureInfo.InvariantCulture), t2.ToString(CultureInfo.InvariantCulture));
                Debug.Log(output);
                foreach (string s in output.Split('\n')) writer.WriteLine(s);

                number[0] = 0;
                number[1] = 0;
                number[2] = 0;
                number[3] = 0;
                t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[0];
                        Helpers.GetChunkIndex3DFrom1D(number[0], out number[1], out number[2],
                                                      out number[3]);
                    }, iters
                    );
                t2 = t / iters;
                output = string.Format("GetChunkIndex3DFrom1D\nout:{0}, x:{1},y:{2},z:{3}\ntime:{4} | {5} ms",
                    number[0], number[1], number[2], number[3], t.ToString(CultureInfo.InvariantCulture), t2.ToString(CultureInfo.InvariantCulture));
                Debug.Log(output);
                foreach (string s in output.Split('\n')) writer.WriteLine(s);
            }
        }

        void Benchmark_1D_to_3D_Index()
        {
            const int iters = 1000000;

            Debug.Log("Bechmark - 1D to 3D index calculation");
            using (StreamWriter writer = File.CreateText("perf_1d_to_3d_index.txt"))
            {
                int[] number = { 0, 0, 0, 0 };
                double t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[1];
                        ++number[2];
                        ++number[3];
                        number[0] = Helpers.GetIndex1DFrom3D(number[1], number[2], number[3],
                                                             Env.ChunkSize, Env.ChunkSize);
                    }, iters
                    );
                t2 = t / iters;
                output = string.Format("GetIndex1DFrom3D\nout:{0}, x:{1},y:{2},z:{3}\ntime:{4} | {5} ms",
                    number[0], number[1], number[2], number[3], t.ToString(CultureInfo.InvariantCulture), t2.ToString(CultureInfo.InvariantCulture));
                Debug.Log(output);
                foreach (string s in output.Split('\n')) writer.WriteLine(s);

                number[0] = 0;
                number[1] = 0;
                number[2] = 0;
                number[3] = 0;
                t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[1];
                        ++number[2];
                        ++number[3];
                        number[0] = Helpers.GetIndex1DFrom3D(number[1], number[2], number[3], 33, 33);
                    }, iters
                    );
                t2 = t / iters;
                output = string.Format("GetIndex1DFrom3D non_pow_of_2\nout:{0}, x:{1},y:{2},z:{3}\ntime:{4} | {5} ms",
                    number[0], number[1], number[2], number[3], t.ToString(CultureInfo.InvariantCulture), t2.ToString(CultureInfo.InvariantCulture));
                Debug.Log(output);
                foreach (string s in output.Split('\n')) writer.WriteLine(s);

                number[0] = 0;
                number[1] = 0;
                number[2] = 0;
                number[3] = 0;
                t = Clock.BenchmarkTime(
                    () =>
                    {
                        ++number[1];
                        ++number[2];
                        ++number[3];
                        number[0] = Helpers.GetChunkIndex1DFrom3D(number[1], number[2], number[3]);
                    }, iters
                    );
                t2 = t / iters;
                output = string.Format("GetChunkIndex1DFrom3D\nout:{0}, x:{1},y:{2},z:{3}\ntime:{4} | {5} ms",
                    number[0], number[1], number[2], number[3], t.ToString(CultureInfo.InvariantCulture), t2.ToString(CultureInfo.InvariantCulture));
                Debug.Log(output);
                foreach (string s in output.Split('\n')) writer.WriteLine(s);
            }
        }

        void Benchmark_Noise()
        {
            const int iters = 10;
            FastNoise noise = new FastNoise(0);

            Debug.Log("Bechmark - 1D, 2D, 3D noise");
            using (StreamWriter writer = File.CreateText("perf_noise.txt"))
            {
                float[] number = { 0 };
                double t = Clock.BenchmarkTime(
                    () =>
                    {
                        for (int y = 0; y < Env.ChunkSize; y++)
                            for (int z = 0; z < Env.ChunkSize; z++)
                                for (int x = 0; x < Env.ChunkSize; x++)
                                    number[0] += noise.SingleSimplex(0, x, z);
                    }, iters);
                t2 = t / iters;
                output = string.Format("noise.Generate 2D\nout:{0}\ntime:{1} | {2} ms",
                    number[0], t.ToString(CultureInfo.InvariantCulture), t2.ToString(CultureInfo.InvariantCulture));
                Debug.Log(output);
                foreach (string s in output.Split('\n')) writer.WriteLine(s);

                number[0] = 0;
                t = Clock.BenchmarkTime(
                    () =>
                    {
                        for (int y = 0; y < Env.ChunkSize; y++)
                            for (int z = 0; z < Env.ChunkSize; z++)
                                for (int x = 0; x < Env.ChunkSize; x++)
                                    number[0] += noise.SingleSimplex(0, x, y, z);
                    }, iters);
                t2 = t / iters;
                output = string.Format("noise.Generate 3D\nout:{0}\ntime:{1} | {2} ms",
                    number[0], t.ToString(CultureInfo.InvariantCulture), t2.ToString(CultureInfo.InvariantCulture));
                Debug.Log(output);
                foreach (string s in output.Split('\n')) writer.WriteLine(s);
            }
        }

        private NoiseItem PrepareLookupTable1D(FastNoise noise, NoiseItem ni)
        {
            // Generate a lookup table
            int i = 0;
            for (int z = 0; z < ni.noiseGen.Size; z++)
            {
                float zf = (z << ni.noiseGen.Step);
                ni.lookupTable[i++] = noise.SingleSimplex(0, 0, zf);
            }

            return ni;
        }

        private NoiseItem PrepareLookupTable2D(FastNoise noise, NoiseItem ni)
        {
            // Generate a lookup table
            int i = 0;
            for (int z = 0; z < ni.noiseGen.Size; z++)
            {
                float zf = (z << ni.noiseGen.Step);

                for (int x = 0; x < ni.noiseGen.Size; x++)
                {
                    float xf = (x << ni.noiseGen.Step);
                    ni.lookupTable[i++] = noise.SingleSimplex(0, xf, zf);
                }
            }

            return ni;
        }

        private NoiseItem PrepareLookupTable3D(FastNoise noise, NoiseItem ni)
        {
            // Generate a lookup table
            int i = 0;
            for (int y = 0; y < ni.noiseGen.Size; y++)
            {
                float yf = (y << ni.noiseGen.Step);
                for (int z = 0; z < ni.noiseGen.Size; z++)
                {
                    float zf = (z << ni.noiseGen.Step);
                    for (int x = 0; x < ni.noiseGen.Size; x++)
                    {
                        float xf = (x << ni.noiseGen.Step);
                        ni.lookupTable[i++] = noise.SingleSimplex(0, xf, yf, zf);
                    }
                }
            }

            return ni;
        }

        void Benchmark_Noise_Dowsampling()
        {
            const int iters = 10;
            FastNoise noise = new FastNoise(0);

            Debug.Log("Bechmark - 1D, 2D, 3D noise downsampling");
            using (StreamWriter writer = File.CreateText("perf_noise_downsampling.txt"))
            {
                for (int i = 1; i <= 3; i++)
                {
                    NoiseItem ni = new NoiseItem { noiseGen = new NoiseInterpolator() };
                    ni.noiseGen.SetInterpBitStep(Env.ChunkSize, i);
                    ni.lookupTable = Helpers.CreateArray1D<float>(ni.noiseGen.Size + 1);

                    float[] number = { 0 };
                    double t = Clock.BenchmarkTime(
                        () =>
                        {
                            PrepareLookupTable1D(noise, ni);
                            for (int x = 0; x < Env.ChunkSize; x++)
                                number[0] += ni.noiseGen.Interpolate(x, ni.lookupTable);
                        }, iters);
                    t2 = t / iters;
                    output = string.Format("noise.Generate 1D\nout:{0}, downsample factor {1}\ntime:{2} | {3} ms", number[0], i,
                                    t.ToString(CultureInfo.InvariantCulture), t2.ToString(CultureInfo.InvariantCulture));
                    Debug.Log(output);
                    foreach (string s in output.Split('\n')) writer.WriteLine(s);
                }

                for (int i = 1; i <= 3; i++)
                {
                    NoiseItem ni = new NoiseItem { noiseGen = new NoiseInterpolator() };
                    ni.noiseGen.SetInterpBitStep(Env.ChunkSize, i);
                    ni.lookupTable = Helpers.CreateArray1D<float>((ni.noiseGen.Size + 1) * (ni.noiseGen.Size + 1));

                    float[] number = { 0 };
                    double t = Clock.BenchmarkTime(
                        () =>
                        {
                            PrepareLookupTable2D(noise, ni);
                            for (int z = 0; z < Env.ChunkSize; z++)
                                for (int x = 0; x < Env.ChunkSize; x++)
                                    number[0] += ni.noiseGen.Interpolate(x, z, ni.lookupTable);
                        }, iters);
                    t2 = t / iters;
                    output = string.Format("noise.Generate 2D\nout:{0}, downsample factor {1}\ntime:{2} | {3} ms", number[0], i,
                                    t.ToString(CultureInfo.InvariantCulture), t2.ToString(CultureInfo.InvariantCulture));
                    Debug.Log(output);
                    foreach (string s in output.Split('\n')) writer.WriteLine(s);
                }

                for (int i = 1; i <= 3; i++)
                {
                    NoiseItem ni = new NoiseItem { noiseGen = new NoiseInterpolator() };
                    ni.noiseGen.SetInterpBitStep(Env.ChunkSize, i);
                    ni.lookupTable = Helpers.CreateArray1D<float>((ni.noiseGen.Size + 1) * (ni.noiseGen.Size + 1) * (ni.noiseGen.Size + 1));

                    float[] number = { 0 };
                    double t = Clock.BenchmarkTime(
                        () =>
                        {
                            PrepareLookupTable3D(noise, ni);
                            for (int y = 0; y < Env.ChunkSize; y++)
                                for (int z = 0; z < Env.ChunkSize; z++)
                                    for (int x = 0; x < Env.ChunkSize; x++)
                                        number[0] += ni.noiseGen.Interpolate(x, y, z, ni.lookupTable);
                        }, iters);
                    t2 = t / iters;
                    output = string.Format("noise.Generate 3D\nout:{0}, downsample factor {1}\ntime:{2} | {3} ms", number[0], i,
                                    t.ToString(CultureInfo.InvariantCulture), t2.ToString(CultureInfo.InvariantCulture));
                    Debug.Log(output);
                    foreach (string s in output.Split('\n')) writer.WriteLine(s);
                }
            }
        }

        private static ChunkBlocks s_verifyBlocks = null;

        void Compression(StreamWriter writer, Chunk chunk, int blockTypes, int probabiltyOfChange)
        {
            const int iters = 100;
            var blocks = chunk.Blocks;

            // Initialize the block array. Padded area contains zeros, the rest is random
            {
                Random r = new Random(0);
                ushort type = (ushort)r.Next(0, blockTypes);

                int index = 0;
                for (int y = 0; y < Env.ChunkSize; ++y)
                {
                    for (int z = 0; z < Env.ChunkSize; ++z)
                    {
                        for (int x = 0; x < Env.ChunkSize; ++x, ++index)
                        {
                            int prob = r.Next(0, 99);
                            if (prob < probabiltyOfChange)
                                type = (ushort)r.Next(0, blockTypes);
                            blocks.SetRaw(index, new BlockData(type));
                        }
                    }
                }
            }

            if (s_verifyBlocks == null)
                s_verifyBlocks = new ChunkBlocks(null, chunk.SideSize);
            s_verifyBlocks.Copy(blocks, 0, 0, ChunkBlocks.GetLength(chunk.SideSize));

            {
                Debug.LogFormat("Bechmark - compression ({0} block types, probability of change: {1})", blockTypes, probabiltyOfChange);

                // Compression
                {
                    float[] number = { 0 };
                    double t = Clock.BenchmarkTime(
                        () =>
                        {
                            blocks.Compress();
                        }, iters);
                    t2 = t / iters;

                    int memSizeCompressed = blocks.BlocksCompressed.Count * StructSerialization.TSSize<BlockDataAABB>.ValueSize;
                    int memSizeUncompressed = Env.ChunkSizeWithPaddingPow3 * StructSerialization.TSSize<BlockData>.ValueSize;
                    float compressionFactor = memSizeCompressed / (float)memSizeUncompressed;

                    output = string.Format("Compression\nout:{0}, boxes created: {1}, mem: {2}/{3} (factor:{4})\ntime:{5} | {6} ms", number[0],
                                   blocks.BlocksCompressed.Count, memSizeCompressed, memSizeUncompressed, compressionFactor,
                                   t.ToString(CultureInfo.InvariantCulture), t2.ToString(CultureInfo.InvariantCulture));
                    Debug.Log(output);
                    foreach (string s in output.Split('\n')) writer.WriteLine(s);
                }                

                // Decompression
                {
                    float[] number = { 0 };
                    double t = Clock.BenchmarkTime(
                        () =>
                        {
                            blocks.Decompress();
                        }, iters);
                    t2 = t / iters;

                    output = string.Format("Decompression\nout:{0}\ntime:{1} | {2} ms", number[0],
                                   t.ToString(CultureInfo.InvariantCulture), t2.ToString(CultureInfo.InvariantCulture));
                    Debug.Log(output);
                    foreach (string s in output.Split('\n')) writer.WriteLine(s);
                }                

                // Verify that data has not changed
                for (int i = 0; i < ChunkBlocks.GetLength(chunk.SideSize); i++)
                    Assert.IsTrue(s_verifyBlocks.Get(i) == chunk.Blocks.Get(i));
            }
        }

        void Benchmark_Compression()
        {
            Chunk chunk = new Chunk();
            chunk.Init(null, Vector3Int.zero);

            using (StreamWriter writer = File.CreateText("compression.txt"))
            {
                Compression(writer, chunk, 2, 100);
                Compression(writer, chunk, 4, 100);
                Compression(writer, chunk, 8, 100);
                Compression(writer, chunk, 12, 100);

                Compression(writer, chunk, 2, 20);
                Compression(writer, chunk, 4, 20);
                Compression(writer, chunk, 8, 20);
                Compression(writer, chunk, 12, 20);

                Compression(writer, chunk, 2, 10);
                Compression(writer, chunk, 4, 10);
                Compression(writer, chunk, 8, 10);
                Compression(writer, chunk, 12, 10);

                Compression(writer, chunk, 2, 5);
                Compression(writer, chunk, 4, 5);
                Compression(writer, chunk, 8, 5);
                Compression(writer, chunk, 12, 5);
            }
        }

        private class TestClass1
        {
            private readonly unsafe byte* m_blocks;
            private readonly IntPtr rawptr;

            public unsafe TestClass1()
            {
                // Force 16-bytes aligment
                rawptr = Marshal.AllocHGlobal(Env.ChunkSizePow3 * StructSerialization.TSSize<BlockData>.ValueSize + 16);
                var aligned = new IntPtr(16 * (((long)rawptr + 15) / 16));
                m_blocks = (byte*)aligned.ToPointer();
            }

            ~TestClass1()
            {
                Marshal.FreeHGlobal(rawptr);
            }

            public unsafe BlockData this[int i]
            {
                get
                {
                    return *((BlockData*)&m_blocks[i << 1]);
                }
                set
                {
                    *((BlockData*)&m_blocks[i << 1]) = value;
                }
            }

            public unsafe void Copy(byte[] src, uint srcIndex, uint dstIndex, uint bytes)
            {
                fixed (byte* pSrc = &src[srcIndex])
                {
                    Utils.MemoryCopy(&m_blocks[dstIndex], pSrc, bytes);
                }
            }
        }

        private static BlockData[] bd2;

        private class TestClass2
        {
            private readonly BlockData[] m_blocks = Helpers.CreateArray1D<BlockData>(bd2.Length);
            public BlockData this[int i]
            {
                get { return m_blocks[i]; }
                set { m_blocks[i] = value; }
            }

            public void Copy(BlockData[] src, int srcIndex, int dstIndex, int length)
            {
                Array.Copy(src, srcIndex, m_blocks, dstIndex, length);
            }
        }

        void Benchmark_MemCopy()
        {
            int[] memItems =
            {
                32,
                64,
                128,
                256,
                Env.ChunkSizeWithPaddingPow2,
                Env.ChunkSizePow3
            };

            int[] iters =
            {
                1000000,
                1000000,
                50000,
                50000,
                10000,
                5000
            };

            Debug.Assert(memItems.Length == iters.Length);
            int maxItems = memItems[memItems.Length - 1];

            byte[] bd1 = Helpers.CreateArray1D<byte>(maxItems * StructSerialization.TSSize<BlockData>.ValueSize);
            for (int i = 0; i < bd1.Length; i++)
                bd1[i] = 1;
            bd2 = Helpers.CreateArray1D<BlockData>(maxItems);
            for (int i = 0; i < bd2.Length; i++)
                bd2[i] = new BlockData(0x101);
            BlockData dummy = new BlockData(0x101);

            TestClass1 tc1 = new TestClass1();
            TestClass2 tc2 = new TestClass2();

            Debug.Log("Bechmark - memory copy");
            using (StreamWriter writer = File.CreateText("perf_memcpy.txt"))
            {
                for (int i = 0; i < iters.Length; i++)
                {
                    int loops = iters[i];
                    int items = memItems[i];
                    uint bytes = (uint)items * (uint)StructSerialization.TSSize<BlockData>.ValueSize;

                    Debug.LogFormat("Bytes to copy: {0}", bytes);
                    writer.WriteLine("Bytes to copy: {0}", bytes);

                    {
                        float[] number = { 0 };
                        t = Clock.BenchmarkTime(
                            () =>
                            {
                                tc1.Copy(bd1, 0, 0, bytes);
                            }, loops);
                        t2 = t / loops;
                        output = string.Format("MemoryCopy\nout:{0}\ntime:{1} | {2} ms", number[0],
                                        t.ToString(CultureInfo.InvariantCulture), t2.ToString(CultureInfo.InvariantCulture));
                        Debug.Log(output);
                        foreach (string s in output.Split('\n')) writer.WriteLine(s);
                    }
                    for (int j = 0; j < items; j++)
                        Assert.IsTrue(tc1[j] == dummy);

                    {
                        float[] number = { 0 };
                        double t = Clock.BenchmarkTime(
                            () =>
                            {
                                tc2.Copy(bd2, 0, 0, items);
                            }, loops);
                        t2 = t / loops;
                        output = string.Format("ArrayCopy\nout:{0}\ntime:{1} | {2} ms", number[0],
                                        t.ToString(CultureInfo.InvariantCulture), t2.ToString(CultureInfo.InvariantCulture));
                        Debug.Log(output);
                        foreach (string s in output.Split('\n')) writer.WriteLine(s);
                    }
                    for (int j = 0; j < items; j++)
                        Assert.IsTrue(tc2[j] == dummy);
                }
            }
        }
    }
}
