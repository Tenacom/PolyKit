#if NET6_0_OR_GREATER

#if POLYKIT_PUBLIC
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.TimeOnly))]
#endif

#else

// https://github.com/dotnet/runtime/blob/v8.0.0/src/libraries/System.Private.CoreLib/src/System/TimeOnly.cs

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using PolyKit.Diagnostics.CodeAnalysis;

#pragma warning disable CA1305 // Specify IFormatProvider - Consistency with BCL code
#pragma warning disable CA1725 // Parameter names should match base declaration - Consistency with BCL code
#pragma warning disable CA2225 // Provide a method named 'Subtract' as a friendly alternate for operator op_Subtraction - Consistency with BCL code

namespace System
{
    /// <summary>
    /// Represents a time of day, as would be read from a clock, within the range 00:00:00 to 23:59:59.9999999.
    /// </summary>
    public // polyfill!
    readonly struct TimeOnly
        : IComparable,
          IComparable<TimeOnly>,
          IEquatable<TimeOnly>
#if POLYKIT_USE_SPAN
          , ISpanFormattable
#endif
    {
        // represent the number of ticks map to the time of the day. 1 ticks = 100-nanosecond in time measurements.
        private readonly long _ticks;

        // MinTimeTicks is the ticks for the midnight time 00:00:00.000 AM
        private const long MinTimeTicks = 0;

        // MaxTimeTicks is the max tick value for the time in the day. It is calculated using DateTime.Today.AddTicks(-1).TimeOfDay.Ticks.
        private const long MaxTimeTicks = 863_999_999_999;

        /// <summary>
        /// Represents the smallest possible value of <see cref="TimeOnly"/>.
        /// </summary>
        public static TimeOnly MinValue => new TimeOnly((ulong)MinTimeTicks);

        /// <summary>
        /// Represents the largest possible value of <see cref="TimeOnly"/>.
        /// </summary>
        public static TimeOnly MaxValue => new TimeOnly((ulong)MaxTimeTicks);

        /// <summary>
        /// Initializes a new instance of the timeOnly structure to the specified hour and the minute.
        /// </summary>
        /// <param name="hour">The hours (0 through 23).</param>
        /// <param name="minute">The minutes (0 through 59).</param>
        public TimeOnly(int hour, int minute) : this(TimeToTicks(hour, minute, 0, 0)) {}

        /// <summary>
        /// Initializes a new instance of the timeOnly structure to the specified hour, minute, and second.
        /// </summary>
        /// <param name="hour">The hours (0 through 23).</param>
        /// <param name="minute">The minutes (0 through 59).</param>
        /// <param name="second">The seconds (0 through 59).</param>
        public TimeOnly(int hour, int minute, int second) : this(TimeToTicks(hour, minute, second, 0)) {}

        /// <summary>
        /// Initializes a new instance of the timeOnly structure to the specified hour, minute, second, and millisecond.
        /// </summary>
        /// <param name="hour">The hours (0 through 23).</param>
        /// <param name="minute">The minutes (0 through 59).</param>
        /// <param name="second">The seconds (0 through 59).</param>
        /// <param name="millisecond">The millisecond (0 through 999).</param>
        public TimeOnly(int hour, int minute, int second, int millisecond) : this(TimeToTicks(hour, minute, second, millisecond)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeOnly"/> structure to the specified hour, minute, second, and millisecond.
        /// </summary>
        /// <param name="hour">The hours (0 through 23).</param>
        /// <param name="minute">The minutes (0 through 59).</param>
        /// <param name="second">The seconds (0 through 59).</param>
        /// <param name="millisecond">The millisecond (0 through 999).</param>
        /// <param name="microsecond">The microsecond (0 through 999).</param>
        public TimeOnly(int hour, int minute, int second, int millisecond, int microsecond) : this(TimeToTicks(hour, minute, second, millisecond, microsecond)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeOnly"/> structure using a specified number of ticks.
        /// </summary>
        /// <param name="ticks">A time of day expressed in the number of 100-nanosecond units since 00:00:00.0000000.</param>
        public TimeOnly(long ticks)
        {
            if ((ulong)ticks > MaxTimeTicks)
            {
                throw new ArgumentOutOfRangeException(nameof(ticks), "Ticks must be between 0 and and TimeOnly.MaxValue.Ticks.");
            }

            _ticks = ticks;
        }

        // exist to bypass the check in the public constructor.
        internal TimeOnly(ulong ticks) => _ticks = (long)ticks;

        /// <summary>
        /// Gets the hour component of the time represented by this instance.
        /// </summary>
        public int Hour => new TimeSpan(_ticks).Hours;

        /// <summary>
        /// Gets the minute component of the time represented by this instance.
        /// </summary>
        public int Minute => new TimeSpan(_ticks).Minutes;

        /// <summary>
        /// Gets the second component of the time represented by this instance.
        /// </summary>
        public int Second => new TimeSpan(_ticks).Seconds;

        /// <summary>
        /// Gets the millisecond component of the time represented by this instance.
        /// </summary>
        public int Millisecond => new TimeSpan(_ticks).Milliseconds;

        /// <summary>
        /// Gets the microsecond component of the time represented by this instance.
        /// </summary>
        public int Microsecond => (int)((_ticks % TicksPerMillisecond) / TicksPerMicrosecond);

        /// <summary>
        /// Gets the nanosecond component of the time represented by this instance.
        /// </summary>
        public int Nanosecond => (int)((_ticks % TicksPerMicrosecond) * 1000 / TicksPerMicrosecond);

        /// <summary>
        /// Gets the number of ticks that represent the time of this instance.
        /// </summary>
        public long Ticks => _ticks;

        private TimeOnly AddTicks(long ticks) => new TimeOnly((_ticks + TimeSpan.TicksPerDay + (ticks % TimeSpan.TicksPerDay)) % TimeSpan.TicksPerDay);

        private TimeOnly AddTicks(long ticks, out int wrappedDays)
        {
            wrappedDays = (int)(ticks / TimeSpan.TicksPerDay);
            long newTicks = _ticks + ticks % TimeSpan.TicksPerDay;
            if (newTicks < 0)
            {
                wrappedDays--;
                newTicks += TimeSpan.TicksPerDay;
            }
            else
            {
                if (newTicks >= TimeSpan.TicksPerDay)
                {
                    wrappedDays++;
                    newTicks -= TimeSpan.TicksPerDay;
                }
            }

            return new TimeOnly(newTicks);
        }

        /// <summary>
        /// Returns a new <see cref="TimeOnly"/> that adds the value of the specified <see cref="TimeSpan"/> to the value of this instance.
        /// </summary>
        /// <param name="value">A positive or negative time interval.</param>
        /// <returns>An object whose value is the sum of the time represented by this instance and the time interval represented by <paramref name="value"/>.</returns>
        public TimeOnly Add(TimeSpan value) => AddTicks(value.Ticks);

        /// <summary>
        /// Returns a new <see cref="TimeOnly"/> that adds the value of the specified <see cref="TimeSpan"/> to the value of this instance.
        /// If the result wraps past the end of the day, this method will return the number of excess days as an out parameter.
        /// </summary>
        /// <param name="value">A positive or negative time interval.</param>
        /// <param name="wrappedDays">When this method returns, contains the number of excess days if any that resulted from wrapping during this addition operation.</param>
        /// <returns>An object whose value is the sum of the time represented by this instance and the time interval represented by <paramref name="value"/>.</returns>
        public TimeOnly Add(TimeSpan value, out int wrappedDays) => AddTicks(value.Ticks, out wrappedDays);

        /// <summary>
        /// Returns a new <see cref="TimeOnly"/> that adds the specified number of hours to the value of this instance.
        /// </summary>
        /// <param name="value">A number of whole and fractional hours. The value parameter can be negative or positive.</param>
        /// <returns>An object whose value is the sum of the time represented by this instance and the number of hours represented by <paramref name="value"/>.</returns>
        public TimeOnly AddHours(double value) => AddTicks((long)(value * TimeSpan.TicksPerHour));

        /// <summary>
        /// Returns a new <see cref="TimeOnly"/> that adds the specified number of hours to the value of this instance.
        /// If the result wraps past the end of the day, this method will return the number of excess days as an out parameter.
        /// </summary>
        /// <param name="value">A number of whole and fractional hours. The value parameter can be negative or positive.</param>
        /// <param name="wrappedDays">When this method returns, contains the number of excess days if any that resulted from wrapping during this addition operation.</param>
        /// <returns>An object whose value is the sum of the time represented by this instance and the number of hours represented by <paramref name="value"/>.</returns>
        public TimeOnly AddHours(double value, out int wrappedDays) => AddTicks((long)(value * TimeSpan.TicksPerHour), out wrappedDays);

        /// <summary>
        /// Returns a new <see cref="TimeOnly"/> that adds the specified number of minutes to the value of this instance.
        /// </summary>
        /// <param name="value">A number of whole and fractional minutes. The value parameter can be negative or positive.</param>
        /// <returns>An object whose value is the sum of the time represented by this instance and the number of minutes represented by <paramref name="value"/>.</returns>
        public TimeOnly AddMinutes(double value) => AddTicks((long)(value * TimeSpan.TicksPerMinute));

        /// <summary>
        /// Returns a new <see cref="TimeOnly"/> that adds the specified number of minutes to the value of this instance.
        /// If the result wraps past the end of the day, this method will return the number of excess days as an out parameter.
        /// </summary>
        /// <param name="value">A number of whole and fractional minutes. The value parameter can be negative or positive.</param>
        /// <param name="wrappedDays">When this method returns, contains the number of excess days if any that resulted from wrapping during this addition operation.</param>
        /// <returns>An object whose value is the sum of the time represented by this instance and the number of minutes represented by <paramref name="value"/>.</returns>
        public TimeOnly AddMinutes(double value, out int wrappedDays) => AddTicks((long)(value * TimeSpan.TicksPerMinute), out wrappedDays);

        /// <summary>
        /// Determines if a time falls within the range provided.
        /// Supports both "normal" ranges such as 10:00-12:00, and ranges that span midnight such as 23:00-01:00.
        /// </summary>
        /// <param name="start">The starting time of day, inclusive.</param>
        /// <param name="end">The ending time of day, exclusive.</param>
        /// <returns><see langword="true"/> if the time falls within the range; <see langword="false"/> otherwise.</returns>
        /// <remarks>
        /// If <paramref name="start"/> and <paramref name="end"/> are equal, this method returns <see langword="false"/>, meaning there is zero elapsed time between the two values.
        /// If you wish to treat such cases as representing one or more whole days, then first check for equality before calling this method.
        /// </remarks>
        public bool IsBetween(TimeOnly start, TimeOnly end)
        {
            long startTicks = start._ticks;
            long endTicks = end._ticks;

            return startTicks <= endTicks
                ? (startTicks <= _ticks && endTicks > _ticks)
                : (startTicks <= _ticks || endTicks > _ticks);
        }

        /// <summary>
        /// Determines whether two specified instances of <see cref="TimeOnly"/> are equal.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> represent the same time; otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(TimeOnly left, TimeOnly right) => left._ticks == right._ticks;

        /// <summary>
        /// Determines whether two specified instances of <see cref="TimeOnly"/> are not equal.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> do not represent the same time; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(TimeOnly left, TimeOnly right) => left._ticks != right._ticks;

        /// <summary>
        /// Determines whether one specified <see cref="TimeOnly"/> is later than another specified <see cref="TimeOnly"/>.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="left"/> is later than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator >(TimeOnly left, TimeOnly right) => left._ticks > right._ticks;

        /// <summary>
        /// Determines whether one specified <see cref="TimeOnly"/> represents a time that is the same as or later than another specified <see cref="TimeOnly"/>.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="left"/> is the same as or later than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator >=(TimeOnly left, TimeOnly right) => left._ticks >= right._ticks;

        /// <summary>
        /// Determines whether one specified <see cref="TimeOnly"/> is earlier than another specified <see cref="TimeOnly"/>.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="left"/> is earlier than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator <(TimeOnly left, TimeOnly right) => left._ticks < right._ticks;

        /// <summary>
        /// Determines whether one specified <see cref="TimeOnly"/> represents a time that is the same as or earlier than another specified <see cref="TimeOnly"/>.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="left"/> is the same as or earlier than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator <=(TimeOnly left, TimeOnly right) => left._ticks <= right._ticks;

        /// <summary>
        ///  Gives the elapsed time between two points on a circular clock, which will always be a positive value.
        /// </summary>
        /// <param name="t1">The first <see cref="TimeOnly"/> instance.</param>
        /// <param name="t2">The second <see cref="TimeOnly"/> instance..</param>
        /// <returns>The elapsed time between <paramref name="t1"/> and <paramref name="t2"/>.</returns>
        public static TimeSpan operator -(TimeOnly t1, TimeOnly t2) => new TimeSpan((t1._ticks - t2._ticks + TimeSpan.TicksPerDay) % TimeSpan.TicksPerDay);

        /// <summary>
        /// Deconstructs <see cref="TimeOnly"/> by <see cref="Hour"/> and <see cref="Minute"/>.
        /// </summary>
        /// <param name="hour">Deconstructed parameter for <see cref="Hour"/>.</param>
        /// <param name="minute">Deconstructed parameter for <see cref="Minute"/>.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Deconstruct(out int hour, out int minute)
        {
            hour = Hour;
            minute = Minute;
        }

        /// <summary>
        /// Deconstructs <see cref="TimeOnly"/> by <see cref="Hour"/>, <see cref="Minute"/> and <see cref="Second"/>.
        /// </summary>
        /// <param name="hour">Deconstructed parameter for <see cref="Hour"/>.</param>
        /// <param name="minute">Deconstructed parameter for <see cref="Minute"/>.</param>
        /// <param name="second">Deconstructed parameter for <see cref="Second"/>.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Deconstruct(out int hour, out int minute, out int second)
        {
            (hour, minute) = this;
            second = Second;
        }

        /// <summary>
        /// Deconstructs <see cref="TimeOnly"/> by <see cref="Hour"/>, <see cref="Minute"/>, <see cref="Second"/> and <see cref="Millisecond"/>.
        /// </summary>
        /// <param name="hour">Deconstructed parameter for <see cref="Hour"/>.</param>
        /// <param name="minute">Deconstructed parameter for <see cref="Minute"/>.</param>
        /// <param name="second">Deconstructed parameter for <see cref="Second"/>.</param>
        /// <param name="millisecond">Deconstructed parameter for <see cref="Millisecond"/>.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Deconstruct(out int hour, out int minute, out int second, out int millisecond)
        {
            (hour, minute, second) = this;
            millisecond = Millisecond;
        }

        /// <summary>
        /// Deconstructs <see cref="TimeOnly"/> by <see cref="Hour"/>, <see cref="Minute"/>, <see cref="Second"/>, <see cref="Millisecond"/> and <see cref="Microsecond"/>.
        /// </summary>
        /// <param name="hour">Deconstructed parameter for <see cref="Hour"/>.</param>
        /// <param name="minute">Deconstructed parameter for <see cref="Minute"/>.</param>
        /// <param name="second">Deconstructed parameter for <see cref="Second"/>.</param>
        /// <param name="millisecond">Deconstructed parameter for <see cref="Millisecond"/>.</param>
        /// <param name="microsecond">Deconstructed parameter for <see cref="Microsecond"/>.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Deconstruct(out int hour, out int minute, out int second, out int millisecond, out int microsecond)
        {
            (hour, minute, second, millisecond) = this;
            microsecond = Microsecond;
        }

        /// <summary>
        /// Constructs a <see cref="TimeOnly"/> object from a <see cref="TimeSpan"/> representing the time elapsed since midnight.
        /// </summary>
        /// <param name="timeSpan">The time interval measured since midnight. This value has to be positive and not exceeding the time of the day.</param>
        /// <returns>A <see cref="TimeOnly"/> object representing the time elapsed since midnight using the <paramref name="timeSpan"/> value.</returns>
        public static TimeOnly FromTimeSpan(TimeSpan timeSpan) => new TimeOnly(timeSpan.Ticks);

        /// <summary>
        /// Constructs a <see cref="TimeOnly"/> object from a <see cref="DateTime"/> representing the time of the day in this <see cref="DateTime"/> object.
        /// </summary>
        /// <param name="dateTime">The <see cref="DateTime"/> object to extract the time of the day from.</param>
        /// <returns>A <see cref="TimeOnly"/> object representing time of the day specified in <paramref name="dateTime"/>.</returns>
        public static TimeOnly FromDateTime(DateTime dateTime) => new TimeOnly(dateTime.TimeOfDay.Ticks);

        /// <summary>
        /// Convert the current <see cref="TimeOnly"/> instance to a <see cref="TimeSpan"/> object.
        /// </summary>
        /// <returns>A <see cref="TimeSpan"/> object spanning to the time specified in the current <see cref="TimeOnly"/> object.</returns>
        public TimeSpan ToTimeSpan() => new TimeSpan(_ticks);

        internal DateTime ToDateTime() => new DateTime(_ticks);

        /// <summary>
        /// Compares the value of this instance to a specified <see cref="TimeOnly"/> value and indicates whether this instance is earlier than, the same as, or later than the specified <see cref="TimeOnly"/> value.
        /// </summary>
        /// <param name="value">The object to compare to the current instance.</param>
        /// <returns>
        /// Less than zero if this instance is earlier than <paramref name="value"/>.
        /// Zero if this instance is the same as <paramref name="value"/>.
        /// Greater than zero if this instance is later than <paramref name="value"/>.
        /// </returns>
        public int CompareTo(TimeOnly value) => _ticks.CompareTo(value._ticks);

        /// <summary>
        /// Compares the value of this instance to a specified object that contains a specified <see cref="TimeOnly"/> value, and returns an integer that indicates whether this instance is earlier than, the same as, or later than the specified <see cref="TimeOnly"/> value.
        /// </summary>
        /// <param name="value">A boxed object to compare, or <see langword="null"/>.</param>
        /// <returns>
        /// Less than zero if this instance is earlier than <paramref name="value"/>.
        /// Zero if this instance is the same as <paramref name="value"/>.
        /// Greater than zero if this instance is later than <paramref name="value"/>.
        /// </returns>
        public int CompareTo(object? value)
        {
            if (value == null) return 1;
            if (value is not TimeOnly timeOnly)
            {
                throw new ArgumentException("Object must be of type TimeOnly.");
            }

            return CompareTo(timeOnly);
        }

        /// <summary>
        /// Returns a value indicating whether the value of this instance is equal to the value of the specified <see cref="TimeOnly"/> instance.
        /// </summary>
        /// <param name="value">The object to compare to this instance.</param>
        /// <returns><see langword="true"/> if the <paramref name="value"/> parameter equals the value of this instance; otherwise, <see langword="false"/>.</returns>
        public bool Equals(TimeOnly value) => _ticks == value._ticks;

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="value">The object to compare to this instance.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is an instance of <see cref="TimeOnly"/> and equals the value of this instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals([NotNullWhen(true)] object? value) => value is TimeOnly timeOnly && _ticks == timeOnly._ticks;

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            long ticks = _ticks;
            return unchecked((int)ticks) ^ (int)(ticks >> 32);
        }

#if POLYKIT_USE_SPAN

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER

        /// <summary>
        /// Converts a memory span that contains string representation of a time to its <see cref="TimeOnly"/> equivalent by using culture-specific format information and a formatting style.
        /// </summary>
        /// <param name="s">The memory span that contains the string to parse.</param>
        /// <param name="provider">An object that supplies culture-specific format information about <paramref name="s"/>.</param>
        /// <param name="styles">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <returns>An object that is equivalent to the time contained in <paramref name="s"/>,
        /// as specified by <paramref name="provider"/> and <paramref name="styles"/>.</returns>
        public static TimeOnly Parse(ReadOnlySpan<char> s, IFormatProvider? provider = default, DateTimeStyles styles = DateTimeStyles.None)
        {
            return FromDateTime(DateTime.Parse(s, provider, styles));
        }

        /// <summary>
        /// Converts the specified span representation of a time to its <see cref="TimeOnly"/> equivalent using the specified format, culture-specific format information, and style.
        /// The format of the string representation must match the specified format exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A span containing the characters that represent a time to convert.</param>
        /// <param name="format">A span containing the characters that represent a format specifier that defines the required format of <paramref name="s"/>.</param>
        /// <param name="provider">An object that supplies culture-specific format information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <returns>An object that is equivalent to the time contained in <paramref name="s"/>,
        /// as specified by <paramref name="format"/>, <paramref name="provider"/> and <paramref name="style"/>.</returns>
        public static TimeOnly ParseExact(ReadOnlySpan<char> s, [StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)] ReadOnlySpan<char> format, IFormatProvider? provider = default, DateTimeStyles style = DateTimeStyles.None)
        {
            return FromDateTime(DateTime.ParseExact(s, format, provider, style));
        }

        /// <summary>
        /// Converts the specified span representation of a time to its <see cref="TimeOnly"/> equivalent using the specified array of formats.
        /// The format of the string representation must match at least one of the specified formats exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A span containing the characters that represent a time to convert.</param>
        /// <param name="formats">An array of allowable formats of <paramref name="s"/>.</param>
        /// <returns>An object that is equivalent to the time contained in <paramref name="s"/>,
        /// as specified by one of <paramref name="formats"/>.</returns>
        public static TimeOnly ParseExact(ReadOnlySpan<char> s, [StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)] string[] formats) => ParseExact(s, formats, null, DateTimeStyles.None);

