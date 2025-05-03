using Autofac;
using Client.Model;
using Client.Model.Crypto;
using Client.Service;
using Client.Service.Abstract;
using Client.View.Window;
using Client.ViewModel;
using Client.ViewModel.Crypto;
using ProjectZeroLib.Enums;
using Serilog;
using System.Windows;

namespace Client
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            LoggerConfig.Configure();
            var builder = new ContainerBuilder();
            builder.RegisterInstance(Log.Logger).As<ILogger>().SingleInstance();

            builder.RegisterType<TcpConnector>().As<Connector>().SingleInstance();

            builder.RegisterType<AuthModel>().SingleInstance();
            builder.RegisterType<AuthViewModel>().SingleInstance();
            builder.RegisterType<AuthView>().AsSelf();

            builder.RegisterType<OkxModel>().AsSelf().SingleInstance().WithParameter("name", BurseName.Okx);
            builder.RegisterType<OkxViewModel>().AsSelf().SingleInstance();

            builder.RegisterType<BinanceModel>().AsSelf().SingleInstance().WithParameter("name", BurseName.Binance);
            builder.RegisterType<BinanceViewModel>().AsSelf().SingleInstance();

            builder.RegisterType<BybitModel>().AsSelf().SingleInstance().WithParameter("name", BurseName.Bybit);
            builder.RegisterType<BybitViewModel>().AsSelf().SingleInstance();

            builder.RegisterType<QuikModel>().AsSelf().SingleInstance().WithParameter("name", BurseName.Quik);
            builder.RegisterType<QuikViewModel>().AsSelf().SingleInstance();

            builder.RegisterType<MultiCryptoModel>().AsSelf().SingleInstance().WithParameter("name", BurseName.Multi); ;
            builder.RegisterType<MultiCryptoViewModel>().AsSelf().SingleInstance();

            builder.RegisterType<MainModel>().AsSelf().SingleInstance();
            builder.RegisterType<MainViewModel>().AsSelf().SingleInstance();
            builder.RegisterType<MainView>().AsSelf();

            builder.RegisterType<SubscriptionsRepository>().AsSelf().SingleInstance();

            builder.RegisterType<SettingsModel>().AsSelf().SingleInstance();
            builder.RegisterType<SettingsViewModel>().AsSelf().SingleInstance();

            builder.RegisterType<CryptosViewModel>().AsSelf().SingleInstance();

            var container = builder.Build();

            var mainvm = container.Resolve<MainViewModel>();
            var mainWindow = container.Resolve<MainView>();
            mainWindow.DataContext = mainvm;

            var authvm = container.Resolve<AuthViewModel>();
            var authWindow = container.Resolve<AuthView>();
            authWindow.DataContext = authvm;

            authvm.Authenticated += (sender, args) =>
            {
                authWindow.Close();
                mainWindow.ShowDialog();
            };
            authvm.CloseRequested += (sender, args) =>
            {
                authWindow.Close();
                Current.Shutdown();
            };

            authWindow.ShowDialog();
        }
    }
}
