// using UnityEngine;
// using System;
// using System.Collections;
// using System.Collections.Generic;
// using Unity.Entities;
// using Unity.Jobs;
// using Unity.Collections;

// namespace MH
// {
//     public class SysUpdRelation : ComponentSystem
//     {
//         EntityQuery _updateGroup;
//         EntityQuery _origGroup;

//         protected override void OnCreate()
//         {
//             _updateGroup = GetEntityQuery(new EntityQueryDesc{
//                 All = new ComponentType[] { 
//                     ComponentType.ReadOnly<CpRelDataUpdate>(), 
//                     ComponentType.ReadOnly<CpRelOwner>(), ComponentType.ReadOnly<CpRelFrom>(), ComponentType.ReadOnly<CpRelData>() 
//                 },
//             });

//             _origGroup = GetEntityQuery(new EntityQueryDesc{
//                 All = new ComponentType[] { ComponentType.ReadOnly<CpRelOwner>(), ComponentType.ReadOnly<CpRelFrom>(), ComponentType.ReadWrite<CpRelData>() },
//                 None = new ComponentType[] { typeof(CpRelDataUpdate) },
//             });
//         }

//         protected override void OnUpdate()
//         {
//             var entmgr = World.Active.EntityManager;
//             var update_entities = _updateGroup.ToEntityArray(Allocator.TempJob);
//             foreach(var upd_ent in update_entities)
//             {
//                 var cp_owner = entmgr.GetSharedComponentData<CpRelOwner>(upd_ent);
//                 var cp_from = entmgr.GetSharedComponentData<CpRelFrom>(upd_ent);
//                 var cp_data  = entmgr.GetComponentData<CpRelData>(upd_ent);

//                 _origGroup.SetFilter(
//                     new CpRelOwner{owner = cp_owner.owner}, 
//                     new CpRelFrom{from = cp_from.from}
//                 );
//                 var orig_entities = _origGroup.ToEntityArray(Allocator.Temp);
//                 bool found = false;
//                 foreach(var orig_ent in orig_entities)
//                 {
//                     var cp_orig_data = entmgr.GetComponentData<CpRelData>(orig_ent);
//                     if (cp_data.to == cp_orig_data.to)
//                     {
//                         var new_data = cp_orig_data;
//                         new_data.strength = cp_data.strength;
//                         PostUpdateCommands.SetComponent(orig_ent, new_data);
//                         PostUpdateCommands.DestroyEntity(upd_ent);
//                         found = true;
//                         break;
//                     }
//                 }
//                 if( ! found )
//                 {
//                     PostUpdateCommands.RemoveComponent(upd_ent, typeof(CpRelDataUpdate));
//                 }
//                 orig_entities.Dispose();
//             }
//             update_entities.Dispose();
//         }
//     }
// }