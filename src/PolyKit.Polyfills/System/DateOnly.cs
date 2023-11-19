#if NET6_0_OR_GREATER

#if POLYKIT_PUBLIC
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.DateOnly))]
#endif

#else

// https://github.com/dotnet/runtime/blob/v8.0.0/src/libraries/System.Private.CoreLib/src/System/DateOnly.cs

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using PolyKit.Diagnostics.CodeAnalysis;

#pragma warning disable CA1305 // Specify IFormatProvider - Consistency with BCL code
#pragma warning disable CA1725 // Parameter names should match base declaration - Consistency with BCL code

namespace System
{
    /// <summary>
    /// Represents dates with values ranging from January 1, 0001 Anno Domini (Common Era) through December 31, 9999 A.D. (C.E.) in the Gregorian calendar.
    /// </summary>
    public // polyfill!
    readonly struct DateOnly
        : IComparable,
          IComparable<DateOnly>,
          IEquatable<DateOnly>
#if POLYKIT_USE_SPAN
        , ISpanFormattable
#endif
    {
        private readonly int _dayNumber;

        // Maps to Jan 1st year 1
        private const int MinDayNumber = 0;

        // Maps to December 31 year 9999. The value calculated from "new DateTime(9999, 12, 31).Ticks / TimeSpan.TicksPerDay"
        private const int MaxDayNumber = 3_652_058;

        private static int DayNumberFromDateTime(DateTime dt) => (int)((ulong)dt.Ticks / TimeSpan.TicksPerDay);

        private DateTime GetEquivalentDateTime() => new DateTime(_dayNumber * TimeSpan.TicksPerDay);

        private DateOnly(int dayNumber)
        {
            Debug.Assert((uint)dayNumber <= MaxDayNumber);
            _dayNumber = dayNumber;
        }

        /// <summary>
        /// Gets the earliest possible date that can be created.
        /// </summary>
        public static DateOnly MinValue => new DateOnly(MinDayNumber);

        /// <summary>
        /// Gets the latest possible date that can be created.
        /// </summary>
        public static DateOnly MaxValue => new DateOnly(MaxDayNumber);

        /// <summary>
        /// Creates a new instance of the <see cref="DateOnly"/> structure to the specified year, month, and day.
        /// </summary>
        /// <param name="year">The year (1 through 9999).</param>
        /// <param name="month">The month (1 through 12).</param>
        /// <param name="day">The day (1 through the number of days in <paramref name="month"/>).</param>
        public DateOnly(int year, int month, int day) => _dayNumber = DayNumberFromDateTime(new DateTime(year, month, day));

        /// <summary>
        /// Creates a new instance of the <see cref="DateOnly"/> structure to the specified year, month, and day for the specified calendar.
        /// </summary>
        /// <param name="year">The year (1 through the number of years in calendar).</param>
        /// <param name="month">The month (1 through the number of months in calendar).</param>
        /// <param name="day">The day (1 through the number of days in <paramref name="month"/>).</param>
        /// <param name="calendar">The calendar that is used to interpret <paramref name="year"/>, <paramref name="month"/>, and <paramref name="day"/>.</param>
        public DateOnly(int year, int month, int day, Calendar calendar) => _dayNumber = DayNumberFromDateTime(new DateTime(year, month, day, calendar));

        /// <summary>
        /// Creates a new instance of the <see cref="DateOnly"/> structure to the specified number of days.
        /// </summary>
        /// <param name="dayNumber">The number of days since January 1, 0001 in the Proleptic Gregorian calendar.</param>
        public static DateOnly FromDayNumber(int dayNumber)
        {
            if ((uint)dayNumber > MaxDayNumber)
            {
                ThrowArgumentOutOfRange_DayNumber();
            }

            return new DateOnly(dayNumber);

            static void ThrowArgumentOutOfRange_DayNumber()
                => throw new ArgumentOutOfRangeException(nameof(dayNumber), "Day number must be between 0 and DateOnly.MaxValue.DayNumber.");
        }

        /// <summary>
        /// Gets the year component of the date represented by this instance.
        /// </summary>
        public int Year => GetEquivalentDateTime().Year;

        /// <summary>
        /// Gets the month component of the date represented by this instance.
        /// </summary>
        public int Month  => GetEquivalentDateTime().Month;

        /// <summary>
        /// Gets the day component of the date represented by this instance.
        /// </summary>
        public int Day => GetEquivalentDateTime().Day;

        /// <summary>
        /// Gets the day of the week represented by this instance.
        /// </summary>
        public DayOfWeek DayOfWeek => (DayOfWeek)(((uint)_dayNumber + 1) % 7);

        /// <summary>
        /// Gets the day of the year represented by this instance.
        /// </summary>
        public int DayOfYear => GetEquivalentDateTime().DayOfYear;

        /// <summary>
        /// Gets the number of days since January 1, 0001 in the Proleptic Gregorian calendar represented by this instance.
        /// </summary>
        public int DayNumber => _dayNumber;

        /// <summary>
        /// Adds the specified number of days to the value of this instance.
        /// </summary>
        /// <param name="value">The number of days to add. To subtract days, specify a negative number.</param>
        /// <returns>An instance whose value is the sum of the date represented by this instance
        /// and the number of days represented by <paramref name="value"/>.</returns>
        public DateOnly AddDays(int value)
        {
            int newDayNumber = _dayNumber + value;
            if ((uint)newDayNumber > MaxDayNumber)
            {
                ThrowOutOfRange();
            }

            return new DateOnly(newDayNumber);

            static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(value), "Value to add was out of range.");
        }

