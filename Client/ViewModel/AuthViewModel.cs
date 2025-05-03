using Client.Model;
using ReactiveUI;
using System.Reactive;

namespace Client.ViewModel
{
    public partial class AuthViewModel : ReactiveObject
    {
        private readonly AuthModel _authModel;

        private string _login;
        private string _password;
        private bool _rememberMe;
        private bool _isAuthenticated;

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
        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            private set => this.RaiseAndSetIfChanged(ref _isAuthenticated, value);
        }

        public event EventHandler Authenticated = delegate { };
        public event EventHandler CloseRequested = delegate { };

        public ReactiveCommand<Unit, Unit> AuthenticateCommand { get; }
        public ReactiveCommand<Unit, Unit> CloseCommand { get; }

        public AuthViewModel(AuthModel authModel)
        {
            _authModel = authModel;

            _login = AuthModel.LoadLogin();
            _password = AuthModel.LoadPassword();

            AuthenticateCommand = ReactiveCommand.CreateFromTask(Authenticate);
            CloseCommand = ReactiveCommand.Create(OnClose);
        }

        private async Task Authenticate()
        {
            if (await _authModel.Authenticate(Login, Password, RememberMe))
            {
                IsAuthenticated = true;
                Authenticated?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnClose()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
