#r "nuget: Npgsql, 9.0.3"
using Npgsql;

var cs = "Host=127.0.0.1;Port=5433;Database=convy;Username=convy;Password=UYpNGP6sBo8bmqt7yKLkTeRJ";
var conn = new NpgsqlConnection(cs);
conn.Open();

Console.WriteLine("=== USERS (last 5) ===");
using (var cmd = new NpgsqlCommand("SELECT id, firebase_uid, display_name, email, created_at FROM users ORDER BY created_at DESC LIMIT 5", conn))
using (var r = cmd.ExecuteReader())
{
    while (r.Read())
        Console.WriteLine($"{r[0]} | {r[1]} | {r[2]} | {r[3]} | {r[4]}");
}

Console.WriteLine("\n=== HOUSEHOLDS ===");
using (var cmd = new NpgsqlCommand("SELECT id, name, created_by, created_at FROM households ORDER BY created_at DESC LIMIT 5", conn))
using (var r = cmd.ExecuteReader())
{
    while (r.Read())
        Console.WriteLine($"{r[0]} | {r[1]} | {r[2]} | {r[3]}");
}

Console.WriteLine("\n=== HOUSEHOLD MEMBERSHIPS ===");
using (var cmd = new NpgsqlCommand("SELECT hm.id, hm.household_id, hm.user_id, hm.role, hm.joined_at, u.email FROM household_memberships hm JOIN users u ON u.id = hm.user_id ORDER BY hm.joined_at DESC LIMIT 10", conn))
using (var r = cmd.ExecuteReader())
{
    while (r.Read())
        Console.WriteLine($"{r[0]} | {r[1]} | {r[2]} | {r[3]} | {r[4]} | {r[5]}");
}

Console.WriteLine("\n=== INVITES ===");
using (var cmd = new NpgsqlCommand("SELECT id, household_id, code, created_by, expires_at, used_at, used_by, revoked_at FROM invites ORDER BY created_at DESC LIMIT 5", conn))
using (var r = cmd.ExecuteReader())
{
    while (r.Read())
        Console.WriteLine($"{r[0]} | {r[1]} | {r[2]} | {r[3]} | {r[4]} | {r[5]} | {r[6]} | {r[7]}");
}
