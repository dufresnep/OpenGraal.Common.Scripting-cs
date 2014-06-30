using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenGraal.Common.Scripting
{
	public class ScriptEvent
	{
		/// <summary>
		/// Member Variables
		/// </summary>
		public double FunctionTimer = 0.0;
		public object[] FunctionArgs;
		public string FunctionName;

		/// <summary>
		/// Constructor - Create Event
		/// </summary>
		public ScriptEvent(double Timeout, String Name, Object[] Args)
		{
			this.FunctionTimer = Timeout;
			this.FunctionName = Name;
			this.FunctionArgs = Args;
		}
	};
}
