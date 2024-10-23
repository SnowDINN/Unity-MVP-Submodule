using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Redbean
{
	[Serializable]
	public class BootstrapContext
	{
		public string FullName;
	}
	
	[CreateAssetMenu(fileName = "ApplicationConfigure", menuName = "Redbean/Library/ApplicationConfigure")]
	public class ApplicationScriptable : ScriptableObject
	{
		[HideInInspector]
		public List<BootstrapContext> SetupBootstraps = new();

		[Header("Get application scriptable asset")]
		public List<ScriptableObject> ScriptableObjects;
		
		[Header("Get application information during runtime")]
		public string Version;
	}

	public class ApplicationLoader
	{
		private static readonly string resourceLocation = $"{nameof(ApplicationScriptable)}";
		
		private static ApplicationScriptable scriptable;
		protected static ApplicationScriptable Scriptable
		{
			get
			{
				if (!scriptable)
					scriptable = Resources.Load<ApplicationScriptable>(resourceLocation);

				return scriptable;
			}
		}
		
		private static List<BootstrapContext> SetupBootstraps => Scriptable.SetupBootstraps;
		private static List<ScriptableObject> ScriptableObjects => Scriptable.ScriptableObjects;
		
		public const string UnityAssembly = "Assembly-CSharp";
		
		public static string GetVersion() =>
			string.IsNullOrEmpty(Scriptable.Version) 
				? Application.version 
				: Scriptable.Version;

		public static List<Bootstrap> GetBootstraps() => 
			SetupBootstraps
				.Select(bootstrap => Type.GetType($"{bootstrap.FullName}, {UnityAssembly}"))
				.Select(type => Activator.CreateInstance(type) as Bootstrap)
				.ToList();

		public static T GetScriptable<T>() where T : ScriptableObject =>
			ScriptableObjects
				.FirstOrDefault(_ => _.GetType() == typeof(T)) as T;
	}
	
#if UNITY_EDITOR
#region UNITY EDITOR
	
	[CustomEditor(typeof(ApplicationScriptable), true)]
	public class ApplicationScriptableEditor : Editor
	{
		private ApplicationScriptable app => target as ApplicationScriptable;
		
		private List<string> bootstrapArray;
		private ReorderableList runtimeBootstrapRecorder;
		private void OnEnable()
		{
			bootstrapArray = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(x => x.GetTypes())
				.Where(x => typeof(IBootstrap).IsAssignableFrom(x)
				            && typeof(Bootstrap).FullName != x.FullName
				            && !x.IsInterface
				            && !x.IsAbstract)
				.Select(x => x.FullName)
				.ToList();
			runtimeBootstrapRecorder = new ReorderableList(app.SetupBootstraps,
			                                               typeof(BootstrapContext),
			                                               true, true, true, true);
			
			runtimeBootstrapRecorder.drawHeaderCallback += OnRuntimeBootstrapDrawHeaderCallback;
			runtimeBootstrapRecorder.drawElementCallback += OnRuntimeBootstrapDrawHeaderCallback;
			runtimeBootstrapRecorder.onChangedCallback += OnChangedCallback;
		}
		private void OnDisable()
		{
			runtimeBootstrapRecorder.drawHeaderCallback -= OnRuntimeBootstrapDrawHeaderCallback;
			runtimeBootstrapRecorder.drawElementCallback -= OnRuntimeBootstrapDrawHeaderCallback;
			runtimeBootstrapRecorder.onChangedCallback -= OnChangedCallback;
		}
		private void OnRuntimeBootstrapDrawHeaderCallback(Rect rect)
		{
			EditorGUI.LabelField(rect, "Runtime Bootstrap List");
		}
		
		private void OnRuntimeBootstrapDrawHeaderCallback(Rect rect, int index, bool isActive, bool isFocused)
		{
			EditorGUI.BeginChangeCheck();
			{
				var indexOfName = bootstrapArray.IndexOf(app.SetupBootstraps[index].FullName);
				if (indexOfName < 0)
					indexOfName = 0;
				indexOfName = EditorGUI.Popup(new Rect(rect.x, rect.y + 2.5f, rect.width, rect.height), indexOfName, bootstrapArray.ToArray());
				app.SetupBootstraps[index].FullName = bootstrapArray[indexOfName];
			}
			if (!EditorGUI.EndChangeCheck())
				return;
			EditorUtility.SetDirty(app);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
		
		private void OnChangedCallback(ReorderableList list)
		{
			EditorUtility.SetDirty(app);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
		public override void OnInspectorGUI()
		{
			EditorGUILayout.LabelField("Sorting Execution Bootstrap", EditorStyles.boldLabel);
			
			serializedObject.Update();
			{
				runtimeBootstrapRecorder.DoLayoutList();
				DrawPropertiesExcluding(serializedObject, "m_Script");
			}
			serializedObject.ApplyModifiedProperties();
		}
	}
	
#endregion
#endif
}