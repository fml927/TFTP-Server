using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace TFTP
{
    class Server
    {


        private bool _running;
        private IPEndPoint _endPoint;
        private Semaphore _semaphore;
        private UdpClient _listener;

        //need to figure out the port thing.  Do I access connections on all ports?  only port 69?
        public Server()
        {
            _endPoint = new IPEndPoint(IPAddress.Any,69);
            _running = true;
            //selected 25 arbitrarially
            _semaphore = new Semaphore(25, 25);
            _listener = new UdpClient(_endPoint);
            loop();
        }
        
        private void loop()
        {
            while(_running)
            {
                
                //I think this blocks, need to check to make sure
                byte[] bytes = _listener.Receive(ref _endPoint);
                Thread t = new Thread(new ParameterizedThreadStart(handleClient));
                t.IsBackground = true;
                t.Start(new HandleParams { Bytes = bytes, Address = _endPoint.Address, Port = _endPoint.Port });
            }
        }
        private void handleClient(object o)
        {
            HandleParams param = (HandleParams)o;
            UdpClient client = new UdpClient(0);
            IPEndPoint endPoint = new IPEndPoint(param.Address, param.Port);
            _semaphore.WaitOne();
            //TODO this will probably throw an exception
            string filename = Helpers.GetString(param.Bytes);
            string[] rawSplit = filename.Split(new char[] { (char)0 });
            filename = rawSplit[1].Substring(1);
            //check to see if read/write request is valid
            switch(checkReadWrite(param.Bytes))
            {
                case Constants.OpCode.Read:
                    read(client, endPoint, filename);
                    break;
                case Constants.OpCode.Write:
                    write(client, endPoint, filename);
                    break;
                case Constants.OpCode.Error:
                    transmitError(client, endPoint, Constants.ErrorCode.UnknownTransferID, "Invalid request");
                    break;
            }
            client.Close();
            _semaphore.Release();
        }
        private void transmitError(UdpClient client, IPEndPoint endPoint, Constants.ErrorCode error, string message)
        {
            byte[] messageBytes;
            byte[] toSend;

            messageBytes = Helpers.GetBytes(message);

            toSend = new byte[messageBytes.Length+5];
            toSend[0] = 0;
            toSend[1] = (byte)Constants.OpCode.Error;
            toSend[2] = 0;
            toSend[3] = (byte)error;
            Array.Copy(messageBytes, 0, toSend, 4, messageBytes.Length);
            toSend[toSend.Length - 1] = 0;

            client.Send(toSend,toSend.Length,endPoint);
        }

        private void transmitAwk(UdpClient client, IPEndPoint endPoint, int block)
        {
            byte[] toSend = new byte[4];
            toSend[0] = 0;
            toSend[1] = (byte)Constants.OpCode.Acknowledge;
            Array.Copy(BitConverter.GetBytes(block),0,toSend,2,2);
            client.Send(toSend,4,endPoint);
        }
        private Constants.OpCode checkReadWrite(byte[] bytes)
        {
            switch(bytes[1])
            {
                case 1: return Constants.OpCode.Read;
                case 2: return Constants.OpCode.Write;
                default: return Constants.OpCode.Error;
            }
        }

        private bool confirmAwk(byte[] input, short block)
        {
            if((input.Length == 4)
                && (input[0] == 0)
                && (input[1] == (byte)Constants.OpCode.Acknowledge)
                && (BitConverter.ToInt16(input,2) == block))
            {
                return true;
            }

            return false;
        }

        private void read(UdpClient client, IPEndPoint endPoint, string filename)
        {
            byte[] toSend;
            byte[] input;
            short block = 0;
            //try to open file
            try
            {
                toSend = File.ReadAllBytes(filename);
            }
            catch(FileNotFoundException e)
            {
                transmitError(client, endPoint, Constants.ErrorCode.FileNotFound, e.Message);
                return;
            }
            catch(UnauthorizedAccessException e)
            {
                transmitError(client, endPoint, Constants.ErrorCode.AccessViolation, e.Message);
                return;
            }
            catch(Exception e)
            {
                transmitError(client, endPoint, Constants.ErrorCode.NotDefined, e.Message);
                return;
            }
            while(block*512 < toSend.Length)
            {
                int byteLength;
                //check if last block
                if(toSend.Length - block < 512)
                {
                    byteLength = toSend.Length - block * 512;
                }
                else
                {
                    byteLength = 512;
                }
                client.Send(Helpers.SubArray<byte>(toSend, block * 512, byteLength), byteLength);
                input = client.Receive(ref _endPoint);
                //TODO check for timeout
                if (confirmAwk(input,block)) block++;
            }
        }

        private void write(UdpClient client, IPEndPoint endPoint, string filename)
        {
            if (File.Exists(filename))
            {
                transmitError(client, endPoint, Constants.ErrorCode.FileAlreadyExists, "File already exists");
            }
            else
            {
                short block = 0;
                transmitAwk(client, endPoint, block);
                List<byte> rawFile = new List<byte>();
                byte[] input = client.Receive(ref _endPoint);
                while (input.Length == 516)
                {
                    //validate data block here?
                    transmitAwk(client, endPoint, block);
                    rawFile.AddRange(input);
                    input = client.Receive(ref _endPoint);
                    //need to timeout and retransmit here
                }
                try
                {
                    File.WriteAllBytes(filename, rawFile.ToArray());
                }
                catch (UnauthorizedAccessException e)
                {
                    transmitError(client, endPoint, Constants.ErrorCode.AccessViolation, e.Message);
                }
                catch (Exception e)
                {
                    transmitError(client, endPoint, Constants.ErrorCode.NotDefined, e.Message);
                }
            }
        }
    }
}
