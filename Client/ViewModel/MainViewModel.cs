using Client.Model;
using ReactiveUI;
using System.Reactive;

namespace Client.ViewModel
{
    public partial class MainViewModel : ReactiveObject
    {
        private readonly MainModel _mainModel;
        private readonly BursesViewModel _burses;
        private readonly SettingsViewModel _settings;

        private bool _applicationClosing;
        public bool ApplicationClosing
        {
            get => _applicationClosing;
            set => this.RaiseAndSetIfChanged(ref _applicationClosing, value);
        }

        public ReactiveCommand<Unit, Unit> CloseCommand { get; }
        public ReactiveCommand<Unit, ReactiveObject> ShowBursesCommand { get; }
        public ReactiveCommand<Unit, ReactiveObject> ShowSettingsCommand { get; }

        private ReactiveObject _currentViewModel;
        public ReactiveObject CurrentViewModel
        {
            get => _currentViewModel;
            set => this.RaiseAndSetIfChanged(ref _currentViewModel, value);
        }

        public MainViewModel(MainModel mainModel, BursesViewModel burses, SettingsViewModel settings)
        {
            _mainModel = mainModel;
            _burses = burses;
            _settings = settings;

            _currentViewModel = burses;

            CloseCommand = ReactiveCommand.Create(CloseApplication);
            ShowBursesCommand = ReactiveCommand.Create(() => CurrentViewModel = _burses);
            ShowSettingsCommand = ReactiveCommand.Create(() => CurrentViewModel = _settings);
        }
        private void CloseApplication()
        {
            _mainModel.CloseApplication();
            ApplicationClosing = true;
        }
    }
}
