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

namespace StreamInsight.Samples.UserExtensions.Afa
{
    using Microsoft.ComplexEventProcessing;

    /// <summary>
    /// The delegate for arcs (transitions) in an AFA.
    /// </summary>
    /// <typeparam name="TInput">The input event type.</typeparam>
    /// <typeparam name="TRegister">The register type.</typeparam>
    /// <param name="inputEvent">The input event.</param>
    /// <param name="oldRegister">The old register value.</param>
    /// <param name="newRegister">Output the new register value after transition.</param>
    /// <returns></returns>
    public delegate bool TransitionDelegate<TInput, TRegister>(PointEvent<TInput> inputEvent, TRegister oldRegister, out TRegister newRegister);
}