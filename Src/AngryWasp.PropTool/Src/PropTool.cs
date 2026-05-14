using System;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;

namespace AngryWasp.PropTool.App
{
    public class PropTool : IDisposable
    {
        private SerialPort port;
        private static int LFSR = 'P';
        private byte[] lfsrBuffer = new byte[500];
        private byte[] receiveBuffer = new byte[258];

        public PropTool()
        {
            for (int i = 0; i < 500; i++)
                lfsrBuffer[i] = (byte)(Iterate() | 0xFE);

            for (int i = 0; i < 258; i++)
                receiveBuffer[i] = 0xF9;
        }

        public bool CheckPortExists(string portName) => SerialPort.GetPortNames().Count(x => x == portName) >= 1;

        public void OpenPort(string portName, int baud)
        {
            Console.WriteLine($"Opening port {portName} @ {baud}");
            port = new SerialPort(portName, baud, Parity.None, 8, StopBits.One);
            port.ReadTimeout = 150;
            port.WriteTimeout = 150;
            port.RtsEnable = false;
            port.DtrEnable = false;

            if (!port.IsOpen)
                port.Open();
        }

        public void ChangeBaudRate(int baud)
        {
            if (!port.IsOpen)
                return;

            Console.WriteLine($"Changing baud rate to {baud}");
            port.DiscardInBuffer();
            port.DiscardOutBuffer();
            port.BaudRate = baud;
        }

        public void Listen()
        {
            port.DiscardInBuffer();
            port.DiscardOutBuffer();
            port.DataReceived += (s, e) =>
            {
                Console.Write(port.ReadExisting());
            };
        }

        public void ClosePort()
        {
            if (port.IsOpen)
            {
                port.DiscardInBuffer();
                port.DiscardOutBuffer();
                port.Close();
            }

            port.Dispose();
        }

        public bool HwFind()
        {
            try
            {
                Reset();

                port.Write(new byte[] { 0xF9 }, 0, 1);

                port.Write(lfsrBuffer, 0, 250);
                port.Write(receiveBuffer, 0, 258);

                WaitForBytes(258, port.ReadTimeout);
                port.Read(receiveBuffer, 0, 258);

                for (int i = 0; i < 250; i++)
                    if (receiveBuffer[i] != lfsrBuffer[i + 250])
                        return false;

                int version = 0;
                for (int i = 250; i < 258; i++)
                    version = ((version >> 1) & 0x7f) | ((receiveBuffer[i] & 0x01) << 7);

                if (version != 1)
                    return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            return true;
        }

        private int Iterate()
        {
            int bit = LFSR & 1;
            LFSR = LFSR << 1 | (LFSR >> 7 ^ LFSR >> 5 ^ LFSR >> 4 ^ LFSR >> 1) & 1;
            return bit;
        }

        public void Reset()
        {
            Console.WriteLine("Resetting device");
            port.RtsEnable = port.DtrEnable = true;
            Thread.Sleep(50);
            port.RtsEnable = port.DtrEnable = false;
            Thread.Sleep(100);
        }

        private void WaitForBytes(int count, int millisecondsTimeout)
        {
            if (port.BytesToRead >= count)
                return;

            DateTime expire = DateTime.Now.AddMilliseconds(millisecondsTimeout);

            while (port.BytesToRead < count)
            {
                if (DateTime.Now >= expire)
                    throw new TimeoutException();

                Thread.Sleep(25);
            }
        }

        public bool LoadBinaryFile(string path, Load_Type type)
        {
            byte[] image = File.ReadAllBytes(path);

            if (HwFind())
            {
                if (Upload(image, type))
                    return true;
            }

            return false;
        }

        private bool Upload(byte[] image, Load_Type type)
        {
            SendLong((int)type);
            SendLong(image.Length / 4);

            for (int i = 0; i < image.Length; i += 4)
                SendLong(image[i] | (image[i + 1] << 8) | (image[i + 2] << 16) | (image[i + 3] << 24));

            Thread.Sleep(50);

            if (GetAck(100))
            {
                if (type == Load_Type.EEPROM)
                {
                    if (GetAck(500))
                    {
                        if (GetAck(500))
                            return true;
                        else
                            return false;
                    }
                    else
                        return false;
                }
                else
                    return true;
            }

            return false;
        }

        private void SendLong(int data)
        {
            byte[] buf = new byte[11];

            for (int i = 0; i < 10; i++)
            {
                buf[i] = (byte)(0x92 | (data & 1) | ((data & 2) << 2) | ((data & 4) << 4));
                data >>= 3;
            }

            buf[10] = (byte)(0xf2 | (data & 1) | ((data & 2) << 2));

            port.Write(buf, 0, 11);
        }

        private bool GetAck(int retries)
        {
            for (int i = 0; i < retries; i++)
            {
                port.Write(new byte[] { 0xF9 }, 0, 1);

                if (port.BytesToRead > 0)
                    return true;

                Thread.Sleep(10);
            }


            return false;
        }

        #region IDisposable implementation

        public void Dispose()
        {
            lfsrBuffer = null;
            receiveBuffer = null;

            if (port != null)
            {
                if (port.IsOpen)
                    port.Close();

                port.Dispose();
            }
        }

        #endregion
    }

}