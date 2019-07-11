using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Unity.Entities;
using Unity.Collections;

using Random = UnityEngine.Random;

namespace MH
{
    public class IterAlgTest : MonoBehaviour
    {
        public enum EAlg
        { 
            JobOnly,
            A1,
        }

        public EAlg _eAlg = EAlg.JobOnly;
        public int _pop_cnt = 1000;
        public int _tar_cnt = 1000;
        public int _rel_per_pop = 100;
        public int _max_iter = 30;

        private Alg_JobOnly _alg_jo = null;
        private Alg_A1 _alg_a1 = null;

        void Start()
        {
            switch (_eAlg)
            {
                case EAlg.JobOnly: _Start_JobOnly(); break;
                case EAlg.A1: _Start_A1(); break;
            }
        }

        void Update()
        {
            switch( _eAlg )
            {
                case EAlg.JobOnly: _alg_jo.DoUpdate(); break;
                case EAlg.A1: _alg_a1.DoUpdate(); break;
            }
        }

        void OnGUI()
        {
            switch( _eAlg )
            {
                case EAlg.JobOnly: _alg_jo.OnGUI(); break;
                case EAlg.A1: _alg_a1.OnGUI(); break;
            }
        }

        private void _Start_A1()
        {
            _alg_a1 = new Alg_A1(_pop_cnt, _tar_cnt, _rel_per_pop, _max_iter);
            
        }

        private void _Start_JobOnly()
        {
            _alg_jo = new Alg_JobOnly(_pop_cnt, _tar_cnt, _rel_per_pop, _max_iter);
        }
    }

//////////////////////////////////////////////////////////////////////////////////////////////////


}