// using UnityEngine;
// using System;
// using System.Collections;
// using System.Collections.Generic;
// using Unity.Entities;
// using Unity.Jobs;
// using Unity.Collections;
// using Unity.Burst;

// namespace MH
// {
//     ///<summary>
//     /// used to directly set the value for attitude dictionary
//     ///</summary>
//     public class SysSetAttMap : JobComponentSystem
//     {
//         public NativeHashMap<EntPair, float> map;

//         EntityCommandBuffer ecb;

//         protected override void OnCreate()
//         {
//             ecb = new EntityCommandBuffer(Allocator.Persistent);
//         }

//         protected override void OnDestroy()
//         {
//             ecb.Dispose();
//         }

//         [BurstCompile]
//         public struct AddMapJob : IJobForEachWithEntity<CpAttUpdate>
//         {
//             public NativeHashMap<EntPair, float>.Concurrent m;
//             [ReadOnly] public EntityCommandBuffer postUpdateCommands;

//             public void Execute(Entity entity, int index, ref CpAttUpdate cp)
//             {
//                 m.TryAdd( new EntPair{a=cp.from, b=cp.to}, cp.attitude);
//                 postUpdateCommands.DestroyEntity(entity);
//             }
//         }

//         protected override JobHandle OnUpdate(JobHandle inputDeps)
//         {
//             var bufferSys = World.Active.GetOrCreateSystem<EntityCommandBufferSystem>();
//             var job = new AddMapJob{
//                 m = map.ToConcurrent(),
//                 postUpdateCommands = ecb,
//             };
//             return job.Schedule(this, inputDeps);
//         }
//     }
// }