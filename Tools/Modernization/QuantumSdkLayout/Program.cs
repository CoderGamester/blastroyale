using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// This tool reads the user's installed SDK. It contains no vendor implementation.
// Changes only source layout: every moved class body is extracted with Roslyn.
var arguments = args.ToList();
string Option(string name) {
  var index = arguments.IndexOf(name);
  if (index < 0 || index + 1 == arguments.Count) throw new ArgumentException("Missing " + name);
  return Path.GetFullPath(arguments[index + 1]);
}
var project = Option("--project");
var manifestPath = Option("--manifest");
var apply = arguments.Contains("--apply");
var rollback = arguments.Contains("--rollback");
if (rollback && (apply || arguments.Contains("--record-output-hashes"))) throw new ArgumentException("Rollback cannot be combined with apply or manifest authoring");
var createManifest = arguments.Contains("--record-output-hashes");
var manifest = JsonSerializer.Deserialize<LayoutManifest>(File.ReadAllText(manifestPath)) ?? throw new InvalidDataException("Empty manifest");
if (manifest.Version != 1) throw new InvalidDataException("Unsupported layout manifest version");
string Hash(byte[] data) => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
string FileHash(string path) => Hash(File.ReadAllBytes(path));
void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
var mergedPath = Path.Combine(project, manifest.MergedPath);
Require(FileHash(mergedPath + ".meta") == manifest.MergedMetaSha256, "Merged source metadata changed");
var before = FileHash(mergedPath) == manifest.MergedBeforeSha256;
var after = manifest.MergedAfterSha256 != null && FileHash(mergedPath) == manifest.MergedAfterSha256;
Require(before || after, "SDK merged source does not match the exact supported SDK hash");
foreach (var target in manifest.Targets) {
  var file = Path.Combine(project, target.RelativePath);
  var exists = File.Exists(file);
  var expected = after ? target.SourceAfterSha256 : target.SourceBeforeSha256;
  Require(expected == null ? !exists : exists && FileHash(file) == expected, "Source guard failed for " + target.Name);
  if (target.MetaSha256 != null) Require(FileHash(file + ".meta") == target.MetaSha256, "Canonical metadata changed for " + target.Name);
  else Require(!File.Exists(file + ".meta") || after && FileHash(file + ".meta") == target.NewMetaSha256, "New metadata guard failed for " + target.Name);
}
if (rollback) {
  if (before) { Console.WriteLine("Verified SDK layout already rolled back; original metadata preserved."); return; }
  Require(!Path.IsPathRooted(manifest.BackupDirectory) && !manifest.BackupDirectory.Contains("..") && !manifest.BackupDirectory.Contains(Path.DirectorySeparatorChar), "Backup must be a project-local directory name");
  var backup = Path.Combine(project, manifest.BackupDirectory);
  var mergedBackup = Path.Combine(backup, manifest.MergedPath);
  Require(File.Exists(mergedBackup) && FileHash(mergedBackup) == manifest.MergedBeforeSha256, "Original merged backup guard failed");
  foreach (var target in manifest.Targets.Where(t => t.SourceBeforeSha256 != null)) {
    var original = Path.Combine(backup, target.RelativePath);
    Require(File.Exists(original) && FileHash(original) == target.SourceBeforeSha256, "Original canonical backup guard failed for " + target.Name);
  }
  var retired = Path.Combine(backup, "retired-layout");
  Require(!Directory.Exists(retired), "Retired layout recovery directory already exists; review before overwriting");
  // Save every exact applied source/metadata before restoring or removing files.
  foreach (var relative in manifest.Targets.SelectMany(t => new[] { t.RelativePath, t.RelativePath + ".meta" }).Prepend(manifest.MergedPath)) {
    var current = Path.Combine(project, relative);
    if (!File.Exists(current)) continue;
    var destination = Path.Combine(retired, relative);
    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
    File.Copy(current, destination);
  }
  File.Copy(mergedBackup, mergedPath, true);
  foreach (var target in manifest.Targets) {
    var file = Path.Combine(project, target.RelativePath);
    if (target.SourceBeforeSha256 != null) File.Copy(Path.Combine(backup, target.RelativePath), file, true);
    else { File.Delete(file); if (target.MetaSha256 == null) File.Delete(file + ".meta"); }
  }
  Console.WriteLine("Rolled back exact SDK layout: " + manifest.Targets.Length + " types; applied sources saved under " + retired + "; original metadata preserved.");
  return;
}
if (after) { Console.WriteLine("Verified SDK layout already applied: " + manifest.Targets.Length + " types; metadata preserved."); return; }
var symbols = new[] { "UNITY_EDITOR", "UNITY_EDITOR_OSX", "UNITY_6000_3_OR_NEWER", "UNITY_2021_3_OR_NEWER", "UNITY_2020_2_OR_NEWER", "QUANTUM_UNITY", "QUANTUM_ENABLE_PHYSICS2D", "QUANTUM_ENABLE_PHYSICS3D", "QUANTUM_ENABLE_AI", "QUANTUM_ENABLE_TERRAIN", "QUANTUM_ENABLE_ADDRESSABLES", "QUANTUM_ENABLE_NEWTONSOFT", "QUANTUM_ENABLE_TEXTMESHPRO", "QUANTUM_ENABLE_AI_NAVIGATION", "QUANTUM_ENABLE_INPUTSYSTEM", "QUANTUM_ODIN_DISABLED", "DEBUG", "UNITY_ASSERTIONS" };
var options = new CSharpParseOptions(LanguageVersion.Latest, preprocessorSymbols: symbols);
CompilationUnitSyntax Parse(string text) => (CompilationUnitSyntax)CSharpSyntaxTree.ParseText(text, options).GetRoot();
var merged = File.ReadAllText(mergedPath);
Require(merged.StartsWith("#if !QUANTUM_DEV", StringComparison.Ordinal), "Expected original merged SDK compilation guard");
var tree = Parse(merged);
string[] EnclosingGuards(SyntaxNode declaration) {
  var stack = new List<(List<string> Branches, string Current)>();
  foreach (var trivia in tree.DescendantTrivia(descendIntoTrivia: true).Where(t => t.IsDirective && t.SpanStart < declaration.SpanStart).OrderBy(t => t.SpanStart)) {
    switch (trivia.GetStructure()) {
      case IfDirectiveTriviaSyntax conditional:
        var condition = conditional.Condition.ToString();
        stack.Add((new List<string> { condition }, condition));
        break;
      case ElifDirectiveTriviaSyntax alternative:
        Require(stack.Count > 0, "Unbalanced SDK conditional directives");
        var frame = stack[^1];
        var next = alternative.Condition.ToString();
        var active = "!(" + string.Join(" || ", frame.Branches.Select(b => "(" + b + ")")) + ") && (" + next + ")";
        frame.Branches.Add(next);
        stack[^1] = (frame.Branches, active);
        break;
      case ElseDirectiveTriviaSyntax:
        Require(stack.Count > 0, "Unbalanced SDK conditional directives");
        var previous = stack[^1];
        stack[^1] = (previous.Branches, "!(" + string.Join(" || ", previous.Branches.Select(b => "(" + b + ")")) + ")");
        break;
      case EndIfDirectiveTriviaSyntax:
        Require(stack.Count > 0, "Unbalanced SDK conditional directives");
        stack.RemoveAt(stack.Count - 1);
        break;
    }
  }
  return stack.Select(s => s.Current).ToArray();
}
var planned = new Dictionary<string, byte[]>();
var removals = new List<Microsoft.CodeAnalysis.Text.TextSpan>();
foreach (var target in manifest.Targets) {
  bool Matches(ClassDeclarationSyntax declaration) => declaration.Identifier.ValueText == target.Name &&
    (declaration.TypeParameterList?.Parameters.Count ?? 0) == target.GenericArity;
  var parts = tree.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(c => Matches(c) && c.Parent is NamespaceDeclarationSyntax n && n.Name.ToString() == "Quantum").ToArray();
  var expectedParts = target.MergedDeclarationCount > 0 ? target.MergedDeclarationCount : target.Name == "QuantumMonoBehaviour" ? 2 : 1;
  Require(parts.Length == expectedParts, "Unexpected merged declaration count for " + target.Name);
  Require(parts.All(p => p.Modifiers.Any(SyntaxKind.PartialKeyword) == target.ExpectedPartial), "SDK partial modifier changed for " + target.Name);
  var declarationWithBase = parts.Where(p => p.BaseList != null).SingleOrDefault() ?? parts.Single();
  var guards = EnclosingGuards(declarationWithBase);
  var sameGuards = parts.All(p => EnclosingGuards(p).SequenceEqual(guards));
  Require(guards.Length > 0 && guards[0] == "!QUANTUM_DEV" && parts.All(p => EnclosingGuards(p).FirstOrDefault() == "!QUANTUM_DEV"), "SDK compilation guard changed for " + target.Name);
  var openGuards = string.Join("\r\n", guards.Select(g => "#if " + g));
  var closeGuards = string.Join("\r\n", guards.Select(_ => "#endif"));
  var file = Path.Combine(project, target.RelativePath);
  var old = File.Exists(file) ? File.ReadAllText(file) : null;
  var canonicalTree = old == null ? null : Parse(old);
  var canonical = canonicalTree?.DescendantNodes().OfType<ClassDeclarationSyntax>().Single(Matches);
  var canonicalNamespace = canonical?.Parent as NamespaceDeclarationSyntax;
  Require(sameGuards || canonical == null, "Different conditional contexts require a new canonical source for " + target.Name);
  var usingNodes = parts.SelectMany(p => ((NamespaceDeclarationSyntax)p.Parent!).Usings);
  var existingUsings = canonicalNamespace?.Usings.Select(u => u.WithoutTrivia().ToString()).ToHashSet() ?? new HashSet<string>();
  var missingUsings = usingNodes.Where(u => !existingUsings.Contains(u.WithoutTrivia().ToString())).Select(u => u.WithoutTrivia().ToString()).Distinct().ToArray();
  var bodies = parts.Select(p => merged.Substring(p.OpenBraceToken.Span.End, p.CloseBraceToken.Span.Start - p.OpenBraceToken.Span.End)).ToArray();
  string output;
  if (canonical != null && old != null && canonicalNamespace != null) {
    // Keep one canonical declaration, so Unity can bind exactly one MonoScript.
    var body = "\r\n" + openGuards + "\r\n" + string.Join("\r\n", bodies) + "\r\n" + closeGuards + "\r\n";
    output = old.Insert(canonical.CloseBraceToken.Span.Start, body);
    var attributes = parts.SelectMany(p => p.AttributeLists).Select(a => merged.Substring(a.Span.Start, a.Span.Length)).ToArray();
    if (attributes.Length > 0) {
      var declarationStart = canonical.Modifiers.First().Span.Start;
      output = output.Insert(declarationStart, "\r\n" + openGuards + "\r\n  " + string.Join("\r\n  ", attributes) + "\r\n" + closeGuards + "\r\n  ");
    }
    if (missingUsings.Length > 0) output = output.Insert(canonicalNamespace.OpenBraceToken.Span.End, "\r\n  " + string.Join("\r\n  ", missingUsings) + "\r\n");
  } else {
    var declaration = declarationWithBase;
    Require(parts.Where(p => p != declaration).All(p => p.AttributeLists.Count == 0), "Additional class attributes need an explicit merge for " + target.Name);
    var header = merged.Substring(declaration.Span.Start, declaration.OpenBraceToken.Span.End - declaration.Span.Start);
    var imports = string.Join("\r\n  ", missingUsings);
    if (!target.ExpectedPartial) {
      // Generic/non-generic names collide in Unity's MonoScript generator even
      // when neither declaration is partial. Preserve conditional imports for
      // these real, single-declaration types (notably UnityEditor in players).
      imports = string.Join("\r\n", usingNodes.GroupBy(u => u.WithoutTrivia().ToString()).Select(group => {
        var alternatives = group.Select(u => "(" + string.Join(" && ", EnclosingGuards(u).Select(g => "(" + g + ")")) + ")").Distinct();
        return "#if " + string.Join(" || ", alternatives) + "\r\n  " + group.First().WithoutTrivia().ToString() + "\r\n#endif";
      }));
    }
    if (!sameGuards) {
      // Preserve each original namespace import and body under its actual condition.
      // This consolidates declarations, without enabling feature-specific members/imports.
      imports = string.Join("\r\n", usingNodes.GroupBy(u => u.WithoutTrivia().ToString()).Select(group => {
        var directive = group.First();
        // A repeated import is emitted once, enabled in the union of its original contexts.
        var alternatives = group.Select(u => "(" + string.Join(" && ", EnclosingGuards(u).Select(g => "(" + g + ")")) + ")").Distinct();
        return "#if " + string.Join(" || ", alternatives) + "\r\n  " + directive.WithoutTrivia().ToString() + "\r\n#endif";
      }));
      bodies = parts.Select((part, index) => {
        var conditional = EnclosingGuards(part);
        return "\r\n" + string.Join("\r\n", conditional.Select(g => "#if " + g)) + "\r\n" + bodies[index] + "\r\n" + string.Join("\r\n", conditional.Select(_ => "#endif")) + "\r\n";
      }).ToArray();
    }
    output = openGuards + "\r\nnamespace Quantum {\r\n  " + imports + "\r\n  " + header + string.Join("\r\n", bodies) + "\r\n  }\r\n}\r\n" + closeGuards + "\r\n";
  }
  Require(!Parse(output).GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error), "Relocated syntax invalid for " + target.Name);
  Require(Parse(output).DescendantNodes().OfType<ClassDeclarationSyntax>().Count(Matches) == 1, "Canonical declaration not unique for " + target.Name);
  planned[file] = Encoding.UTF8.GetBytes(output);
  target.SourceAfterSha256 = Hash(planned[file]);
  if (target.MetaSha256 == null) {
    var meta = "fileFormatVersion: 2\nguid: " + target.Guid + "\nMonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n";
    planned[file + ".meta"] = Encoding.UTF8.GetBytes(meta);
    target.NewMetaSha256 = Hash(planned[file + ".meta"]);
  }
  removals.AddRange(parts.Select(p => p.Span));
}
foreach (var span in removals.OrderByDescending(s => s.Start)) merged = merged.Remove(span.Start, span.Length);
Require(!Parse(merged).GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error), "Merged source became syntactically invalid");
planned[mergedPath] = Encoding.UTF8.GetBytes(merged);
manifest.MergedAfterSha256 = Hash(planned[mergedPath]);
var completedManifest = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }) + "\n";
if (!createManifest) {
  var recorded = JsonSerializer.Deserialize<LayoutManifest>(File.ReadAllText(manifestPath))!;
  Require(recorded.MergedAfterSha256 == manifest.MergedAfterSha256 && recorded.Targets.Zip(manifest.Targets).All(pair => pair.First.SourceAfterSha256 == pair.Second.SourceAfterSha256 && pair.First.NewMetaSha256 == pair.Second.NewMetaSha256), "Relocation differs from reviewed output hashes");
}
if (apply) {
  Require(!Path.IsPathRooted(manifest.BackupDirectory) && !manifest.BackupDirectory.Contains("..") && !manifest.BackupDirectory.Contains(Path.DirectorySeparatorChar), "Backup must be a project-local directory name");
  var backup = Path.Combine(project, manifest.BackupDirectory);
  Require(!Directory.Exists(backup), "Backup already exists; review before overwriting");
  foreach (var item in planned) {
    if (File.Exists(item.Key)) {
      var relative = Path.GetRelativePath(project, item.Key);
      var destination = Path.Combine(backup, relative);
      Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
      File.Copy(item.Key, destination);
    }
  }
  foreach (var item in planned) { Directory.CreateDirectory(Path.GetDirectoryName(item.Key)!); File.WriteAllBytes(item.Key, item.Value); }
}
if (createManifest) File.WriteAllText(manifestPath, completedManifest);
Console.WriteLine((apply ? "Applied" : "Checked") + " exact SDK layout relocation of " + manifest.Targets.Length + " types, preserving original members and canonical metadata.");

sealed class LayoutManifest {
  public int Version { get; set; }
  public string Sdk { get; set; } = "";
  public string MergedPath { get; set; } = "";
  public string MergedBeforeSha256 { get; set; } = "";
  public string? MergedAfterSha256 { get; set; }
  public string MergedMetaSha256 { get; set; } = "";
  public string BackupDirectory { get; set; } = ".modernization-sdk-layout-backup";
  public Target[] Targets { get; set; } = [];
}
sealed class Target {
  public string Name { get; set; } = "";
  public bool ExpectedPartial { get; set; } = true;
  public int GenericArity { get; set; }
  public int MergedDeclarationCount { get; set; }
  public string RelativePath { get; set; } = "";
  public string? SourceBeforeSha256 { get; set; }
  public string? SourceAfterSha256 { get; set; }
  public string? MetaSha256 { get; set; }
  public string? NewMetaSha256 { get; set; }
  public string Guid { get; set; } = "";
}
