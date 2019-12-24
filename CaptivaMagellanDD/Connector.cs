using System;
using System.Xml;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CaptivaMagellan
{
    public class Connector
    {
        #region Fields

        private string _host;
        private int _port;

        #endregion

        /// <summary>
        /// Connector to TME
        /// </summary>
        /// <param name="host">Host name</param>
        /// <param name="port">Port number</param>
        public Connector(string host, int port)
        {
            _host = host;
            _port = port;
        }

        /// <summary>
        /// Send XML request to TME
        /// </summary>
        /// <param name="request">XML request</param>
        /// <returns>XML response</returns>
        public string Process(string request)
        {
            if (request == null)
                return null;
            byte[] data = Encoding.UTF8.GetBytes(request);
            int length = data.Length;
            if (length == 0)
                return null;
            TcpClient client = new TcpClient(_host, _port);
            try
            {
                NetworkStream stream = client.GetStream();
                // Header (4 bytes little-endian data length)
                Byte[] header = new byte[4];
                header[3] = (Byte)((length & 0xFF000000) >> 24);
                header[2] = (Byte)((length & 0xFF0000) >> 16);
                header[1] = (Byte)((length & 0xFF00) >> 8);
                header[0] = (Byte)(length & 0xFF);
                stream.Write(header, 0, header.Length);
                stream.Write(data, 0, length);
                stream.Flush();
                // Retrieve response length
                length = stream.Read(header, 0, header.Length);
                if (length < 4)
                    throw (new Exception("Invalid response from TME."));
                length = (header[0] & 0xFF) | ((header[1] & 0xFF) << 8) | ((header[2] & 0xFF) << 16) | ((header[3] & 0xFF) << 24);
                if (length <= 0)
                    return null;
                // Retrieve response content
                data = new byte[length];
                for (int pos = 0, size = 0; length > 0; pos += size, length -= size)
                {
                    size = stream.Read(data, pos, length);
                    if (size == 0)
                        throw (new Exception("Invalid response from TME."));
                }
                return Encoding.UTF8.GetString(data);
            }
            finally
            {
                client.Close();
            }
        }
    }
}
