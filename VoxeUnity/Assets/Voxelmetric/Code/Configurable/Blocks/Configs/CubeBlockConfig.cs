﻿using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources.Textures;

public class CubeBlockConfig: BlockConfig
{
    public TextureCollection[] textures;

    public override bool OnSetUp(Hashtable config, World world)
    {
        if (!base.OnSetUp(config, world))
            return false;

        textures = new TextureCollection[6];
        JArray textureNames = (JArray)JsonConvert.DeserializeObject(config["textures"].ToString());

        for (int i = 0; i < 6; i++)
            textures[i] = world.textureProvider.GetTextureCollection(textureNames[i].ToObject<string>());

        return true;
    }
}