        /// <summary>
        /// Converts the specified span representation of a time to its <see cref="TimeOnly"/> equivalent using the specified array of formats, culture-specific format information, and style.
        /// The format of the string representation must match at least one of the specified formats exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A span containing the characters that represent a time to convert.</param>
        /// <param name="formats">An array of allowable formats of <paramref name="s"/>.</param>
        /// <param name="provider">An object that supplies culture-specific format information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <returns>An object that is equivalent to the time contained in <paramref name="s"/>,
        /// as specified by <paramref name="provider"/>, <paramref name="style"/>, and one of <paramref name="formats"/>.</returns>
        public static TimeOnly ParseExact(ReadOnlySpan<char> s, [StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)] string[] formats, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
        {
            return FromDateTime(DateTime.ParseExact(s, formats, provider, style));
        }

#else

        /// <summary>
        /// Converts a memory span that contains string representation of a time to its <see cref="TimeOnly"/> equivalent by using culture-specific format information and a formatting style.
        /// </summary>
        /// <param name="s">The memory span that contains the string to parse.</param>
        /// <param name="provider">An object that supplies culture-specific format information about <paramref name="s"/>.</param>
        /// <param name="styles">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <returns>An object that is equivalent to the time contained in <paramref name="s"/>,
        /// as specified by <paramref name="provider"/> and <paramref name="styles"/>.</returns>
        public static TimeOnly Parse(ReadOnlySpan<char> s, IFormatProvider? provider = default, DateTimeStyles styles = DateTimeStyles.None)
        {
            return FromDateTime(DateTime.Parse(s.ToString(), provider, styles));
        }

