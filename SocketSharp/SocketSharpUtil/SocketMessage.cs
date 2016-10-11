using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SocketSharpUtil
{
    /// <summary>
    /// socket信息
    /// author:chaix
    /// date:2016-09-12
    /// </summary>
    public class SocketMessage
    {
        public ClientInfo Client { get; set; }
        public string Message { get; set; }
        public DateTime Time { get; set; }
    }
}
