using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using Lime;
using Orange;

namespace Robot.OrangePlugin
{
	public static partial class OrangePlugin
	{
		private static class EditorOnlyConfiguration
		{
			public const string ConfigurationName = "Editor.Release";

			private static readonly List<Func<string, bool>> IncludeCondition = new List<Func<string, bool>> {
				"Fonts".Equals,
				"Game".Equals,
				"Game/Countries".Equals,
				(path) => path.StartsWith("Game/Countries/00Tutorial"),
				(path) => path.StartsWith("Game/Countries/01Italy"),
				"Levels".Equals,
				"Overlays".Equals,
				"Shell".Equals,
				(path) => path.StartsWith("Shell/Board"),
				(path) => path.StartsWith("Shell/Chars"),
				(path) => path.StartsWith("Shell/Common"),
				(path) => path.StartsWith("Shell/EditorFilter"),
				(path) => path.StartsWith("Shell/EditorToolbar"),
				(path) => path.StartsWith("Shell/Icons"),
				(path) => path.StartsWith("Shell/Particles"),
				(path) => path.StartsWith("Shell/UI")
			};

			private static string AbsoluteDataPath => Path.GetFullPath(The.Workspace.AssetsDirectory);

			public static bool ScanFilter(DirectoryInfo directory)
			{
				var relativeUnixPath = CsprojSynchronization.ToUnixSlashes(
					directory.FullName.Remove(0, AbsoluteDataPath.Length + 1));
				return IncludeCondition.Any(condition => condition(relativeUnixPath));
			}
		}


		private static ConfigWindow Window;

		private static void InstallHooks()
		{
			var gitRootPath = Path.Combine(The.Workspace.ProjectDirectory, ".git");
			var di = new System.IO.DirectoryInfo(gitRootPath);
			if (di.Exists) {
				var installedHookPath = Path.Combine(gitRootPath, "hooks", "pre-commit");
				var distrHookPath = Path.Combine(The.Workspace.ProjectDirectory, "hooks", "pre-commit");
				var fiInstalledHook = new System.IO.FileInfo(installedHookPath);
				var fiDistrHook = new System.IO.FileInfo(distrHookPath);
				if (!fiDistrHook.Exists) {
					return;
				}
				if (!fiInstalledHook.Exists || fiInstalledHook.LastWriteTime != fiDistrHook.LastWriteTime) {
					System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(installedHookPath));
					System.IO.File.Copy(distrHookPath, installedHookPath, true);
					File.SetLastWriteTime(installedHookPath, fiDistrHook.LastWriteTime);
					if (!fiInstalledHook.Exists) {
						Console.WriteLine("Installed git hook.");
					} else if (fiInstalledHook.LastWriteTime != fiDistrHook.LastWriteTime) {
						Console.WriteLine("Updated git hook.");
					}
				}
			}
		}

		[Export(nameof(Orange.OrangePlugin.Initialize))]
		public static void DoInitialization()
		{
			InstallHooks();
			Console.WriteLine("Robot Orange plugin initialized.");
		}

		[Export(nameof(Orange.OrangePlugin.BuildUI))]
		public static void BuildUI(IPluginUIBuilder builder)
		{
			Window = new ConfigWindow(builder);
		}

		private static bool isCustomWidgetsAssebliesBuilt;

		[Export(nameof(Orange.OrangePlugin.GetRequiredAssemblies))]
		public static string[] GetCustomWidgetsAssebliesList()
		{
#if DEBUG
			return new string[0];
#endif

			string configurationShort = BuildConfiguration.Debug;
#if DEBUG
			configurationShort = BuildConfiguration.Debug;
#else
			configurationShort = BuildConfiguration.Release;
#endif

			if (configurationShort != BuildConfiguration.Debug && !isCustomWidgetsAssebliesBuilt) {
				string path = "";
				string configuration = configurationShort;

				if (The.Workspace.ActivePlatform == TargetPlatform.Win) {
					path = Path.Combine(The.Workspace.ProjectDirectory, "Robot.Tangerine", "Robot.Tangerine.Win.sln");
					configuration += "LibsOnly";
				}
				else if (The.Workspace.ActivePlatform == TargetPlatform.iOS) {
					path = Path.Combine(The.Workspace.ProjectDirectory, "Robot.CustomWidgets", "CustomWidgets.iOS", "CustomWidgets.iOS.sln");
				}
				else if (The.Workspace.ActivePlatform == TargetPlatform.Android) {
					path = Path.Combine(The.Workspace.ProjectDirectory, "Robot.CustomWidgets", "CustomWidgets.Android", "CustomWidgets.Android.sln");
				}

				if (!string.IsNullOrEmpty(path)) {
					var builder = new SolutionBuilder(
					The.Workspace.ActivePlatform,
					path,
					configuration
				);

					builder.Clean();
					builder.Restore();
					builder.Build();
				}
			}
			isCustomWidgetsAssebliesBuilt = true;

			return new[] {
				$"../{configurationShort}CustomWidgets/Robot.Core.Common",
				$"../{configurationShort}CustomWidgets/Robot.Layer1.Common",
				$"../{configurationShort}CustomWidgets/Robot.Layer2.Common",
				$"../{configurationShort}CustomWidgets/Robot.Layer3.Common",
				$"../{configurationShort}CustomWidgets/Robot.Layer4.Common",
			};
		}

