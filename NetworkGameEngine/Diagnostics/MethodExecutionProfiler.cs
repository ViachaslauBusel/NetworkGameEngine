using System.Diagnostics;

namespace NetworkGameEngine.Diagnostics
{
    public class SampleData
    {
        private readonly MethodType _methodType;
        private readonly int _maxSamples;
        private readonly List<long> _samples = new List<long>();

        public MethodType MethodType => _methodType;
        public long MinTime => _samples.Count > 0 ? _samples.Min() : 0;
        public long MaxTime => _samples.Count > 0 ? _samples.Max() : 0;
        public double AverageTime => _samples.Count > 0 ? _samples.Average() : 0;

        public SampleData(MethodType methodType, int maxSamples)
        {
            _methodType = methodType;
            _maxSamples = maxSamples;
        }

        internal void AddMeasure(long elapsedMilliseconds)
        {
            if (_samples.Count >= _maxSamples)
            {
                _samples.RemoveAt(0);
            }
            _samples.Add(elapsedMilliseconds);
        }
    }
    public class MethodExecutionProfiler
    {
        private int maxSampleCount;
        private Stopwatch _stopwatch = new Stopwatch();
        private volatile MethodType _currentMethod = MethodType.None;
        private Dictionary<MethodType, SampleData> _methodSamples = new Dictionary<MethodType, SampleData>();

        public MethodExecutionProfiler(int maxSamples)
        {
            maxSampleCount = maxSamples;
        }

        internal void StartMethodProfiling(MethodType prepare)
        {
             _currentMethod = prepare;
            _stopwatch.Restart();
        }

        internal void StopMethodProfiling(MethodType prepare)
        {
            if (_currentMethod == prepare)
            {
                _stopwatch.Stop();
                if (!_methodSamples.ContainsKey(prepare))
                {
                    _methodSamples[prepare] = new SampleData(prepare, maxSampleCount);
                }
                _methodSamples[prepare].AddMeasure(_stopwatch.ElapsedMilliseconds);
                _currentMethod = MethodType.None;
            }
        }

        public long GetMinTime(MethodType method)
        {
            if (_methodSamples.ContainsKey(method))
            {
                return _methodSamples[method].MinTime;
            }
            return 0;
        }

        public long GetMaxTime(MethodType method)
        {
            if (_methodSamples.ContainsKey(method))
            {
                return _methodSamples[method].MaxTime;
            }
            return 0;
        }

        public double GetAverageTime(MethodType method)
        {
            if (_methodSamples.ContainsKey(method))
            {
                return _methodSamples[method].AverageTime;
            }
            return 0;
        }
    }
}
