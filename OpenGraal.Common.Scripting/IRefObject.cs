using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace OpenGraal.Common.Scripting
{
	/// <summary>
	/// Reference Interface
	/// </summary>
	public abstract class IRefObject
	{
		/// <summary>
		/// Script Type
		/// </summary>
		public enum ScriptType
		{
			WEAPON = 0,
			LEVELNPC = 1,
			CLASS = 2,
		}

		/// <summary>
		/// Member Variables
		/// </summary>
		public ScriptObj ScriptObject;
		public String Asm;
		public ScriptType Type;
		public String Script;

		/// <summary>
		/// Constructor
		/// </summary>
		public IRefObject(ScriptType Type)
		{
			this.Type = Type;
		}

		/// <summary>
		/// Call Function
		/// </summary>
		public void Call(string Event, object[] Args)
		{
			if (ScriptObject == null)
				return;

			try
			{
				ScriptObject.Call(Event, Args);
			}
			catch (TargetInvocationException e)
			{
				ScriptObject.SendToNC("Script runtime error occurred:\rerror: " + e.InnerException.Message);
			}
		}

		/// <summary>
		/// Get Error Text
		/// </summary>
		/// <returns></returns>
		public abstract string GetErrorText();
	}
}
