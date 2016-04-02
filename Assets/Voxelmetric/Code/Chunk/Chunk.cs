﻿using System.Text;
using UnityEngine;

public enum Stage {created, terrain, buildMesh, priorityBuildMesh, render, ready }

public class Chunk
{
    public World world;
    public BlockPos pos;

    public ChunkBlocks blocks;
    public ChunkLogic  logic;
    public ChunkRender render;

    public Stage _stage;
    public Stage stage {
        get
        {
            return _stage;
        }
        set
        {
            if (_stage != value)
            {
                world.chunksLoop.ChunkStageChanged(this, oldStage: _stage, newStage: value);
            }
            _stage = value;
        }
    }

    public Chunk(World world, BlockPos pos)
    {
        this.world = world;
        this.pos = pos;

        if (render == null)
            render = new ChunkRender(this);

        if (blocks == null)
            blocks = new ChunkBlocks(this);

        if (logic == null)
            logic = new ChunkLogic(this);
    }

    public override string ToString() {
        StringBuilder sb = new StringBuilder();
        sb.Append(world.name);
        sb.Append(", ");
        sb.Append(pos.ToString());
        sb.Append(", stage=");
        sb.Append(_stage.ToString());
        sb.Append(", blocks=");
        sb.Append(blocks.ToString());
        sb.Append(", logic=");
        sb.Append(logic.ToString());
        sb.Append(", render=");
        sb.Append(render.ToString());
        return sb.ToString();
    }

    public virtual void StartLoading()
    {
        stage = Stage.terrain;
    }

    public virtual void RegularUpdate()
    {
        if (stage != Stage.ready)
            return;

        logic.TimedUpdated();
    }

    /// <summary> Updates the chunk either now or as soon as the chunk is no longer busy </summary>
    public void UpdateNow()
    {
        stage = Stage.priorityBuildMesh;
    }

    public void UpdateSoon()
    {
        stage = Stage.buildMesh;
    }

    public void MarkForDeletion()
    {
        world.chunksLoop.AddToDeletionList(this);
    }
}