        /// <summary>
        /// Adds the specified number of months to the value of this instance.
        /// </summary>
        /// <param name="value">A number of months. The months parameter can be negative or positive.</param>
        /// <returns>An object whose value is the sum of the date represented by this instance
        /// and the number of months represented by <paramref name="value"/>.</returns>
        public DateOnly AddMonths(int value) => new DateOnly(DayNumberFromDateTime(GetEquivalentDateTime().AddMonths(value)));

        /// <summary>
        /// Adds the specified number of years to the value of this instance.
        /// </summary>
        /// <param name="value">A number of years. The value parameter can be negative or positive.</param>
        /// <returns>An object whose value is the sum of the date represented by this instance
        /// and the number of years represented by <paramref name="value"/>.</returns>
        public DateOnly AddYears(int value) => new DateOnly(DayNumberFromDateTime(GetEquivalentDateTime().AddYears(value)));

        /// <summary>
        /// Determines whether two specified instances of <see cref="DateOnly"/> are equal.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> represent the same date;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(DateOnly left, DateOnly right) => left._dayNumber == right._dayNumber;

        /// <summary>
        /// Determines whether two specified instances of <see cref="DateOnly"/> are not equal.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> do not represent the same date;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(DateOnly left, DateOnly right) => left._dayNumber != right._dayNumber;

        /// <summary>
        /// Determines whether one specified <see cref="DateOnly"/> is later than another specified <see cref="DateOnly"/>.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="left"/> is later than <paramref name="right"/>;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool operator >(DateOnly left, DateOnly right) => left._dayNumber > right._dayNumber;

        /// <summary>
        /// Determines whether one specified <see cref="DateOnly"/> represents a date that is the same as or later than another specified <see cref="DateOnly"/>.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="left"/> is the same as or later than <paramref name="right"/>;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool operator >=(DateOnly left, DateOnly right) => left._dayNumber >= right._dayNumber;

        /// <summary>
        /// Determines whether one specified <see cref="DateOnly"/> is earlier than another specified <see cref="DateOnly"/> .
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="left"/> is earlier than <paramref name="right"/>;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool operator <(DateOnly left, DateOnly right) => left._dayNumber < right._dayNumber;

        /// <summary>
        /// Determines whether one specified <see cref="DateOnly"/> represents a date that is the same as or earlier than another specified <see cref="DateOnly"/>.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="left"/> is the same as or earlier than <paramref name="right"/>;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool operator <=(DateOnly left, DateOnly right) => left._dayNumber <= right._dayNumber;

        /// <summary>
        /// Returns a <see cref="DateTime"/> that is set to the date of this <see cref="DateOnly"/> instance and the time of specified input time.
        /// </summary>
        /// <param name="time">The time of the day.</param>
        /// <returns>The <see cref="DateTime"/> instance composed of the date of the current <see cref="DateOnly"/> instance and the time specified by the input time.</returns>
        public DateTime ToDateTime(TimeOnly time) => new DateTime(_dayNumber * TimeSpan.TicksPerDay + time.Ticks);

        /// <summary>
        /// Returns a <see cref="DateTime"/> instance with the specified input kind that is set to the date of this <see cref="DateOnly"/> instance and the time of specified input time.
        /// </summary>
        /// <param name="time">The time of the day.</param>
        /// <param name="kind">One of the enumeration values that indicates whether ticks specifies a local time, Coordinated Universal Time (UTC), or neither.</param>
        /// <returns>The <see cref="DateTime"/> instance composed of the date of the current <see cref="DateOnly"/> instance and the time specified by the input time.</returns>
        public DateTime ToDateTime(TimeOnly time, DateTimeKind kind) => new DateTime(_dayNumber * TimeSpan.TicksPerDay + time.Ticks, kind);

        /// <summary>
        /// Returns a <see cref="DateOnly"/> instance that is set to the date part of the specified <see cref="DateTime"/>.
        /// </summary>
        /// <param name="dateTime">The <see cref="DateTime"/> instance.</param>
        /// <returns>The <see cref="DateOnly"/> instance composed of the date part of <paramref name="dateTime"/>.</returns>
        public static DateOnly FromDateTime(DateTime dateTime) => new DateOnly(DayNumberFromDateTime(dateTime));

        /// <summary>
        /// Compares the value of this instance to a specified <see cref="DateOnly"/> value and returns an integer that indicates whether this instance is earlier than, the same as, or later than the specified value.
        /// </summary>
        /// <param name="value">The object to compare to the current instance.</param>
        /// <returns>Less than zero if this instance is earlier than <paramref name="value"/>.
        /// Greater than zero if this instance is later than <paramref name="value"/>.
        /// Zero if this instance is the same as <paramref name="value"/>.</returns>
        public int CompareTo(DateOnly value) => _dayNumber.CompareTo(value._dayNumber);

        /// <summary>
        /// Compares the value of this instance to a specified object that contains a specified <see cref="DateOnly"/> value, and returns an integer that indicates whether this instance is earlier than, the same as, or later than the specified value.
        /// </summary>
        /// <param name="value">A boxed object to compare, or <see langword="null"/>.</param>
        /// <returns>Less than zero if this instance is earlier than <paramref name="value"/>.
        /// Greater than zero if this instance is later than <paramref name="value"/>.
        /// Zero if this instance is the same as <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="value"/> is neither <see langword="null"/> nor an instance of <see cref="DateOnly"/>.</exception>
        public int CompareTo(object? value)
        {
            if (value == null) return 1;
            if (value is not DateOnly dateOnly)
            {
                throw new ArgumentException("Object must be of type DateOnly.");
            }

            return CompareTo(dateOnly);
        }

