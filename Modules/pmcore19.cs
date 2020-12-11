using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Timers;
using System.Linq;
using UnityEngine;
using PoseMod;

namespace pmcaller
{
	public static class pmcaller
	{
		public const string UNITY_VERSION = "Unity 2019";


		internal static bool hooked = false;
		internal static System.Timers.Timer timer;

		//Initial call from Native dll
		public static void Call()
		{
			if (!hooked)
			{
				//Start a timer that will check every half-second until the game is initiated
				timer = new System.Timers.Timer(500);
				timer.Elapsed += new System.Timers.ElapsedEventHandler(_Wait);
				timer.Start();
				hooked = true;

				Core.DebugMode = File.Exists("pmdebug.txt");

				string _dir = "PoseMod/Modules/" + pmcaller.UNITY_VERSION + "/PreInit/";
				if(Directory.Exists(_dir))
					pm_initializer.LoadModules(_dir);
			}
		}

		internal static void _Wait(object source, ElapsedEventArgs e)
		{
			try
			{
				//check if current scene name is valid
				//if it's not, the game is not ready yet
				if(Application.loadedLevelName != null && Application.loadedLevelName.Length > 0)
				{
					Core.Pm_Gameobject = new GameObject("PoseMod");
					Core.Pm_Gameobject.AddComponent<pm_initializer>();
					GameObject.DontDestroyOnLoad(Core.Pm_Gameobject);

					//Optional, might remove or make optional later
					//New error messages are added to posemod.log
					Application.logMessageReceived += delegate(string condition, string stackTrace, LogType type)
					{
						File.AppendAllText("PoseMod.log", condition + "\x0d\x0a" + stackTrace + "\x0d\x0a\x0d\x0a");
					};

					if(Core.Pm_Gameobject != null)
						timer.Stop();
				}
			}catch{}
		}
	}



	public class pm_initializer : MonoBehaviour
	{
		public void Start()
		{
			LoadModules("PoseMod/Modules/" + pmcaller.UNITY_VERSION + "/");

			//This MonoBehaviour is no longer needed
			GameObject.Destroy(this);
		}


		public static void LoadModules(string directory)
		{
			string[] _modules = Directory.GetFiles(directory, "*.dll");

			foreach (string _module in _modules)
			{
				LoadModule(_module);
			}
		}

		public static void LoadModule(string fileName)
		{
			try
			{
				Assembly _assembly = Assembly.LoadFile(fileName);

				if(_assembly == null)
					throw new Exception("Assembly.LoadFile() returned null");

				foreach (Type _type in _assembly.GetTypes())
				{
					if(_type.IsClass && _type.Namespace == "PoseMod" && _type.Name == "Module")
					{

						//Attempt to call Init()
						try
						{
							MethodInfo _method = _type.GetMethod("Init");

							if(_method != null)
								_method.Invoke(null, null);
						}
						catch (Exception ex)
						{
							WriteToLog("ERROR: Calling Init() \"" + fileName + "\"\x0d\x0a" + ex.Message + "\x0d\x0a" + ex.StackTrace);
						}

						//Add as monobehaviour (IF it is a monobehaviour)
						/*if(_type.IsSubclassOf(typeof(MonoBehaviour)))
						{
							Core.Pm_Gameobject.AddComponent(_type);
						}*/

						return;
					}
				}

				throw new Exception("PoseMod.Module.Init() function not found, make sure it's static");
			}
			catch (Exception ex)
			{
				WriteToLog("ERROR: Loading module \"" + fileName + "\"\x0d\x0a" + ex.Message + "\x0d\x0a" + ex.StackTrace);
			}
		}

		public static void WriteToLog(string text)
		{
			text.Log();
		}
	}
}



//namespace PoseMod.Core
namespace PoseMod
{
	public static class Core
	{
		public const string NewLine = "\x0d\x0a";
		public static GameObject Pm_Gameobject;

		public static bool DebugMode;

		public static void Log(this string text, string fileName = "PoseMod.log")
		{
			File.AppendAllText(fileName, text + NewLine);
		}



