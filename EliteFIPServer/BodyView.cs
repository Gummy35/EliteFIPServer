using ExoScan;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace EliteFIPServer
{
    public class BodyView : INotifyPropertyChanged
    {
        public int BodyId { get; set; }
        public string BodyName { get; set; }
        public string AtmosphereType { get; set; }
        public string PlanetClass { get; set; }
        public ObservableCollection<ScanResultEntry> Scans { get; set; } = new();

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
