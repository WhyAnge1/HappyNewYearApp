using CommunityToolkit.Mvvm.ComponentModel;
using TL;

namespace HappyNewYearApp.Telegram
{
    public partial class Dialog : ObservableObject
    {
        public long Id { get; set; }

        public object Peer { get; set; }

        public string PublicName { get; set; }

        public string? Username { get; set; }

        private bool isSelected;
        public bool IsSelected 
        {
            get => isSelected; 
            set => SetProperty(ref isSelected, value); 
        }

        public byte[] ImageBytes { get; set; }

        public ImageSource Image => ImageSource.FromStream(() => new MemoryStream(ImageBytes));

        public bool IsUser => Peer is User;
    }
}
