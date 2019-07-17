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
    public class SysChunk : JobComponentSystem
    {
        public struct Stat
        {
            public int chunkIndex, chunkLen, firstEntIdx;
        }

        EntityQuery query_A, query_B;
        NativeHashMap<int, Stat> _map;

        JobHandle _waitHandle;

        protected override void OnCreate()
        {
            query_A = GetEntityQuery( ComponentType.ReadOnly<CpA>(), ComponentType.ReadOnly<CpB>() );
            query_B = GetEntityQuery( ComponentType.ReadOnly<CpC>(), ComponentType.ReadOnly<CpD>(), ComponentType.ReadOnly<CpE>() );
            _map = new NativeHashMap<int, Stat>(10000, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            _map.Dispose();
        }

        public struct ChunkJob : IJobChunk
        {
            public int indexMod;
            [WriteOnly] public NativeHashMap<int, Stat>.Concurrent cmap;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                cmap.TryAdd(chunkIndex + indexMod, new Stat{chunkIndex=chunkIndex, chunkLen=chunk.Count, firstEntIdx=firstEntityIndex});
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            _map.Clear();
            var ha = new ChunkJob{indexMod=0, cmap = _map.ToConcurrent()}.Schedule(query_A, inputDeps);
            var hb = new ChunkJob{indexMod=1000, cmap = _map.ToConcurrent()}.Schedule(query_B, ha);
            _waitHandle = hb;
            return _waitHandle;
        }

        public void OutputStat()
        {
            _waitHandle.Complete();

            StringBuilder bld = new StringBuilder();
            var arr = _map.GetValueArray(Allocator.Temp);
            for(int i=0; i<_map.Length; ++i)
            {
                var x = arr[i];
                bld.AppendLine($"{x.chunkIndex} {x.chunkLen} {x.firstEntIdx}");
            }
            arr.Dispose();
            Debug.Log(bld.ToString());
        }
    }
}