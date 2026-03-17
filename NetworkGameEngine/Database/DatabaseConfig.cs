using System.Xml;

namespace NetworkGameEngine.Database
{
    internal sealed class DatabaseConfig
    {
        public string ServerAddress { get; private init; } = string.Empty;
        public int? ServerIndex { get; private init; }

        internal static DatabaseConfig LoadFromXmlFile(string configPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(configPath);

            XmlDocument document = new();
            document.Load(configPath);

            XmlElement root = document.DocumentElement
                ?? throw new InvalidDataException("Configuration root element is missing.");

            string databaseName = ReadRequiredElementText(root, "database");
            string host = ReadRequiredElementText(root, "address");
            string port = ReadRequiredElementText(root, "port");
            string userName = ReadRequiredElementText(root, "username");
            string password = ReadRequiredElementText(root, "password");
            int serverIndex = ReadOptionalIntElementOrDefault(root, "index", defaultValue: 0);

            return new DatabaseConfig
            {
                ServerAddress = BuildPostgresConnectionString(host, databaseName, port, userName, password),
                ServerIndex = serverIndex
            };
        }

        private static string BuildPostgresConnectionString(
            string host,
            string databaseName,
            string port,
            string userName,
            string password)
        {
            return $"Server={host}; Username={userName}; Database={databaseName}; Port={port}; Password={password}; SSLMode=Prefer; MaxPoolSize=100;";
        }

        private static int ReadOptionalIntElementOrDefault(XmlElement root, string elementName, int defaultValue)
        {
            XmlNode? node = root[elementName];
            return node is not null && int.TryParse(node.InnerText.Trim(), out int value)
                ? value
                : defaultValue;
        }

        private static string ReadRequiredElementText(XmlElement root, string elementName)
        {
            XmlNode? node = root[elementName];
            if (node is null || string.IsNullOrWhiteSpace(node.InnerText))
            {
                throw new InvalidDataException($"Missing required element '{elementName}'.");
            }

            return node.InnerText.Trim();
        }
    }
}
