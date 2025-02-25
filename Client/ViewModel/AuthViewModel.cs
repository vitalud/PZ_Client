using Client.Model;
using ReactiveUI;
using System.Reactive;

namespace Client.ViewModel
{
    public partial class AuthViewModel : ReactiveObject
    {
        private readonly AuthModel _authModel;

        private bool _isConnected = false;
        public bool IsConnected
        {
            get => _isConnected;
            set => this.RaiseAndSetIfChanged(ref _isConnected, value);
        }

        public string Login
        {
            get => _authModel.Login;
            set => _authModel.Login = value;
        }

        public string Password
        {
            get => _authModel.Password;
            set => _authModel.Password = value;
        }

        public bool RememberMe
        {
            get => _authModel.RememberMe;
            set => _authModel.RememberMe = value;
        }

        public ReactiveCommand<Unit, Unit> ConnectCommand { get; }

        public AuthViewModel(AuthModel authModel)
        {
            _authModel = authModel;

            ConnectCommand = ReactiveCommand.CreateFromTask(Connect);
        }

        private async Task Connect()
        {
            IsConnected = await _authModel.Authentication();
        }
    }
}
