using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using SocketSharpUtil;

namespace SocketSharp
{
    public class TcpServer
    {
        #region 字段
        private object _lockAcceptObj = new object();
        private object _lockReceiveObj = new object();
        private object _lockSendObj = new object();
        private Dictionary<Socket, ClientInfo> _clientPool = new Dictionary<Socket, ClientInfo>();
        private List<SocketMessage> _msgPool = new List<SocketMessage>();
        private bool _isClearMsgPool = true;  //是指消息池是否为空
        #endregion

        #region 属性
        private int _port = 9000;
        /// <summary>
        /// 端口号
        /// </summary>
        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }

        private string _host = "127.0.0.1";
        /// <summary>
        /// 主机ip地址
        /// </summary>
        public string Host
        {
            get { return _host; }
            set { _host = value; }
        }
        #endregion

        #region 委托
        public delegate void SessionHandler(object sender, EventArgs e);
        public delegate void SessionHandler<T>(T target);
        public delegate void SessionHandler<T, TParam>(T target, TParam tParam);
        #endregion

        #region 事件
        public event SessionHandler OnChange;
        public event SessionHandler<Socket> OnSessionConnected;
        public event SessionHandler<Socket> OnSessionClosed;
        public event SessionHandler<Socket, string> OnMessageReceived;
        public event SessionHandler<Socket, byte[]> OnDataBufferReceived;
        #endregion

        #region 虚拟函数
        protected virtual void OnChanged(EventArgs e) { }
        #endregion

        #region 公共方法
        /// <summary>
        /// 启动服务器
        /// </summary>
        /// <param name="port">端口号默认9000</param>
        public void Run()
        {
            Thread serverSocketThread = new Thread(() =>
            {
                Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                server.Bind(new IPEndPoint(IPAddress.Any, _port));
                server.Listen(10);
                server.BeginAccept(new AsyncCallback(Accept), server);
            });

            serverSocketThread.Start();
        }

        /// <summary>
        /// 启动服务器
        /// </summary>
        /// <param name="host">ip</param>
        /// <param name="port">端口号</param>
        public void Run(string host, int port)
        {
            if (string.IsNullOrEmpty(host))
                host = _host;
            if (port <= 0)
                port = _port;

            Thread serverSocketThread = new Thread(() =>
            {
                Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress ip = IPAddress.Parse(host);
                server.Bind(new IPEndPoint(ip, port));
                server.Listen(10);
                server.BeginAccept(new AsyncCallback(Accept), server);
            });

            serverSocketThread.Start();
        }