        /// <summary>
        /// Returns a value indicating whether the value of this instance is equal to the value of the specified <see cref="DateOnly"/> instance.
        /// </summary>
        /// <param name="value">The object to compare to this instance.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> equals the value of this instance;
        /// otherwise, <see langword="false"/>.</returns>
        public bool Equals(DateOnly value) => _dayNumber == value._dayNumber;

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="value">The object to compare to this instance.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is an instance of <see cref="DateOnly"/> and equals the value of this instance;
        /// otherwise, <see langword="false"/>.</returns>
        public override bool Equals([NotNullWhen(true)] object? value) => value is DateOnly dateOnly && _dayNumber == dateOnly._dayNumber;

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode() => _dayNumber;

#if POLYKIT_USE_SPAN

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER

        /// <summary>
        /// Converts a memory span that contains string representation of a date to its <see cref="DateOnly"/> equivalent
        /// by using culture-specific format information and a formatting style.
        /// </summary>
        /// <param name="s">The memory span that contains the string to parse.</param>
        /// <param name="provider">An object that supplies culture-specific format information about <paramref name="s"/>.</param>
        /// <param name="styles">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <returns>An object that is equivalent to the date contained in <paramref name="s"/>,
        /// as specified by <paramref name="provider"/> and <paramref name="styles"/>.</returns>
        /// <exception cref="FormatException"><paramref name="s"/> does not contain a valid string representation of a date.</exception>
        public static DateOnly Parse(ReadOnlySpan<char> s, IFormatProvider? provider = default, DateTimeStyles styles = DateTimeStyles.None)
        {
            return FromDateTime(DateTime.Parse(s, provider, styles));
        }

        /// <summary>
        /// Converts the specified span representation of a date to its <see cref="DateOnly"/> equivalent using the specified format, culture-specific format information, and style.
        /// The format of the string representation must match the specified format exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A span containing the characters that represent a date to convert.</param>
        /// <param name="format">A span containing the characters that represent a format specifier that defines the required format of <paramref name="s"/>.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <returns>An object that is equivalent to the date contained in <paramref name="s"/>,
        /// as specified by <paramref name="format"/>, <paramref name="provider"/>, and <paramref name="style"/>.</returns>
        /// <exception cref="FormatException"><paramref name="s"/> does not contain a valid string representation of a date.</exception>
        public static DateOnly ParseExact(ReadOnlySpan<char> s, [StringSyntax(StringSyntaxAttribute.DateOnlyFormat)] ReadOnlySpan<char> format, IFormatProvider? provider = default, DateTimeStyles style = DateTimeStyles.None)
        {
            return FromDateTime(DateTime.ParseExact(s, format, provider, style));
        }

        /// <summary>
        /// Converts the specified span representation of a date to its <see cref="DateOnly"/> equivalent using the specified array of formats.
        /// The format of the string representation must match at least one of the specified formats exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A span containing the characters that represent a date to convert.</param>
        /// <param name="formats">An array of allowable formats of <paramref name="s"/>.</param>
        /// <returns>An object that is equivalent to the date contained in <paramref name="s"/>,
        /// as specified by one of <paramref name="formats"/>.</returns>
        /// <exception cref="FormatException"><paramref name="s"/> does not contain a valid string representation of a date.</exception>
        public static DateOnly ParseExact(ReadOnlySpan<char> s, [StringSyntax(StringSyntaxAttribute.DateOnlyFormat)] string[] formats) => ParseExact(s, formats, null, DateTimeStyles.None);

