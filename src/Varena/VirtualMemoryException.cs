// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace Varena;

/// <summary>
/// The main exception for arena related allocation. problems.
/// </summary>
public sealed class VirtualMemoryException : Exception
{
    /// <summary>
    /// Creates a new instance of this exception.
    /// </summary>
    /// <param name="message"></param>
    public VirtualMemoryException(string? message) : base(message)
    {
    }
}