        /// <summary>
        /// 广播
        /// </summary>
        public void Broadcast()
        {
            Thread broadcastThread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        if (!_isClearMsgPool)
                        {
                            //表示消息池不为空，可以广播消息
                            byte[] msg = Encoding.UTF8.GetBytes(_msgPool[0].Message);
                            foreach (KeyValuePair<Socket, ClientInfo> node in _clientPool)
                            {
                                Socket client = node.Key;
                                if (client.Poll(20, SelectMode.SelectWrite))
                                {
                                    client.Send(msg, msg.Length, SocketFlags.None);
                                    //Console.WriteLine("广播消息已发送...");
                                }
                            }

                            _msgPool.RemoveAt(0);
                            _isClearMsgPool = _msgPool.Count == 0 ? true : false;
                        }
                        else
                        { 
                            //消息池中没有消息了，
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Broadcast Error：" + ex.Message);
                    }
                }
            });

            broadcastThread.Start();
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 发送消息给指定对象
        /// </summary>
        private void Emit(Socket client, SocketMessage sm)
        {
            Thread emitThread = new Thread(() =>
            {
                try
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(sm.Message);
                    if (client.Poll(20, SelectMode.SelectWrite))
                    {
                        client.Send(buffer, buffer.Length, SocketFlags.None);
                        Console.WriteLine("发送消息给指定用户...");
                    }

                    //_msgPool.Remove(sm);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Emit Error：" + ex.Message);
                }
            });

            emitThread.Start();
        }

        /// <summary>
        /// 处理客户端连接请求,成功后把客户端加入到clientPool中
        /// </summary>
        /// <param name="result"></param>
        private void Accept(IAsyncResult result)
        {
            //监视器
            Monitor.Enter(_lockAcceptObj);
            Socket server = result.AsyncState as Socket;
            Socket client = server.EndAccept(result);
            try
            {
                //处理下一个客户端连接
                server.BeginAccept(new AsyncCallback(Accept), server);
                byte[] buffer = new byte[1024];
                //接收客户端消息
                client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(Receive), client);
                //把客户端存入clientPool
                ClientInfo info = new ClientInfo();
                info.Id = client.RemoteEndPoint;
                info.Handle = client.Handle;
                info.Buffer = buffer;
                info.IsConnected = true;  //表示客户端已经连接
                _clientPool.Add(client, info);
                //Console.WriteLine(string.Format("Client {0} connecting", client.RemoteEndPoint));
                if (OnSessionConnected != null)
                    OnSessionConnected(client);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Accept Error :" + ex.ToString());
            }
            Monitor.Exit(_lockAcceptObj);
        }

        /// <summary>
        /// 接收客户端发送的消息，接收成功后加入到_msgPool，等待广播或者发送给指定对象
        /// </summary>
        /// <param name="result"></param>
        private void Receive(IAsyncResult result)
        {
            Monitor.Enter(_lockReceiveObj);
            Socket client = result.AsyncState as Socket;
            if (result == null)
                return;

            try
            {
                int length = client.EndReceive(result);
                byte[] buffer = _clientPool[client].Buffer;

                //接收消息
                client.BeginReceive(buffer, 0, length, SocketFlags.None, new AsyncCallback(Receive), client);
                if (length <= 0)
                    return;

                //判断是否已经连接
                if (!_clientPool[client].IsConnected)
                {
                    return;
                }

                SocketMessage sm = new SocketMessage();
                sm.Client = _clientPool[client];
                sm.Time = DateTime.Now;
                sm.Message = Encoding.UTF8.GetString(buffer);
                _msgPool.Add(sm);
                _isClearMsgPool = false;

                if (OnMessageReceived != null)
                    OnMessageReceived(client, sm.Message);

                if (OnDataBufferReceived != null)
                    OnDataBufferReceived(client, buffer);

                //处理客户端发过来的消息，处理完毕之后返回给客户端
                //buffer = GetDataPackage(buffer);
                //client.Send(buffer, SocketFlags.None);
                //client.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(Send), client);
            }
            catch (Exception)
            {
                Disconnect(client);
            }
            Monitor.Exit(_lockReceiveObj);
        }

        /// <summary>
        /// 发送数据到客户端
        /// </summary>
        /// <param name="result"></param>
        private void Send(IAsyncResult result)
        {
            Monitor.Enter(_lockSendObj);
            Socket client = result.AsyncState as Socket;
            if (result == null)
                return;

            try
            {
                int length = client.EndSend(result);
                byte[] buffer = new byte[client.SendBufferSize];
                client.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(Send), client);
            }
            catch (Exception)
            {
                
                throw;
            }
            Monitor.Exit(_lockSendObj);
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <param name="client"></param>
        private void Disconnect(Socket client)
        {
            if (client.Connected)
            {
                client.Disconnect(true);
                _clientPool.Remove(client);
                Console.WriteLine("Client {0} disconnect", _clientPool[client].Name);
                if (OnSessionClosed != null)
                    OnSessionClosed(client);
            }
        }

        /// <summary>
        /// 数据处理
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private byte[] GetDataPackage(byte[] buffer)
        {
            byte[] dataBuffer = null;
            try
            {
                string msg = Encoding.UTF8.GetString(buffer);

                dataBuffer = Encoding.UTF8.GetBytes(msg);
            }
            catch (Exception)
            {
                //
            }
            return dataBuffer;
        }
        #endregion
    }
}
