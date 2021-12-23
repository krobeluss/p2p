using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace P2P.Internal
{

    public class WintunWrapper : IDisposable
    {
        private static bool libLoaded = Init();

        private IntPtr wrapperPtrStaruct;

        private bool sessionStarted;

        private object readLock = new object();

        public readonly static Guid guid = Guid.Parse("{0d5752d6-e2a3-4ba7-9c8c-09a01ae11de9}");

        public static bool LibLoaded { get => libLoaded; }
        public bool SessionStarted { get => sessionStarted; }

        public WintunWrapper( String name, String type, Guid guid )
        {
            if (!libLoaded)
                throw new InvalidOperationException("Lib not loaded");

            wrapperPtrStaruct = AllocStruct();

            if (wrapperPtrStaruct == IntPtr.Zero)
                throw new Exception("Unable alloc wintun struct");

            if (!CreateCancelEvent(wrapperPtrStaruct))
            {
                int error = Marshal.GetLastWin32Error();
                FreeStruct(wrapperPtrStaruct);
                throw new Exception("Unable create cancel event. Error code: " + error);
            }


            if (!CreateAdapter(wrapperPtrStaruct, name, type, guid))
            {
                int error = Marshal.GetLastWin32Error();
                CloseEvent(wrapperPtrStaruct);
                FreeStruct(wrapperPtrStaruct);
                throw new Exception("Unable create adapter. Error code: " + error);
            }
        }

        public void SetMtu(uint mtu)
        {
            if (wrapperPtrStaruct == IntPtr.Zero)
                throw new ObjectDisposedException(GetType().FullName);

            if (mtu > 0xFFFF)
                throw new ArgumentException("Mtu too big");

            if (mtu < 20)
                throw new ArgumentException("Mtu too short");

            SetMtu(wrapperPtrStaruct, mtu);

            int lastError = Marshal.GetLastWin32Error();

            if (lastError != 0)
                throw new Exception("Unable set mtu. Error code: " + lastError);
        }

        public void SetIPSettings( IPAddress ip, byte onLinkPrefix )
        {
            if (wrapperPtrStaruct == IntPtr.Zero)
                throw new ObjectDisposedException(GetType().FullName);

            if (ip.AddressFamily != AddressFamily.InterNetwork)
                throw new ArgumentException( "IP not AF_INET" );

            if(onLinkPrefix < 0 || onLinkPrefix > 32)
                throw new ArgumentException("onLinkPrefix invalid. Allow range: 0-32. Got: " + onLinkPrefix);

            SetIPSettings(wrapperPtrStaruct, ip.ToString( ), onLinkPrefix);

            int lastError = Marshal.GetLastWin32Error();

            if (lastError != 0)
                throw new Exception("Unable set mtu. Error code: " + lastError);
        }

        public void StartSession()
        {
            if (wrapperPtrStaruct == IntPtr.Zero)
                throw new ObjectDisposedException(GetType().FullName);

            if (sessionStarted)
                throw new InvalidOperationException("Session already started");

            sessionStarted = StartSession(wrapperPtrStaruct);

            if (!sessionStarted)
                throw new Exception("Unable start sesstion. Error code: " + Marshal.GetLastWin32Error());
        }

        public void StopSession()
        {
            if (wrapperPtrStaruct == IntPtr.Zero)
                throw new ObjectDisposedException(GetType().FullName);

            if (!sessionStarted)
                throw new InvalidOperationException("Session already stopped");

            sessionStarted = false;

            try
            { 
                CloseSession(wrapperPtrStaruct);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

//            if (!sessionStarted)
//                throw new Exception("Unable start sesstion. Error code: " + Marshal.GetLastWin32Error());
        }

        public byte[] Read(int waitTime)
        {
            lock (readLock)
            {
                if (wrapperPtrStaruct == IntPtr.Zero)
                    throw new ObjectDisposedException(GetType().FullName);

                if (!sessionStarted)
                    throw new InvalidOperationException("Session not started");

                uint paketSize = 0;
                IntPtr dataPtr = ReceivePacket(wrapperPtrStaruct, out paketSize);

                if (dataPtr == IntPtr.Zero)
                {
                    int errorCode = Marshal.GetLastWin32Error();

                    switch (errorCode)
                    {
                        case 38: // EOF
                            return new byte[0];
                        case 259: // No more intems
                            if (waitTime != 0)
                            {
                                WaitPacket(wrapperPtrStaruct, (uint)waitTime);
                                errorCode = Marshal.GetLastWin32Error();

                                if (errorCode == 0)
                                {
                                    dataPtr = ReceivePacket(wrapperPtrStaruct, out paketSize);

                                    errorCode = Marshal.GetLastWin32Error();

                                    if (errorCode == 259)
                                        return null;

                                    if (errorCode == 38)
                                        return new byte[0];

                                    if (dataPtr == IntPtr.Zero)
                                        throw new Exception("Unable receive packet. Error code: " + errorCode);


                                    byte[] resultData = new byte[paketSize];
                                    Marshal.Copy(dataPtr, resultData, 0, (int)paketSize);

                                    ReleasePacket(wrapperPtrStaruct, dataPtr);

                                    return resultData;
                                }
                                else
                                {
                                    errorCode = Marshal.GetLastWin32Error();

                                    if (errorCode == 995) // Aborted
                                        throw new OperationCanceledException();
                                    else if (errorCode == 1460)
                                        throw new TimeoutException();
                                    else
                                        throw new Exception("Unable receive packet. Error code: " + Marshal.GetLastWin32Error());
                                }
                            }
                            else
                                return null;
                        default:
                            throw new Exception("Unable receive packet. Error code: " + Marshal.GetLastWin32Error());
                    }
                }

                byte[] resultData2 = new byte[paketSize];
                Marshal.Copy(dataPtr, resultData2, 0, (int)paketSize);

                ReleasePacket(wrapperPtrStaruct, dataPtr);

                return resultData2;
            }
        }

        public int Read(byte[] data, int waitTime)
        {
            lock (readLock)
            {
                if (wrapperPtrStaruct == IntPtr.Zero)
                    throw new ObjectDisposedException(GetType().FullName);

                if (!sessionStarted)
                    throw new InvalidOperationException("Session not started");

                uint paketSize = 0;
                IntPtr dataPtr = ReceivePacket(wrapperPtrStaruct, out paketSize);

                if (dataPtr == IntPtr.Zero)
                {
                    int errorCode = Marshal.GetLastWin32Error();

                    switch (errorCode)
                    {
                        case 38: // EOF
                            return -1;
                        case 259: // No more intems
                            if (waitTime != 0)
                            {
                                WaitPacket(wrapperPtrStaruct, (uint)waitTime);

                                errorCode = Marshal.GetLastWin32Error();

                                if (errorCode == 0)
                                {
                                    dataPtr = ReceivePacket(wrapperPtrStaruct, out paketSize);

                                    if (dataPtr == IntPtr.Zero)
                                        throw new Exception("Unable receive packet. Error code: " + errorCode);

                                    if(data.Length < paketSize)
                                    {
                                        ReleasePacket(wrapperPtrStaruct, dataPtr);
                                        throw new IndexOutOfRangeException();
                                    }

                                    Marshal.Copy(dataPtr, data, 0, (int)paketSize);

                                    return (int)paketSize;
                                }
                                else
                                {
                                    errorCode = Marshal.GetLastWin32Error();

                                    if (errorCode == 995) // Aborted
                                        throw new OperationCanceledException();
                                    else if (errorCode == 1460)
                                        throw new TimeoutException();
                                    else
                                        throw new Exception("Unable receive packet. Error code: " + Marshal.GetLastWin32Error());
                                }
                            }
                            else
                                return 0;
                        default:
                            throw new Exception("Unable receive packet. Error code: " + Marshal.GetLastWin32Error());
                    }
                }

                if (data.Length < paketSize)
                {
                    ReleasePacket(wrapperPtrStaruct, dataPtr);
                    throw new IndexOutOfRangeException();
                }

                Marshal.Copy(dataPtr, data, 0, (int)paketSize);

                return (int)paketSize;
            }
        }

        public bool Send( byte[] data )
        {
            if (wrapperPtrStaruct == IntPtr.Zero)
                throw new ObjectDisposedException(GetType().FullName);

            if (!sessionStarted)
                throw new InvalidOperationException("Session not started");

            bool sendResult = SendPakcet(wrapperPtrStaruct, data, (uint)data.Length);

            if (!sendResult)
            {
                int errorCode = Marshal.GetLastWin32Error();

                if (errorCode == 111 || errorCode == 38)
                    return false;

                throw new Exception("Unable send. Error code: " + sendResult);
            }
            else
                return true;
        }

        public void Abort()
        {
            if (wrapperPtrStaruct == IntPtr.Zero)
                throw new ObjectDisposedException(GetType().FullName);

            SetAbortEvent(wrapperPtrStaruct, true);
        }

        public void Dispose()
        {
            if (wrapperPtrStaruct == IntPtr.Zero)
                throw new ObjectDisposedException(GetType().FullName);

            SetAbortEvent(wrapperPtrStaruct, false);

            lock (readLock)
            {
                if (sessionStarted)
                    CloseSession(wrapperPtrStaruct);

                CloseAdapter(wrapperPtrStaruct);
                CloseEvent(wrapperPtrStaruct);

                FreeStruct(wrapperPtrStaruct);
                wrapperPtrStaruct = IntPtr.Zero;
            }
        }

        [DllImport("WintunWrapper.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool Init();

        [DllImport("WintunWrapper.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr AllocStruct();

        [DllImport("WintunWrapper.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateCancelEvent(IntPtr winTunWrapperInstance);

        [DllImport("WintunWrapper.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateAdapter(IntPtr winTunWrapperInstance, String name, String tunnelType, Guid guid);

        [DllImport("WintunWrapper.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetIPSettings(IntPtr winTunWrapperInstance, String ip, byte onLinkPrefixLength);

        [DllImport("WintunWrapper.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetMtu(IntPtr winTunWrapperInstance, uint mtu);

        [DllImport("WintunWrapper.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool StartSession(IntPtr winTunWrapperInstance);

        [DllImport("WintunWrapper.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr ReceivePacket(IntPtr winTunWrapperInstance, out uint PacketSize);

        [DllImport("WintunWrapper.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern void WaitPacket(IntPtr winTunWrapperInstance, uint waitSeconds);

        [DllImport("WintunWrapper.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern void ReleasePacket(IntPtr winTunWrapperInstance, IntPtr Packet);

        [DllImport("WintunWrapper.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SendPakcet(IntPtr winTunWrapperInstance, byte[] Data, uint Size);

        [DllImport("WintunWrapper.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern void CloseSession(IntPtr winTunWrapperInstance);

        [DllImport("WintunWrapper.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern void CloseAdapter(IntPtr winTunWrapperInstance);

        [DllImport("WintunWrapper.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern void FreeStruct(IntPtr winTunWrapperInstance);

        [DllImport("WintunWrapper.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetAbortEvent(IntPtr winTunWrapperInstance, bool enableAutoReset);

        [DllImport("WintunWrapper.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern void CloseEvent(IntPtr winTunWrapperInstance);

    }
}
