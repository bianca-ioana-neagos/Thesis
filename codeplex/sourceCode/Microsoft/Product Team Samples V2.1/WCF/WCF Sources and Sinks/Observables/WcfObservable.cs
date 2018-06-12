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
    using System.ServiceModel;
    using System.ServiceModel.Description;
    using EventTypes;
    
    // Define a service contract.
    [ServiceContract(Namespace = "http://Microsoft.ServiceModel.WcfObservable",
        Name="IWCFObservable")]
    public interface IWCFObservable
    {
        [OperationContract(IsOneWay = true)]
        void OnCompleted();
        [OperationContract(IsOneWay = true)]
        void OnError(Exception error);
        [OperationContract]
        void OnNext(InputEvent value);
    }

    [ServiceBehavior(IncludeExceptionDetailInFaults = true,
        InstanceContextMode = InstanceContextMode.Single)]
    public class WcfObservableService : IWCFObservable
    {
        WcfObservable _ObsInstance;
        public WcfObservableService(WcfObservable ObsInstance)
        {
            _ObsInstance = ObsInstance;
        }

        public void OnCompleted()
        {
            _ObsInstance.OnCompleted();
        }

        public void OnError(Exception error)
        {
            _ObsInstance.OnError(error);
        }

        public void OnNext(InputEvent value)
        {
            _ObsInstance.OnNext(value);
        }
    }

    
    public class WcfObservable : IObservable<InputEvent>
    {
        private List<IObserver<InputEvent>> observers;
        ServiceHost _selfHost;


        public WcfObservable(string baseAddress)
        {
            observers = new List<IObserver<InputEvent>>();
            _selfHost = new ServiceHost(new WcfObservableService(this), new Uri(baseAddress));

           _selfHost.AddServiceEndpoint(
                    typeof(IWCFObservable),
                    new WSHttpBinding(),
                    "WcfObservableService");
                ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
                smb.HttpGetEnabled = true;
                _selfHost.Description.Behaviors.Add(smb);
        }

        public IDisposable Subscribe(IObserver<InputEvent> observer)
        {
            if (observers.Count == 0 && _selfHost.State != CommunicationState.Opened)
            {
                _selfHost.Open();
            }

            if (!observers.Contains(observer))
                observers.Add(observer);
            
            return new Unsubscriber(observers, observer, _selfHost);
        }

        private class Unsubscriber : IDisposable
        {
            private List<IObserver<InputEvent>> _observers;
            private IObserver<InputEvent> _observer;
            private ServiceHost _selfHost;

            public Unsubscriber(List<IObserver<InputEvent>> observers, IObserver<InputEvent> observer, ServiceHost selfHost)
            {
                this._observers = observers;
                this._observer = observer;
                this._selfHost = selfHost;
            }

            public void Dispose()
            {
                if (_observer != null && _observers.Contains(_observer))
                    _observers.Remove(_observer);
                if (_observers.Count == 0)
                {
                    _selfHost.Close();
                }
            }
        }

        public void OnCompleted()
        {
            Debug.WriteLine("Wcf Observable OnCompleted");
            foreach (var observer in observers.ToArray())
            {
                if (observers.Contains(observer))
                    observer.OnCompleted();
            }
            observers.Clear();
        }

        public void OnError(Exception error)
        {
            Debug.WriteLine("Wcf Observable OnError: " + error.Message);
            foreach (var observer in observers)
            {
                observer.OnError(error);
            }
        }

        public void OnNext(InputEvent value)
        {
            Debug.WriteLine("Wcf Observable OnNext: " + value.ToString());
            foreach (var observer in observers)
            {
                observer.OnNext(value);
            }
        }
    }
}
