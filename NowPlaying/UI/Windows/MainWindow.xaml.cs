﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using NowPlaying.ApiResponses;
using NowPlaying.Extensions;
using NowPlaying.UI.UserControls;
using System.Windows.Media;
using MenuItem = System.Windows.Forms.MenuItem;
using System.Windows.Media.Animation;

namespace NowPlaying.UI.Windows
{
    public partial class MainWindow : Window
    {
        private bool IsAutoTrackChangeEnabled { get; set; }
        private string CurrentKeyBind { get; set; }
        protected string LastPlayingTrackId { get; set; }

        private CancellationTokenSource _cancellationGetSpotifyUpdates;

        public MainWindow()
        {
            this.InitializeComponent();

            #if DEBUG
            DebugCheckBox.Visibility = Visibility.Visible;
            #endif
        }

        private void InitializeTrayMenu()
        {
            Program.TrayMenu.Items.AddRange(new MenuItem[]
            {
                new MenuItem("Show", TrayMenu.CreateEventHandler(ShowFromTray)),
                new MenuItem("Exit", TrayMenu.CreateEventHandler(Close)),
            });

            Program.TrayMenu.Icon.DoubleClick += TrayMenu.CreateEventHandler(ShowFromTray);
            Program.TrayMenu.NpcWorkTrayCheckBox.Click += TrayMenu.CreateEventHandler(NpcWorkCheckChange);
            Program.TrayMenu.TopMostCheckBox.Click += TrayMenu.CreateEventHandler(TopMostChange);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Hide();

            var browserWindow = new BrowserWindow();
            browserWindow.ShowDialog();

            if (browserWindow.ResultToken == null)
            {
                this.Close();
                return;
            }

            AppInfo.State.SpotifyAccessToken = browserWindow.ResultToken;
            AppInfo.State.SpotifyRefreshToken = browserWindow.RefreshToken;
            AppInfo.State.TokenExpireTime = DateTime.Now.AddSeconds(browserWindow.ExpireTime - 5);

            this.Show();

            if (AccountsList.SelectedItem == null)
            {
                MessageBox.Show("Файл loginusers.vdf пуст");
                this.Close();
                return;
            }

            AcrylicMaterial.EnableBlur(this);
            this.InitializeTrayMenu();
            Program.TrayMenu.Show();
        }

        private void ButtonDo_Click(object sender, RoutedEventArgs e)
        {
            if (AppInfo.State.TokenExpireTime < DateTime.Now)
            {
                this.ButtonDo.Content = "spotify token expired!";
                return;
            }

            var trackResp = Requests.GetCurrentTrack(AppInfo.State.SpotifyAccessToken);

            if (trackResp == null)
                return;

            this.UpdateInterfaceTrackInfo(trackResp);

            if (AccountsList.SelectedItem == null)
                return;

            var cfgWriter = new ConfigWriter($@"{SteamIdLooker.UserdataPath}\{this.GetSelectedAccountId().ToString()}\730\local\cfg\audio.cfg");
            cfgWriter.RewriteKeyBinding(trackResp);
        }

        private async void ToggleSwitch_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!this.SpotifySwitch.Toggled)
            {
                this._cancellationGetSpotifyUpdates?.Cancel();
                Program.TrayMenu.NpcWorkTrayCheckBox.Checked = false;
                return;
            }

            if (!SourceKeysExtensions.SourceEngineAllowedKeys.Contains(this.TextBoxKeyBind.CurrentText))
            {
                this.SpotifySwitch.TurnOff();
                MessageBox.Show("такой кнопки в кантре нет");
                return;
            }

            TextBoxToConsole.Text = $"bind \"{this.TextBoxKeyBind.CurrentText}\" \"exec audio.cfg\"";

            this.ButtonDo_Click(this, null); // force first request to not wait for the Thread.Sleep(1000)

            string keyboardButton = AccountsList.SelectedItem;
            int _SelectedAccount = AccountsList.SelectedIndex;
            this._cancellationGetSpotifyUpdates = new CancellationTokenSource();

            var cfgWriter = new ConfigWriter($@"{SteamIdLooker.UserdataPath}\{this.GetSelectedAccountId().ToString()}\730\local\cfg\audio.cfg");

            await Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (AccountsList.SelectedIndex != _SelectedAccount)
                        this.Dispatcher.Invoke(() => OnAccountsListSelectionChanged());

                    Thread.Sleep(1000);

                    if (AppInfo.State.TokenExpireTime < DateTime.Now)
                    {
                        AppInfo.State.RefreshToken();
                        cfgWriter.RewriteKeyBinding("say \"spotify token expired!\"");
                    }

                    var trackResp = Requests.GetCurrentTrack(AppInfo.State.SpotifyAccessToken);

