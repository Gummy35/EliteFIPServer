using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EliteFIPServer
{
    public class ExoViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ExobiologyData> Systems { get; } = new();

        private ExobiologyData _selectedSystem;
        public ExobiologyData SelectedSystem
        {
            get => _selectedSystem;
            set
            {
                _selectedSystem = value;
                UpdateBodies();
                OnPropertyChanged();
            }
        }

        public ObservableCollection<BodyView> Bodies { get; } = new();

        private void UpdateBodies()
        {
            Bodies.Clear();
            if (SelectedSystem == null) return;

            foreach (var kv in SelectedSystem.Scans)
            {
                var firstData = kv.Value.FirstOrDefault();
                if (firstData != null)
                {
                    var body = new BodyView
                    {
                        BodyId = kv.Key,
                        BodyName = firstData.BodyName,
                        AtmosphereType = firstData.AtmosphereType,
                        PlanetClass = firstData.PlanetClass,
                        BioSignalCount = firstData.BioSignalCount,
                        Scans = new ObservableCollection<ExoScan.ScanResultEntry>(kv.Value)
                    };
                    Bodies.Add(body);
                }
            }
        }

        public void RefreshSelectedSystem()
        {
            if (SelectedSystem != null)
                UpdateBodies();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        internal void SetSystems(List<ExobiologyData> systems)
        {
            Systems.Clear();
            var tmp = systems.OrderBy(s => ExobiologyData.ReferencePosition.GetDistance(s.Position));
            foreach (var item in tmp)
            {
                Systems.Add(item);
            }
        }
    }
}
