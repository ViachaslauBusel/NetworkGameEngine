namespace NetworkGameEngine.Sync
{
    public class SyncMarkers
    {
        private Dictionary<SyncMarkerType, bool> _markers = new();

        public SyncMarkers()
        {
            foreach (SyncMarkerType type in Enum.GetValues(typeof(SyncMarkerType)))
            {
                _markers[type] = true;
            }
        }

        public bool IsDirty()
        {
            foreach (var marker in _markers)
            {
                if (marker.Value)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsDirty(SyncMarkerType local)
        {
            if (_markers.TryGetValue(local, out var value))
            {
                return value;
            }
            return false;
        }

        public virtual void MakeAllDirty()
        {
            foreach (var marker in _markers)
            {
                _markers[marker.Key] = true;
            }
        }

        public virtual void MarkAsClean(SyncMarkerType local)
        {
            if (_markers.ContainsKey(local))
            {
                _markers[local] = false;
            }
        }

        public virtual bool IsDirtyAndMarkClean(SyncMarkerType type)
        {
            if (_markers.TryGetValue(type, out var isDirty))
            {
                _markers[type] = false;
                return isDirty;
            }
            return false;
        }
    }
}
