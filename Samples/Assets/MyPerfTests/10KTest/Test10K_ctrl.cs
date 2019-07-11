using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Profiling;

using Random = UnityEngine.Random;
using Unity.Collections;
using Unity.Jobs;
using System.Threading;

namespace MH
{
    public class Test10K_ctrl : MonoBehaviour
    {
        const int ITEM_CNT = 10000;
        const int RUN_LOOP = 100;
        const int ACCESS_CNT = ITEM_CNT;
        const int CORE = 8;
        public enum EMode {DUMB, NAIVE, PRE_ARRAY, JOB_PRE_ARRAY, THREADED_PRE_ARRAY, NAIVE_JOB, };

        public EMode _mode = EMode.NAIVE;
        public int _submode = 0;
        public int _dumb_loopcnt = 10000000;

        private List<Test10K> _lst = new List<Test10K>();
        // public List<Test10K> lst => _lst;

        private int[][] _randIndices = new int[RUN_LOOP][];
        private int[] _indices = null;

        private int[] _preArray = new int[ITEM_CNT];
        private NativeArray<int> _native_prearray; //= new NativeArray<int>(ITEM_CNT, Allocator.Persistent);
        private NativeArray<int> _native_indices;// = new NativeArray<int>(ITEM_CNT, Allocator.Persistent);

        private ManualResetEvent[] _waitEvts = new ManualResetEvent[CORE];

        void Awake()
        {
#region "GameObjects"
            _lst.Clear();
            for(int i=0; i<ITEM_CNT; ++i)
            {
                var go = new GameObject();
                var comp = go.AddComponent(typeof(Test10K));
                go.hideFlags = HideFlags.HideAndDontSave;
                _lst.Add((Test10K)comp);
            }
#endregion "GameObjects"

#region "indices"
            for(int i=0; i<RUN_LOOP; ++i)
            {
                _randIndices[i] = Shuffle(Enumerable.Range(0, ITEM_CNT).ToArray());
            }
            _indices = _randIndices[0];
#endregion "indices"

#region "threaded"
            for(int i=0; i<CORE; ++i)
                _waitEvts[i] = new ManualResetEvent(false);
            _cb_thread_workitem = _Thread_WorkItem;
#endregion "threaded"

#region "persist native_array"
            _native_prearray = new NativeArray<int>(ITEM_CNT, Allocator.Persistent);
            _native_indices = new NativeArray<int>(ITEM_CNT, Allocator.Persistent);
#endregion "persist native_array"
        }

        void OnDestroy()
        {
            _native_prearray.Dispose();
            _native_indices.Dispose();
        }

        public static int[] Shuffle(int[] list)  
        {  
            int n = list.Length;  
            while (n > 1) {  
                n--;  
                int k = Random.Range(0, n+1);
                var value = list[k];  
                list[k] = list[n];  
                list[n] = value;  
            }  
            return list;
        }

        void Update()
        {
            switch(_mode)
            {
                case EMode.DUMB: _Update_Dumb(_submode); break;
                case EMode.NAIVE: _Update_Naive(_submode); break;
                case EMode.PRE_ARRAY: _Update_PreArray(_submode); break;
                case EMode.JOB_PRE_ARRAY: _Update_PreArray_Job(); break;
                case EMode.THREADED_PRE_ARRAY: _Update_Threaded_PreArray(); break;
                case EMode.NAIVE_JOB: break;
            }
        }

        private void _Update_Dumb(int submode)
        {
            var x = 0;
            for(int i=0; i<_dumb_loopcnt; ++i)
            {
                x+=i;
            }
        }

        #region "threaded workitems"
        private void _Thread_WorkItem(object data)
        {
            var evt = (ManualResetEvent)data;

            int v = 0;
            int _loopcnt = Mathf.CeilToInt(RUN_LOOP / (float)CORE);
            for (int i = 0; i < _loopcnt; ++i)
            {
                for (int j = 0; j < ACCESS_CNT; ++j)
                {
                    var idx = _indices[j]; //1.18ms
                    v += _preArray[idx];
                }
            }

            evt.Set();
        }
        private WaitCallback _cb_thread_workitem;

        private void _Update_Threaded_PreArray() //1.96ms
        {
            Profiler.BeginSample("Pre_array");
            for(int i=0; i<ITEM_CNT; ++i)
                _preArray[i] = _lst[i].v;
            Profiler.EndSample();

            Profiler.BeginSample("Prepare threads");
            Array.ForEach(_waitEvts, x => x.Reset());
            for(int i=0; i<CORE; ++i)
                ThreadPool.QueueUserWorkItem(_cb_thread_workitem, _waitEvts[i]); //has GC!
            Profiler.EndSample();

            Profiler.BeginSample("wait threads");
            Array.ForEach( _waitEvts, x => x.WaitOne() ); // WaitHandle.WaitAll has GC!
            Profiler.EndSample();
        }
        #endregion "threaded workitems"

