using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace _20220209_tictactoe
{
    enum MType
    {
        INIT,
        COORD,
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private ObservableCollection<string> _fieldCollection;

        private readonly int _serverPort = 8080;
        private Socket _serverSocket;
        private Socket _remoteSocket;

        private string _userName;
        private string _enemyName;

        private bool _isServer = false;

        private ModalWindow _mw;
        private ModalWindowClient _mwc;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            _fieldCollection = new ObservableCollection<string>();
            // TODO: This is a kostil
            for (int i = 0; i < 9; ++i)
                _fieldCollection.Add(string.Empty);
        }


        public ObservableCollection<string> FieldCollection
        {
            get { return _fieldCollection; }
            set
            {
                _fieldCollection = value;
                OnPropertyChanged(nameof(FieldCollection));
            }
        }

        public string UserName
        {
            get { return _userName; }
            set 
            { 
                _userName = value; 
                OnPropertyChanged(nameof(UserName)); 
            }
        }

        public string EnemyName
        {
            get { return _enemyName; }
            set 
            { 
                _enemyName = value; 
                OnPropertyChanged(nameof(EnemyName)); 
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        

        private void menuCreateGame_Click(object sender, RoutedEventArgs e)
        {
            _isServer = true;

            GetUserName();

            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPAddress ip = GetHostIp();
            UserName += $" ({ip}:{_serverPort})";

            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, _serverPort));
            _serverSocket.Listen(0);

            WaitConnectionAsync();
        }
        private void menuConnectToGame_Click(object sender, RoutedEventArgs e)
        {
            _isServer = false;

            GetClientInfo();

            if (_mwc.DialogResult == false)
                return;

            _remoteSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                IPAddress serverIp = IPAddress.Parse(_mwc.ServerIp);
                int serverPort = Int32.Parse(_mwc.ServerPort);

                _remoteSocket.Connect(new IPEndPoint(serverIp, serverPort));

                SendDataAsync(MType.INIT, UserName);

                field.IsEnabled = false;

                ReceiveDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void menuExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void MouseLeftButtonDownHandler(object sender, MouseButtonEventArgs e)
        {
            TextBlock? tb = sender as TextBlock;
            string name = tb.Name;

            int id = Int32.Parse(name.Replace("C", ""));

            FieldCollection[id] = _isServer ? "X" : "0";

            field.IsEnabled = false;

            SendDataAsync(MType.COORD, id.ToString());

            // TODO: check game
        }


        private async void WaitConnectionAsync()
        {
            await Task.Run(() => {
                _remoteSocket = _serverSocket.Accept();
            });

            if (_remoteSocket != null)
            {
                ReceiveDataAsync();
                SendDataAsync(MType.INIT, UserName);

                MessageBox.Show("Connection established. Game has begun");
            }
        }

        private async void ReceiveDataAsync()
        {
            while (true)
            {
                string result = string.Empty;

                result = await Task.Run(() =>
                {
                    string receivedMessage = string.Empty;

                    do
                    {
                        byte[] buffer = new byte[64];
                        int byteCount = _remoteSocket.Receive(buffer);
                        receivedMessage += Encoding.ASCII.GetString(buffer, 0, byteCount);
                    } while (_remoteSocket.Available > 0);

                    return receivedMessage;
                });

                MType mt = (MType)Int32.Parse(result.Substring(0, 1));

                switch (mt)
                {
                    case MType.INIT:
                        EnemyName = result.Substring(1);
                        break;
                    case MType.COORD:
                        int id = Int32.Parse(result.Substring(1));
                        FieldCollection[id] = _isServer ? "0" : "X";

                        // TODO: check game

                        field.IsEnabled = true;
                        break;
                }






            }

        }

        private async void SendDataAsync(MType mt, string data)
        {
            try
            {
                await Task.Run(() =>
                {
                    _remoteSocket.Send(Encoding.ASCII.GetBytes($"{(int)mt}{data}"));
                });
            }catch (Exception ex)
            {
                Close();
            }
        }


        private void GetUserName()
        {
            _mw = new ModalWindow();
            if (_mw.ShowDialog() == true)
                UserName = _mw.Name;
        }
        private void GetClientInfo()
        {
            _mwc = new ModalWindowClient();
            if (_mwc.ShowDialog() == true)
                UserName = _mwc.Name;
        }

        private IPAddress GetHostIp()
        {
            // TODO: check ip
            IPHostEntry entry = Dns.GetHostEntry(Dns.GetHostName());

            foreach (var ip in entry.AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip;

            return IPAddress.Loopback;
        }
    }
}
