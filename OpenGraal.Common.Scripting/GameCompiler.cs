using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CSharp;
using OpenGraal;
using OpenGraal.Core;


using System.IO;

namespace OpenGraal.Common.Scripting
{
	public class GameCompiler
	{
		/// <summary>
		/// Compile Thread
		/// </summary>
		public void CompileThread()
		{
			while (CompileList.Count > 0)
			{
				// Grab from Queue
				IRefObject cur;
				lock (CompileList)
					cur = CompileList.Dequeue();

				// Compile
				this.Compile(cur);

				// Add to Run List
				lock (RunList)
					RunList.Enqueue(cur);
			}
		}

		/// <summary>
		/// Member Variables
		/// </summary>
		protected List<CSocket> _sockets;
		protected Int32[] NextId = new Int32[] { 0, 0, 0 };
		protected Int32 ActiveCompilers = 0;
		protected Thread[] Compilers;
		protected Queue<IRefObject> CompileList = new Queue<IRefObject>();
		//protected IFramework Server;
		public Queue<IRefObject> RunList = new Queue<IRefObject>();

		/// <summary>
		/// Constructor -> Create Compiler, pass NPCServer reference
		/// </summary>
		public GameCompiler()
		{
			//this.Server = Server;

			// Two compilers / processor
			Compilers = new Thread[Environment.ProcessorCount * 4];
		}

		/// <summary>
		/// Manage active compilers
		/// </summary>
		public void ManageCompilers()
		{
			// No scripts to compile, or all compilers are running
			if (CompileList.Count == ActiveCompilers || (CompileList.Count > ActiveCompilers && ActiveCompilers == Compilers.Length))
				return;
			
			// Iterate compilers
			for (int i = 0; i < Compilers.Length; i++)
			{
				// Remove Compilers
				if (Compilers[i] != null)
				{
					if (!Compilers[i].IsAlive)
					{
						ActiveCompilers--;
						Compilers[i] = null;
					}
				}

				// No need to create another compiler, continue
				if (CompileList.Count > i && Compilers[i] == null)
				{
					Compilers[i] = new Thread(CompileThread);
					Compilers[i].IsBackground = true;
					Compilers[i].Start();
					ActiveCompilers++;
				}
			}
		}

		/// <summary>
		/// Add to Compile List
		/// </summary>
		public void CompileAdd(IRefObject ScriptObj)
		{
			lock (CompileList)
				CompileList.Enqueue(ScriptObj);
		}

		/// <summary>
		/// Compile & Execute -> Script
		/// </summary>
		/// <returns></returns>
		public void Compile(IRefObject ScriptObj)
		{
			CompilerResults Results;
			String[] Script = ParseJoins(ScriptObj.Script);

			//Serverside script
			Assembly Asm = CompileScript(ScriptObj, out Results, Script[0]);

			if (Asm != null)
			{
				if (ScriptObj.Type != IRefObject.ScriptType.CLASS)
					ScriptObj.ScriptObject = this.RunScript(ScriptObj, Asm);
				else
				{
					ScriptObj.Asm = Results.PathToAssembly;
					Results = null;
					Asm = null;
				}
			}
			else
				HandleErrors((ScriptObj.Type == IRefObject.ScriptType.WEAPON ? "weapon" : "levelnpc_") + ScriptObj.GetErrorText(), Results);
			/*
			//Clientside script
			Assembly Asm2 = CompileScript(ScriptObj, out Results, Script[1], true);
			if (Asm2 == null)
				HandleErrors((ScriptObj.Type == IRefObject.ScriptType.WEAPON ? "weapon" : "levelnpc_") + ScriptObj.GetErrorText(), Results, true);
			Asm2 = null;
			*/ 
			
		}

