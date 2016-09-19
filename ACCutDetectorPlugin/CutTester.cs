using System;
using System.Collections.Generic;
using System.IO;

namespace ACCutDetectorPlugin
{
    public class CutTester
    {
        private static readonly List<Line> m_lines = new List<Line>();

        public static void LoadTrack(string track, string trackLayout)
        {
            m_lines.Clear();
            string filename = $"cut_files\\{track}-{trackLayout}.csv";

            if (!File.Exists(filename))
            {
                Console.WriteLine("Unable to open cut file.");
                return;
            }

            foreach (string line in File.ReadAllLines(filename))
            {
                if (line.StartsWith("corner;"))
                    continue;

                string[] parts = line.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 5)
                {
                    Console.WriteLine($"Log: Invalid line: {line}");
                    continue;
                }

                double startx, starty, endx, endy;
                if( !Double.TryParse( parts[1], out startx ) )
                {
                    Console.WriteLine( $"Log: Invalid line: {line}" );
                    continue;
                }
                if( !Double.TryParse( parts[2], out starty ) )
                {
                    Console.WriteLine( $"Log: Invalid line: {line}" );
                    continue;
                }
                if( !Double.TryParse( parts[3], out endx ) )
                {
                    Console.WriteLine( $"Log: Invalid line: {line}" );
                    continue;
                }
                if( !Double.TryParse( parts[4], out endy ) )
                {
                    Console.WriteLine( $"Log: Invalid line: {line}" );
                    continue;
                }

                m_lines.Add(new Line(parts[0], startx, starty, endx, endy));
            }
        }

        public static bool TestCutLines(Vector2F lastPosition, Vector2F currentPosition, out string cornerName)
        {
            foreach (Line line in m_lines)
            {
                if (line.LineSegementsIntersect(lastPosition, currentPosition))
                {
                    cornerName = line.Name;
                    return true;
                }
            }

            cornerName = String.Empty;
            return false;
        }
    }

    public class Line
    {
        public Line(string name, double startX, double startY, double endX, double endY)
        {
            Name = name;
            Start = new Vector2F(startX, startY);
            End = new Vector2F(endX, endY);
        }

        public string Name { get; }
        public Vector2F Start { get; }
        public Vector2F End { get; }


        // Following function obtained from http://www.codeproject.com/Tips/862988/Find-the-Intersection-Point-of-Two-Line-Segments
        public bool LineSegementsIntersect( Vector2F qStart, Vector2F qEnd, bool considerCollinearOverlapAsIntersect = false )
        {

            var r = End - Start;
            var s = qEnd - qStart;
            var rxs = r.Cross( s );
            var qpxr = ( qStart - Start ).Cross( r );

            // If r x s = 0 and (qStart - pStart) x r = 0, then the two lines are collinear.
            if( rxs.IsZero() && qpxr.IsZero() )
            {
                // 1. If either  0 <= (qStart - pStart) * r <= r * r or 0 <= (pStart - qStart) * s <= * s
                // then the two lines are overlapping,
                if( considerCollinearOverlapAsIntersect )
                    if( ( 0 <= ( qStart - Start ) * r && ( qStart - Start ) * r <= r * r ) || ( 0 <= ( Start - qStart ) * s && ( Start - qStart ) * s <= s * s ) )
                        return true;

                // 2. If neither 0 <= (qStart - pStart) * r = r * r nor 0 <= (pStart - qStart) * s <= s * s
                // then the two lines are collinear but disjoint.
                // No need to implement this expression, as it follows from the expression above.
                return false;
            }

            // 3. If r x s = 0 and (qStart - pStart) x r != 0, then the two lines are parallel and non-intersecting.
            if( rxs.IsZero() && !qpxr.IsZero() )
                return false;

            // t = (qStart - pStart) x s / (r x s)
            var t = ( qStart - Start ).Cross( s ) / rxs;

            // u = (qStart - pStart) x r / (r x s)

            var u = ( qStart - Start ).Cross( r ) / rxs;

            // 4. If r x s != 0 and 0 <= t <= 1 and 0 <= u <= 1
            // the two line segments meet at the point pStart + t r = qStart + u s.
            if( !rxs.IsZero() && ( 0 <= t && t <= 1 ) && ( 0 <= u && u <= 1 ) )
            {
                // An intersection was found.
                return true;
            }

            // 5. Otherwise, the two line segments are not parallel but do not intersect.
            return false;
        }
    }
}