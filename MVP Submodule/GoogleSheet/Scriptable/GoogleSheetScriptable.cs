using Redbean.Base;
using UnityEngine;

namespace Redbean.Table
{
	[CreateAssetMenu(fileName = "GoogleSheetScriptable", menuName = "Redbean/Library/GoogleSheetScriptable")]
	public class GoogleSheetScriptable : ScriptableObject
	{
		[Header("Get generation path")]
		public string ContainerPath;
		public string ItemPath;
	}
	
	public class GoogleSheetReferencer : ScriptableBase<GoogleSheetScriptable>
	{
		public static string Path
		{
			get => Scriptable.ContainerPath;
			set
			{
				Scriptable.ContainerPath = value;
				Save();
			}
		}

		public static string ItemPath
		{
			get => Scriptable.ItemPath;
			set
			{
				Scriptable.ItemPath = value;
				Save();
			}
		}

		public static string SheetId;
		public static string ClientId;
		public static string ClientSecretId;
	}
}