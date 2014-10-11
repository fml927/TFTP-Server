using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TFTP
{
    /// <summary>
    /// This class handles one TFTP request
    /// </summary>
    class RequestHandler
    {
        private UdpClient _client;
        private IPEndPoint _endPoint;
        private string _filename;

        /// <summary>
        /// Construct and start RequestHandler
        /// </summary>
        /// <param name="address">Client's IP address</param>
        /// <param name="port">Client's port</param>
        /// <param name="request">Byte array containing initial request</param>
        public RequestHandler(IPAddress address, int port, byte[] request, Semaphore semaphore)
        {
            semaphore.WaitOne();
            //create UdpClient and set timeout interval
            _client = new UdpClient(0);
            _client.Client.ReceiveTimeout = Constant.timeout;
            _client.Client.SendTimeout = Constant.timeout;
            _endPoint = new IPEndPoint(address, port);
            _filename = Helper.GetString(request);
            string[] rawSplit = _filename.Split(new char[] { (char)0 });
            _filename = rawSplit[1].Substring(1);
            int bytes;

            switch ((Constant.OpCode)request[1])
            {
                case Constant.OpCode.Read:
                    Console.WriteLine(address.ToString()+": get "+_filename);
                    bytes = read();
                    if (bytes != -1) Console.WriteLine("Sent file \"" + _filename + "\" (" + bytes + " bytes)");
                    break;
                case Constant.OpCode.Write:
                    Console.WriteLine(address.ToString() + ":S put " + _filename);
                    bytes = write();
                    if (bytes != -1) Console.WriteLine("Received file \"" + _filename + "\" (" + bytes + " bytes)");
                    break;
                case Constant.OpCode.Error:
                    transmitError(Constant.ErrorCode.UnknownTransferID, "Invalid request");
                    break;
            }
            _client.Close();
            semaphore.Release();
        }

        /// <summary>
        /// Sends an error packet to the client
        /// </summary>
        /// <param name="error">Error code</param>
        /// <param name="message">Explanatory message for end user</param>
        private void transmitError(Constant.ErrorCode error, string message)
        {
            byte[] messageBytes;
            byte[] toSend;

            messageBytes = Helper.GetBytes(message);

            toSend = new byte[messageBytes.Length + 5];

            //create header
            toSend[0] = 0;
            toSend[1] = (byte)Constant.OpCode.Error;
            toSend[2] = 0;
            toSend[3] = (byte)error;
            
            //concatinate header and message
            Array.Copy(messageBytes, 0, toSend, 4, messageBytes.Length);
            
            //set trailing byte
            toSend[toSend.Length - 1] = 0;
            _client.Send(toSend, toSend.Length, _endPoint);
            Console.WriteLine("Error " + error + ": " + message);
        }

        /// <summary>
        /// Transmits acknowledgement to client
        /// </summary>
        /// <param name="block">Block number of packet being acknowledged</param>
        private void transmitAwk(int block)
        {
            byte[] toSend = new byte[4];
            toSend[0] = 0;
            toSend[1] = (byte)Constant.OpCode.Acknowledge;
            toSend[2] = (byte)(block >> 8);
            toSend[3] = (byte)block;
            _client.Send(toSend, 4, _endPoint);
        }

        /// <summary>
        /// Handles read request from client
        /// </summary>
        /// <returns>Number of bytes sent, or -1 for error cases</returns>
        private int read()
        {
            int byteCount = 0;
            int timeouts = 0;
            byte[] byteFile;
            byte[] input;
            ushort block = 1;

            //try to open file
            try
            {
                byteFile = File.ReadAllBytes(_filename);
            }
            catch (FileNotFoundException e)
            {
                transmitError(Constant.ErrorCode.FileNotFound, e.Message);
                return -1;
            }
            catch (UnauthorizedAccessException e)
            {
                transmitError(Constant.ErrorCode.AccessViolation, e.Message);
                return -1;
            }
            catch (Exception e)
            {
                transmitError(Constant.ErrorCode.NotDefined, e.Message);
                return -1;
            }
            //if filesize is > 32MB
            if (byteFile.Length > 33554432)
            {
                transmitError(Constant.ErrorCode.IllegalOpeation, "File is to large.  File size: " + byteFile.Length + " bytes, max TFTP transfer size is 33554432 bytes");
                return -1;
            }
            while ((block-1) * 512 < byteFile.Length)
            {
                int byteLength;

                //check if last block
                if (byteFile.Length - (block-1) * 512 < 512)
                {
                    byteLength = byteFile.Length - (block - 1) * 512;
                }
                else
                {
                    byteLength = 512;
                }
                byteCount += byteLength;
                byte[] send = new byte[byteLength + 4];
                send[0] = 0;
                send[1] = (byte)Constant.OpCode.Data;
                send[2] = (byte)(block >> 8);
                send[3] = (byte)block;
                for(int i=4; i<send.Length; i++)
                {
                    send[i] = byteFile[(block - 1) * 512 + i-4];
                }
                do
                {
                    _client.Send(send, byteLength + 4, _endPoint);
                    input = null;
                    input = _client.Receive(ref _endPoint);
                    timeouts++;
                }
                while (input == null && timeouts <= Constant.maxTimouts);
                
                //connection timedout
                if(input == null)
                {
                    Console.WriteLine("Connection timed out");
                    return -1;
                }

                //Confirm acknowledgement packet
                if ((input.Length == 4)
                    && (input[0] == 0)
                    && (input[1] == (byte)Constant.OpCode.Acknowledge)
                    && ((short)((input[2] << 8) + input[3]) == block))
                {
                    block++;
                }
                       
            }
            return byteCount;
        }

        /// <summary>
        /// Handles write request from client
        /// </summary>
        /// <returns>Number of bytes received, or -1 for error cases</returns>
        private int write()
        {
            int byteCount = 0;
            int timeouts = 0;
            if (File.Exists(_filename))
            {
                transmitError(Constant.ErrorCode.FileAlreadyExists, "File already exists");
            }
            else
            {
                short block = 0;

                byte[] input = null;
                List<byte> rawFile = new List<byte>();

                //initial acknowledgment
                transmitAwk(block++);
                do
                {
                    do
                    {
                        input = null;
                        input = _client.Receive(ref _endPoint);
                        timeouts++;
                        transmitAwk(block++);
                    }
                    while (input == null && timeouts <= Constant.maxTimouts);

                    //connection timedout
                    if (input == null)
                    {
                        Console.WriteLine("Connection timedout");
                        return -1;
                    }
                    rawFile.AddRange(Helper.SubArray<Byte>(input, 4, input.Length - 4));
                    byteCount += input.Length - 4;
                }
                while (input.Length == 516);

                try
                {
                    File.WriteAllBytes(_filename, rawFile.ToArray());
                }
                catch (UnauthorizedAccessException e)
                {
                    transmitError(Constant.ErrorCode.AccessViolation, e.Message);
                    return -1;
                }
                catch (Exception e)
                {
                    transmitError(Constant.ErrorCode.NotDefined, e.Message);
                    return -1;
                }
            }
            return byteCount;
        }
    }
}
