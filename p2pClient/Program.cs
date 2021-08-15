using System;
using P2P;

namespace p2pClient
{
    class Program
    {
        static void Main(string[] args)
        {
            PrivateNetwork privateNetwork = new PrivateNetwork.Builder().Build();
        }
    }
}
