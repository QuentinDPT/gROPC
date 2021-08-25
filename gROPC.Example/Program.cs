﻿using System;
using System.Collections.Generic;

namespace gROPC.Example
{
    class Program
    {
        private static string serverURL = "frmfgopc02.sxb.punchpowerglide.com:50000";
        //private static string serverURL = "frsxbscdev01.sxb.punchpowerglide.com:50000";
        //private static string serverURL = "localhost:50000";

        private static string OPCValue  = "ns=2;s=Channel1.Device1.Tag1";
        private static string OPCValue2 = "ns=2;s=Channel1.Device1.Tag2";

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
            var sub = subscribe_to_a_value();

            Console.WriteLine("Complete");

            System.Threading.Thread.Sleep(3000);

            Console.WriteLine("Part 2.2\t Unsubscription");
            unsubscribe_to_a_value(sub);

            Console.WriteLine("Complete");

            Console.WriteLine("Part 3  \t Write");
            write_a_value();

            Console.WriteLine("Complete");

            Console.WriteLine("Part 4.1\t Deconnection");

            sub = subscribe_to_a_value();

            Console.WriteLine("You can now restart the server");

            while (true) { }
        }

        /// <summary>
        /// Read a value from the OPC server
        /// </summary>
        static void read_a_value()
        {
            Console.WriteLine(" > Read the value " + OPCValue2);

            Console.WriteLine(" > " + OPCService.Read<int>(OPCValue2));
        }

        /// <summary>
        /// Subscribe to a value from the OPC server
        /// </summary>
        /// <returns>id of the subscription</returns>
        static gROPCSubscription<int> subscribe_to_a_value()
        {
            Console.WriteLine(" > Subscribe to " + OPCValue);

            var sub = OPCService.Subscribe<int>(OPCValue);

            var retVal = new List<string>();
            retVal.Add(OPCValue2);
            sub.ReturnedValues = retVal;

            sub.onChangeValue += onRecieve;

            return sub.Subscribe();
        }

        /// <summary>
        /// Unsubscribe to a value from the OPC server
        /// </summary>
        /// <param name="subscriptionId">id of the subscription to stop</param>
        static void unsubscribe_to_a_value(gROPCSubscription<int> sub)
        {
            Console.WriteLine(" > Unsubscribe to " + sub.NodeValue);

            sub.Unsubscribe();
        }


        static void write_a_value()
        {
            Console.WriteLine(" > Write a value " + OPCValue);

            OPCService.Write(OPCValue, 666);
        }

        /// <summary>
        /// Function launched each time we have a new value from OPC server
        /// </summary>
        /// <param name="value">value readed</param>
        static void onRecieve(object sender, SubscriptionResponse<int> value)
        {
            Console.WriteLine(" > Recieve a value : " + value.responseValue);
            Console.WriteLine(" > Associated      : " + value.responsesAssociated[OPCValue2]);
        }
    }
}
