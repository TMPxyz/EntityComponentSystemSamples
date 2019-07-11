using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;

namespace MH
{
    public class Alg_JobOnly
    {
        const int CORE = 6;
        private List<Pop> allPop = new List<Pop>();
        private List<Tar> allTar = new List<Tar>();

        private AutoResetEvent[] _allEvents = new AutoResetEvent[CORE];

        private int[] _loopcnt = new int[CORE];
        private int[] _nonConvergeCnt = new int[CORE];
        private int _max_iter;

        public Alg_JobOnly(int pop_cnt, int tar_cnt, int rel_per_pop, int max_iter)
        {
            _max_iter = max_iter;
            for(int i=0; i<tar_cnt; ++i)
                allTar.Add(new Tar{id = i} ); //add tar

            for(int i=0; i<pop_cnt; ++i)
            {
                var new_pop = new Pop();
                allPop.Add(new_pop); // add pop

                new_pop.relations.AddRange(  // add relations
                    Enumerable.Range(0, rel_per_pop).Select( _ => new Rel{
                        a = allTar[ Random.Range(0, allTar.Count) ],
                        b = allTar[ Random.Range(0, allTar.Count) ],
                        strength = Random.Range(-0.2f, 0.2f),
                        tab = 0, 
                        tba = 0,
                    } ) 
                );
            }

            // add events
            for(int i=0; i<CORE; ++i)
            {
                _allEvents[i] = new AutoResetEvent(false);
            }

            Profiler.BeginSample("Prepare");
            allPop.ForEach( x => x.RandomReset(allTar) ); // reset every frame, as the amount of computation drops after first time
            Profiler.EndSample();
        }

        public void OnGUI()
        {
            GUILayout.Label($"LoopCnt:{_loopcnt.Sum()}\nTotalNonConverge:{_nonConvergeCnt.Sum()}");
        }

        public void DoUpdate() //1000pop, 100rel = 550ms / frame
        {
            Array.Clear(_loopcnt, 0, CORE);

            Profiler.BeginSample("Calc");
            var inc = (allPop.Count + CORE - 1)/ CORE;
            var idx = 0;
            for(int i=0; i<CORE; ++i)
            {
                ThreadPool.QueueUserWorkItem(_WorkItem, (idx, Mathf.Min(idx + inc, allPop.Count), _allEvents[i], i, Time.frameCount) );
                idx += inc;
            }
            Array.ForEach( _allEvents, x => x.WaitOne() );
            // WaitHandle.WaitAll(_allEvents);
            Profiler.EndSample();

            // _WorkItem((0, allPop.Count, _allEvents[0], 0)); //single-thread ver. for debugging
        }

        private void _WorkItem(object p)
        {
            const float THRES = 0.001f;

            (var from, var end, var evt, var coreIdx, var fcnt) = (ValueTuple<int, int, AutoResetEvent, int, int>)p;
            for (int i=from; i<end; ++i) 
            { //process a pop
                var pop = allPop[i];
                var atts = pop.attitudes;
                var shadow = pop.shadow;

                bool notConverge = false;
                for(int iter = 0; iter < _max_iter; ++iter)
                {
                    notConverge = false;
                    for(int j=0; j<pop.relations.Count; ++j)
                    {
                        var rel = pop.relations[j];
                        (var a, var b, var oldtab, var oldtba) = (rel.a, rel.b, rel.tab, rel.tba);

                        if( fcnt < 10 )
                        {
                            rel.tab = atts[a] * rel.strength;
                            shadow[b] += rel.tab - oldtab;

                            rel.tba = atts[b] * rel.strength;
                            shadow[a] += rel.tba - oldtba;
                        }

                        pop.relations[j] = rel; //writeback

                        if( Mathf.Abs(rel.tab - oldtab) > THRES || Mathf.Abs(rel.tba - oldtba) > THRES )
                            notConverge = true;

                        _loopcnt[coreIdx]++;
                    }

                    if( !notConverge )
                    {
                        (pop.attitudes, pop.shadow) = (shadow, atts);
                        break;
                    }
                    else
                    {
                        (atts, shadow) = (shadow, atts);
                        shadow.Clear();
                        foreach(var pr in atts)
                            shadow.Add(pr.Key, pr.Value);
                    }
                }

                if(notConverge)
                {
                    _nonConvergeCnt[coreIdx]++;
                }
            }

            evt.Set();
        }

        public class Tar
        {
            public int id;
        }

        public struct Rel
        {
            public Tar a, b;
            public float strength;
            public float tab, tba;
        }

        public class Pop
        {
            public List<Rel> relations = new List<Rel>();
            public Dictionary<Tar, float> attitudes = new Dictionary<Tar, float>();
            public Dictionary<Tar, float> shadow = new Dictionary<Tar, float>();

            public void RandomReset(List<Tar> allTar)
            {
                attitudes.Clear();
                shadow.Clear();
                for(int i=0; i<allTar.Count; ++i)
                {
                    var v = Random.Range(-1f, 1f);
                    attitudes[allTar[i]] = v;
                    shadow[allTar[i]] = v;
                }
            }
        }
    }
}