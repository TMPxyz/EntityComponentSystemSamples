using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;

namespace MH
{
    [DisableAutoCreation]
    public class SysModData : JobComponentSystem
    {
        public float max_relation_strength;

        EntityCommandBufferSystem _cmdBufferSys;
        int _lastRunFrame;

        //---------containers---------//
        public NativeList<ModAttitude> mod_att; //access by ctrl
        public NativeList<ModLink> mod_link; //access by ctrl

        protected override void OnCreate()
        {
            Debug.Log("SysModData.OnCreate");
            // _cmdBufferSys = World.Active.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
            _cmdBufferSys = World.Active.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            
            mod_att = new NativeList<ModAttitude>(Allocator.Persistent);
            mod_link = new NativeList<ModLink>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            Debug.Log("SysModData.OnDestroy");
            mod_att.Dispose();
            mod_link.Dispose();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            AttRelCtrl.evtEOF += _OnEndOfFrame;
            Debug.Log("SysModData.OnStartRunning");
        }

        protected override void OnStopRunning()
        {
            Debug.Log("SysModData.OnStopRunning");
            AttRelCtrl.evtEOF -= _OnEndOfFrame;
            base.OnStartRunning();
        }

        private void _OnEndOfFrame()
        {
            if( _lastRunFrame == Time.frameCount )
            {
                // mod_att.Dispose();
                // mod_link.Dispose();
            }
        }

        [BurstCompile]
        public struct ModAttJob : IJobParallelFor 
        {
            [ReadOnly] public NativeList<ModAttitude> mods;
            [ReadOnly] public BufferFromEntity<CpRelationBuf> ro_relation_buf;
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<CpAttitude> rw_att;
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<CpIsDirty> rw_isDirty;

            public void Execute(int index)
            {
                var aMod = mods[index];
                var entNode = aMod.entMindImageTargetNode;

                // modify the node's attitude field
                var cpAtt = rw_att[entNode];
                cpAtt.attitude = math.clamp(cpAtt.attitude + aMod.att_mod, -1f, 1f);
                rw_att[entNode] = cpAtt;

                //set dirty on each link from this node
                var cpLstRel = ro_relation_buf[entNode];
                int len_relation = cpLstRel.Length;
                for (int i=0; i<len_relation; ++i)
                {
                    var entRel = cpLstRel[i].entRelation;
                    rw_isDirty[entRel] = new CpIsDirty{dirty = true};
                }
            }
        }

        [BurstCompile]
        public struct ModLinkJob : IJobParallelFor
        {
            [ReadOnly] 
            public NativeList<ModLink> mods;
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<CpRelation> rw_rel;
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<CpIsDirty> rw_isDirty;
            public float max_relation_strength;

            public void Execute(int index)
            {
                var aMod = mods[index];
                var entLink = aMod.entLink;

                // mod link's strength
                var cpRel = rw_rel[entLink];
                cpRel.strength = math.clamp(cpRel.strength + aMod.str_mod, -max_relation_strength, max_relation_strength);
                rw_rel[entLink] = cpRel;

                rw_isDirty[entLink] = new CpIsDirty{dirty=true};
            }
        }

        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            //---------check condition and set sysSchedule---------//
            var entSchedule = GetSingletonEntity<CpSysSchedule>();
            var cpSchedule = GetSingleton<CpSysSchedule>();

            if (!cpSchedule.CanRunSysModData())
                return inputDeps;
            
            if( !mod_att.IsCreated && !mod_link.IsCreated) //check if data is available
                return inputDeps;

            cpSchedule.Finish_SysModData();
            _cmdBufferSys.CreateCommandBuffer().SetComponent(entSchedule, cpSchedule);

            //---------start---------//
            _lastRunFrame = Time.frameCount;

            //---------modifications---------//
            var jobModAtt = new ModAttJob{
                mods = mod_att,
                ro_relation_buf = GetBufferFromEntity<CpRelationBuf>(isReadOnly:true),
                rw_att = GetComponentDataFromEntity<CpAttitude>(isReadOnly:false),
                rw_isDirty = GetComponentDataFromEntity<CpIsDirty>(isReadOnly:false),
            }.Schedule(mod_att.Length, 16, inputDeps);
            var jobClearAtt = mod_att.ClearWithJob(jobModAtt);

            var jobModLink = new ModLinkJob{
                mods = mod_link,
                rw_rel = GetComponentDataFromEntity<CpRelation>(isReadOnly:false),
                rw_isDirty = GetComponentDataFromEntity<CpIsDirty>(isReadOnly:false),
                max_relation_strength = this.max_relation_strength,
            }.Schedule(mod_link.Length, 16, jobModAtt);
            var jobClearLink = mod_link.ClearWithJob(jobModLink);

            //---------finish---------//
            var final_handle = JobHandle.CombineDependencies(jobClearAtt, jobClearLink);
            return final_handle;
        }
    }
}