        /// <summary>
        /// Converts the specified span representation of a time to its <see cref="TimeOnly"/> equivalent using the specified format, culture-specific format information, and style.
        /// The format of the string representation must match the specified format exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A span containing the characters that represent a time to convert.</param>
        /// <param name="format">A span containing the characters that represent a format specifier that defines the required format of <paramref name="s"/>.</param>
        /// <param name="provider">An object that supplies culture-specific format information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <returns>An object that is equivalent to the time contained in <paramref name="s"/>,
        /// as specified by <paramref name="format"/>, <paramref name="provider"/> and <paramref name="style"/>.</returns>
        public static TimeOnly ParseExact(ReadOnlySpan<char> s, [StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)] ReadOnlySpan<char> format, IFormatProvider? provider = default, DateTimeStyles style = DateTimeStyles.None)
        {
            return FromDateTime(DateTime.ParseExact(s.ToString(), format.ToString(), provider, style));
        }

        /// <summary>
        /// Converts the specified span representation of a time to its <see cref="TimeOnly"/> equivalent using the specified array of formats.
        /// The format of the string representation must match at least one of the specified formats exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A span containing the characters that represent a time to convert.</param>
        /// <param name="formats">An array of allowable formats of <paramref name="s"/>.</param>
        /// <returns>An object that is equivalent to the time contained in <paramref name="s"/>,
        /// as specified by one of <paramref name="formats"/>.</returns>
        public static TimeOnly ParseExact(ReadOnlySpan<char> s, [StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)] string[] formats) => ParseExact(s, formats, null, DateTimeStyles.None);

        /// <summary>
        /// Converts the specified span representation of a time to its <see cref="TimeOnly"/> equivalent using the specified array of formats, culture-specific format information, and style.
        /// The format of the string representation must match at least one of the specified formats exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A span containing the characters that represent a time to convert.</param>
        /// <param name="formats">An array of allowable formats of <paramref name="s"/>.</param>
        /// <param name="provider">An object that supplies culture-specific format information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <returns>An object that is equivalent to the time contained in <paramref name="s"/>,
        /// as specified by <paramref name="provider"/>, <paramref name="style"/>, and one of <paramref name="formats"/>.</returns>
        public static TimeOnly ParseExact(ReadOnlySpan<char> s, [StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)] string[] formats, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
        {
            return FromDateTime(DateTime.ParseExact(s.ToString(), formats, provider, style));
        }

