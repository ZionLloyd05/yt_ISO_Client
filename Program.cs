using System.Net.Sockets;
using System.Net;
using System.Text;
using BIM_ISO8583.NET;
using ISOClient;

namespace ISO_Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            while(true)
            {
                Console.WriteLine("------New Session Started------------------------------\n");
                Console.WriteLine("ZION POS TERMINAL\n");
                Console.WriteLine("You have opted for the purchase of an Apple Watch Ultra");

                var productAmount = 900;

                Console.WriteLine("Total Cost = $900, press y to continue");

                var userInput = Console.ReadLine();

                if (userInput != "y")
                {
                    Console.WriteLine("Transaction was terminated\n");
                    continue;
                }

                Console.WriteLine("Enter your account number\n");
                var accountNumber = Console.ReadLine();

                if (string.IsNullOrEmpty(accountNumber) || 
                    accountNumber.Length != 11)
                {
                    Console.WriteLine("Invalid account number\n");
                    Console.WriteLine("-----------------------Session Terminated------------------------------\n");
                    continue;
                }

                var customerTransactionValidity = IsCustomerTransactionRequestValid(accountNumber, productAmount);

                if (!customerTransactionValidity)
                {
                    Console.WriteLine("Insufficient funds to complete this transaction\n");
                    Console.WriteLine("-----------------------Session Terminated------------------------------\n");
                    continue;
                }

                Console.WriteLine("Customer is eligible for purchase\n");

                Console.WriteLine("...debitting customer.....\n");

                Thread.Sleep(30);
                Console.WriteLine("🎊🎊🎊Transaction was successful🎊🎊\n");
                Console.WriteLine("-----------------------Session Terminated------------------------------\n");
            }
        }

        private static bool IsCustomerTransactionRequestValid(string accountNumber, int productAmount)
        {
            //1. Set/Define Data

            //Message Type Identifier (Financial Message Request)
            string MTI = "0200";

            //Processing Code.[DE #3] (Purchase Transaction, Savings)
            string ProCode = "001000";

            //Transmission Date and Time.[DE #7] (format: MMDDhhmmss)
            string TransDate = "0429104720";

            //System Trace Audit No.[DE #11] (456 or 000456)
            string STAN = "456";

            //2.Create an object BIM-ISO8583.ISO8583
            ISO8583 iso8583 = new ISO8583();

            //3. Create Arrays
            string[] DE = new string[130];

            DE[2] = accountNumber;
            DE[3] = ProCode;
            DE[7] = TransDate;
            DE[8] = productAmount.ToString();
            DE[11] = STAN;

            //4.Use "Build" method of object iso8583 to create a new  message.
            string NewISOmsg = iso8583.Build(DE, MTI);

            Console.WriteLine($"ISO Request Message -> {NewISOmsg}");

            // Send the sign on request to the remote server.  Exceptions will happen here
            // which need to be dealt with
            var response = SendClientRequest(NewISOmsg);

            var isoResponse = Unpack(response);

            var customerAccountBalance = Convert.ToInt32(isoResponse[8]);
            Console.WriteLine($"Customer Account Balance -> {customerAccountBalance}\n");

            if (isoResponse[39] == "00")
            {
                return true;
            }

            return false;
        }

        private static string[] Unpack(string resData)
        {
            ISO8583 iso8583 = new ISO8583();

            string[] DE;

            DE = iso8583.Parse(resData);

            return DE;
        }

        private static string SendClientRequest(string isoMessage)
        {
            byte[] bytes = new byte[1024];
            string response = string.Empty;

            try
            {
                // Connect to a Remote server
                // Get Host IP Address that is used to establish a connection
                // In this case, we get one IP address of localhost that is IP : 127.0.0.1
                // If a host has multiple addresses, you will get a list of addresses
                IPHostEntry host = Dns.GetHostEntry("localhost");
                IPAddress ipAddress = host.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

                // Create a TCP/IP  socket.
                Socket sender = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.
                try
                {
                    // Connect to Remote EndPoint
                    sender.Connect(remoteEP);

                    Console.WriteLine("Socket connected to {0}",
                        sender.RemoteEndPoint.ToString());

                    // Encode the data string into a byte array.
                    byte[] msg = Encoding.ASCII.GetBytes($"{isoMessage}<EOF>");

                    // Send the data through the socket.
                    int bytesSent = sender.Send(msg);

                    // Receive the response from the remote device.
                    int bytesRecieved = sender.Receive(bytes);

                    response = Encoding.ASCII.GetString(bytes, 0, bytesRecieved);
                    
                    Console.WriteLine("Echoed test = {0}", response);

                    // Release the socket.
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();

                    return response;
                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return response;
        }
    }
}