using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace PolyKit.Diagnostics.CodeAnalysis;

/// <summary>
/// Indicates to Code Analysis that a method validates a parameter,
/// so that if / when the method returns the parameter is known to be non-null.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
[ExcludeFromCodeCoverage, DebuggerNonUserCode]
#if POLYKIT_PUBLIC
public
#else
internal
#endif
sealed class ValidatedNotNullAttribute : Attribute
{
}
