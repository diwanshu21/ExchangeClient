using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.IO;


namespace ExchangeClient
{
    class Program
    {
        // Method to calculate GCD of two numbers
        static int CalculateGCD(int a, int b)
        {
            if (b == 0)
                return a;
            return CalculateGCD(b, a % b);
        }

        // Method to calculate GCD of numbers in a HashSet
        static int CalculateGCD(HashSet<int> numbers)
        {
            if (numbers.Count == 0)
                throw new ArgumentException("HashSet cannot be empty");

            int result = 0;
            foreach (int num in numbers)
            {
                result = CalculateGCD(result, num);
            }
            return result;
        }

           class Packet
    {
        // Properties same as above
               public string Symbol { get; set; }
        public char BuySellIndicator { get; set; }
        public int Quantity { get; set; }
        public int Price { get; set; }
        public int PacketSequence { get; set; }
    }

    static void allpackets(HashSet<int> receivedSequences,List<Packet> packetList)
    { 
        using (TcpClient client = new TcpClient("localhost", 3000))
            using (NetworkStream stream = client.GetStream())
            {
                    int gcd = CalculateGCD(receivedSequences);


                // Request missing packets
                int firstSequence = gcd;
                int lastSequence = receivedSequences.Max();

                byte[] responseBuffer = new byte[17]; // Fixed packet size: 17 bytes

                // Inside the loop for requesting missing packets
            for (int sequence = firstSequence; sequence < lastSequence; sequence+=gcd)
            {
                if (!receivedSequences.Contains(sequence))
                {
                    // Request missing packet by sending a "Resend Packet" request
                    byte[] resendRequestPayload = new byte[] { 2, (byte)(sequence/gcd) };
                    stream.Write(resendRequestPayload, 0, resendRequestPayload.Length);

                    // Receive and process the resent packet
                    int bytesRead = stream.Read(responseBuffer, 0, responseBuffer.Length);

                    if (bytesRead == 17)
                    {
                        // Process the resent response packet
                        string symbol = Encoding.ASCII.GetString(responseBuffer, 0, 4);
                        char buySellIndicator = (char)responseBuffer[4];
                        int quantity = BitConverter.ToInt32(responseBuffer, 5);
                        int price = BitConverter.ToInt32(responseBuffer, 9);
                        int packetSequence = BitConverter.ToInt32(responseBuffer, 13);

                        // Do something with the resent packet's data
                        // Console.WriteLine($"Resent - Symbol: {symbol}, Buy/Sell: {buySellIndicator}, Quantity: {quantity}, Price: {price}, Sequence: {packetSequence}");

                         packetList.Add(new Packet { Symbol = symbol, BuySellIndicator = buySellIndicator, Quantity = quantity, Price = price, PacketSequence = packetSequence });


                        // Update the received sequences data structure
                        receivedSequences.Add(packetSequence);
                    }
                }
            }
// Sort the packetList based on PacketSequence using LINQ
List<Packet> sortedPackets = packetList.OrderBy(p => p.PacketSequence).ToList();

// Print the sorted packets
foreach (Packet packet in sortedPackets)
{
    Console.WriteLine($"Symbol: {packet.Symbol}, Buy/Sell: {packet.BuySellIndicator}, Quantity: {packet.Quantity}, Price: {packet.Price}, Sequence: {packet.PacketSequence}");
}


// Serialize the sortedPackets list to a JSON string
string jsonString = JsonSerializer.Serialize(sortedPackets, new JsonSerializerOptions { WriteIndented = true });

// Specify the path for the JSON file
string jsonFilePath = "sortedPackets.json";

// Write the JSON string to the specified file
File.WriteAllText(jsonFilePath, jsonString);

Console.WriteLine($"JSON file '{jsonFilePath}' generated.");


            }
    }

        static void Main(string[] args)
        {
            using (TcpClient client = new TcpClient("localhost", 3000))
            using (NetworkStream stream = client.GetStream())
            {
                byte callType = 1; // Call Type 1: Stream All Packets
                byte resendSeq = 0;

                // Construct and send the request payload
                byte[] requestPayload = new byte[] { callType, resendSeq };
                stream.Write(requestPayload, 0, requestPayload.Length);

                // Receive and process response packets
                byte[] responseBuffer = new byte[17]; // Fixed packet size: 17 bytes
                int totalBytesRead = 0;

                                HashSet<int> receivedSequences = new HashSet<int>();
        List<Packet> packetList = new List<Packet>();


                while (true)
                {
                    int bytesRead = stream.Read(responseBuffer, 0, responseBuffer.Length);

                    if (bytesRead == 0) // No more data
                        break;

                    totalBytesRead += bytesRead;

                    while (totalBytesRead >= 17)
                    {
                        // Process the response packet
                        string symbol = Encoding.ASCII.GetString(responseBuffer, 0, 4);
                        char buySellIndicator = (char)responseBuffer[4];
                        int quantity = BitConverter.ToInt32(responseBuffer, 5);
                        int price = BitConverter.ToInt32(responseBuffer, 9);
                        int packetSequence = BitConverter.ToInt32(responseBuffer, 13);

                        // Do something with the extracted data
                        // Console.WriteLine($"Symbol: {symbol}, Buy/Sell: {buySellIndicator}, Quantity: {quantity}, Price: {price}, Sequence: {packetSequence}");

                                packetList.Add(new Packet { Symbol = symbol, BuySellIndicator = buySellIndicator, Quantity = quantity, Price = price, PacketSequence = packetSequence });


                        receivedSequences.Add(packetSequence);

                        totalBytesRead -= 17;

                        // Move remaining bytes to the beginning of the buffer
                        Array.Copy(responseBuffer, 17, responseBuffer, 0, totalBytesRead);
                    }
                }

    int gcd = CalculateGCD(receivedSequences);
            Console.WriteLine($"GCD of the numbers: {gcd}");
            allpackets(receivedSequences,packetList);
  
            }
        }
    }
}
