using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

namespace MH
{
    ///<summary>
    /// each Mind is a entity:
    /// * CpTargetNodeBuf : list of entities of mindImageTargetNode
    ///
    /// Each mindImageTargetNode is a entity:
    /// * CpOwner : point to the mind entity
    /// * CpMindImageTargetNode : point to self
    /// * CpAttitude : attitude of mind to this target
    /// * CpRelationBuf : list of entities containing relations
    /// 
    /// Each relation node entity:
    /// * CpRelation : from, to, strength, trans
    /// * CpIsDirty
    ///</summary>
    [InternalBufferCapacity(0)]
    public struct CpTargetNodeBuf :IBufferElementData 
    {
        public Entity target;
    }

    public struct CpOwner : IComponentData
    {
        public Entity entOwner;
    }

    public struct CpMindImageTargetNode : IComponentData
    {
        public Entity entTargetNode;
    }

    [InternalBufferCapacity(0)]
    public struct CpRelationBuf : IBufferElementData
    {
        public Entity entRelation; //entity of a CpRelation
    }

    public struct CpRelation : IComponentData
    {
        public Entity from; // this entity is mindImage of "From" in a mind "M"
        public Entity to; // this entity is mindimage of "To" in a mind "M"
        public float strength;
        public float trans;
    }

    public struct CpIsDirty : IComponentData
    {
        public bool dirty;
    }

    //this is the mind image of "target" in a "mind"
    public struct CpAttitude : IComponentData
    {
        public float attitude;
    }


}