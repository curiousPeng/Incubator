using System;
using System.Net;

namespace Incubator.SocketClient.Rpc
{
    public sealed class TcpProxy
    {
        public static TInterface CreateProxy<TInterface>(IPEndPoint endpoint) where TInterface : class
        {
            return ProxyFactory.CreateProxy<TInterface>(typeof(RpcClient2), typeof(IPEndPoint), endpoint);
        }
    }
}
