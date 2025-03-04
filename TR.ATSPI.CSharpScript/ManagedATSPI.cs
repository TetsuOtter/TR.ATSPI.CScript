﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TR.ATSPI.CSharpScript
{
	public sealed class ManagedATSPI : IDisposable, ISettings<Func<GlobalVariable, Task>>
	{
		static string CurrentDllDirectoryPath { get; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
		static string CurrentDllFileNameWithoutExtension { get; } = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location) ?? string.Empty;

		static string[] ScriptsImports { get; } = new string[]
		{
			"System",
			"System.IO",
			"System.Collections.Generic",
			"System.Threading.Tasks"
		};
		static Assembly[] ScriptReferences { get; } = new Assembly[]
		{
			typeof(object).Assembly,
			typeof(System.IO.File).Assembly,
			typeof(System.Collections.Generic.Dictionary<object, object>).Assembly,
			typeof(System.Threading.Tasks.Task).Assembly,
			typeof(System.Diagnostics.Debugger).Assembly
		};

		static ScriptOptions UsingScriptOptions { get; }
			= ScriptOptions.Default
				.WithAllowUnsafe(true)
				.WithFileEncoding(System.Text.Encoding.UTF8)
				.WithImports(ScriptsImports)
				.WithReferences(ScriptReferences)
				.WithOptimizationLevel(OptimizationLevel.Release)
				.WithEmitDebugInformation(false)
				.WithSourceResolver(ScriptSourceResolver.Default.WithBaseDirectory(CurrentDllDirectoryPath))
				.WithMetadataResolver(ScriptMetadataResolver.Default.WithBaseDirectory(CurrentDllDirectoryPath));

		static XmlSerializer Serializer { get; } = new XmlSerializer(typeof(ScriptPathListClass));

		public List<ScriptPathListClass> ScriptFileLists { get; } = new();

		#region ScriptActionsList
		public List<Func<GlobalVariable, Task>> LoadScripts { get; } = new();
		public List<Func<GlobalVariable, Task>> DisposeScripts { get; } = new();
		public List<Func<GlobalVariable, Task>> SetVehicleSpecScripts { get; } = new();
		public List<Func<GlobalVariable, Task>> InitializeScripts { get; } = new();
		public List<Func<GlobalVariable, Task>> ElapseScripts { get; } = new();
		public List<Func<GlobalVariable, Task>> SetPowerScripts { get; } = new();
		public List<Func<GlobalVariable, Task>> SetBrakeScripts { get; } = new();
		public List<Func<GlobalVariable, Task>> SetReverserScripts { get; } = new();
		public List<Func<GlobalVariable, Task>> KeyDownScripts { get; } = new();
		public List<Func<GlobalVariable, Task>> KeyUpScripts { get; } = new();
		public List<Func<GlobalVariable, Task>> HornBlowScripts { get; } = new();
		public List<Func<GlobalVariable, Task>> DoorOpenScripts { get; } = new();
		public List<Func<GlobalVariable, Task>> DoorCloseScripts { get; } = new();
		public List<Func<GlobalVariable, Task>> SetSignalScripts { get; } = new();
		public List<Func<GlobalVariable, Task>> SetBeaconDataScripts { get; } = new();
		public List<Func<GlobalVariable, Task>> GetPluginVersionScripts { get; } = new();
		#endregion

		public GlobalVariable GlobalVariableInstance { get; private set; } = new();

		/// <summary>スクリプトを1つでも読み込んだかどうか</summary>
		/// <returns></returns>
		public bool LoadScriptPathListsAndScripts()
		{
			//設定ファイルを読み込む => XML
			// C:\abcDef\TR.ATSPI.CSharpScript.xml
			string rootSettingFilePath = Path.Combine(CurrentDllDirectoryPath, CurrentDllFileNameWithoutExtension + ".xml");

			//ルートとなる設定ファイルが存在する場合のみ, 読み込みを実行
			if (File.Exists(rootSettingFilePath))
			{
				try
				{
					//スクリプトファイルリストを読み込む
					LoadScriptListFile(rootSettingFilePath);
				}
				catch (Exception)
				{
					//何かエラーが起きたら, とりあえず空のXMLファイル(?)を書き出す
					string emptyXMLFilePath = Path.Combine(CurrentDllDirectoryPath, CurrentDllFileNameWithoutExtension + ".empty.xml");

					//XMLのタグのみが含まれたファイルを書き出す
					using StreamWriter sw = File.CreateText(emptyXMLFilePath);
					Serializer.Serialize(sw, new ScriptPathListClass());

					throw;
				}

				//スクリプトを読み込む
				LoadScriptFiles();

				//スクリプトのロードに成功しているかどうかを返す
				return
						LoadScripts.Count > 0 ||
						DisposeScripts.Count > 0 ||
						SetVehicleSpecScripts.Count > 0 ||
						InitializeScripts.Count > 0 ||
						ElapseScripts.Count > 0 ||
						SetPowerScripts.Count > 0 ||
						SetBrakeScripts.Count > 0 ||
						SetReverserScripts.Count > 0 ||
						KeyDownScripts.Count > 0 ||
						KeyUpScripts.Count > 0 ||
						HornBlowScripts.Count > 0 ||
						DoorOpenScripts.Count > 0 ||
						DoorCloseScripts.Count > 0 ||
						SetSignalScripts.Count > 0 ||
						SetBeaconDataScripts.Count > 0 ||
						GetPluginVersionScripts.Count > 0;
			}
			else
			{
				//XMLのタグのみが含まれたファイルを書き出す
				using StreamWriter sw = File.CreateText(rootSettingFilePath);
				ScriptPathListClass splc = new();

				Serializer.Serialize(sw, splc);

				//スクリプトのロードには失敗しているため
				return false;
			}
		}

		/// <summary>スクリプトリストファイルを読み込む</summary>
		/// <param name="path">ファイルの絶対パス</param>
		private void LoadScriptListFile(in string path)
		{
			ScriptPathListClass? listFile = null;

			//スクリプトリストファイルを読み込む
			using (StreamReader sr = new(path))
				listFile = Serializer.Deserialize(sr) as ScriptPathListClass;

			//スクリプトリストの読み込みに失敗した
			if (listFile is null)
				return;

			//スクリプトリストファイル情報にスクリプトリストファイルへのフルパス (`..`や`.`を取り除いたもの) を記録する
			listFile.CurrentScriptFileListPath = Path.GetFullPath(path);

			//読み込み済みスクリプトリストファイルに追加する
			ScriptFileLists.Add(listFile);

			//これ以上スクリプトリストファイルを参照していないか確認する
			if (listFile.ScriptFileLists is null || listFile.ScriptFileLists.Count <= 0)
				return;

			//スクリプトリストファイルを再帰的に読み込む
			string directoryPath = Path.GetDirectoryName(path) ?? string.Empty;
			foreach (var s in listFile.ScriptFileLists)
			{
				//`..` や `.` を除いたパスを取得する
				string nextListFilePath = Path.GetFullPath(Path.IsPathRooted(s) ? s : Path.Combine(directoryPath, s));

				//既にファイルを読み込み済みでないか確認する
				foreach (var list in ScriptFileLists)
					if (Equals(list.CurrentScriptFileListPath, nextListFilePath))
						continue;

				//スクリプトリストファイルを読み込む (再帰的)
				LoadScriptListFile(nextListFilePath);
			}
		}

		private void LoadScriptsFromPathList(List<Func<GlobalVariable, Task>> targetList, Func<ScriptPathListClass, List<string>> pathListSelector)
		{
			foreach(var source in ScriptFileLists)
			{
				List<string> filePathList = pathListSelector.Invoke(source);

				if (filePathList.Count <= 0)
					return;

				foreach (var s in filePathList)
				{
					//絶対パスに変換する
					string scriptString = string.Empty;
					string scriptFilePath = Path.IsPathRooted(s) ? s : Path.Combine(Path.GetDirectoryName(source.CurrentScriptFileListPath), s);

					//相対パスは, スクリプトファイルリストからの相対パスとして絶対パスに変換する
					using (StreamReader sr = new(scriptFilePath))
						scriptString = sr.ReadToEnd();

					var func = CreateActionFromScriptString(scriptString, scriptFilePath);

					if (func is not null)
						targetList.Add(func);
				}
			}
		}

		private void LoadScriptFiles()
		{
			LoadScriptsFromPathList(LoadScripts, v => v.LoadScripts);
			LoadScriptsFromPathList(DisposeScripts, v => v.DisposeScripts);
			LoadScriptsFromPathList(SetVehicleSpecScripts, v => v.SetVehicleSpecScripts);
			LoadScriptsFromPathList(InitializeScripts, v => v.InitializeScripts);
			LoadScriptsFromPathList(ElapseScripts, v => v.ElapseScripts);
			LoadScriptsFromPathList(SetPowerScripts, v => v.SetPowerScripts);
			LoadScriptsFromPathList(SetBrakeScripts, v => v.SetBrakeScripts);
			LoadScriptsFromPathList(SetReverserScripts, v => v.SetReverserScripts);
			LoadScriptsFromPathList(KeyDownScripts, v => v.KeyDownScripts);
			LoadScriptsFromPathList(KeyUpScripts, v => v.KeyUpScripts);
			LoadScriptsFromPathList(HornBlowScripts, v => v.HornBlowScripts);
			LoadScriptsFromPathList(DoorOpenScripts, v => v.DoorOpenScripts);
			LoadScriptsFromPathList(DoorCloseScripts, v => v.DoorCloseScripts);
			LoadScriptsFromPathList(SetSignalScripts, v => v.SetBeaconDataScripts);
			LoadScriptsFromPathList(GetPluginVersionScripts, v => v.GetPluginVersionScripts);
		}

		public static Func<GlobalVariable, Task>? CreateActionFromScriptString(in string scriptString)
			=> CreateActionFromScriptString(scriptString, UsingScriptOptions, false); //スクリプトを一時ファイルに書き出すのが面倒なので, ファイルパスなしでのデバッグはサポートしない
		public static Func<GlobalVariable, Task>? CreateActionFromScriptString(in string scriptString, in string scriptFilePath, bool IsDebug = false)
		{
			if(string.IsNullOrWhiteSpace(scriptFilePath))
				throw new ArgumentException("scriptFilePath cannot be null or empty or whitespace");

			string scriptFileFullPath = Path.GetFullPath(scriptFilePath);

			//スクリプトファイルが存在するディレクトリ
			string scriptFileDirectory = Path.GetDirectoryName(scriptFileFullPath); //空じゃないなら

			return CreateActionFromScriptString(
				scriptString,
				UsingScriptOptions
					.WithFilePath(scriptFileFullPath)
					.WithSourceResolver(ScriptSourceResolver.Default.WithBaseDirectory(scriptFileDirectory))
					.WithMetadataResolver(ScriptMetadataResolver.Default.WithBaseDirectory(scriptFileDirectory)),
				IsDebug);
		}
		public static Func<GlobalVariable, Task>? CreateActionFromScriptString(in string scriptString, in ScriptOptions options, bool IsDebug)
		{
			//スクリプト自体は空ファイルを許容する
			if (scriptString is null)
				throw new ArgumentNullException("scriptString cannot be null");

			var scriptRunner = Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.Create(
				scriptString,
				IsDebug ? //デバッグモードが有効であれば, デバッグ用の情報を含めてコンパイルする
					options.WithEmitDebugInformation(true).WithOptimizationLevel(OptimizationLevel.Debug)
					: options,
				typeof(GlobalVariable));

			//先にコンパイルを行う
			var compileResult = scriptRunner.Compile();

			//ログとして出力すべきだけど, 面倒なのでとりあえず保留
			foreach (var result in compileResult)
				if (result.Severity == DiagnosticSeverity.Error)
					return null; //エラーが返ったら実行しない

			var createdDelegate = scriptRunner.CreateDelegate();
			if (createdDelegate is null)
				return null;

			return (globals) => createdDelegate.Invoke(globals);
		}

		private Task RunScripts(List<Func<GlobalVariable, Task>> funcs)
		{
			List<Task> tasks = new();

			//Funcを受け取ってここでGlobalVariableInstanceの更新をやってもいい
			//...というかその方がコードが綺麗になりそうだけど, 実行コストがかかりそうなので採用しない

			foreach (var f in funcs)
				tasks.Add(f.Invoke(GlobalVariableInstance));

			return Task.WhenAll(tasks);
		}

		#region ATS Plugin Methods
		public async void Dispose()
		{
			await RunScripts(DisposeScripts);

			GlobalVariableInstance.ObjectHolder.Clear();

			GC.SuppressFinalize(this);
		}

		public async void DoorClose()
		{
			GlobalVariableInstance.IsDoorClosed = true;

			await RunScripts(DoorCloseScripts);
		}

		public async void DoorOpen()
		{
			GlobalVariableInstance.IsDoorClosed = false;

			await RunScripts(DoorOpenScripts);
		}


		public Hand Elapse(in State s, in IntPtr Pa, in IntPtr So)
		{
			GlobalVariableInstance.Elapse(s, Pa, So);

			RunScripts(ElapseScripts).Wait();

			return GlobalVariableInstance.GetHand();
		}

		public uint GetPluginVersion()
		{
			//返り値付きのメソッドはTask<T>を返さねばならないため, 同期的に実行
			RunScripts(GetPluginVersionScripts).Wait();

			return ATSPluginInterface.ATSPluginInterfaceVersionNum;
		}

		public async void HornBlow(int k)
		{
			GlobalVariableInstance.HornBlow = k;

			await RunScripts(HornBlowScripts);
		}

		public async void Initialize(int s)
		{
			GlobalVariableInstance.InitializeNum = s;

			await RunScripts(InitializeScripts);
		}

		public async void KeyDown(int k)
		{
			GlobalVariableInstance.KeyDown = k;

			await RunScripts(KeyDownScripts);
		}

		public async void KeyUp(int k)
		{
			GlobalVariableInstance.KeyUp = k;

			await RunScripts(KeyUpScripts);
		}

		public async void Load() => await RunScripts(LoadScripts);

		public async void SetBeaconData(Beacon b)
		{
			GlobalVariableInstance.SetBeaconData(b);

			await RunScripts(SetBeaconDataScripts);
		}

		public async void SetBrake(int b)
		{
			GlobalVariableInstance.SetBrakeArg = b;

			await RunScripts(SetBrakeScripts);
		}

		public async void SetPower(int p)
		{
			GlobalVariableInstance.SetPowerArg = p;

			await RunScripts(SetPowerScripts);
		}

		public async void SetReverser(int r)
		{
			GlobalVariableInstance.SetReverserArg = r;

			await RunScripts(SetReverserScripts);
		}

		public async void SetSignal(int s)
		{
			GlobalVariableInstance.SetSignal = s;

			await RunScripts(SetSignalScripts);
		}

		public async void SetVehicleSpec(Spec s)
		{
			GlobalVariableInstance.SetVehicleSpec(s);

			await RunScripts(SetVehicleSpecScripts);
		}
		#endregion
	}
}
