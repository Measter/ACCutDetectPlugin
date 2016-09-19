using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACCutDetectorPlugin
{
    // Following class obtained from http://www.codeproject.com/Tips/862988/Find-the-Intersection-Point-of-Two-Line-Segments
    public class Vector2F
    {
        public double X;
        public double Y;

        // Constructors.
        public Vector2F( double x, double y )
        {
            X = x;
            Y = y;
        }
        public Vector2F() : this( double.NaN, double.NaN ) { }

        public static Vector2F operator -( Vector2F v, Vector2F w )
        {
            return new Vector2F( v.X - w.X, v.Y - w.Y );
        }

        public static Vector2F operator +( Vector2F v, Vector2F w )
        {
            return new Vector2F( v.X + w.X, v.Y + w.Y );
        }

        public static double operator *( Vector2F v, Vector2F w )
        {
            return v.X * w.X + v.Y * w.Y;
        }

        public static Vector2F operator *( Vector2F v, double mult )
        {
            return new Vector2F( v.X * mult, v.Y * mult );
        }

        public static Vector2F operator *( double mult, Vector2F v )
        {
            return new Vector2F( v.X * mult, v.Y * mult );
        }

        public double Cross( Vector2F v )
        {
            return X * v.Y - Y * v.X;
        }

        public override bool Equals( object obj )
        {
            var v = (Vector2F)obj;
            return ( X - v.X ).IsZero() && ( Y - v.Y ).IsZero();
        }
    }
}
