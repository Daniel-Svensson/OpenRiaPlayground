//
// QueryStringConverter.cs
//
// Author:
//	Atsushi Enomoto  <atsushi@ximian.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Globalization;
using OpenRiaServices;

namespace Mono.System.ServiceModel.Dispatcher
{
    /// <summary>
    /// Mono's query string converter, with minor changes
    /// </summary>
	static class QueryStringConverter
	{
		// "Service Operation Parameters and URLs"
		// http://msdn2.microsoft.com/en-us/library/bb412172.aspx
        public static bool CanConvert(Type type)
		{
#if PORTABLE 
            switch (Type.GetTypeCode (type))
#else
            switch (Convert.GetTypeCode(type))
#endif
            {
			case TypeCode.Empty:
            case (TypeCode)2: // DBNull
				return false;
			case TypeCode.Object:
				if (type == typeof (TimeSpan))
					return true;
				if (type == typeof (DateTimeOffset))
					return true;
				if (type == typeof (Guid))
					return true;
				if (type == typeof (object))
					return true;
//				if (type.GetCustomAttributes (typeof (TypeConverterAttribute), true).Length > 0)
//					return true;
				return false;
			default:
				return true;
			}
		}

        public static string ConvertValueToString(object parameter, Type parameterType)
		{
			if (parameterType == null)
				throw new ArgumentNullException ("parameterType");
			if (parameterType.GetTypeInfo().IsValueType && parameter == null)
				throw new ArgumentNullException ("parameter");

			if (parameter == null)
				return null;

			if (parameter.GetType () != parameterType)
				throw new InvalidCastException (String.Format ("This QueryStringConverter does not support cast from {0} to {1}", parameter.GetType (), parameterType));

			if (!CanConvert (parameterType))
				throw new NotSupportedException (String.Format ("Conversion from the argument parameterType '{0}' is not supported", parameterType));

			if (parameter is IFormattable)
				((IFormattable) parameter).ToString (null, CultureInfo.InvariantCulture);
			return parameter.ToString ();
		}
	}
}