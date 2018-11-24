using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace devcenter_snippets_collector
{
    struct Snippet
    {
        public string File;
        public int Line;
        public string Code;
    }

    class Program
    {
        private static IEnumerable<string> IgnorePaths => new List<string> {
            "content/downloads",
            "content/guide/xamarin/getting-started.md",
            "content/guide/xamarin/datastore.md",
            "content/guide/xamarin/users.md",
            "content/guide/xamarin/files.md",
            "content/guide/xamarin-v3.0/migration.md",
            "content/guide/common/enterprise/dynamics.md",
        };

        const string snippetDeclaration = "```csharp";

        private static IEnumerable<Snippet> CodeSnippets(string path)
        {
            foreach (var ignorePath in IgnorePaths)
            {
                if (path.EndsWith(ignorePath, StringComparison.Ordinal)) return new List<Snippet>();
            }
            var attr = File.GetAttributes(path);
            if (attr.HasFlag(FileAttributes.Directory))
            {
                return Directory.GetDirectories(path)
                                .Union(Directory.GetFiles(path))
                                .SelectMany(x => CodeSnippets(x));
            }
            else
            {
                if (Path.GetExtension(path).Equals(".md"))
                {
                    var fileContent = File.ReadAllText(path);
                    var snippets = new List<Snippet>();
                    var offset = 0;
                    var line = 0;
                    while (offset != fileContent.Length)
                    {
                        var startIndex = fileContent.IndexOf(snippetDeclaration, offset, StringComparison.Ordinal);
                        if (startIndex == -1)
                        {
                            offset = fileContent.Length;
                            continue;
                        }
                        line += fileContent.Substring(offset, startIndex - offset).Count(x => x == '\n') + 1;
                        var endIndex = fileContent.IndexOf("```", startIndex + 1, StringComparison.Ordinal);
                        if (endIndex == -1)
                        {
                            offset = fileContent.Length;
                            continue;
                        }
                        startIndex += snippetDeclaration.Length;
                        var code = fileContent.Substring(startIndex, endIndex - startIndex).Trim();
                        snippets.Add(new Snippet() {
                            File = path,
                            Line = line,
                            Code = code
                        });
                        offset = endIndex;
                    }
                    return snippets;
                }
            }
            return new List<Snippet>();
        }
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                throw new ArgumentException("Usage: devcenter-snippets-collector <devcenter path> <output cs file path>");
            }
            var devCenterPath = args[0].Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.Personal));
            var csFile = args[1].Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.Personal));
            if (!Directory.Exists(devCenterPath))
            {
                throw new ArgumentException($"DevCenter Path {args[0]} does not exists");
            }
            var codeSnippets = CodeSnippets(devCenterPath);
            var usings = new List<string>();
            var assemblies = new List<List<string>>();
            var output = codeSnippets.Select((snippet, index) =>
            {
                var file = snippet.File.Substring(devCenterPath.Length + 1);
                var linesOfCode = snippet.Code.Split("\n");
                var snippetLines = new List<string>
                {
                    "",
                    $"// {file}:{snippet.Line}"
                };
                usings.AddRange(linesOfCode.Where(x => x.StartsWith("using ") && !x.StartsWith("using (")).Select(x => $"{x} // {file}:{snippet.Line}"));
                linesOfCode = linesOfCode.Where(x => {
                    return (
                            !x.StartsWith("using ") &&
                            !x.StartsWith("nuget") &&
                            !x.StartsWith("<") &&
                            x.Trim().Length > 0
                        ) ||
                        x.StartsWith("using (");
                }).ToArray();
                var isIOS = snippet.Code.Contains("UIApplication") ||
                            snippet.Code.Contains("NSFileManager") ||
                            snippet.Code.Contains(".RegisterForToken()") ||
                            snippet.Code.Contains(".DisablePush()");
                var isAndroid = snippet.Code.Contains("Intent intent") ||
                                snippet.Code.Contains("new Intent") ||
                                snippet.Code.Contains("KinveyGCMService") ||
                                snippet.Code.Contains("Android.App.Application.Context");
                if (isIOS)
                {
                    snippetLines.Add("#if __IOS__");
                }
                else if (isAndroid)
                {
                    snippetLines.Add("#if __ANDROID__");
                }
                if (linesOfCode.Where(x => x.StartsWith("[assembly: ")).Count() > 0)
                {
                    snippetLines.AddRange(linesOfCode);
                    assemblies.Add(snippetLines);
                    if (isIOS || isAndroid)
                    {
                        snippetLines.Add("#endif");
                    }
                    return new List<string>();
                }
                if (linesOfCode.Where(x => {
                    return (x.StartsWith("public ") && !x.StartsWith("public override ") && !x.StartsWith("public string ")) ||
                        (x.StartsWith("protected ") && !x.StartsWith("protected override ")) ||
                        (x.StartsWith("private ") && !x.StartsWith("private override ")) ||
                        x.StartsWith("[");
                }).Count() > 0)
                {
                    snippetLines.AddRange(linesOfCode);
                    if (isIOS || isAndroid)
                    {
                        snippetLines.Add("#endif");
                    }
                    return snippetLines;
                }
                snippetLines.AddRange(new string[] {
                    $"class CodeSnippet{index + 1} : CodeSnippet",
                    "{",
                });
                if (linesOfCode.Where(x => {
                    return x.StartsWith("public override ") ||
                        x.StartsWith("protected override ") ||
                        x.StartsWith("private override ") ||
                        x.StartsWith("public string ");
                }).Count() == 0)
                {
                    snippetLines.AddRange(new string[] {
                        $"    async Task Snippet{index + 1}()",
                        "    {",
                    });
                    snippetLines.AddRange(linesOfCode.Select(x => $"        {x}"));
                    snippetLines.AddRange(new string[] {
                        "        ",
                        "        // *******************************************",
                        "        await new Task(() => Console.WriteLine(\"\"));",
                        "    }",
                    });
                }
                else
                {
                    snippetLines.AddRange(linesOfCode.Select(x => $"    {x}"));
                }
                snippetLines.AddRange(new string[] {
                    "}",
                });
                if (isIOS || isAndroid)
                {
                    snippetLines.Add("#endif");
                }
                return snippetLines;
            }).ToArray();
            var lines = new List<string> {
                "using System;",
                "using System.IO;",
                "using System.Linq;",
                "using System.Collections.Generic;",
                "using System.Threading.Tasks;",
                "using Kinvey;",
                "using Newtonsoft.Json;",
                "using Newtonsoft.Json.Linq;",
                "#if __ANDROID__",
                "    using Android.App;",
                "    using Android.Content;",
                "    using Android.Support.V4.App;",
                "    using KinveySnippets.Droid;",
                "#elif __IOS__",
                "    using Foundation;",
                "    using UIKit;",
                "    using KinveySnippets.iOS;",
                "#endif",
                "",
                "#pragma warning disable CS0105 // Using directive appeared previously in this namespace",
                "#pragma warning disable CS0168 // Variable is declared but never used",
                "#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body",
                "#pragma warning disable RECS0117 // Local variable has the same name as a member and hides it",
            };
            lines.AddRange(usings.Select(x => {
                if (x.Contains("using Foundation;"))
                {
                    return string.Join("\n", new string[] {
                        "#if __IOS__",
                        x,
                        "#endif",
                    });
                }
                else if (x.Contains("using Android."))
                {
                    return string.Join("\n", new string[] {
                        "#if __ANDROID__",
                        x,
                        "#endif",
                    });
                }
                return x;
            }));
            lines.Add("");
            foreach (var assembly in assemblies)
            {
                lines.AddRange(assembly.Where((x, i) => i == 1 || x.StartsWith("[assembly: ")));
            }
            foreach (var assembly in assemblies)
            {
                lines.AddRange(assembly.Where(x => !x.StartsWith("[assembly: ")));
            }
            lines.AddRange(new string[] {
                "",
                "namespace KinveySnippets",
                "{",
            });
            lines.AddRange(output.SelectMany(x => x).Select(x => $"    {x}"));
            lines.AddRange(new string[] {
                "}",
                "#pragma warning restore CS0105 // Using directive appeared previously in this namespace",
                "#pragma warning restore CS0168 // Variable is declared but never used",
                "#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body",
                "#pragma warning restore RECS0117 // Local variable has the same name as a member and hides it",
            });
            File.WriteAllLines(csFile, lines);
            Console.WriteLine($"{codeSnippets.Count()} snippets");
        }
    }
}
