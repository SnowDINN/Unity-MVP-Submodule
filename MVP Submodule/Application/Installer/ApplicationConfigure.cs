using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Redbean.Base;
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
	public class ApplicationConfigure : ScriptableObject
	{
		public List<BootstrapContext> SetupBootstraps = new();
		
		[Header("Get application information during runtime")]
		public string Version;
	}

	public class AppSettings : SettingsBase<ApplicationConfigure>
	{
		public static List<BootstrapContext> SetupBootstraps => Installer.SetupBootstraps;
		
		public static string Version =>
			string.IsNullOrEmpty(Installer.Version) ? Application.version : Installer.Version;
		
		public static async Task BootstrapSetup<T>() where T : Bootstrap =>
			await Activator.CreateInstance<T>().Start();
	}
	
#if UNITY_EDITOR
#region UNITY EDITOR
	
	[CustomEditor(typeof(ApplicationConfigure), true)]
	public class AppInstallerEditor : Editor
	{
		private ApplicationConfigure app => target as ApplicationConfigure;
		
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