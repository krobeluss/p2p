using System;
using System.Collections.Generic;
using System.Text;

namespace P2P.Internal
{
    internal class PeerConfig
    {
        private bool preferTcp;

        public bool PreferTcp { get => preferTcp; set => preferTcp = value; }
    }
}
