using Npgsql;

// One-off dev utility: applies seed_demo.sql (demo accounts, labs, machines) to any
// Postgres database. Reads the connection string from ConnectionStrings__DefaultConnection
// (the same env name the API uses) or PGCONN.
//
// Usage (from the repo root):
//   $env:ConnectionStrings__DefaultConnection="Host=...;Port=...;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true"
//   dotnet run --project tools/SeedDemo
//
// Optional arg: path to the SQL file (defaults to seed_demo.sql in the current directory).

var connectionString =
    Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? Environment.GetEnvironmentVariable("PGCONN");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Connection string missing. Set ConnectionStrings__DefaultConnection (or PGCONN).");
    Console.Error.WriteLine("Example:");
    Console.Error.WriteLine("  $env:ConnectionStrings__DefaultConnection=\"Host=<host>;Port=<port>;Database=railway;Username=postgres;Password=<password>;SSL Mode=Require;Trust Server Certificate=true\"");
    return 1;
}

var sqlPath = args.Length > 0 ? args[0] : "seed_demo.sql";
if (!File.Exists(sqlPath))
{
    Console.Error.WriteLine($"SQL file not found: {Path.GetFullPath(sqlPath)}");
    Console.Error.WriteLine("Run from the repo root or pass the path: dotnet run --project tools/SeedDemo -- <path>");
    return 1;
}

var sql = await File.ReadAllTextAsync(sqlPath);

await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();
Console.WriteLine("Connected.");

await using (var command = new NpgsqlCommand(sql, connection))
{
    await command.ExecuteNonQueryAsync();
}
Console.WriteLine($"Applied {sqlPath}.");

await using (var check = new NpgsqlCommand(
    "SELECT count(*) FROM customer.customers", connection))
{
    var users = (long)(await check.ExecuteScalarAsync() ?? 0L);
    Console.WriteLine($"customer.customers rows: {users}");
}

await using (var roles = new NpgsqlCommand(
    "SELECT \"Email\", array_to_string(\"Roles\", ',') FROM customer.customers " +
    "WHERE \"Email\" LIKE '%@printgrid.dev' ORDER BY \"Email\"", connection))
await using (var reader = await roles.ExecuteReaderAsync())
{
    Console.WriteLine("Demo accounts:");
    while (await reader.ReadAsync())
        Console.WriteLine($"  {reader.GetString(0)} -> [{reader.GetString(1)}]");
}

return 0;
