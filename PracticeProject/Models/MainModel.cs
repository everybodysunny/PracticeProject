using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using MaterialDesignThemes.Wpf;

namespace PracticeProject.Models
{
   public class MainModel
   {
        public PackIconKind IconKind { get; set; }
        public string? Text { get; set; }
        public string? PageName { get; set; }
        public bool IsExpanded { get; set; } = true;
        public ObservableCollection<MainModel> Children { get; set; } = new();
   }
}
