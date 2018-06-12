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

namespace StreamInsight.Samples.HelloToll
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.ComplexEventProcessing;
    using Microsoft.ComplexEventProcessing.Extensibility;

    public sealed class TagInfo
    {
        public string TagId { get; set; }
        public DateTime RenewalDate { get; set; }
        public bool IsReportedLostOrStolen { get; set; }
        public string AccountId { get; set; }

        public static bool IsLostOrStolen(string tagId)
        {
            return Tags.Any(tag => tag.TagId == tagId && tag.IsReportedLostOrStolen);
        }

        public static bool IsExpired(string tagId)
        {
            return Tags.Any(tag => tag.TagId == tagId && tag.RenewalDate.AddYears(1) > DateTime.Now);
        }

        public static TagInfo[] Tags
        {
            get { return tags; }
        }

        /// <summary>
        /// Simulation of a reference database; for the user defined function to search against this.
        /// In reality, this could be a database, an in-memory cache, or another input stream.
        /// </summary>
        static TagInfo[] tags = new TagInfo[]
        {
           new TagInfo { TagId = "123456789", RenewalDate = new DateTime(2009, 02, 20), IsReportedLostOrStolen = false, AccountId = "NJ100001JET1109" },
           new TagInfo { TagId = "234567891", RenewalDate = new DateTime(2008, 12, 06), IsReportedLostOrStolen = true,  AccountId = "NY100002GNT0109" },
           new TagInfo { TagId = "345678912", RenewalDate = new DateTime(2008, 09, 01), IsReportedLostOrStolen = true,  AccountId = "CT100003YNK0210" },
        };
    }

    public class OutOfStateVehicleRatio : CepAggregate<TollReading, float>
    {
        public override float GenerateOutput(IEnumerable<TollReading> tollReadings)
        {
            float tempCount = 0;
            float totalCount = 0;
            foreach (var tollReading in tollReadings)
            {
                totalCount++;
                if (tollReading.State != "NY")
                {
                    tempCount++;
                }
            }

            return tempCount / totalCount;
        }
    }

    public class OutOfStateVehicleRatio2 : CepAggregate<string, float>
    {
        public override float GenerateOutput(IEnumerable<string> stateReadings)
        {
            float tempCount = 0;
            float totalCount = 0;
            foreach (var state in stateReadings)
            {
                totalCount++;
                if (state != "NY")
                {
                    tempCount++;
                }
            }

            return tempCount / totalCount;
        }
    }

    public class VehicleWeights : CepTimeSensitiveOperator<TollReading, VehicleWeightInfo>
    {
        private double weightcharge = 0.5; // defined as a constant within the UDO; this could be passed as a config if required

        public override IEnumerable<IntervalEvent<VehicleWeightInfo>> GenerateOutput(
            IEnumerable<IntervalEvent<TollReading>> events,
            WindowDescriptor windowDescriptor)
        {
            List<IntervalEvent<VehicleWeightInfo>> output = new List<IntervalEvent<VehicleWeightInfo>>();

            // Identify any commercial vehicles in this window for the given window duration
            foreach (var e in events.Where(e => e.StartTime.Hour >= 0 && e.Payload.VehicleType == 2))
            {
                // create an output interval event
                IntervalEvent<VehicleWeightInfo> vehicleWeightEvent = CreateIntervalEvent();

                // populate the output interval event
                vehicleWeightEvent.StartTime = e.StartTime;
                vehicleWeightEvent.EndTime = e.EndTime;
                vehicleWeightEvent.Payload = new VehicleWeightInfo
                {
                    LicensePlate = e.Payload.LicensePlate,
                    Weight = e.Payload.VehicleWeight,

                    // here is the interesting part; note how the output is dependent on
                    // the start and end timestamps of the input event. The weight charge
                    // is a function of the rush hour definition, the weigh charge factor
                    // and the vehicle tonnage itself
                    WeightCharge = ((e.StartTime.Hour >= 7 && e.StartTime.Hour <= 14) ? 2 : 1) * this.weightcharge * e.Payload.VehicleWeight
                };

                // output the event via the IEnumerable interface
                output.Add(vehicleWeightEvent);
            }

            return output;
        }
    }
}
