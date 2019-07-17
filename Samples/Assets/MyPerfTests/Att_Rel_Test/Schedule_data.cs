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
    public struct CpSysSchedule : IComponentData
    {
        public float interval;
        public float lastUpdateTime;
        public bool sysUpdateRunning;

        public bool CanRunSysModData()
        {
            if (Time.time - lastUpdateTime < interval || sysUpdateRunning) //check if the system is done CD and updateAtt is done
                return false;
            else
                return true;
        }

        public void Finish_SysModData()
        {
            lastUpdateTime = Time.time;
            sysUpdateRunning = true;
        }

        public bool CanRunSysUpdateAtt()
        {
            return sysUpdateRunning;
        }

        public void Finish_UpdateAtt()
        {
            sysUpdateRunning = false;
        }
    }

}