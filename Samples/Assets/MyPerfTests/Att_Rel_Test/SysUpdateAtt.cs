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

    [Serializable]
    public struct OutMod
    {
        public Entity target;
        public float mod;
        public OutMod(Entity a, float m){this.target =a; this.mod = m;}
    }
    
    public class SysUpdateAtt : ComponentSystem
    {
        public int max_iter = 30;

        const int BUCKET_CNT  = 48;
        EntityQuery _group_rel;

        protected override void OnCreate()
        {
            _group_rel = GetEntityQuery( new EntityQueryDesc{
                All = new ComponentType[] { typeof(CpOwner), typeof(CpRelation) },
            });

        }

        [BurstCompile]
        public struct CollectModJob : IJobParallelFor
        {
            [ReadOnly] 
            public NativeArray<Entity> arr_relation_ent;
            [NativeDisableParallelForRestriction] 
            public ComponentDataFromEntity<CpRelation> cpget_rel;
            [ReadOnly] 
            public ComponentDataFromEntity<CpAttitude> cpget_att;
            [WriteOnly] 
            public NativeMultiHashMap<int, OutMod>.Concurrent mmap;
            [NativeDisableParallelForRestriction][WriteOnly]
            public NativeArray<bool> arr_converge;

            public void Execute(int index)
            {
                var e = arr_relation_ent[index];
                var cp_relation = cpget_rel[e];

                var e_from = cp_relation.from;
                var e_to = cp_relation.to;
                var str = cp_relation.strength;
                var oldt = cp_relation.trans;
                var att_from = cpget_att[e_from].attitude;
                var newt = att_from * str;
                var mod = newt - oldt;

                if( math.abs(mod) > 0.001f )
                {
                    arr_converge[0] = false;
                    cp_relation.trans = newt;
                    cpget_rel[e] = cp_relation;
                    int iBucket = e_to.Index % BUCKET_CNT;
                    mmap.Add(iBucket, new OutMod(e_to, mod));
                }
            }
        }

        [BurstCompile]
        public struct WriteBackJob : IJobParallelFor
        {
            [ReadOnly] public NativeMultiHashMap<int, OutMod> mmap;

            [NativeDisableParallelForRestriction]  //allow write to component, must ensure the alg is thread-safe
            public ComponentDataFromEntity<CpAttitude> cpget_att;

            public void Execute(int keyIndex) //all values of a index would wholly in a thread
            {
                if( !mmap.TryGetFirstValue(keyIndex, out OutMod mod, out NativeMultiHashMapIterator<int> it) )
                    return;
                
                do
                {
                    var ent_att = mod.target;
                    var cp_att = cpget_att[ent_att];
                    var new_att = cp_att.attitude + mod.mod;
                    cpget_att[ent_att] = new CpAttitude{attitude=new_att};
                }while( mmap.TryGetNextValue(out mod, ref it) );
            }
        }

        protected override void OnUpdate()
        {
            var em = World.Active.EntityManager;

            //---------preapre array and list---------//
            Profiler.BeginSample("ToEntityArray");
            var arr_rel_ent = _group_rel.ToEntityArray(Allocator.TempJob);
            Profiler.EndSample();
            Profiler.BeginSample("GetCpDataFromEntity");
            var arr_converge = new NativeArray<bool>(1, Allocator.TempJob); // use to return result
            var cpget_rel_rw = GetComponentDataFromEntity<CpRelation>();
            var cpget_att_ro = GetComponentDataFromEntity<CpAttitude>(isReadOnly:true);
            var cpget_att_rw = GetComponentDataFromEntity<CpAttitude>();
            Profiler.EndSample();

            int relCnt = arr_rel_ent.Length; //relation count

            Profiler.BeginSample("Init MMap");
            var mmap = new NativeMultiHashMap<int, OutMod>(relCnt, Allocator.TempJob);
            Profiler.EndSample();

            //---------run---------//
            for(int iter = 0; iter < max_iter; ++iter)
            {
                arr_converge[0] = true;
                //---------collect modifications---------//
                var collectJob = new CollectModJob{ arr_relation_ent = arr_rel_ent, cpget_rel = cpget_rel_rw, cpget_att = cpget_att_ro, mmap = mmap.ToConcurrent(), arr_converge = arr_converge };
                var handle_collect = collectJob.Schedule(relCnt, 64);
                handle_collect.Complete();

                //---------assign back to attitudes---------//
                var writeBackJob = new WriteBackJob{ mmap = mmap, cpget_att = cpget_att_rw };
                var handle_write = writeBackJob.Schedule(BUCKET_CNT, 1);
                handle_write.Complete();

                Profiler.BeginSample("Clear MMap");
                mmap.Clear();
                Profiler.EndSample();

                if( arr_converge[0] ) //check convergence
                    break;
            }

            //---------clean---------//
            Profiler.BeginSample("Dispose");
            arr_converge.Dispose();
            mmap.Dispose();
            arr_rel_ent.Dispose();
            Profiler.EndSample();
        }
    }
}