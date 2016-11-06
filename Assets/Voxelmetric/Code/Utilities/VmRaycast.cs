﻿using System;
using UnityEngine;

public class VmRaycast
{
    public static VmRaycastHit Raycast(Ray ray, World world, float range = 10000f) {
        return Raycast(ray, world,
            (pos, dir, hitBlock, bPos) => hitBlock.RaycastHit(pos, dir, bPos),
            range);
    }

    public static VmRaycastHit Raycast(Ray ray, World world,
        Func<Vector3, Vector3, Block, BlockPos, bool> raycastHit,
        float range = 10000f)
    {
        // Position as we work through the raycast, starts at origin and gets updated as it reaches each block boundary on the route
        Vector3 pos = ray.origin;
        //Normalized direction of the ray
        Vector3 dir = ray.direction.normalized;

        //Transform the ray to match the rotation and position of the world:
        pos -= world.transform.position;
        pos = Quaternion.Inverse(world.gameObject.transform.rotation) * pos;
        dir = Quaternion.Inverse(world.transform.rotation) * dir;

        // BlockPos to check if the block should be returned
        BlockPos bPos = pos;
        //Block pos that gets set to one block behind the hit block, useful for placing blocks at the hit location
        BlockPos adjacentBPos = pos;

        // Positive copy of the direction
        Vector3 dirP = new Vector3(MakePositive(dir.x), MakePositive(dir.y), MakePositive(dir.z));
        // The sign of the direction
        BlockPos dirS = new Vector3(dir.x > 0 ? 1 : -1, dir.y > 0 ? 1 : -1, dir.z > 0 ? 1 : -1);

        // Boundary will be set each step as the nearest block boundary to each direction
        Vector3 boundary;
        // dist will be set to the distance in each direction to hit a boundary
        Vector3 dist;

        //The block at bPos
        Block hitBlock;
        BlockPos cPos = bPos.ContainingChunkCoordinates();
        BlockPos lPos = bPos - cPos;
        Chunk hitChunk = world.chunks.ChunkGet(cPos);
        if (hitChunk != null && bPos.y >= world.config.minY) {
            hitBlock = hitChunk.blocks.LocalGetL(lPos);
        } else {
            hitBlock = world.Void;
        }

        //Color debugLineColor = Color.magenta; //debug
        while (!raycastHit(pos, dir, hitBlock, bPos) && Vector3.Distance(ray.origin, pos) < range)
        {
            // Get the nearest upcoming boundary for each direction
            boundary.x = MakeBoundary(dirS.x, pos.x);
            boundary.y = MakeBoundary(dirS.y, pos.y);
            boundary.z = MakeBoundary(dirS.z, pos.z);

            //Find the distance to each boundary and make the number positive
            dist = boundary - pos;
            dist = new Vector3(MakePositive(dist.x), MakePositive(dist.y), MakePositive(dist.z));
            // Divide the distance by the strength of the corresponding direction, the
            // lowest number will be the boundary we will hit first. This is like distance
            // over speed = time where dirP is the speed and the it's time to reach the boundary
            dist.x = dist.x / dirP.x;
            dist.y = dist.y / dirP.y;
            dist.z = dist.z / dirP.z;

            //Vector3 oPos = pos; //debug
            // Use the shortest distance as the distance to travel this step times each direction
            // to give us the position where the ray intersects the closest boundary
            if (dist.x < dist.y && dist.x < dist.z)
            {
                pos += dist.x * dir;
            }
            else if (dist.y < dist.z)
            {
                pos += dist.y * dir;
            }
            else
            {
                pos += dist.z * dir;
            }
            //Debug line:
            //Debug.DrawLine(oPos, pos, debugLineColor, 30);
            //if (debugLineColor == Color.magenta)
            //{
            //    debugLineColor = Color.green;
            //}
            //else
            //{
            //    debugLineColor = Color.magenta;
            //}

            // Set the block pos but use ResolveBlockPos because one of the components of pos will be exactly on a block boundary
            // and will need to use the corresponding direction sign to decide which side of the boundary to fall on
            adjacentBPos = bPos;
            bPos = new BlockPos(ResolveBlockPos(pos.x, dirS.x), ResolveBlockPos(pos.y, dirS.y), ResolveBlockPos(pos.z, dirS.z));
            BlockPos newChunkPos = bPos.ContainingChunkCoordinates();
            if (newChunkPos != cPos) {
                cPos = newChunkPos;
                hitChunk = world.chunks.ChunkGet(cPos);
            }
            lPos = bPos - cPos;
            if (hitChunk != null && bPos.y >= world.config.minY) {
                hitBlock = hitChunk.blocks.LocalGetL(lPos);
            } else {
                hitBlock = world.Void;
            }

            // The while loop then evaluates if hitblock is a viable block to stop on and
            // if not does it all again starting from the new position
        }
        return new VmRaycastHit()
        {
            block = hitBlock,
            blockPos = bPos,
            adjacentPos = adjacentBPos,
            dir = dir,
            distance = Vector3.Distance(ray.origin, pos),
            scenePos = pos,
            world = world
        };
    }

    //Resolve a component of a vector3 into an int for a blockPos by using the sign
    // of the corresponding direction to decide if the position is on a boundary
    static int ResolveBlockPos(float pos, int dirS)
    {
        float fPos = pos + 0.5f;
        int iPos = (int)fPos;

        if (fPos == iPos)
        {
            if (dirS == 1)
            {
                return iPos;
            }
            else
            {
                return iPos - 1;
            }
        }
        else
        {
            return Mathf.RoundToInt(pos);
        }
    }

    // Returns the nearest boundary to pos
    static float MakeBoundary(int dirS, float pos)
    {
        int result;
        pos += 0.5f;

        if (dirS == -1)
        {
            result = Mathf.FloorToInt(pos);
        }
        else
        {
            result = Mathf.CeilToInt(pos);
        }

        if (result == pos)
        {
            result += dirS;
        }

        return result - 0.5f;
    }

    //returns f as a positive number
    static float MakePositive(float f)
    {
        if (f < 0)
            return -f;

        return f;
    }

}
