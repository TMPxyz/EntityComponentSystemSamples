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

    [DisableAutoCreation]
    public class SysMapECS : JobComponentSystem
    {
        private NativeHashMap<Entity, int> _map;
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

        [BurstCompile]
        public struct ClearJob : IJob
        {
            [WriteOnly] public NativeHashMap<Entity, int> m;

            public void Execute()
            {
                m.Clear();
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            _map = new NativeHashMap<Entity, int>((int)2e6, Allocator.Persistent);
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
            Debug.Log("StartRunning");
        }

        protected override void OnStopRunning()
        {
            Debug.Log("StopRunning");
            base.OnStopRunning();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) //1,000,000 insert = 5ms
        {
            var job = new AddEntryJob{ m = _map.ToConcurrent() }.Schedule(this, inputDeps);
            var handle = new ClearJob { m = _map }.Schedule(job);
            return handle;
        }
    }
}