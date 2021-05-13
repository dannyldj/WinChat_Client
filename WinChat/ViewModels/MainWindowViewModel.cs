using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Media;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WinChat.Models;
using WinChat.Properties;

namespace WinChat.ViewModels
{
    class MainWindowViewModel : ObservableObject
    {
        #region Private Props
        private bool _buttonStatus = false;
        private string _connectStatus = "Disconnected";
        private string _enterMessage;
        //private bool _toggleOverLengthTooltip = false;
        //private string _overLengthTooltipText;
        #endregion

        #region Commands
        public ICommand ToggleConnectCommand { get; private set; }
        public IRelayCommand SendCommand { get; private set; }
        #endregion

        private TcpClient client = null;
        private NetworkStream chatStream = null;
        private Thread receiveThread = null;

        public MainWindowViewModel()
        {
            ToggleConnectCommand = new RelayCommand(ExecuteToggleConnectCommand);
            SendCommand = new RelayCommand(ExecuteSendCommand, CanExecuteSendCommand);

            MessageList = new ObservableCollection<Message>();
            ConnectionList = new ObservableCollection<string>();
        }

        /// <summary>
        /// 메시지 수신 쓰레드
        /// </summary>
        private void ReceiveMessage()
        {
            while (true)
            {
                try
                {
                    byte[] receiveBuf = new byte[400];
                    int readLen = chatStream.Read(receiveBuf, 0, receiveBuf.Length);
                    string receiveMessage = Encoding.UTF8.GetString(receiveBuf, 0, readLen);

                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        MessageList.Add(new Message("Server", receiveMessage));
                    }));
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Exception",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                }

                Thread.Sleep(500);
            }

            Disconnect();
        }

        /// <summary>
        /// 서버 연결 종료
        /// </summary>
        private void Disconnect()
        {
            try
            {
                if (receiveThread != null)
                {
                    receiveThread.Abort();
                }
            }
            finally
            {
                if (client != null)
                {
                    client.Close();
                    client = null;
                }

                ButtonStatus = false;
                ConnectStatus = "Disconnected";

                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                {
                    MessageList.Clear();
                    EnterMessage = string.Empty;
                }));
            }
        }

        /// <summary>
        /// 프로그램 종료 이벤트 핸들러
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            Settings.Default.Save();
            Disconnect();
        }

        #region Public Props
        public ObservableCollection<Message> MessageList { get; private set; }
        public ObservableCollection<string> ConnectionList { get; private set; }

        public bool ButtonStatus
        {
            get { return _buttonStatus; }
            set { SetProperty(ref _buttonStatus, value); }
        }

        public string ConnectStatus
        {
            get { return _connectStatus; }
            set { SetProperty(ref _connectStatus, value); }
        }

        public string EnterMessage
        {
            get { return _enterMessage; }
            set
            {
                if (value.Length <= 100)
                {
                    SetProperty(ref _enterMessage, value);
                    //ToggleOverLengthTooltip = false;
                    //OverLengthTooltipText = string.Empty;
                }
                else
                {
                    SystemSounds.Beep.Play();
                    MessageBox.Show("메시지는 100자를 넘을 수 없습니다.");
                    //OverLengthTooltipText = "메시지는 100자를 넘을 수 없습니다.";
                    //ToggleOverLengthTooltip = true;
                }
                SendCommand.NotifyCanExecuteChanged();
            }
        }

        //public bool ToggleOverLengthTooltip
        //{
        //    get { return _toggleOverLengthTooltip; }
        //    set { SetProperty(ref _toggleOverLengthTooltip, value); }
        //}

        //public string OverLengthTooltipText
        //{
        //    get { return _overLengthTooltipText; }
        //    set { SetProperty(ref _overLengthTooltipText, value); }
        //}
        #endregion

        #region Execute Methods
        void ExecuteToggleConnectCommand()
        {
            if (ButtonStatus)
            {
                try
                {
                    client = new TcpClient();
                    client.Connect(Settings.Default.connectingIp, Settings.Default.connectingPort);
                    chatStream = client.GetStream();

                    receiveThread = new Thread(ReceiveMessage);
                    receiveThread.Start();

                    ConnectStatus = "Connected";
                }
                catch (SocketException ex)
                {
                    MessageBox.Show(ex.Message, "연결 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    ButtonStatus = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Exception",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    ButtonStatus = false;
                }
            }
            else
            {
                Disconnect();
            }
        }

        void ExecuteSendCommand()
        {
            try
            {
                byte[] sendBuf = new byte[400];
                sendBuf = Encoding.UTF8.GetBytes(EnterMessage);
                chatStream.Write(sendBuf, 0, sendBuf.Length);
                MessageList.Add(new Message(Settings.Default.userName, EnterMessage));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Exception",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Disconnect();
            }

            EnterMessage = string.Empty;
        }
        #endregion

        #region CanExecute
        bool CanExecuteSendCommand()
        {
            return string.IsNullOrEmpty(EnterMessage) == false;
        }
        #endregion
    }
}
