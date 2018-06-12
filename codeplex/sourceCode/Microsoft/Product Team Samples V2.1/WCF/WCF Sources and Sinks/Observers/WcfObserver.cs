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

namespace StreamInsightWCFArtifacts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.ServiceModel;
    using System.ServiceModel.Description;
    using EventTypes;

    // Define a service contract.
    [ServiceContract(Namespace = "http://Microsoft.ServiceModel.WcfObserver",
        CallbackContract = typeof(IWcfObserverCallback))]
    public interface IWCFObserver
    {
        [OperationContract]
        void Subscribe();

        [OperationContract]
        void Unsubscribe();
    }

    public interface IWcfObserverCallback
    {
        [OperationContract(IsOneWay = true)]
        void OnCompleted();
        [OperationContract(IsOneWay = true)]
        void OnError(Exception error);
        [OperationContract(IsOneWay = true)]
        void OnNext(OutputEvent value);
    }

    [ServiceBehavior(IncludeExceptionDetailInFaults = true,
        InstanceContextMode = InstanceContextMode.Single)]
    class WcfObserverService : IWCFObserver
    {
        List<IWcfObserverCallback> subscribersList = new List<IWcfObserverCallback>();

        public void Subscribe()
        {
            var wcfClientCallback = OperationContext.Current.GetCallbackChannel<IWcfObserverCallback>();
            if (!subscribersList.Contains(wcfClientCallback))
            {
                subscribersList.Add(wcfClientCallback);
            }
        }

        public void Unsubscribe()
        {
            var wcfClientCallback = OperationContext.Current.GetCallbackChannel<IWcfObserverCallback>();
            if (subscribersList.Contains(wcfClientCallback))
            {
                subscribersList.Remove(wcfClientCallback);
            }
        }

        public void OnCompleted()
        {
            Action(s => s.OnCompleted());
        }

        public void OnError(Exception error)
        {
            Action(s => s.OnError(error));
        }

        public void OnNext(OutputEvent value)
        {
            Action(s => s.OnNext(value));
        }

        void Action(Action<IWcfObserverCallback> call)
        {
            var disconnected = subscribersList.Where(s =>
                {
                    try
                    {
                        call(s);
                        return false;
                    }
                    catch (ObjectDisposedException)
                    {
                        Console.WriteLine("Connection already disposed, disconnecting client's channel");
                        return true;
                    }
                }
            );
            disconnected.ToList().ForEach(s => subscribersList.Remove(s));
        }
    }

    public class WcfObserver : IObserver<OutputEvent>
    {
        ServiceHost _selfHost;
        WcfObserverService _observerService;

        public WcfObserver(string baseAddress)
        {
            _observerService = new WcfObserverService();
            _selfHost = new ServiceHost(_observerService, new Uri(baseAddress));

            _selfHost.AddServiceEndpoint(
                     typeof(IWCFObserver),
                     new WSDualHttpBinding(),
                     "WcfObserverService");
            ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
            smb.HttpGetEnabled = true;
            _selfHost.Description.Behaviors.Add(smb);
            _selfHost.Open();
        }

        public void OnCompleted()
        {
            Debug.WriteLine("Wcf Observer OnCompleted");
            _observerService.OnCompleted();
        }

        public void OnError(Exception error)
        {
            Debug.WriteLine("Wcf Observer OnError: " + error.Message);
            _observerService.OnError(error);
        }

        public void OnNext(OutputEvent value)
        {
            Debug.WriteLine("Wcf Observer OnNext: " + value.ToString());
            _observerService.OnNext(value);
        }
    }
}
