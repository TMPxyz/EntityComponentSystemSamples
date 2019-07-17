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
    public struct CpA : IComponentData
    {
        public int vA;
    }
    public struct CpB : IComponentData
    {
        public int vB;
    }

    public struct CpC : IComponentData
    {
        public int vC;
    }
    public struct CpD : IComponentData
    {
        public int vD;
    }
    public struct CpE : IComponentData
    {
        public int vE;
    }
}