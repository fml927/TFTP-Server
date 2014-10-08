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

        //need to figure out the port thing.  Do I access connections on all ports?  only port 69?
        public Server(int port)
        {
            _endPoint = new IPEndPoint(IPAddress.Any,port);
            _running = true;
            //selected 25 arbitrarially
            _semaphore = new Semaphore(0, 25);
            loop();
        }
        
        private void loop()
        {
            while(_running)
            {
                UdpClient newClient = new UdpClient(_endPoint);
                //I think this blocks, need to check to make sure
                byte[] bytes = newClient.Receive(ref _endPoint);
                Thread t = new Thread(new ParameterizedThreadStart(handleClient));
                t.Start(new HandleParams { client = newClient, bytes = bytes });
            }
        }
        private void handleClient(object o)
        {
            HandleParams param = (HandleParams)o;
            _semaphore.WaitOne();
            //check to see if read/write request is valid
            switch(checkReadWrite(param.bytes))
            {
                case Constants.RequstType.Read:
                    read(param.client);
                    break;
                case Constants.RequstType.Write:
                    write(param.client);
                    break;
                case Constants.RequstType.Invalid:
                    transmitError(param.client, Constants.ErrorCode.UnknownTransferID);
                    break;
            }
            param.client.Close();
            _semaphore.Release();
        }
        private void transmitError(UdpClient client, Constants.ErrorCode error, string message)
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

            client.Send(toSend,toSend.Length);
        }

        private void transmitAwk(UdpClient client, int block)
        {
            byte[] toSend = new byte[4];
            toSend[0] = 0;
            toSend[1] = (byte)Constants.OpCode.Acknowledge;
            Array.Copy(BitConverter.GetBytes(block),0,toSend,2,2);
            client.Send(toSend,4);
        }
        //TODO
        private Constants.RequstType checkReadWrite(byte[] bytes)
        {
            return Constants.RequstType.Read;
        }
        //TODO
        private void read(UdpClient client, string filename)
        {
            short block = 0;
            //try to open file
            try
            {
                byte[] toSend = File.ReadAllBytes(filename);
            }
            catch(FileNotFoundException e)
            {
                transmitError(client, Constants.ErrorCode.FileNotFound, e.Message);
            }
            catch(UnauthorizedAccessException e)
            {
                transmitError(client, Constants.ErrorCode.AccessViolation, e.Message);
            }
            catch(Exception e)
            {
                transmitError(client, Constants.ErrorCode.NotDefined, e.Message);
            }
            transmitAwk(client, block);

        }
        //TODO
        private void write(UdpClient client, string filename)
        {
            //Check if file exists here
            short block = 0;
            transmitAwk(client, block);
            List<byte> rawFile = new List<byte>();
            byte[] input = client.Receive(ref _endPoint);
            while(input.Length == 516)
            {
                //validate data block here
                transmitAwk(client, block);
                rawFile.AddRange(input);
                input = client.Receive(ref _endPoint);
            }
            try
            {
                File.WriteAllBytes(filename, rawFile.ToArray());
            }
            catch(UnauthorizedAccessException e)
            {
                transmitError(client,Constants.ErrorCode.AccessViolation,e.Message);
            }
            catch(Exception e)
            {
                transmitError(client, Constants.ErrorCode.NotDefined, e.Message);
            }
        }
    }
}
