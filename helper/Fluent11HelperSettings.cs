using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace Fluent11Helper
{
    public enum ColorMode
    {
        Auto = 0,
        Light = 1,
        Dark = 2
    }

    public enum HltbSort
    {
        Remaining = 0,
        Progress = 1,
        Name = 2,
        Played = 3,
        TimeToBeat = 4,
        LastPlayed = 5
    }

    public enum StatsSort
    {
        Playtime = 0,
        LastPlayed = 1,
        Name = 2
    }

    public enum AchSort
    {
        LastUnlocked = 0,
        Progress = 1,
        Name = 2
    }

    public class Fluent11HelperSettings : ObservableObject
    {
        private ColorMode mode = ColorMode.Auto;
        public ColorMode Mode { get => mode; set => SetValue(ref mode, value); }

        private bool micaEnabled = true;
        public bool MicaEnabled { get => micaEnabled; set => SetValue(ref micaEnabled, value); }

        private bool acrylicBackdrop = false;
        public bool AcrylicBackdrop { get => acrylicBackdrop; set => SetValue(ref acrylicBackdrop, value); }

        private string acrylicTintDark = AcrylicTint.DefaultDark;
        public string AcrylicTintDark
        {
            get => acrylicTintDark;
            set => SetTint(ref acrylicTintDark, value);
        }

        private string acrylicTintLight = AcrylicTint.DefaultLight;
        public string AcrylicTintLight
        {
            get => acrylicTintLight;
            set => SetTint(ref acrylicTintLight, value);
        }

        private void SetTint(ref string field, string value, [System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            var normalized = AcrylicTint.Normalize(value);
            if (normalized == null)
            {
                OnPropertyChanged(name);
                return;
            }

            SetValue(ref field, normalized, name);
        }

        private bool mainAreaPanel = true;
        public bool MainAreaPanel { get => mainAreaPanel; set => SetValue(ref mainAreaPanel, value); }

        private bool windowsAccentColor = true;
        public bool WindowsAccentColor { get => windowsAccentColor; set => SetValue(ref windowsAccentColor, value); }

        private bool centerWindows = true;
        public bool CenterWindows { get => centerWindows; set => SetValue(ref centerWindows, value); }

        private bool hideMenuItems = false;
        public bool HideMenuItems { get => hideMenuItems; set => SetValue(ref hideMenuItems, value); }

        public List<string> HiddenMenuEntries { get; set; }

        private bool showHiddenGamesButton = false;
        public bool ShowHiddenGamesButton { get => showHiddenGamesButton; set => SetValue(ref showHiddenGamesButton, value); }

        private bool showFilterButton = true;
        public bool ShowFilterButton { get => showFilterButton; set => SetValue(ref showFilterButton, value); }

        private bool showAddonUpdateCount = true;
        public bool ShowAddonUpdateCount { get => showAddonUpdateCount; set => SetValue(ref showAddonUpdateCount, value); }

        private bool showFluent11Button = true;
        public bool ShowFluent11Button { get => showFluent11Button; set => SetValue(ref showFluent11Button, value); }

        [DontSerialize]
        public RelayCommand OpenSettingsCommand { get; } = new RelayCommand(SettingsPage.Open);

        private bool settingsPageOpen;
        [DontSerialize]
        public bool SettingsPageOpen { get => settingsPageOpen; set => SetValue(ref settingsPageOpen, value); }

        private bool showAddonsButton = true;
        public bool ShowAddonsButton { get => showAddonsButton; set => SetValue(ref showAddonsButton, value); }

        private bool screenshotStrip = true;
        public bool ScreenshotStrip { get => screenshotStrip; set => SetValue(ref screenshotStrip, value); }

        private bool showTrailer = true;
        public bool ShowTrailer { get => showTrailer; set => SetValue(ref showTrailer, value); }

        private bool trailerMicro = false;
        public bool TrailerMicro { get => trailerMicro; set => SetValue(ref trailerMicro, value); }

        private bool videoStartMuted = true;
        public bool VideoStartMuted { get => videoStartMuted; set => SetValue(ref videoStartMuted, value); }

        private bool systemRequirementsCard = false;
        public bool SystemRequirementsCard { get => systemRequirementsCard; set => SetValue(ref systemRequirementsCard, value); }

        private bool steamReviewsCard = false;
        public bool SteamReviewsCard { get => steamReviewsCard; set => SetValue(ref steamReviewsCard, value); }

        private bool fullDescription = false;
        public bool FullDescription { get => fullDescription; set => SetValue(ref fullDescription, value); }

        private bool fullDetails = false;
        public bool FullDetails { get => fullDetails; set => SetValue(ref fullDetails, value); }

        private bool screenshotTrimBars = false;
        public bool ScreenshotTrimBars { get => screenshotTrimBars; set => SetValue(ref screenshotTrimBars, value); }

        private bool acrylicMenus = true;
        public bool AcrylicMenus { get => acrylicMenus; set => SetValue(ref acrylicMenus, value); }

        private bool smootherScrolling = false;
        public bool SmootherScrolling { get => smootherScrolling; set => SetValue(ref smootherScrolling, value); }

        private bool screenshotFullScreen = true;
        public bool ScreenshotFullScreen { get => screenshotFullScreen; set => SetValue(ref screenshotFullScreen, value); }

        private bool showZoomButton = true;
        public bool ShowZoomButton { get => showZoomButton; set => SetValue(ref showZoomButton, value); }

        private bool gridDetailsOverlay = true;
        public bool GridDetailsOverlay
        {
            get => gridDetailsOverlay;
            set
            {
                SetValue(ref gridDetailsOverlay, value);
                OnPropertyChanged(nameof(GridPlayButtonAvailable));
            }
        }

        private bool gridOverlayFull = true;
        public bool GridOverlayFull { get => gridOverlayFull; set => SetValue(ref gridOverlayFull, value); }

        internal static bool OverlayActiveThisSession;
        [DontSerialize]
        public bool GridOverlayActive => OverlayActiveThisSession;

        private bool gridOverlayOpen;
        [DontSerialize]
        public bool GridOverlayOpen { get => gridOverlayOpen; set => SetValue(ref gridOverlayOpen, value); }

        private bool versionMismatch;
        [DontSerialize]
        public bool VersionMismatch { get => versionMismatch; set => SetValue(ref versionMismatch, value); }

        private IList<ShotThumb> screenshotThumbs = new List<ShotThumb>();
        [DontSerialize]
        public IList<ShotThumb> ScreenshotThumbs { get => screenshotThumbs; set { SetValue(ref screenshotThumbs, value); OnPropertyChanged(nameof(HasScreenshotThumbs)); } }
        [DontSerialize]
        public bool HasScreenshotThumbs => screenshotThumbs?.Count > 0;

        private string screenshotStripTitle = string.Empty;
        [DontSerialize]
        public string ScreenshotStripTitle { get => screenshotStripTitle; set => SetValue(ref screenshotStripTitle, value); }

        private RequirementsView requirements;
        [DontSerialize]
        public RequirementsView Requirements { get => requirements; set { SetValue(ref requirements, value); OnPropertyChanged(nameof(HasRequirements)); } }
        [DontSerialize]
        public bool HasRequirements => requirements != null;

        private ReviewsView reviews;
        [DontSerialize]
        public ReviewsView Reviews { get => reviews; set { SetValue(ref reviews, value); OnPropertyChanged(nameof(HasReviews)); } }
        [DontSerialize]
        public bool HasReviews => reviews != null;

        private bool posterTiles = false;
        public bool PosterTiles { get => posterTiles; set => SetValue(ref posterTiles, value); }

        private bool posterDetails = false;
        public bool PosterDetails { get => posterDetails; set => SetValue(ref posterDetails, value); }

        private bool gridFitWindow = true;
        public bool GridFitWindow { get => gridFitWindow; set => SetValue(ref gridFitWindow, value); }

        private int gridColumns = 6;
        public int GridColumns
        {
            get => gridColumns;
            set
            {
                SetValue(ref gridColumns, Math.Max(AutoColumns.MinColumns, Math.Min(AutoColumns.MaxColumns, value)));
                OnPropertyChanged(nameof(GridColumnsZoom));
                OnPropertyChanged(nameof(GridColumnsIndex));
            }
        }

        [DontSerialize]
        public double GridColumnsZoom
        {
            get => AutoColumns.MinColumns + AutoColumns.MaxColumns - GridColumns;
            set => GridColumns = AutoColumns.MinColumns + AutoColumns.MaxColumns - (int)Math.Round(value);
        }

        [DontSerialize]
        public int GridSizeIndex { get => GridFitWindow ? 1 : 0; set { GridFitWindow = value == 1; OnPropertyChanged(); } }

        [DontSerialize]
        public int GridColumnsIndex { get => GridColumns - AutoColumns.MinColumns; set => GridColumns = value + AutoColumns.MinColumns; }

        private bool showGridPlayButton = false;
        public bool ShowGridPlayButton { get => showGridPlayButton; set => SetValue(ref showGridPlayButton, value); }

        [DontSerialize]
        public bool GridPlayButtonAvailable => !GridDetailsOverlay;

        private bool showAchievementsPage = true;
        public bool ShowAchievementsPage { get => showAchievementsPage; set => SetValue(ref showAchievementsPage, value); }

        private bool hidePlayniteAchievementsEntry = false;
        public bool HidePlayniteAchievementsEntry { get => hidePlayniteAchievementsEntry; set => SetValue(ref hidePlayniteAchievementsEntry, value); }

        private bool showHltbPage = true;
        public bool ShowHltbPage { get => showHltbPage; set => SetValue(ref showHltbPage, value); }

        private bool showHltbEntry = true;
        [DontSerialize]
        public bool ShowHltbEntry { get => showHltbEntry; set => SetValue(ref showHltbEntry, value); }

        public HltbSort HltbSort { get; set; } = HltbSort.Remaining;
        public bool HltbSortDescending { get; set; } = false;
        public bool HltbInstalledOnly { get; set; } = false;
        public bool HltbHideFinished { get; set; } = false;
        public bool HltbHideNotStarted { get; set; } = false;
        public bool HltbIncludeHidden { get; set; } = false;
        public bool HltbIncludeNoData { get; set; } = false;

        public AchSort AchSort { get; set; } = AchSort.LastUnlocked;
        public bool AchSortDescending { get; set; } = true;
        public bool AchCompletedOnly { get; set; } = false;
        public bool AchShowHidden { get; set; } = false;
        public bool AchUnlocksByMonth { get; set; } = false;

        private bool showStatisticsPage = true;
        public bool ShowStatisticsPage { get => showStatisticsPage; set => SetValue(ref showStatisticsPage, value); }

        private bool hideStatisticsEntry = false;
        public bool HideStatisticsEntry { get => hideStatisticsEntry; set => SetValue(ref hideStatisticsEntry, value); }

        private bool listUseIcons = true;
        public bool ListUseIcons { get => listUseIcons; set => SetValue(ref listUseIcons, value); }

        private bool replaceAchievementsWindow = true;
        public bool ReplaceAchievementsWindow { get => replaceAchievementsWindow; set => SetValue(ref replaceAchievementsWindow, value); }

        private bool replaceHltbWindow = true;
        public bool ReplaceHltbWindow { get => replaceHltbWindow; set => SetValue(ref replaceHltbWindow, value); }

        public bool StatsIncludeHidden { get; set; } = false;
        public bool StatsIncludeUnplayed { get; set; } = false;
        public StatsSort StatsSort { get; set; } = StatsSort.Playtime;
        public bool StatsSortDescending { get; set; } = true;

        private bool sidebarExpanded = true;
        public bool SidebarExpanded { get => sidebarExpanded; set => SetValue(ref sidebarExpanded, value); }

        [DontSerialize]
        public int ColorModeIndex { get => (int)Mode; set { Mode = (ColorMode)value; OnPropertyChanged(); } }

        [DontSerialize]
        public int BackdropIndex
        {
            get => !MicaEnabled ? 0 : AcrylicBackdrop ? 2 : 1;
            set { MicaEnabled = value > 0; AcrylicBackdrop = value == 2; OnPropertyChanged(); OnPropertyChanged(nameof(AcrylicSelected)); }
        }

        [DontSerialize]
        public bool AcrylicSelected => MicaEnabled && AcrylicBackdrop;

        [DontSerialize]
        public int GridDetailsIndex
        {
            get => !GridDetailsOverlay ? 0 : GridOverlayFull ? 2 : 1;
            set
            {
                GridDetailsOverlay = value > 0;
                GridOverlayFull = value == 2;
                if (value > 0)
                {
                    ShowGridPlayButton = false;
                }

                OnPropertyChanged();
            }
        }

        [DontSerialize]
        public int ScreenshotIndex
        {
            get => !ScreenshotStrip ? 0 : ScreenshotFullScreen ? 2 : 1;
            set { ScreenshotStrip = value > 0; ScreenshotFullScreen = value == 2; OnPropertyChanged(); }
        }

        [DontSerialize]
        public int TrailerVideoIndex { get => TrailerMicro ? 1 : 0; set { TrailerMicro = value == 1; OnPropertyChanged(); } }

        [DontSerialize]
        public int ShowMoreIndex
        {
            get => (FullDescription ? 1 : 0) + (FullDetails ? 2 : 0);
            set { FullDescription = (value & 1) != 0; FullDetails = (value & 2) != 0; OnPropertyChanged(); }
        }

        [DontSerialize]
        public int StatisticsIndex
        {
            get => !ShowStatisticsPage ? 2 : HideStatisticsEntry ? 0 : 1;
            set { ShowStatisticsPage = value != 2; HideStatisticsEntry = value == 0; OnPropertyChanged(); }
        }

        [DontSerialize]
        public int AchievementsIndex
        {
            get => !ShowAchievementsPage ? 2 : HidePlayniteAchievementsEntry ? 0 : 1;
            set { ShowAchievementsPage = value != 2; HidePlayniteAchievementsEntry = value == 0; OnPropertyChanged(); }
        }

        [DontSerialize]
        public int TimeToBeatIndex
        {
            get => !ShowHltbPage ? 2 : ShowHltbEntry ? 1 : 0;
            set { ShowHltbPage = value != 2; ShowHltbEntry = value != 0; OnPropertyChanged(); }
        }

        [DontSerialize]
        public int ListImagesIndex { get => ListUseIcons ? 1 : 0; set { ListUseIcons = value == 1; OnPropertyChanged(); } }

        [DontSerialize]
        public int PosterIndex
        {
            get => !PosterTiles ? 0 : PosterDetails ? 2 : 1;
            set { PosterTiles = value > 0; PosterDetails = value == 2; OnPropertyChanged(); }
        }

        public void NotifyChoices()
        {
            OnPropertyChanged(nameof(TimeToBeatIndex));
        }
    }

    public class Fluent11HelperSettingsViewModel : ObservableObject, ISettings
    {
        private readonly Fluent11Helper plugin;
        private static readonly ILogger logger = LogManager.GetLogger();
        private Fluent11HelperSettings editingClone;
        private readonly string sessionRestartKey;

        private Fluent11HelperSettings settings;
        public Fluent11HelperSettings Settings { get => settings; set => SetValue(ref settings, value); }

        public Fluent11HelperSettingsViewModel(Fluent11Helper plugin)
        {
            this.plugin = plugin;
            Settings = plugin.LoadPluginSettings<Fluent11HelperSettings>() ?? new Fluent11HelperSettings();
            if (Settings.HiddenMenuEntries == null)
            {
                Settings.HiddenMenuEntries = Settings.HideMenuItems ? MenuPolish.MenuEntries.Select(a => a.Id).ToList() : new List<string>();
            }

            sessionRestartKey = RestartKey(Settings);
        }

        private static string RestartKey(Fluent11HelperSettings s)
        {
            return $"{s.GridDetailsOverlay}|{s.ShowStatisticsPage}|{s.ShowAchievementsPage}|{s.ShowHltbPage}";
        }

        public const string RestartFlag = "IsRestartRequired";

        private static void RequestRestart()
        {
            try
            {
                foreach (Window window in Application.Current.Windows)
                {
                    var property = window.DataContext?.GetType().GetProperty(RestartFlag, BindingFlags.Public | BindingFlags.Instance);
                    if (window.GetType().Name == "AddonsWindow" && property?.CanWrite == true && property.PropertyType == typeof(bool))
                    {
                        property.SetValue(window.DataContext, true);
                        return;
                    }
                }

                logger.Info("Fluent 11: restart needed, but settings were not saved from the add-ons window.");
            }
            catch (Exception e)
            {
                logger.Warn(e, "Fluent 11: could not request the restart prompt.");
            }
        }

        private bool? hltbEntryAtBegin;

        public void BeginEdit()
        {
            hltbEntryAtBegin = HltbSidebarEntry.Get(plugin.PlayniteApi);
            Settings.ShowHltbEntry = hltbEntryAtBegin ?? true;
            Settings.NotifyChoices();
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            editingClone.ScreenshotThumbs = Settings.ScreenshotThumbs;
            editingClone.Requirements = Settings.Requirements;
            editingClone.Reviews = Settings.Reviews;
            editingClone.GridOverlayOpen = Settings.GridOverlayOpen;
            editingClone.VersionMismatch = Settings.VersionMismatch;
            editingClone.SettingsPageOpen = Settings.SettingsPageOpen;
            editingClone.ShowHltbEntry = hltbEntryAtBegin ?? true;
            Settings = editingClone;
            DescriptionCollapse.RefreshAll();
            ChipsCollapse.RefreshAll();
        }

        public bool NeedsRestart => RestartKey(Settings) != sessionRestartKey;

        public void EndEdit()
        {
            EndEdit(true);
        }

        public void EndEdit(bool askRestart)
        {
            plugin.SavePluginSettings(Settings);
            var restartKey = RestartKey(Settings);
            if (askRestart && restartKey != RestartKey(editingClone) && restartKey != sessionRestartKey)
            {
                RequestRestart();
            }

            OnPropertyChanged(nameof(NeedsRestart));

            if (hltbEntryAtBegin.HasValue && hltbEntryAtBegin.Value != Settings.ShowHltbEntry)
            {
                HltbSidebarEntry.Set(plugin.PlayniteApi, Settings.ShowHltbEntry);
                hltbEntryAtBegin = Settings.ShowHltbEntry;
            }

            plugin.ApplyCurrentScheme();
            AddonUpdateBadge.Update(plugin.PlayniteApi);
            PosterCovers.RefreshAll();
            DescriptionCollapse.RefreshAll();
            ChipsCollapse.RefreshAll();
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}
