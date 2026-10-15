using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Scriban;
using Scriban.Runtime;

namespace Gameplay.Tags.Generation;

public static class Program
{
    private const string Usage = "GameplayTags.Generator --input <xml> --template <sbn> --output <file> [--template <sbn> --output <file> ...]";
    private sealed record Output(string Template, string Path);
    private sealed record Options(string Input, List<Output> Outputs);

    private sealed class TagNode
    {
        public required XElement Element { get; init; }
        public required string Name { get; init; }
        public required string Path { get; init; }
        public TagNode? Parent { get; init; }
        public int Id { get; set; }
        public List<TagNode> Children { get; } = new();
    }

    private static readonly HashSet<string> ReservedNames = new(StringComparer.Ordinal)
    {
        "Tag", "Catalog", "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event",
        "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit",
        "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object",
        "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return",
        "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this",
        "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
        "void", "volatile", "while", "__arglist", "__makeref", "__reftype", "__refvalue"
    };

    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 1 && args[0] == "--help")
            {
                Console.WriteLine(Usage);
                return 0;
            }
            var options = ParseOptions(args);
            Generate(options);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Gameplay tags: {exception.Message}");
            return 1;
        }
    }

    private static Options ParseOptions(string[] args)
    {
        string? input = null;
        string? template = null;
        var outputs = new List<Output>();
        for (var index = 0; index < args.Length; index += 2)
        {
            var key = args[index];
            if (index + 1 >= args.Length)
            {
                throw new ArgumentException(Usage);
            }
            var path = System.IO.Path.GetFullPath(args[index + 1]);
            switch (key)
            {
                case "--input" when input == null:
                    input = path;
                    break;
                case "--template" when template == null:
                    template = path;
                    break;
                case "--output" when template != null:
                    outputs.Add(new Output(template, path));
                    template = null;
                    break;
                default:
                    throw new ArgumentException(Usage);
            }
        }
        if (input == null || outputs.Count == 0 || template != null)
        {
            throw new ArgumentException(Usage);
        }
        var paths = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        paths.Add(input);
        foreach (var output in outputs)
        {
            if (!paths.Add(output.Path) || Directory.Exists(output.Path))
            {
                throw new ArgumentException("Input and outputs must be distinct file paths.");
            }
        }
        foreach (var output in outputs)
        {
            if (paths.Contains(output.Template))
            {
                throw new ArgumentException("Templates must not be overwritten by input or outputs.");
            }
        }
        return new Options(input, outputs);
    }

    private static void Generate(Options options)
    {
        var input = options.Input;
        var original = File.ReadAllText(input, Encoding.UTF8);
        var document = XDocument.Parse(original, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        var root = document.Root;
        if (root == null || root.Name != "gameplayTags")
        {
            throw new ArgumentException("Expected a gameplayTags root element.");
        }
        ValidateAttributes(root, "nextId");
        var nodes = new List<TagNode>();
        var ids = new HashSet<int>();
        var roots = ReadChildren(root, null, nodes, ids);
        var nextIdAttribute = root.Attribute("nextId");
        var nextId = nextIdAttribute == null ? 1L : ParsePositive(nextIdAttribute.Value, int.MaxValue + 1L, "nextId");
        foreach (var node in nodes)
        {
            nextId = Math.Max(nextId, (long)node.Id + 1);
        }
        var sourceChanged = false;
        foreach (var node in nodes)
        {
            if (node.Id != 0)
            {
                continue;
            }
            if (nextId > int.MaxValue)
            {
                throw new OverflowException("Gameplay tag IDs are exhausted.");
            }
            node.Id = (int)nextId++;
            node.Element.SetAttributeValue("id", node.Id.ToString(CultureInfo.InvariantCulture));
            sourceChanged = true;
        }
        var nextIdText = nextId.ToString(CultureInfo.InvariantCulture);
        if (nextIdAttribute?.Value != nextIdText)
        {
            root.SetAttributeValue("nextId", nextIdText);
            sourceChanged = true;
        }

        var model = CreateModel(roots, nodes, nextId);
        var rendered = new List<string>();
        foreach (var output in options.Outputs)
        {
            var template = Template.Parse(File.ReadAllText(output.Template, Encoding.UTF8), output.Template);
            if (template.HasErrors)
            {
                throw new ArgumentException(template.Messages.ToString());
            }
            // Templates are project-owned; tag volume must not hit Scriban's default 1000-iteration limit.
            var context = new TemplateContext { StrictVariables = true, EnableRelaxedMemberAccess = false, LoopLimit = 0, RecursiveLimit = 0 };
            context.PushGlobal(model);
            var content = template.Render(context).Replace("\r\n", "\n");
            CheckWritable(output.Path, content);
            rendered.Add(content);
        }
        var source = sourceChanged ? SerializeSource(document, original) : original;
        CheckWritable(input, source);
        if (File.ReadAllText(input, Encoding.UTF8) != original)
        {
            throw new IOException("Source changed during generation; rerun against the new source.");
        }
        if (sourceChanged)
        {
            WriteIfChanged(input, source);
        }
        for (var index = 0; index < options.Outputs.Count; ++index)
        {
            WriteIfChanged(options.Outputs[index].Path, rendered[index]);
        }
        Console.WriteLine($"Generated {nodes.Count} gameplay tags. Next ID: {nextIdText}.");
    }

    private static List<TagNode> ReadChildren(XElement element, TagNode? parent, List<TagNode> nodes, HashSet<int> ids)
    {
        var children = new List<TagNode>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var child in element.Elements())
        {
            if (child.Name != "tag")
            {
                throw new ArgumentException($"Unsupported element [{child.Name}] at {Location(child)}.");
            }
            ValidateAttributes(child, "name", "id");
            var name = child.Attribute("name")?.Value ?? "";
            var containingName = parent?.Name ?? "GameplayTags";
            if (!IsIdentifier(name) || ReservedNames.Contains(name) || name == containingName || !names.Add(name))
            {
                throw new ArgumentException($"Invalid, reserved or duplicate tag name [{name}] at {Location(child)}.");
            }
            foreach (var content in child.Nodes())
            {
                if (content is XText text && !string.IsNullOrWhiteSpace(text.Value))
                {
                    throw new ArgumentException($"Tag [{name}] cannot contain text at {Location(child)}.");
                }
            }
            var idAttribute = child.Attribute("id");
            var id = idAttribute == null ? 0 : (int)ParsePositive(idAttribute.Value, int.MaxValue, "tag id");
            if (id != 0 && !ids.Add(id))
            {
                throw new ArgumentException($"Duplicate tag ID [{id}] at {Location(child)}.");
            }
            var node = new TagNode
            {
                Element = child,
                Name = name,
                Path = parent == null ? name : parent.Path + "." + name,
                Parent = parent,
                Id = id
            };
            nodes.Add(node);
            children.Add(node);
            node.Children.AddRange(ReadChildren(child, node, nodes, ids));
        }
        return children;
    }

    private static ScriptObject CreateModel(List<TagNode> roots, List<TagNode> nodes, long nextId)
    {
        var models = new Dictionary<TagNode, ScriptObject>();
        var tags = new ScriptArray();
        foreach (var node in nodes)
        {
            var model = new ScriptObject
            {
                ["name"] = node.Name,
                ["path"] = node.Path,
                ["id"] = node.Id,
                ["parent_id"] = node.Parent?.Id ?? 0,
                ["children"] = new ScriptArray()
            };
            models.Add(node, model);
            tags.Add(model);
        }
        foreach (var node in nodes)
        {
            var children = (ScriptArray)models[node]["children"]!;
            foreach (var child in node.Children)
            {
                children.Add(models[child]);
            }
        }
        var rootModels = new ScriptArray();
        foreach (var root in roots)
        {
            rootModels.Add(models[root]);
        }
        return new ScriptObject { ["tags"] = tags, ["roots"] = rootModels, ["next_id"] = nextId };
    }

    private static bool IsIdentifier(string name)
    {
        if (name.Length == 0 || !(char.IsLetter(name[0]) || name[0] == '_'))
        {
            return false;
        }
        for (var index = 1; index < name.Length; ++index)
        {
            var category = char.GetUnicodeCategory(name[index]);
            if (!(char.IsLetterOrDigit(name[index]) || name[index] == '_' || category == UnicodeCategory.NonSpacingMark || category == UnicodeCategory.SpacingCombiningMark || category == UnicodeCategory.ConnectorPunctuation))
            {
                return false;
            }
        }
        return true;
    }

    private static long ParsePositive(string text, long max, string name)
    {
        if (!long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value <= 0 || value > max)
        {
            throw new ArgumentException($"Invalid {name} [{text}].");
        }
        return value;
    }

    private static void ValidateAttributes(XElement element, params string[] allowed)
    {
        foreach (var attribute in element.Attributes())
        {
            if (Array.IndexOf(allowed, attribute.Name.ToString()) < 0)
            {
                throw new ArgumentException($"Unsupported attribute [{attribute.Name}] at {Location(element)}.");
            }
        }
    }

    private static string Location(XElement element) => $"line {((IXmlLineInfo)element).LineNumber}";

    private static string SerializeSource(XDocument document, string original)
    {
        var text = document.ToString(SaveOptions.DisableFormatting);
        if (document.Declaration != null)
        {
            text = document.Declaration + "\n" + text;
        }
        return original.Contains("\r\n", StringComparison.Ordinal) ? text.Replace("\n", "\r\n") : text;
    }

    private static void CheckWritable(string path, string content)
    {
        if (File.Exists(path) && File.ReadAllText(path, Encoding.UTF8) != content && (File.GetAttributes(path) & FileAttributes.ReadOnly) != 0)
        {
            throw new IOException($"File is read-only: {path}");
        }
    }

    private static void WriteIfChanged(string path, string content)
    {
        if (File.Exists(path) && File.ReadAllText(path, Encoding.UTF8) == content)
        {
            return;
        }
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, content, new UTF8Encoding(false));
            File.Move(temporary, path, true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }
}