		/// <summary>
		/// Compile Script -> Return Assembly
		/// </summary>
		public Assembly CompileScript(IRefObject ScriptObject, out CompilerResults Result, String script, bool ClientSide = false)
		{
			String ClassName, PrefixName, AssemblyName = "";
			
			switch (ScriptObject.Type)
			{
				case IRefObject.ScriptType.WEAPON:
					ClassName = "ScriptWeapon";
					PrefixName = "WeaponPrefix";
					ServerWeapon tempWeap = (ServerWeapon)ScriptObject;
					AssemblyName = tempWeap.Name.Replace("-", "weapon").Replace("*", "weapon");
					break;
				case IRefObject.ScriptType.CLASS:
					PrefixName = "NpcClass";
					ServerClass tempClass = (ServerClass)ScriptObject;
					ClassName = tempClass.Name;
					AssemblyName = "NpcClass." + tempClass.Name + ".dll";
					break;
				default:
					ClassName = "ScriptLevelNpc";
					PrefixName = "LevelNpcPrefix";
					//ScriptLevelNpc tempNpc = (ScriptLevelNpc)ScriptObject;
					AssemblyName = "";
					break;
			}

			// Setup our options
			CompilerParameters options = new CompilerParameters();
			options.GenerateExecutable = false;

			if (ClientSide || ScriptObject.Type == IRefObject.ScriptType.CLASS)
				options.GenerateInMemory = false;
			else
				options.GenerateInMemory = true;

			if (!ClientSide)
				options.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);
			options.ReferencedAssemblies.Add("System.dll");
			options.ReferencedAssemblies.Add("System.Core.dll");
			options.ReferencedAssemblies.Add("OpenGraal.Core.dll");
			options.ReferencedAssemblies.Add("OpenGraal.Common.dll");
			options.ReferencedAssemblies.Add("Microsoft.CSharp.dll");
			options.ReferencedAssemblies.Add("MonoGame.Framework.dll");
			if (this.GetClasses() != null)
			{
				foreach (KeyValuePair<string, ServerClass> npcClass in this.GetClasses())
				{
					if (npcClass.Value.Asm != null)
						options.ReferencedAssemblies.Add(npcClass.Value.Asm);
				}
			}

			//options.ReferencedAssemblies.
			options.CompilerOptions = "/optimize";

			if (AssemblyName == "")
				AssemblyName = PrefixName + NextId[(int)ScriptObject.Type];
			
			if (ClientSide)
				options.OutputAssembly = AssemblyName + "_ClientSide.dll";

			if (ScriptObject.Type == IRefObject.ScriptType.CLASS)
				options.OutputAssembly = AssemblyName;
			
			// Compile our code
			CSharpCodeProvider csProvider = new Microsoft.CSharp.CSharpCodeProvider();
			
			string usingNamespace = "";
			usingNamespace = "using Microsoft.Xna.Framework.Input;";
			string[] CompileData = new string[1];
			if (ScriptObject.Type != IRefObject.ScriptType.CLASS)
				CompileData.SetValue("using System;" + usingNamespace + "using OpenGraal; using OpenGraal.Core; using OpenGraal.Common.Levels; using OpenGraal.Common.Players; using OpenGraal.Common.Scripting; " + (this.GetClasses() != null && this.GetClasses().Count > 0 ? "using OpenGraal.Common.Scripting.NpcClass;" : "") + " public class " + PrefixName + NextId[(int)ScriptObject.Type] + " : " + ClassName + " { public " + PrefixName + NextId[(int)ScriptObject.Type] + "(CSocket Server, IRefObject Ref) : base(Server, Ref) { } " + script + " } ", 0);
			else
				CompileData.SetValue("using System;" + usingNamespace + "using OpenGraal; using OpenGraal.Core; using OpenGraal.Common.Levels; using OpenGraal.Common.Players; using OpenGraal.Common.Scripting; namespace OpenGraal.Common.Scripting.NpcClass { public class " + ClassName + " { " + script + "\n } }", 0);

			Result = null;
			try
			{
				Result = csProvider.CompileAssemblyFromSource(options, CompileData);
			}
			catch (Exception e)
			{
				return null;
			}
			csProvider.Dispose();

			NextId[(int)ScriptObject.Type]++;
			return (Result.Errors.HasErrors ? null : Result.CompiledAssembly);
		}

		/// <summary>
		/// Send Errors to NC Chat
		/// </summary>
		public void HandleErrors(String Name, CompilerResults Results, bool ClientSide = false)
		{
			if (Results != null)
			{
				if (Results.Errors.HasErrors)
				{
					if (ClientSide)
						this.OutputError("//#CLIENTSIDE:");
					this.OutputError("Script compiler output for " + Name + ":");
					foreach (CompilerError CompErr in Results.Errors)
					{
						this.OutputError("error: " + CompErr.ErrorText + " at line " + CompErr.Line);
					}
				}
			}
			else
			{
				this.OutputError("error: Result is null.");
			}
		}

