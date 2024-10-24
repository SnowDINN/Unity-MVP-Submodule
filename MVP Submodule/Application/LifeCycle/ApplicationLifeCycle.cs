using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Redbean
{
	public class ApplicationStart
	{
		[RuntimeInitializeOnLoadMethod]
		public static void Start()
		{
			Application.runInBackground = true;
			Application.targetFrameRate = 60;
			
			var go = new GameObject("[Application Life Cycle]", typeof(ApplicationLifeCycle));
			GameObject.DontDestroyOnLoad(go);
		}
	}
	
	public class ApplicationLifeCycle : MonoBase
	{
		public delegate void onApplicationExit();
		public static event onApplicationExit OnApplicationExit;

		public static Transform Transform { get; private set; }
		public static bool IsAppChecked { get; private set; }
		public static bool IsAppReady { get; private set; }

		private async void Awake()
		{
			Transform = transform;

			foreach (var instance in ApplicationLoader.GetSetupBootstraps())
				await instance.Start();
			
			IsAppReady = true;
		}

		public override void OnDestroy()
		{
			IsAppReady = false;
			OnApplicationExit?.Invoke();
			
#if UNITY_EDITOR
			if (EditorApplication.isPlaying)
				EditorApplication.isPlaying = false;
#endif
		}

		public static void AppCheckSuccess()
		{
			IsAppChecked = true;
		}
		
		public static void AppCheckFail()
		{
			IsAppChecked = false;
		}
	}
}