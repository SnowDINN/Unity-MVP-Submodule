﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using UnityEngine;

namespace Redbean.Table
{
	public class GoogleSheetGenerator
	{
		public const string Namespace = nameof(Redbean);

		private static GoogleSheetScriptable googleSheetScriptable => 
			ApplicationLoader.GetScriptable<GoogleSheetScriptable>();
		
		private static string containerPath =>
			$"{Application.dataPath.Replace("Assets", "")}{googleSheetScriptable.ContainerPath}";

		private static string itemPath =>
			$"{Application.dataPath.Replace("Assets", "")}{googleSheetScriptable.ItemPath}";

		/// <summary>
		/// 테이블 시트 데이터 호출
		/// </summary>
		public static async Task<Dictionary<string, string[]>> GetSheetAsync()
		{
			var sheetDictionary = new Dictionary<string, string[]>();
			var client = new ClientSecrets
			{
				ClientId = googleSheetScriptable.GoogleClientId,
				ClientSecret = googleSheetScriptable.GoogleSecretId
			};
			var scopes = new[]
			{
				SheetsService.Scope.SpreadsheetsReadonly
			};
			var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(client, scopes, Namespace, CancellationToken.None).Result;

			var service = new SheetsService(new BaseClientService.Initializer
			{
				HttpClientInitializer = credential
			});
			var sheets = await service.Spreadsheets.Get(googleSheetScriptable.GoogleSheetId).ExecuteAsync();
			
			// Skip Summary Sheet
			var skipSheets = sheets.Sheets.Skip(1);
			foreach (var sheet in skipSheets)
			{
				var sheetName = sheet.Properties.Title;
				var sheetInfo = await service.Spreadsheets.Values.Get(googleSheetScriptable.GoogleSheetId, $"{sheetName}!A:Z").ExecuteAsync();

				var tsv = ToTSV(sheetInfo.Values).Split("\r\n");
				var tsvRefined = TsvRefined(tsv);
				
				sheetDictionary.Add(sheetName, tsvRefined);
			}

			DeleteFiles($"{googleSheetScriptable.ItemPath}");
			return sheetDictionary;
		}
		
		/// <summary>
		/// 테이블 C# 스크립트 생성
		/// </summary>
		public static async Task GenerateCSharpTableAsync(Dictionary<string, string[]> tables)
		{
			var stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("using System.Collections.Generic;");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine($"namespace {Namespace}.Table");
			stringBuilder.AppendLine("{");
			stringBuilder.AppendLine($"\tpublic class TableContainer");
			stringBuilder.AppendLine("\t{");

			foreach (var table in tables)
				stringBuilder.AppendLine($"\t\tpublic static Dictionary<{table.Value[1].Split("\t").First()}, T{table.Key}> {table.Key} = new();");
			
			stringBuilder.AppendLine("\t}");
			stringBuilder.AppendLine("}");
			
			if (Directory.Exists(containerPath))
				Directory.CreateDirectory(containerPath);
			
			File.Delete($"{containerPath}/Table.cs");
			await File.WriteAllTextAsync($"{containerPath}/TableContainer.cs", $"{stringBuilder}");
		}

