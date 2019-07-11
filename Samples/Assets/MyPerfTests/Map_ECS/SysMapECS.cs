using System;
using UnityEngine;
using System.Collections.Generic;

using Unity.Entities;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;

namespace MH
{
    [Serializable]
    public struct CpMap : IComponentData
    {
        public int v;
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    // [DisableAutoCreation]
    public class SysMapECS : JobComponentSystem
    {
        private NativeHashMap<Entity, int> _map;
        private NativeHashMap<Entity, int>.Concurrent _cm;
        private MapECSCtrl _ctrl;

        [BurstCompile]
        public struct AddEntryJob : IJobForEachWithEntity<CpMap>
        {
            [WriteOnly] public NativeHashMap<Entity, int>.Concurrent m;

            public void Execute(Entity entity, int index, ref CpMap cp)
            {
                m.TryAdd(entity, cp.v);
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            _map = new NativeHashMap<Entity, int>((int)3e6, Allocator.Persistent);
            _cm = _map.ToConcurrent();
            Debug.Log("OnCreate");
        }

        protected override void OnDestroy()
        {
            Debug.Log("OnDestroy");
            _map.Dispose();
            base.OnDestroy();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            _map.Clear();
            Debug.Log("StartRunning");
        }

        protected override void OnStopRunning()
        {
            Debug.Log("StopRunning");
            base.OnStopRunning();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) //1,000,000 insert = 5ms
        {
            var job = new AddEntryJob{ m = _cm };
            var handle = job.Schedule(this, inputDeps);
            return handle;
        }
    }
}