#if !NET6_0_OR_GREATER

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace System.Diagnostics;

// https://github.com/dotnet/runtime/blob/v8.0.0/src/libraries/System.Private.CoreLib/src/System/Diagnostics/StackTrace.cs#L186
// TraceFormat is used to specify options for how the string-representation of a StackTrace should be generated.
internal enum TraceFormat
{
    Normal,
    TrailingNewLine,        // include a trailing new line character
}

#endif
