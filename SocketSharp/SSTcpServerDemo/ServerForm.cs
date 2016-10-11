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
using System.Threading;
using SocketSharp;

namespace SSTcpServer
{
    public partial class ServerForm : Form
    {
        private delegate void MyInvoke();

        public ServerForm()
        {
            InitializeComponent();
        }

        private void ServerForm_Load(object sender, EventArgs e)
        {
            //
        }

        private void Accept(IAsyncResult result)
        {
            //
        }

        private void Receive(IAsyncResult result)
        {
            //
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

        private void btnStartServer_Click(object sender, EventArgs e)
        {
            TcpServer tcp = new TcpServer();
            tcp.OnSessionConnected += new TcpServer.SessionHandler<Socket>(tcp_OnSessionConnected);
            tcp.Run();
        }

        void tcp_OnSessionConnected(Socket target)
        {
            IntPtr handle = target.Handle;
        }

        private void btnStopServer_Click(object sender, EventArgs e)
        {
            //
        }
    }
}
