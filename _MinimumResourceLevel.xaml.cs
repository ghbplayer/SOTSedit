using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SOTSEdit
{
    /// <summary>
    /// Interaction logic for MinimumResourceLevel.xaml
    /// </summary>
    partial class MinimumResourceLevel : Window
    {
        public delegate void Callback(List<int> resources, int player);

        public MinimumResourceLevel(Callback cb_)
        {
            cb = cb_;
            InitializeComponent();
        }

        public void cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void okay(object sender, RoutedEventArgs e)
        {
            int baseR;
            int astR;
            int dustR;
            int playerID;
            int.TryParse(minBase.Text, out baseR);
            int.TryParse(minAst.Text, out astR);
            int.TryParse(minDust.Text, out dustR);
            int.TryParse(player.Text, out playerID);
            List<int> levels = new List<int>(3);
            levels.Add(baseR);
            levels.Add(astR);
            levels.Add(dustR);
            cb(levels, playerID);
            this.Close();
        }

        Callback cb;
    }
}
