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

namespace StreamInsight.Samples.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using Microsoft.ComplexEventProcessing;

    public abstract class TextFileObserverBase<TPayload, TEvent> : IObserver<TEvent>
        where TEvent : TypedEvent<TPayload>
    {
        private readonly StreamWriter writer;
        private readonly string delimiter;

        public string Delimiter { get { return delimiter; } }

        public void OnCompleted()
        {
            writer.Flush();
            writer.Close();
        }

        public void OnNext(TEvent currentEvent)
        {
            OnNext(currentEvent, this.writer);
        }

        protected abstract void OnNext(TEvent currentEvent, StreamWriter writer);

        public void OnError(Exception error)
        {
            Console.WriteLine("Error occurred: " + error);
            writer.Flush();
            writer.Close();
        }

        public TextFileObserverBase(string fileName, string delimiter)
        {
            writer = new StreamWriter(fileName);
            this.delimiter = delimiter;
        }

        /// <summary>
        /// Generate the payload output
        /// </summary>
        protected void SerializePayload(StreamWriter writer, TEvent currentEvent)
        {
            var PositionValuePairs = new Dictionary<uint, string>();
            foreach (PropertyInfo property in currentEvent.Payload.GetType().GetProperties())
            {
                object[] attributes = property.GetCustomAttributes(typeof(ImportFieldAttribute), false);
                foreach (object attribute in attributes)
                {
                    var field = (ImportFieldAttribute)attribute;
                    PositionValuePairs.Add((uint)field.Position, property.Name + " = " + property.GetValue(currentEvent.Payload, null).ToString());
                }
            }

            var keys = new List<uint>(PositionValuePairs.Keys);
            foreach (uint key in keys)
            {
                writer.Write(PositionValuePairs[key]);
                writer.Write(delimiter);
            }
        }

        /// <summary>
        /// Process time by culture
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        protected DateTime GetTimeByCulture(string time)
        {
            return DateTime.Parse(time, CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal);
        }
    }
}