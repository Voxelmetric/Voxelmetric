﻿using UnityEngine;
using System;
using System.Collections;
using SimplexNoise;
using Stopwatch = System.Diagnostics.Stopwatch;

public class TerrainGen
{
    public Noise noise;
    World world;
    TerrainLayer[] layers;

    public TerrainGen(World world, string layerFolder)
    {
        this.world = world;
        noise = new Noise(world.name);

        ConfigLoader<LayerConfig> layerConfigs = new ConfigLoader<LayerConfig>(new string[] { layerFolder });

        layers = new TerrainLayer[layerConfigs.AllConfigs().Length];
        for (int i = 0; i < layerConfigs.AllConfigs().Length; i++)
        {
            LayerConfig config = layerConfigs.AllConfigs()[i];
            var type = Type.GetType(config.layerType + ", " + typeof(Block).Assembly, false);
            if (type == null)
                Debug.LogError("Could not create layer " + config.layerType + " : " + config.name);

            layers[i] = (TerrainLayer)Activator.CreateInstance(type);
            layers[i].BaseSetUp(config, world, this);
        }
        
        //Sort the layers by index
        Array.Sort(layers);
    }

    public IEnumerator GenerateTerrainForChunkCoroutine(Chunk chunk)
    {
        long maxTime = 5; // Limit to 5 milliseconds
        int numDone = 0, numCheck = 16;
        Stopwatch stopwatch = Stopwatch.StartNew();
        for (int x = chunk.pos.x; x < chunk.pos.x + Config.Env.ChunkSize; x++)
        {
            for (int z = chunk.pos.z; z < chunk.pos.z + Config.Env.ChunkSize; z++)
            {
                GenerateTerrainForBlockColumn(x, z, false, chunk);
                ++numDone;
                if (numDone % numCheck == 0) {
                    if (stopwatch.ElapsedMilliseconds >= maxTime) {
                        stopwatch.Reset();
                        yield return null;
                        stopwatch.Start();
                    }
                }
            }
        }
        GenerateStructuresForChunk(chunk);
        yield return null;
    }

    public int GenerateTerrainForBlockColumn(int x, int z, bool justGetHeight, Chunk chunk)
    {
        int height = world.config.minY;
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == null)
            {
                Debug.LogError("Layer name '" + layers[i] + "' in layer order didn't match a valid layer");
                continue;
            }

            if (!layers[i].isStructure)
            {
                height = layers[i].GenerateLayer(chunk, x, z, height, 1, justGetHeight);
            }
        }
        return height;
    }

    public void GenerateStructuresForChunk(Chunk chunk)
    {
        for (int i = 0; i < layers.Length; i++)
        {

            if (layers[i] == null)
                continue;

            if (layers[i].isStructure)
            {
                layers[i].GenerateStructures(chunk);
            }
        }
    }

}


