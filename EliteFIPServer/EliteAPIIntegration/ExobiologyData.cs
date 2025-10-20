using EliteAPI.Abstractions.Events;
using ExoScan;
using ExoScan.StellarStructs;
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace EliteFIPServer
{
    public class ExobiologyData : IEvent, INotifyPropertyChanged
    {
        public static Position ReferencePosition = new();
        public DateTime Timestamp { get; set; }
        public string Event => "Exobiology";
        public string SystemName { get; set; }
        public Position Position { get; set; } = new();
        public double Distance
        {
            get
            {
                return Math.Round(ReferencePosition.GetDistance(Position), 1);
            }
        }
        public string Desc
        {
            get
            {
                return $"{SystemName} ({Distance:0.0} ly) - {Scans.Count} Scans - {SystemTotalValue().ToString("C0")}";
            }
        }

        public int LastBodyId { get; set; }
        public string LastBodyName { get; set; }

        private Dictionary<int, List<ScanResultEntry>> _scans = new();
        public Dictionary<int, List<ScanResultEntry>> Scans
        {
            get => _scans;
            set
            {
                _scans = value;
                OnPropertyChanged();
            }
        }

        public long SystemTotalValue() => Scans.Sum(kv => kv.Value.Sum(s => s.Value));

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Update(ExoScan.StellarStructs.StarSystem currentSystem, int? bodyId, DateTime timestamp)
        {
            SystemName = currentSystem.Name;
            if (bodyId == null)
                bodyId = currentSystem.LastBodyId;
            var p = currentSystem.Planets.Find(p => p.BodyId == bodyId.Value);
            if (p != null)
            {
                LastBodyId = p.BodyId;
                LastBodyName = p.Name;
            }
            Scans = currentSystem.ScanResults;
            Position = currentSystem.Position.DeepCopy();
            Timestamp = timestamp;
        }
    }
}
