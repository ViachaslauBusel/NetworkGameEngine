using System.Globalization;
using NetworkGameEngine.JobsSystem;
using Newtonsoft.Json;
using Npgsql;

namespace NetworkGameEngine.Database
{
    public sealed class DatabaseRequest<TResult>
    {
        private readonly string _connectionString;
        private readonly string _functionName;
        private readonly IReadOnlyList<object> _arguments;
        private Job<TResult> _executionJob;
        private Task<TResult> _executionTask;
        private Exception _suppressedExecutionException;

        public string FunctionName => _functionName;
        public string ArgumentsDescription => string.Join(", ", _arguments.Select(arg => arg?.ToString() ?? "null"));
        public bool IsCompleted => (_executionJob?.IsCompleted ?? false) || (_executionTask?.IsCompleted ?? false);
        public bool IsFaulted => (_executionJob?.IsFaulted ?? false) || (_executionTask?.IsFaulted ?? false) || _suppressedExecutionException != null;
        public Exception Exception => _executionJob?.Exception ?? _executionTask?.Exception?.GetBaseException()
                                                               ?? _suppressedExecutionException;

        public TResult Result =>
            _executionJob is not null
                ? _executionJob.Result
                : _executionTask is not null
                    ? _executionTask.GetAwaiter().GetResult()
                    : throw new InvalidOperationException("Request has not been started. Call ExecuteAsJob() or ExecuteAsTask().");

        internal DatabaseRequest(string serverAddress, string functionName, IReadOnlyList<object> arguments)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(serverAddress);
            ArgumentException.ThrowIfNullOrWhiteSpace(functionName);

            _connectionString = serverAddress;
            _functionName = functionName;
            _arguments = arguments ?? Array.Empty<object>();
        }

        public Job<TResult> ExecuteAsJob(bool throwException = true)
        {
            if (_executionTask != null)
            {
                throw new InvalidOperationException(
                    "This request is already being executed as a Task. Cannot execute as Job.");
            }

            if (_executionJob != null)
            {
                return _executionJob;
            }

            _executionJob = Job.Run(() =>
            {
                try
                {
                    return Execute();
                }
                catch (Exception ex) when (!throwException)
                {
                    _suppressedExecutionException = ex;
                    return default;
                }
            });

            return _executionJob;
        }

        public Task<TResult> ExecuteAsTask(bool throwException = true)
        {
            if (_executionJob != null)
            {
                throw new InvalidOperationException(
                    "This request is already being executed as a Job. Cannot execute as Task.");
            }
            if (_executionTask != null)
            {
                return _executionTask;
            }
            _executionTask = Task.Run(() =>
            {
                try
                {
                    return Execute();
                }
                catch (Exception ex) when (!throwException)
                {
                    _suppressedExecutionException = ex;
                    return default;
                }
            });

            return _executionTask;
        }

        public TResult Execute()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var command = BuildSqlCommand(connection);
            object scalarValue = command.ExecuteScalar();

            return ConvertScalarToResult(scalarValue);
        }

        private NpgsqlCommand BuildSqlCommand(NpgsqlConnection connection)
        {
            string[] argumentPlaceholders = new string[_arguments.Count];
            var command = new NpgsqlCommand { Connection = connection };

            for (int i = 0; i < _arguments.Count; i++)
            {
                string parameterName = $"p{i}";
                argumentPlaceholders[i] = $"@{parameterName}";
                command.Parameters.AddWithValue(parameterName, _arguments[i] ?? DBNull.Value);
            }

            command.CommandText = $"SELECT \"{_functionName}\"({string.Join(", ", argumentPlaceholders)});";
            return command;
        }

        private static TResult ConvertScalarToResult(object scalarValue)
        {
            if (scalarValue is null || scalarValue == DBNull.Value)
            {
                return default;
            }

            if (scalarValue is TResult typedValue)
            {
                return typedValue;
            }

            if (scalarValue is string rawText)
            {
                if (typeof(TResult) == typeof(string))
                {
                    return (TResult)(object)rawText;
                }

                TResult parsed = JsonConvert.DeserializeObject<TResult>(rawText);
                if (parsed is null)
                {
                    throw new InvalidOperationException(
                        $"Failed to deserialize database payload to '{typeof(TResult).Name}'.");
                }

                return parsed;
            }

            if (scalarValue is IConvertible)
            {
                return (TResult)Convert.ChangeType(scalarValue, typeof(TResult), CultureInfo.InvariantCulture);
            }

            throw new InvalidCastException(
                $"Cannot convert database result type '{scalarValue.GetType().Name}' to '{typeof(TResult).Name}'.");
        }
    }
}