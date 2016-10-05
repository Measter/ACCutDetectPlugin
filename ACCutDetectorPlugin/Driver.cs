using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ACCutDetectorPlugin
{
    public class Driver
    {
        private double m_speed;
        private readonly List<Lap> m_qualiLaps;

        public byte CarID
        {
            get;
            set;
        }
        public string Name
        {
            get; set;
        }
        public string GUID
        {
            get; set;
        }

        public Vector3F CurrentPosition
        {
            get; private set;
        }

        public Vector3F LastPosition
        {
            get; private set;
        }

        public UInt16 CutCount
        {
            get; private set;
        }

        public UInt16 Laps
        {
            get; private set;
        }

        public bool DidCutThisLap
        {
            get; private set;
        }

        public ReadOnlyCollection<Lap> LapTimes => m_qualiLaps.AsReadOnly();


        public Driver( string driverGUID )
        {
            Name = String.Empty;
            GUID = driverGUID;
            CarID = 255;
            m_qualiLaps = new List<Lap>();
            DidCutThisLap = false;
            ResetPosition();
            ResetCutCount();
        }

        public void ResetPosition()
        {
            CurrentPosition = LastPosition = new Vector3F( Single.NaN, Single.NaN, Single.NaN );
        }

        public void ResetCutCount()
        {
            CutCount = 0;
        }

        public void IncrementCut()
        {
            CutCount++;
        }

        public void ResetLapCount()
        {
            Laps = 0;
        }

        public void IncrementLapcount()
        {
            Laps++;
            DidCutThisLap = false;
        }

        public void ResetLapTimes()
        {
            m_qualiLaps.Clear();
        }

        public void AddLap( TimeSpan time )
        {
            m_qualiLaps.Add( new Lap( time, DidCutThisLap ) );
        }


        public void UpdatePositionAndSpeed( Vector3F pos, Vector3F vel )
        {
            m_speed = vel.Length() * 3.6; // Km/H

            LastPosition = CurrentPosition;
            CurrentPosition = pos;
        }

        public bool DidCut( out string cornerName )
        {
            cornerName = String.Empty;

            // If the driver is on their first update since connecting, they obviously can't cut.
            if( Double.IsNaN( LastPosition.X ) )
                return false;


            // Do speed test. May not be going fast enough to punish for cutting.
            if( m_speed < 50 )
                return false;

            // Absurd speeds are down to lag, so should also ignore.
            if( m_speed > 400 )
                return false;

            bool didCut = CutTester.TestCutLines( new Vector2F( LastPosition.X, LastPosition.Z ),
                                                  new Vector2F( CurrentPosition.X, CurrentPosition.Z ), out cornerName );

            DidCutThisLap |= didCut;

            return didCut;
        }
    }
}