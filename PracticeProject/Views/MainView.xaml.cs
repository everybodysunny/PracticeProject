using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PracticeProject.Views
{
    /// <summary>
    /// MainView.xaml 的交互逻辑
    /// </summary>
    public partial class MainView : Window
    {
        private readonly IRegionManager _regionManager;
        public MainView(IRegionManager regionManager)
        {
            InitializeComponent();
            _regionManager=regionManager;
            this.StateChanged+=MainWindow_StateChanged;
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 加载默认视图
            if (_regionManager.Regions.ContainsRegionWithName("ContentRegion")) 
            {
                _regionManager.RequestNavigate("ContentRegion", "HomeView");
            }
               
        }
        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if(this.WindowState==WindowState.Maximized)
            {
                MaximizeIcon.Kind = PackIconKind.WindowRestore;
                MaximizeButton.ToolTip = "还原";
            }
            else
            {
                MaximizeIcon.Kind = PackIconKind.WindowMaximize;
                MaximizeButton.ToolTip = "最大化";
            }
        }


        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MenuTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {

        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if(this.WindowState==WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }

        }

        private void ColorZone_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                // 双击标题栏最大化/还原
                MaximizeButton_Click(sender, e);
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                // 单击拖动窗口
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
