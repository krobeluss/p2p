using NaCl;
using P2P.Internal;
using P2P.Packets;
using P2P.Packets.Structures.Address;
using P2P.Packets.Structures.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Timer = System.Timers.Timer;

namespace P2P
{
    public class PrivateNetwork : IDisposable 
    {
        private UdpClient socket;
        private ConcurrentDictionary<UInt32, RemoteClient> clients = new ConcurrentDictionary<UInt32, RemoteClient>();
        private UInt32 myPeerID;
        private NaCLKeyPair keyPair;
        private IPacketDissector adderssDissector = new AddressDissector();

        internal Timer helloTask = new Timer();
        internal Timer pingTask = new Timer();

        private Thread readSocketThread;

        private IPEndPoint stunEndpoint;
        private IPEndPoint externalEndpoint;
        private IPEndPoint internalEndpoint;

        internal NetworkConfig config = new NetworkConfig();

        public IPEndPoint ExternalEndpoint { get => externalEndpoint; }
        public IPEndPoint InternalEndpoint { get => internalEndpoint; }
        public byte[] PublicKey { get => keyPair.publicKey; }

        private ConcurrentDictionary<int, VirtualUdpSocket> openedUdpSockets = new ConcurrentDictionary<int, VirtualUdpSocket>();

        private Dictionary<UInt16, Object> openedTcpSockets = new Dictionary<UInt16, Object>();

        private ReaderWriterLockSlim tcpSocketsLock = new ReaderWriterLockSlim();

        public uint MyPeerID
        {
            get => myPeerID; set
            {
                if (myPeerID != 0)
                    throw new InvalidOperationException("myPeerID cant change");
                myPeerID = value;
            }
        }

        public void Dispose()
        {
            readSocketThread.Interrupt();
            readSocketThread.Join();

            socket.Close();

            helloTask?.Dispose();
            pingTask?.Dispose();

            foreach(KeyValuePair<uint, RemoteClient> client in clients)
            {
                client.Value.Dispose();
            }

            clients.Clear();

        }

        public void Start()
        {
            if (readSocketThread != null && readSocketThread.IsAlive)
                throw new InvalidOperationException("Already started");

            readSocketThread = new Thread(ReadSocketThread);
            readSocketThread.Start();

            helloTask.Interval = config.HelloInterval;
            pingTask.Interval = config.PingInterval;

            helloTask.Start();
            pingTask.Start();
        }

        private void ReadSocketThread()
        {

            while (readSocketThread.IsAlive)
            {
                IPEndPoint from = null;
                byte[] data = socket.Receive(ref from);

                if (data.Length == 0)
                    continue;

                if(from.Equals(this.stunEndpoint))
                {
                    try
                    {
                        externalEndpoint = STUNParser.ParseSTUNResponse(data);
                    }
                    catch (Exception e)
                    {

                    }

                   
                    this.stunEndpoint = null;
                }
                else if(this.myPeerID != 0)
                {
                    IDAdderss adderss = (IDAdderss)adderssDissector.Dissect(data);

                    RemoteClient client;

                    if(clients.TryGetValue(adderss.FromID, out client))
                        client.ProcessPacket(adderss, from);
                }


            }
        }

        internal void SendTo(byte[] data, IPEndPoint to)
        {
            IDAdderss adderssPacket = new IDAdderss();
            adderssPacket.FromID = myPeerID;
            adderssPacket.Payload = data;

            byte[] packet = adderssDissector.Assembly(adderssPacket);

            socket.Send(packet, packet.Length, to);
        }

        public void AddPeer(uint peerID, byte[] publicKey, IPEndPoint externalAddress, IPEndPoint internalAddress)
        {

            if (myPeerID == 0)
                return;
            //throw new InvalidOperationException("myPeerID");


            Console.WriteLine("New peer " + internalAddress + " " + externalAddress + " with ID " + peerID);

            RemoteClient.Builder builder = new RemoteClient.Builder(this);

            builder
                .AddID(peerID)
                .AddAddEndpoint(externalAddress, internalAddress);

            if(this.keyPair.privateKey != null)
            {
                builder.AddNacl(new Curve25519XSalsa20Poly1305(this.keyPair.privateKey, publicKey));
                builder.AddNonceUtils(myPeerID > peerID, 60000, 0);
            }

            if (clients.TryAdd(peerID, builder.Build()))
            {

            }
            else
                throw new ArgumentException("peerID is dublicate");

        }

        public bool RemovePeer(UInt32 peerID)
        {
            RemoteClient peer;

            if (clients.TryRemove(peerID, out peer))
            {
                
                peer.Dispose();

                return true;
            }

            return false;
        }

        public void ReceiveExternalIP(IPEndPoint stunServer)
        {
            byte[] data = STUNParser.CreateStunRequest(new byte[12]);
            socket.Send(data, data.Length, stunServer);

            this.stunEndpoint = stunServer;
        }

