// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using Dacs7.Alarms;
using Dacs7.Domain;
using System;

namespace Dacs7
{
    internal static class ExceptionThrowHelper
    {
        public static void ThrowNotConnectedException(Exception ex = null) => throw new Dacs7NotConnectedException(ex);

        public static void ThrowCouldNotChangeValueWhileConnectionIsOpen(string variable) => throw new InvalidOperationException($"Value of {variable} can only be changed while connection is closed!");

        public static void ThrowInvalidCastException() => throw new InvalidCastException();

        public static void ThrowInvalidAreaException(string area) => throw new ArgumentException($"Invalid area <{area}>");

        public static void ThrowException(Exception ex) => throw ex;

        public static void ThrowTypesNotMatching(Type expected, Type resultType) => throw new InvalidOperationException($"Generic type <{expected}> is not Equal to Type <{resultType}>");

        public static void ThrowWriteTimeoutException(ushort id) => throw new Dacs7WriteTimeoutException(id);

        public static void ThrowReadTimeoutException(ushort id) => throw new Dacs7ReadTimeoutException(id);

        public static void ThrowTimeoutException() => throw new TimeoutException();

        public static void ThrowCouldNotAddPackageException(string type) => throw new InvalidOperationException($"Could not add {type}.");

        public static void ThrowStringToLongException(string arg) => throw new ArgumentOutOfRangeException(arg, $"The given string is to long!");

        public static void ThrowTypeNotSupportedException(Type type) => throw new Dacs7TypeNotSupportedException(type);

        public static void ThrowInvalidWriteResult(WriteItem result) => throw new ArgumentOutOfRangeException($"Given number of elements {result.NumberOfItems} and given number of values {result.Data.Length / result.ElementSize} are not matching!");

        public static void ThrowUnknownAlarmSubfunction(AlarmMessageType subfunction) => throw new Exception($"Unknown alarm subfunction {subfunction}");

        public static void ThrowUnknownAlarmSyntax(byte syntaxId) => throw new Exception($"Unknown alarm syntaxID {syntaxId}");
        internal static void ThrowTagParseException(TagParserState area, string v, string tag) => throw new Dacs7TagParserException(area, v, tag);
    }
}
