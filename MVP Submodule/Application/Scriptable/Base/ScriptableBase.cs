using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Redbean.Base
{
	public class ScriptableBase : ScriptableObject
	{
		public void Save()
		{
#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
#endif
		}
	}
}