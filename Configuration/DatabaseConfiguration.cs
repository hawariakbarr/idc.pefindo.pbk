namespace idc.pefindo.pbk.Configuration
{
    // <summary>
    /// Database configuration model for multi-database support
    /// </summary>
    public class DatabaseConfiguration
    {
        public Dictionary<string, string> Names { get; set; } = new();
        public Dictionary<string, string> ConnectionStrings { get; set; } = new();

        /// <summary>
        /// Validates the configuration at startup
        /// </summary>
        public void Validate()
        {
            if (!Names.Any())
                throw new InvalidOperationException("DatabaseConfiguration.Names cannot be empty");

            if (!ConnectionStrings.Any())
                throw new InvalidOperationException("DatabaseConfiguration.ConnectionStrings cannot be empty");

            // Validate that all named databases have connection strings
            foreach (var name in Names.Keys)
            {
                if (!ConnectionStrings.ContainsKey(name))
                    throw new InvalidOperationException($"Missing connection string for database '{name}'");
            }
        }

        /// <summary>
        /// Gets database name by key (e.g., "idccore" -> "idc.core")
        /// </summary>
        public string GetDatabaseName(string key)
        {
            if (!Names.TryGetValue(key, out var databaseName))
                throw new InvalidOperationException($"Database name not found for key: {key}");
            return databaseName;
        }

        /// <summary>
        /// Gets connection string by key
        /// </summary>
        public string GetConnectionString(string key)
        {
            if (!ConnectionStrings.TryGetValue(key, out var connectionString))
                throw new InvalidOperationException($"Connection string not found for key: {key}");
            return connectionString;
        }
    }

}