using System;
using System.Collections.Generic;
using System.Text;

namespace P2P.Internal
{
    internal class NetworkConfig
    {
        private int maxHelloAttemts  = 30;

        private int pingInterval = 5000;

        private int helloInterval = 1000;

        public int MaxHelloAttemts { get => maxHelloAttemts; set => maxHelloAttemts = value; }
        public int PingInterval { get => pingInterval; set => pingInterval = value; }
        public int HelloInterval { get => helloInterval; set => helloInterval = value; }
    }
}
