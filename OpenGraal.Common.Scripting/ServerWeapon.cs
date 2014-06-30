using System;
using System.Text;
using OpenGraal;
using OpenGraal.Core;
using OpenGraal.Common;

namespace OpenGraal.Common.Scripting
{
	/// <summary>
	/// Class: ServerWeapon Reference
	/// </summary>
	public class ServerWeapon : IRefObject
	{
		/// <summary>
		/// Member Variables
		/// </summary>
		internal CSocket Server;
		public String Image, Name;

		/// <summary>
		/// Constructor
		/// </summary>
		public ServerWeapon(CSocket Server, String WeaponName, String WeaponImage, String WeaponScript)
			: base(ScriptType.WEAPON)
		{
			this.Server = Server;
			this.UpdateWeapon(WeaponName, WeaponImage, WeaponScript);
		}

		/// <summary>
		/// Update Weapon Properties
		/// </summary>
		public void UpdateWeapon(String WeaponName, String WeaponImage, String WeaponScript)
		{
			this.Name = WeaponName;
			this.Image = WeaponImage;
			this.Script = WeaponScript.Replace("\xa7", "\n");
			this.Script = this.Script.Replace("ï¿½", "\n");
			this.Script = this.Script.Replace("Â", "");
			this.Script = this.Script.Replace("�", "\n");
		}

		/// <summary>
		/// Override -> Error Text
		/// </summary>
		public override string GetErrorText()
		{
			return new StringBuilder(Name).ToString();
		}
	}

	/// <summary>
	/// Class: ScriptWeapon Object
	/// </summary>
	public class ScriptWeapon : ScriptObj
	{
		// -- Member Variables -- //
		public readonly ServerWeapon Ref;
		public readonly bool isweapon = true;

		/// <summary>
		/// Name -> Read Only
		/// </summary>
		public string name
		{
			get { return Ref.Name; }
		}

		/// <summary>
		/// Image -> Read Only
		/// </summary>
		public string image
		{
			get { return Ref.Image; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public ScriptWeapon()
		{
		}

		public ScriptWeapon(IRefObject Ref)
		{
			this.Ref = (ServerWeapon)Ref;
		}

		public ScriptWeapon(CSocket socket, IRefObject Ref) : base(socket)
		{
			this.Ref = (ServerWeapon)Ref;
		}
	};
}
