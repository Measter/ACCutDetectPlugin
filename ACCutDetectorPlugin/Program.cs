using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IniParser;
using IniParser.Model;

namespace ACCutDetectorPlugin
{
    class Program
    {
        private static string m_version = "0.1";
        private static StreamWriter m_logFile;

        private static readonly Dictionary<string, Driver> m_driversFromGUID = new Dictionary<string, Driver>();
        private static readonly Dictionary<byte, Driver> m_driversFromCarID = new Dictionary<byte, Driver>();

        private static SessionType m_sessionType = SessionType.None;

        private static IPEndPoint m_serverDataPoint;
        private static IPEndPoint m_serverCommandPoint;
        private static IPEndPoint m_clientDataPoint;
        private static IPEndPoint m_clientCommandPoint;


        private static UdpClient m_forwardClient;
        private static UdpClient m_serverClient;

        private static bool m_forwardingEnabled = false;
        private static string m_configName = "CutPluginConfig.ini";
        private static string m_lastTrackLayout = String.Empty;


        static void Main( string[] args )
        {
            Console.WriteLine( $"Assetto Corsa Cut Detection Plugin - Version {m_version}" );

            if( !ReadConfig() )
            {
                Console.WriteLine( "Config error, Exiting." );
                return;
            }

            string filename = $"cutlog-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.log";
            Console.WriteLine( $"Opening log file {filename}" );
            m_logFile = new StreamWriter(filename, false, Encoding.UTF8);

            if( m_forwardingEnabled )
            {
                Console.WriteLine( $"Opening Forwarding UDP Client at {m_clientDataPoint.Address}:{m_clientDataPoint.Port}" );
                m_forwardClient = new UdpClient( m_clientCommandPoint );
                Console.WriteLine( "Client Opened." );
                Thread commandThread = new Thread( CommandForwardTask );
                commandThread.Start();
            }

            Console.WriteLine( $"Opening UDP Client at {m_serverDataPoint.Address}:{m_serverDataPoint.Port}" );
            m_serverClient = new UdpClient( m_serverDataPoint );
            Console.WriteLine( "Client Opened. Waiting for server." );

            while( true )
            {
                byte[] bytes = m_serverClient.Receive( ref m_serverCommandPoint );

                if( m_forwardingEnabled )
                    m_forwardClient.Send( bytes, bytes.Length, m_clientDataPoint );

                BinaryReader reader = new BinaryReader( new MemoryStream( bytes ) );

                ACSProtocol packetID = (ACSProtocol)reader.ReadByte();

                switch( packetID )
                {
                    case ACSProtocol.CarInfo:
                    case ACSProtocol.ClientEvent:
                    case ACSProtocol.LapCompleted:
                    case ACSProtocol.Version:
                    case ACSProtocol.Chat:
                    case ACSProtocol.ClientLoaded:
                    case ACSProtocol.EndSession:
                        break;

                    case ACSProtocol.NewSession: // Is immediately followed by session info.
                    case ACSProtocol.SessionInfo:
                        HandleNewSessionInfo( reader, packetID );
                        break;

                    case ACSProtocol.NewConnection:
                    case ACSProtocol.ConnectionClosed:
                        HandleConnectionChange( reader, packetID );
                        break;

                    case ACSProtocol.CarUpdate:
                        HandleCarUpdate( reader );
                        break;

                    case ACSProtocol.Error:
                        Console.WriteLine( "Error from server:" );
                        Console.WriteLine( $" - {ReadUnicodeString( reader )}" );
                        break;
                    default:
                        Console.WriteLine( $"Invalid packet ID: {(byte)packetID}:H" );
                        break;
                }
            }
        }

        private static bool ReadConfig()
        {
            if( !File.Exists( m_configName ) )
            {
                Console.WriteLine( "Error: Config file missing." );
                return false;
            }

            FileIniDataParser iniParser = new FileIniDataParser();
            IniData data = iniParser.ReadFile( m_configName );

            IPAddress ip;
            try
            {
                var serverSection = data.Sections["server"];
                ip = IPAddress.Parse( serverSection["address"] );
                m_serverCommandPoint = new IPEndPoint( ip, Int32.Parse( serverSection["commandport"] ) );
                m_serverDataPoint = new IPEndPoint( ip, Int32.Parse( serverSection["dataport"] ) );
            } catch( Exception ex )
            {
                Console.WriteLine( "Error: Misconfigured server details." );
                Console.WriteLine( ex.ToString() );
                return false;
            }

            if( !data.Sections.ContainsSection( "forwarding" ) )
                return true;

            try
            {
                var forwardSection = data.Sections["forwarding"];

                m_forwardingEnabled = Boolean.Parse( forwardSection["enabled"] );
                if( !m_forwardingEnabled )
                    return true;

                ip = IPAddress.Parse( forwardSection["address"] );
                m_clientCommandPoint = new IPEndPoint( ip, Int32.Parse( forwardSection["commandport"] ) );
                m_clientDataPoint = new IPEndPoint( ip, Int32.Parse( forwardSection["dataport"] ) );
            } catch( Exception ex )
            {
                Console.WriteLine( "Error: Misconfigured forwarding." );
                Console.WriteLine( ex.ToString() );
                return false;
            }

            return true;
        }