                    if (trackResp != null && trackResp.Id != this.LastPlayingTrackId)
                    {
                        cfgWriter.RewriteKeyBinding(trackResp);
                        this.LastPlayingTrackId = trackResp.Id;
                        if (trackResp.FormattedArtists.Length > 27)
                            Dispatcher.Invoke(() => LabelArtistAnimation());
                        else Dispatcher.Invoke(() => LabelArtist.BeginAnimation(System.Windows.Controls.Canvas.RightProperty, null));
                        if (IsAutoTrackChangeEnabled && Program.GameProcess.IsValid)
                            KeySender.SendInputWithAPI(CurrentKeyBind);
                    }

                    this.Dispatcher.Invoke(() => this.UpdateInterfaceTrackInfo(trackResp));
                    this.Dispatcher.Invoke(() => LabelWindowHandle.Content = AppInfo.State.WindowHandle);

                    if (this._cancellationGetSpotifyUpdates.IsCancellationRequested)
                        return;
                }
            });

        }

        private void UpdateInterfaceTrackInfo(CurrentTrackResponse trackResp)
        {
            this.IsAutoTrackChangeEnabled = this.CheckBoxAutoSend.IsChecked;

            this.CurrentKeyBind = this.TextBoxKeyBind.CurrentText;
            this.LabelWithButton.Content = this.TextBoxKeyBind.CurrentText;

            if (trackResp == null)
            {
                this.LabelArtist.Content = "NowPlaying";
                this.LabelFormatted.Content = "Nothing is playing!";
                return;
            }

            if (trackResp.IsLocalFile)
                this.LabelLocalFilesWarning.Visibility = Visibility.Visible;
            else
                this.LabelLocalFilesWarning.Visibility = Visibility.Collapsed;

            this.LabelArtist.Content = $"{trackResp.FormattedArtists}";
            this.LabelFormatted.Content = $"{trackResp.Name}";
            this.LabelCurrentTime.Content = $"{trackResp.ProgressMinutes.ToString()}:{trackResp.ProgressSeconds:00}";
            this.LabelEstimatedTime.Content = $"{trackResp.DurationMinutes.ToString()}:{trackResp.DurationSeconds:00}";
        }

        private int GetSelectedAccountId()
        {
            return AppInfo.State.AccountNameToSteamId3[AccountsList.SelectedItem];
        }

        private void OnAccountsListSelectionChanged()
        {
            if (this.SpotifySwitch.Toggled && this._cancellationGetSpotifyUpdates != null)
            {
                this.SpotifySwitch.TurnOff();
                this._cancellationGetSpotifyUpdates?.Cancel();
            }
        }

        private void LabelSourceKeysClick(object sender, RoutedEventArgs e)
        {
            if (!SourceKeysExtensions.TryOpenSourceKeysFile())
                MessageBox.Show("не найден файл с биндами (SourceKeys.txt)");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Program.Dispose();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized) //костыль для работы трея из форм в впфе
                this.Hide();
            else
                ShowInTaskbar = true;
        }

        private void ShowFromTray()
        {
            this.Show();
            WindowState = WindowState.Normal;
        }

        private void NpcWorkCheckChange()
        {
            this.SpotifySwitch.Toggle();
            ToggleSwitch_MouseLeftButtonDown(null, null);
        }

        private void TopMostChange() => this.Topmost = !this.Topmost;

        private void LabelHelpClick(object sender, RoutedEventArgs e)
            => Process.Start("https://github.com/veselv2010/NowPlaying/blob/master/README.md");

        private void CloseButton_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) => this.Close();

        private void Rectangle_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OnMouseLeftButtonDown(e);
            this.DragMove();
        }

        private void MinimizeWindowButton_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
            ShowInTaskbar = false;
        }

        private void ToggleSwitchNightMode_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (NightModeSwitch.IsNightModeToggled)
            {
                this.Background = new SolidColorBrush(Color.FromRgb(23, 23, 23)); //#171717
                this.LabelCurrentKey.Foreground = new SolidColorBrush(Color.FromRgb(178, 178, 178)); //#B2B2B2
                this.LabelNpcWork.Foreground = new SolidColorBrush(Color.FromRgb(249, 249, 249)); //#F9F9F9
                this.SpotifySwitch.NightModeEnable();
                this.TextBoxKeyBind.NightModeEnable();
            }
            else
            {
                this.Background = new SolidColorBrush(Color.FromRgb(249, 249, 249)); //#F9F9F9
                this.LabelCurrentKey.Foreground = new SolidColorBrush(Color.FromRgb(126, 126, 126)); //#7e7e7e
                this.LabelNpcWork.Foreground = new SolidColorBrush(Color.FromRgb(126, 126, 126)); //#F9F9F9
                this.SpotifySwitch.NightModeDisable();
                this.TextBoxKeyBind.NightModeDisable();
            }
        }
        private void LabelArtistAnimation()
        {
            var doubleAnimation = new DoubleAnimation
            {
                From = -LabelArtist.ActualWidth,
                To = ArtistCanv.ActualWidth,
                RepeatBehavior = RepeatBehavior.Forever,
                Duration = new Duration(TimeSpan.Parse("0:0:8"))
            };
            this.LabelArtist.BeginAnimation(System.Windows.Controls.Canvas.RightProperty, doubleAnimation);
        }
    }
}

//-_=ICON BY SCOUTPAN_=