using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenGraal.Common.Scripting
{
	public static class ExtensionMethods
	{
		/// <summary>
		/// Extension Method -> ToInt
		/// </summary>
		public static int ToInt(this String str)
		{
			try
			{
				return Convert.ToInt32(str);
			}
			catch (System.FormatException)
			{
				return 0;
			}
		}

		/// <summary>
		/// Extension Method -> ToInt
		/// </summary>
		public static double ToDouble(this String str)
		{
			try
			{
				return Convert.ToDouble(str);
			}
			catch (System.FormatException)
			{
				return 0;
			}
		}

		/// <summary>
		/// Extension Method -> Case-insensitive compare
		/// </summary>
		public static bool comparei(this String str, String str2)
		{
			return string.Equals(str, str2, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Extension Method -> Case-insensitive compare
		/// </summary>
		public static bool startswith(this String str, String str2)
		{
			return str.StartsWith(str2, StringComparison.OrdinalIgnoreCase);
		}
	}
}
