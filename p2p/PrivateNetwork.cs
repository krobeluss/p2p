using NaCl;
using P2P.Packets;
using P2P.Packets.Structures.Address;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace P2P
{
    class PrivateNetwork : IDisposable 
    {
        private UdpClient socket;

        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        private SortedDictionary<UInt32, RemoteClient> clients = new SortedDictionary<UInt32, RemoteClient>();

        private UInt32 myID;

        NaCLKeyPair keyPair;

        IPacketDissector adderssDissector = new AddressDissector();

        Timer helloTask;
        public void Dispose()
        {
            tokenSource.Cancel();
            socket.Close();

            helloTask.Dispose();
        }

        private void Run()
        {

            while (!tokenSource.Token.IsCancellationRequested)
            {
                IPEndPoint from = null;
                var data = socket.Receive(ref from);

                if (data.Length == 0)
                    continue;

                IDAdderss adderss = (IDAdderss)adderssDissector.Dissect(data);
            }
        }

        private void SendHello(object state)
        {
            lock(clients)
            {            
                foreach(var client in clients)
                {
                    if(!client.Value.IsConnected)
                    {

                    }
                }
            }
        }

        public void AddPeer(IPEndPoint endPoint, UInt32 peerID)
        {
            clients.Add(peerID, new RemoteClient(endPoint, endPoint, peerID));
        }



        public class PrivateNetworkBuilder
        {
            PrivateNetwork network = new PrivateNetwork();
            IPEndPoint endPoint;

            public PrivateNetworkBuilder()
            {

            }

            public PrivateNetworkBuilder AddNaCl(byte[] privateKey, byte[] publicKey )
            {
                network.keyPair = new NaCLKeyPair();
                network.keyPair.publicKey = publicKey;
                network.keyPair.privateKey = privateKey;

                return this;
            }

            public PrivateNetworkBuilder AddNaCl()
            {
                network.keyPair = new NaCLKeyPair();
                Curve25519XSalsa20Poly1305.KeyPair(out network.keyPair.privateKey, out network.keyPair.publicKey);

                return this;
            }

            public PrivateNetworkBuilder AddID(uint id)
            {
                network.myID = id;

                return this;
            }

            public PrivateNetworkBuilder AddBindAddress(IPEndPoint endPoint)
            {
                this.endPoint = endPoint;

                return this;
            }

            public PrivateNetwork Build()
            {
                if (network.myID == 0)
                    throw new InvalidOperationException("ID required");

                // Build Socket

                if (endPoint != null)
                    network.socket = new UdpClient(endPoint);
                else
                    network.socket = new UdpClient();

                const int SIO_UDP_CONNRESET = -1744830452;
                byte[] inValue = new byte[] { 0 };
                byte[] outValue = new byte[] { 0 };
                network.socket.Client.IOControl(SIO_UDP_CONNRESET, inValue, outValue);

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
