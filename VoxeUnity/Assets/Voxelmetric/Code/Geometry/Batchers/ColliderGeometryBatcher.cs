﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Voxelmetric.Code.Common.MemoryPooling;
using Voxelmetric.Code.Geometry.Buffers;

namespace Voxelmetric.Code.Geometry.Batchers
{
    public class ColliderGeometryBatcher: IGeometryBatcher
    {
        private readonly string m_prefabName;
        //! Materials our meshes are to use
        private readonly PhysicMaterial[] m_materials;
        //! A list of buffers for each material
        private readonly List<ColliderGeometryBuffer>[] m_buffers;
        //! GameObjects used to hold our geometry
        private readonly List<GameObject> m_objects;

        private bool m_enabled = false;
        public bool Enabled
        {
            set
            {
                if (value != m_enabled)
                {
                    for (int i = 0; i < m_objects.Count; i++)
                        m_objects[i].SetActive(value);
                }
                m_enabled = value;
            }
            get
            {
                return m_enabled;
            }
        }

        public ColliderGeometryBatcher(string prefabName, PhysicMaterial[] materials)
        {
            m_prefabName = prefabName;
            m_materials = materials;

            int buffersLen = (materials==null || materials.Length<1) ? 1 : materials.Length;
            m_buffers = new List<ColliderGeometryBuffer>[buffersLen];
            for (int i = 0; i < m_buffers.Length; i++)
            {
                /* TODO: Let's be optimistic and allocate enough room for just one buffer. It's going to suffice
                 * in >99% of cases. However, this prediction should maybe be based on chunk size rather then
                 * optimism. The bigger the chunk the more likely we're going to need to create more meshes to
                 * hold its geometry because of Unity's 65k-vertices limit per mesh. For chunks up to 32^3 big
                 * this should not be an issue, though.
                 */
                m_buffers[i] = new List<ColliderGeometryBuffer>(1)
                {
                    // Default render buffer
                    new ColliderGeometryBuffer()
                };
            }

            m_objects = new List<GameObject>();

            Clear();
        }

        public void Reset()
        {
            // Buffers need to be reallocated. Otherwise, more and more memory would be consumed by them. This is
            // because internal arrays grow in capacity and we can't simply release their memory by calling Clear().
            // Objects and renderers are fine, because there's usually only 1 of them. In some extreme cases they
            // may grow more but only by 1 or 2 (and only if Env.ChunkPow>5).
            for (int i = 0; i < m_buffers.Length; i++)
            {
                var geometryBuffer = m_buffers[i];
                for (int j = 0; j < geometryBuffer.Count; j++)
                {
                    if (geometryBuffer[j].WasUsed)
                        geometryBuffer[j] = new ColliderGeometryBuffer();
                }
            }

            ReleaseOldData();
            m_enabled = false;
        }

        /// <summary>
        ///     Clear all draw calls
        /// </summary>
        public void Clear()
        {
            foreach (var holder in m_buffers)
            {
                for (int i = 0; i < holder.Count; i++)
                    holder[i].Clear();
            }

            ReleaseOldData();
            m_enabled = false;
        }

        /// <summary>
        ///     Addds one face to our render buffer
        /// </summary>
        /// <param name="materialID">ID of material to use when building the mesh</param>
        /// <param name="verts"> An array of 4 vertices forming the face</param>
        /// <param name="backFace">If false, vertices are added clock-wise</param>
        public void AddFace(int materialID, Vector3[] verts, bool backFace)
        {
            Assert.IsTrue(verts.Length == 4);

            var holder = m_buffers[materialID];
            var buffer = holder[holder.Count - 1];

            // If there are too many vertices we need to create a new separate buffer for them
            if (buffer.Vertices.Count + 4 > 65000)
            {
                buffer = new ColliderGeometryBuffer();
                holder.Add(buffer);
            }

            // Add vertices
            buffer.AddVertices(verts);

            // Add indices
            buffer.AddIndices(buffer.Vertices.Count, backFace);
        }

