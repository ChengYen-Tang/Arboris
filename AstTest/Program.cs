// See https://aka.ms/new-console-template for more information

using Arboris.Models.Code;
using ClangSharp;
using ClangSharp.Interop;
using System.Text.Encodings.Web;
using System.Text.Json;
using Index = ClangSharp.Index;

namespace AstTest;

public class Program
{
    private readonly static HashSet<Function> functions = [];
    private readonly static string[] leftString = ["const "];
    private readonly static string[] rightString = [" &", " *"];

    unsafe public static void Main()
    {
        string basePath = @"D:\Code\MHM_Library_Test\MHM_Dll";
        string[] args =
        [
            "-std=c++14",
            "-xc++",
            $"-I{Path.Combine(basePath, "Include")}",
            $"-I{Path.Combine(basePath, "Include\\Self")}",
            $"-I{Path.Combine(basePath, "Source\\src")}",
            $"-I{Path.Combine(basePath, "Source\\Soruce_Tool")}",
            $"-I{Path.Combine(basePath, "Source\\Source_MATLAB_Lib")}",
            $"-I{Path.Combine(basePath, "Extlib\\rapidjson")}",
        ];

        using Index index = Index.Create(false, true);

        CXTranslationUnit translationUnit = CXTranslationUnit.CreateFromSourceFile(index.Handle, Path.Combine(basePath, @"Source\Source_MATLAB_Lib\datetime.cpp"), args, []);
        using TranslationUnit tu = TranslationUnit.GetOrCreate(translationUnit);

        for (uint i = 0; i < translationUnit.NumDiagnostics; i++)
        {
            CXDiagnostic diagnostic = translationUnit.GetDiagnostic(i);
            using CXString cXString = diagnostic.Spelling;
            Console.WriteLine(cXString.ToString());
        }

        if (translationUnit == null)
        {
            Console.WriteLine("Unable to parse translation unit. Quitting.");
            return;
        }

        foreach (var cursor in tu.TranslationUnitDecl.CursorChildren)
        {
            PrintNode(cursor, 0, null!, 0);
        }


        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        if (File.Exists("functions.json"))
            File.Delete("functions.json");
        File.WriteAllText("functions.json", JsonSerializer.Serialize(functions, options));
    }


    public static void PrintNode(Cursor cursor, uint depth, Function function, uint includeDepth)
    {
        if (!cursor.Location.IsFromMainFile && function is null)
            return;
        //else if (!cursor.Location.IsFromMainFile && function is not null && includeDepth > 5)
        //    return;
        //else if (!cursor.Location.IsFromMainFile && function is not null && includeDepth <= 5)
        //    includeDepth++;

        //if (cursor.Location.IsFromMainFile)
        //    includeDepth = 0;

        cursor.Extent.Start.GetExpansionLocation(out CXFile file, out uint startLine, out uint startColumn, out uint _);
        cursor.Extent.End.GetExpansionLocation(out CXFile _, out uint endLine, out uint endColumn, out uint _);
        Location location = new(file.Name.ToString(), startLine, startColumn, endLine, endColumn);
        for (uint i = 0; i < depth; ++i)
            Console.Write(" ");


        CXString cXType = cursor.Handle.Type.Spelling;
        Console.WriteLine($"{cursor.CursorKindSpelling} {cursor.Spelling} {cXType} (StartLine: {location.StartLine}, StartColumn:{location.StartColumn}, EndLine: {location.EndLine}, EndColumn: {location.EndColumn})");
        if (/*cursor.CursorKindSpelling == "DeclRefExpr" && */cursor.Spelling == "minus")
        {
            Console.WriteLine(cursor.GetType());
        }

        //if (cursor.CursorKindSpelling == "ParmDecl")
        //    return;

        if (cursor.CursorKindSpelling == "FunctionDecl")
        {
            function = new(cursor.Spelling, location, []);
            functions.Add(function);
        }
        //if (function is null)
        //    return;

        //string type = cXType.ToString();
        //if (type != string.Empty)
        //{
        //    foreach (var left in leftString)
        //        type = RemoveLeftString(type, left);
        //    foreach (var right in rightString)
        //        type = RemoveRightString(type, right);
        //    if (!function.Types.ContainsKey(type))
        //        function.Types[type] = [];
        //    function.Types[type].Add(location);
        //}
        clang.disposeString(cXType);

        foreach (var child in cursor.CursorChildren)
        {
            PrintNode(child, depth + 1, function, includeDepth);
        }
    }

    public static string RemoveLeftString(string str, string remove)
    {
        if (str.StartsWith(remove))
            return str[remove.Length..];
        return str;
    }

    public static string RemoveRightString(string str, string remove)
    {
        if (str.EndsWith(remove))
            return str[..^remove.Length];
        return str;
    }
}
