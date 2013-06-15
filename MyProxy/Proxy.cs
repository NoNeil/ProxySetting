using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyProxy
{
    public class Proxy
    {
        public string name;
        public string ip;
        public string port;

        public void Init(string theName, string theIp, string thePort)
        {
            name = theName;
            ip = theIp;
            port = thePort;
        }

    }
}
