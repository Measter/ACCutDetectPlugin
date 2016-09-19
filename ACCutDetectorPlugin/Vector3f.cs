using System;

namespace ACCutDetectorPlugin
{
    public struct Vector3F
    {
        private float m_x;
        private float m_y;
        private float m_z;

        public float X => m_x;
        public float Y => m_y;
        public float Z => m_z;

        public Vector3F(float x, float y, float z)
        {
            m_x = x;
            m_y = y;
            m_z = z;
        }

        public double Length() => Math.Sqrt(X*X + Y*Y + Z*Z);

        public static Vector3F operator -(Vector3F v, Vector3F w) => new Vector3F(v.X - w.X, v.Y - w.Y, v.Z - w.Z);

        public override string ToString() => $"[{m_x}, {m_y}, {m_z}]";
    }
}