		//Nowdays SceneManager.Scenes exists
		//public static List<string> GetScenes()
		public static List<string> GetScenesFromDataFile()
		{
			List<string> _scenes = new List<string>();

			string _filename = Application.dataPath + "/globalgamemanagers";
			if(!File.Exists(_filename))
			{
				_filename = Application.dataPath + "/maindata";
				if(!File.Exists(_filename))
				{
					throw new Exception("GetScenes() can't find globalgamemanagers file, perhaps no file access");
				}
			}

			byte[] _data = File.ReadAllBytes(_filename);

			List<char> __find = new List<char>();
			__find.AddRange(".unity".ToCharArray());
			//__find.Add('\x00');
			char[] _find = __find.ToArray();

			uint _index = 0;

			LBL_Redo:
			_index = _data.ScanBinStr(_find, _index);

			if(_index == uint.MaxValue)
				goto LBL_End;

			uint x = _index + 6u;//(uint)".unity".Length;
			if(_data[x] == '3')
			{
				_index = x;
				goto LBL_Redo;
			}

			for (x = _index; x != 0; x--)
			{
				if(_data[x] == '/' || _data[x] == '\\' || _data[x] == '\x00')
				{
					string _newSceneName = string.Empty;
					uint _newLen = _index - ++x;

					for (int i = 0; i < _newLen; i++)
						_newSceneName += ((char)_data[x + i]);

					_scenes.Add(_newSceneName);
					_index += (uint)_newSceneName.Length;
					goto LBL_Redo;
				}
			}
			LBL_End:
			return _scenes;
		}

		public static uint ScanBinStr(this byte[] bytes, string text, uint startIndex = 0)
		{
			return bytes.ScanBinStr(text.ToCharArray());
		}
		public static uint ScanBinStr(this byte[] bytes, char[] text, uint startIndex = 0)
		{
			char[] _chars = text;
			uint _len = (uint)bytes.LongLength;
			int _len2 = _chars.Length;
			uint _test = 0;

			for (uint i = startIndex; i < _len; i++)
			{
				if(bytes[i] == _chars[0])
				{
					for (uint ii = 0; ii < _len2; ii++)
					{
						_test = (i+ii);
						if(_test >= _len)
							goto LBL_End;

						if(bytes[_test] != _chars[ii])
							goto LBL_Continue;
					}

					//success, return address
					return i;
				}
				LBL_Continue:;
			}
			LBL_End:;
			return uint.MaxValue;
		}



		public static bool ToBool(this string b)
		{return System.Convert.ToBoolean(b);}

		public static int ToInt(this string i)
		{return System.Convert.ToInt32(i);}

		public static float ToFloat(this string f)
		{return System.Convert.ToSingle(f);}



		//UNITY ---------------------------------------------------------------------------------

		public static GameObject AddChild(this GameObject parent, string objName = null)
		{
			GameObject gameObject = objName == null ? new GameObject() : new GameObject(objName);

			Transform transform = gameObject.transform;
			transform.parent = parent.transform;
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			transform.localScale = Vector3.one;

			return gameObject;
		}

		public static void SetShape(this GameObject obj, string shape, float value)
		{
			SkinnedMeshRenderer _sk = obj.GetComponentInChildren<SkinnedMeshRenderer>();
			if(_sk != null)
				for (int i = 0; i < _sk.sharedMesh.blendShapeCount; i++)
					if(_sk.sharedMesh.GetBlendShapeName(i) == shape)
						_sk.SetBlendShapeWeight(i, value);
		}

		public static Component AddComponent(this GameObject obj, string component)
		{
			Type _type = Type.GetType(component, false, true);
			if(_type != null) return obj.AddComponent(_type);
			else return null;
		}

		public static Transform FindChild(this Transform transform, string name)
		{
			Transform[] _trans = transform.GetComponentsInChildren<Transform>();

			for (int i = 0; i < _trans.Length; i++)
				if(_trans[i].name == name)
					return _trans[i];
			return null;
		}
		public static string GetFullPath(this Transform transform, bool RemoveTopParent = true)
		{
			string _retval = transform.name;
			Transform _dealingTransform = transform;

			LBL_NEXT:;
			_dealingTransform = _dealingTransform.parent;
			if(_dealingTransform != null)
			{
				_retval = _dealingTransform.name + "/" + _retval;
				goto LBL_NEXT;
			}
			else if(RemoveTopParent)
			{
				if(_retval.Contains('/'))
					_retval = _retval.Remove(0, _retval.IndexOf('/')+1);
			}

			return _retval;
		}


		//INVOKE -----------------------------------------------------------------------------------

