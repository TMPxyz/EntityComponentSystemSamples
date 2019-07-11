using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;

using Random = UnityEngine.Random;

namespace MH
{
    public class Alg_A1
    {
        private int _pop_cnt, _tar_cnt, _rel_per_pop, _max_iter;

        public Alg_A1(int pop_cnt, int tar_cnt, int rel_per_pop, int max_iter)
        {
            (_pop_cnt, _tar_cnt, _rel_per_pop, _max_iter) = (pop_cnt, tar_cnt, rel_per_pop, max_iter);

        }

        public void DoUpdate()
        {

        }

        public void OnGUI()
        {
            throw new NotImplementedException();
        }
    }
}