using System.Text.Json.Nodes;

namespace Store.Tests;

public class Tester : IDisposable
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;
    private const string SCHEMA = "store";

    public Tester()
    {
        _dataSource = NpgsqlDataSource.Create("Username=dude;Password=mysecretpassword;Server=127.0.0.1;Database=dude;Port=5432");
        _connection = _dataSource.CreateConnection();
        _transaction = _connection.BeginTransaction();
    }
    
    public async Task<T> QueryFirst<T>(string sql)
    {
        return await _connection.QueryFirstAsync<T>(sql);
    }

    public async Task<Result> ExecuteFunction(string fname, params object[] list)
    {
        var paramArr = list.Select((_, i) => $"@{i}").ToArray();
        var paramStr = String.Join(", ", paramArr);
        var sql = $"select * from {SCHEMA}.{fname}({paramStr})";
        var dynamicParameters = new DynamicParameters();
        for (int i = 0; i < paramArr.Length; i++)
        {
            dynamicParameters.Add(paramArr[i], list[i]);
        }
        return await _connection.QueryFirstAsync<Result>(sql, dynamicParameters);
    }

    public async Task<Result> ExecuteFunction(string fname)
    {
        var sql = $"select * from {SCHEMA}.{fname}()";
        return await _connection.QueryFirstAsync<Result>(sql);
    }

    public async Task Exec(string sql)
    {
        await using var command = _dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        _transaction.Rollback();
    }
}