		public static void Broadcast(this List<Type> types, Action<Type> action)
		{
			for (int i = 0; i < types.Count; i++)
				action(types[i]);
		}
		public static T CallFrom<T>(this Type type, string function, object instance = null, object[] parameters = null)
		{
			return (T)type.GetMethod(function).Invoke(instance, parameters);
		}
		public static void CallFrom(this Type type, string function, object instance = null, object[] parameters = null)
		{
			type.GetMethod(function).Invoke(instance, parameters);
		}
		public static void RunMethod(this string scriptName, string methodName, object[] parameters, BindingFlags _flags = BindingFlags.Default)
		{
			Component[] scripts = GameObject.FindObjectsOfType<Component>();
			MethodInfo _method;

			for (int i = 0; i < scripts.Length; i++)
				if(scripts[i].GetType().Name == scriptName)
				{
					if(_flags == BindingFlags.Default)
						_method = scripts[i].GetType().GetMethod(methodName);
					else
						_method = scripts[i].GetType().GetMethod(methodName, _flags);

					if(_method != null)
					{
						_method.Invoke(scripts[i], parameters);
					}
				}
		}
		public static void Run(this string scriptName, Action<Component> action)
		{
			Component[] scripts = GameObject.FindObjectsOfType<Component>();
			for (int i = 0; i < scripts.Length; i++)
				if(scripts[i].GetType().Name == scriptName)
					action(scripts[i]);
		}

		//GET -----------------------------------------------------------------------------------

		public static MonoBehaviour GetScript(this GameObject obj, string name)
		{
			MonoBehaviour[] _monos = obj.GetComponents<MonoBehaviour>();
			for (int i = 0; i < _monos.Length; i++)
				if(_monos[i].GetType().Name == name)
					return _monos[i];
			return null;
		}

		public static MethodInfo GetMethod(this Component script, string name)
		{
			MethodInfo _retval = script.GetType().GetMethod(name);
			if(_retval == null)
			{
				_retval = script.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
				if(_retval == null)
					_retval = script.GetType().GetMethod(name, BindingFlags.Default);
			}

			return _retval;
		}

		public static MemberInfo GetFieldOrProperty(this Component script, string name)
		{
			PropertyInfo _retval = script.GetProperty(name);
			if(_retval == null) return script.GetField(name);
			else return _retval;
		}
		public static PropertyInfo GetProperty(this Component script, string name)
		{
			return script.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
		}
		public static FieldInfo GetField(this Component script, string name)
		{
			return script.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
		}

		public static object GetFieldOrPropertyValue(this Component script, string name)
		{
			object _retval = script.GetPropertyValue(name);
			if(_retval == null) return script.GetFieldValue(name);
			else return _retval;
		}
		public static object GetPropertyValue(this Component script, string name)
		{
			PropertyInfo _prop = script.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
			if(_prop == null)
				return null;
			else
				return _prop.GetValue(script, null);
		}
		public static object GetFieldValue(this Component script, string name)
		{
			FieldInfo _prop = script.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
			if(_prop == null)
				return null;
			else
				return _prop.GetValue(script);
		}

		public static object Get(this string scriptName, Func<Component, object> action)
		{
			Component[] scripts = GameObject.FindObjectsOfType<Component>();
			for (int i = 0; i < scripts.Length; i++)
				if(scripts[i].GetType().Name == scriptName)
				{
					return action(scripts[i]);
				}
			return null;
		}

		public static T[] That<T>(this T[] array, Predicate<T> predicate)
		{
			List<T> _retval = new List<T>();

			foreach (var item in array)
				if(predicate(item))
					_retval.Add(item);

			return _retval.ToArray();
		}

		public static Component[] AllOf(this string typeName)
		{
			Type _testType = Type.GetType(typeName);

			if(_testType == null)
			{
				return GameObject.FindObjectsOfType<Component>().That
					(
						delegate(Component obj)
						{
							return obj.GetType().Name == typeName;
						}
					);
			}
			else
			{
				return (Component[])GameObject.FindObjectsOfType(_testType);
			}
		}

		/// <summary>
		/// Static or Unique ONLY!
		/// </summary>
		public static object GetValue(this string ScriptName, string varName)
		{
			return ScriptName.Get(delegate(Component obj)
				{
					PropertyInfo _property = obj.GetProperty(varName);
					if(_property != null)
					{
						return _property.GetValue(obj, null);
					}
					else
					{
						FieldInfo _field = obj.GetField(varName);
						if(_field != null)
							return _field.GetValue(obj);
					}
					return null;
				}
			);
		}



		//SET -----------------------------------------------------------------------------------

		public static void SetFieldAndProperty(this Component script, string name, object value)
		{
			script.SetProperty(name, value);
			script.SetField(name, value);
		}
		public static void SetProperty(this Component script, string name, object value)
		{
			PropertyInfo _field = script.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
			if(_field != null) _field.SetValue(script, value, null);
		}
		public static void SetField(this Component script, string name, object value)
		{
			FieldInfo _field = script.GetType().GetField(name);
			if(_field != null) _field.SetValue(script, value);
		}


		public static void SetValue(this string ScriptName, string varName, object value/*, int index = 0*/)
		{
			ScriptName.Run((Component obj) => obj.SetFieldAndProperty(varName, value));
		}






	}
}




