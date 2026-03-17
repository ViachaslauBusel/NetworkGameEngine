namespace NetworkGameEngine.Database
{
    public class DatabaseProvider<TDatabase> where TDatabase : Enum
    {
        private readonly Dictionary<(TDatabase Database, int ServerIndex), DatabaseConfig> _databaseConfigs = new();

        public DatabaseRequestBuilder<TDatabase> SelectDatabase(TDatabase database)
        {
            return new DatabaseRequestBuilder<TDatabase>(this, database);
        }

        public void LoadDatabase(TDatabase database, string configPath)
        {
            DatabaseConfig config = DatabaseConfig.LoadFromXmlFile(configPath);
            int serverIndex = config.ServerIndex ?? 0;
            var key = (database, serverIndex);

            if (_databaseConfigs.ContainsKey(key))
            {
                throw new InvalidOperationException(
                    $"Database with type '{database}' and index '{serverIndex}' already exists.");
            }

            _databaseConfigs.Add(key, config);
        }

        internal DatabaseConfig GetConfig(TDatabase database, int serverIndex)
        {
            if (_databaseConfigs.TryGetValue((database, serverIndex), out DatabaseConfig config))
            {
                return config;
            }

            throw new KeyNotFoundException(
                $"Database config not found for type '{database}' and index '{serverIndex}'.");
        }
    }
}