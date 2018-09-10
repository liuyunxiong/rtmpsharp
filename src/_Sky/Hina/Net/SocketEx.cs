using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Konseki;

// csharp: hina/linq/socketex.cs [snipped]
namespace Hina.Net
{
    static class SocketEx
    {
        static readonly bool   IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        static readonly byte[] Yes       = { 1 };

        public static Socket FastSocket(Socket socket)
        {
            CheckDebug.NotNull(socket);

            socket.NoDelay = true;

            if (IsWindows)
            {
                unchecked
                {
                    try
                    {
                        // defined in `mstcpip.h`
                        const int SIO_LOOPBACK_FAST_PATH = (int)0x98000010;
                        socket.IOControl(SIO_LOOPBACK_FAST_PATH, optionInValue: Yes, optionOutValue: null);
                    }
                    catch (Exception e)
                    {
                        Kon.DebugException(e);
                    }
                }
            }

            return socket;
        }
    }
}
