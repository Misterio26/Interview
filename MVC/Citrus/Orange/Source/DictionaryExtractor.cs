using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using Lime;

namespace Orange
{
	public class DictionaryExtractor
	{
		private static readonly Regex tanTextMatcher = new Regex(
			@"^(\s*""Text""\s*:)\s*""(?<string>[^""\\]*(?:\\.[^""\\]*)*)"",?\s?$",
			RegexOptions.Compiled | RegexOptions.Multiline);

		private static readonly Regex tanAnimatedTextMatcher = new Regex(
			@"^\s*\[\s*\d+\s*,\s*\d+\s*,\s*""(?<string>[^""\\]*(?:\\.[^""\\]*)*)""\],?\s?$",
			RegexOptions.Compiled | RegexOptions.Multiline);

		private static readonly Regex sourceTextMatcher = new Regex(
			@"(?<prefix>[@$])?""(?<string>[^""\\]*(?:\\.[^""\\]*)*)""", RegexOptions.Compiled);

		private static readonly Regex taggedStringMatcher = new Regex(@"^\[.*\](.+)$", RegexOptions.Compiled);

		private LocalizationDictionary dictionary;

		public void ExtractDictionary()
		{
			dictionary = new LocalizationDictionary();
			ExtractTexts();
			foreach (var name in GetFileNames()) {
				CleanupAndSaveDictionary(name);
			}
		}

		private static string GetDefaultFileName()
		{
			return Path.ChangeExtension("Dictionary.txt", CreateSerializer().GetFileExtension());
		}

		private static IEnumerable<string> GetFileNames()
		{
			var files = Directory.GetFiles(
				The.Workspace.AssetsDirectory,
				"*" + CreateSerializer().GetFileExtension(),
				SearchOption.TopDirectoryOnly);
			return files
				.Select(f => Path.GetFileName(f))
				.Where(f => f.StartsWith("Dictionary"));
		}

		private void CleanupAndSaveDictionary(string path)
		{
			var result = new LocalizationDictionary();
			LoadDictionary(result, path);
			var addContext = ShouldAddContextToLocalizedDictionary() || path == GetDefaultFileName();
			MergeDictionaries(result, dictionary, addContext);
			SaveDictionary(result, path);
		}

		private static void MergeDictionaries(LocalizationDictionary current, LocalizationDictionary modified, bool addContext)
		{
			int added = 0, deleted = 0, updated = 0;
			foreach (var key in modified.Keys.ToList()) {
				if (!current.ContainsKey(key)) {
					Logger.Write("+ " + key);
					added++;
					current[key] = new LocalizationEntry() {
						Text = modified[key].Text,
						Context = modified[key].Context
					};
				} else {
					var currentEntry = current[key];
					var newEntry = modified[key];
					if (currentEntry.Context != newEntry.Context) {
						Logger.Write("Context updated for: " + key);
						updated++;
						currentEntry.Context = newEntry.Context;
					}
				}
				if (!addContext) {
					current[key].Context = "";
				}
			}
			foreach (var key in current.Keys.ToList()) {
				if (!modified.ContainsKey(key) && !LocalizationDictionary.IsComment(key)) {
					Logger.Write("- " + key);
					deleted++;
					current.Remove(key);
				}
			}

			Logger.Write("Added {0}\nDeleted {1}\nContext updated {2}", added, deleted, updated);
		}

		private static void SaveDictionary(LocalizationDictionary dictionary, string path)
		{
			using (new DirectoryChanger(The.Workspace.AssetsDirectory)) {
				using (var stream = new FileStream(path, FileMode.Create)) {
					dictionary.WriteToStream(CreateSerializer(), stream);
				}
			}
		}

		private static ILocalizationDictionarySerializer CreateSerializer()
		{
			var format = The.Workspace.ProjectJson.GetValue("LocalizationDictionaryFormat", "Text");
			if (format == "Text") {
				return new LocalizationDictionaryTextSerializer();
			} else {
				throw new Lime.Exception();
			}
		}

		private static void LoadDictionary(LocalizationDictionary dictionary, string path)
		{
			using (new DirectoryChanger(The.Workspace.AssetsDirectory)) {
				if (File.Exists(path)) {
					using (var stream = new FileStream(path, FileMode.Open)) {
						dictionary.ReadFromStream(CreateSerializer(), stream);
					}
				}
			}
		}