        /// <summary>
        /// Converts the specified span representation of a date to its <see cref="DateOnly"/> equivalent using the specified array of formats, culture-specific format information, and style.
        /// The format of the string representation must match at least one of the specified formats exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A span containing the characters that represent a date to convert.</param>
        /// <param name="formats">An array of allowable formats of <paramref name="s"/>.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <returns>An object that is equivalent to the date contained in <paramref name="s"/>,
        /// as specified by <paramref name="provider"/>, <paramref name="style"/>, and one of <paramref name="formats"/>.</returns>
        /// <exception cref="FormatException"><paramref name="s"/> does not contain a valid string representation of a date.</exception>
        public static DateOnly ParseExact(ReadOnlySpan<char> s, [StringSyntax(StringSyntaxAttribute.DateOnlyFormat)] string[] formats, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
        {
            return FromDateTime(DateTime.ParseExact(s, formats, provider, style));
        }

#else

        /// <summary>
        /// Converts a memory span that contains string representation of a date to its <see cref="DateOnly"/> equivalent
        /// by using culture-specific format information and a formatting style.
        /// </summary>
        /// <param name="s">The memory span that contains the string to parse.</param>
        /// <param name="provider">An object that supplies culture-specific format information about <paramref name="s"/>.</param>
        /// <param name="styles">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <returns>An object that is equivalent to the date contained in <paramref name="s"/>,
        /// as specified by <paramref name="provider"/> and <paramref name="styles"/>.</returns>
        /// <exception cref="FormatException"><paramref name="s"/> does not contain a valid string representation of a date.</exception>
        public static DateOnly Parse(ReadOnlySpan<char> s, IFormatProvider? provider = default, DateTimeStyles styles = DateTimeStyles.None)
        {
            return FromDateTime(DateTime.Parse(s.ToString(), provider, styles));
        }

        /// <summary>
        /// Converts the specified span representation of a date to its <see cref="DateOnly"/> equivalent using the specified format, culture-specific format information, and style.
        /// The format of the string representation must match the specified format exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A span containing the characters that represent a date to convert.</param>
        /// <param name="format">A span containing the characters that represent a format specifier that defines the required format of <paramref name="s"/>.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <returns>An object that is equivalent to the date contained in <paramref name="s"/>,
        /// as specified by <paramref name="format"/>, <paramref name="provider"/>, and <paramref name="style"/>.</returns>
        /// <exception cref="FormatException"><paramref name="s"/> does not contain a valid string representation of a date.</exception>
        public static DateOnly ParseExact(ReadOnlySpan<char> s, [StringSyntax(StringSyntaxAttribute.DateOnlyFormat)] ReadOnlySpan<char> format, IFormatProvider? provider = default, DateTimeStyles style = DateTimeStyles.None)
        {
            return FromDateTime(DateTime.ParseExact(s.ToString(), format.ToString(), provider, style));
        }

        /// <summary>
        /// Converts the specified span representation of a date to its <see cref="DateOnly"/> equivalent using the specified array of formats.
        /// The format of the string representation must match at least one of the specified formats exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A span containing the characters that represent a date to convert.</param>
        /// <param name="formats">An array of allowable formats of <paramref name="s"/>.</param>
        /// <returns>An object that is equivalent to the date contained in <paramref name="s"/>,
        /// as specified by one of <paramref name="formats"/>.</returns>
        /// <exception cref="FormatException"><paramref name="s"/> does not contain a valid string representation of a date.</exception>
        public static DateOnly ParseExact(ReadOnlySpan<char> s, [StringSyntax(StringSyntaxAttribute.DateOnlyFormat)] string[] formats) => ParseExact(s, formats, null, DateTimeStyles.None);

        /// <summary>
        /// Converts the specified span representation of a date to its <see cref="DateOnly"/> equivalent using the specified array of formats, culture-specific format information, and style.
        /// The format of the string representation must match at least one of the specified formats exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A span containing the characters that represent a date to convert.</param>
        /// <param name="formats">An array of allowable formats of <paramref name="s"/>.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <returns>An object that is equivalent to the date contained in <paramref name="s"/>,
        /// as specified by <paramref name="provider"/>, <paramref name="style"/>, and one of <paramref name="formats"/>.</returns>
        /// <exception cref="FormatException"><paramref name="s"/> does not contain a valid string representation of a date.</exception>
        public static DateOnly ParseExact(ReadOnlySpan<char> s, [StringSyntax(StringSyntaxAttribute.DateOnlyFormat)] string[] formats, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
        {
            return FromDateTime(DateTime.ParseExact(s.ToString(), formats, provider, style));
        }

#endif

#endif

        /// <summary>
        /// Converts a string that contains string representation of a date to its <see cref="DateOnly"/> equivalent by using the conventions of the current culture.
        /// </summary>
        /// <param name="s">The string that contains the string to parse.</param>
        /// <returns>An object that is equivalent to the date contained in s.</returns>
        public static DateOnly Parse(string s) => Parse(s, null, DateTimeStyles.None);

        /// <summary>
        /// Converts a string that contains string representation of a date to its <see cref="DateOnly"/> equivalent by using culture-specific format information and a formatting style.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">An object that supplies culture-specific format information about <paramref name="s"/>.</param>
        /// <param name="styles">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <returns>An object that is equivalent to the date contained in <paramref name="s"/>,
        /// as specified by <paramref name="provider"/> and <paramref name="styles"/>.</returns>
        /// <exception cref="FormatException"><paramref name="s"/> does not contain a valid string representation of a date.</exception>
        public static DateOnly Parse(string s, IFormatProvider? provider, DateTimeStyles styles = DateTimeStyles.None)
        {
            return FromDateTime(DateTime.Parse(s, provider, styles));
        }

        /// <summary>
        /// Converts the specified string representation of a date to its <see cref="DateOnly"/> equivalent using the specified format.
        /// The format of the string representation must match the specified format exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A string containing the characters that represent a date to convert.</param>
        /// <param name="format">A string that represent a format specifier that defines the required format of <paramref name="s"/>.</param>
        /// <returns>An object that is equivalent to the date contained in <paramref name="s"/>,
        /// as specified by <paramref name="format"/>.</returns>
        /// <exception cref="FormatException"><paramref name="s"/> does not contain a valid string representation of a date.</exception>
        public static DateOnly ParseExact(string s, [StringSyntax(StringSyntaxAttribute.DateOnlyFormat)] string format) => ParseExact(s, format, null, DateTimeStyles.None);

        /// <summary>
        /// Converts the specified string representation of a date to its <see cref="DateOnly"/> equivalent using the specified format, culture-specific format information, and style.
        /// The format of the string representation must match the specified format exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A string containing the characters that represent a date to convert.</param>
        /// <param name="format">A span containing the characters that represent a format specifier that defines the required format of <paramref name="s"/>.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <returns>An object that is equivalent to the date contained in <paramref name="s"/>,
        /// as specified by <paramref name="format"/>, <paramref name="provider"/>, and <paramref name="style"/>.</returns>
        /// <exception cref="FormatException"><paramref name="s"/> does not contain a valid string representation of a date.</exception>
        public static DateOnly ParseExact(string s, [StringSyntax(StringSyntaxAttribute.DateOnlyFormat)] string format, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
        {
            return FromDateTime(DateTime.ParseExact(s, format, provider, style));
        }

        /// <summary>
        /// Converts the specified span representation of a date to its <see cref="DateOnly"/> equivalent using the specified array of formats.
        /// The format of the string representation must match at least one of the specified formats exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A span containing the characters that represent a date to convert.</param>
        /// <param name="formats">An array of allowable formats of <paramref name="s"/>.</param>
        /// <returns>An object that is equivalent to the date contained in <paramref name="s"/>,
        /// as specified by one of <paramref name="formats"/>.</returns>
        /// <exception cref="FormatException"><paramref name="s"/> does not contain a valid string representation of a date.</exception>
        public static DateOnly ParseExact(string s, [StringSyntax(StringSyntaxAttribute.DateOnlyFormat)] string[] formats) => ParseExact(s, formats, null, DateTimeStyles.None);

        /// <summary>
        /// Converts the specified string representation of a date to its <see cref="DateOnly"/> equivalent using the specified array of formats, culture-specific format information, and style.
        /// The format of the string representation must match at least one of the specified formats exactly or an exception is thrown.
        /// </summary>
        /// <param name="s">A string containing the characters that represent a date to convert.</param>
        /// <param name="formats">An array of allowable formats of <paramref name="s"/>.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <returns>An object that is equivalent to the date contained in <paramref name="s"/>,
        /// as specified by <paramref name="provider"/>, <paramref name="style"/>, and one of <paramref name="formats"/>.</returns>
        /// <exception cref="FormatException"><paramref name="s"/> does not contain a valid string representation of a date.</exception>
        public static DateOnly ParseExact(string s, [StringSyntax(StringSyntaxAttribute.DateOnlyFormat)] string[] formats, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
        {
            return FromDateTime(DateTime.ParseExact(s, formats, provider, style));
        }

#if POLYKIT_USE_SPAN

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER

        /// <summary>
        /// Converts the specified span representation of a date to its <see cref="DateOnly"/> equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A span containing the characters representing the date to convert.</param>
        /// <param name="result">When this method returns, contains the <see cref="DateOnly"/> value equivalent to the date contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed.
        /// The conversion fails if the <paramref name="s"/> parameter is empty string, or does not contain a valid string representation of a date.
        /// This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, out DateOnly result)
        {
            var returnValue = DateTime.TryParse(s, out var dateTime);
            result = returnValue ? FromDateTime(dateTime) : MinValue;
            return returnValue;
        }

        /// <summary>
        /// Converts the specified span representation of a date to its <see cref="DateOnly"/> equivalent using the specified array of formats, culture-specific format information, and style. And returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A span containing the characters representing the date to convert.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="DateOnly"/> value equivalent to the date contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed.
        /// The conversion fails if the <paramref name="s"/> parameter is empty string, or does not contain a valid string representation of a date.
        /// This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
        {
            var returnValue = DateTime.TryParse(s, provider, style, out var dateTime);
            result = returnValue ? FromDateTime(dateTime) : MinValue;
            return returnValue;
        }

        /// <summary>
        /// Converts the specified span representation of a date to its <see cref="DateOnly"/> equivalent using the specified format and style.
        /// The format of the string representation must match the specified format exactly. The method returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A span containing the characters representing a date to convert.</param>
        /// <param name="format">The required format of <paramref name="s"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="DateOnly"/> value equivalent to the date contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed.
        /// The conversion fails if the <paramref name="s"/> parameter is empty string, or does not contain a valid string representation of a date.
        /// This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool TryParseExact(ReadOnlySpan<char> s, [StringSyntax(StringSyntaxAttribute.DateOnlyFormat)] ReadOnlySpan<char> format, out DateOnly result) => TryParseExact(s, format, null, DateTimeStyles.None, out result);

        /// <summary>
        /// Converts the specified span representation of a date to its <see cref="DateOnly"/> equivalent using the specified format, culture-specific format information, and style.
        /// The format of the string representation must match the specified format exactly. The method returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A span containing the characters representing a date to convert.</param>
        /// <param name="format">The required format of <paramref name="s"/>.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="DateOnly"/> value equivalent to the date contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed.
        /// The conversion fails if the <paramref name="s"/> parameter is empty string, or does not contain a valid string representation of a date.
        /// This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool TryParseExact(ReadOnlySpan<char> s, [StringSyntax(StringSyntaxAttribute.DateOnlyFormat)] ReadOnlySpan<char> format, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
        {
            var returnValue = DateTime.TryParseExact(s, format, provider, style, out var dateTime);
            result = returnValue ? FromDateTime(dateTime) : MinValue;
            return returnValue;
        }

        /// <summary>
        /// Converts the specified char span of a date to its <see cref="DateOnly"/> equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">The span containing the string to parse.</param>
        /// <param name="formats">An array of allowable formats of <paramref name="s"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="DateOnly"/> value equivalent to the date contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed.
        /// The conversion fails if the <paramref name="s"/> parameter is empty string, or does not contain a valid string representation of a date.
        /// This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool TryParseExact(ReadOnlySpan<char> s, [NotNullWhen(true), StringSyntax(StringSyntaxAttribute.DateOnlyFormat)] string?[]? formats, out DateOnly result) => TryParseExact(s, formats, null, DateTimeStyles.None, out result);

        /// <summary>
        /// Converts the specified char span of a date to its <see cref="DateOnly"/> equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">The span containing the string to parse.</param>
        /// <param name="formats">An array of allowable formats of <paramref name="s"/>.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="DateOnly"/> value equivalent to the date contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed.
        /// The conversion fails if the <paramref name="s"/> parameter is empty string, or does not contain a valid string representation of a date.
        /// This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool TryParseExact(ReadOnlySpan<char> s, [NotNullWhen(true), StringSyntax(StringSyntaxAttribute.DateOnlyFormat)] string?[]? formats, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
        {
            var returnValue = DateTime.TryParseExact(s, formats, provider, style, out var dateTime);
            result = returnValue ? FromDateTime(dateTime) : MinValue;
            return returnValue;
        }

#else

        /// <summary>
        /// Converts the specified span representation of a date to its <see cref="DateOnly"/> equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A span containing the characters representing the date to convert.</param>
        /// <param name="result">When this method returns, contains the <see cref="DateOnly"/> value equivalent to the date contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed.
        /// The conversion fails if the <paramref name="s"/> parameter is empty string, or does not contain a valid string representation of a date.
        /// This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, out DateOnly result)
        {
            var returnValue = DateTime.TryParse(s.ToString(), out var dateTime);
            result = returnValue ? FromDateTime(dateTime) : MinValue;
            return returnValue;
        }

        /// <summary>
        /// Converts the specified span representation of a date to its <see cref="DateOnly"/> equivalent using the specified array of formats, culture-specific format information, and style. And returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A span containing the characters representing the date to convert.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="DateOnly"/> value equivalent to the date contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed.
        /// The conversion fails if the <paramref name="s"/> parameter is empty string, or does not contain a valid string representation of a date.
        /// This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
        {
            var returnValue = DateTime.TryParse(s.ToString(), provider, style, out var dateTime);
            result = returnValue ? FromDateTime(dateTime) : MinValue;
            return returnValue;
        }

        /// <summary>
        /// Converts the specified span representation of a date to its <see cref="DateOnly"/> equivalent using the specified format and style.
        /// The format of the string representation must match the specified format exactly. The method returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A span containing the characters representing a date to convert.</param>
        /// <param name="format">The required format of <paramref name="s"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="DateOnly"/> value equivalent to the date contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed.
        /// The conversion fails if the <paramref name="s"/> parameter is empty string, or does not contain a valid string representation of a date.
        /// This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool TryParseExact(ReadOnlySpan<char> s, [StringSyntax(StringSyntaxAttribute.DateOnlyFormat)] ReadOnlySpan<char> format, out DateOnly result) => TryParseExact(s, format, null, DateTimeStyles.None, out result);

        /// <summary>
        /// Converts the specified span representation of a date to its <see cref="DateOnly"/> equivalent using the specified format, culture-specific format information, and style.
        /// The format of the string representation must match the specified format exactly. The method returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A span containing the characters representing a date to convert.</param>
        /// <param name="format">The required format of <paramref name="s"/>.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="DateOnly"/> value equivalent to the date contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed.
        /// The conversion fails if the <paramref name="s"/> parameter is empty string, or does not contain a valid string representation of a date.
        /// This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool TryParseExact(ReadOnlySpan<char> s, [StringSyntax(StringSyntaxAttribute.DateOnlyFormat)] ReadOnlySpan<char> format, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
        {
            var returnValue = DateTime.TryParseExact(s.ToString(), format.ToString(), provider, style, out var dateTime);
            result = returnValue ? FromDateTime(dateTime) : MinValue;
            return returnValue;
        }

        /// <summary>
        /// Converts the specified char span of a date to its <see cref="DateOnly"/> equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">The span containing the string to parse.</param>
        /// <param name="formats">An array of allowable formats of <paramref name="s"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="DateOnly"/> value equivalent to the date contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed.
        /// The conversion fails if the <paramref name="s"/> parameter is empty string, or does not contain a valid string representation of a date.
        /// This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool TryParseExact(ReadOnlySpan<char> s, [NotNullWhen(true), StringSyntax(StringSyntaxAttribute.DateOnlyFormat)] string?[]? formats, out DateOnly result) => TryParseExact(s, formats, null, DateTimeStyles.None, out result);

        /// <summary>
        /// Converts the specified char span of a date to its <see cref="DateOnly"/> equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">The span containing the string to parse.</param>
        /// <param name="formats">An array of allowable formats of <paramref name="s"/>.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="DateOnly"/> value equivalent to the date contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed.
        /// The conversion fails if the <paramref name="s"/> parameter is empty string, or does not contain a valid string representation of a date.
        /// This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool TryParseExact(ReadOnlySpan<char> s, [NotNullWhen(true), StringSyntax(StringSyntaxAttribute.DateOnlyFormat)] string?[]? formats, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
        {
            var returnValue = DateTime.TryParseExact(s.ToString(), formats, provider, style, out var dateTime);
            result = returnValue ? FromDateTime(dateTime) : MinValue;
            return returnValue;
        }

#endif

#endif

        /// <summary>
        /// Converts the specified string representation of a date to its <see cref="DateOnly"/> equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A string containing the characters representing the date to convert.</param>
        /// <param name="result">When this method returns, contains the <see cref="DateOnly"/> value equivalent to the date contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed. The conversion fails if the s parameter is empty string,
        /// or does not contain a valid string representation of a date. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, out DateOnly result) => TryParse(s, null, DateTimeStyles.None, out result);

        /// <summary>
        /// Converts the specified string representation of a date to its <see cref="DateOnly"/> equivalent using the specified array of formats, culture-specific format information, and style. And returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A string containing the characters that represent a date to convert.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="DateOnly"/> value equivalent to the date contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed. The conversion fails if the s parameter is empty string,
        /// or does not contain a valid string representation of a date. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
        {
            var returnValue = DateTime.TryParse(s, provider, style, out var dateTime);
            result = returnValue ? FromDateTime(dateTime) : MinValue;
            return returnValue;
        }

        /// <summary>
        /// Converts the specified string representation of a date to its <see cref="DateOnly"/> equivalent using the specified format and style.
        /// The format of the string representation must match the specified format exactly. The method returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A string containing the characters representing a date to convert.</param>
        /// <param name="format">The required format of <paramref name="s"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="DateOnly"/> value equivalent to the date contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed. The conversion fails if the s parameter is empty string,
        /// or does not contain a valid string representation of a date. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true), StringSyntax(StringSyntaxAttribute.DateOnlyFormat)] string? format, out DateOnly result) => TryParseExact(s, format, null, DateTimeStyles.None, out result);

        /// <summary>
        /// Converts the specified span representation of a date to its <see cref="DateOnly"/> equivalent using the specified format, culture-specific format information, and style.
        /// The format of the string representation must match the specified format exactly. The method returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A span containing the characters representing a date to convert.</param>
        /// <param name="format">The required format of <paramref name="s"/>.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="DateOnly"/> value equivalent to the date contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed. The conversion fails if the s parameter is empty string,
        /// or does not contain a valid string representation of a date. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true), StringSyntax(StringSyntaxAttribute.DateOnlyFormat)] string? format, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
        {
            var returnValue = DateTime.TryParseExact(s, format, provider, style, out var dateTime);
            result = returnValue ? FromDateTime(dateTime) : MinValue;
            return returnValue;
        }

        /// <summary>
        /// Converts the specified string of a date to its <see cref="DateOnly"/> equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">The string containing date to parse.</param>
        /// <param name="formats">An array of allowable formats of <paramref name="s"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="DateOnly"/> value equivalent to the date contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed. The conversion fails if the s parameter is empty string,
        /// or does not contain a valid string representation of a date. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true), StringSyntax(StringSyntaxAttribute.DateOnlyFormat)] string?[]? formats, out DateOnly result) => TryParseExact(s, formats, null, DateTimeStyles.None, out result);

        /// <summary>
        /// Converts the specified string of a date to its <see cref="DateOnly"/> equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">The string containing the date to parse.</param>
        /// <param name="formats">An array of allowable formats of <paramref name="s"/>.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s"/>.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of <paramref name="s"/>.
        /// A typical value to specify is <see cref="DateTimeStyles.None"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="DateOnly"/> value equivalent to the date contained in <paramref name="s"/>,
        /// if the conversion succeeded, or <see cref="MinValue"/> if the conversion failed. The conversion fails if the s parameter is empty string,
        /// or does not contain a valid string representation of a date. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the <paramref name="s"/> parameter was converted successfully;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true), StringSyntax(StringSyntaxAttribute.DateOnlyFormat)] string?[]? formats, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
        {
            var returnValue = DateTime.TryParseExact(s, formats, provider, style, out var dateTime);
            result = returnValue ? FromDateTime(dateTime) : MinValue;
            return returnValue;
        }

        /// <summary>
        /// Converts the value of the current <see cref="DateOnly"/> object to its equivalent long date string representation.
        /// </summary>
        /// <returns>A string that contains the long date string representation of the current <see cref="DateOnly"/> object.</returns>
        public string ToLongDateString() => ToString("D");

        /// <summary>
        /// Converts the value of the current <see cref="DateOnly"/> object to its equivalent short date string representation.
        /// </summary>
        /// <returns>A string that contains the short date string representation of the current <see cref="DateOnly"/> object.</returns>
        public string ToShortDateString() => ToString();

        /// <summary>
        /// Converts the value of the current <see cref="DateOnly"/> object to its equivalent string representation using the formatting conventions of the current culture.
        /// The <see cref="DateOnly"/> object will be formatted in short form.
        /// </summary>
        /// <returns>A string that contains the short date string representation of the current <see cref="DateOnly"/> object.</returns>
        public override string ToString() => ToString("d");

        /// <summary>
        /// Converts the value of the current <see cref="DateOnly"/> object to its equivalent string representation using the specified format and the formatting conventions of the current culture.
        /// </summary>
        /// <param name="format">A standard or custom date format string.</param>
        /// <returns>A string representation of value of the current <see cref="DateOnly"/> object as specified by <paramref name="format"/>.</returns>
        public string ToString([StringSyntax(StringSyntaxAttribute.DateOnlyFormat)] string? format)
        {
            if (string.IsNullOrEmpty(format))
            {
                format = "d";
            }

            EnsureValidCustomDateOnlyFormat(format);
            return GetEquivalentDateTime().ToString(format);
        }

        /// <summary>
        /// Converts the value of the current <see cref="DateOnly"/> object to its equivalent string representation using the specified culture-specific format information.
        /// </summary>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <returns>A string representation of value of the current <see cref="DateOnly"/> object as specified by provider.</returns>
        public string ToString(IFormatProvider? provider) => ToString("d", provider);

        /// <summary>
        /// Converts the value of the current <see cref="DateOnly"/> object to its equivalent string representation using the specified culture-specific format information.
        /// </summary>
        /// <param name="format">A standard or custom date format string.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <returns>A string representation of value of the current <see cref="DateOnly"/> object as specified by format and provider.</returns>
        public string ToString([StringSyntax(StringSyntaxAttribute.DateOnlyFormat)] string? format, IFormatProvider? provider)
        {
            if (string.IsNullOrEmpty(format))
            {
                format = "d";
            }

            EnsureValidCustomDateOnlyFormat(format);
            return GetEquivalentDateTime().ToString(format, provider);
        }

#if POLYKIT_USE_SPAN

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER

        /// <summary>
        /// Tries to format the value of the current <see cref="DateOnly"/> instance into the provided span of characters.
        /// </summary>
        /// <param name="destination">When this method returns, this instance's value formatted as a span of characters.</param>
        /// <param name="charsWritten">When this method returns, the number of characters that were written in <paramref name="destination"/>.</param>
        /// <param name="format">A span containing the characters that represent a standard or custom format string that defines the acceptable format for <paramref name="destination"/>.</param>
        /// <param name="provider">An optional object that supplies culture-specific formatting information for <paramref name="destination"/>.</param>
        /// <returns><see langword="true"/> if the formatting was successful; otherwise, <see langword="false"/>.</returns>
        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.DateOnlyFormat)] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
        {
            if (format.Length == 0)
            {
                format = "d";
            }

            _ = IsValidCustomDateOnlyFormat(format, true);
            return GetEquivalentDateTime().TryFormat(destination, out charsWritten, format, provider);
        }

#else

        /// <summary>
        /// Tries to format the value of the current <see cref="DateOnly"/> instance into the provided span of characters.
        /// </summary>
        /// <param name="destination">When this method returns, this instance's value formatted as a span of characters.</param>
        /// <param name="charsWritten">When this method returns, the number of characters that were written in <paramref name="destination"/>.</param>
        /// <param name="format">A span containing the characters that represent a standard or custom format string that defines the acceptable format for <paramref name="destination"/>.</param>
        /// <param name="provider">An optional object that supplies culture-specific formatting information for <paramref name="destination"/>.</param>
        /// <returns><see langword="true"/> if the formatting was successful; otherwise, <see langword="false"/>.</returns>
        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.DateOnlyFormat)] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
        {
            var formatStr = format.Length == 0 ? "d" : format.ToString();
            _ = IsValidCustomDateOnlyFormat(formatStr, true);
            var str = GetEquivalentDateTime().ToString(formatStr, provider);
            charsWritten = str.Length;
            return str.AsSpan().TryCopyTo(destination);
        }

#endif

#endif

