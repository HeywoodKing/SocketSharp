using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace SSTcpClient
{
    public partial class ClientForm : Form
    {
        private delegate void MyInvoke();
        private Socket _clientSocket = null;

        public ClientForm()
        {
            InitializeComponent();
        }

        private void ClientForm_Load(object sender, EventArgs e)
        {
            //
        }

        private void ConnectServer()
        {
            int port = 5555;  //监听端口号
            string host = "192.168.10.253";  //连接服务端IP
            IPAddress ip = IPAddress.Parse(host);  //将IP地址转换为IP实例
            IPEndPoint ipe = new IPEndPoint(ip, port);  //将网络端点表示为 IP 地址和端口号

            UpdateUI("正在连接服务器...");
            //建立客户端Socket
            _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //客户端开始连接服务端
            _clientSocket.Connect(ipe);
            UpdateUI("已连接到服务器");
        }

        private void UpdateUI(string value)
        {
            MyInvoke myInvoke = delegate
            {
                string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                listBox1.Items.Add(date + "：" + value);
            };
            this.Invoke(myInvoke);
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            //向服务器发送消息
            string sendMsg = txtSendMsg.Text.Trim();

            if (string.IsNullOrEmpty(sendMsg))
                return;

            if (_clientSocket == null)
                return;

            byte[] sendBuffer = Encoding.ASCII.GetBytes(sendMsg);
            _clientSocket.Send(sendBuffer);
            UpdateUI(sendMsg);

            //接收来自服务器的消息
            string receMsg = string.Empty;
            byte[] receBuffer = new byte[4096];
            int bytes = _clientSocket.Receive(receBuffer, receBuffer.Length, 0);

            receMsg += Encoding.ASCII.GetString(receBuffer);
            UpdateUI(string.Format("来自服务端的回应:{0}", receMsg));
            //_clientSocket.Close();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            ConnectServer();
        }
    }
}
