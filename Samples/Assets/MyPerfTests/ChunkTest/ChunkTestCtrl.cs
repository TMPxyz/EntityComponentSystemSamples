using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

namespace MH
{
    public class ChunkTestCtrl : MonoBehaviour
    {
        const int ENT_COUNT = 10000;

        EntityArchetype arche_A;
        EntityArchetype arche_B;

        SysChunk _sys_chunk;
        SysChunk2 _sys_chunk2;

        void Start()
        {
            var em = World.Active.EntityManager;
            arche_A = em.CreateArchetype(typeof(CpA), typeof(CpB), typeof(CpC));
            arche_B = em.CreateArchetype(typeof(CpC), typeof(CpD), typeof(CpE));

            _sys_chunk = World.Active.GetOrCreateSystem<SysChunk>(); _sys_chunk.Enabled = false;
            _sys_chunk2 = World.Active.GetOrCreateSystem<SysChunk2>();

            for(int i=0; i<ENT_COUNT; ++i)
            {
                em.CreateEntity(arche_A);
                em.CreateEntity(arche_B);
            }
        }

        void Update()
        {
            if( Input.GetMouseButtonDown(0) )
            {
                _sys_chunk2.OutputStat();
            }
        }
    }
}