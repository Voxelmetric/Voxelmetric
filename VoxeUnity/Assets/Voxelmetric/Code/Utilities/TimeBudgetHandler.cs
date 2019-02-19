﻿using UnityEngine;

namespace Voxelmetric.Code.Utilities
{
    public class TimeBudgetHandler
    {
        //! Time in ms allowed to be spent working on something
        public long TimeBudgetMs { get; set; }

        public bool HasTimeBudget { get; private set; }

        private long m_startTime;
        private long m_totalTime;
        
        public TimeBudgetHandler(long budget=0)
        {
            Reset();
            TimeBudgetMs = budget;
        }

        public void Reset()
        {
            m_startTime = 0;
            m_totalTime = 0;
            HasTimeBudget = true;
        }

        public void StartMeasurement()
        {
            m_startTime = Globals.Watch.ElapsedMilliseconds;
        }

        public void StopMeasurement()
        {
            long stopTime = Globals.Watch.ElapsedMilliseconds;
            Debug.Assert(stopTime>=m_startTime); // Let's make sure the class is used correctly

            m_totalTime += (stopTime-m_startTime);
            HasTimeBudget = m_totalTime<TimeBudgetMs;
        }
    }
}
