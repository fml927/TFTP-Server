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
    class RequestHandler
    {
        private UdpClient _client;
        private IPEndPoint _endPoint;
        private string _filename;
        private byte[] _request;

        public RequestHandler(IPAddress address, int port, byte[] request)
        {
            _client = new UdpClient(0);
            _endPoint = new IPEndPoint(address, port);
            _request = request;
            _filename = Helpers.GetString(request);
            string[] rawSplit = _filename.Split(new char[] { (char)0 });
            _filename = rawSplit[1].Substring(1);
            //check to see if read/write request is valid
            switch (checkReadWrite(request))
            {
                case Constants.OpCode.Read:
                    read();
                    break;
                case Constants.OpCode.Write:
                    write();
                    break;
                case Constants.OpCode.Error:
                    transmitError(Constants.ErrorCode.UnknownTransferID, "Invalid request");
                    break;
            }
            _client.Close();
            
        }
        private void transmitError(Constants.ErrorCode error, string message)
        {
            byte[] messageBytes;
            byte[] toSend;

            messageBytes = Helpers.GetBytes(message);

            toSend = new byte[messageBytes.Length + 5];
            toSend[0] = 0;
            toSend[1] = (byte)Constants.OpCode.Error;
            toSend[2] = 0;
            toSend[3] = (byte)error;
            Array.Copy(messageBytes, 0, toSend, 4, messageBytes.Length);
            toSend[toSend.Length - 1] = 0;

            _client.Send(toSend, toSend.Length, _endPoint);
        }
        private void transmitAwk(int block)
        {
            byte[] toSend = new byte[4];
            toSend[0] = 0;
            toSend[1] = (byte)Constants.OpCode.Acknowledge;
            toSend[2] = (byte)(block >> 8);
            toSend[3] = (byte)block;
            _client.Send(toSend, 4, _endPoint);
            Console.WriteLine(toSend[0] + " " + toSend[1] + " " + toSend[2] + " " + toSend[3] + " " + _endPoint.Address + " " + _endPoint.Port);
        }
        private Constants.OpCode checkReadWrite(byte[] bytes)
        {
            switch (bytes[1])
            {
                case 1: return Constants.OpCode.Read;
                case 2: return Constants.OpCode.Write;
                default: return Constants.OpCode.Error;
            }
        }
        private bool confirmAwk(byte[] input, short block)
        {
            //Console.WriteLine(Helpers.GetString(Helpers.SubArray<byte>(input,2,input.Length-2)));
            if ((input.Length == 4)
                && (input[0] == 0)
                && (input[1] == (byte)Constants.OpCode.Acknowledge)
                && ((short)((input[2] << 8) + input[3]) == block))
            {
                return true;
            }

            return false;
        }
        //TODO take care of the unexpected port error
        private void read()
        {
            Console.WriteLine(_endPoint.Address + " " + _endPoint.Port);
            Console.WriteLine(Helpers.GetString(_request));
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
                transmitError(Constants.ErrorCode.FileNotFound, e.Message);
                return;
            }
            catch (UnauthorizedAccessException e)
            {
                transmitError(Constants.ErrorCode.AccessViolation, e.Message);
                return;
            }
            catch (Exception e)
            {
                transmitError(Constants.ErrorCode.NotDefined, e.Message);
                return;
            }
            int temp = 0;
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
                byte[] header = { 0, (byte)Constants.OpCode.Data, (byte)(block >> 8), (byte)block };
                byte[] data = Helpers.SubArray<byte>(toSend, (block-1) * 512, byteLength);
                byte[] send = new byte[header.Length + data.Length];
                header.CopyTo(send, 0);
                data.CopyTo(send, 4);
                _client.Send(send, byteLength + 4, _endPoint);
                if (temp == 0)
                {
                    Console.WriteLine(Helpers.GetString(send));
                    Console.WriteLine("Local endpoint: " + _client.Client.LocalEndPoint.ToString());
                    //Console.WriteLine("Remote endpoint: " + client.Client.RemoteEndPoint.ToString());
                }
                input = _client.Receive(ref _endPoint);
                if (temp == 0)
                {
                    Console.WriteLine(_endPoint.Address + " " + _endPoint.Port);
                    Console.WriteLine((short)input[3]);
                    Console.WriteLine(Helpers.GetString(input));
                }

                //TODO check for timeout
                if (confirmAwk(input, block)) block++;
                temp++;
            }
        }

        private void write()
        {
            if (File.Exists(_filename))
            {
                transmitError(Constants.ErrorCode.FileAlreadyExists, "File already exists");
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
                    Console.WriteLine(Helpers.GetString(input));
                    //validate data block here?
                    rawFile.AddRange(Helpers.SubArray<Byte>(input, 4, input.Length - 4));
                    input = _client.Receive(ref _endPoint);
                    Console.WriteLine(_client.Client.LocalEndPoint);
                    //need to timeout and retransmit here
                }
                //last block
                transmitAwk(block++);
                Console.WriteLine(Helpers.GetString(input));
                //validate data block here?
                rawFile.AddRange(Helpers.SubArray<Byte>(input, 4, input.Length - 4));
                try
                {
                    File.WriteAllBytes(_filename, rawFile.ToArray());
                }
                catch (UnauthorizedAccessException e)
                {
                    transmitError(Constants.ErrorCode.AccessViolation, e.Message);
                }
                catch (Exception e)
                {
                    transmitError(Constants.ErrorCode.NotDefined, e.Message);
                }
            }
        }
    }
}
