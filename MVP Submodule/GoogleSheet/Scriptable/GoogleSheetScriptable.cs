using Redbean.Base;
using UnityEngine;

namespace Redbean.Table
{
	[CreateAssetMenu(fileName = "GoogleSheetScriptable", menuName = "Redbean/Library/GoogleSheetScriptable")]
	public class GoogleSheetScriptable : ScriptableBase
	{
		[HideInInspector] public string GoogleSheetId;
		[HideInInspector] public string GoogleClientId;
		[HideInInspector] public string GoogleSecretId;
		
		[Header("Get generation path")]
		public string ContainerPath;
		public string ItemPath;
	}
}