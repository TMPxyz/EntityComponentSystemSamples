using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

namespace MH
{
    using BUF = CpBufFloat;
    public struct CpBufFloat : IBufferElementData
    {
        public float v;
    }

    public class BufferCtrl : MonoBehaviour
    {
        // public int pop_cnt = 1000;
        // public int tar_cnt = 1000;

        void Start()
        {
            var w = World.Active;
            var em = w.EntityManager;

            var arche0 = em.CreateArchetype(typeof(BUF));

            var e0 = em.CreateEntity(arche0);
            var e1 = em.CreateEntity();
            var buf = em.GetBuffer<BUF>(e0);
            buf.Add(new BUF{v = 10f});

            // var arche_pop = em.CreateArchetype(typeof(CpTargetNodeBuf));
            // var arche_tar = em.CreateArchetype(typeof(CpOwner), typeof(CpAttitude), typeof(CpMindImageTargetNode), typeof(CpRelationBuf));
            // var arche_rel = em.CreateArchetype(typeof(CpOwner), typeof(CpRelation));

            // var lst_pop = new List<Entity>();
            // var lst_tar = new List<Entity>();

            // for(int i=0; i<pop_cnt; ++i)
            // {
            //     var entPop = em.CreateEntity(arche_pop);
            //     var targetBuf = em.GetBuffer<CpTargetNodeBuf>(entPop);
            //     lst_pop.Add(entPop);

            //     //---------init all (mindimage of tar in mind of pop)---------//
            //     lst_tar.Clear();
            //     for(int j=0; j<tar_cnt; ++j)
            //     {
            //         var e = em.CreateEntity(arche_tar); 
            //         targetBuf.Add( new CpTargetNodeBuf{target = e});
            //         // em.SetComponentData(e, new CpOwner{entOwner = entPop});
            //         // em.SetComponentData(e, new CpMindImageTargetNode{entTargetNode = e});
            //         // em.SetComponentData(e, new CpAttitude{ attitude = Random.Range(-1, 1f), });

            //         // lst_tar.Add(e);
            //     }
            // }
        }
    }
}