        private static void HandleCarUpdate( BinaryReader reader )
        {
            var carID = reader.ReadByte();
            Vector3F pos = ReadPosition( reader ); // Position.
            Vector3F vel = ReadPosition( reader ); // Velocity vector.

            var curDriver = m_driversFromCarID[carID];

            curDriver.UpdatePositionAndSpeed( pos, vel );

            string cornerName;
            if( curDriver.DidCut( out cornerName ) )
            {
                curDriver.IncrementCut();
                Console.WriteLine( $"[Cut] : {curDriver.Name} - {cornerName} - {curDriver.CutCount}" );
                m_logFile.WriteLine( $"[Cut] : {curDriver.Name} - {cornerName} - {curDriver.CutCount}" );
            }

            m_logFile.Flush();
        }

        private static void HandleConnectionChange( BinaryReader reader, ACSProtocol packetID )
        {
            string driverName = ReadUnicodeString( reader ); // Driver name.
            string driverGUID = ReadUnicodeString( reader ); // Driver GUID.
            var carID = reader.ReadByte();

            if( packetID == ACSProtocol.NewConnection )
            {
                Console.WriteLine( $"Driver joined: {driverName}" );
                Driver curDriver;
                if( m_driversFromGUID.ContainsKey( driverGUID ) )
                {
                    curDriver = m_driversFromGUID[driverGUID];
                } else
                {
                    curDriver = new Driver( driverGUID );
                    m_driversFromGUID[driverGUID] = curDriver;
                }

                curDriver.Name = driverName;
                curDriver.CarID = carID;
                curDriver.ResetPosition();
                m_driversFromCarID[carID] = curDriver;
            } else
            {
                Console.WriteLine( $"Driver left: {driverName}" );
                m_driversFromCarID.Remove( carID );
            }
        }

        private static void HandleNewSessionInfo( BinaryReader reader, ACSProtocol packetID )
        {
            reader.ReadByte(); // Version.
            reader.ReadByte(); // Session index.
            reader.ReadByte(); // Current session index.
            reader.ReadByte(); // Session count.
            ReadUnicodeString( reader ); // Server name.

            string track = ReadAsciiString( reader );
            string trackLayout = ReadAsciiString( reader );
            ReadAsciiString( reader ); // Session name.

            var prevSessionType = m_sessionType;
            m_sessionType = (SessionType)reader.ReadByte();

            if( packetID == ACSProtocol.NewSession )
            {
                string currentLayout = $"{track}-{trackLayout}";

                if (m_lastTrackLayout != currentLayout )
                {
                    Console.WriteLine( $"Loading cut file for {track}-{trackLayout}" );
                    CutTester.LoadTrack( track, trackLayout );
                    m_lastTrackLayout = $"{track}-{trackLayout}";
                }

                Console.WriteLine( $"New session started: {m_sessionType}" );
                m_logFile.WriteLine( $"[Session] : Session ended {prevSessionType}" );

                foreach( var driver in m_driversFromGUID )
                    driver.Value.ResetCutCount();

                m_logFile.Flush();
            }
        }

        private static void CommandForwardTask()
        {
            while( true )
            {
                byte[] bytes = m_forwardClient.Receive( ref m_clientCommandPoint );
                m_serverClient.Send( bytes, bytes.Length, m_serverCommandPoint );
            }
        }

        private static void ActivateRealTimeReporting( UdpClient client )
        {
            byte[] buffer = new byte[4];
            BinaryWriter bw = new BinaryWriter( new MemoryStream( buffer ) );

            bw.Write( (byte)ACSProtocolCommands.RealtimeposInterval );
            bw.Write( (UInt16)77 ); // 15hz.

            client.Send( buffer, (int)bw.BaseStream.Length, m_serverCommandPoint );
        }


        private static Vector3F ReadPosition( BinaryReader reader ) => new Vector3F( reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() );

        private static string ReadAsciiString( BinaryReader br )
        {
            byte length = br.ReadByte();
            return new String( br.ReadChars( length ) );
        }

        private static string ReadUnicodeString( BinaryReader br )
        {
            byte length = br.ReadByte();
            return Encoding.UTF32.GetString( br.ReadBytes( length * 4 ) );
        }
    }
}
