namespace NetworkGameEngine.Database
{
    public sealed class DatabaseRequestBuilder<TDatabase> : IDatabaseRequestBuilder where TDatabase : Enum
    {
        private readonly DatabaseProvider<TDatabase> _provider;
        private readonly TDatabase _database;
        private int _serverIndex;
        private string _funcName;
        private object[] _args;

        public TDatabase Database { get; }

        internal DatabaseRequestBuilder(DatabaseProvider<TDatabase> provider, TDatabase database)
        {
            _provider = provider;
            _database = database;
            _serverIndex = 0;
        }

        public DatabaseRequestBuilder<TDatabase> WithServerIndex(int serverIndex)
        {
            _serverIndex = serverIndex;
            return this;
        }

        public DatabaseRequestBuilder<TDatabase> WithFunctionName(string funcName)
        {
            _funcName = funcName;
            return this;
        }

        public DatabaseRequestBuilder<TDatabase> WithArguments(params object[] args)
        {
            _args = args;
            return this;
        }

        public DatabaseRequest<TResult> BuildRequest<TResult>()
        {
            string serverAddress = _provider.GetConfig(_database, _serverIndex)?.ServerAddress;

            ArgumentException.ThrowIfNullOrWhiteSpace(_funcName);
            ArgumentException.ThrowIfNullOrWhiteSpace(serverAddress);

            return new DatabaseRequest<TResult>(serverAddress, _funcName, _args);
        }
    }
}