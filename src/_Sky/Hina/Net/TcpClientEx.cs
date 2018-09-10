using System.Net.Sockets;
using System.Threading.Tasks;

// csharp: hina/linq/tcpclientex.cs [snipped]
namespace Hina.Net
{
    static class TcpClientEx
    {
        public static async Task<TcpClient> ConnectAsync(string host, int port, bool exclusiveAddressUse = true)
        {
            var x = new TcpClient() { NoDelay = true, ExclusiveAddressUse = exclusiveAddressUse };

            SocketEx.FastSocket(x.Client);
            await x.ConnectAsync(host, port);

            return x;
        }
    }
}
