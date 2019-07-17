using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

namespace MH
{
    ///<summary>
    /// data used for modification from external input
    ///</summary>
    public struct ModAttitude
    {
        public Entity entMindImageTargetNode;
        public float att_mod;
        public ModAttitude(Entity entNode, float mod ){this.entMindImageTargetNode = entNode; this.att_mod = mod;}
    }

    public struct ModLink
    {
        public Entity entLink;
        public float str_mod;
        public ModLink(Entity entLink, float mod) { this.entLink = entLink; this.str_mod = mod; }
    }

}