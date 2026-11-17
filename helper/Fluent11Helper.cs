using Microsoft.Win32;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;

namespace Fluent11Helper
{
    public class Fluent11Helper : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        internal const string ThemeMarkerKey = "Fluent11ThemeMarker";
        private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        private const string IcoFontBackArrow = "\uea5c";

        private readonly Fluent11HelperSettingsViewModel settings;
        private ResourceDictionary lightPalette;
        private ResourceDictionary darkPalette;
        private readonly List<KeyValuePair<string, string>> brushMap = new List<KeyValuePair<string, string>>();
        private readonly Dictionary<string, Brush> currentBrushes = new Dictionary<string, Brush>();
        private bool? currentDark;
        private Color? currentAccent;
        private bool micaActive;
        private bool acrylicActive;
        private Brush acrylicTint = Brushes.Transparent;
        private GridDetailsOverlay gridOverlay;

        private const double ContentLayerMicaOpacity = 0.7;

        private const double TimeExtraMix = 0.4;
        private const double TimeExtraMixDark = 0.35;
        private const double TimeCompletionistMix = 0.7;
        private const double TimeCompletionistMixDark = 0.65;
        private bool active;
        private string themeDir;
        private bool exiting;

        public static readonly Guid PluginId = Guid.Parse("fc7e2b69-0066-4f06-919e-86ad1f39cc62");
        public override Guid Id { get; } = PluginId;

        public Fluent11HelperSettingsViewModel SettingsViewModel => settings;

        public Fluent11Helper(IPlayniteAPI api) : base(api)
        {
            settings = new Fluent11HelperSettingsViewModel(this);
            Properties = new GenericPluginProperties { HasSettings = true };
            AddSettingsSupport(new AddSettingsSupportArgs
            {
                SourceName = "Fluent11Helper",
                SettingsRoot = $"{nameof(SettingsViewModel)}.{nameof(Fluent11HelperSettingsViewModel.Settings)}"
            });

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                return;
            }

