using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Entities;
using Unity.Collections;

using Random = UnityEngine.Random;

namespace MH
{
    [Serializable]
    public struct EntPair : IEquatable<EntPair>
    {
        public Entity a;
        public Entity b;

        public EntPair(Entity a, Entity b)
        {
            this.a = a;
            this.b = b;
        }

        public override int GetHashCode()
        {
            return (a.Index<<16) + (b.Index & 0xFFFF);
        }

        public bool Equals(EntPair other)
        {
            return a == other.a && b == other.b;
        }
    }

    public class AttRelCtrl : MonoBehaviour
    {
        public int pop_cnt = 1000;
        public int tar_cnt = 1000;
        public int rel_per_pop = 300;
        public int max_iter = 30;
        [Range(0.05f, 0.3f)] 
        public float max_relation_strength = 0.2f;

        void Start()
        {
            var w = World.Active;

            var sysUpdateAtt = w.GetOrCreateSystem<SysUpdateAtt>();
            sysUpdateAtt.max_iter = max_iter;

            _Init();
        }

        void OnGUI()
        {
            float fps = 1f / Time.deltaTime;
            GUILayout.Label($"FPS: {fps:F1}");
        }

        private void _Init()
        {
            var em = World.Active.EntityManager;
            
            var w = World.Active;
            var entmgr = w.EntityManager;
            var arche_pop = entmgr.CreateArchetype();
            var arche_tar = entmgr.CreateArchetype(typeof(CpOwner), typeof(CpAttitude));
            var arche_rel = entmgr.CreateArchetype(typeof(CpOwner), typeof(CpRelation));

            var lst_pop = new List<Entity>();
            var lst_tar = new List<Entity>();

            for(int i=0; i<pop_cnt; ++i)
            {
                var entPop = em.CreateEntity(arche_pop);
                lst_pop.Add(entPop);

                //---------init all (mindimage of tar in mind of pop)---------//
                lst_tar.Clear();
                for(int j=0; j<tar_cnt; ++j)
                {
                    var e = em.CreateEntity(arche_tar); lst_tar.Add(e);
                    em.SetSharedComponentData(e, new CpOwner{owner = entPop});
                }

                //---------init all relations in mind for this pop---------//
                for(int j=0; j<rel_per_pop; ++j) 
                {
                    var str = Random.Range(-max_relation_strength, max_relation_strength);
                    var a = lst_tar[ Random.Range(0, lst_tar.Count) ];
                    var b = lst_tar[ Random.Range(0, lst_tar.Count) ];

                    var rel = em.CreateEntity(arche_rel);
                    em.SetSharedComponentData(rel, new CpOwner{owner = entPop});
                    em.SetComponentData(rel, new CpRelation{ from=a, to = b, strength = str, trans = 0,});

                    rel = em.CreateEntity(arche_rel);
                    em.SetSharedComponentData(rel, new CpOwner{owner = entPop});
                    em.SetComponentData(rel, new CpRelation{ from=b, to = a, strength = str, trans = 0,});
                }

                foreach(var entTar in lst_tar) //init attitude for tar in the mind of pop
                {
                    em.SetComponentData(entTar, new CpAttitude{ attitude = Random.Range(-1, 1f), });
                }
            }
        }

        void OnDestroy()
        {
        }
    }
}