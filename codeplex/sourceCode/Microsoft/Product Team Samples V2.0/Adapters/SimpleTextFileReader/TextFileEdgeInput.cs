﻿// *********************************************************
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

namespace StreamInsight.Samples.Adapters.SimpleTextFileReader
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using Microsoft.ComplexEventProcessing;
    using Microsoft.ComplexEventProcessing.Adapters;

    /// <summary>
    /// Edge version of the simple text file reader input adapter.
    /// <para/>
    /// TextFileEdgeInput adapter enqueues generic point events from a CSV file with the format:
    ///   Start{delimiter}{StartTime}{delimiter}{delimiter}{field1}{delimiter}...{fieldN}
    ///   End{delimiter}{StartTime}{delimiter}{Endtime}{delimiter}{field1}{delimiter}...{fieldN}
    /// The specific file that works with this example is: TextFileEdgeInput.csv
    /// Edge Events are used to model interval events whose EndTime of the interval is not known
    /// at the time of the start of the event. Rather than wait for end of the interval at a cost
    /// to the overall latency of the system, you can specify the Start edge with just the Start time
    /// and the payload values, to be subsequently followed by the end edge. A few notes:
    /// - the EndTime for the Start Edge is a don't care - the engine treats the value as +Infinity.
    ///   In this example, the EndTime for Start Edge is an empty string (between the two successive
    ///   delimiters shown above). This requires some accommodation in the use of split function while
    ///   reading the file input.
    /// - the End Edge must be specified to have the same StartTime and payload fields as the Start Edge.
    /// - every start edge should have a matching end edge.
    /// <para/>
    /// See additional notes in TextFilePointInput.cs
    /// </summary>
    public class TextFileEdgeInput : EdgeInputAdapter
    {
        /// <summary>
        /// Number of system field expected in each line by this adapter.
        /// Edge adapter expects the edge type (start/end) and two timestamps.
        /// </summary>
        private const int NumNonPayloadFields = 3;

        /// <summary>
        /// Text file reader.
        /// </summary>
        private StreamReader streamReader;

        /// <summary>
        /// Delimiting character between each field.
        /// </summary>
        private char delimiter;

        /// <summary>
        /// Culture of the input file. Important for reading DateTime correctly.
        /// </summary>
        private CultureInfo cultureInfo;

        /// <summary>
        /// Mapping of the payload fields in each line with the fields in the stream type.
        /// </summary>
        private Dictionary<int, int> inputOrdinalToCepOrdinal;

        /// <summary>
        /// Stream type received at binding time.
        /// </summary>
        private CepEventType bindtimeEventType;

        /// <summary>
        /// Debug and error output.
        /// </summary>
        private System.Diagnostics.TraceListener consoleTracer;

        /// <summary>
        /// Current line to be enqueued.
        /// </summary>
        private string currentLine;

        /// <summary>
        /// Initializes a new instance of the TextFileEdgeInput class.
        /// </summary>
        /// <param name="configInfo">Configuration structure provided by the Factory. If
        /// the InputFieldOrders member is null, the default mapping will be applied.</param>
        /// <param name="eventType">Represents the actual event type that will be passed into this
        /// constructor from the query binding via the adapter factory class.</param>
        public TextFileEdgeInput(TextFileReaderConfig configInfo, CepEventType eventType)
        {
            this.streamReader = new StreamReader(configInfo.InputFileName);
            this.cultureInfo = new CultureInfo(configInfo.CultureName);
            this.delimiter = configInfo.Delimiter;

            this.bindtimeEventType = eventType;

            // check whether all fields are contained in the config's field-to-column mapping
            if (configInfo.InputFieldOrders != null && configInfo.InputFieldOrders.Count != eventType.Fields.Count)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The configuration element InputFieldOrders should have {0} elements, but it has {1} elements",
                        eventType.Fields.Count,
                        configInfo.InputFieldOrders.Count));
            }

            // Create mapping from input field ordinal to engine field ordinal
            this.inputOrdinalToCepOrdinal = new Dictionary<int, int>();

            for (int i = 0; i < eventType.Fields.Count; i++)
            {
                if (configInfo.InputFieldOrders != null)
                {
                    CepEventTypeField engineField;

                    if (!eventType.Fields.TryGetValue(configInfo.InputFieldOrders[i], out engineField))
                    {
                        throw new ArgumentException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Event type {0} doesn't have an input field named '{1}'",
                                eventType.ShortName,
                                configInfo.InputFieldOrders[i]));
                    }

                    this.inputOrdinalToCepOrdinal.Add(i, engineField.Ordinal);
                }
                else
                {
                    // Use default mapping: lexicographic ordering.
                    this.inputOrdinalToCepOrdinal.Add(i, i);
                }
            }

            this.consoleTracer = new System.Diagnostics.ConsoleTraceListener();
        }

        /// <summary>
        /// Start() is called when the engine wants to let the adapter start producing events.
        /// This method is called on a threadpool thread, which should be released as soon as possible.
        /// </summary>
        public override void Start()
        {
            new Thread(this.ProduceEvents).Start();
        }

        /// <summary>
        /// Resume() is called when the engine is able to receive further events after having pushed
        /// back on an Enqueue call. Resume() will only be called after the adapter called Ready().
        /// This method is called on a threadpool thread, which should be released as soon as possible.
        /// </summary>
        public override void Resume()
        {
            new Thread(this.ProduceEvents).Start();
        }

        /// <summary>
        /// Default Dispose from the base class.
        /// </summary>
        /// <param name="disposing">Indicates whether to free both managed and unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.streamReader.Dispose();
                this.consoleTracer.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Main driver to read events from the CSV file and enqueue them
        /// </summary>
        private void ProduceEvents()
        {
            EdgeEvent currentEvent = default(EdgeEvent);

            try
            {
                // Keep reading lines from the file.
                while (true)
                {
                    if (AdapterState.Stopping == AdapterState)
                    {
                        Stopped();
                        return;
                    }

                    // Did we enqueue the previous line successfully?
                    if (this.currentLine == null)
                    {
                        this.currentLine = this.streamReader.ReadLine();

                        if (this.currentLine == null)
                        {
                            // Stop adapter (and hence the query) at the end of the file.
                            Stopped();
                            return;
                        }
                    }

                    try
                    {
                        // Create and fill event structure with data from text file line.
                        currentEvent = this.CreateEventFromLine(this.currentLine);

                        // In case we just went into the stopping state.
                        if (currentEvent == null)
                        {
                            continue;
                        }
                    }
                    catch (Exception e)
                    {
                        // The line couldn't be transformed into an event.
                        // Just ignore it, and release the event's memory.
                        if (currentEvent != null)
                        {
                            ReleaseEvent(ref currentEvent);
                        }

                        this.consoleTracer.WriteLine(this.currentLine + " could not be read into a CEP event: " + e.Message);

                        // Make sure we read a new line next time.
                        this.currentLine = null;

                        continue;
                    }

                    // in case we went into the stopping state after the check above
                    if (currentEvent == null)
                    {
                        continue;
                    }

                    if (EnqueueOperationResult.Full == Enqueue(ref currentEvent))
                    {
                        // If the enqueue was not successful, we keep the event.
                        // It is good practice to release the event right away and
                        // not hold on to it.
                        ReleaseEvent(ref currentEvent);

                        // We are suspended now. Tell the engine we are ready to be resumed.
                        Ready();

                        // Leave thread to wait for call into Resume().
                        return;
                    }

                    // Enqueue was successful, so we can read a new line again.
                    this.currentLine = null;
                }
            }
            catch (AdapterException e)
            {
                this.consoleTracer.WriteLine("ProduceEvents - " + e.Message + e.StackTrace);
            }
        }

        /// <summary>
        /// Create event from text line.
        /// </summary>
        /// <param name="line">Line read from text file.</param>
        /// <returns>Newly created edge event.</returns>
        private EdgeEvent CreateEventFromLine(string line)
        {
            string[] split = line.Split(new char[] { this.delimiter }, StringSplitOptions.None);
            if (this.bindtimeEventType.Fields.Count != (split.Length - NumNonPayloadFields))
            {
                throw new InvalidOperationException("Number of payload columns in input file: " + (split.Length - NumNonPayloadFields) +
                    " does not match number of fields in EventType: " + this.bindtimeEventType.Fields.Count);
            }

            // Request event memory allocation from engine.
            // Create start or end edge, according to the line.
            EdgeEvent edgeEvent = CreateInsertEvent(("Start" == split[0]) ? EdgeType.Start : EdgeType.End);

            // In case we just went into the stopping state.
            if (edgeEvent == null)
            {
                return null;
            }

            // Set start, end, and new end timestamps.
            edgeEvent.StartTime = DateTime.Parse(split[1], this.cultureInfo, DateTimeStyles.AssumeUniversal).ToUniversalTime();

            if (edgeEvent.EdgeType == EdgeType.End)
            {
                edgeEvent.EndTime = DateTime.Parse(split[2], this.cultureInfo, DateTimeStyles.AssumeUniversal).ToUniversalTime();
            }

            // Populate the payload fields.
            for (int ordinal = 0; ordinal < this.bindtimeEventType.FieldsByOrdinal.Count; ordinal++)
            {
                try
                {
                    int cepOrdinal = this.inputOrdinalToCepOrdinal[ordinal];
                    CepEventTypeField evtField = this.bindtimeEventType.FieldsByOrdinal[cepOrdinal];

                    object value;

                    if (evtField.Type.ClrType.FullName == "System.Byte[]")
                    {
                        System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
                        value = encoding.GetBytes(split[ordinal + NumNonPayloadFields]);
                    }
                    else
                    {
                        Type t = Nullable.GetUnderlyingType(evtField.Type.ClrType) ?? evtField.Type.ClrType;
                        value = Convert.ChangeType(split[ordinal + NumNonPayloadFields], t, this.cultureInfo);
                    }
                    
                    edgeEvent.SetField(cepOrdinal, value);
                }
                catch (AdapterException e)
                {
                    this.consoleTracer.WriteLine(e.Message + e.StackTrace);
                }
            }

            return edgeEvent;
        }
    }
}