#endif

#endif

        /// <summary>
        /// Converts a string that contains string representation of a time to its <see cref="TimeOnly"/> equivalent by using the conventions of the current culture.
        /// </summary>
        /// <param name="s">The string that contains the string to parse.</param>
        /// <returns>An object that is equivalent to the time contained in <paramref name="s"/>.</returns>
        public static TimeOnly Parse(string s) => Parse(s, null, DateTimeStyles.None);

        /// <summary>
        /// Converts a string that contains string representation of a time to its <see cref="TimeOnly"/> equivalent by using culture-specific format information and a formatting style.
        /// </summary>
        /// <param name="s">The string that contains the string to parse.</param>
        /// <param name="provider">An object that supplies culture-specific format information about <paramref name="s"/>.</param>
        /// <param name="styles">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <returns>An object that is equivalent to the time contained in <paramref name="s"/>,
        /// as specified by <paramref name="provider"/> and <paramref name="styles"/>.</returns>
        public static TimeOnly Parse(string s, IFormatProvider? provider, DateTimeStyles styles = DateTimeStyles.None)
        {
            return FromDateTime(DateTime.Parse(s, provider, styles));
        }

        /// <summary>
        /// Converts the specified string representation of a time to its <see cref="TimeOnly"/> equivalent using the specified format.
        /// The format of the string representation must match the specified format exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A string containing the characters that represent a time to convert.</param>
        /// <param name="format">A string that represent a format specifier that defines the required format of <paramref name="s"/>.</param>
        /// <returns>An object that is equivalent to the time contained in <paramref name="s"/>,
        /// as specified by <paramref name="format"/>.</returns>
        public static TimeOnly ParseExact(string s, [StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)] string format) => ParseExact(s, format, null, DateTimeStyles.None);

        /// <summary>
        /// Converts the specified string representation of a time to its <see cref="TimeOnly"/> equivalent using the specified format, culture-specific format information, and style.
        /// The format of the string representation must match the specified format exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A string containing the characters that represent a time to convert.</param>
        /// <param name="format">A string that represent a format specifier that defines the required format of <paramref name="s"/>.</param>
        /// <returns>An object that is equivalent to the time contained in <paramref name="s"/>,
        /// as specified by <paramref name="format"/>.</returns>
        /// <param name="provider">An object that supplies culture-specific format information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <returns>An object that is equivalent to the time contained in <paramref name="s"/>,
        /// as specified by <paramref name="format"/>, <paramref name="provider"/> and <paramref name="style"/>.</returns>
        public static TimeOnly ParseExact(string s, [StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)] string format, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
        {
            return FromDateTime(DateTime.ParseExact(s, format, provider, style));
        }

        /// <summary>
        /// Converts the specified span representation of a time to its <see cref="TimeOnly"/> equivalent using the specified array of formats.
        /// The format of the string representation must match at least one of the specified formats exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A span containing the characters that represent a time to convert.</param>
        /// <param name="formats">An array of allowable formats of <paramref name="s"/>.</param>
        /// <returns>An object that is equivalent to the time contained in <paramref name="s"/>,
        /// as specified by one of <paramref name="formats"/>.</returns>
        public static TimeOnly ParseExact(string s, [StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)] string[] formats) => ParseExact(s, formats, null, DateTimeStyles.None);

        /// <summary>
        /// Converts the specified string representation of a time to its <see cref="TimeOnly"/> equivalent using the specified array of formats, culture-specific format information, and style.
        /// The format of the string representation must match at least one of the specified formats exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A string containing the characters that represent a time to convert.</param>
        /// <param name="formats">An array of allowable formats of <paramref name="s"/>.</param>
        /// <param name="provider">An object that supplies culture-specific format information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <returns>An object that is equivalent to the time contained in <paramref name="s"/>,
        /// as specified by <paramref name="provider"/>, <paramref name="style"/>, and one of <paramref name="formats"/>.</returns>
        public static TimeOnly ParseExact(string s, [StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)] string[] formats, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
        {
            return FromDateTime(DateTime.ParseExact(s, formats, provider, style));
        }

