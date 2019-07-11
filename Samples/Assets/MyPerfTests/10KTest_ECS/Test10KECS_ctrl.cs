using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Entities;

using Random = UnityEngine.Random;

namespace MH
{
    public class Test10KECS_ctrl : MonoBehaviour
    {
        public int _item_cnt = 10000;
        public int _loop_cnt = 10000;

        void Start()
        {
            var w = World.Active;
            w.CreateSystem<CalcSystem>();

            var entmgr = w.EntityManager;
            for(int i=0; i<_item_cnt; ++i)
            {
                var e = entmgr.CreateEntity();
                entmgr.AddComponentData(e, new CalcComponent{ 
                    v = Random.Range(0, 100), 
                    loopcnt = _loop_cnt 
                });
            }
        }        
    }
}