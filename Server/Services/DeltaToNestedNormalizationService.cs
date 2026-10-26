using System.Text.Json;
using System.Text.Json.Serialization;

namespace Server.Services;

public static class DeltaToNestedNormalizationService
{
    // Rules (Allowed HTML elements and attributes)
    //
    //
    //
    // Structure:
    // { element: string, text: string?, attributes: Dictionary<string, string>, children: Array<this> }
    //

    private static readonly JsonSerializerOptions options = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(
            System.Text.Unicode.UnicodeRanges.BasicLatin,
            System.Text.Unicode.UnicodeRanges.Latin1Supplement,
            System.Text.Unicode.UnicodeRanges.LatinExtendedA
        )
    };

    public static string Normalize(string deltaJSON)
    {
        Console.WriteLine(deltaJSON);

        QuillDelta? deltaObjects = DeSerialize(deltaJSON);
        if (deltaObjects is null) return string.Empty;

        // Console.WriteLine("DeltaObjects");
        // Dump(deltaObjects);
        // Console.WriteLine("SplitIntoLines:");
        List<QuillLine> lines = SplitIntoLines(deltaObjects.Ops);
        // Dump(lines);
        // Console.WriteLine("ReConstructed:");
        List<PageLine> reConstructedLines = ReConstruct(lines);
        // Dump(reConstructedLines);
        // Console.WriteLine("CreateLists:");
        List<PageLine> linesWithLists = CreateLists(reConstructedLines);
        // Dump(linesWithLists);
        // Console.WriteLine("RemoveBrElements:");
        List<PageLine> linesWithoutBr = RemoveBrElements(linesWithLists);
        // Dump(linesWithoutBr);
        // Console.WriteLine("TranslateAllCSSAbleAttributes:");
        List<PageLine> translatedCssAttributes = TranslateAttributesToCSS(linesWithoutBr);
        // Dump(translatedCssAttributes);
        // Console.WriteLine("ClearElementAttributes:");
        // Dump(ClearAttributes(translatedCssAttributes));
        return Serialize(ClearAttributes(translatedCssAttributes));
    }

    public static bool IsCorrectlyNormalized(string json)
    {
        List<PageLine>? lines = JsonSerializer.Deserialize<List<PageLine>>(json, options);
        return lines is not null;
    }

    public static bool IsNotNullOrEmpty(string json)
    {
        List<PageLine>? lines = JsonSerializer.Deserialize<List<PageLine>>(json, options);
        return lines is not null && lines.Count > 0;
    }

    private static List<PageLine> TranslateAttributesToCSS(List<PageLine> lines)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Attributes.Count == 0 && lines[i].ChildLines.Count == 0)
                continue;

            lines[i].Attributes = TryTranslateAttributesToCSSRules(lines[i].Attributes);
            if (lines[i].ChildLines.Count >= 0)
                _ = TranslateAttributesToCSS(lines[i].ChildLines);
        }

        return lines;
    }

    private static List<PageLine> RemoveBrElements(List<PageLine> lines)
    {
        List<PageLine> copyLines = lines.Select(line => new PageLine
        {
            Element = line.Element,
            Text = line.Text,
            Attributes = line.Attributes,
            ChildLines = line.ChildLines
        }).ToList();

        Dictionary<int, List<string>> brGroups = FindBrElements(copyLines);
        if (brGroups.Count == 0) return lines;
        List<PageLine> transformedLines = [];

        // Adds a margin-bottom css rule on the element that comes before a br group, the value is dependant on the quantity of br elements in that group.
        for (int i = 0; i < copyLines.Count; i++)
        {
            if (brGroups.TryGetValue(i, out List<string>? group)) // Find the position of a br group. 
            {
                if (i - 1 < 0) // Filter out br group at the beginning of a doc.
                    continue;
                if (TryAddCSS(copyLines[i - 1].Attributes, $"margin-bottom: {group.Count} rem", out Dictionary<string, JsonElement> updatedAttributes))
                    copyLines[i - 1].Attributes = updatedAttributes;
            }
        }

        // Filters the br elements.
        for (int i = 0; i < copyLines.Count; i++)
        {
            if (string.Equals(copyLines[i].Element, "br")) // Filter all br elements from the lines.
                continue;
            transformedLines.Add(copyLines[i]);
        }

        return transformedLines;
    }

    private static Dictionary<int, List<string>> FindBrElements(List<PageLine> lines)
    {
        Dictionary<int, List<string>> brGroups = [];

        for (int i = 0; i < lines.Count; i++)
        {
            if (!string.Equals(lines[i].Element, "br"))
                continue;

            List<string> group = [];
            int j = i;
            while (j < lines.Count)
            {
                group.Add(lines[j].Element);
                j++;

                if (j == lines.Count || !string.Equals(lines[j].Element, "br"))
                    break;
            }

            if (brGroups.TryAdd(i, group))
                i = j;
        }

        return brGroups;
    }

    private static List<PageLine> CreateLists(List<PageLine> lines)
    {
        List<PageLine> copyLines = lines.Select(line => new PageLine
        {
            Element = line.Element,
            Text = line.Text,
            Attributes = line.Attributes,
            ChildLines = line.ChildLines
        }).ToList();

        Dictionary<int, List<PageLine>> lists = FindLists(copyLines);

        if (lists.Count == 0) return lines;
        List<PageLine> transformedLines = [];

        for (int i = 0; i < lines.Count; i++)
        {
            if (!lists.TryGetValue(i, out List<PageLine>? list) || list.Count < 1)
            {
                if (lines[i].Attributes.TryGetValue("list", out _))
                    continue;
                transformedLines.Add(lines[i]);
                continue;
            }

            if (!list[0].Attributes.TryGetValue("list", out JsonElement listType))
                continue;

            // Setting the checked attribute on CheckLists.
            if (string.Equals(listType.GetString(), "checked") || string.Equals(listType.GetString(), "unchecked"))
            {
                for (int j = 0; j < list.Count; j++)
                {
                    if (list[j].Attributes.TryGetValue("list", out JsonElement type))
                    {
                        string isCheckedJson = string.Equals(type.GetString(), "checked") ? "true" : "false";
                        using JsonDocument doc = JsonDocument.Parse(isCheckedJson);
                        list[j].Attributes.TryAdd("checked", doc.RootElement.Clone());
                    }
                }
            }

            transformedLines.Add(new PageLine
            {
                Element = listType.GetString() switch
                {
                    "ordered" => "ol",
                    "bullet" => "ul",
                    "checked" => "cl",
                    "unchecked" => "cl",
                    _ => "unsupported list"
                },
                Text = null,
                Attributes = [],
                ChildLines = list
            });
        }

        return transformedLines;
    }

    private static Dictionary<int, List<PageLine>> FindLists(List<PageLine> lines)
    {
        Dictionary<int, List<PageLine>> lists = [];
        for (int i = 0; i < lines.Count; i++)
        {
            if (!(lines[i].Element.Equals("li") || lines[i].Element.Equals("cli")) || !lines[i].Attributes.TryGetValue("list", out JsonElement primaryListItem))
                continue;

            List<PageLine> list = [];
            int j = i;

            while (j < lines.Count)
            {
                list.Add(lines[j]);
                j++;

                if (j == lines.Count || !(lines[j].Element.Equals("li") || lines[j].Element.Equals("cli") || lines[j].Attributes.TryGetValue("list", out JsonElement secondaryListItem) && primaryListItem.GetString() != secondaryListItem.GetString()))
                    break;
            }

            if (lists.TryAdd(i, list))
                i = j;
        }

        return lists;
    }

    private static List<PageLine> ClearAttributes(List<PageLine> lines)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Attributes.Count == 0 && lines[i].ChildLines.Count == 0)
                continue;

            string[] attributesToRemove = ["header", "blockquote", "list", "bold", "align", "strike", "underline", "color", "background"];
            foreach (string attr in attributesToRemove)
            {
                lines[i].Attributes.Remove(attr);
            }

            if (lines[i].ChildLines.Count >= 0)
                ClearAttributes(lines[i].ChildLines);
        }

        return lines;
    }

    private static List<PageLine> ReConstruct(List<QuillLine> lines)
    {
        List<PageLine> pageLines = [];

        for (int i = 0; i < lines.Count; i++)
        {
            pageLines.Add(TransformLine(lines[i]));
        }

        return pageLines;
    }

    private static PageLine TransformLine(QuillLine line)
    {
        int opsSize = line.Ops.Count;
        Dictionary<string, JsonElement>? lineAttributes = line.Attributes;

        if (opsSize == 0)
            return new()
            {
                Element = "br",
                Text = null,
                Attributes = []
            };

        if (lineAttributes is null)
        {
            if (opsSize > 0)
            {
                if (opsSize == 1)
                    return new()
                    {
                        Element = GetElementFromAttributes(lineAttributes),
                        Text = line.Ops[0].Insert,
                        Attributes = line.Ops[0].Attributes ?? [],
                        ChildLines = []
                    };

                PageLine parentLine = new()
                {
                    Element = GetElementFromAttributes(lineAttributes),
                    Text = null,
                    Attributes = lineAttributes ?? [],
                    ChildLines = []
                };

                for (int i = 0; i < opsSize; i++)
                {
                    parentLine.ChildLines.Add(new()
                    {
                        Element = GetElementFromAttributes(line.Ops[i].Attributes, true),
                        Text = line.Ops[i].Insert,
                        Attributes = line.Ops[i].Attributes ?? [],
                        ChildLines = []
                    });
                }
                return parentLine;
            }
        }
        else
        {
            if (opsSize > 0)
            {
                if (opsSize == 1)
                    return new()
                    {
                        Element = GetElementFromAttributes(lineAttributes),
                        Text = line.Ops[0].Insert,
                        Attributes = lineAttributes.Concat(line.Ops[0].Attributes ?? []).GroupBy(x => x.Key).ToDictionary(g => g.Key, g => g.First().Value),
                        ChildLines = []
                    };

                PageLine parentLine = new()
                {
                    Element = GetElementFromAttributes(lineAttributes),
                    Text = null,
                    Attributes = lineAttributes ?? [],
                    ChildLines = []
                };

                for (int i = 0; i < opsSize; i++)
                {
                    parentLine.ChildLines.Add(new()
                    {
                        Element = GetElementFromAttributes(line.Ops[i].Attributes, true),
                        Text = line.Ops[i].Insert,
                        Attributes = line.Ops[i].Attributes ?? [],
                        ChildLines = []
                    });
                }

                return parentLine;
            }
        }

        return new()
        {
            Element = "nullElement",
            Text = null,
            Attributes = [],
            ChildLines = []
        };
    }

    private static string GetElementFromAttributes(Dictionary<string, JsonElement>? attributes, bool isChild = false)
    {
        if (attributes is null)
            return isChild ? "span" : "p";

        if (attributes.TryGetValue("header", out JsonElement header))
            return header.GetInt32() switch
            {
                1 => "h1",
                2 => "h2",
                3 => "h3",
                4 => "h4",
                _ => "unsupported header"
            };

        if (attributes.TryGetValue("list", out JsonElement listItem))
            return listItem.GetString() switch
            {
                "ordered" => "li",
                "bullet" => "li",
                "checked" => "cli",
                "unchecked" => "cli",
                _ => "unsupported listItem"
            };

        if (attributes.TryGetValue("blockquote", out JsonElement blockquote) && blockquote.GetBoolean())
            return "blockquote";

        if (attributes.TryGetValue("background", out _))
            return isChild ? "span" : "p";

        if (attributes.TryGetValue("color", out _))
            return isChild ? "span" : "p";

        if (attributes.TryGetValue("strike", out _))
            return isChild ? "span" : "p";

        (bool isItalic, bool isBold, bool isUnderlined) = (false, false, false);

        if (attributes.TryGetValue("italic", out JsonElement italic))
            isItalic = italic.GetBoolean();

        if (attributes.TryGetValue("bold", out JsonElement bold))
            isBold = bold.GetBoolean();

        if (attributes.TryGetValue("underline", out JsonElement underline))
            isUnderlined = underline.GetBoolean();

        if ((isItalic || isUnderlined) && !isBold)
            return "span";

        if (isBold)
            return "strong";

        return "p";
    }

    private static List<QuillLine> SplitIntoLines(List<QuillOp> ops)
    {
        List<QuillLine> lines = [];
        QuillLine current = new();

        foreach (QuillOp op in ops)
        {
            if (op.Insert is null) continue;

            string[] parts = op.Insert.Split("\n");

            for (int i = 0; i < parts.Length; i++)
            {
                string text = parts[i];

                if (text.Length > 0)
                {
                    current.Ops.Add(new()
                    {
                        Insert = text,
                        Attributes = op.Attributes
                    });
                }

                // There is a newline after this part.
                if (i < parts.Length - 1)
                {
                    current.Attributes = op.Attributes;

                    lines.Add(current);
                    current = new();
                }
            }
        }

        if (current.Ops.Count > 0)
            lines.Add(current);

        return lines;
    }

    private static Dictionary<string, JsonElement> TryTranslateAttributesToCSSRules(Dictionary<string, JsonElement> attributes)
    {
        Dictionary<string, JsonElement> attributesCopy = attributes.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value
        );

        string[] attrKeys = ["align", "color", "background", "strike", "underline"];
        List<bool> succeededTranslations = [];
        Dictionary<string, JsonElement> foundCSSAbleAttributes = [];

        foreach (string key in attrKeys)
        {
            if (!attributesCopy.TryGetValue(key, out JsonElement val))
                continue;

            string keyVal = $"{key}-{val}";
            Console.WriteLine(keyVal);
            Dictionary<string, string> mapper = new()
            {
                { "aling-left", "margin-right: auto" },
                { "align-center", "margin: auto" },
                { "align-right", "margin-left: auto" },
                { "strike-True", "text-decoration: line-through" },
                { "underline-True", "text-decoration: underline" }
            };

            string? cssRule = string.Empty;
            if (!mapper.TryGetValue(keyVal, out cssRule))
            {
                if (keyVal.Contains("color"))
                    cssRule = $"color: {val}";

                if (keyVal.Contains("background"))
                    cssRule = $"background-color: {val}";
            }

            if (string.IsNullOrEmpty(cssRule))
                continue;

            if (TryAddCSS(attributesCopy, cssRule, out Dictionary<string, JsonElement> updatedAttributes))
            {
                attributesCopy = updatedAttributes;
                succeededTranslations.Add(true);
            }
            else
            {
                succeededTranslations.Add(false);
            }
        }

        if (!succeededTranslations.Contains(false))
        {
            return attributesCopy.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value
            );
        }
        else
        {
            return attributes;
        }
    }

    private static bool TryAddCSS(Dictionary<string, JsonElement> attributes, string newRule, out Dictionary<string, JsonElement> updatedAttributes)
    {
        Dictionary<string, JsonElement> attributesCopy = attributes.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value
        );

        string? nextCss = "";
        if (attributesCopy.TryGetValue("css", out JsonElement css))
            nextCss = css.GetString();

        nextCss = $"\"{nextCss} {newRule}\"";
        attributesCopy.Remove("css");

        using JsonDocument doc = JsonDocument.Parse(nextCss);
        if (attributesCopy.TryAdd("css", doc.RootElement.Clone()))
        {
            updatedAttributes = attributesCopy.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value
            );
            return true;
        }
        else
        {
            updatedAttributes = attributes;
            return false;
        }
    }

    private static QuillDelta? DeSerialize(string deltaJSON)
    {
        return JsonSerializer.Deserialize<QuillDelta>(deltaJSON);
    }

    private static string Serialize(List<PageLine> lines)
    {
        return JsonSerializer.Serialize(lines, options);
    }


    private static void Dump<T>(T value)
    {
        Console.WriteLine(
            JsonSerializer.Serialize(
                value,
                options
            )
        );
    }

    private record PageLine
    {
        public string Element { get; set; } = string.Empty;
        public string? Text { get; set; }
        public Dictionary<string, JsonElement> Attributes { get; set; } = [];
        public List<PageLine> ChildLines { get; set; } = [];
    }

    private record QuillDelta
    {
        [JsonPropertyName("ops")]
        public List<QuillOp> Ops { get; set; } = [];
    }

    private record QuillLine
    {
        public List<QuillOp> Ops { get; set; } = [];
        public Dictionary<string, JsonElement>? Attributes { get; set; } = [];
    }

    private record QuillOp
    {
        [JsonPropertyName("insert")]
        public string? Insert { get; set; }
        [JsonPropertyName("attributes")]
        public Dictionary<string, JsonElement>? Attributes { get; set; }
    }
}
