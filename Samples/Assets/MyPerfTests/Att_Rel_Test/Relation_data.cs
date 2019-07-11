using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

namespace MH
{
    public struct CpRelation : IComponentData
    {
        public Entity from; // this entity is mindImage of "From" in a mind "M"
        public Entity to; // this entity is mindimage of "To" in a mind "M"
        public float strength;
        public float trans;
    }

    public struct CpOwner : ISharedComponentData
    {
        public Entity owner;
    }

    // public struct CpRelFrom : ISharedComponentData
    // {
    //     public Entity from;
    // }

    // public struct CpRelData : IComponentData
    // {
    //     public Entity to;
    //     public float strength;
    //     public float trans;
    // }

    // public struct CpRelDataUpdate : IComponentData
    // {}
}