		[Export(nameof(Orange.OrangePlugin.MenuItems))]
		[ExportMetadata(nameof(IMenuItemMetadata.Label), "Trim dictionaries")]
		public static void TrimDictionaries()
		{
			var encoding = new UTF8Encoding(true);
			foreach (var path in Directory.GetFiles(The.Workspace.AssetsDirectory, "Dictionary*.txt")) {
				var fileName = Path.GetFileNameWithoutExtension(path);

				Func<string, string> lineTransformer = (s) => s.TrimEnd();
				if (fileName.EndsWith("FR")) {
					lineTransformer = (s) => {
						s = s.TrimEnd();
						s = s.Replace(" ?", "\u00A0?");
						s = s.Replace(" !", "\u00A0!");
						s = s.Replace(" :", "\u00A0:");
						s = s.Replace(" ;", "\u00A0;");
						return s;
					};
				}

				var result = new StringBuilder();
				var changed = false;
				var separatorLineFound = true;
				using (var reader = new StreamReader(path)) {
					string line;
					while ((line = reader.ReadLine()) != null) {
						var newLine = lineTransformer(line);
						var empty = (newLine == "");
						if (empty && separatorLineFound) {
							changed = true;
							continue;
						}
						separatorLineFound = empty;
						if (!changed) {
							changed = newLine != line;
						}
						result.AppendLine(newLine);
					}
				}
				if (changed) {
					Console.WriteLine("'{0}' trimmed", fileName);
					File.WriteAllText(path, result.ToString(), encoding);
				}
			}
		}

		[Export(nameof(Orange.OrangePlugin.Finalize))]
		public static void DoFinalization()
		{
			Window = null;
			Console.WriteLine("Let's Eat Orange plugin finalized.");
		}

		[Export(nameof(Orange.OrangePlugin.CommandLineArguments))]
		public static string GetCommandLineArguments()
		{
			return Window.GetCommandLineArguments();
		}

		[Export(nameof(Orange.OrangePlugin.MenuItems))]
		[ExportMetadata(nameof(IMenuItemMetadata.Label), "Cook Editor Assets")]
		[ExportMetadata(nameof(IMenuItemMetadata.Priority), 4)]
		public static void CookEditorAssetsAction()
		{
			var previousFileEnumerator = The.Workspace.AssetFiles;
			The.Workspace.AssetFiles = new ScanOptimizedFileEnumerator(
				The.Workspace.AssetsDirectory, EditorOnlyConfiguration.ScanFilter);
			AssetCooker.CookForActivePlatform();
			The.Workspace.AssetFiles = previousFileEnumerator;
		}

		[Export(nameof(Orange.OrangePlugin.MenuItemsWithErrorDetails))]
		[ExportMetadata(nameof(IMenuItemMetadata.Label), "Build & Run Match3 editor only")]
		[ExportMetadata(nameof(IMenuItemMetadata.Priority), 1)]
		public static string BuildAndRunEditorOnly()
		{
			CookEditorAssetsAction();
			if (!Actions.BuildGame(
				The.Workspace.ActivePlatform,
				The.Workspace.CustomSolution,
				EditorOnlyConfiguration.ConfigurationName)
			) {
				return "Can not BuildGame";
			}

			The.UI.ScrollLogToEnd();
			Actions.RunGame(
				The.Workspace.ActivePlatform,
				The.Workspace.CustomSolution,
				EditorOnlyConfiguration.ConfigurationName);

			return null;
		}

		[Export(nameof(Orange.OrangePlugin.MenuItemsWithErrorDetails))]
		[ExportMetadata(nameof(IMenuItemMetadata.Label), "Build, Restore & Run (Robot)")]
		[ExportMetadata(nameof(IMenuItemMetadata.Priority), -1)]
		public static string BuildAndRestoreAndRun()
		{
			var builder = new SolutionBuilder(
				The.Workspace.ActivePlatform,
				The.Workspace.CustomSolution,
				BuildConfiguration.Release
			);
			if (!builder.Restore()) {
				return "Can not restore game";
			}

			Actions.BuildAndRun(BuildConfiguration.Release);
			return null;
		}
	}
}
