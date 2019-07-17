using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Unity.Collections;

using Random = UnityEngine.Random;
namespace MH
{
    public struct St : IEquatable<St>
    {
        int a, b, c, d;

        public St(int v) {a=b=c=d=v;}

        public bool Equals(St other)
        {
            return a == other.a;
        }

        public override int GetHashCode()
        {
            return a;
        }
    }

    public struct CpDummy : IComponentData
    {
        public int a, b, c, d, e;
    }

    public class MapECSCtrl : MonoBehaviour
    {
        public enum EMode { EMap, EMapConcurrent, ENativeMap, EInc };

        public const int CORE = 8;

        public EMode _mode = EMode.EMap;
        public int _map_entry_cnt = (int)1e6;

        Dictionary<Entity, int> _norm_map = new Dictionary<Entity, int>();
        ConcurrentDictionary<Entity, int> _con_map = null;
        AutoResetEvent[] _events = new AutoResetEvent[CORE];

        NativeHashMap<EntPair, int> _incMap;
        public int _incMapSingleAdd = (int)1e5;

        private EntityManager _em;
        private EntityArchetype _arche;
        
        void Start()
        {
            _em = World.Active.EntityManager;
            _arche = _em.CreateArchetype(typeof(CpDummy));

            switch( _mode )
            {
                case EMode.EMap: _Start_Map(); break;
                case EMode.EMapConcurrent: _Start_MapConcurrent(); break;
                case EMode.ENativeMap: _Start_NativeMap(); break;
                case EMode.EInc: _Start_Inc(); break;
            }
            
        }

        private void _Start_Inc()
        {
            _incMap = new NativeHashMap<EntPair, int>(_incMapSingleAdd, Allocator.Persistent);
        }

        private void _Start_MapConcurrent()
        {
            _con_map = new ConcurrentDictionary<Entity, int>(CORE, _map_entry_cnt * 2);
            for(int i=0; i<CORE; ++i)
                _events[i] = new AutoResetEvent(false);
        }

        private void _Start_Map()
        {
            
        }

        

        void Update()
        {
            switch(_mode)
            {
                case EMode.EMap: _Update_EMap(); break;
                case EMode.ENativeMap:  break;
                case EMode.EMapConcurrent: _Update_EMapCon(); break;
            }
        }

        void _Update_EMapCon()
        {
            _con_map.Clear();
            Parallel.For(0, _map_entry_cnt, idx => { //100K -- 167ms
                var ent = new Entity{Index=idx, Version=1};
                _con_map.AddOrUpdate(ent, idx, (k, oldval) => idx);
            });
        }

        private void _Start_NativeMap() //1M - 5ms
        {
            var w = World.Active;
            var grp = w.GetOrCreateSystem<SimulationSystemGroup>();
            var sys = w.GetOrCreateSystem<SysMapECS>();
            grp.AddSystemToUpdateList(sys);

            var entmgr = w.EntityManager;
            for(int i=0; i<_map_entry_cnt; ++i)
            {
                var e = entmgr.CreateEntity();
                entmgr.AddComponentData(e, new CpMap{ 
                    v = Random.Range(0, 100), 
                });
            }
        }

        void _Update_EMap()
        {
            _norm_map.Clear();
            for(int i=0; i<_map_entry_cnt; ++i)
            {
                var ent = new Entity{Index=i, Version=1}; //1M - 60ms
                _norm_map.Add(ent, i);
            }
        }

        void OnGUI()
        {
            switch(_mode)
            {
                case EMode.EInc: _OnGUI_Inc(); break;
            }
        }

        private void _OnGUI_Inc()
        {
            GUILayout.Label($"{_incMap.Length}");

            if(GUILayout.Button("Add"))
            {
                int cnt = _incMap.Length;
                for(int i=cnt; i<cnt+_incMapSingleAdd; ++i)
                {
                    var e = _em.CreateEntity(_arche);
                    _incMap.TryAdd(new EntPair(e, e), i);
                }
            }
        }
    }
}