        #region "Burst Jobs"
        [Unity.Burst.BurstCompile]
        public struct PreArray_Job : IJob
        {
            [ReadOnly] public NativeArray<int> _preArray;
            [ReadOnly] public NativeArray<int> _indices;
            public int _loopcnt; 

            public void Execute()
            {
                int v = 0;
                for (int i = 0; i < _loopcnt; ++i)
                {
                    for (int j = 0; j < ACCESS_CNT; ++j)
                    {
                        var idx = _indices[j]; //non-burst 20ms; burst 0.6ms
                        // var idx = j; //non-burst 10ms; burst 0.3ms
                        v += _preArray[idx];
                    }
                }
            }
        }
        
        private void _Update_PreArray_Job() //total 2ms
        {
            Profiler.BeginSample("Pre_array");
            for(int i=0; i<ITEM_CNT; ++i)
                _native_prearray[i] = _lst[i].v;
            _native_indices.CopyFrom(_randIndices[0]);
            Profiler.EndSample();

            Profiler.BeginSample("Prepare jobs");
            var jobs = new NativeArray<JobHandle>(CORE, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for(int i=0; i<CORE; ++i)
            {
                var job = new PreArray_Job{
                    _preArray = _native_prearray,
                    _indices = _native_indices,
                    _loopcnt = Mathf.CeilToInt(RUN_LOOP / (float)CORE),
                };
                jobs[i] = job.Schedule();
            }
            Profiler.EndSample();

            Profiler.BeginSample("Wait jobs");
            JobHandle.CompleteAll(jobs);
            Profiler.EndSample();

            jobs.Dispose();
        }
        #endregion "Burst Jobs"

        void _Update_PreArray(int submode)
        {
            Profiler.BeginSample("Pre_array");
            for(int i=0; i<ITEM_CNT; ++i)
                _preArray[i] = _lst[i].v;
            Profiler.EndSample();

            Profiler.BeginSample("Run");
            switch( submode )
            {
                case 0: _submode0(); break;
                case 1: _submode1(); break;
                case 2: _submode2(); break;
                case 3: _submode3(); break;
            }
            Profiler.EndSample();

            void _submode0()
            {
                int v = 0;
                for (int i = 0; i < RUN_LOOP; ++i)
                {
                    for (int j = 0; j < ACCESS_CNT; ++j)
                    {
                        var idx = 0; //4ms
                        v += _preArray[idx];
                    }
                }
            }

            void _submode1()
            {
                int v = 0;
                for (int i = 0; i < RUN_LOOP; ++i)
                {
                    for (int j = 0; j < ACCESS_CNT; ++j)
                    {
                        var idx = Random.Range(0, ITEM_CNT); //32ms
                        v += _preArray[idx];
                    }
                }
            }

            void _submode2()
            {
                int v = 0;
                for (int i = 0; i < RUN_LOOP; ++i)
                {
                    for (int j = 0; j < ACCESS_CNT; ++j)
                    {
                        var idx = j; //5ms
                        v += _preArray[idx];
                    }
                }
            }

            void _submode3()
            {
                int v = 0;
                for (int i = 0; i < RUN_LOOP; ++i)
                {
                    _indices = _randIndices[i];
                    for (int j = 0; j < ACCESS_CNT; ++j)
                    {
                        var idx = _indices[j]; //6ms
                        v += _preArray[idx];
                    }
                }
            }

        }

        void _Update_Naive(int submode)
        {
            int v = 0;
            switch( submode )
            {
                case 0 : _submode0(); break;
                case 1 : _submode1(); break;
                case 2 : _submode2(); break;
                case 3 : _submode3(); break;
                case 4 : _submode4(); break;
            }

            void _submode0()
            {
                for (int i = 0; i < RUN_LOOP; ++i)
                {
                    for (int j = 0; j < ACCESS_CNT; ++j)
                    {
                        var idx = 0; //12ms
                        v += _lst[idx].v;
                    }
                }
            }

            void _submode1()
            {
                for (int i = 0; i < RUN_LOOP; ++i)
                {
                    for (int j = 0; j < ACCESS_CNT; ++j)
                    {
                        var idx = Random.Range(0, ITEM_CNT); //104ms
                        v += _lst[idx].v;
                    }
                }
            }

            void _submode2()
            {
                for (int i = 0; i < RUN_LOOP; ++i)
                {
                    for (int j = 0; j < ACCESS_CNT; ++j)
                    {
                        var idx = Random.Range(0, 10); //40ms
                        v += _lst[idx].v;
                    }
                }
            }

            void _submode3()
            {
                for (int i = 0; i < RUN_LOOP; ++i)
                {
                    for (int j = 0; j < ACCESS_CNT; ++j)
                    {
                        var idx = j; //50ms 
                        v += _lst[idx].v;
                    }
                }
            }

            void _submode4()
            {
                for (int i = 0; i < RUN_LOOP; ++i)
                {
                    _indices = _randIndices[i];
                    for (int j = 0; j < ACCESS_CNT; ++j)
                    {
                        var idx = _indices[j]; // 52ms
                        v += _lst[idx].v;
                    }
                }
            }
            
        }
    }
}