#if POLYKIT_USE_SPAN

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER

        /// <summary>
        /// Converts the specified span representation of a time to its <see cref="TimeOnly"/> equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A span containing the characters representing the time to convert.</param>
        /// <param name="result">When this method returns, contains the <see cref="TimeOnly"/> value equivalent to the time contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed. The conversion fails if the <paramref name="s"/> parameter is empty string,
        /// or does not contain a valid string representation of a time. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, out TimeOnly result)
        {
            var returnValue = DateTime.TryParse(s, out var dateTime);
            result = returnValue ? FromDateTime(dateTime) : MinValue;
            return returnValue;
        }

        /// <summary>
        /// Converts the specified span representation of a time to its <see cref="TimeOnly"/> equivalent using the specified array of formats, culture-specific format information, and style. And returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A string containing the characters that represent a time to convert.</param>
        /// <param name="provider">An object that supplies culture-specific format information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="TimeOnly"/> value equivalent to the time contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed. The conversion fails if the <paramref name="s"/> parameter is empty string,
        /// or does not contain a valid string representation of a time. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, DateTimeStyles style, out TimeOnly result)
        {
            var returnValue = DateTime.TryParse(s, provider, style, out var dateTime);
            result = returnValue ? FromDateTime(dateTime) : MinValue;
            return returnValue;
        }

        /// <summary>
        /// Converts the specified span representation of a time to its <see cref="TimeOnly"/> equivalent using the specified format and style.
        /// The format of the string representation must match the specified format exactly. The method returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A span containing the characters representing a time to convert.</param>
        /// <param name="format">The required format of <paramref name="s"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="TimeOnly"/> value equivalent to the time contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed. The conversion fails if the <paramref name="s"/> parameter is empty string,
        /// or does not contain a valid string representation of a time. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully; otherwise, <see langword="false"/>.</returns>
        public static bool TryParseExact(ReadOnlySpan<char> s, [StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)] ReadOnlySpan<char> format, out TimeOnly result) => TryParseExact(s, format, null, DateTimeStyles.None, out result);

        /// <summary>
        /// Converts the specified span representation of a time to its <see cref="TimeOnly"/> equivalent using the specified format, culture-specific format information, and style.
        /// The format of the string representation must match the specified format exactly. The method returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A span containing the characters representing a time to convert.</param>
        /// <param name="format">The required format of <paramref name="s"/>.</param>
        /// <param name="provider">An object that supplies culture-specific format information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="TimeOnly"/> value equivalent to the time contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed. The conversion fails if the <paramref name="s"/> parameter is empty string,
        /// or does not contain a valid string representation of a time. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully; otherwise, <see langword="false"/>.</returns>
        public static bool TryParseExact(ReadOnlySpan<char> s, [StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)] ReadOnlySpan<char> format, IFormatProvider? provider, DateTimeStyles style, out TimeOnly result)
        {
            var returnValue = DateTime.TryParseExact(s, format, provider, style, out var dateTime);
            result = returnValue ? FromDateTime(dateTime) : MinValue;
            return returnValue;
        }

        /// <summary>
        /// Converts the specified char span of a time to its <see cref="TimeOnly"/> equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">The span containing the string to parse.</param>
        /// <param name="formats">An array of allowable formats of <paramref name="s"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="TimeOnly"/> value equivalent to the time contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed. The conversion fails if the <paramref name="s"/> parameter is empty string,
        /// or does not contain a valid string representation of a time. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully; otherwise, <see langword="false"/>.</returns>
        public static bool TryParseExact(ReadOnlySpan<char> s, [NotNullWhen(true), StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)] string?[]? formats, out TimeOnly result) => TryParseExact(s, formats, null, DateTimeStyles.None, out result);

        /// <summary>
        /// Converts the specified char span of a time to its <see cref="TimeOnly"/> equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">The span containing the string to parse.</param>
        /// <param name="formats">An array of allowable formats of <paramref name="s"/>.</param>
        /// <param name="provider">An object that supplies culture-specific format information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="TimeOnly"/> value equivalent to the time contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed. The conversion fails if the <paramref name="s"/> parameter is empty string,
        /// or does not contain a valid string representation of a time. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully; otherwise, <see langword="false"/>.</returns>
        public static bool TryParseExact(ReadOnlySpan<char> s, [NotNullWhen(true), StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)] string?[]? formats, IFormatProvider? provider, DateTimeStyles style, out TimeOnly result)
        {
            var returnValue = DateTime.TryParseExact(s, formats, provider, style, out var dateTime);
            result = returnValue ? FromDateTime(dateTime) : MinValue;
            return returnValue;
        }

