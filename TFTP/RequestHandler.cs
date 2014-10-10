using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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
        public RequestHandler(IPAddress address, int port, byte[] request)
        {
            //create UdpClient and set timeout interval
            _client = new UdpClient(0);
            _client.Client.ReceiveTimeout = Constant.timeout;
            _client.Client.SendTimeout = Constant.timeout;
            _endPoint = new IPEndPoint(address, port);
            _filename = Helper.GetString(request);
            string[] rawSplit = _filename.Split(new char[] { (char)0 });
            _filename = rawSplit[1].Substring(1);

            //check to see if read/write request is valid
            //TODO printing error conditions will be weird
            switch ((Constant.OpCode)request[1])
            {
                case Constant.OpCode.Read:
                    Console.WriteLine("Sent file \""+_filename+"\" ("+read()+" bytes)");
                    break;
                case Constant.OpCode.Write:
                    Console.WriteLine("Received file \""+_filename+"\" ("+write()+" bytes)");
                    break;
                case Constant.OpCode.Error:
                    transmitError(Constant.ErrorCode.UnknownTransferID, "Invalid request");
                    break;
            }
            _client.Close();
            
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
        /// <returns></returns>
        private int read()
        {
            int byteCount = 0;
            int timeouts = 0;
            byte[] toSend;
            byte[] input;
            short block = 1;

            //try to open file
            try
            {
                toSend = File.ReadAllBytes(_filename);
            }
            catch (FileNotFoundException e)
            {
                transmitError(Constant.ErrorCode.FileNotFound, e.Message);
                return 0;
            }
            catch (UnauthorizedAccessException e)
            {
                transmitError(Constant.ErrorCode.AccessViolation, e.Message);
                return 0;
            }
            catch (Exception e)
            {
                transmitError(Constant.ErrorCode.NotDefined, e.Message);
                return 0;
            }
            while ((block-1) * 512 < toSend.Length)
            {
                int byteLength;
                //check if last block
                if (toSend.Length - (block-1) * 512 < 512)
                {
                    byteLength = toSend.Length - (block - 1) * 512;
                }
                else
                {
                    byteLength = 512;
                }
                byteCount += byteLength;
                byte[] header = { 0, (byte)Constant.OpCode.Data, (byte)(block >> 8), (byte)block };
                byte[] data = Helper.SubArray<byte>(toSend, (block-1) * 512, byteLength);
                byte[] send = new byte[header.Length + data.Length];
                header.CopyTo(send, 0);
                data.CopyTo(send, 4);
                _client.Send(send, byteLength + 4, _endPoint);
                input = _client.Receive(ref _endPoint);

                //TODO check for timeout

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
        /// <returns></returns>
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
                transmitAwk(block++);
                byte[] input = _client.Receive(ref _endPoint);
                List<byte> rawFile = new List<byte>();

                while (input.Length == 516)
                {
                    transmitAwk(block++);
                    //validate data block here?
                    rawFile.AddRange(Helper.SubArray<Byte>(input, 4, input.Length - 4));
                    byteCount += input.Length - 4;
                    input = _client.Receive(ref _endPoint);
                    //need to timeout and retransmit here
                }
                //last block
                transmitAwk(block++);
                //validate data block here?
                rawFile.AddRange(Helper.SubArray<Byte>(input, 4, input.Length - 4));
                byteCount += input.Length - 4;
                try
                {
                    File.WriteAllBytes(_filename, rawFile.ToArray());
                }
                catch (UnauthorizedAccessException e)
                {
                    transmitError(Constant.ErrorCode.AccessViolation, e.Message);
                }
                catch (Exception e)
                {
                    transmitError(Constant.ErrorCode.NotDefined, e.Message);
                }
            }
            return byteCount;
        }
    }
}