		public virtual void OutputError(string errorText)
		{
			Console.WriteLine(errorText);
		}

		/// <summary>
		/// Parse Joins and return new script
		/// </summary>
		public String[] ParseJoins(String Script)
		{
			MatchCollection col = Regex.Matches(Script, "join\\(\"(?<class>[A-Za-z0-9]*)\"\\);", RegexOptions.IgnoreCase);
			String NewScript = Regex.Replace(Script, "join\\(\"(?<class>[A-Za-z0-9]*)\"\\);", "", RegexOptions.IgnoreCase);
			String Serverside, Clientside;

			foreach (Match x in col)
			{
				ServerClass Class = this.FindClass(x.Groups["class"].Value);
				if (Class != null)
					NewScript += "\n" + Class.Script;
			}

			int pos = NewScript.IndexOf("//#CLIENTSIDE");
			if (pos >= 0)
			{
				Serverside = NewScript.Substring(0, pos);
				Clientside = NewScript.Substring(pos + 13);
			}
			else
			{
				Serverside = NewScript;
				Clientside = "";
			}

			//Console.WriteLine(NewScript);
			NewScript = Regex.Replace(NewScript, "function\\s*([a-z0-9]+)\\s*\\((.*)\\)(\t|\r|\\s)*\\{(.*)\\}", delegate(Match match)
			{
				string v = match.ToString();
				return char.ToUpper(v[0]) + v.Substring(1);//"public void $1 ($2)\n{\n}\n"
			}, RegexOptions.IgnoreCase);

			//Console.WriteLine("after regexp: " + NewScript);
			String[] scripts = new String[2];
			scripts.SetValue(Serverside, 0);
			scripts.SetValue(Clientside, 1);
			return scripts;
		}

		public virtual ServerClass FindClass(string Name)
		{
			//return this.Server.FindClass(Name);
			throw new NotImplementedException();
		}

		public virtual Dictionary<string,ServerClass> GetClasses()
		{
			//throw new NotImplementedException();
			return null;
		}

		/// <summary>
		/// Run Script -> Return Object
		/// </summary>
		public ScriptObj RunScript(IRefObject Reference, Assembly Script)
		{
			Type[] types = Script.GetTypes();
			foreach (Type type in types)
			{
				if (!type.IsSubclassOf(typeof(ScriptObj)))
				{
					//Console.WriteLine("Is not scriptobj");
					continue;
				} //else
				//Console.WriteLine("Is scriptobj");

				ConstructorInfo constructor = type.GetConstructor(new Type[] { typeof(CSocket), typeof(IRefObject) });
				if (constructor != null && constructor.IsPublic)
				{

					ScriptObj obj = InvokeConstruct(Reference, constructor); //GSConn
					obj.RunEvents();
					//Console.WriteLine("Script Constructed");
					return obj;
				}
				else
					Console.WriteLine("error3");
			}

			//Console.WriteLine("No object created, return null");

			// No object created, return null
			return null;
		}

		public virtual ScriptObj InvokeConstruct(IRefObject Reference, ConstructorInfo constructor)
		{
			ScriptObj obj = (ScriptObj)constructor.Invoke(new object[] { null, Reference });
			return obj;
		}
	}

	public class CompilerRunner : MarshalByRefObject
	{
		private Assembly assembly = null;

		public void PrintDomain()
		{
			Console.WriteLine("Object is executing in AppDomain \"{0}\"", AppDomain.CurrentDomain.FriendlyName);
		}

		public bool Compile(string code)
		{
			CSharpCodeProvider codeProvider = new CSharpCodeProvider();
			CompilerParameters parameters = new CompilerParameters();
			parameters.GenerateInMemory = true;
			parameters.GenerateExecutable = false;
			parameters.ReferencedAssemblies.Add("system.dll");

			CompilerResults results = codeProvider.CompileAssemblyFromSource(parameters, code);
			if (!results.Errors.HasErrors)
			{
				this.assembly = results.CompiledAssembly;
			}
			else
			{
				this.assembly = null;
			}

			return this.assembly != null;
		}

		public object Run(string typeName, string methodName, object[] args)
		{
			Type type = this.assembly.GetType(typeName);
			return type.InvokeMember(methodName, BindingFlags.InvokeMethod, null, assembly, args);
		}
	}
}
