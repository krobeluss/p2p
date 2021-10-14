using System;
using System.Collections.Generic;
using System.Text;

namespace P2P.Internal
{
    internal class SynGenerator
    {
        public static uint GetNextSeq()
        {
            return 0;
            //return (uint)(DateTimeOffset.Now.ToUnixTimeMilliseconds() % 0xFFFFFFFF);
        }
    }
}
