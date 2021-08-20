using System;
using System.Net;
using System.Threading;
using P2P;
using P2P.Internal;

namespace p2pClient
{
    class Program
    {
        static void Main(string[] args)
        {
            NonceUtils nu = new NonceUtils(true, NaCl.Curve25519XSalsa20Poly1305.NonceLength, 5000, 0);

            Console.WriteLine(DateTimeOffset.Now.ToUnixTimeMilliseconds());

            for(int i = 0; i < 1000000; ++i)
            {
                byte[] nonce = nu.GetNextNonce();
                nu.TrackNonce(nonce);
            }


            //Thread.Sleep(1000);

            nu.RemoveExpiredNonces();

            Console.WriteLine(DateTimeOffset.Now.ToUnixTimeMilliseconds());
        }
    }
}