		/// <summary>
		/// 테이블 아이템 C# 스크립트 생성
		/// </summary>
		public static async Task GenerateCSharpSheetAsync(string key, string[] value)
		{
			var classname = $"T{key}";
			
			var variableNames = value[0].Split("\t");
			var variableTypes = value[1].Split("\t");

			var stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("using System.Collections.Generic;");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine($"namespace {Namespace}.Table");
			stringBuilder.AppendLine("{");
			stringBuilder.AppendLine($"\tpublic class {classname} : {nameof(ITable)}");
			stringBuilder.AppendLine("\t{");
			
			for (var i = 0; i < variableNames.Length; i++)
				stringBuilder.AppendLine($"\t\tpublic {variableTypes[i]} {variableNames[i]};");
			
			stringBuilder.AppendLine();
			stringBuilder.AppendLine($"\t\tpublic void {nameof(ITable.Apply)}(IEnumerable<string> values)");
			stringBuilder.AppendLine("\t\t{");
			stringBuilder.AppendLine($"\t\t\tTableContainer.{key}.Clear();");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("\t\t\tforeach (var value in values)");
			stringBuilder.AppendLine("\t\t\t{");
			stringBuilder.AppendLine("\t\t\t\tvar split = value.Split(\"\\t\");");
			stringBuilder.AppendLine($"\t\t\t\tvar item = new {classname}");
			stringBuilder.AppendLine("\t\t\t\t{");

			for (var i = 0; i < variableNames.Length; i++)
			{
				var type = variableTypes[i];
				var convert = type switch
				{
					"int" => $"int.Parse(split[{i}]),",
					"long" => $"long.Parse(split[{i}]),",
					"float" => $"float.Parse(split[{i}]),",
					"double" => $"double.Parse(split[{i}]),",
					"int[]" => $"Array.ConvertAll(split[{i}].Split('|'), int.Parse),",
					"long[]" => $"Array.ConvertAll(split[{i}].Split('|'), long.Parse),",
					"float[]" => $"Array.ConvertAll(split[{i}].Split('|'), float.Parse),",
					"double[]" => $"Array.ConvertAll(split[{i}].Split('|'), double.Parse),",
					"string[]" => $"split[{i}].Split('|'),",
					_ => $"split[{i}],"
				};

				stringBuilder.AppendLine($"\t\t\t\t\t{variableNames[i]} = {convert}");
			}
			
			stringBuilder.AppendLine("\t\t\t\t};");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine($"\t\t\t\tTableContainer.{key}.Add(item.Id, item);");
			stringBuilder.AppendLine("\t\t\t}");
			stringBuilder.AppendLine("\t\t}");
			stringBuilder.AppendLine("\t}");
			stringBuilder.AppendLine("}");
			
			if (Directory.Exists(itemPath))
				Directory.CreateDirectory(itemPath);
			
			await File.WriteAllTextAsync($"{itemPath}/{classname}.cs", $"{stringBuilder}");
		}
		
		private static string ToTSV(IList<IList<object>> rows)
		{
			var csvString = new StringBuilder();
			for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
			{
				var stringBuilder = new StringBuilder();
				for (var i = 0; i < rows[0].Count; i++)
				{
					if (i >= rows[rowIndex].Count)
					{
						for (var idx = 0; idx < rows[0].Count - rows[rowIndex].Count; idx++)
							stringBuilder.Append("\t");
						break;
					}
					
					if (rows[rowIndex][i].ToString().Contains("\t"))
						stringBuilder.Append($"\"{rows[rowIndex][i]}\"");
					else
						stringBuilder.Append(rows[rowIndex][i]);
					
					if (i < rows[rowIndex].Count - 1)
						stringBuilder.Append("\t");
				}
				
				csvString.Append(stringBuilder.ToString());
				
				if (rowIndex < rows.Count - 1)
					csvString.Append("\r\n");
			}
			
			return csvString.ToString();
		}
		
		private static string[] TsvRefined(string[] tsv)
		{
			// Skip Contains '~' Column
			var skipIndex = tsv[0].Split("\t")
				.Select((key, index) => (key, index))
				.Where(_ => _.key.Contains('~'))
				.ToArray();
			if (!skipIndex.Any())
				return tsv;

			for (var index = 0; index < tsv.Length; index++)
			{
				var sheetValues = tsv[index].Split("\t").ToList();
				var removeTarget = skipIndex.Select(index => sheetValues[index.index]).ToList();

				foreach (var target in removeTarget)
					sheetValues.Remove(target);

				tsv[index] = string.Join("\t", sheetValues);
			}

			return tsv;
		}
		
		private static void DeleteFiles(string path)
		{
			var directory = new DirectoryInfo(path);
			foreach (var file in directory.GetFiles())
				file.Delete();
		}
	}
}