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
    /// <summary>
    /// Define various properties that define a field in a CSV file
    /// position is required, as it is will be used in the class constructor, need to be explicitly passed
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ImportFieldAttribute : Attribute
    {
        /// <summary>
        /// The position of the field
        /// </summary>
        private readonly int position;
        public int Position { get { return position; } }

        public ImportFieldAttribute(int position)
        {
            this.position = position; 
        }
    }
}