		private void ExtractTexts()
		{
			bool ScanFilter(DirectoryInfo directoryInfo)
			{
				if (directoryInfo.Name == "bin") return false;
				if (directoryInfo.Name == "obj") return false;
				if (directoryInfo.Name == ".svn") return false;
				if (directoryInfo.Name == ".git") return false;
				if (directoryInfo.Name == ".vs") return false;
				if (directoryInfo.Name == "Citrus") return false;
				return true;
			}

			var sourceFiles = new ScanOptimizedFileEnumerator(The.Workspace.ProjectDirectory, ScanFilter);
			using (new DirectoryChanger(The.Workspace.ProjectDirectory)) {
				var files = sourceFiles.Enumerate(".cs");
				foreach (var fileInfo in files) {
					ProcessSourceFile(fileInfo.Path);
				}
			}
			using (new DirectoryChanger(The.Workspace.AssetsDirectory)) {
				var sceneArray = The.Workspace.AssetFiles.Enumerate(".scene");
				var jsonArray = The.Workspace.AssetFiles.Enumerate(".json");

				var files = sceneArray.Concat(jsonArray);

				foreach (var fileInfo in files) {
					// First of all scan lines like this: "[]..."
					ProcessSourceFile(fileInfo.Path);
					// Then like this: Text "..."
					if (!ShouldLocalizeOnlyTaggedSceneTexts()) {
						ProcessSceneFile(fileInfo.Path);
					}
				}
			}
			using (new DirectoryChanger(The.Workspace.AssetsDirectory)) {
				var files = The.Workspace.AssetFiles.Enumerate(".tan");
				foreach (var fileInfo in files) {
					ProcessTanFile(fileInfo.Path);
				}
			}
		}

		private static bool ShouldLocalizeOnlyTaggedSceneTexts()
		{
			return The.Workspace.ProjectJson.GetValue("LocalizeOnlyTaggedSceneTexts", false);
		}

		private static bool ShouldAddContextToLocalizedDictionary()
		{
			return The.Workspace.ProjectJson.GetValue("AddContextToLocalizedDictionary", true);
		}

		private void ProcessSourceFile(string file)
		{
			var code = File.ReadAllText(file, Encoding.UTF8);
			var context = GetContext(file);
			foreach (var match in sourceTextMatcher.Matches(code)) {
				var m = match as Match;
				var prefix = m.Groups["prefix"].Value;
				var s = m.Groups["string"].Value;
				if (HasAlphabeticCharacters(s) && IsCorrectTaggedString(s)) {
					if (string.IsNullOrEmpty(prefix)) {
						AddToDictionary(s, context);
					} else {
						var type = prefix == "@" ? "verbatim" : "interpolated";
						Logger.Write($"WARNING: Ignoring {type} string: {s}");
					}
				}
			}
		}

		private void ProcessSceneFile(string file)
		{
			const string textPropertiesPattern = @"^(\s*Text)\s""([^""\\]*(?:\\.[^""\\]*)*)""$";
			var code = File.ReadAllText(file, Encoding.Default);
			var context = GetContext(file);
			foreach (var match in Regex.Matches(code, textPropertiesPattern, RegexOptions.Multiline)) {
				var s = ((Match)match).Groups[2].Value;
				if (HasAlphabeticCharacters(s)) {
					AddToDictionary(s, context);
				}
			}
		}

		private void ProcessTanFile(string path)
		{
			var content = File.ReadAllText(path, Encoding.UTF8);
			var context = GetContext(path);
			var onlyTagged = ShouldLocalizeOnlyTaggedSceneTexts();
			var matches1 = tanTextMatcher.Matches(content);
			var matches2 = tanAnimatedTextMatcher.Matches(content);

			foreach (var match in matches1.OfType<Match>().Concat(matches2.OfType<Match>()).Where(m => m.Success)) {
				var s = match.Groups["string"].Value;
				if (HasAlphabeticCharacters(s) && (!onlyTagged || IsCorrectTaggedString(s))) {
					AddToDictionary(s, context);
				}
			}
		}

		private static string GetContext(string file)
		{
			return file;
		}

		private static bool IsCorrectTaggedString(string str)
		{
			return taggedStringMatcher.Match(str).Success;
		}

		private void AddToDictionary(string key, string context)
		{
			var match = taggedStringMatcher.Match(key);
			if (match.Success) {
				// The line starts with "[...]..."
				var value = Unescape(match.Groups[1].Value);
				if (key.StartsWith("[]")) {
					key = key.Substring(2);
				}
				AddToDictionaryHelper(Unescape(key), value, context);
			} else {
				// The line has no [] prefix, but still should be localized.
				// E.g. most of texts in scene files.
				AddToDictionaryHelper(Unescape(key), Unescape(key), context);
			}
		}

		private static bool HasAlphabeticCharacters(string text)
		{
			return text.Any(c => char.IsLetter(c));
		}

		private void AddToDictionaryHelper(string key, string value, string context)
		{
			var e = dictionary.GetEntry(key);
			e.Text = value;
			var ctx = new List<string>();
			if (!string.IsNullOrWhiteSpace(e.Context)) {
				ctx = e.Context.Split('\n').ToList();
			}
			if (!ctx.Contains(context)) {
				ctx.Add(context);
			}
			e.Context = string.Join("\n", ctx);
		}

		private static string Unescape(string text)
		{
			return text.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\'", "'");
		}
	}
}
