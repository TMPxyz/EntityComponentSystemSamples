using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using System.Text;

namespace MH
{
    public class SysChunk2 : JobComponentSystem
    {
        public struct Stat
        {
            public int chunkIndex, chunkLen, firstEntIdx;
        }

        EntityQuery query;
        NativeQueue<Stat> _queue;

        JobHandle _waitHandle;

        protected override void OnCreate()
        {
            query = GetEntityQuery( ComponentType.ReadOnly<CpC>() );
            _queue = new NativeQueue<Stat>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            _queue.Dispose();
        }

        public struct ChunkJob : IJobChunk
        {
            [WriteOnly] public NativeQueue<Stat>.Concurrent cq;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                cq.Enqueue(new Stat{chunkIndex=chunkIndex, chunkLen=chunk.Count, firstEntIdx=firstEntityIndex});
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            _queue.Clear();
            var ha = new ChunkJob{cq = _queue.ToConcurrent()}.Schedule(query, inputDeps);
            _waitHandle = ha;
            return _waitHandle;
        }

        public void OutputStat()
        {
            _waitHandle.Complete();
            StringBuilder bld = new StringBuilder();
            var cnt = _queue.Count;
            for(int i=0; i<cnt; ++i)
            {
                var x = _queue.Dequeue();
                bld.AppendLine($"{x.chunkIndex} {x.chunkLen} {x.firstEntIdx}");
            }
            Debug.Log(bld.ToString());
        }
    }
}