using Client.Service.Abstract;
using Client.ViewModel.Burse;
using ReactiveUI;
using System.Collections.ObjectModel;

namespace Client.ViewModel
{
    public partial class BursesViewModel : ReactiveObject
    {
        private readonly BurseViewModel _okxViewModel;
        private readonly BurseViewModel _binanceViewModel;
        private readonly BurseViewModel _bybitViewModel;

        public BurseViewModel OkxViewModel => _okxViewModel;
        public BurseViewModel BinanceViewModel => _binanceViewModel;
        public BurseViewModel BybitViewModel => _bybitViewModel;

        public ObservableCollection<BurseViewModel> BurseViewModels { get; }

        private BurseViewModel _currentBurseViewModel;
        public BurseViewModel CurrentBurseViewModel
        {
            get => _currentBurseViewModel;
            set => this.RaiseAndSetIfChanged(ref _currentBurseViewModel, value);
        }

        public BursesViewModel(OkxViewModel okxViewModel, BinanceViewModel binanceViewModel, BybitViewModel bybitViewModel)
        {
            _okxViewModel = okxViewModel;
            _binanceViewModel = binanceViewModel;
            _bybitViewModel = bybitViewModel;

            _currentBurseViewModel = _okxViewModel;

            BurseViewModels = [_okxViewModel, _binanceViewModel, _bybitViewModel];
        }
    }
}
