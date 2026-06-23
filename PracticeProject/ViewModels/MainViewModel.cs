using PracticeProject.Models;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MaterialDesignThemes.Wpf;
using System.Threading.Tasks;
using System.ComponentModel;

namespace PracticeProject.ViewModels
{
    public class MainViewModel : BindableBase
    {
        private IRegionManager _regionManager;
        public ObservableCollection<MainModel> MenuItems { get; set; } = new();

        public DelegateCommand<MainModel> NavigateCommand => new DelegateCommand<MainModel>(NavigateToPage);

        public MainViewModel(IRegionManager regionmanger)
        {
            _regionManager = regionmanger;
            InitMenuItems();
        }

        #region 菜单初始化

        private void InitMenuItems()
        {
            // 主页 - 数据总览（只做数据显示）
            MenuItems.Add(new MainModel
            {
                IconKind = PackIconKind.Home,
                Text = "主页",
                PageName = "HomeView",
                IsExpanded = true
            });

            // 数据采集 - 包含通讯设置
            var dataCollection = new MainModel
            {
                IconKind = PackIconKind.ChartLine,
                Text = "数据采集",
                PageName = "",
                IsExpanded = true
            };
            dataCollection.Children.Add(new MainModel
            {
                IconKind = PackIconKind.Lan,
                Text = "通讯设置",
                PageName = "PlcConnectionView"
            }
            
            );

            dataCollection.Children.Add(new MainModel
            {
                IconKind= PackIconKind.Road,
                Text="通讯界面",
                PageName= "PlcSettingPage"
            });
            MenuItems.Add(dataCollection);

            // SQL 连接
            MenuItems.Add(new MainModel
            {
                IconKind = PackIconKind.Database,
                Text = "SQL 数据库",
                PageName = "SqlConnectionView",
                IsExpanded = true
            });
        }

        #endregion

        #region 导航方法

        private void NavigateToPage(MainModel model)
        {
            if (model != null && !string.IsNullOrEmpty(model.PageName))
            {
                _regionManager.RequestNavigate("ContentRegion", model.PageName);
            }
        }

        #endregion
    }
}
