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
    /// Interaction logic for MinMax.xaml
    /// </summary>
    partial class MinMax : Window
    {
        public delegate void Callback(List<float> resources, int player);

        public MinMax(Callback cb_)
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
            float minI;
            float maxI;
            int playerID;
            float.TryParse(min.Text, out minI);
            if(!float.TryParse(max.Text, out maxI))
                maxI = 1e24f;   //rather large, right?
            int.TryParse(player.Text, out playerID);
            List<float> levels = new List<float>(2);
            levels.Add(minI);
            levels.Add(maxI);
            cb(levels, playerID);
            this.Close();
        }

        Callback cb;
    }
}
