namespace NetworkGameEngine.Generator
{
    internal struct GeneratedSource
    {
        private readonly string _fileName;
        private readonly string _sourceCode;

        public string FileName => _fileName;
        public string SourceCode => _sourceCode;

        public GeneratedSource(string fileName, string sourceCode)
        {
            _fileName = fileName;
            _sourceCode = sourceCode;
        }
    }
}