        public void ReceiveInternalIP()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    this.internalEndpoint = new IPEndPoint(ip, ((IPEndPoint)this.socket.Client.LocalEndPoint).Port);
                    return;
                }
            }
        }

        internal void SendVirtualTcpPacket(TcpData data, uint to)
        {
            RemoteClient client;

            if (to == 0)
            {
                ProcessTcpPacket(data, 0);
            }
            else if (clients.TryGetValue(to, out client))
            {
                Console.WriteLine( myPeerID + "." + data.FromPort + " -> " + to + "." + data.ToPort + " [" + (data.SynFlag ? "S" : "") + (data.AckFlag ? "A" : "") + (data.FinFlag ? "F" : "") + (data.RstFlag ? "R" : "") + "] Seq = " + data.Seq + " Ack = " + data.Ack + " Len = " + (data.Payload == null ? 0 : data.Payload.Length) );
                client.SendTcpPacket(data);
            }
        }

        internal void SendVirtualUdpPacket(int fromPort, VirtualEndpoint to, byte[] data)
        {
            RemoteClient client;

            if(to.Id == int.MaxValue)
            {
                foreach (var i in clients)
                {
                    i.Value.SendUdpPacket(fromPort, to.Port, data);
                }
                    
            }
            else if (to.Id == 0)
            {
                ProcessUdpPacket(new VirtualEndpoint(0, fromPort), fromPort, data);
            }
            else if(clients.TryGetValue(to.Id, out client))
            {
                client.SendUdpPacket(fromPort, to.Port, data);
            }
        }

        internal void ProcessUdpPacket(VirtualEndpoint from, int toPort, byte[] data)
        {
            VirtualUdpSocket socket;

            if(openedUdpSockets.TryGetValue(toPort, out socket))
            {
                socket.incomingPackets.Add(new KeyValuePair<VirtualEndpoint, byte[]>(from, data));
            }
        }

        internal void ProcessTcpPacket(TcpData data, uint from)
        {

            if (from != 0)
                Console.WriteLine(from + "." + data.FromPort + " -> " + myPeerID + "." + data.ToPort + " [" + (data.SynFlag ? "S" : "") + (data.AckFlag ? "A" : "") + (data.FinFlag ? "F" : "") + (data.RstFlag ? "R" : "") + "] Seq = " + data.Seq + " Ack = " + data.Ack + " Len = " + (data.Payload == null ? 0 : data.Payload.Length) );
            else
                Console.WriteLine(0 + "." + data.FromPort + " -> " + 0 + "." + data.ToPort + " [" + (data.SynFlag ? "S" : "") + (data.AckFlag ? "A" : "") + (data.FinFlag ? "F" : "") + (data.RstFlag ? "R" : "") + "] Seq = " + data.Seq + " Ack = " + data.Ack + " Len = " + (data.Payload == null ? 0 : data.Payload.Length) );

            VirtualEndpoint remoteEndpoint = new VirtualEndpoint(from, data.FromPort);

            IVirtualTcpSocket socket = GetSocket(data.ToPort, remoteEndpoint);

            socket.ProcessFrame(data, remoteEndpoint);
        }

        public VirtualUdpSocket OpenUdpSocket(int port)
        {
            VirtualUdpSocket socket = new VirtualUdpSocket();
            socket.network = this;

            socket.incomingPackets = new BlockingCollection<KeyValuePair<VirtualEndpoint, byte[]>>(new ConcurrentQueue<KeyValuePair<VirtualEndpoint, byte[]>>(), 500);

            if (openedUdpSockets.TryAdd(port, socket))
                return socket;
            else
                throw new SocketException();

        }

        public VirtualTcpClient OpenTcpClient(VirtualEndpoint remoteClient)
        {
            if (remoteClient.Port == 0)
                throw new Exception("Invalid port");

            try
            {
                tcpSocketsLock.EnterWriteLock();

                for(int i = config.StartDynamicTcpPortRange; i < config.StartDynamicTcpPortRange + config.DynamicTcpPortCount; ++i)
                {
                    if(!openedTcpSockets.ContainsKey((ushort)i))
                    {
                        VirtualTcpClient newClient = new VirtualTcpClient((ushort)i, remoteClient, this);
                        openedTcpSockets.Add((ushort)i, newClient);

                        return newClient;
                    }
                }
            }
            finally
            {
                tcpSocketsLock.ExitWriteLock();
            }

            throw new SocketException();
        }

        public VirtualTcpClient OpenTcpClient(VirtualEndpoint remoteEndpoint, UInt16 localPort)
        {
            return OpenTcpClient(remoteEndpoint, localPort, false);
        }

        public VirtualTcpClient OpenTcpClient(VirtualEndpoint remoteEndpoint, UInt16 localPort, bool reuseLocalPort)
        {
            if (localPort == 0 || remoteEndpoint.Port == 0)
                throw new Exception("Invalid port");

            tcpSocketsLock.EnterWriteLock();

            object value;

            bool contains = openedTcpSockets.TryGetValue(localPort, out value);

            try
            {
                if (contains)
                {
                    if (typeof(Dictionary<VirtualEndpoint, IVirtualTcpSocket>).IsInstanceOfType(value))
                    {
                        Dictionary<VirtualEndpoint, IVirtualTcpSocket> reuseSockets = (Dictionary<VirtualEndpoint, IVirtualTcpSocket>)value;

                        if (reuseSockets.ContainsKey(remoteEndpoint))
                            throw new SocketException(10048);

                        if (!reuseLocalPort)
                            throw new SocketException(10048);
                        else
                        {
                            VirtualTcpClient newClient = new VirtualTcpClient(localPort, remoteEndpoint, this);
                            reuseSockets.Add(remoteEndpoint, newClient);

                            return newClient;
                        }
                            
                    }
                    else
                    {
                        // Уже заюзано

                        throw new SocketException(10048);
                    }
                }
                else
                {
                    if (reuseLocalPort)
                    {
                        Dictionary<VirtualEndpoint, VirtualTcpClient> reuseSockets = new Dictionary<VirtualEndpoint, VirtualTcpClient>();
                        VirtualTcpClient newClient = new VirtualTcpClient(localPort, remoteEndpoint, this);

                        reuseSockets.Add(remoteEndpoint, newClient);
                        openedTcpSockets.Add(localPort, reuseSockets);

                        return newClient;
                    }
                    else
                    {
                        VirtualTcpClient newClient = new VirtualTcpClient(localPort, remoteEndpoint, this);
                        openedTcpSockets.Add(localPort, newClient);

                        return newClient;
                    }
                        
                }
            }
            finally
            {
                tcpSocketsLock.ExitWriteLock();
            }

            //throw new Exception("Bug exceprion");
        }

        public VirtualTcpServer OpenTcpServer(UInt16 localPort)
        { 
            if (localPort == 0 )
                throw new Exception("Invalid port");

            tcpSocketsLock.EnterWriteLock();

            object value;

            bool contains = openedTcpSockets.TryGetValue(localPort, out value);

            try
            {
                if (!contains)
                {
                    VirtualTcpServer newClient = new VirtualTcpServer(localPort, this);

                    Dictionary<VirtualEndpoint, IVirtualTcpSocket> sockets = new Dictionary<VirtualEndpoint, IVirtualTcpSocket>();

                    sockets.Add( VirtualEndpoint.ANY, newClient );

                    openedTcpSockets.Add(localPort, sockets);

                    return newClient;
                }
                else
                    throw new SocketException(10048);

            }
            finally
            {
                tcpSocketsLock.ExitWriteLock();
            }
        }



        internal IVirtualTcpSocket GetSocket( ushort localPort, VirtualEndpoint remote)
        {
            tcpSocketsLock.EnterReadLock();

            object value;

            bool contains = openedTcpSockets.TryGetValue(localPort, out value);

            try
            {
                if (contains)
                {
                    if (typeof(Dictionary<VirtualEndpoint, IVirtualTcpSocket>).IsInstanceOfType(value))
                    {
                        Dictionary<VirtualEndpoint, IVirtualTcpSocket> sockets = (Dictionary<VirtualEndpoint, IVirtualTcpSocket>)value;

                        IVirtualTcpSocket socket;

                        sockets.TryGetValue(remote, out socket);

                        if(socket == null)
                            sockets.TryGetValue(VirtualEndpoint.ANY, out socket);

                        return socket;
                    }
                    else
                    {
                        IVirtualTcpSocket socket = (IVirtualTcpSocket)value;

                        VirtualTcpClient client = (VirtualTcpClient)socket;

                        if(client.remoteEndpoint.Equals(remote))
                            return socket;
                    }
                }

                return null;
            }
            finally
            {
                tcpSocketsLock.ExitReadLock();
            }

 
        }

        public class Builder
        {
            PrivateNetwork network = new PrivateNetwork();
            IPEndPoint endPoint;

            NetworkConfig config = new NetworkConfig();

            public Builder()
            {

            }

            public Builder SetMaxHelloAttemts(int count)
            {
                config.MaxHelloAttemts = count;

                return this;
            }

            public Builder AddNaCl(byte[] privateKey, byte[] publicKey )
            {
                network.keyPair = new NaCLKeyPair();
                network.keyPair.publicKey = publicKey;
                network.keyPair.privateKey = privateKey;

                return this;
            }

            public Builder AddNaCl()
            {
                network.keyPair = new NaCLKeyPair();
                Curve25519XSalsa20Poly1305.KeyPair(out network.keyPair.privateKey, out network.keyPair.publicKey);

                return this;
            }

            public Builder AddID(uint id)
            {
                network.myPeerID = id;

                return this;
            }

            public Builder AddBindAddress(IPEndPoint endPoint)
            {
                this.endPoint = endPoint;

                return this;
            }

            public PrivateNetwork Build()
            {
                // Build Socket

                if (endPoint != null)
                    network.socket = new UdpClient(endPoint);
                else
                    network.socket = new UdpClient();

                const int SIO_UDP_CONNRESET = -1744830452;
                byte[] inValue = new byte[] { 0 };
                byte[] outValue = new byte[] { 0 };

                network.socket.Client.IOControl(SIO_UDP_CONNRESET, inValue, outValue);
                network.config = config;

                network.socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 5 * 1024 * 1024);

                return network;
            }
        }

        private struct NaCLKeyPair
        {
            public byte[] privateKey;
            public byte[] publicKey;
        }
    }
}