#else

        /// <summary>
        /// Converts the specified span representation of a time to its <see cref="TimeOnly"/> equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A span containing the characters representing the time to convert.</param>
        /// <param name="result">When this method returns, contains the <see cref="TimeOnly"/> value equivalent to the time contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed. The conversion fails if the <paramref name="s"/> parameter is empty string,
        /// or does not contain a valid string representation of a time. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, out TimeOnly result)
        {
            var returnValue = DateTime.TryParse(s.ToString(), out var dateTime);
            result = returnValue ? FromDateTime(dateTime) : MinValue;
            return returnValue;
        }

        /// <summary>
        /// Converts the specified span representation of a time to its <see cref="TimeOnly"/> equivalent using the specified array of formats, culture-specific format information, and style. And returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A string containing the characters that represent a time to convert.</param>
        /// <param name="provider">An object that supplies culture-specific format information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="TimeOnly"/> value equivalent to the time contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed. The conversion fails if the <paramref name="s"/> parameter is empty string,
        /// or does not contain a valid string representation of a time. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, DateTimeStyles style, out TimeOnly result)
        {
            var returnValue = DateTime.TryParse(s.ToString(), provider, style, out var dateTime);
            result = returnValue ? FromDateTime(dateTime) : MinValue;
            return returnValue;
        }

        /// <summary>
        /// Converts the specified span representation of a time to its <see cref="TimeOnly"/> equivalent using the specified format and style.
        /// The format of the string representation must match the specified format exactly. The method returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A span containing the characters representing a time to convert.</param>
        /// <param name="format">The required format of <paramref name="s"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="TimeOnly"/> value equivalent to the time contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed. The conversion fails if the <paramref name="s"/> parameter is empty string,
        /// or does not contain a valid string representation of a time. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully; otherwise, <see langword="false"/>.</returns>
        public static bool TryParseExact(ReadOnlySpan<char> s, [StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)] ReadOnlySpan<char> format, out TimeOnly result) => TryParseExact(s, format, null, DateTimeStyles.None, out result);

        /// <summary>
        /// Converts the specified span representation of a time to its <see cref="TimeOnly"/> equivalent using the specified format, culture-specific format information, and style.
        /// The format of the string representation must match the specified format exactly. The method returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A span containing the characters representing a time to convert.</param>
        /// <param name="format">The required format of <paramref name="s"/>.</param>
        /// <param name="provider">An object that supplies culture-specific format information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="TimeOnly"/> value equivalent to the time contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed. The conversion fails if the <paramref name="s"/> parameter is empty string,
        /// or does not contain a valid string representation of a time. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully; otherwise, <see langword="false"/>.</returns>
        public static bool TryParseExact(ReadOnlySpan<char> s, [StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)] ReadOnlySpan<char> format, IFormatProvider? provider, DateTimeStyles style, out TimeOnly result)
        {
            var returnValue = DateTime.TryParseExact(s.ToString(), format.ToString(), provider, style, out var dateTime);
            result = returnValue ? FromDateTime(dateTime) : MinValue;
            return returnValue;
        }

        /// <summary>
        /// Converts the specified char span of a time to its <see cref="TimeOnly"/> equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">The span containing the string to parse.</param>
        /// <param name="formats">An array of allowable formats of <paramref name="s"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="TimeOnly"/> value equivalent to the time contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed. The conversion fails if the <paramref name="s"/> parameter is empty string,
        /// or does not contain a valid string representation of a time. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully; otherwise, <see langword="false"/>.</returns>
        public static bool TryParseExact(ReadOnlySpan<char> s, [NotNullWhen(true), StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)] string?[]? formats, out TimeOnly result) => TryParseExact(s, formats, null, DateTimeStyles.None, out result);

        /// <summary>
        /// Converts the specified char span of a time to its <see cref="TimeOnly"/> equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">The span containing the string to parse.</param>
        /// <param name="formats">An array of allowable formats of <paramref name="s"/>.</param>
        /// <param name="provider">An object that supplies culture-specific format information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="TimeOnly"/> value equivalent to the time contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed. The conversion fails if the <paramref name="s"/> parameter is empty string,
        /// or does not contain a valid string representation of a time. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully; otherwise, <see langword="false"/>.</returns>
        public static bool TryParseExact(ReadOnlySpan<char> s, [NotNullWhen(true), StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)] string?[]? formats, IFormatProvider? provider, DateTimeStyles style, out TimeOnly result)
        {
            var returnValue = DateTime.TryParseExact(s.ToString(), formats, provider, style, out var dateTime);
            result = returnValue ? FromDateTime(dateTime) : MinValue;
            return returnValue;
        }

#endif

#endif

        /// <summary>
        /// Converts the specified string representation of a time to its <see cref="TimeOnly"/> equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A string containing the characters representing the time to convert.</param>
        /// <param name="result">When this method returns, contains the <see cref="TimeOnly"/> value equivalent to the time contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed. The conversion fails if the <paramref name="s"/> parameter is empty string,
        /// or does not contain a valid string representation of a time. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, out TimeOnly result) => TryParse(s, null, DateTimeStyles.None, out result);

        /// <summary>
        /// Converts the specified string representation of a time to its <see cref="TimeOnly"/> equivalent using the specified array of formats, culture-specific format information, and style. And returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A string containing the characters that represent a time to convert.</param>
        /// <param name="provider">An object that supplies culture-specific format information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="TimeOnly"/> value equivalent to the time contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed. The conversion fails if the <paramref name="s"/> parameter is empty string,
        /// or does not contain a valid string representation of a time. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, DateTimeStyles style, out TimeOnly result)
        {
            var returnValue = DateTime.TryParse(s, provider, style, out var dateTime);
            result = returnValue ? FromDateTime(dateTime) : MinValue;
            return returnValue;
        }

        /// <summary>
        /// Converts the specified string representation of a time to its <see cref="TimeOnly"/> equivalent using the specified format and style.
        /// The format of the string representation must match the specified format exactly. The method returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A string containing the characters representing a time to convert.</param>
        /// <param name="format">The required format of <paramref name="s"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="TimeOnly"/> value equivalent to the time contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed. The conversion fails if the <paramref name="s"/> parameter is empty string,
        /// or does not contain a valid string representation of a time. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully; otherwise, <see langword="false"/>.</returns>
        public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true), StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)] string? format, out TimeOnly result) => TryParseExact(s, format, null, DateTimeStyles.None, out result);

        /// <summary>
        /// Converts the specified span representation of a time to its <see cref="TimeOnly"/> equivalent using the specified format, culture-specific format information, and style.
        /// The format of the string representation must match the specified format exactly. The method returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A span containing the characters representing a time to convert.</param>
        /// <param name="format">The required format of <paramref name="s"/>.</param>
        /// <param name="provider">An object that supplies culture-specific format information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="TimeOnly"/> value equivalent to the time contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed. The conversion fails if the <paramref name="s"/> parameter is empty string,
        /// or does not contain a valid string representation of a time. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully; otherwise, <see langword="false"/>.</returns>
        public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true), StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)] string? format, IFormatProvider? provider, DateTimeStyles style, out TimeOnly result)
        {
            var returnValue = DateTime.TryParseExact(s, format, provider, style, out var dateTime);
            result = returnValue ? FromDateTime(dateTime) : MinValue;
            return returnValue;
        }

        /// <summary>
        /// Converts the specified string of a time to its <see cref="TimeOnly"/> equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">The string containing time to parse.</param>
        /// <param name="formats">An array of allowable formats of <paramref name="s"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="TimeOnly"/> value equivalent to the time contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed. The conversion fails if the <paramref name="s"/> parameter is empty string,
        /// or does not contain a valid string representation of a time. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully; otherwise, <see langword="false"/>.</returns>
        public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true), StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)] string?[]? formats, out TimeOnly result) => TryParseExact(s, formats, null, DateTimeStyles.None, out result);

        /// <summary>
        /// Converts the specified string of a time to its <see cref="TimeOnly"/> equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">The string containing the time to parse.</param>
        /// <param name="formats">An array of allowable formats of <paramref name="s"/>.</param>
        /// <param name="provider">An object that supplies culture-specific format information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="TimeOnly"/> value equivalent to the time contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed. The conversion fails if the <paramref name="s"/> parameter is empty string,
        /// or does not contain a valid string representation of a time. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully; otherwise, <see langword="false"/>.</returns>
        public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true), StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)] string?[]? formats, IFormatProvider? provider, DateTimeStyles style, out TimeOnly result)
        {
            var returnValue = DateTime.TryParseExact(s, formats, provider, style, out var dateTime);
            result = returnValue ? FromDateTime(dateTime) : MinValue;
            return returnValue;
        }

        /// <summary>
        /// Converts the value of the current <see cref="TimeOnly"/> object to its equivalent long date string representation.
        /// </summary>
        /// <returns>A string that contains the long time string representation of the current <see cref="TimeOnly"/> object.</returns>
        public string ToLongTimeString() => ToString("T");

        /// <summary>
        /// Converts the value of the current <see cref="TimeOnly"/> object to its equivalent short time string representation.
        /// </summary>
        /// <returns>A string that contains the short time string representation of the current <see cref="TimeOnly"/> object.</returns>
        public string ToShortTimeString() => ToString();

        /// <summary>
        /// Converts the value of the current <see cref="TimeOnly"/> object to its equivalent string representation using the formatting conventions of the current culture.
        /// The <see cref="TimeOnly"/> object will be formatted in short form.
        /// </summary>
        /// <returns>A string that contains the short time string representation of the current <see cref="TimeOnly"/> object.</returns>
        public override string ToString() => ToString("t");

        /// <summary>
        /// Converts the value of the current <see cref="TimeOnly"/> object to its equivalent string representation using the specified format and the formatting conventions of the current culture.
        /// </summary>
        /// <param name="format">A standard or custom time format string.</param>
        /// <returns>A string representation of value of the current <see cref="TimeOnly"/> object as specified by <paramref name="format"/>.</returns>
        /// <remarks>The accepted standard formats are 'r', 'R', 'o', 'O', 't' and 'T'. </remarks>
        public string ToString([StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)] string? format)
        {
            if (string.IsNullOrEmpty(format))
            {
                format = "t";
            }

            EnsureValidCustomTimeFormat(format);
            return ToDateTime().ToString(format);
        }

        /// <summary>
        /// Converts the value of the current <see cref="TimeOnly"/> object to its equivalent string representation using the specified culture-specific format information.
        /// </summary>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <returns>A string representation of value of the current <see cref="TimeOnly"/> object as specified by <paramref name="provider"/>.</returns>
        public string ToString(IFormatProvider? provider) => ToString("t", provider);

        /// <summary>
        /// Converts the value of the current <see cref="TimeOnly"/> object to its equivalent string representation using the specified culture-specific format information.
        /// </summary>
        /// <param name="format">A standard or custom time format string.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <returns>A string representation of value of the current <see cref="TimeOnly"/> object
        /// as specified by <paramref name="format"/> and <paramref name="provider"/>.</returns>
        /// <remarks>The accepted standard formats are 'r', 'R', 'o', 'O', 't' and 'T'. </remarks>
        public string ToString([StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)] string? format, IFormatProvider? provider)
        {
            if (string.IsNullOrEmpty(format))
            {
                format = "t";
            }

            EnsureValidCustomTimeFormat(format);
            return ToDateTime().ToString(format);
        }

