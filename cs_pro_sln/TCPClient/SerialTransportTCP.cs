using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace TCPClient
{
    /// <summary>
    /// Serial transport TCP framework calls
    /// </summary>
    public class SerialTransportTCP
    {
        private TcpClient clientSocket = null;
        NetworkStream serverStream = null;
        /// <summary>
        /// host name
        /// </summary>
        private string ip = string.Empty;
        private int port = 8086;

        /// <summary>
        /// Constructor
        /// Initialize native serial Tcp port
        /// </summary>
        public SerialTransportTCP(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
            clientSocket = new TcpClient();
        }
        
        
        /// <summary>
        /// Gets a value indicating the open or closed status of the TCP Port object.
        /// </summary>
        public bool IsOpen
        {
            get
            {
                return clientSocket.Connected;

            }
        }

        /// <summary>
        /// Opens a TCP connection.
        /// </summary>
        public void Open()
        {
            if (!clientSocket.Connected)
            {
                 clientSocket.Connect(ip,port);
                 serverStream = clientSocket.GetStream();
            }
        }
        
        /// <summary>
        /// Reads a number of bytes from the Tcp SerialPort input buffer and writes those 
        /// bytes into a byte array at the specified offset.
        /// </summary>
        /// <param name="length">The number of bytes to read.</param>
        /// <param name="messageSpace">The byte array to write the input to.</param>
        /// <param name="offset">The offset in the buffer array to begin writing.</param>
        /// <returns>The number of bytes read</returns>
        public int ReceiveBytes(int length, byte[] messageSpace, int offset)
        {
            return serverStream.Read(messageSpace, offset, length);
        }
        /// <summary>
        /// Writes a specified number of bytes to the tcp serial port using data from a buffer.
        /// </summary>
        /// <param name="length">The number of bytes to write.</param>
        /// <param name="message">The byte array that contains the data to write to the port.</param>
        /// <param name="offset">The zero-based byte offset in the buffer parameter at which to begin copying bytes to the port.</param>
         public void SendBytes(int length, byte[] message, int offset)
        {
            serverStream.Write(message, offset, length);
        }
        /// <summary>
        /// Closes the port connection,
        /// </summary>
         public void Shutdown()
        {
            serverStream.Close();
            clientSocket.Close();
        }
        /// <summary>
        /// Gets or sets the number of milliseconds before a time-out occurs when
        /// a write operation does not finish.
        /// </summary>
        public int WriteTimeout
        {
            get
            {
                return serverStream.WriteTimeout;
            }
            set
            {
                serverStream.WriteTimeout = value;
            }
        }
        /// <summary>
        /// Gets or sets the number of milliseconds before a time-out occurs when
        /// a read operation does not finish.
        /// </summary>
         public int ReadTimeout
        {
            get
            {
                return serverStream.ReadTimeout;
            }
            set
            {
                serverStream.ReadTimeout = value;
            }
        }
    }
}
