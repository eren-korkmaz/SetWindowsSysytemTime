using System;
using System.Net;
using System.Net.Sockets;

namespace LdkSetSysTime
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                DateTime currentTime = GetNetworkTime();

                Console.WriteLine("NTP zaman: " + currentTime);
                Console.WriteLine("NTP zaman local: " + currentTime.ToLocalTime());

                SetSystemTime(currentTime);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Hata: " + ex.Message);
            }
        }


        private static DateTime GetNetworkTime()
        {
            // NTP sunucusu adresi
            const string ntpServer = "time.windows.com";

            // NTP mesajı (48 byte)
            byte[] ntpData = new byte[48];
            ntpData[0] = 0x1B; // NTP versiyonu 3 ve istemci modunda çalışmak için

            IPAddress[] addresses = Dns.GetHostEntry(ntpServer).AddressList;
            IPEndPoint ipEndPoint = new IPEndPoint(addresses[0], 123);

            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Connect(ipEndPoint);
                socket.Send(ntpData);
                socket.Receive(ntpData);
                socket.Close();
            }

            // NTP zaman bilgisi 40. byte'tan itibaren başlar
            ulong intPart = BitConverter.ToUInt32(ntpData, 40);
            ulong fractPart = BitConverter.ToUInt32(ntpData, 44);

            // Big-endian'dan little-endian'a çevir
            intPart = SwapEndianness(intPart);
            fractPart = SwapEndianness(fractPart);

            // Unix epoch'a göre zaman farkı (1 Ocak 1900)
            ulong milliseconds = (intPart * 1000 + (fractPart * 1000) / 0x100000000L);
            DateTime networkDateTime = (new DateTime(1900, 1, 1)).AddMilliseconds((long)milliseconds);

            return networkDateTime;
        }

        private static uint SwapEndianness(ulong x)
        {
            return (uint)(((x & 0x000000ff) << 24) + ((x & 0x0000ff00) << 8) + ((x & 0x00ff0000) >> 8) + ((x & 0xff000000) >> 24));
        }

        private static void SetSystemTime(DateTime dateTime)
        {
            SystemTime st = new SystemTime
            {
                Year = (ushort)dateTime.Year,
                Month = (ushort)dateTime.Month,
                Day = (ushort)dateTime.Day,
                Hour = (ushort)dateTime.Hour,
                Minute = (ushort)dateTime.Minute,
                Second = (ushort)dateTime.Second,
                Milliseconds = (ushort)dateTime.Millisecond
            };
            SetSystemTime(ref st);
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetSystemTime(ref SystemTime st);

        public struct SystemTime
        {
            public ushort Year;
            public ushort Month;
            public ushort DayOfWeek;
            public ushort Day;
            public ushort Hour;
            public ushort Minute;
            public ushort Second;
            public ushort Milliseconds;
        }

    }
}
