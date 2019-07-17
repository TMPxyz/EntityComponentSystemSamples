using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Profiling;

namespace MH
{
    // using MAP = NativeHashMap<EntPair, float>;
    // using CMAP = NativeHashMap<EntPair, float>.Concurrent;

    [DisableAutoCreation]
    public class SysUpdateAtt : JobComponentSystem
    {
        public struct OutMod
        {
            public float mod;
            public OutMod(float m){mod = m;}
        }

        const int BUCKET_CNT  = 128;
        const int START_MAP_SIZE = (int)1e5;
        public const float CONVERGE_THRES = 0.001f;

        public int max_iter = 30;
        public float max_relation_strength = 0.2f;
        public float NODE_MOD_THRES => CONVERGE_THRES / max_relation_strength;

        EntityQuery _group_rel;
        // EntityCommandBufferSystem _cmdBufferSys;
        int _lastRunFrame;

        //---------containers---------//
        NativeHashMap<Entity, float> orig_attitudes;
        NativeMultiHashMap<Entity, OutMod> target_mods;
        NativeMultiHashMap<Entity, bool> unique_nodes;
        NativeArray<bool> is_converge_arr;
 
        protected override void OnCreate()
        {
            Debug.Log("SysUpdateAtt.OnCreate");
            _group_rel = GetEntityQuery( new EntityQueryDesc{
                All = new ComponentType[] { typeof(CpOwner), typeof(CpRelation), typeof(CpIsDirty) },
            });

            // _cmdBufferSys = World.Active.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();

            orig_attitudes = new NativeHashMap<Entity, float>(START_MAP_SIZE, Allocator.Persistent);
            target_mods = new NativeMultiHashMap<Entity, OutMod>(START_MAP_SIZE, Allocator.Persistent);
            unique_nodes = new NativeMultiHashMap<Entity, bool>(START_MAP_SIZE, Allocator.Persistent);
            is_converge_arr = new NativeArray<bool>(1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }

        protected override void OnDestroy()
        {
            Debug.Log("SysUpdateAtt.OnDestroy");
            orig_attitudes.Dispose();
            target_mods.Dispose();
            unique_nodes.Dispose();
            is_converge_arr.Dispose();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            AttRelCtrl.evtEOF += _OnEndOfFrame;
            Debug.Log("SysUpdateAtt.OnStartRunning");
        }

        protected override void OnStopRunning()
        {
            Debug.Log("SysUpdateAtt.OnStopRunning");
            AttRelCtrl.evtEOF -= _OnEndOfFrame;
            base.OnStopRunning();
        }

        void _OnEndOfFrame()
        {
            if(Time.frameCount == _lastRunFrame)
            {
                // all container clear is executed by jobs
            }
        }

        [BurstCompile]
        public struct CollectJob : IJobForEachWithEntity<CpRelation, CpIsDirty>
        {
            [ReadOnly] 
            public ComponentDataFromEntity<CpAttitude> acc_att;
            [WriteOnly][NativeDisableParallelForRestriction]
            public NativeArray<bool> is_converge_arr;

            public NativeHashMap<Entity, float>.Concurrent orig_attitudes;
            public NativeMultiHashMap<Entity, OutMod>.Concurrent target_mods;
            public NativeMultiHashMap<Entity, bool>.Concurrent unique_nodes;
            

            public void Execute(Entity entRelation, int index, 
                [ReadOnly] ref CpRelation cpRel, 
                [ReadOnly] ref CpIsDirty cpDirty
            )
            {
                if( !cpDirty.dirty )
                    return;

                var entFrom = cpRel.from;
                var entTo = cpRel.to;
                var from_att = acc_att[entFrom].attitude;
                var to_att = acc_att[entTo].attitude;

                bool is_unique = orig_attitudes.TryAdd(entTo, to_att);
                if( is_unique )
                {
                    unique_nodes.Add(entTo, false);
                }

                var newt = from_att * cpRel.strength;
                var mod = newt - cpRel.trans;

                if( math.abs(mod) > CONVERGE_THRES )
                {
                    cpRel.trans += mod;
                    target_mods.Add(entTo, new OutMod(mod));
                    is_converge_arr[0] = false; //mark not converged
                }
                else
                {
                    cpDirty.dirty = false;
                }
            }
        }

        [BurstCompile]
        public struct CheckConvergeJob : IJob
        {
            [ReadOnly] public NativeArray<bool> is_converge_arr;
            public Entity entSchedule;  
            public ComponentDataFromEntity<CpSysSchedule> acc_schedule;

            public void Execute()
            {
                if( is_converge_arr[0] )
                {
                    var cpSchedule = acc_schedule[entSchedule];
                    cpSchedule.Finish_UpdateAtt();
                    acc_schedule[entSchedule] = cpSchedule;
                }
            }
        }

        // public struct EntityComparer : IComparer<Entity>
        // {
        //     public int Compare(Entity x, Entity y)
        //     {
        //         return x.Index - y.Index;
        //     }
        // }

        [BurstCompile]
        public struct WriteBackJob : IJobNativeMultiHashMapVisitKeyValue<Entity, OutMod>
        {
            [NativeDisableParallelForRestriction]  //allow write to component, must ensure the alg is thread-safe
            public ComponentDataFromEntity<CpAttitude> acc_att;

            public void ExecuteNext(Entity ent_to, OutMod mod)
            {
                var cpAtt = acc_att[ent_to];
                cpAtt.attitude += mod.mod;
                acc_att[ent_to] = cpAtt;
            }
        }

        [BurstCompile]
        public struct SetDirtyByTargetNodes : IJobNativeMultiHashMapVisitKeyValue<Entity, bool>
        {
            [ReadOnly]
            public NativeHashMap<Entity, float> orig_attitude;
            [ReadOnly]
            public BufferFromEntity<CpRelationBuf> accbuf_relation;
            [ReadOnly]
            public ComponentDataFromEntity<CpAttitude> acc_att;
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<CpIsDirty> acc_dirty;

            public float NODE_THRES;

            public void ExecuteNext(Entity ent_target, bool _)
            {
                var start_att = orig_attitude[ent_target];
                var cur_att = acc_att[ent_target].attitude;
                if( math.abs(cur_att - start_att) > NODE_THRES )
                {
                    var rel_buf = accbuf_relation[ent_target];    
                    int len_buf = rel_buf.Length;
                    for(int i=0; i<len_buf; ++i)
                    {
                        var ent_relation = rel_buf[i].entRelation;
                        acc_dirty[ent_relation] = new CpIsDirty{dirty=true};
                    }
                }
            }
        }

        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            //---------check if we need to run---------//
            var entSchedule = GetSingletonEntity<CpSysSchedule>();
            var cpSchedule = GetSingleton<CpSysSchedule>();
            if( ! cpSchedule.CanRunSysUpdateAtt() )
                return inputDeps;

            _lastRunFrame = Time.frameCount;
            var em = World.Active.EntityManager;
            //---------preapre---------//
            is_converge_arr[0] = true;
            int cnt_relation = _group_rel.CalculateLength();

            var access_att_ro = GetComponentDataFromEntity<CpAttitude>(isReadOnly:true);
            var access_att_rw = GetComponentDataFromEntity<CpAttitude>();
            var access_schedule = GetComponentDataFromEntity<CpSysSchedule>();
            var access_dirty = GetComponentDataFromEntity<CpIsDirty>(isReadOnly:false);
            var accbuf_relation = GetBufferFromEntity<CpRelationBuf>(isReadOnly:true);

            //---------run---------//

            //---------collect modifications---------//
            var jobCollect = new CollectJob{ 
                acc_att = access_att_ro, 
                is_converge_arr = is_converge_arr,
                orig_attitudes = orig_attitudes.ToConcurrent(),
                target_mods = target_mods.ToConcurrent(),
                unique_nodes = unique_nodes.ToConcurrent(),
            }.Schedule(this, inputDeps);

            var jobCheckConverge = new CheckConvergeJob{
                is_converge_arr = is_converge_arr,
                entSchedule = entSchedule,
                acc_schedule = access_schedule,
            }.Schedule(jobCollect);

            //---------assign back to attitudes---------//
            var jobWriteBack = new WriteBackJob{ 
                acc_att = access_att_rw,
            }.Schedule(target_mods, 1, jobCollect);

            var jobClearTargetMods = target_mods.ClearWithJob(jobWriteBack);

            var jobSetDirtyNode = new SetDirtyByTargetNodes{
                orig_attitude = orig_attitudes,
                accbuf_relation = accbuf_relation,
                acc_att = access_att_ro,
                acc_dirty = access_dirty,
                NODE_THRES = NODE_MOD_THRES,
            }.Schedule(unique_nodes, 8, jobWriteBack);

            var jobClearUniqueNodes = unique_nodes.ClearWithJob(jobSetDirtyNode);
            var jobClearOrigAttitude = orig_attitudes.ClearWithJob(jobSetDirtyNode);

            var handle_clear = JobHandle.CombineDependencies(jobClearTargetMods, jobClearUniqueNodes, jobClearOrigAttitude);
            return JobHandle.CombineDependencies(handle_clear, jobCheckConverge);
        }

        // private EntityCommandBuffer.Concurrent _GetCmdBuf()
        // {
        //     return _cmdBufferSys.CreateCommandBuffer().ToConcurrent();
        // }
    }
}