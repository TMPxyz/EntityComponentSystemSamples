using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Random = UnityEngine.Random;

namespace MH
{
    [Serializable]
    public struct CalcComponent : IComponentData
    {
        public int v;
        public int loopcnt;
    }

    public class CalcSystem : JobComponentSystem
    {
        [Unity.Burst.BurstCompile]
        struct AddValueJob : IJobForEach<CalcComponent>
        {
            public int add;
            public void Execute(ref CalcComponent c0)
            {
                for(int i=0; i<c0.loopcnt; ++i)
                    c0.v += add;
            }
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new AddValueJob{ add = Random.Range(1, 100) };
            return job.Schedule(this, inputDeps);
        }
    }
}