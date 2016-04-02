﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;

[Serializable]
public class Save
{
    public BlockPos[] positions = new BlockPos[0];
    public Block[] blocks = new Block[0];

    public bool changed = false;

    [NonSerialized()] private Chunk chunk;

    public Chunk Chunk { get { return chunk; } }

    public Save(Chunk chunk, Save existing) {
        this.chunk = chunk;

        Dictionary<BlockPos, Block> blocksDictionary = new Dictionary<BlockPos, Block>();

        if (existing != null) {
            //Because existing saved blocks aren't marked as modified we have to add the
            //blocks already in the save file if there is one.
            existing.AddBlocks(blocksDictionary);
        }

        // Then add modified blocks from this chunk
        foreach (var pos in chunk.blocks.modifiedBlocks) {
            //remove any existing blocks in the dictionary as they're
            //from the existing save and are overwritten
            blocksDictionary.Remove(pos);
            blocksDictionary.Add(pos, chunk.blocks.Get(pos));
            changed = true;
        }

        blocks = new Block[blocksDictionary.Keys.Count];
        positions = new BlockPos[blocksDictionary.Keys.Count];

        int index = 0;
        foreach (var pair in blocksDictionary) {
            blocks[index] = pair.Value;
            positions[index] = pair.Key;
            index++;
        }
    }

    private void AddBlocks(Dictionary<BlockPos, Block> blocksDictionary) {
        for (int i = 0; i < blocks.Length; i++) {
            blocksDictionary.Add(positions[i], blocks[i]);
        }
    }
}