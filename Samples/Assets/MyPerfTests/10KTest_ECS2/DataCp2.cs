// using UnityEngine;
// using System;
// using System.Collections;
// using System.Collections.Generic;

// using Unity.Entities;
// using Unity.Jobs;
// using Unity.Collections;
// using Unity.Burst;
// using Unity.Mathematics;

// using Random = UnityEngine.Random;

// namespace MH
// {
//     [Serializable]
//     public struct DataCp2 : IComponentData
//     {
//         public NativeHashMap<Entity, float> map;
//     }

//     public class Sys2 : JobComponentSystem
//     {
//         NativeArray<Entity> _allMatchEntity;
//         EntityQuery _query;

//         [BurstCompile]
//         struct ModMapJob : IJobForEach<DataCp2>
//         {
//             public NativeArray<Entity> match_entities;
//             public void Execute(ref DataCp2 cp)
//             {
//                 int ent_cnt = match_entities.Length;
//                 if (cp.map.Length < 100)
//                 {
//                     int gen_cnt = Random.Range(1, 10);
//                     for(int i=0; i<gen_cnt; ++i)
//                     {
//                         var e = match_entities[ Random.Range(0, ent_cnt) ];
//                         cp.map.TryAdd(e, (float)i);
//                     }
//                 }
//                 else
//                 {
//                     var ks = cp.map.GetKeyArray(Allocator.Temp);
//                     cp.map.Remove(ks[0]);
//                 }
//             }
//         }

//         protected override void OnCreate()
//         {
//             base.OnCreate();
//             _query = World.Active.EntityManager.CreateEntityQuery(typeof(DataCp2));
//         }

//         protected override void OnDestroy()
//         {
//             _query.Dispose();
//             base.OnDestroy();
//         }

//         protected override void OnStartRunning()
//         {
//             base.OnStartRunning();
//             _allMatchEntity = _query.ToEntityArray(Allocator.TempJob);
//         }

//         protected override void OnStopRunning()
//         {
//             _allMatchEntity.Dispose();
//             base.OnStopRunning();
//         }

//         protected override JobHandle OnUpdate(JobHandle inputDeps)
//         {
//             var job = new ModMapJob{ match_entities = _allMatchEntity };
//             return job.Schedule(this, inputDeps);
//         }
//     }
// }