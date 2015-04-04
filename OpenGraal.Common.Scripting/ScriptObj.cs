using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using OpenGraal.Common.Players; // Not in Common according to compiler
using OpenGraal.Core;
using System.Reflection;

namespace OpenGraal.Common.Scripting
{
	/// <summary>
	/// ScriptObject Class
	/// </summary>
	public class ScriptObj
	{
		// -- Member Variables -- //
		static public Random rand = new Random();
		public List<ScriptEvent> ScriptEvents = new List<ScriptEvent>();
		private  CSocket _server;
		private Dictionary<String, ServerWeapon> _weaponList;

		public Dictionary<String, ServerWeapon> WeaponList
		{
			get
			{
				return _weaponList;
			}
			set
			{
				_weaponList = value;
			}
		}

		public CSocket Server
		{
			get { return _server; }
			set { _server = value; }
		}

		public double timeout = 0.0;
		private int _nwTime = 0;

		public int NWTime
		{
			get
			{
				return _nwTime;
			}
			set
			{
				_nwTime = value;
			}
		}
		/*
		private GraalPlayerList _playerManager = null;

		public GraalPlayerList PlayerManager
		{
			get
			{
				return _playerManager;
			}
			set
			{
				_playerManager = value;
			}
		}
		*/
		/// <summary>
		/// Constructor
		/// </summary>
		public ScriptObj()
		{
		}

		public ScriptObj(CSocket Server)
		{
			this.Server = Server;
		}

		public ScriptObj(object PL, int NWTime)
		{
			//this.PlayerManager = PL;



			this.NWTime = NWTime;
		}

		/// <summary>
		/// Function -> Call Event for Object
		/// </summary>
		public void Call(String Event, Object[] Args)
		{
			Type type = this.GetType();
			try
			{
				MethodInfo m = type.GetMethod(Event);
				if (m != null)
					type.InvokeMember(Event, BindingFlags.InvokeMethod, null, this, Args);
			}
			catch (Exception)
			{
			}
		}

		/// <summary>
		/// Function -> Run Scheduled Events
		/// </summary>
		public void RunEvents()
		{
		
			// Run Timeout
			if (this.timeout > 0.0)
			{
				this.timeout -= 0.05;
				if (this.timeout <= 0.0)
					this.Call("onTimeout", null);
				
			}

			// Run Scheduled Events
			for (int i = ScriptEvents.Count - 1; i >= 0; i--)
			{
				ScriptEvent e = ScriptEvents[i];
				if (e.FunctionTimer > 0.0)
				{
					e.FunctionTimer -= 0.05;
					if (e.FunctionTimer <= 0.0)
					{
						Call(e.FunctionName, e.FunctionArgs);
						Console.WriteLine(e.FunctionName + " " + e.FunctionArgs);
						ScriptEvents.RemoveAt(i);
					}
				}
			}
		}

		/// <summary>
		/// Library Function -> FindWeapon by Name
		/// </summary>
		public dynamic FindWeapon(string Name)
		{
			ServerWeapon wep;
			if (this.WeaponList.TryGetValue(Name, out wep))
				return (dynamic)wep.ScriptObject;
			return null;
		}

		/// <summary>
		/// Library Function -> Find Player by Account
		/// </summary>
		/*
		public virtual GraalPlayer FindPlayer(string Account)
		{
			throw new NotImplementedException("requires override!");
			//return this.PlayerManager.FindPlayer (Account);
		}
		*/
		/// <summary>
		/// Library Function -> Find Player by Id
		/// </summary>
		
		/*
		public GraalPlayer FindPlayer(short Id)
		{
			return this.PlayerManager.FindPlayer(Id);
		}
		*/
		/// <summary>
		/// Library Function -> MD5 String
		/// </summary>
		public String MD5(String Input)
		{
			System.Security.Cryptography.MD5CryptoServiceProvider x = new System.Security.Cryptography.MD5CryptoServiceProvider();
			byte[] bs = x.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Input));
			string ret = String.Empty;
			foreach (byte b in bs)
				ret += b.ToString("x2").ToLower();
			return ret;
		}

		/// <summary>
		/// Random (Integer)
		/// </summary>
		public double random(int min, int max)
		{
			return min + rand.NextDouble() * (max - min);
		}

		/// <summary>
		/// Library Function -> Schedule Event
		/// </summary>
		public void ScheduleEvent(double Timer, String Name, params object[] Args)
		{
			bool isReplaced = false;
			foreach (ScriptEvent e in ScriptEvents)
			{
				if (e.FunctionName == Name)
				{
					e.FunctionTimer = Timer;
					isReplaced = true;
				}
			}

			if (!isReplaced)
				ScriptEvents.Add(new ScriptEvent(Timer, Name, Args));
		}

		/// <summary>
		/// Library Function -> Trigger Event
		/// </summary>
		public void trigger(string Event, params object[] Args)
		{
			this.Call(Event, Args);
		}

		/// <summary>
		/// Library Function -> Trigger Event
		/// </summary>
		public void trigger(IPlayer player, string Event, string[] args)
		{
			this.Call(Event, new object[] { player, args });
		}

		/// <summary>
		/// Library Function -> SetTimer
		/// </summary>
		public void SetTimer(double Timer)
		{
			this.timeout = Timer;
		}

		/// <summary>
		/// Library Function -> Send Text to NPC-Controls
		/// </summary>
		public void SendToNC(String Message)
		{
			//Server.SendNCChat(Message, null);
			this.SendToRC(Message);
		}

		/// <summary>
		/// Library Function -> Send Text to Remote Controls
		/// </summary>
		public void SendToRC(String Message)
		{
			//Server.SendRCChat(Message);
			this.SendRCChat(Message);

		}

		/// <summary>
		/// Send Text to NPC-Controls
		/// </summary>
		public void SendRCChat(String Message)
		{
			this.Server.SendPacket(new CString() + (byte)79 + Message);
		}

		/// <summary>
		/// Retrieve Players-Array
		/// </summary>
		/*
		public GraalPlayerList allplayers
		{
			get { return this.PlayerManager; }
		}
		*/
		/// <summary>
		/// New World Time
		/// </summary>
		public int timevar
		{
			get { return this.NWTime; }
		}

		/// <summary>
		/// Computer Time
		/// </summary>
		public double timevar2
		{
			get { return (double)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds; }
		}
	};
}
