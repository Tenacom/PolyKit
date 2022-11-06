using System;

namespace PolyKit.Diagnostics.CodeAnalysis;

/// <summary>
/// Indicates to Code Analysis that a method validates a parameter,
/// so that if / when the method returns the parameter is known to be non-null.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public // polyfill!
sealed class ValidatedNotNullAttribute : Attribute
{
}