#if POLYKIT_USE_SPAN

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER

        /// <summary>
        /// Tries to format the value of the current <see cref="TimeOnly"/> instance into the provided span of characters.
        /// </summary>
        /// <param name="destination">When this method returns, this instance's value formatted as a span of characters.</param>
        /// <param name="charsWritten">When this method returns, the number of characters that were written in <paramref name="destination"/>.</param>
        /// <param name="format">A span containing the characters that represent a standard or custom format string that defines the acceptable format for <paramref name="destination"/>.</param>
        /// <param name="provider">An optional object that supplies culture-specific formatting information for <paramref name="destination"/>.</param>
        /// <returns><see langword="true"/> if the formatting was successful; otherwise, <see langword="false"/>.</returns>
        /// <remarks>The accepted standard formats are 'r', 'R', 'o', 'O', 't' and 'T'. </remarks>
        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
        {
            if (format.Length == 0)
            {
                format = "t";
            }

            _ = IsValidCustomTimeFormat(format, true);
            return ToDateTime().TryFormat(destination, out charsWritten, format, provider);
        }

#else

        /// <summary>
        /// Tries to format the value of the current <see cref="TimeOnly"/> instance into the provided span of characters.
        /// </summary>
        /// <param name="destination">When this method returns, this instance's value formatted as a span of characters.</param>
        /// <param name="charsWritten">When this method returns, the number of characters that were written in <paramref name="destination"/>.</param>
        /// <param name="format">A span containing the characters that represent a standard or custom format string that defines the acceptable format for <paramref name="destination"/>.</param>
        /// <param name="provider">An optional object that supplies culture-specific formatting information for <paramref name="destination"/>.</param>
        /// <returns><see langword="true"/> if the formatting was successful; otherwise, <see langword="false"/>.</returns>
        /// <remarks>The accepted standard formats are 'r', 'R', 'o', 'O', 't' and 'T'. </remarks>
        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.TimeOnlyFormat)] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
        {
            var formatStr = format.Length == 0 ? "t" : format.ToString();
            _ = IsValidCustomTimeFormat(formatStr, true);
            var str = ToDateTime().ToString(formatStr, provider);
            charsWritten = str.Length;
            return str.AsSpan().TryCopyTo(destination);
        }

#endif

#endif

        //
        // IParsable
        //

        /// <summary>
        /// Parses a string into a <see cref="TimeOnly"/>.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s"/>.</param>
        /// <returns>The result of parsing <paramref name="s"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException"><paramref name="s"/> is not in the correct format.</exception>
        /// <exception cref="OverflowException"><paramref name="s"/> is not representable by <see cref="TimeOnly"/>.</exception>
        public static TimeOnly Parse(string s, IFormatProvider? provider) => Parse(s, provider, DateTimeStyles.None);

        /// <summary>
        /// Tries to parse a string into a <see cref="TimeOnly"/>.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s"/>.</param>
        /// <param name="result">When this method returns, contains the result of successfully parsing <paramref name="s"/>
        /// or an undefined value on failure.</param>
        /// <returns><see langword="true"/> if s was successfully parsed; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out TimeOnly result) => TryParse(s, provider, DateTimeStyles.None, out result);

#if POLYKIT_USE_SPAN

        //
        // ISpanParsable
        //

        /// <summary>
        /// Parses a span of characters into a <see cref="TimeOnly"/>.
        /// </summary>
        /// <param name="s">The span of characters to parse.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s"/>.</param>
        /// <returns>The result of parsing <paramref name="s"/>.</returns>
        /// <exception cref="FormatException"><paramref name="s"/> is not in the correct format.</exception>
        /// <exception cref="OverflowException"><paramref name="s"/> is not representable by <see cref="TimeOnly"/>.</exception>
        public static TimeOnly Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, provider, DateTimeStyles.None);

        /// <summary>
        /// Tries to parse a span of characters into a <see cref="TimeOnly"/>.
        /// </summary>
        /// <param name="s">The span of characters to parse.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s"/>.</param>
        /// <param name="result">When this method returns, contains the result of successfully parsing <paramref name="s"/>
        /// or an undefined value on failure.</param>
        /// <returns><see langword="true"/> if s was successfully parsed; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out TimeOnly result) => TryParse(s, provider, DateTimeStyles.None, out result);

