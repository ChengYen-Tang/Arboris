// See https://aka.ms/new-console-template for more information

using ClangSharp;
using ClangSharp.Interop;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AstTest;

public class Program
{
    unsafe public static void Main()
    {
        string basePath = @"D:\Downloads\MHM_Dll";
        string[] args =
        [
            //$"-I {Path.Combine(basePath, @"Include\Self")} -I {Path.Combine(basePath, "Include")} -I {Path.Combine(basePath, @"Source\src")} -I {Path.Combine(basePath, @"Source\Soruce_Tool")} -I {Path.Combine(basePath, @"Source\Source_MATLAB_Lib")} -I {Path.Combine(basePath, @"Extlib\rapidjson")}",
            "-std=c++14",
            //"-Xclang",
            //"-ast-dump",
            //"-fsyntax-only",
            $"-I{Path.Combine(basePath, "Include")}",
            $"-I{Path.Combine(basePath, "Include\\Self")}",
            $"-I{Path.Combine(basePath, "Source\\src")}",
            $"-I{Path.Combine(basePath, "Source\\Soruce_Tool")}",
            $"-I{Path.Combine(basePath, "Source\\Source_MATLAB_Lib")}",
            $"-I{Path.Combine(basePath, "Extlib\\rapidjson")}",
        ];

        using CXIndex index = CXIndex.Create(false, false);

        //using CXTranslationUnit translationUnit = CXTranslationUnit.CreateFromSourceFile(index, Path.Combine(basePath, @"Z:\Code\SnapshotAdapter\reference\result\include\result.hpp"), [], []);
        using CXTranslationUnit translationUnit = CXTranslationUnit.CreateFromSourceFile(index, Path.Combine(basePath, @"Source\src\atw_mcalc_MATLAB_MHM_SchedulingProgramming_Main.cpp"), args, []);
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

            PrintNode(cursor, 0);
        }
    }


    public static void PrintNode(Cursor cursor, uint depth)
    {
        if (!cursor.Location.IsFromMainFile)
        {
            return;
        }

        for (uint i = 0; i < depth; ++i)
            Console.Write(" ");


        CXString cXType = cursor.Handle.Type.Spelling;
        Console.WriteLine($"{cursor.CursorKindSpelling} {cursor.Spelling} {cXType}");
        clang.disposeString(cXType);


        foreach (var child in cursor.CursorChildren)
        {
            PrintNode(child, depth + 1);
        }
    }

    unsafe public static void PrintNode(CXCursor cursor, uint depth)
    {
        //CXSourceLocation location = clang.getCursorLocation(cursor);
        //cursor.Location.GetExpansionLocation(out CXFile file, out uint line, out uint column, out uint offset);
        //Console.WriteLine($"{file.Name} {line} {column} {offset}");
        if (clang.Location_isFromMainFile(cursor.Location) == 0)
        {
            return;
        }

        for (uint i = 0; i < depth; ++i)
            Console.Write(" ");

        CXString cursor_kind = clang.getCursorKindSpelling(clang.getCursorKind(cursor));
        CXString cursor_spelling = clang.getCursorSpelling(cursor);
        CXString cXType = clang.getCursorType(cursor).Spelling;

        Console.WriteLine($"{cursor_kind} {cursor_spelling} {cXType}");



        clang.disposeString(cursor_kind);
        clang.disposeString(cursor_spelling);
        clang.disposeString(cXType);

        clang.visitChildren(cursor, &Visitor, &depth);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private unsafe static CXChildVisitResult Visitor(CXCursor c, CXCursor parent, void* client_data)
    {
        uint depth = *(uint*)client_data;
        //Console.WriteLine(depth);
        PrintNode(c, depth + 1);
        return CXChildVisitResult.CXChildVisit_Recurse;
    }
}
