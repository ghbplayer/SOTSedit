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
    /// Interaction logic for WhichPlayer.xaml
    /// </summary>
    partial class WhichPlayer : Window
    {
        public delegate void Callback(int player);

        public WhichPlayer(Callback cb_)
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
            int playerID;
            int.TryParse(player.Text, out playerID);
            cb(playerID);
            this.Close();
        }

        Callback cb;
    }
}