#endif

// Internal constants ported from DateTime
// https://github.com/dotnet/runtime/blob/v7.0.0/src/libraries/System.Private.CoreLib/src/System/DateTime.cs#L57

        // Number of 100ns ticks per time unit
        private const int MicrosecondsPerMillisecond = 1000;
        private const long TicksPerMicrosecond = 10;
        private const long TicksPerMillisecond = TicksPerMicrosecond * MicrosecondsPerMillisecond;

        private const int HoursPerDay = 24;
        private const long TicksPerSecond = TicksPerMillisecond * 1000;
        private const long TicksPerMinute = TicksPerSecond * 60;
        private const long TicksPerHour = TicksPerMinute * 60;
        private const long TicksPerDay = TicksPerHour * HoursPerDay;

        // Number of milliseconds per time unit
        private const int MillisPerSecond = 1000;

        // Number of days in a non-leap year
        private const int DaysPerYear = 365;
        // Number of days in 4 years
        private const int DaysPer4Years = DaysPerYear * 4 + 1;       // 1461
        // Number of days in 100 years
        private const int DaysPer100Years = DaysPer4Years * 25 - 1;  // 36524
        // Number of days in 400 years
        private const int DaysPer400Years = DaysPer100Years * 4 + 1; // 146097

        // Number of days from 1/1/0001 to 12/31/9999
        private const int DaysTo10000 = DaysPer400Years * 25 - 366;  // 3652059

        internal const long MaxTicks = DaysTo10000 * TicksPerDay - 1;

// Throw helper methods ported from DateTime
// https://github.com/dotnet/runtime/blob/v7.0.0/src/libraries/System.Private.CoreLib/src/System/DateTime.cs#L190

        private static void ThrowMillisecondOutOfRange() => throw new ArgumentOutOfRangeException("millisecond", $"Valid values are between 0 and {MillisPerSecond - 1}, inclusive.");

        private static void ThrowMicrosecondOutOfRange() => throw new ArgumentOutOfRangeException("microsecond", $"Valid values are between 0 and {MicrosecondsPerMillisecond - 1}, inclusive.");

// TimeToTicks methods ported from DateTime
// https://github.com/dotnet/runtime/blob/v7.0.0/src/libraries/System.Private.CoreLib/src/System/DateTime.cs#L1095

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong TimeToTicks(int hour, int minute, int second)
        {
            if ((uint)hour >= 24 || (uint)minute >= 60 || (uint)second >= 60)
            {
                ThrowArgumentOutOfRange_BadHourMinuteSecond();
            }

            int totalSeconds = hour * 3600 + minute * 60 + second;
            return (uint)totalSeconds * (ulong)TicksPerSecond;

            static void ThrowArgumentOutOfRange_BadHourMinuteSecond()
                => throw new ArgumentOutOfRangeException(nameof(hour), "Hour, Minute, and Second parameters describe an un-representable DateTime.");
        }

        private static ulong TimeToTicks(int hour, int minute, int second, int millisecond)
        {
            ulong ticks = TimeToTicks(hour, minute, second);

            if ((uint)millisecond >= MillisPerSecond) ThrowMillisecondOutOfRange();

            ticks += (uint)millisecond * (uint)TicksPerMillisecond;

            Debug.Assert(ticks <= MaxTicks, "Input parameters validated already");

            return ticks;
        }

        internal static ulong TimeToTicks(int hour, int minute, int second, int millisecond, int microsecond)
        {
            ulong ticks = TimeToTicks(hour, minute, second, millisecond);

            if ((uint)microsecond >= MicrosecondsPerMillisecond) ThrowMicrosecondOutOfRange();

            ticks += (uint)microsecond * (uint)TicksPerMicrosecond;

            Debug.Assert(ticks <= MaxTicks, "Input parameters validated already");

            return ticks;
        }

// The following is an internal method of class System.Globalization.DateTimeFormat
// https://github.com/dotnet/runtime/blob/v7.0.0/src/libraries/System.Private.CoreLib/src/System/Globalization/DateTimeFormat.cs#L1144
// The string version is just a copy+paste of the same method with the parameter type changed, for platforms with no Span support
#if POLYKIT_USE_SPAN

        internal static bool IsValidCustomTimeFormat(ReadOnlySpan<char> format, bool throwOnError)
        {
            int length = format.Length;
            int i = 0;

            while (i < length)
            {
                switch (format[i])
                {
                    case '\\':
                        if (i == length - 1)
                        {
                            if (throwOnError)
                            {
                                throw new FormatException("Input string was not in a correct format.");
                            }

                            return false;
                        }

                        i += 2;
                        break;

                    case '\'':
                    case '"':
                        char quoteChar = format[i++];
                        while (i < length && format[i] != quoteChar)
                        {
                            i++;
                        }

                        if (i >= length)
                        {
                            if (throwOnError)
                            {
                                throw new FormatException($"Cannot find a matching quote character for the character '{quoteChar}'.");
                            }

                            return false;
                        }

                        i++;
                        break;

                    case 'd':
                    case 'M':
                    case 'y':
                    case '/':
                    case 'z':
                    case 'k':
                        if (throwOnError)
                        {
                            throw new FormatException("Input string was not in a correct format.");
                        }

                        return false;

                    default:
                        i++;
                        break;
                }
            }

            return true;
        }

        internal static bool IsValidCustomTimeFormat(string format, bool throwOnError)
            => IsValidCustomTimeFormat(format.AsSpan(), throwOnError);

#else

        internal static bool IsValidCustomTimeFormat(string format, bool throwOnError)
        {
            int length = format.Length;
            int i = 0;

            while (i < length)
            {
                switch (format[i])
                {
                    case '\\':
                        if (i == length - 1)
                        {
                            if (throwOnError)
                            {
                                throw new FormatException("Input string was not in a correct format.");
                            }

                            return false;
                        }

                        i += 2;
                        break;

                    case '\'':
                    case '"':
                        char quoteChar = format[i++];
                        while (i < length && format[i] != quoteChar)
                        {
                            i++;
                        }

                        if (i >= length)
                        {
                            if (throwOnError)
                            {
                                throw new FormatException($"Cannot find a matching quote character for the character '{quoteChar}'.");
                            }

                            return false;
                        }

                        i++;
                        break;

                    case 'd':
                    case 'M':
                    case 'y':
                    case '/':
                    case 'z':
                    case 'k':
                        if (throwOnError)
                        {
                            throw new FormatException("Input string was not in a correct format.");
                        }

                        return false;

                    default:
                        i++;
                        break;
                }
            }

            return true;
        }

#endif

        [DoesNotReturn]
        private static void ThrowArgumentNullException(string name) => throw new ArgumentNullException(name);

        private static void EnsureValidCustomTimeFormat([ValidatedNotNull]string? format)
        {
            if (format == null) ThrowArgumentNullException(nameof(format));
            _ = IsValidCustomTimeFormat(format, true);
        }
    }
}

#endif
