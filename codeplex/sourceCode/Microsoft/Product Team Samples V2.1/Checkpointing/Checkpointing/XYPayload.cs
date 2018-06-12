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

namespace Checkpointing
{
    /// <summary>
    /// A simple payload containing an x and y coordinate.
    /// </summary>
    public class XYPayload
    {
        /// <summary>
        /// Initializes a new instance of the XYPayload class with a default (0,0) payload.
        /// </summary>
        public XYPayload()
        {
        }

        /// <summary>
        /// Initializes a new instance of the XYPayload class with a specified payload.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        public XYPayload(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }

        /// <summary>
        /// Gets or sets the y coordinate of the payload.
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Gets or sets the x coordinate of the payload.
        /// </summary>
        public double X { get; set; }

        public override string ToString()
        {
            return string.Format("{0},{1}", X, Y);
        }
    }
}