        //
        // IParsable
        //

        /// <summary>
        /// Parses a string into a <see cref="DateOnly"/>.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s"/>.</param>
        /// <returns>The result of parsing <paramref name="s"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException"><paramref name="s"/> is not in the correct format.</exception>
        /// <exception cref="OverflowException"><paramref name="s"/> is not representable by <see cref="DateOnly"/>.</exception>
        public static DateOnly Parse(string s, IFormatProvider? provider) => Parse(s, provider, DateTimeStyles.None);

        /// <summary>
        /// Tries to parse a string into a <see cref="DateOnly"/>.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s"/>.</param>
        /// <param name="result">When this method returns, contains the result of successfully parsing <paramref name="s"/>
        /// or an undefined value on failure.</param>
        /// <returns><see langword="true"/> if s was successfully parsed; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out DateOnly result)
            => TryParse(s, provider, DateTimeStyles.None, out result);

#if POLYKIT_USE_SPAN

        //
        // ISpanParsable
        //

        /// <summary>
        /// Parses a span of characters into a <see cref="DateOnly"/>.
        /// </summary>
        /// <param name="s">The span of characters to parse.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s"/>.</param>
        /// <returns>The result of parsing <paramref name="s"/>.</returns>
        /// <exception cref="FormatException"><paramref name="s"/> is not in the correct format.</exception>
        /// <exception cref="OverflowException"><paramref name="s"/> is not representable by <see cref="DateOnly"/>.</exception>
        public static DateOnly Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, provider, DateTimeStyles.None);

        /// <summary>
        /// Tries to parse a span of characters into a <see cref="DateOnly"/>.
        /// </summary>
        /// <param name="s">The span of characters to parse.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s"/>.</param>
        /// <param name="result">When this method returns, contains the result of successfully parsing <paramref name="s"/>
        /// or an undefined value on failure.</param>
        /// <returns><see langword="true"/> if s was successfully parsed; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out DateOnly result) => TryParse(s, provider, DateTimeStyles.None, out result);

