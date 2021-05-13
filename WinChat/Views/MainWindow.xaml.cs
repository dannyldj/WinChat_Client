using System;
using System.Linq;
using System.Windows;
using WinChat.ViewModels;

namespace WinChat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainWindowViewModel viewModel = (MainWindowViewModel)DataContext;
            Closing += viewModel.OnWindowClosing;
        }
    }
}
