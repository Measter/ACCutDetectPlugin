using System;

namespace ACCutDetectorPlugin
{
    public class Lap
    {
        public TimeSpan LapTime
        {
            get; private set;
        }
        public Boolean DidCut
        {
            get; private set;
        }

        public Lap( TimeSpan lapTime, bool didCut )
        {
            LapTime = lapTime;
            DidCut = didCut;
        }
    }
}