#endif

// The following is an internal method of class System.Globalization.DateTimeFormat
// https://github.com/dotnet/runtime/blob/v7.0.0/src/libraries/System.Private.CoreLib/src/System/Globalization/DateTimeFormat.cs#L1073
// https://github.com/dotnet/runtime/blob/v8.0.0/src/libraries/System.Private.CoreLib/src/System/Globalization/DateTimeFormat.cs#L1176
// The string version is just a copy+paste of the same method with the parameter type changed, for platforms with no Span support
#if POLYKIT_USE_SPAN
        internal static bool IsValidCustomDateOnlyFormat(ReadOnlySpan<char> format, bool throwOnError)
        {
            var i = 0;
            while (i < format.Length)
            {
                switch (format[i])
                {
                    case '\\':
                        if (i == format.Length - 1)
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
                        var quoteChar = format[i++];
                        while (i < format.Length && format[i] != quoteChar)
                        {
                            i++;
                        }

                        if (i >= format.Length)
                        {
                            if (throwOnError)
                            {
                                throw new FormatException($"Cannot find a matching quote character for the character '{quoteChar}'.");
                            }

                            return false;
                        }

                        i++;
                        break;

                    case ':':
                    case 't':
                    case 'f':
                    case 'F':
                    case 'h':
                    case 'H':
                    case 'm':
                    case 's':
                    case 'z':
                    case 'K':
                        // reject non-date formats
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

        private static bool IsValidCustomDateOnlyFormat(string format, bool throwOnError)
            => IsValidCustomDateOnlyFormat(format.AsSpan(), throwOnError);

#else

        private static bool IsValidCustomDateOnlyFormat(string format, bool throwOnError)
        {
            var i = 0;
            while (i < format.Length)
            {
                switch (format[i])
                {
                    case '\\':
                        if (i == format.Length - 1)
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
                        var quoteChar = format[i++];
                        while (i < format.Length && format[i] != quoteChar)
                        {
                            i++;
                        }

                        if (i >= format.Length)
                        {
                            if (throwOnError)
                            {
                                throw new FormatException($"Cannot find a matching quote character for the character '{quoteChar}'.");
                            }

                            return false;
                        }

                        i++;
                        break;

                    case ':':
                    case 't':
                    case 'f':
                    case 'F':
                    case 'h':
                    case 'H':
                    case 'm':
                    case 's':
                    case 'z':
                    case 'K':
                        // reject non-date formats
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

        private static void EnsureValidCustomDateOnlyFormat([ValidatedNotNull]string? format)
        {
            if (format == null) ThrowArgumentNullException(nameof(format));
            _ = IsValidCustomDateOnlyFormat(format, true);
        }
    }
}

#endif
