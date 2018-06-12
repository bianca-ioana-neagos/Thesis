// *********************************************************
//
//  Copyright (c) Microsoft. All rights reserved.
//  This code is licensed under the Apache 2.0 License.
//  THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OR
//  CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
//  INCLUDING, WITHOUT LIMITATION, ANY IMPLIED WARRANTIES
//  OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
//  PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
// *********************************************************

namespace WcfClient
{
    using System;
    using System.Management;
    using System.Threading;
    using EventTypes;
    using WcfClient.ServiceReference1;
    
    class WcfClientConsole
    {
        static WCFObservableClient wcfClient = new WCFObservableClient();

        static bool Send(int i, DateTimeOffset t)
        {
            try
            {
                wcfClient.OnNext(new InputEvent { I = i, T = t });
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine("An exception was hit when sending the events: {0}", e.Message);
                return false;
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("WCF Client sending events");
            ManagementObject processor = new ManagementObject(
                "Win32_PerfFormattedData_PerfOS_Processor.Name='_Total'");
            Thread.Sleep(5000);
            while (!Console.KeyAvailable)
            {
                processor.Get();
                int currentCpuUsage = Convert.ToInt32(processor.Properties["PercentProcessorTime"].Value);

                Console.WriteLine("Sending " + currentCpuUsage);
                if (!Send(currentCpuUsage, DateTimeOffset.Now))
                {
                    Console.WriteLine("Leaving loop due to sending failure, please check that the client and the server are still online.");
                    break;
                }
                Thread.Sleep(100);
            }
            try
            {
                wcfClient.OnCompleted();
            }
            catch(Exception e)
            {
                Console.WriteLine("Exception encountered: {0}\n.Could not send the 'OnCompleted' message, please check that the client and the server are still online.", e.Message);
            } 
            Console.WriteLine("Press <Enter> to exit.");
            Console.ReadLine();
        }
    }
}
