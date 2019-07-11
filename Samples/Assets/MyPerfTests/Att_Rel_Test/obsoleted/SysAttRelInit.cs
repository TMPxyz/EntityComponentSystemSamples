// using UnityEngine;
// using System;
// using System.Collections;
// using System.Collections.Generic;

// using Unity.Entities;
// using Unity.Collections;
// using Unity.Burst;

// using Random = UnityEngine.Random;

// namespace MH
// {
//     public class SysAttRelInit : ComponentSystem
//     {
//         public int pop_cnt = 1000;
//         public int tar_cnt = 1000;
//         public int rel_per_pop = 300;

//         EntityArchetype _arche_pop;
//         EntityArchetype _arche_tar;
//         EntityArchetype _arche_rel;
//         EntityArchetype _arche_attupdate;

//         List<Entity> _lst_pop = new List<Entity>();
//         List<Entity> _lst_tar = new List<Entity>();

//         protected override void OnCreate()
//         {
//             var w = World.Active;
//             var entmgr = w.EntityManager;
//             _arche_pop = entmgr.CreateArchetype();
//             _arche_tar = entmgr.CreateArchetype();
//             _arche_rel = entmgr.CreateArchetype(typeof(CpRelOwner), typeof(CpRelFrom), typeof(CpRelData));
//             _arche_attupdate = entmgr.CreateArchetype(typeof(CpAttUpdate));
//         }

//         protected override void OnUpdate()
//         {
//             _lst_pop.Clear();
//             _lst_tar.Clear();

//             for(int i=0; i<pop_cnt; ++i)
//             {
//                 var e = PostUpdateCommands.CreateEntity(_arche_pop);
//                 _lst_pop.Add(e);
//             }

//             for(int i=0; i<tar_cnt; ++i)
//             {
//                 var e = PostUpdateCommands.CreateEntity(_arche_tar);
//                 _lst_tar.Add(e);
//             }

//             foreach(var entPop in _lst_pop)
//             {
//                 for(int j=0; j<rel_per_pop; ++j) //init relations for this pop
//                 {
//                     var str = Random.Range(-0.15f, 0.15f);
//                     var a = _lst_tar[ Random.Range(0, _lst_tar.Count) ];
//                     var b = _lst_tar[ Random.Range(0, _lst_tar.Count) ];

//                     var rel = PostUpdateCommands.CreateEntity(_arche_rel);
//                     PostUpdateCommands.SetSharedComponent(rel, new CpRelOwner{owner = entPop});
//                     PostUpdateCommands.SetSharedComponent(rel, new CpRelFrom{from = a});
//                     PostUpdateCommands.SetComponent(rel, new CpRelData{ to = b, strength = str, trans = 0,});

//                     rel = PostUpdateCommands.CreateEntity(_arche_rel);
//                     PostUpdateCommands.SetSharedComponent(rel, new CpRelOwner{owner = entPop});
//                     PostUpdateCommands.SetSharedComponent(rel, new CpRelFrom{from = b});
//                     PostUpdateCommands.SetComponent(rel, new CpRelData{ to = a, strength = str, trans = 0,});
//                 }

//                 foreach(var entTar in _lst_tar) //init attitude for <pop, tar>
//                 {
//                     var att = PostUpdateCommands.CreateEntity(_arche_attupdate);
//                     PostUpdateCommands.SetComponent(att, new CpAttUpdate{
//                         from = _lst_pop[Random.Range(0, _lst_pop.Count)],
//                         to = _lst_tar[Random.Range(0, _lst_tar.Count)],
//                         attitude = Random.Range(-1, 1f),
//                     });
//                 }
//             }

//             this.Enabled = false;
//         }
//     }
// }