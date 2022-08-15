﻿#if NET5_0_OR_GREATER

#if POLYKIT_PUBLIC
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.CompilerServices.IsExternalInit))]
#endif

#else

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices;

// https://github.com/dotnet/runtime/blob/v5.0.16/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/IsExternalInit.cs

/// <summary>
/// Reserved to be used by the compiler for tracking metadata.
/// This class should not be used by developers in source code.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[ExcludeFromCodeCoverage, DebuggerNonUserCode]
#if POLYKIT_PUBLIC
public
#else
internal
#endif
static class IsExternalInit
{
}

#endif