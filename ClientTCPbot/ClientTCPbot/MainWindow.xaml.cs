
using System.Configuration;
using System.Windows;
using Telegram.Bot;

namespace ClientTCPbot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static TelegramBotClient client;
        public MainWindow()
        {
            InitializeComponent();
            string smth = ConfigurationSettings.AppSettings.Get("TeleBotClientKey");
            client = new TelegramBotClient("1125825450:AAF0L7OLkTMJWLdOpVkL7ua1vAXN6I8rm58");
            client.StartReceiving();

        }
    }
}
