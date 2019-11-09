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
    /// Interaction logic for Population.xaml
    /// </summary>
    partial class Population : Window
    {
        public delegate void Callback(List<float> resources, int player);

        public Population(Callback cb_)
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
            float imp;
            float civ;
            int playerID;
            float.TryParse(minImp.Text, out imp);
            float.TryParse(minCiv.Text, out civ);
            int.TryParse(player.Text, out playerID);
            List<float> levels = new List<float>(3);
            levels.Add(civ/100);
            levels.Add(Math.Min(imp,100)/100);
            levels.Add(Math.Max(imp-100,0)/100);
            cb(levels, playerID);
            this.Close();
        }

        Callback cb;
    }
}
