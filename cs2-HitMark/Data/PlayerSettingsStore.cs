using System;
using System.Threading.Tasks;
using CS2_HitMark.Models;
using MySqlConnector;

namespace CS2_HitMark;

public sealed class PlayerSettingsStore
{
    private readonly string _connectionString;
    private readonly string _table;

    public PlayerSettingsStore(Config.MySqlSettings settings)
    {
        var builder = new MySqlConnectionStringBuilder
        {
            Server = settings.Host,
            Port = (uint)settings.Port,
            Database = settings.Database,
            UserID = settings.Username,
            Password = settings.Password,
            Pooling = true,
            SslMode = MySqlSslMode.None
        };

        _connectionString = builder.ConnectionString;
        _table = string.IsNullOrWhiteSpace(settings.Table)
            ? "cs2_hitmark_settings"
            : settings.Table.Trim();
    }

    public async Task InitializeAsync()
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        string sql = $@"
            CREATE TABLE IF NOT EXISTS `{_table}` (
                steam_id BIGINT UNSIGNED NOT NULL PRIMARY KEY,
                hitmark_enabled TINYINT(1) NOT NULL,
                sound_enabled TINYINT(1) NOT NULL,
                updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

        await using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<PlayerSettings?> GetAsync(ulong steamId)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        string sql = $"SELECT hitmark_enabled, sound_enabled FROM `{_table}` WHERE steam_id = @steamId LIMIT 1;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@steamId", steamId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        bool hitmarkEnabled = reader.GetBoolean(0);
        bool soundEnabled = reader.GetBoolean(1);
        return new PlayerSettings(hitmarkEnabled, soundEnabled);
    }

    public async Task UpsertAsync(ulong steamId, bool hitmarkEnabled, bool soundEnabled)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        string sql = $@"
            INSERT INTO `{_table}` (steam_id, hitmark_enabled, sound_enabled)
            VALUES (@steamId, @hitmarkEnabled, @soundEnabled)
            ON DUPLICATE KEY UPDATE
                hitmark_enabled = VALUES(hitmark_enabled),
                sound_enabled = VALUES(sound_enabled);";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@steamId", steamId);
        command.Parameters.AddWithValue("@hitmarkEnabled", hitmarkEnabled);
        command.Parameters.AddWithValue("@soundEnabled", soundEnabled);
        await command.ExecuteNonQueryAsync();
    }
}

public readonly struct PlayerSettings
{
    public PlayerSettings(bool hitmarkEnabled, bool soundEnabled)
    {
        HitMarkEnabled = hitmarkEnabled;
        SoundEnabled = soundEnabled;
    }

    public bool HitMarkEnabled { get; }
    public bool SoundEnabled { get; }
}
