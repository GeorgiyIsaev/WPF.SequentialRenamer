using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WPF.SequentialRenamer.Utils;

namespace WPF.SequentialRenamer
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>Общий логгер приложения: ошибки и предупреждения пишутся в файл рядом с exe.</summary>
        public static readonly Logger Log = new Logger();

        public App()
        {
            // Обработчики ставятся как можно раньше, чтобы успеть записать причину любого вылета в лог.
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error("Необработанная ошибка в интерфейсе.", e.Exception);
            e.Handled = true; // чтобы тот же сбой не пришёл повторно в OnAppDomainUnhandledException

            // Если главное окно ещё не открылось, продолжать работу не в чем — процесс повис бы без интерфейса.
            bool startupFailed = MainWindow == null;

            MessageBox.Show(
                "Произошла непредвиденная ошибка." + Environment.NewLine +
                "Причина: " + e.Exception.Message + Environment.NewLine + Environment.NewLine +
                (startupFailed
                    ? "Программа будет закрыта. Подробности записаны в лог: "
                    : "Работа продолжена. Подробности записаны в лог: ") + Log.FilePath,
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            if (startupFailed)
                Environment.Exit(1);
        }

        // Неустранимый сбой вне интерфейсного потока: предотвратить вылет нельзя, остаётся записать причину.
        private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Error("Критическая ошибка — приложение будет закрыто системой.", e.ExceptionObject as Exception);
        }

        // Исключение в фоновой задаче, которое никто не обработал: приложение продолжает работу.
        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Log.Warn("Фоновая задача упала с необработанным исключением.", e.Exception);
            e.SetObserved();
        }
    }
}