            if (dispatcher.CheckAccess())
            {
                Initialize();
            }
            else
            {
                dispatcher.Invoke(new Action(Initialize));
            }
        }

        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            if (active)
            {
                yield return SettingsPage.CreateSidebarItem(settings, themeDir);
            }

            if (active && settings.Settings.ShowStatisticsPage)
            {
                yield return new StatisticsPage(PlayniteApi, themeDir, () => settings.Settings, () => SavePluginSettings(settings.Settings)).CreateSidebarItem();
            }

            if (active && settings.Settings.ShowAchievementsPage && AchievementsPage.IsPlayniteAchievementsInstalled(PlayniteApi))
            {
                yield return new AchievementsPage(PlayniteApi, themeDir, () => settings.Settings, () => SavePluginSettings(settings.Settings)).CreateSidebarItem();
            }

            if (active && settings.Settings.ShowHltbPage && HltbData.IsInstalled(PlayniteApi))
            {
                yield return new HltbPage(PlayniteApi, themeDir, () => settings.Settings, () => SavePluginSettings(settings.Settings)).CreateSidebarItem();
            }
        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            if (!active || args.Games == null || args.Games.Count == 0)
            {
                yield break;
            }

            if (settings.Settings.ReplaceHltbWindow && args.Games.Count == 1 && HltbData.IsInstalled(PlayniteApi))
            {
                var game = args.Games[0];
                yield return new GameMenuItem
                {
                    MenuSection = ResourceProvider.GetString("LOCHowLongToBeat"),
                    Description = "Time to Beat",
                    Action = a =>
                    {
                        if (!HltbGameWindow.Show(PlayniteApi, themeDir, game.Id))
                        {
                            PlayniteApi.Dialogs.ShowMessage("HowLongToBeat data could not be loaded. See extensions.log for details.", "Time to Beat");
                        }
                    }
                };
            }

            if (settings.Settings.PosterTiles)
            {
                if (args.Games.Count == 1)
                {
                    var game = args.Games[0];
                    yield return new GameMenuItem
                    {
                        MenuSection = "Fluent 11",
                        Description = "Choose desktop cover…",
                        Action = a =>
                        {
                            var file = PlayniteApi.Dialogs.SelectFile(PosterCovers.FileFilter);
                            if (!string.IsNullOrEmpty(file))
                            {
                                PosterCovers.SetCover(game.Id, file);
                            }
                        }
                    };
                }

                var withCover = args.Games.Where(g => PosterCovers.HasCover(g.Id)).ToList();
                if (withCover.Count > 0)
                {
                    yield return new GameMenuItem
                    {
                        MenuSection = "Fluent 11",
                        Description = withCover.Count == 1 ? "Remove desktop cover" : $"Remove desktop covers ({withCover.Count})",
                        Action = a => PosterCovers.RemoveCovers(withCover.Select(g => g.Id))
                    };
                }
            }

            if (settings.Settings.SystemRequirementsCard)
            {
                var steamGames = args.Games.Where(g => SystemRequirements.SteamAppId(g) != null).ToList();
                if (steamGames.Count > 0)
                {
                    yield return new GameMenuItem
                    {
                        MenuSection = "Fluent 11",
                        Description = steamGames.Count == 1 ? "Reload system requirements" : $"Reload system requirements ({steamGames.Count})",
                        Action = a => SystemRequirements.Reload(steamGames, PlayniteApi.MainView.SelectedGames)
                    };
                }
            }
        }

        private const string SlideInTag = "Fluent11SlideIn";

        private void OnMenuOpened(object sender, RoutedEventArgs e)
        {
            if (sender is ItemsControl opened)
            {
                opened.Dispatcher.BeginInvoke(new Action(() => MenuPolish.MarkRadioItems(opened)), DispatcherPriority.Normal);
            }

            if (sender is ContextMenu contextMenu)
            {
                if (TrayMenu.Is(contextMenu))
                {
                    TrayMenu.Prepare(contextMenu);
                    contextMenu.Dispatcher.BeginInvoke(new Action(() => TrayMenu.StyleIcons(contextMenu)), DispatcherPriority.Normal);
                }

                contextMenu.Dispatcher.BeginInvoke(new Action(() => MenuPolish.Apply(contextMenu, settings.Settings.HiddenMenuEntries ?? new List<string>(), settings.Settings.ShowAddonsButton)), DispatcherPriority.Normal);

                var owner = MenuReclick.GetOwner(contextMenu) as FrameworkElement;
                var besideSidebar = false;
                if (owner?.Name == "PART_ButtonMoreActions")
                {
                    contextMenu.PlacementTarget = owner;
                    contextMenu.Placement = PlacementMode.Bottom;
                    var acrylic = AcrylicPopups.IsAcrylic(contextMenu);
                    contextMenu.HorizontalOffset = acrylic ? 0 : -8;
                    contextMenu.VerticalOffset = acrylic ? 4 : 0;
                }
                else if (owner != null && FindAncestor(owner, "TopPanel") is FrameworkElement topPanel)
                {
                    PageTools.PlaceBelowBar(contextMenu, owner, () => topPanel.ActualHeight - owner.TranslatePoint(new Point(0, 0), topPanel).Y);
                }
                else if (owner?.Name == "PART_ElemMainMenu" && FindAncestor(owner, "Sidebar") is FrameworkElement sidebar)
                {
                    PageTools.PlaceBesideBar(contextMenu, owner, () => sidebar.ActualWidth - owner.TranslatePoint(new Point(0, 0), sidebar).X);
                    besideSidebar = true;
                }

                if (contextMenu.Tag == null || contextMenu.Tag as string == SlideInTag)
                {
                    contextMenu.Tag = contextMenu.Placement != PlacementMode.MousePoint && !besideSidebar ? SlideInTag : null;
                }
            }

            if (!settings.Settings.ReplaceHltbWindow)
            {
                return;
            }

            var label = ResourceProvider.GetString("LOCHowLongToBeatPluginView");
            if (sender is ItemsControl menu && !string.IsNullOrEmpty(label))
            {
                HideMenuEntry(menu, label);
            }
        }

        private static FrameworkElement FindAncestor(DependencyObject element, string typeName)
        {
            for (var current = element; current != null; current = VisualTreeHelper.GetParent(current))
            {
                if (current.GetType().Name == typeName)
                {
                    return current as FrameworkElement;
                }
            }

            return null;
        }

        private static void HideMenuEntry(ItemsControl menu, string label)
        {
            foreach (var item in menu.Items.OfType<MenuItem>())
            {
                if (item.Header as string == label)
                {
                    item.Visibility = Visibility.Collapsed;
                }
                else if (item.Items.Count > 0)
                {
                    HideMenuEntry(item, label);
                }
            }
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunView)
        {
            return new Fluent11HelperSettingsView();
        }

        private void Initialize()
        {
            try
            {
                if (Application.Current?.TryFindResource(ThemeMarkerKey) == null)
                {
                    logger.Info("Fluent 11 is not the active theme, extension stays inactive.");
                    return;
                }

                themeDir = FindThemeDirectory();
                if (themeDir == null)
                {
                    logger.Warn("Fluent 11 theme folder not found.");
                    return;
                }

                CheckThemeVersion();
                lightPalette = LoadDictionary(Path.Combine(themeDir, "Constants.xaml"));
                darkPalette = LoadDictionary(Path.Combine(themeDir, "ConstantsDark.xaml"));
                ReadBrushMap(Path.Combine(themeDir, "Common.xaml"));
                if (lightPalette == null || darkPalette == null)
                {
                    return;
                }

                active = true;
                EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnWindowLoaded));
                EventManager.RegisterClassHandler(typeof(TextBlock), FrameworkElement.SizeChangedEvent, new SizeChangedEventHandler(OnTextBlockSizeChanged));
                EventManager.RegisterClassHandler(typeof(Image), FrameworkElement.SizeChangedEvent, new SizeChangedEventHandler(OnImageSizeChanged));
                EventManager.RegisterClassHandler(typeof(ListBox), FrameworkElement.SizeChangedEvent, new SizeChangedEventHandler(OnListBoxSizeChanged));
                CoverFormat.Configure(PlayniteApi, () => settings.Settings.ListUseIcons);
                PosterCovers.Register(GetPluginUserDataPath(), () => settings.Settings);
                ScreenshotStrip.Register(PlayniteApi, () => settings.Settings);
                MediaViewer.Register(themeDir);
                SystemRequirements.Register(GetPluginUserDataPath(), () => settings.Settings);
                DescriptionCollapse.Register(() => settings.Settings.FullDescription);
                ChipsCollapse.Register(() => settings.Settings.FullDetails);
                AchievementsPage.RegisterDetailsLink(PlayniteApi);
                SteamReviews.Register(GetPluginUserDataPath(), () => settings.Settings);
                ScreenshotUtilitiesGuard.Register(PlayniteApi);
                ToolTipPlacement.Register();
                FocusAreas.Register();
                NotificationClosing.Register();
                MenuReclick.Register();
                AcrylicPopups.Register(() => settings.Settings.AcrylicMenus, () => currentDark == true);
                SmoothScroll.Register(() => settings.Settings.SmootherScrolling);
                EventManager.RegisterClassHandler(typeof(ContextMenu), ContextMenu.OpenedEvent, new RoutedEventHandler(OnMenuOpened));
                EventManager.RegisterClassHandler(typeof(MenuItem), MenuItem.SubmenuOpenedEvent, new RoutedEventHandler(OnMenuOpened));
                WatchGridItemWidth();
                AutoColumns.Register(settings);
                Fluent11HelperSettings.OverlayActiveThisSession = settings.Settings.GridDetailsOverlay;
                if (settings.Settings.GridDetailsOverlay)
                {
                    gridOverlay = new GridDetailsOverlay(open => settings.Settings.GridOverlayOpen = open);
                    gridOverlay.WatchFilter(() => settings.Settings.GridOverlayFull);
                    EventManager.RegisterClassHandler(typeof(Border), UIElement.MouseLeftButtonDownEvent, new MouseButtonEventHandler((s, e) =>
                    {
                        if ((s as Border)?.Tag as string == "Fluent11OverlayDim")
                        {
                            gridOverlay.Close();
                            e.Handled = true;
                        }
                    }));
                }
                SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
                UpdateBackdropState();
                ApplyScheme(WantsDark());
            }
            catch (Exception e)
            {
                logger.Error(e, "Fluent 11 Helper failed to start.");
            }
        }

        private void WatchGridItemWidth()
        {
            Application.Current.Resources["Fluent11PosterTileHeight"] = 300d;
            try
            {
                var appSettings = PlayniteInternals.AppSettings;
                if (appSettings?.GetType().GetProperty("GridItemWidth") == null)
                {
                    logger.Warn("Fluent 11: Playnite GridItemWidth not found, poster covers keep a fixed height.");
                    return;
                }

                void Update()
                {
                    var height = Math.Round(Convert.ToDouble(Reflect.GetMember(appSettings, "GridItemWidth")) * 4 / 3);
                    if (!(Application.Current.Resources["Fluent11PosterTileHeight"] is double old) || old != height)
                    {
                        Application.Current.Resources["Fluent11PosterTileHeight"] = height;
                        PosterCovers.RefreshAfterZoom();
                    }
                }

                Update();
                if (appSettings is INotifyPropertyChanged notify)
                {
                    notify.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == "GridItemWidth")
                        {
                            Update();
                        }
                    };
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Fluent 11: failed to watch the grid item width.");
            }
        }

        public override void OnGameSelected(OnGameSelectedEventArgs args)
        {
            if (!active)
            {
                return;
            }

            ScreenshotUtilitiesGuard.OnGameSelected(args.NewValue?.Count ?? 0);
            ScreenshotStrip.OnGameSelected(args.NewValue?.Count == 1 ? args.NewValue[0] : null);
            SystemRequirements.OnGameSelected(args.NewValue?.Count == 1 ? args.NewValue[0] : null);
            DescriptionCollapse.OnGameSelected();
            ChipsCollapse.OnGameSelected();
            SteamReviews.OnGameSelected(args.NewValue?.Count == 1 ? args.NewValue[0] : null);
            PosterCovers.OnGameSelected(args.NewValue?.Count == 1 ? args.NewValue[0].Id : (Guid?)null);

            if (args.NewValue == null || args.NewValue.Count == 0)
            {
                gridOverlay?.CloseFromAnyThread();
            }
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            gridOverlay?.CloseFromAnyThread();
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            if (!active)
            {
                return;
            }

            AddonIcons.Apply();
            AddonIcons.ApplySteamFriends(PlayniteApi);
            AddonIcons.ApplyAchievementsTrophy(PlayniteApi);
            AddonIcons.ApplyGameActivity(PlayniteApi);

            HltbLiveRefresh.Register(PlayniteApi);

            AddonUpdateBadge.Register(PlayniteApi, () => settings.Settings.ShowAddonsButton && settings.Settings.ShowAddonUpdateCount);

            Application.Current?.Dispatcher.BeginInvoke(new Action(() => DependencyCheck.Run(PlayniteApi)), DispatcherPriority.ApplicationIdle);
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            exiting = true;

            SavePluginSettings(settings.Settings);

            if (active)
            {
                SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
            }
        }

        public void ApplyCurrentScheme()
        {
            if (active)
            {
                UpdateBackdropState();
                ApplyScheme(WantsDark());
                foreach (Window window in Application.Current.Windows)
                {
                    ApplyMica(window);
                }
            }
        }

        private static readonly DependencyProperty HasFallbackBackgroundProperty =
            DependencyProperty.RegisterAttached("HasFallbackBackground", typeof(bool), typeof(Fluent11Helper), new PropertyMetadata(false));

        private void UpdateBackdropState()
        {
            micaActive = settings.Settings.MicaEnabled && MicaBackdrop.IsSupported();
            acrylicActive = micaActive && settings.Settings.AcrylicBackdrop;
        }

        private void UpdateAcrylicTint(bool dark)
        {
            var text = dark ? settings.Settings.AcrylicTintDark : settings.Settings.AcrylicTintLight;
            var tint = new SolidColorBrush(AcrylicTint.ToColor(text, dark));
            tint.Freeze();
            acrylicTint = tint;
        }

        private void ApplyMica(Window window)
        {
            try
            {
                if (micaActive)
                {
                    if (!MicaBackdrop.Enable(window, acrylicActive ? acrylicTint : Brushes.Transparent, acrylicActive) &&
                        window.Background is SolidColorBrush brush && brush.Color.A == 0 &&
                        Application.Current.TryFindResource("WindowBackgroundColor") is Color color)
                    {
                        var fallback = new SolidColorBrush(color);
                        fallback.Freeze();
                        window.Background = fallback;
                        window.SetValue(HasFallbackBackgroundProperty, true);
                    }
                }
                else
                {
                    MicaBackdrop.Disable(window);
                    if ((bool)window.GetValue(HasFallbackBackgroundProperty))
                    {
                        window.ClearValue(Window.BackgroundProperty);
                        window.ClearValue(HasFallbackBackgroundProperty);
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Fluent 11: failed to set Mica.");
            }
        }

        private bool WantsDark()
        {
            switch (settings.Settings.Mode)
            {
                case ColorMode.Light:
                    return false;
                case ColorMode.Dark:
                    return true;
                default:
                    return IsWindowsDark();
            }
        }

        private string FindThemeDirectory()
        {
            var current = PlayniteInternals.CurrentThemeDirectory;
            if (!string.IsNullOrEmpty(current) && File.Exists(Path.Combine(current, "ConstantsDark.xaml")))
            {
                return current;
            }

            logger.Warn("Fluent 11: active theme folder not reported by Playnite, searching the theme folders.");
            var roots = new[]
            {
                Path.Combine(PlayniteApi.Paths.ConfigurationPath, "Themes", "Desktop"),
                Path.Combine(PlayniteApi.Paths.ApplicationPath, "Themes", "Desktop")
            };

            foreach (var root in roots.Distinct().Where(Directory.Exists))
            {
                foreach (var dir in Directory.GetDirectories(root))
                {
                    var manifest = Path.Combine(dir, "theme.yaml");
                    if (File.Exists(manifest) &&
                        File.Exists(Path.Combine(dir, "ConstantsDark.xaml")) &&
                        File.ReadAllText(manifest).Contains("Name: Fluent 11"))
                    {
                        return dir;
                    }
                }
            }

            return null;
        }

        private void CheckThemeVersion()
        {
            try
            {
                var line = File.ReadAllLines(Path.Combine(themeDir, "theme.yaml"))
                    .FirstOrDefault(a => a.StartsWith("Version:", StringComparison.Ordinal));
                if (line == null || !Version.TryParse(line.Substring("Version:".Length).Trim(), out var theme))
                {
                    logger.Warn("Fluent 11: theme version not found, skipping the version check.");
                    return;
                }

                var helper = GetType().Assembly.GetName().Version;
                if (theme.Major != helper.Major || theme.Minor != helper.Minor)
                {
                    logger.Warn($"Fluent 11: theme {theme} and helper {helper} don't match.");
                    settings.Settings.VersionMismatch = true;
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Fluent 11: failed to check the theme version.");
            }
        }

        private static ResourceDictionary LoadDictionary(string path)
        {
            try
            {
                using (var stream = File.OpenRead(path))
                {
                    return XamlReader.Load(stream) as ResourceDictionary;
                }
            }
            catch (Exception e)
            {
                logger.Error(e, $"Failed to read color file: {path}");
                return null;
            }
        }

        private void ReadBrushMap(string commonPath)
        {
            var text = File.ReadAllText(commonPath);
            var matches = Regex.Matches(text, @"<SolidColorBrush x:Key=""(\w+)"" Color=""\{DynamicResource (\w+)\}""");
            foreach (Match match in matches)
            {
                brushMap.Add(new KeyValuePair<string, string>(match.Groups[1].Value, match.Groups[2].Value));
            }
        }

        private static bool IsWindowsDark()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey))
            {
                return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
            }
        }

        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category != UserPreferenceCategory.General || currentDark == null)
            {
                return;
            }

            var dark = settings.Settings.Mode == ColorMode.Auto ? IsWindowsDark() : currentDark.Value;
            var accent = settings.Settings.WindowsAccentColor ? WindowsAccent.Read(dark) : null;
            if (dark != currentDark || accent != currentAccent)
            {
                Application.Current?.Dispatcher.BeginInvoke(new Action(() => ApplyScheme(dark)));
            }
        }

        private static Dictionary<string, Color> AccentColors(Color accent, Color background, bool dark)
        {
            return new Dictionary<string, Color>
            {
                ["GlyphColor"] = accent,
                ["TimeMainColor"] = accent,
                ["TimeExtraColor"] = Mix(accent, background, dark ? TimeExtraMixDark : TimeExtraMix),
                ["TimeCompletionistColor"] = Mix(accent, background, dark ? TimeCompletionistMixDark : TimeCompletionistMix)
            };
        }

        private static Color Mix(Color a, Color b, double amount)
        {
            byte channel(byte x, byte y) => (byte)Math.Round(x + (y - x) * amount);
            return Color.FromRgb(channel(a.R, b.R), channel(a.G, b.G), channel(a.B, b.B));
        }

        private void ApplyScheme(bool dark)
        {
            try
            {
                var palette = dark ? darkPalette : lightPalette;
                var resources = Application.Current.Resources;
                var accent = settings.Settings.WindowsAccentColor ? WindowsAccent.Read(dark) : null;
                var accentColors = accent.HasValue && palette["LayerColor"] is Color layer ? AccentColors(accent.Value, layer, dark) : null;
                Func<string, object> colorOf = key => accentColors != null && accentColors.TryGetValue(key, out var c) ? c : palette[key];

                foreach (var key in darkPalette.Keys.OfType<string>())
                {
                    if (palette.Contains(key))
                    {
                        resources[key] = colorOf(key);
                    }
                }

                var replaced = new Dictionary<Brush, Brush>();
                foreach (var pair in brushMap)
                {
                    if (colorOf(pair.Value) is Color color)
                    {
                        var brush = new SolidColorBrush(color);
                        brush.Freeze();
                        if (currentBrushes.TryGetValue(pair.Key, out var old) && !replaced.ContainsKey(old))
                        {
                            replaced[old] = brush;
                        }

                        currentBrushes[pair.Key] = brush;
                        resources[pair.Key] = brush;
                    }
                }

                if (replaced.Count > 0)
                {
                    ReplaceBrushCopies(resources, replaced, new HashSet<ResourceDictionary>());
                }

                if (micaActive)
                {
                    resources["WindowBackgourndBrush"] = Brushes.Transparent;
                }

                if (micaActive && palette.Contains("LayerOnMicaColor") && palette["LayerOnMicaColor"] is Color onMicaColor)
                {
                    var contentLayer = new SolidColorBrush(onMicaColor);
                    contentLayer.Freeze();
                    resources["ContentLayerBrush"] = contentLayer;
                }
                else if (palette["LayerColor"] is Color layerColor)
                {
                    var contentLayer = new SolidColorBrush(layerColor) { Opacity = micaActive ? ContentLayerMicaOpacity : 1.0 };
                    contentLayer.Freeze();
                    resources["ContentLayerBrush"] = contentLayer;
                }

                UpdateAcrylicTint(dark);
                DefaultCover.Apply(themeDir, dark);

                currentDark = dark;
                currentAccent = accent;
                TrayMenu.SchemeChanged();
                foreach (Window window in Application.Current.Windows)
                {
                    if (replaced.Count > 0)
                    {
                        ReplaceStaleBrushes(window, replaced);
                    }

                    SetDarkFrame(window, dark);
                    if (micaActive)
                    {
                        ApplyMica(window);
                    }
                }

                logger.Info($"Fluent 11: applied {(dark ? "dark" : "light")} color scheme.");
            }
            catch (Exception e)
            {
                logger.Error(e, "Fluent 11: failed to apply color scheme.");
            }
        }

        private static void ReplaceBrushCopies(ResourceDictionary dictionary, Dictionary<Brush, Brush> replaced, HashSet<ResourceDictionary> visited)
        {
            if (dictionary == null || !visited.Add(dictionary))
            {
                return;
            }

            try
            {
                foreach (var key in dictionary.Source == null ? dictionary.Keys.Cast<object>().ToList() : new List<object>())
                {
                    if (dictionary[key] is Brush value && replaced.TryGetValue(value, out var newBrush))
                    {
                        dictionary[key] = newBrush;
                    }
                }
            }
            catch (Exception)
            {
            }

            foreach (var merged in dictionary.MergedDictionaries.ToList())
            {
                ReplaceBrushCopies(merged, replaced, visited);
            }
        }

        private static void ReplaceStaleBrushes(DependencyObject root, Dictionary<Brush, Brush> replaced)
        {
            var visited = new HashSet<DependencyObject>();
            var pending = new Stack<DependencyObject>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                var element = pending.Pop();
                if (!visited.Add(element))
                {
                    continue;
                }

                var stale = new List<KeyValuePair<DependencyProperty, Brush>>();
                var values = element.GetLocalValueEnumerator();
                while (values.MoveNext())
                {
                    if (values.Current.Value is Brush brush && replaced.TryGetValue(brush, out var newBrush))
                    {
                        stale.Add(new KeyValuePair<DependencyProperty, Brush>(values.Current.Property, newBrush));
                    }
                }

                if (!(element is Freezable freezable && freezable.IsFrozen))
                {
                    foreach (var pair in stale)
                    {
                        try
                        {
                            element.SetValue(pair.Key, pair.Value);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

                if (element is Visual || element is System.Windows.Media.Media3D.Visual3D)
                {
                    for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
                    {
                        pending.Push(VisualTreeHelper.GetChild(element, i));
                    }
                }

                foreach (var child in LogicalTreeHelper.GetChildren(element).OfType<DependencyObject>())
                {
                    pending.Push(child);
                }
            }
        }

        private void OnRandomGameDetails()
        {
            try
            {
                if (PlayniteApi.MainView.ActiveDesktopView != DesktopView.Grid)
                {
                    return;
                }

                if (gridOverlay != null)
                {
                    gridOverlay.OpenSelected();
                    return;
                }

                var game = PlayniteApi.MainView.SelectedGames?.FirstOrDefault();
                PlayniteApi.MainView.ActiveDesktopView = DesktopView.Details;
                if (game != null)
                {
                    PlayniteApi.MainView.SelectGame(game.Id);
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Fluent 11: failed to show details of the random game.");
            }
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is Window window))
            {
                return;
            }

            if (MediaViewer.FullScreenTag.Equals(window.Tag))
            {
                return;
            }

            if (exiting && window.GetType().Name == "ProgressWindow")
            {
                window.ShowInTaskbar = false;
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Left = -32000;
                window.Top = -32000;
                return;
            }

            if (currentDark == true)
            {
                SetDarkFrame(window, true);
            }

            if (micaActive)
            {
                ApplyMica(window);
            }

            if (settings.Settings.ReplaceHltbWindow)
            {
                HltbSelectDialog.TryApply(window);
            }

            if (settings.Settings.CenterWindows)
            {
                WindowCentering.Apply(window);
            }

            WindowPolish.Apply(window);

            if (window.GetType().Name == "SearchWindow")
            {
                MenuPolish.WatchSearch(window);
            }

            if (window.GetType().Name == "MessageBoxWindow")
            {
                FixMessageBoxIcon(window);
                MessageDialog.Apply(window);
            }

            if (window.GetType().Name == "ProgressWindow")
            {
                ProgressDialog.Apply(window);
            }

            if (window.GetType().Name == RandomGameDialog.WindowType)
            {
                RandomGameDialog.Apply(window, OnRandomGameDetails);
            }

            if (settings.Settings.ReplaceAchievementsWindow)
            {
                AchievementsGameView.TryApply(window, themeDir);
                AchievementsRefreshDialog.TryApply(window, micaActive);
            }
            else if (window.Content?.GetType().Name == "ViewAchievementsControl")
            {
                logger.Info("Fluent 11: achievements window style is switched off in the settings.");
            }

            if (window.GetType().Name == "GameEditWindow")
            {
                window.AddHandler(Selector.SelectionChangedEvent, new SelectionChangedEventHandler((s, a) =>
                    window.Dispatcher.BeginInvoke(new Action(() => RoundImages(window)), DispatcherPriority.ContextIdle)), true);
                window.Dispatcher.BeginInvoke(new Action(() => RoundImages(window)), DispatcherPriority.ContextIdle);
                StyleActionCards(window);
            }

            if (window.GetType().Name == "UpdateWindow")
            {
                window.Dispatcher.BeginInvoke(new Action(() => FixChangelogColors(window)), DispatcherPriority.ContextIdle);
            }
        }

        private void OnTextBlockSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!(sender is TextBlock text))
            {
                return;
            }

            if (WindowPolish.TryRestyleFullscreenNote(text))
            {
                return;
            }

            if (text.Text == IcoFontBackArrow)
            {
                text.Text = string.Empty;
                RestyleSideViewHeader(text);
            }
        }

        private static void RestyleSideViewHeader(TextBlock arrow)
        {
            try
            {
                var link = (arrow.Parent as System.Windows.Documents.InlineUIContainer)?.Parent as System.Windows.Documents.Hyperlink;
                var outer = link?.Parent as TextBlock;
                if (outer == null)
                {
                    return;
                }

                outer.Visibility = Visibility.Collapsed;
                if (!(outer.Parent is StackPanel row) || row.Orientation != Orientation.Horizontal)
                {
                    return;
                }

                row.Margin = new Thickness(0);
                row.Height = 48;
                var title = row.Children.OfType<TextBlock>().FirstOrDefault(a => a != outer);
                if (title != null)
                {
                    title.SetResourceReference(TextBlock.FontSizeProperty, "FontSizeLarger");
                    title.FontWeight = FontWeights.SemiBold;
                    title.Margin = new Thickness(4, 0, 0, 0);
                    title.VerticalAlignment = VerticalAlignment.Center;
                }

                MoveHeaderToggle(row);
            }
            catch (Exception e)
            {
                logger.Error(e, "Fluent 11: failed to restyle page header.");
            }
        }

        private static void MoveHeaderToggle(StackPanel row)
        {
            var toggle = row.Children.OfType<CheckBox>().FirstOrDefault();
            if (toggle == null || !(row.Parent is DockPanel dock))
            {
                return;
            }

            var index = dock.Children.IndexOf(row);
            row.Children.Remove(toggle);
            dock.Children.Remove(row);

            toggle.SetResourceReference(FrameworkElement.StyleProperty, "Fluent11HeaderToggle");
            toggle.Margin = new Thickness(0, 0, 146, 0);
            toggle.HorizontalAlignment = HorizontalAlignment.Right;

            var header = new Grid();
            DockPanel.SetDock(header, Dock.Top);
            header.Children.Add(row);
            header.Children.Add(toggle);
            dock.Children.Insert(index, header);
        }

        private void OnListBoxSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.PreviousSize.Height > 0 || !(sender is ListBox list) || list.Name != "PART_ListGames")
            {
                return;
            }

            if (list.TemplatedParent?.GetType().Name == "LibraryGridView")
            {
                gridOverlay?.Attach(list);
                return;
            }

            if (list.TemplatedParent?.GetType().Name != "LibraryDetailsView")
            {
                return;
            }

            if (!(bool)list.GetValue(IsFirstGameWatchedProperty))
            {
                list.SetValue(IsFirstGameWatchedProperty, true);
                list.IsVisibleChanged += (s, a) =>
                {
                    if (true.Equals(a.NewValue))
                    {
                        SelectFirstGame(list);
                    }
                };
            }

            SelectFirstGame(list);
        }

        private static void SelectFirstGame(ListBox list)
        {
            list.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (list.IsVisible && list.SelectedItem == null && list.Items.Count > 0)
                {
                    list.SelectedIndex = 0;
                    list.ScrollIntoView(list.Items[0]);
                }
            }), DispatcherPriority.Loaded);
        }

        private static void FixMessageBoxIcon(Window window)
        {
            try
            {
                if (!(window.FindName("ViewIcon") is Viewbox box) || !(box.Child is TextBlock icon))
                {
                    return;
                }

                var kind = window.GetType().GetProperty("DisplayIcon")?.GetValue(window) as MessageBoxImage?;
                string glyph;
                string brushKey;
                switch (kind)
                {
                    case MessageBoxImage.Question:
                        glyph = "\ue9ce"; brushKey = "AccentBrush"; break;
                    case MessageBoxImage.Information:
                        glyph = "\ue946"; brushKey = "AccentBrush"; break;
                    case MessageBoxImage.Warning:
                        glyph = "\ue7ba"; brushKey = "CautionBrush"; break;
                    case MessageBoxImage.Error:
                        glyph = "\uea39"; brushKey = "CriticalBrush"; break;
                    default:
                        return;
                }

                icon.SetResourceReference(TextBlock.FontFamilyProperty, "FluentIconFont");
                icon.SetResourceReference(TextBlock.ForegroundProperty, brushKey);
                icon.Text = glyph;
                box.MaxWidth = 28;
                box.MaxHeight = 28;
            }
            catch (Exception e)
            {
                logger.Error(e, "Fluent 11: failed to replace message box icon.");
            }
        }

        private const double ListViewIconSize = 28;

        private static void OnImageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!(sender is Image image) || image.Width != 20 || (bool)image.GetValue(IsRoundedProperty) || !IsInListView(image))
            {
                return;
            }

            image.Width = ListViewIconSize;
            image.Height = ListViewIconSize;
            image.SetValue(IsRoundedProperty, true);
            image.SizeChanged += (s, a) => ApplyRoundClip(image);
            ApplyRoundClip(image);
        }

        private static bool IsInListView(DependencyObject element)
        {
            for (var parent = VisualTreeHelper.GetParent(element); parent != null; parent = VisualTreeHelper.GetParent(parent))
            {
                var name = parent.GetType().Name;
                if (name == "GamesGridView")
                {
                    return true;
                }

                if (parent is Window)
                {
                    return false;
                }
            }

            return false;
        }

        private static readonly DependencyProperty IsFirstGameWatchedProperty =
            DependencyProperty.RegisterAttached("IsFirstGameWatched", typeof(bool), typeof(Fluent11Helper), new PropertyMetadata(false));

        private static readonly DependencyProperty IsRoundedProperty =
            DependencyProperty.RegisterAttached("IsRounded", typeof(bool), typeof(Fluent11Helper), new PropertyMetadata(false));

        private static void RoundImages(Window window)
        {
            foreach (var image in FindVisualChildren(window).OfType<Image>())
            {
                if ((bool)image.GetValue(IsRoundedProperty))
                {
                    continue;
                }

                image.SetValue(IsRoundedProperty, true);
                image.SizeChanged += (s, a) => ApplyRoundClip(image);
                ApplyRoundClip(image);
            }
        }

        public const string ActionListName = "OtherTasksItems";

        private static void StyleActionCards(Window window)
        {
            if (!(window.FindName(ActionListName) is ItemsControl list))
            {
                return;
            }

            list.ItemContainerGenerator.StatusChanged += (s, a) =>
            {
                if (list.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                {
                    list.Dispatcher.BeginInvoke(new Action(() => StyleActionCards(list)), DispatcherPriority.Loaded);
                }
            };
            list.Dispatcher.BeginInvoke(new Action(() => StyleActionCards(list)), DispatcherPriority.Loaded);
        }

        private static void StyleActionCards(ItemsControl list)
        {
            for (var i = 0; i < list.Items.Count; i++)
            {
                if (!(list.ItemContainerGenerator.ContainerFromIndex(i) is ContentPresenter presenter))
                {
                    continue;
                }

                presenter.ApplyTemplate();
                if (VisualTreeHelper.GetChildrenCount(presenter) == 0 || !(VisualTreeHelper.GetChild(presenter, 0) is Border border)
                    || (bool)border.GetValue(IsRoundedProperty))
                {
                    continue;
                }

                border.SetValue(IsRoundedProperty, true);
                border.SetResourceReference(Border.BackgroundProperty, "SurfaceCardBrush");
                border.SetResourceReference(Border.BorderBrushProperty, "CardStrokeBrush");
                border.SetResourceReference(Border.CornerRadiusProperty, "CardCornerRadius");
            }
        }

        internal static void ApplyRoundClip(Image image)
        {
            var width = image.ActualWidth;
            var height = image.ActualHeight;
            if (width <= 0 || height <= 0)
            {
                image.Clip = null;
                return;
            }

            var radius = Math.Min(8, Math.Min(width, height) * 0.12);
            var clip = new RectangleGeometry(new Rect(0, 0, width, height), radius, radius);
            clip.Freeze();
            image.Clip = clip;
        }

        private void FixChangelogColors(Window window)
        {
            try
            {
                if (!(Application.Current.TryFindResource("TextColor") is Color textColor))
                {
                    return;
                }

                var css = $"color: #{textColor.R:X2}{textColor.G:X2}{textColor.B:X2};";
                foreach (var panel in FindVisualChildren(window).Where(a => a.GetType().Name == "HtmlPanel"))
                {
                    var textProperty = panel.GetType().GetProperty("Text");
                    if (textProperty?.GetValue(panel) is string html)
                    {
                        var fixedHtml = Regex.Replace(html, @"color:\s*#f2f2f2;?", css, RegexOptions.IgnoreCase);
                        if (fixedHtml != html)
                        {
                            textProperty.SetValue(panel, fixedHtml);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Fluent 11: failed to adjust changelog color.");
            }
        }

        private static IEnumerable<DependencyObject> FindVisualChildren(DependencyObject parent)
        {
            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                yield return child;
                foreach (var descendant in FindVisualChildren(child))
                {
                    yield return descendant;
                }
            }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        private static void SetDarkFrame(Window window, bool dark)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            var value = dark ? 1 : 0;
            if (DwmSetWindowAttribute(hwnd, 20, ref value, sizeof(int)) != 0)
            {
                DwmSetWindowAttribute(hwnd, 19, ref value, sizeof(int));
            }
        }
    }
}
