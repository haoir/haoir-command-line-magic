using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.Version;

namespace HaOir
{
    public partial class MainWindow : Window
    {
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
            // 启动背景动画
            var storyboard = (System.Windows.Media.Animation.Storyboard)Resources["BackgroundAnimation"];
            storyboard.Begin();
        }

        private async void InitializeLauncher()
        {
            try
            {
                var path = new MinecraftPath();
                launcher = new MinecraftLauncher(path);
                
                // 默认加载原版版本列表
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
                    case "原版":
                        await LoadVanillaVersions();
                        break;
                    case "Forge":
                        await LoadForgeVersions();
                        break;
                    case "Fabric":
                        await LoadFabricVersions();
                        break;
                    case "NeoForge":
                        await LoadNeoForgeVersions();
                        break;
                    case "服务端":
                        await LoadServerVersions();
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载版本列表失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadVanillaVersions()
        {
            var versions = await launcher.GetVersionMetadatas();
            VersionList.Items.Clear();
            foreach (var version in versions)
            {
                VersionList.Items.Add(new System.Windows.Controls.ListBoxItem { Content = version.Name });
            }
        }

        private async Task LoadForgeVersions()
        {
            var forgeInstaller = new ForgeInstaller();
            var versions = await forgeInstaller.GetForgeVersions();
            VersionList.Items.Clear();
            foreach (var version in versions)
            {
                VersionList.Items.Add(new System.Windows.Controls.ListBoxItem { Content = version });
            }
        }

        private async Task LoadFabricVersions()
        {
            var fabricLoader = new FabricLoader();
            var versions = await fabricLoader.GetBuilds();
            VersionList.Items.Clear();
            foreach (var version in versions)
            {
                VersionList.Items.Add(new System.Windows.Controls.ListBoxItem { Content = $"Fabric-{version.Version}" });
            }
        }

        private async Task LoadNeoForgeVersions()
        {
            // NeoForge 版本列表（示例，实际需从 API 获取）
            VersionList.Items.Clear();
            var neoForgeVersions = new[] { "1.20.4-49.0.0", "1.20.1-47.1.0", "1.19.4-45.0.0" };
            foreach (var version in neoForgeVersions)
            {
                VersionList.Items.Add(new System.Windows.Controls.ListBoxItem { Content = version });
            }
        }

        private async Task LoadServerVersions()
        {
            VersionList.Items.Clear();
            var serverVersions = new[] { "server-1.21.1", "server-1.20.4", "server-1.19.4" };
            foreach (var version in serverVersions)
            {
                VersionList.Items.Add(new System.Windows.Controls.ListBoxItem { Content = version });
            }
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
                    // 服务端模式：下载 server.jar
                    await DownloadServerJar(selectedVersion);
                    return;
                }

                if (isPremiumMode)
                {
                    // 正版登录
                    await LoginWithMicrosoft();
                }
                else
                {
                    // 离线登录
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
            var loginHandler = new JELoginHandlerBuilder().Build();
            var loginTask = loginHandler.TryLogin(launcher.Path, (uri, request) =>
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = uri.ToString(),
                    UseShellExecute = true
                });
            });

            session = await loginTask;
            await LaunchMinecraft();
        }

        private async Task LaunchMinecraft()
        {
            var processBuilder = new MProcessBuilder(launcher)
            {
                VersionName = selectedVersion,
                Session = session
            };

            var process = await processBuilder.BuildAsync();
            process.Start();
            ModalOverlay.Visibility = Visibility.Collapsed;
            MessageBox.Show($"Minecraft {selectedVersion} 已启动", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task DownloadServerJar(string version)
        {
            try
            {
                var versionName = version.Replace("server-", "");
                var downloadUrl = $"https://serverjars.com/fetchJar/vanilla/{versionName}";
                
                var response = await httpClient.GetAsync(downloadUrl);
                response.EnsureSuccessStatusCode();

                var filePath = System.IO.Path.Combine(launcher.Path.BasePath, $"server-{versionName}.jar");
                using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
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
