using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Redbean.Base
{
	public class ScriptableBase<T> where T : ScriptableObject
	{
		private static readonly string resourceLocation = $"{typeof(T).Name}";
		
		private static T scriptable;
		protected static T Scriptable
		{
			get
			{
				if (!scriptable)
					scriptable = Resources.Load<T>(resourceLocation);

				return scriptable;
			}
		}
		

		public static void Save()
		{
#if UNITY_EDITOR
			EditorUtility.SetDirty(Scriptable);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
#endif
		}
	}
}