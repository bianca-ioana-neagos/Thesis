//*********************************************************
//
//  Copyright (c) Microsoft. All rights reserved.
//  This code is licensed under the Apache 2.0 License.
//  THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OR
//  CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
//  INCLUDING, WITHOUT LIMITATION, ANY IMPLIED WARRANTIES
//  OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
//  PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

namespace StreamInsight.Samples.ComposingQueries
{
    using System;

    /// <summary>
    /// Event type for the generator adapters.
    /// </summary>
    public class GeneratedEvent
    {
        /// <summary>
        /// Gets or sets the simulated measurement value.
        /// </summary>
        public string DeviceId { get; private set; }
        public float Value { get; private set; }

        public GeneratedEvent() 
        {
            Random rnd = new Random();
            const int DeviceCount = 5;  // Cannot be passed through ctor of TPlayload
            const int MaxValue = 100;

            DeviceId = rnd.Next((int)DeviceCount).ToString();
            Value = (float)rnd.Next((int)MaxValue);
        }

        public override string ToString()
        {
            return DeviceId;
        }
    }

    /// <summary>
    /// Output type of the primary query.
    /// Dynamic query composition requires explicit types at the query
    /// boundaries.
    /// </summary>
    public class AnnotatedValue
    {
        public string DeviceID { get; set; }
        public float Value { get; set; }
        public float ValueDelta { get; set; }
    }

    /// <summary>
    /// Output type of the secondary query.
    /// Dynamic query composition requires explicit types at the query
    /// boundaries.
    /// </summary>
    public class MaxValue
    {
        public float Value { get; set; }
    }  

    /// <summary>
    /// State of one device. Contains the device data and the corresponding timestamp.
    /// </summary>
    public class DeviceState<TPayload>
    {
        /// <summary>
        /// Stored payload.
        /// </summary>
        private TPayload data;

        /// <summary>
        /// Stored timestamp.
        /// </summary>
        private DateTimeOffset timestamp;

        /// <summary>
        /// Initializes a new instance of the DeviceState class.
        /// </summary>
        /// <param name="data">Payload to store.</param>
        /// <param name="timestamp">Timestamp to store.</param>
        public DeviceState(TPayload data, DateTimeOffset timestamp)
        {
            this.data = data;
            this.timestamp = timestamp;
        }

        /// <summary>
        /// Gets the payload of the stored event.
        /// </summary>
        public TPayload Data
        {
            get { return this.data; }
        }

        /// <summary>
        /// Gets the timestamp of the stored event.
        /// </summary>
        public DateTimeOffset Timestamp
        {
            get { return this.timestamp; }
        }
    }

}
