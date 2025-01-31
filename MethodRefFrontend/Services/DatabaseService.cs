using System;
using Microsoft.Data.Sqlite;
using Dapper;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MethodRefFrontend.Services;

public sealed class MethodRef(
    long id,
    string methodName,
    string returnType,
    string fileName,
    long textSpanStart,
    long textSpanEnd)
{
    public static MethodRef FromMethodReference(MethodDeclarationSyntax methodReference, string fileName) =>
        new (0, methodReference.Identifier.Text, methodReference.ReturnType.GetText().ToString(), fileName, methodReference.Span.Start, methodReference.Span.Length);

    public string MethodHeader => $"{ReturnType} {MethodName}";
    public long Id { get; init; } = id;
    public string MethodName { get; init; } = methodName;
    public string ReturnType { get; init; } = returnType;
    public string FileName { get; init; } = fileName;
    public long TextSpanStart { get; init; } = textSpanStart;
    public long TextSpanEnd { get; init; } = textSpanEnd;

    
}

public class DatabaseService
{
    private readonly string _databasePath;

    public DatabaseService(string databasePath)
    {
        _databasePath = databasePath;
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();

        // Create the MethodRefs table if it doesn't exist
        var command = connection.CreateCommand();
        command.CommandText =
        @"
            CREATE TABLE IF NOT EXISTS MethodRefs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                MethodName TEXT NOT NULL,
                ReturnType TEXT NOT NULL,
                FileName TEXT NOT NULL,
                TextSpanStart INTEGER NOT NULL,
                TextSpanEnd INTEGER NOT NULL
            );
        ";
        command.ExecuteNonQuery();
    }

    public void AddMethodRef(MethodRef methodRef)
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();

        // Use Dapper to insert a new MethodRef
        connection.Execute(
            "INSERT INTO MethodRefs (MethodName, ReturnType, FileName, TextSpanStart, TextSpanEnd) VALUES (@MethodName, @ReturnType, @FileName, @TextSpanStart, @TextSpanEnd)",
            methodRef
        );
    }

    public List<MethodRef> GetAllMethodRefs()
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();

        // Use Dapper to query all MethodRefs
        return connection.Query<MethodRef>("SELECT Id, MethodName, ReturnType, FileName, TextSpanStart, TextSpanEnd FROM MethodRefs").AsList();
    }

    public void UpdateMethodRef(MethodRef methodRef)
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();

        // Use Dapper to update an existing MethodRef
        connection.Execute(
            "UPDATE MethodRefs SET MethodName = @MethodName, ReturnType = @ReturnType, FileName = @FileName, TextSpanStart = @TextSpanStart, TextSpanEnd = @TextSpanEnd WHERE Id = @Id",
            methodRef
        );
    }

    public void DeleteMethodRef(int id)
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();

        // Use Dapper to delete a MethodRef by Id
        connection.Execute("DELETE FROM MethodRefs WHERE Id = @Id", new { Id = id });
    }

    public async Task<List<MethodRef>> Search(string text)
    {
        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();
        text = $"%{text}%";

        var res = await connection.QueryAsync<MethodRef>(
            """
                SELECT Id, MethodName, ReturnType, FileName, TextSpanStart, TextSpanEnd FROM MethodRefs
                WHERE MethodName LIKE @Text OR ReturnType LIKE @Text OR FileName LIKE @Text 
                """
                , new { Text= text});

        return [..res];
    }
}