using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.Installer.Forge;

namespace HaOir
{
    public partial class MainWindow : Window
    {
        private MinecraftPath mcPath;
        private MinecraftLauncher launcher;
        private MSession session;
        private string selectedVersion;
        private bool isPremiumMode = false;
        private HttpClient httpClient;

        public MainWindow()
        {
            InitializeComponent();
            httpClient = new HttpClient();
            InitializeLauncher();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var storyboard = (System.Windows.Media.Animation.Storyboard)Resources["BackgroundAnimation"];
            storyboard.Begin();
        }

        private async void InitializeLauncher()
        {
            try
            {
                mcPath = new MinecraftPath();
                launcher = new MinecraftLauncher(mcPath);
                await LoadVanillaVersions();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化启动器失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CategoryList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CategoryList.SelectedItem == null) return;
            var category = (CategoryList.SelectedItem as System.Windows.Controls.ListBoxItem)?.Content?.ToString();
            
            try
            {
                switch (category)
                {
                    case "原版": await LoadVanillaVersions(); break;
                    case "Forge": await LoadForgeVersions(); break;
                    case "Fabric": await LoadFabricVersions(); break;
                    case "NeoForge": await LoadNeoForgeVersions(); break;
                    case "服务端": await LoadServerVersions(); break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载列表失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadVanillaVersions()
        {
            var versions = await launcher.GetAllVersionsAsync();
            VersionList.Items.Clear();
            foreach (var version in versions)
            {
                VersionList.Items.Add(new System.Windows.Controls.ListBoxItem { Content = version.Name });
            }
        }

        private async Task LoadForgeVersions()
        {
            try
            {
                var installer = new ForgeInstaller(launcher);
                var vanillaVersions = await installer.GetForgeVersions("1.20.1");
                VersionList.Items.Clear();
                foreach (var version in vanillaVersions)
                {
                    VersionList.Items.Add(new System.Windows.Controls.ListBoxItem { Content = version.Version });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取Forge版本失败：{ex.Message}", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task LoadFabricVersions()
        {
            VersionList.Items.Clear();
            VersionList.Items.Add(new System.Windows.Controls.ListBoxItem { Content = "fabric-loader-0.15.11-1.20.1" });
            await Task.CompletedTask;
        }

        private async Task LoadNeoForgeVersions()
        {
            VersionList.Items.Clear();
            var neoForgeVersions = new[] { "1.20.4-49.0.0", "1.20.1-47.1.0" };
            foreach (var version in neoForgeVersions)
            {
                VersionList.Items.Add(new System.Windows.Controls.ListBoxItem { Content = version });
            }
            await Task.CompletedTask;
        }

        private async Task LoadServerVersions()
        {
            VersionList.Items.Clear();
            var serverVersions = new[] { "server-1.21.1", "server-1.20.4" };
            foreach (var version in serverVersions)
            {
                VersionList.Items.Add(new System.Windows.Controls.ListBoxItem { Content = version });
            }
            await Task.CompletedTask;
        }

        private void VersionList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (VersionList.SelectedItem == null) return;
            selectedVersion = (VersionList.SelectedItem as System.Windows.Controls.ListBoxItem)?.Content?.ToString();
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                DropdownMenu.IsOpen = false;
                ModalOverlay.Visibility = Visibility.Visible;
            }
        }

        private void StartGameBtn_Click(object sender, RoutedEventArgs e)
        {
            DropdownMenu.IsOpen = !DropdownMenu.IsOpen;
        }

        private void MainCloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void DialogCloseBtn_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Collapsed;
        }

        private void ModeToggleBtn_Click(object sender, RoutedEventArgs e)
        {
            isPremiumMode = !isPremiumMode;
            if (isPremiumMode)
            {
                ModeToggleBtn.Content = "离线";
                InputNameBox.Visibility = Visibility.Collapsed;
                PremiumText.Visibility = Visibility.Visible;
            }
            else
            {
                ModeToggleBtn.Content = "正版";
                InputNameBox.Visibility = Visibility.Visible;
                PremiumText.Visibility = Visibility.Collapsed;
            }
        }

        private async void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedVersion)) return;

            try
            {
                if (selectedVersion.StartsWith("server-"))
                {
                    await DownloadServerJar(selectedVersion);
                    return;
                }

                if (isPremiumMode)
                {
                    await LoginWithMicrosoft();
                }
                else
                {
                    var username = InputNameBox.Text.Trim();
                    if (string.IsNullOrEmpty(username))
                    {
                        MessageBox.Show("请输入用户名", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    session = MSession.GetOfflineSession(username);
                    await LaunchMinecraft();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoginWithMicrosoft()
        {
            var loginHandler = JELoginHandlerBuilder.BuildDefault();
            session = await loginHandler.AuthenticateInteractively();
            await LaunchMinecraft();
        }

        private async Task LaunchMinecraft()
        {
            ModalOverlay.Visibility = Visibility.Collapsed;
            MessageBox.Show("正在准备游戏文件并启动，请稍候...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);

            var launchOption = new MLaunchOption
            {
                Session = session,
                MaximumRamMb = 4096
            };

            var process = await launcher.InstallAndBuildProcessAsync(selectedVersion, launchOption);
            process.Start();
        }

        private async Task DownloadServerJar(string version)
        {
            try
            {
                var versionName = version.Replace("server-", "");
                var downloadUrl = $"https://serverjars.com/fetchJar/vanilla/{versionName}";
                
                var response = await httpClient.GetAsync(downloadUrl);
                response.EnsureSuccessStatusCode();

                var filePath = Path.Combine(mcPath.BasePath, $"server-{versionName}.jar");
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await response.Content.CopyToAsync(fileStream);
                }

                MessageBox.Show($"服务端已下载到：{filePath}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                ModalOverlay.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"下载服务端失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RootGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
