using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

namespace MH
{
    //this is the mind image of "target" in a "mind"
    public struct CpAttitude : IComponentData
    {
        public float attitude;
    }
}