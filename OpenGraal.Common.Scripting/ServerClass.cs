using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenGraal;
using OpenGraal.Core;
using OpenGraal.Common;

namespace OpenGraal.Common.Scripting
{
	public class ServerClass : IRefObject
	{
		/// <summary>
		/// Member Variables
		/// </summary>
		public String Name;

		/// <summary>
		/// Constructor
		/// </summary>
		public ServerClass(String ClassName, String ClassScript) : base(ScriptType.CLASS)
		{
			this.UpdateClass(ClassName, ClassScript);
		}

		/// <summary>
		/// Update Weapon Properties
		/// </summary>
		public void UpdateClass(String ClassName, String ClassScript)
		{
			this.Name = ClassName;
			this.Script = ClassScript;
		}

		/// <summary>
		/// Override -> Error Text
		/// </summary>
		public override string GetErrorText()
		{
			return new StringBuilder(Name).ToString();
		}
	}
}
