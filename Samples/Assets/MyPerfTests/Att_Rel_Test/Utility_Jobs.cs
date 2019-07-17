using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using UnityEngine.Assertions;

namespace MH
{
    [BurstCompile]
    public struct CopyQueueToListJob<T> : IJob
        where T : struct
    {
        public NativeQueue<T> Queue;
        public NativeList<T> List;
        public void Execute()
        {
            this.List.Clear();
            this.List.Capacity = this.Queue.Count;
 
            while (this.Queue.TryDequeue(out var v))
            {
                this.List.Add(v);
            }
        }
    }

    [BurstCompile]
    public struct DestroyEntityJob : IJobChunk
    {
        public EntityCommandBuffer.Concurrent cmdBuf;
        [ReadOnly] public ArchetypeChunkEntityType entType;

        public DestroyEntityJob(EntityCommandBuffer.Concurrent cmdBuf)
        {
            this.cmdBuf = cmdBuf;
            this.entType = World.Active.EntityManager.GetArchetypeChunkEntityType();
        }

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var arr_ent = chunk.GetNativeArray(entType);
            for(int i=0; i<chunk.Count; ++i)
            {
                cmdBuf.DestroyEntity(chunkIndex, arr_ent[i]);
            }
        }
    }


    // // [BurstCompile]
    // public struct GetUniqueHashMapKeyArrayJob<TKey, TValue, U> : IJob 
    //     where TKey : struct, IEquatable<TKey>
    //     where TValue : struct
    //     where U : struct, IComparer<TKey>
    // {
    //     [ReadOnly] public NativeMultiHashMap<TKey, TValue> mmap;
    //     [WriteOnly] public NativeList<TKey> keyList;
    //     [ReadOnly] public U cmp;

    //     public void Execute()
    //     {
    //         var arr_key = mmap.GetKeyArray(Allocator.Temp);

    //         arr_key.Sort( cmp );
    //         TKey prev = default(TKey);
    //         for(int i=0; i<arr_key.Length; ++i)
    //         {
    //             var elem = arr_key[i];
    //             if (!elem.Equals(prev))
    //             {
    //                 keyList.Add(elem);
    //                 prev = elem;
    //             }
    //         }

    //         arr_key.Dispose();
    //     }
    // }

    // [BurstCompile] //cannot use BurstCompile on EntityCommandBuffer except DestroyEntity
    public struct SetDirtyJobDefer : IJobParallelForDefer
    {
        public EntityCommandBuffer.Concurrent cmdBuf;
        [ReadOnly] public NativeArray<Entity> dirty_entities;
        
        public void Execute(int index)
        {
            cmdBuf.AddComponent(index, dirty_entities[index], new CpIsDirty());
        }
    }
    // [BurstCompile] //cannot use BurstCompile on EntityCommandBuffer except DestroyEntity
    public struct RemoveDirtyJobDefer : IJobParallelForDefer
    {
        [WriteOnly]
        public EntityCommandBuffer.Concurrent cmdBuf;
        [ReadOnly] public NativeArray<Entity> dirty_entities;
        
        public void Execute(int index)
        {
            cmdBuf.RemoveComponent<CpIsDirty>(index, dirty_entities[index]);
        }
    }
    // [BurstCompile] //cannot use BurstCompile on EntityCommandBuffer except DestroyEntity
    public struct SetDirtyJob : IJobParallelFor
    {
        public EntityCommandBuffer.Concurrent cmdBuf;
        [ReadOnly] public NativeArray<Entity> dirty_entities;
        
        public void Execute(int index)
        {
            cmdBuf.AddComponent(index, dirty_entities[index], new CpIsDirty());
        }
    }

    
    
}

public static class ContainerExt
{
    //---------clear containers---------//
    public static JobHandle ClearWithJob<T>(this NativeList<T> container, JobHandle handle) where T : struct
    {
        return new ClearNativeListJob<T>{container=container}.Schedule(handle);
    }
    public static JobHandle ClearWithJob<T>(this NativeQueue<T> container, JobHandle handle) where T : struct
    {
        return new ClearNativeQueueJob<T>{container=container}.Schedule(handle);
    }
    public static JobHandle ClearWithJob<K,V>(this NativeHashMap<K,V> container, JobHandle handle) where K:struct,IEquatable<K> where V:struct
    {
        return new ClearNativeHashMapJob<K,V>{container=container}.Schedule(handle);
    }
    public static JobHandle ClearWithJob<K,V>(this NativeMultiHashMap<K,V> container, JobHandle handle) where K:struct,IEquatable<K> where V:struct
    {
        return new ClearNativeMultiHashMap<K,V>{container=container}.Schedule(handle);
    }

    public struct ClearNativeListJob<T> : IJob where T : struct
    {
        public NativeList<T> container;
        public void Execute()
        {
            container.Clear();
        }
    }
    public struct ClearNativeQueueJob<T> : IJob where T : struct
    {
        public NativeQueue<T> container;
        public void Execute()
        {
            container.Clear();
        }
    }
    public struct ClearNativeHashMapJob<K,V> : IJob where K:struct,IEquatable<K> where V:struct
    {
        public NativeHashMap<K,V> container;
        public void Execute()
        {
            container.Clear();
        }
    }
    public struct ClearNativeMultiHashMap<K,V> : IJob where K:struct,IEquatable<K> where V:struct
    {
        public NativeMultiHashMap<K,V> container;
        public void Execute()
        {
            container.Clear();
        }
    }
}