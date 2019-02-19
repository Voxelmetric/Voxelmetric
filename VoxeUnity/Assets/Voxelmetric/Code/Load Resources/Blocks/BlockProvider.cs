﻿using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Load_Resources.Blocks
{
    public class BlockProvider
    {
        //! Air type block will always be present
        public static readonly ushort AirType = 0;
        public static readonly BlockData AirBlock = new BlockData(AirType, false);

        //! Special reserved block types
        public static readonly ushort FirstReservedSimpleType = 1;
        public static readonly ushort LastReservedSimpleType = (ushort)(FirstReservedSimpleType+254);
        public static readonly ushort LastReservedType = LastReservedSimpleType;
        public static readonly ushort FirstCustomType = (ushort)(LastReservedType+1);

        //! An array of loaded block configs
        private BlockConfig[] m_configs;

        //! Mapping from config's name to type
        private readonly Dictionary<string, ushort> m_names;
        //! Mapping from typeInConfig to type
        private ushort[] m_types;

        public Block[] BlockTypes { get; private set; }

        public static BlockProvider Create()
        {
            return new BlockProvider();
        }

        private BlockProvider()
        {
            m_names = new Dictionary<string, ushort>();
        }

        public void Init(string blockFolder, World world)
        {
            // Add all the block definitions defined in the config files
            ProcessConfigs(world, blockFolder);
            
            // Build block type lookup table
            BlockTypes = new Block[m_configs.Length];
            for (int i = 0; i< m_configs.Length; i++)
            {
                BlockConfig config = m_configs[i];

                Block block = (Block)Activator.CreateInstance(config.blockClass);
                block.Init((ushort)i, config);
                BlockTypes[i] = block;
            }

            // Once all blocks are set up, call OnInit on them. It is necessary to do it in a separate loop
            // in order to ensure there will be no dependency issues.
            for (int i = 0; i < BlockTypes.Length; i++)
            {
                Block block = BlockTypes[i];
                block.OnInit(this);
            }

            // Add block types from config
            foreach (var configFile in m_configs)
            {
                configFile.OnPostSetUp(world);
            }
        }

        // World is only needed for setting up the textures
        private void ProcessConfigs(World world, string blockFolder)
        {
            var configFiles = Resources.LoadAll<TextAsset>(blockFolder);
            List<BlockConfig> configs = new List<BlockConfig>(configFiles.Length);
            Dictionary<ushort, ushort> types = new Dictionary<ushort, ushort>();

            // Add reserved block types
            AddBlockType(configs, types, BlockConfig.CreateAirBlockConfig(world));
            for(ushort i=1; i<=LastReservedSimpleType; i++)
                AddBlockType(configs, types, BlockConfig.CreateColorBlockConfig(world, i));

            // Add block types from config
            foreach (var configFile in configFiles)
            {
                Hashtable configHash = JsonConvert.DeserializeObject<Hashtable>(configFile.text);

                Type configType = Type.GetType(configHash["configClass"] + ", " + typeof(BlockConfig).Assembly, false);
                if (configType == null)
                {
                    Debug.LogError("Could not create config for " + configHash["configClass"]);
                    continue;
                }

                BlockConfig config = (BlockConfig)Activator.CreateInstance(configType);
                if (!config.OnSetUp(configHash, world))
                    continue;

                if (!VerifyBlockConfig(types, config))
                    continue;

                AddBlockType(configs, types, config);
            }

            m_configs = configs.ToArray();

            // Now iterate over configs and find the one with the highest TypeInConfig
            ushort maxTypeInConfig = LastReservedType;
            for (int i = 0; i<m_configs.Length; i++)
            {
                if (m_configs[i].typeInConfig>maxTypeInConfig)
                    maxTypeInConfig = m_configs[i].typeInConfig;
            }

            // Allocate maxTypeInConfigs big array now and map config types to runtime types
            m_types = new ushort[maxTypeInConfig+FirstCustomType];
            for (ushort i = 0; i < m_configs.Length; i++)
            {
                m_types[m_configs[i].typeInConfig] = i;
            }
        }

        private bool VerifyBlockConfig(Dictionary<ushort, ushort> types, BlockConfig config)
        {
            // Unique identifier of block type
            if (m_names.ContainsKey(config.name))
            {
                Debug.LogErrorFormat("Two blocks with the name {0} are defined", config.name);
                return false;
            }

            // Unique identifier of block type
            if (types.ContainsKey(config.typeInConfig))
            {
                Debug.LogErrorFormat("Two blocks with type {0} are defined", config.typeInConfig);
                return false;
            }

            // Class name must be valid
            if (config.blockClass == null)
            {
                Debug.LogErrorFormat("Invalid class name {0} for block {1}", config.className, config.name);
                return false;
            }

            // Use the type defined in the config if there is one, otherwise add one to the largest index so far
            if (config.type == ushort.MaxValue)
            {
                Debug.LogError("Maximum number of block types reached for " + config.name);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Adds a block type to the index and adds it's name to a dictionary for quick lookup
        /// </summary>
        /// <param name="configs">A list of configs</param>
        /// <param name="types"></param>
        /// <param name="config">The controller object for this block</param>
        /// <returns>The index of the block</returns>
        private void AddBlockType(List<BlockConfig> configs, Dictionary<ushort, ushort> types, BlockConfig config)
        {
            config.type = (ushort)configs.Count;
            configs.Add(config);
            m_names.Add(config.name, config.type);
            types.Add(config.typeInConfig, config.type);
        }

        public ushort GetType(string name)
        {
            ushort type;
            if (m_names.TryGetValue(name, out type))
                return type;

            Debug.LogError("Block not found: " + name);
            return AirType;
        }

        public ushort GetTypeFromTypeInConfig(ushort typeInConfig)
        {
            if (typeInConfig<m_types.Length)
                return m_types[typeInConfig];

            Debug.LogError("TypeInConfig not found: " + typeInConfig);
            return AirType;
        }

        public Block GetBlock(string name)
        {
            ushort type;
            if (m_names.TryGetValue(name, out type))
                return BlockTypes[type];

            Debug.LogError("Block not found: " + name);
            return BlockTypes[AirType];
        }

        public BlockConfig GetConfig(ushort type)
        {
            if (type<m_configs.Length)
                return m_configs[type];

            Debug.LogError("Config not found: "+type);
            return m_configs[AirType];
        }
    }
}