        /// <summary>
        ///     Creates a mesh and commits it to the engine. Bounding box is calculated from vertices
        /// </summary>
        public void Commit(Vector3 position, Quaternion rotation
#if DEBUG
            , string debugName = null
#endif
            )
        {
            ReleaseOldData();
            
            for (int j = 0; j<m_buffers.Length; j++)
            {
                var holder = m_buffers[j];
                var material = (m_materials == null || m_materials.Length < 1) ? null : m_materials[j];

                for (int i = 0; i< holder.Count; i++)
                {
                    ColliderGeometryBuffer buffer = holder[i];

                    // No data means there's no mesh to build
                    if (buffer.IsEmpty)
                        continue;

                    // Create a game object for collider. Unfortunatelly, we can't use object pooling
                    // here. Unity3D would have to rebake the geometry of the old object because of a
                    // change in its position and that is very time consuming.
                    GameObject prefab = GameObjectProvider.GetPool(m_prefabName).Prefab;
                    GameObject go = Object.Instantiate(prefab);
                    go.transform.parent = GameObjectProvider.Instance.ProviderGameObject.transform;

                    {
#if DEBUG
                        go.name = string.Format(debugName, "_", i.ToString());
#endif

                        Mesh mesh = Globals.MemPools.MeshPool.Pop();
                        Assert.IsTrue(mesh.vertices.Length<=0);
                        buffer.SetupMesh(mesh, true);

                        MeshCollider collider = go.GetComponent<MeshCollider>();
                        collider.enabled = true;
                        collider.sharedMesh = null;
                        collider.sharedMesh = mesh;
                        var t = collider.transform;
                        t.position = position;
                        t.rotation = rotation;
                        collider.sharedMaterial = material;

                        m_objects.Add(go);
                    }

                    buffer.Clear();
                }
            }
        }

        /// <summary>
        ///     Creates a mesh and commits it to the engine. Bounding box set according to value passed in bounds
        /// </summary>
        public void Commit(Vector3 position, Quaternion rotation, ref Bounds bounds
#if DEBUG
            , string debugName = null
#endif
        )
        {
            ReleaseOldData();

            for (int j = 0; j<m_buffers.Length; j++)
            {
                var holder = m_buffers[j];
                var material = (m_materials == null || m_materials.Length < 1) ? null : m_materials[j];

                for (int i = 0; i<holder.Count; i++)
                {
                    ColliderGeometryBuffer buffer = holder[i];

                    // No data means there's no mesh to build
                    if (buffer.IsEmpty)
                        continue;

                    // Create a game object for collider. Unfortunatelly, we can't use object pooling
                    // here. Unity3D would have to rebake the geometry of the old object because of a
                    // change in its position and that is very time consuming.
                    GameObject prefab = GameObjectProvider.GetPool(m_prefabName).Prefab;
                    GameObject go = Object.Instantiate(prefab);
                    go.transform.parent = GameObjectProvider.Instance.ProviderGameObject.transform;

                    {
#if DEBUG
                        go.name = string.Format(debugName, "_", i.ToString());
#endif

                        Mesh mesh = Globals.MemPools.MeshPool.Pop();
                        Assert.IsTrue(mesh.vertices.Length<=0);
                        buffer.SetupMesh(mesh, false);
                        mesh.bounds = bounds;

                        MeshCollider collider = go.GetComponent<MeshCollider>();
                        collider.enabled = true;
                        collider.sharedMesh = null;
                        collider.sharedMesh = mesh;
                        var t = collider.transform;
                        t.position = position;
                        t.rotation = rotation;
                        collider.sharedMaterial = material;

                        m_objects.Add(go);
                    }

                    buffer.Clear();
                }
            }
        }

        private void ReleaseOldData()
        {
            for (int i = 0; i<m_objects.Count; i++)
            {
                var go = m_objects[i];
                // If the component does not exist it means nothing else has been added as well
                if (go==null)
                    continue;

#if DEBUG
                go.name = m_prefabName;
#endif

                MeshCollider collider = go.GetComponent<MeshCollider>();
                collider.sharedMesh.Clear(false);
                Globals.MemPools.MeshPool.Push(collider.sharedMesh);
                collider.sharedMesh = null;
                collider.sharedMaterial = null;

                Object.DestroyImmediate(go);
            }

            m_objects.Clear();
        }
    }
}