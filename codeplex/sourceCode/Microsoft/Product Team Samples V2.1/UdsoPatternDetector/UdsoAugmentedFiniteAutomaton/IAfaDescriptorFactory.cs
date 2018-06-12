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
    /// <summary>
    /// The interface that query writers implement to define their AFA.
    /// </summary>
    /// <typeparam name="TInput">The event type.</typeparam>
    /// <typeparam name="TRegister">The register type.</typeparam>
    public interface IAfaDescriptorFactory<TInput, TRegister>
    {
        AfaDescriptor<TInput, TRegister> CreateDescriptor();
    }
}
