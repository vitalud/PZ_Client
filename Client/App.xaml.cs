using Autofac;
using Client.Model;
using Client.Model.Burse;
using Client.Service;
using Client.Service.Abstract;
using Client.View.Window;
using Client.ViewModel;
using Client.ViewModel.Burse;
using ProjectZeroLib.Enums;
using ReactiveUI;
using System.Reactive.Linq;
using System.Windows;

namespace Client
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var builder = new ContainerBuilder();

            builder.RegisterType<TcpConnector>().As<Connector>().SingleInstance();

            builder.RegisterType<AuthModel>().SingleInstance();
            builder.RegisterType<AuthViewModel>().SingleInstance();
            builder.RegisterType<AuthView>()
                .OnActivating(eventArgs => eventArgs.Instance.DataContext = eventArgs.Context.Resolve<AuthViewModel>());

            builder.RegisterType<OkxModel>().AsSelf().SingleInstance().WithParameter("name", BurseName.Okx);
            builder.RegisterType<OkxViewModel>().AsSelf().SingleInstance();

            builder.RegisterType<BinanceModel>().AsSelf().SingleInstance().WithParameter("name", BurseName.Binance); ;
            builder.RegisterType<BinanceViewModel>().AsSelf().SingleInstance();

            builder.RegisterType<BybitModel>().AsSelf().SingleInstance().WithParameter("name", BurseName.Bybit); ;
            builder.RegisterType<BybitViewModel>().AsSelf().SingleInstance();

            builder.RegisterType<MainModel>().AsSelf().SingleInstance();
            builder.RegisterType<MainViewModel>().AsSelf().SingleInstance();
            builder.RegisterType<MainView>().AsSelf()
                .OnActivating(eventArgs => eventArgs.Instance.DataContext = eventArgs.Context.Resolve<MainViewModel>());

            builder.RegisterType<SubscriptionsRepository>().AsSelf().SingleInstance();

            builder.RegisterType<SettingsModel>().AsSelf().SingleInstance();
            builder.RegisterType<SettingsViewModel>().AsSelf().SingleInstance();

            builder.RegisterType<BursesModel>().AsSelf().SingleInstance();
            builder.RegisterType<BursesViewModel>().AsSelf().SingleInstance();

            var container = builder.Build();

            using var scope = container.BeginLifetimeScope();

            var mainWindow = scope.Resolve<MainView>();

            //mainWindow.ShowDialog();

            var authWindow = scope.Resolve<AuthView>();
            authWindow.ShowDialog();

            var mainViewModel = scope.Resolve<MainViewModel>();
            mainViewModel.WhenAnyValue(x => x.ApplicationClosing)
                .Where(closing => closing)
                .Subscribe(_ => mainWindow.Close());

            if ((bool)authWindow.DialogResult)
                mainWindow.ShowDialog();
            else
                mainWindow.Close();
        }
    }
}
