using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Fluent11Helper
{
    public class Fluent11HelperSettingsView : UserControl
    {
        public Fluent11HelperSettingsView()
        {
            if (Application.Current?.TryFindResource(Fluent11Helper.ThemeMarkerKey) == null)
            {
                var notice = new StackPanel { Margin = new Thickness(20) };
                notice.Children.Add(new TextBlock
                {
                    Text = "Fluent 11 Helper works only with the Fluent 11 theme. Choose it under Settings > Appearance, then restart Playnite.",
                    TextWrapping = TextWrapping.Wrap
                });
                var showAnyway = new Button
                {
                    Content = "Show settings anyway",
                    Opacity = 0.6,
                    Padding = new Thickness(12, 4, 12, 4),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, 12, 0, 0)
                };
                showAnyway.Click += (s, e) => Content = SettingsTabs();
                notice.Children.Add(showAnyway);
                Content = notice;
                return;
            }

            Content = SettingsTabs();
        }

        private static TabControl SettingsTabs()
        {
            var tabs = new TabControl { Margin = new Thickness(20, 12, 20, 20) };
            tabs.Items.Add(Tab("General", GeneralTab()));
            tabs.Items.Add(Tab("Library", LibraryTab()));
            tabs.Items.Add(Tab("Pages", PagesTab()));
            tabs.Items.Add(Tab("Experimental", ExperimentalTab()));
            return tabs;
        }

        private static StackPanel GeneralTab()
        {
            var panel = new StackPanel();
            Group(panel, "Appearance",
                Choice("\uE771", "Color scheme", "Light or dark Fluent 11 colors. Automatic follows the Windows app mode", "Settings.ColorModeIndex", null,
                    "Automatic", "Light", "Dark"),
                Choice("\uE7F4", "Window background", "Real Windows 11 materials. Mica is tinted by the desktop, Acrylic blurs what is behind the window",
                    "Settings.BackdropIndex", "Windows 11 22H2 or later",
                    "Off", "Mica", "Acrylic"),
                Dependent(ColorEntry("Acrylic tint, dark", "Color laid over the blur in the dark scheme, as #AARRGGBB",
                    "Settings.AcrylicTintDark", AcrylicTint.DefaultDark), "Settings.AcrylicSelected"),
                Dependent(ColorEntry("Acrylic tint, light", "Color laid over the blur in the light scheme, as #AARRGGBB",
                    "Settings.AcrylicTintLight", AcrylicTint.DefaultLight), "Settings.AcrylicSelected"),
                Row("\uE7C4", "Main area panel", "The tinted panel with a border behind the library and pages. Best turned off when using Acrylic",
                    "Settings.MainAreaPanel"),
                Row("\uE790", "Windows accent color", "Uses the accent color chosen in Windows instead of the default blue",
                    "Settings.WindowsAccentColor"),
                Row("\uE8A7", "Center windows", "Dialogs and add-on windows open centered over the Playnite window",
                    "Settings.CenterWindows"));

            Group(panel, "Navigation",
                Expandable(Heading("\uE700", "Sidebar entries", "Extra entries at the bottom of the sidebar, above Settings"), null,
                    Row(null, "Fluent 11", "Opens these settings", "Settings.ShowFluent11Button"),
                    Row(null, "Add-ons", "Opens the add-ons window", "Settings.ShowAddonsButton"),
                    Available(Row(null, "Add-on update count", "Number of available add-on updates on the Add-ons entry",
                        "Settings.ShowAddonUpdateCount"), "Settings.ShowAddonsButton")),
                Multi("\uE71C", "Top bar buttons", "Hidden games: shows only hidden games. Filter: Playnite's filter panel button",
                    s => Count(new[] { s.ShowHiddenGamesButton, s.ShowFilterButton }),
                    s => new object[]
                    {
                        Toggle("Hidden games (Fluent 11)", () => s.ShowHiddenGamesButton, v => s.ShowHiddenGamesButton = v),
                        Toggle("Filter", () => s.ShowFilterButton, v => s.ShowFilterButton = v)
                    }),
                Multi("\uF2C7", "Hide menu entries", "Optional entries of the main menu and the right-click menu of games",
                    s => HiddenCount(MenuPolish.MenuEntries.Count(a => s.HiddenMenuEntries.Contains(a.Id))),
                    s => MenuEntryItems(s)));
            return panel;
        }

        private static StackPanel LibraryTab()
        {
            var panel = new StackPanel();
            Group(panel, "Grid view",
                Choice("\uE90C", "Game details", "How details open when you click a cover", "Settings.GridDetailsIndex", null,
                    "None", "Overlay", "Full page"),
                Available(Row("\uE768", "Play/Install button on covers", "Shown when hovering a cover. Not available with Overlay or Full page",
                    "Settings.ShowGridPlayButton"), "Settings.GridPlayButtonAvailable"),
                Choice("\uF0E2", "Cover size", "Fit window: covers grow and shrink with the window, the zoom sets the number of columns",
                    "Settings.GridSizeIndex", null, "Fixed (Playnite default)", "Fit window"),
                Dependent(Choice(null, "Columns", "Covers side by side, also set with the zoom", "Settings.GridColumnsIndex", null,
                    Enumerable.Range(AutoColumns.MinColumns, AutoColumns.MaxColumns - AutoColumns.MinColumns + 1).Select(a => a.ToString()).ToArray()),
                    "Settings.GridFitWindow"),
                Row("\uE8A3", "Zoom button", "Magnifier at the bottom right to change the cover size or number of columns", "Settings.ShowZoomButton"));

            Group(panel, "Game details",
                Needs(Choice("\uF7ED", "Screenshots", "Screenshot thumbnails in the game details. Needs Screenshot Utilities", "Settings.ScreenshotIndex", null,
                    "Off", "Thumbnails", "Thumbnails, click opens viewer"), ScreenshotUtilities),
                Needs(Expandable(Row("\uE714", "Trailer", "Game trailer as the first thumbnail, plays on click. Needs Extra Metadata Loader",
                    "Settings.ShowTrailer",
                    "Uses the video Extra Metadata Loader downloaded for the game. Shown even without screenshots"),
                    "Settings.ShowTrailer",
                    Choice(null, "Video", "Micro trailers are short clips without sound. If the chosen one is missing, the other is used",
                        "Settings.TrailerVideoIndex",
                        "Extra Metadata Loader downloads micro trailers only when that is turned on in its settings",
                        "Trailer", "Micro trailer"),
                    Row(null, "Start muted", "Sound can be turned on in the player at any time. Also for videos among the screenshots",
                        "Settings.VideoStartMuted")), ExtraMetadataLoader),
                Choice("\uE70D", "Show more", "Long description and details are shortened with Show more, or always shown in full",
                    "Settings.ShowMoreIndex", null, "Shorten both", "Full description", "Full details", "Show everything"));
            return panel;
        }

        private static StackPanel PagesTab()
        {
            var panel = new StackPanel();
            Group(panel, "Insights",
                Choice("\uE8A1", "Insights page", "Fluent 11's own statistics page, Playnite's Statistics or both in the sidebar", "Settings.StatisticsIndex", null,
                    "Fluent 11", "Both", "Playnite only"));

            Group(panel, "Achievements",
                Needs(Choice("geo:FluentTrophyGeometry", "Achievements page", "Fluent 11's own page, the add-on's or both. Needs Playnite Achievements",
                    "Settings.AchievementsIndex", null,
                    "Fluent 11", "Both", "Playnite Achievements only"), PlayniteAchievements),
                Needs(Row("geo:FluentTrophyGeometry", "Achievements window", "Fluent 11 style for View Achievements of a game and the refresh progress", "Settings.ReplaceAchievementsWindow"),
                    PlayniteAchievements));

            Group(panel, "Time to Beat",
                Needs(Choice("geo:FluentHourglassGeometry", "Time to Beat page", "Fluent 11's own page, HowLongToBeat's or both. Needs HowLongToBeat",
                    "Settings.TimeToBeatIndex", "Linked to the HowLongToBeat sidebar option",
                    "Fluent 11", "Both", "HowLongToBeat only"), HowLongToBeat),
                Needs(Row("geo:FluentHourglassGeometry", "Time to Beat window", "Replaces View HowLongToBeat data of a game and restyles the search window", "Settings.ReplaceHltbWindow"),
                    HowLongToBeat));

            Group(panel, "Lists",
                Choice("\uE8FD", "List images", "Covers or game icons in the lists of these pages", "Settings.ListImagesIndex", null,
                    "Covers", "Icons"));
            return panel;
        }

        private static StackPanel ExperimentalTab()
        {
            var panel = new StackPanel();
            panel.Children.Add(Footer("In testing, may change or go away."));
            Group(panel, "Appearance",
                Choice("\uE8B9", "Poster covers (3:4)", "3:4 covers in desktop mode", "Settings.PosterIndex",
                    "Whatever aspect ratio is set in Playnite, so it can stay at 1:1, e.g. for a fullscreen theme. " +
                    "Own covers via right-click > Fluent 11 > Choose desktop cover, recommended size 600 × 800 px",
                    "Off", "Grid view", "Grid view and game details"),
                Row("\uEABC", "Acrylic menus", "Blurred background behind menus, like in Windows 11", "Settings.AcrylicMenus",
                    "Windows' own acrylic material behind right-click menus, submenus and dropdowns. Applies to menus opened after switching"));
            Group(panel, "Scrolling",
                Row("\uEC8F", "Smoother scrolling", "Momentum and overscroll, mainly for touchpads", "Settings.SmootherScrolling",
                    "Like in Windows 11. The mouse wheel eases out, and Fluent 11's own pages scroll like the library. " +
                    "Needs Smooth scrolling on in Playnite's settings. So far only tested with an Apple Magic Trackpad"));
            Group(panel, "Game details",
                Row("\uE950", "System requirements", "Minimum and recommended specs for Steam games", "Settings.SystemRequirementsCard",
                    "Shown below the description. Requested once per game from Steam's store API (appdetails) when its details are " +
                    "first opened, then kept. Games without requirements on Steam are asked again after 30 days"),
                Row("\uE8E1", "Steam reviews", "Steam rating and the most helpful reviews for Steam games", "Settings.SteamReviewsCard",
                    "Steam rating in place of Community score, plus a card with the most helpful positive and critical review. " +
                    "Three small requests per game to Steam's store (ajaxappreviews): the overall rating across all languages and the " +
                    "most helpful positive and critical English review of the past year. Only after a game stays selected for a moment, " +
                    "one game at a time. Kept for a week, then requested again"),
                Needs(Available(Row("\uE7A8", "Crop black bars", "Removes black bars from screenshot thumbnails. " +
                    "Needs Screenshots on in the Library tab", "Settings.ScreenshotTrimBars",
                    "May crop images that don't need it, e.g. very dark scenes"), "Settings.ScreenshotStrip"), ScreenshotUtilities));
            return panel;
        }

        private static IEnumerable<object> MenuEntryItems(Fluent11HelperSettings s)
        {
            foreach (var group in MenuPolish.MenuEntries.GroupBy(a => a.GameMenu))
            {
                if (group.Key)
                {
                    yield return new Separator();
                }

                yield return MenuHeader(group.Key ? "Game menu" : "Main menu");
                foreach (var entry in group)
                {
                    yield return Toggle(entry.DisplayName, () => s.HiddenMenuEntries.Contains(entry.Id), v =>
                    {
                        s.HiddenMenuEntries.Remove(entry.Id);
                        if (v)
                        {
                            s.HiddenMenuEntries.Add(entry.Id);
                        }
                    });
                }
            }
        }

        private static MenuItem MenuHeader(string text)
        {
            var label = new TextBlock { Text = text, FontWeight = FontWeights.SemiBold };
            label.SetResourceReference(TextBlock.ForegroundProperty, "TextBrushDarker");
            label.SetResourceReference(TextBlock.FontSizeProperty, "FontSizeSmall");
            return new MenuItem { Header = label, IsHitTestVisible = false, Focusable = false };
        }

        private static string Count(bool[] shown)
        {
            var count = shown.Count(a => a);
            return count == shown.Length ? "All shown" : count == 0 ? "None shown" : $"{count} of {shown.Length} shown";
        }

        private static string HiddenCount(int hidden)
        {
            return hidden == 0 ? "None hidden" : $"{hidden} hidden";
        }

        private static MenuItem Toggle(string header, Func<bool> get, Action<bool> set)
        {
            var item = new MenuItem { Header = header, IsCheckable = true, IsChecked = get(), StaysOpenOnClick = true };
            item.Click += (s, e) => set(item.IsChecked);
            return item;
        }

        private static void Group(StackPanel panel, string title, params FrameworkElement[] rows)
        {
            panel.Children.Add(Header(title, panel.Children.Count == 0));
            StackPanel card = null;
            foreach (var row in rows)
            {
                if (card == null || !(bool)row.GetValue(IsDependentProperty))
                {
                    card = new StackPanel();
                    panel.Children.Add(Card(card));
                }

                card.Children.Add(row);
            }
        }

        private const double CardPadding = 16;

        private static Border Card(UIElement content)
        {
            var card = new Border
            {
                Child = content,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(CardPadding, 4, CardPadding, 4),
                Margin = new Thickness(0, 0, 0, 4)
            };
            card.SetResourceReference(Border.BackgroundProperty, "SurfaceCardBrush");
            card.SetResourceReference(Border.BorderBrushProperty, "CardStrokeBrush");
            card.SetResourceReference(Border.CornerRadiusProperty, "ControlCornerRadius");
            return card;
        }

        private static FrameworkElement Row(string icon, string title, string description, string path, string tooltip = null)
        {
            var check = new CheckBox { Padding = new Thickness(0), HorizontalAlignment = HorizontalAlignment.Right };
            check.SetResourceReference(StyleProperty, "Fluent11ToggleSwitch");
            check.SetBinding(ToggleButton.IsCheckedProperty, new Binding(path) { Mode = BindingMode.TwoWay });
            return ControlRow(icon, title, description, check, tooltip);
        }

        private static FrameworkElement Choice(string icon, string title, string description, string path, string tooltip, params string[] options)
        {
            var combo = new ComboBox { ItemsSource = options, MinWidth = 200 };
            combo.SetBinding(Selector.SelectedIndexProperty, new Binding(path) { Mode = BindingMode.TwoWay });
            return ControlRow(icon, title, description, combo, tooltip);
        }

        private static FrameworkElement ColorEntry(string title, string description, string path, string defaultValue)
        {
            var box = new TextBox
            {
                Width = 120,
                MaxLength = 9,
                VerticalContentAlignment = VerticalAlignment.Center,
                ToolTip = $"#AARRGGBB, #RRGGBB or #RGB. Default {defaultValue}"
            };
            var binding = new Binding(path) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.LostFocus };
            box.SetBinding(TextBox.TextProperty, binding);

            void Commit() => box.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            void Reload() => box.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
            box.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    Commit();
                    Reload();
                    e.Handled = true;
                }
            };

            box.LostFocus += (s, e) => box.Dispatcher.BeginInvoke(new Action(Reload), DispatcherPriority.Input);

            var swatch = new Border
            {
                Width = 24,
                Height = 24,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 8, 0),
                CornerRadius = new CornerRadius(4),
                VerticalAlignment = VerticalAlignment.Center
            };
            swatch.SetResourceReference(Border.BorderBrushProperty, "ControlStrokeBrush");
            swatch.SetBinding(Border.BackgroundProperty, new Binding(path) { Mode = BindingMode.OneWay, Converter = TintBrushConverter.Instance });

            var reset = new Button { Content = "Reset", Margin = new Thickness(8, 0, 0, 0), Padding = new Thickness(10, 4, 10, 4) };
            reset.Click += (s, e) =>
            {
                box.Text = defaultValue;
                Commit();
                Reload();
            };

            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            panel.Children.Add(swatch);
            panel.Children.Add(box);
            panel.Children.Add(reset);
            return ControlRow(null, title, description, panel, null);
        }

        private sealed class TintBrushConverter : IValueConverter
        {
            public static readonly TintBrushConverter Instance = new TintBrushConverter();

            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                var text = AcrylicTint.Normalize(value as string);
                return text == null ? Brushes.Transparent : new SolidColorBrush((Color)ColorConverter.ConvertFromString(text));
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotSupportedException();
            }
        }

        private static FrameworkElement Multi(string icon, string title, string description, Func<Fluent11HelperSettings, string> summary,
            Func<Fluent11HelperSettings, IEnumerable<object>> items)
        {
            var text = new TextBlock { VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
            var chevron = new TextBlock { Text = "\uE70D", FontSize = 12, Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            chevron.SetResourceReference(TextBlock.FontFamilyProperty, "FluentIconFont");
            chevron.RenderTransform = new TranslateTransform();
            var content = new DockPanel();
            DockPanel.SetDock(chevron, Dock.Right);
            content.Children.Add(chevron);
            content.Children.Add(text);
            var button = new Button { Content = content, MinWidth = 200, HorizontalContentAlignment = HorizontalAlignment.Stretch };

            Fluent11HelperSettings Current() => (button.DataContext as Fluent11HelperSettingsViewModel)?.Settings;
            void Update()
            {
                var settings = Current();
                text.Text = settings != null ? summary(settings) : string.Empty;
            }

            button.DataContextChanged += (s, e) => Update();
            button.Loaded += (s, e) => Update();
            button.Click += (s, e) =>
            {
                var settings = Current();
                if (settings == null)
                {
                    return;
                }

                var menu = new ContextMenu { PlacementTarget = button, Placement = PlacementMode.Bottom };
                foreach (var item in items(settings))
                {
                    if (item is MenuItem entry && entry.IsCheckable)
                    {
                        entry.Click += (a, b) => Update();
                    }

                    menu.Items.Add(item);
                }

                AcrylicPopups.Prepare(menu);
                menu.IsOpen = true;
                NudgeChevron(chevron);
            };
            return ControlRow(icon, title, description, button, null);
        }

        private static void NudgeChevron(UIElement chevron)
        {
            if (!SystemParameters.ClientAreaAnimation || !(chevron.RenderTransform is TranslateTransform move))
            {
                return;
            }

            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            var nudge = new DoubleAnimationUsingKeyFrames();
            nudge.KeyFrames.Add(new EasingDoubleKeyFrame(3, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.08)), ease));
            nudge.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.3)), ease));
            move.BeginAnimation(TranslateTransform.YProperty, nudge);
        }

        private static Grid ControlRow(string icon, string title, string description, FrameworkElement control, string tooltip)
        {
            var grid = new Grid { Margin = new Thickness(0, 10, 0, 10) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var label = Label(icon, title, description);
            label.VerticalAlignment = VerticalAlignment.Center;
            grid.Children.Add(label);
            grid.IsEnabledChanged += (s, e) => label.Opacity = grid.IsEnabled ? 1 : 0.5;
            control.Margin = new Thickness(16, 0, 0, 0);
            control.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(control, 1);
            grid.Children.Add(control);
            if (!string.IsNullOrEmpty(tooltip))
            {
                grid.ToolTip = tooltip;
            }

            return grid;
        }

        private static FrameworkElement Label(string icon, string title, string description)
        {
            var text = Texts(title, description);
            if (string.IsNullOrEmpty(icon))
            {
                return text;
            }

            FrameworkElement symbol;
            if (icon.StartsWith("geo:", StringComparison.Ordinal))
            {
                var path = new System.Windows.Shapes.Path { Stretch = System.Windows.Media.Stretch.Uniform, Width = IconWidth, Height = IconWidth };
                path.SetResourceReference(System.Windows.Shapes.Path.DataProperty, icon.Substring(4));
                path.SetResourceReference(System.Windows.Shapes.Shape.FillProperty, "TextBrush");
                symbol = path;
            }
            else
            {
                var glyph = new TextBlock { Text = icon, FontSize = IconWidth, TextAlignment = TextAlignment.Center };
                glyph.SetResourceReference(TextBlock.FontFamilyProperty, "FluentIconFont");
                symbol = glyph;
            }

            symbol.Width = IconWidth;
            symbol.Margin = new Thickness(0, 0, IconGap, 0);
            symbol.VerticalAlignment = VerticalAlignment.Center;
            var row = new DockPanel();
            DockPanel.SetDock(symbol, Dock.Left);
            row.Children.Add(symbol);
            row.Children.Add(text);
            return row;
        }

        private const double IconWidth = 16;
        private const double IconGap = 16;

        private static StackPanel Texts(string title, string description)
        {
            var text = new StackPanel();
            text.Children.Add(new TextBlock { Text = title, TextWrapping = TextWrapping.Wrap });
            if (!string.IsNullOrEmpty(description))
            {
                var note = new TextBlock { Text = description, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) };
                note.SetResourceReference(TextBlock.FontSizeProperty, "FontSizeSmall");
                note.SetResourceReference(TextBlock.ForegroundProperty, "TextBrushDarker");
                text.Children.Add(note);
            }

            return text;
        }

        private const string ScreenshotUtilities = "ScreenshotUtilities";
        private const string PlayniteAchievements = "PlayniteAchievements";
        private const string HowLongToBeat = "HowLongToBeat";
        private const string ExtraMetadataLoader = "ExtraMetadataLoader";

        private static FrameworkElement Needs(FrameworkElement row, string addonAssembly)
        {
            var plugins = API.Instance?.Addons?.Plugins;
            if (plugins != null && !plugins.Any(a => a.GetType().Assembly.GetName().Name == addonAssembly))
            {
                row.IsEnabled = false;
            }

            return row;
        }

        private static FrameworkElement Available(FrameworkElement row, string conditionPath)
        {
            row.SetBinding(IsEnabledProperty, new Binding(conditionPath));
            return row;
        }

        private static readonly DependencyProperty IsDependentProperty =
            DependencyProperty.RegisterAttached("IsDependent", typeof(bool), typeof(Fluent11HelperSettingsView), new PropertyMetadata(false));

        private static FrameworkElement Expandable(FrameworkElement row, string enabledPath, params FrameworkElement[] options)
        {
            var head = new Grid();
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            head.Children.Add(row);
            var expand = new ToggleButton { Template = ExpandTemplate(), Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(expand, 1);
            head.Children.Add(expand);

            var body = new StackPanel { Margin = new Thickness(IconWidth + IconGap, 0, 0, 0), Visibility = Visibility.Collapsed };
            if (enabledPath != null)
            {
                body.SetBinding(IsEnabledProperty, new Binding(enabledPath));
            }

            foreach (var option in options)
            {
                var line = new Border { Height = 1, Margin = new Thickness(-(IconWidth + IconGap + CardPadding), 0, -CardPadding, 0) };
                line.SetResourceReference(Border.BackgroundProperty, "PanelSeparatorBrush");
                body.Children.Add(line);
                body.Children.Add(option);
            }

            if (row is Grid heading && heading.Tag as string == HeadingTag)
            {
                head.Background = Brushes.Transparent;
                head.MouseLeftButtonUp += (s, e) =>
                {
                    if (!expand.IsMouseOver)
                    {
                        expand.IsChecked = expand.IsChecked != true;
                    }
                };
            }

            expand.Checked += (s, e) => body.Visibility = Visibility.Visible;
            expand.Unchecked += (s, e) => body.Visibility = Visibility.Collapsed;
            var panel = new StackPanel();
            panel.Children.Add(head);
            panel.Children.Add(body);
            return panel;
        }

        private const string HeadingTag = "Heading";

        private static FrameworkElement Heading(string icon, string title, string description)
        {
            var label = Label(icon, title, description);
            label.VerticalAlignment = VerticalAlignment.Center;
            var grid = new Grid { Margin = new Thickness(0, 10, 0, 10), Tag = HeadingTag, MinHeight = 32 };
            grid.Children.Add(label);
            return grid;
        }

        private static ControlTemplate expandTemplate;
        private static ControlTemplate ExpandTemplate()
        {
            return expandTemplate ?? (expandTemplate = (ControlTemplate)System.Windows.Markup.XamlReader.Parse(
                "<ControlTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
                "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" TargetType=\"ToggleButton\">" +
                "<Border x:Name=\"Bg\" Width=\"32\" Height=\"32\" Background=\"Transparent\" CornerRadius=\"{DynamicResource ControlCornerRadius}\">" +
                "<TextBlock x:Name=\"Glyph\" Text=\"&#xE70D;\" FontSize=\"12\" FontFamily=\"{DynamicResource FluentIconFont}\" " +
                "Foreground=\"{DynamicResource TextBrush}\" HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\" /></Border>" +
                "<ControlTemplate.Triggers>" +
                "<Trigger Property=\"IsChecked\" Value=\"True\"><Setter TargetName=\"Glyph\" Property=\"Text\" Value=\"&#xE70E;\" /></Trigger>" +
                "<Trigger Property=\"IsMouseOver\" Value=\"True\"><Setter TargetName=\"Bg\" Property=\"Background\" Value=\"{DynamicResource SubtleHoverBrush}\" /></Trigger>" +
                "<Trigger Property=\"IsPressed\" Value=\"True\"><Setter TargetName=\"Bg\" Property=\"Background\" Value=\"{DynamicResource SubtlePressedBrush}\" /></Trigger>" +
                "<Trigger Property=\"IsEnabled\" Value=\"False\"><Setter Property=\"Opacity\" Value=\"0.5\" /></Trigger>" +
                "</ControlTemplate.Triggers></ControlTemplate>"));
        }

        private static FrameworkElement Dependent(FrameworkElement row, string parentPath)
        {
            row.SetValue(IsDependentProperty, true);
            row.Margin = new Thickness(IconWidth + IconGap, 0, 0, 8);
            row.SetBinding(IsEnabledProperty, new Binding(parentPath));
            return row;
        }

        private static TabItem Tab(string header, FrameworkElement content)
        {
            return new TabItem
            {
                Header = header,
                Content = new ScrollViewer
                {
                    Content = LimitWidth(content, 1200),
                    Padding = new Thickness(0, 8, 0, 0),
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
                }
            };
        }

        private static Grid LimitWidth(FrameworkElement content, double maxWidth)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MaxWidth = maxWidth });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Star) });
            grid.Children.Add(content);
            return grid;
        }

        private static TextBlock Header(string text, bool first)
        {
            return new TextBlock { Text = text, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, first ? 8 : 28, 0, 4) };
        }

        private static TextBlock Footer(string text)
        {
            var footer = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 24, 0, 0) };
            footer.SetResourceReference(TextBlock.FontSizeProperty, "FontSizeSmall");
            footer.SetResourceReference(TextBlock.ForegroundProperty, "TextBrushDarker");
            return footer;
        }
    }
}
