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

        public delegate void DelEndOfFrame(); 
        public static event DelEndOfFrame evtEOF;

        void Start()
        {
            StartCoroutine(_CoEndOfFrame());
            _InitSys();
            _InitWorld();
        }

        private IEnumerator _CoEndOfFrame()
        {
            var wait = new WaitForEndOfFrame();
            while(true)
            {
                yield return wait;
                evtEOF?.Invoke();
            }
        }

        void Update()
        {
            #if !UNITY_EDITOR
            if( Time.deltaTime > 0.015f )
            {
                Debug.Log($"Frame{Time.frameCount} : {Time.deltaTime}");
            }
            #endif
        }

        void OnDestroy()
        {
        }

        private string _perc_mod_target_likes = "0.25";
        private string _perc_mod_target_link_strength = "0.25";
        void OnGUI()
        {
            float fps = 1f / Time.deltaTime;
            GUILayout.Label($"FPS: {fps:F1}");

            _GUI_Float_Execute("% target like mod:", ref _perc_mod_target_likes, _Mod_Target_Likes);
            _GUI_Float_Execute("% target link strength mod:", ref _perc_mod_target_link_strength, _Mod_Link_Str);
        }

        private void _GUI_Float_Execute(string label, ref string strfield, Action<float> cb, float minval = 0f, float maxval = 1f)
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(label, GUILayout.Width(200f));
                strfield = GUILayout.TextField(strfield, GUILayout.Width(200f));
                bool valid = float.TryParse(strfield, out float fVal) && (minval < fVal && fVal < maxval);
                GUI.enabled = valid;
                if (GUILayout.Button("Execute"))
                {
                    cb(fVal);
                }
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
        }

        private void _Mod_Target_Likes(float percentage)
        {
            var em = World.Active.EntityManager;
            var query = em.CreateEntityQuery(ComponentType.ReadOnly<CpAttitude>());
            var arr_ent = query.ToEntityArray(Allocator.TempJob);
            var arr_att = query.ToComponentDataArray<CpAttitude>(Allocator.TempJob);
            var att_mods = _sysModData.mod_att;
            for(int i=0; i<arr_ent.Length; ++i)
            {            
                var ent = arr_ent[i];
                var cpAtt = arr_att[i];
                if (Random.value < percentage)
                {
                    att_mods.Add(new ModAttitude(ent, Random.Range(-0.5f, 0.5f)));
                }
            }

            arr_att.Dispose();
            arr_ent.Dispose();
        }

        private void _Mod_Link_Str(float percentage)
        {
            var em = World.Active.EntityManager;
            var query = em.CreateEntityQuery(ComponentType.ReadOnly<CpRelation>());
            var arr_ent = query.ToEntityArray(Allocator.TempJob);
            var arr_rel = query.ToComponentDataArray<CpRelation>(Allocator.TempJob);
            var link_mods = _sysModData.mod_link;
            for(int i=0; i<arr_ent.Length; ++i)
            {
                var ent = arr_ent[i];
                var cpRel = arr_rel[i];
                if( Random.value < percentage )
                {
                    link_mods.Add(new ModLink(ent, Random.Range(0.05f, 0.2f) * Mathf.Sign(Random.Range(-1f, 1f))));
                }
            }
            arr_rel.Dispose();
            arr_ent.Dispose();
        }

        private SysModData _sysModData;
        private void _InitSys()
        {
            var w = World.Active;
            _sysModData = w.GetOrCreateSystem<SysModData>();
            _sysModData.max_relation_strength = max_relation_strength;
            var sysUpdateAtt = w.GetOrCreateSystem<SysUpdateAtt>();
            sysUpdateAtt.max_iter = max_iter;
            sysUpdateAtt.max_relation_strength = max_relation_strength;

            var simSysGroup = w.GetExistingSystem<SimulationSystemGroup>();
            simSysGroup.AddSystemToUpdateList(_sysModData);
            simSysGroup.AddSystemToUpdateList(sysUpdateAtt);
        }

        private void _InitWorld()
        {
            var w = World.Active;
            var em = w.EntityManager;

            // set singleton
            em.CreateEntity(typeof(CpSysSchedule));
            var query_schedule = em.CreateEntityQuery(typeof(CpSysSchedule));
            query_schedule.SetSingleton<CpSysSchedule>(new CpSysSchedule{
                interval = 2f,
                lastUpdateTime = -1000,
                sysUpdateRunning = false,
            });

            // archetypes
            var arche_pop = em.CreateArchetype(typeof(CpTargetNodeBuf));
            var arche_tar = em.CreateArchetype(typeof(CpOwner), typeof(CpAttitude), typeof(CpMindImageTargetNode), typeof(CpRelationBuf));
            var arche_rel = em.CreateArchetype(typeof(CpOwner), typeof(CpRelation), typeof(CpIsDirty));

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
                    var e = em.CreateEntity(arche_tar); 
                    
                    em.SetComponentData(e, new CpOwner{entOwner = entPop});
                    em.SetComponentData(e, new CpMindImageTargetNode{entTargetNode = e});
                    em.SetComponentData(e, new CpAttitude{ attitude = Random.Range(-1, 1f), });

                    lst_tar.Add(e);
                }

                //---------add targetNodeBuf data---------//
                var targetBuf = em.GetBuffer<CpTargetNodeBuf>(entPop); // <== WARNING: any structural change will invalidate the buffer pointer!!
                foreach(var eTar in lst_tar)
                    targetBuf.Add( new CpTargetNodeBuf{target = eTar});

                //---------init all relations in mind for this pop---------//
                for(int j=0; j<rel_per_pop; ++j) 
                {
                    var str = Random.Range(-max_relation_strength, max_relation_strength);
                    var a = lst_tar[ Random.Range(0, lst_tar.Count) ];
                    var b = lst_tar[ Random.Range(0, lst_tar.Count) ];

                    var rel = em.CreateEntity(arche_rel);
                    em.SetComponentData(rel, new CpOwner{entOwner = entPop});
                    em.SetComponentData(rel, new CpRelation{from=a, to = b, strength = str, trans = 0,});
                    em.SetComponentData(rel, new CpIsDirty{dirty = false,});
                    var aRelBuf = em.GetBuffer<CpRelationBuf>(a);
                    aRelBuf.Add(new CpRelationBuf{entRelation = rel});

                    rel = em.CreateEntity(arche_rel);
                    em.SetComponentData(rel, new CpOwner{entOwner = entPop});
                    em.SetComponentData(rel, new CpRelation{ from=b, to = a, strength = str, trans = 0,});
                    em.SetComponentData(rel, new CpIsDirty{dirty = false,});
                    var bRelBuf = em.GetBuffer<CpRelationBuf>(b);
                    bRelBuf.Add(new CpRelationBuf{entRelation = rel});
                }
            }
        }

        
    }
}