﻿
using System.Windows.Controls;
using System.Windows.Media;

namespace NowPlaying.UI.UserControls
{
    public partial class CustomComboBoxNight : UserControl
    {
        public string SelectedItem { get; private set; }
        public int SelectedIndex { get; private set; }

        public CustomComboBoxNight()
        {
            InitializeComponent();

            foreach (string a in AppInfo.State.AccountNames)
            {
                this.DefaultComboBox.Items.Add(a);
            }

            this.DefaultComboBox.Foreground = new SolidColorBrush(Color.FromArgb(0xff, 0xf9, 0xf9, 0xf9));
            this.DefaultComboBox.SelectedItem = SteamIdLooker.SteamLastLoggedOnAccount;
        }

        private void DefaultComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedItem = this.DefaultComboBox.SelectedItem as string;
            SelectedIndex = this.DefaultComboBox.SelectedIndex;
        }
    }
}