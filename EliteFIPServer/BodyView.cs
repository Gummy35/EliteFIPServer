using ExoScan;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Navigation;

namespace EliteFIPServer
{
    public class BodyView : INotifyPropertyChanged
    {
        public int BodyId { get; set; }
        public string BodyName { get; set; }
        public string BodyDesc
        {
            get
            {
                var s = BodyName;
                if (BioSignalCount >= 0)
                    s += $" - {BioSignalCount} signal{(BioSignalCount > 1 ? "s" : "")}";
                else
                {
                    s += " - (not scanned)";
                }
                return s;
            }
        }
        public string AtmosphereType { get; set; }
        public string PlanetClass { get; set; }
        public int BioSignalCount { get; set; }
        public ObservableCollection<ScanResultEntry> Scans { get; set; } = new();

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
