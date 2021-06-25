using System;

namespace gROPC.Example
{
    class Program
    {
        private static string serverURL = "localhost:50000";

        private static string OPCValue = "ns=2;s=Channel1.Device1.TEST";

        private static gROPCService OPCService;

        static void Main(string[] args)
        {
            Console.WriteLine("NEED TO START SERVER ON : " + serverURL);

            System.Threading.Thread.Sleep(2000);

            Console.WriteLine("Connection...");

            OPCService = new gROPC.gROPCService(serverURL);

            Console.WriteLine("Complete");

            Console.WriteLine("Start tests...");
            Console.WriteLine("");

            // CRUD
            Console.WriteLine("Part 1  \t Read");
            read_a_value();

            Console.WriteLine("Complete");


            Console.WriteLine("Part 2.1\t Subscription");
            int subID = subscribe_to_a_value();

            Console.WriteLine("Complete");

            System.Threading.Thread.Sleep(15000);

            Console.WriteLine("Part 2.2\t Unsubscription");
            unsubscribe_to_a_value(subID);

            Console.WriteLine("Complete");
        }

        /// <summary>
        /// Read a value from the OPC server
        /// </summary>
        static void read_a_value()
        {
            Console.WriteLine(" > Read the value " + OPCValue);

            Console.WriteLine(" > " + OPCService.Read(OPCValue));
        }

        /// <summary>
        /// Subscribe to a value from the OPC server
        /// </summary>
        /// <returns>id of the subscription</returns>
        static int subscribe_to_a_value()
        {
            Console.WriteLine(" > Subscribe to " + OPCValue);

            var id = OPCService.SubscribeAsync(OPCValue, onRecieve);

            Console.WriteLine(" > (id) " + id);

            return id;
        }

        /// <summary>
        /// Unsubscribe to a value from the OPC server
        /// </summary>
        /// <param name="subscriptionId">id of the subscription to stop</param>
        static void unsubscribe_to_a_value(int subscriptionId)
        {
            Console.WriteLine(" > Unsubscribe to " + subscriptionId);

            OPCService.Unsubscribe(subscriptionId);
        }


        /// <summary>
        /// Function launched each time we have a new value from OPC server
        /// </summary>
        /// <param name="value">value readed</param>
        static void onRecieve(string value)
        {
            Console.WriteLine(" > Recieve a value : " + value);
        }
    }
}
