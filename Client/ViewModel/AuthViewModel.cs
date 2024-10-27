using Client.Model;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using Windows.Media.Ocr;

namespace Client.ViewModel
{
    public class AuthViewModel : ReactiveObject
    {
        private readonly AuthModel _authModel;

        private string _login;
        private string _password;
        private bool _rememberMe;
        private bool _isConnected = false;

        public string Login
        {
            get => _login;
            set => this.RaiseAndSetIfChanged(ref _login, value);
        }
        public string Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value);
        }
        public bool RememberMe
        {
            get => _rememberMe;
            set => this.RaiseAndSetIfChanged(ref _rememberMe, value);
        }
        public bool IsConnected
        {
            get => _isConnected;
            set => this.RaiseAndSetIfChanged(ref _isConnected, value);
        }

        public ReactiveCommand<Unit, Unit> ConnectCommand { get; }

        public AuthViewModel(AuthModel authModel)
        {
            _authModel = authModel;

            Login = _authModel.Login;
            Password = _authModel.Password;
            RememberMe = _authModel.RememberMe;

            this.WhenAnyValue(x => x.Login)
                .Skip(1)
                .Subscribe(value => _authModel.Login = value);
            this.WhenAnyValue(x => x.Password)
                .Skip(1)
                .Subscribe(value => _authModel.Password = value);
            this.WhenAnyValue(x => x.RememberMe)
                .Skip(1)
                .Subscribe(value => _authModel.RememberMe = value);

            ConnectCommand = ReactiveCommand.CreateFromTask(Connect);
        }

        private async Task Connect()
        {
            IsConnected = await _authModel.Authorization();
        }
    }
}
