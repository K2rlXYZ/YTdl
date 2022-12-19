using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Text.RegularExpressions;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using System.Threading;
using YoutubeExplode.Converter;
using TagLib;
using System.IO;
using System.Collections.ObjectModel;

//next step watching videos in app, realsis ei lae faile alla

namespace YTv2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool initialized = false;
        IWebDriver driver;

        public MainWindow()
        {
            InitializeComponent();
            initialized = true;
            DataResults.Children.Clear();
            DataDownloads.Children.Clear();
            if (Properties.Settings.Default.destFolder == "") Properties.Settings.Default.destFolder = AppDomain.CurrentDomain.BaseDirectory;
            var chromeOptions = new ChromeOptions();
            //chromeOptions.AddArguments("headless");
            var chromeDriverService = ChromeDriverService.CreateDefaultService();
            chromeDriverService.HideCommandPromptWindow = true;
            driver = new ChromeDriver(chromeDriverService, chromeOptions);

        }

        Brush textBlock_Base_Background;
        private void TextBlock_MouseEnter(object sender, MouseEventArgs e)
        {
            var s = sender as TextBlock;
            var p = s.Parent as Border;
            textBlock_Base_Background = p.Background;
            p.Background = new SolidColorBrush(Color.FromRgb(160, 160, 160));
        }

        private void TextBlock_MouseLeave(object sender, MouseEventArgs e)
        {
            var s = sender as TextBlock;
            var p = s.Parent as Border;
            p.Background = textBlock_Base_Background;
        }

        #region windowbar
        private void Minimize_TextBlock_MouseDown(object sender, MouseButtonEventArgs e) => this.WindowState = WindowState.Minimized;

        private void Exit_TextBlock_MouseDown(object sender, RoutedEventArgs e)
        {
            driver.Quit();
            System.Windows.Application.Current.Shutdown();
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e) => DragMove();
        #endregion

        #region selectionbar
        string selected = "search";
        private void ChangeSelected()
        {
            textBlock_Base_Background = new SolidColorBrush(Color.FromRgb(96, 96, 96));
            switch (selected)
            {
                case "search":
                    SelectionBarSearch.Background = new SolidColorBrush(Color.FromRgb(96, 96, 96));
                    SelectionBarResult.Background = null;
                    SelectionBarDownloads.Background = null;

                    GridSearchPage.Visibility = Visibility.Visible;
                    GridResultsPage.Visibility = Visibility.Hidden;
                    GridDownloadsPage.Visibility = Visibility.Hidden;
                    break;
                case "results":
                    SelectionBarSearch.Background = null;
                    SelectionBarResult.Background = new SolidColorBrush(Color.FromRgb(96, 96, 96));
                    SelectionBarDownloads.Background = null;

                    GridSearchPage.Visibility = Visibility.Hidden;
                    GridResultsPage.Visibility = Visibility.Visible;
                    GridDownloadsPage.Visibility = Visibility.Hidden;
                    break;
                case "downloads":
                    SelectionBarSearch.Background = null;
                    SelectionBarResult.Background = null;
                    SelectionBarDownloads.Background = new SolidColorBrush(Color.FromRgb(96, 96, 96));

                    GridSearchPage.Visibility = Visibility.Hidden;
                    GridResultsPage.Visibility = Visibility.Hidden;
                    GridDownloadsPage.Visibility = Visibility.Visible;
                    break;

            }
        }

        private void Selection_TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var s = sender as TextBlock;
            if (s.Text == "Search") selected = "search";
            if (s.Text == "Results") selected = "results";
            if (s.Text == "Downloads") selected = "downloads";
            ChangeSelected();
        }
        #endregion

        #region search page
        private async void query_enter_pressed(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await search();
            }
        }

        private void Link_TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var s = sender as System.Windows.Controls.TextBox;
            if (s.Text == "Enter query or link") s.Text = "";
        }

        private void Link_TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var s = sender as System.Windows.Controls.TextBox;
            if (s.Text == "") s.Text = "Enter query or link";
        }

        private void Link_TextBox_GotFocusNR(object sender, RoutedEventArgs e)
        {
            var s = sender as System.Windows.Controls.TextBox;
            if (s.Text == "Number of results") s.Text = "";
        }

        private void Link_TextBox_LostFocusNR(object sender, RoutedEventArgs e)
        {
            var s = sender as System.Windows.Controls.TextBox;
            if (s.Text == "") s.Text = "Number of results";
        }

        [STAThread]
        private async void SearchPageButton_TextBlock_MouseDown(object sender, MouseButtonEventArgs e) => await search();

        List<List<object>> videos = new List<List<object>>(); //0-title, 1-videoid, 2-thumbnail link, 3-duration, 4-viewcount, 5-thumbnail

        [STAThread]
        private async Task search()
        {
            N = 0;
            videos.Clear();
            DataResults.Children.Clear();
            string query = "";
            Link_TextBox.Dispatcher.Invoke(new Action(() => query = Link_TextBox.Text));

            selected = "results";
            ChangeSelected();

            int maxres;
            if (NrResults_TextBox.Text == "" || NrResults_TextBox.Text == "Number of results") maxres = 20;
            else maxres = int.Parse(NrResults_TextBox.Text);
            Console.WriteLine(maxres);
            //PLAYLIST PART
            if (query.Contains("&list=") || query.Contains("?list="))
            {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
                await Task.Run(async () =>
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
                {
                        
                    driver.Navigate().GoToUrl("https://www.youtube.com/playlist?" + Regex.Match(query, "(?=list=)([^\n&])*(?=[\n&]|)").Value);
                    
                    try
                    {
                        List<IWebElement> elems = new List<IWebElement>();
                        foreach (var elem in driver.FindElements(By.TagName("button")))
                        {
                            elems.Add(elem);
                        }
                        var elemc = elems.Find(e => e.GetAttribute("aria-label") == "Accept all");
                        elemc.Click();
                    }
                    catch (Exception) { }
                    Thread.Sleep(100);

                    try
                    {
                        while (videos.Count < maxres)
                        {
                            bool all_done = true;
                            IWebElement contents = driver.FindElement(By.Id("contents")).FindElement(By.Id("contents")).FindElement(By.Id("contents"));
                            var grp = contents.FindElements(By.TagName("ytd-playlist-video-renderer"));
                            while (Math.Round((decimal)grp.Count / 100, 2) == (int)grp.Count / 100 && grp.Count < maxres)
                            {
                                grp = contents.FindElements(By.TagName("ytd-playlist-video-renderer"));
                                var element = grp[grp.Count-1];
                                Actions actions = new Actions(driver);
                                actions.MoveToElement(element);
                                actions.Perform();
                                IJavaScriptExecutor js = driver as IJavaScriptExecutor;
                                js.ExecuteScript("window.scrollBy(0,100);");
                                Thread.Sleep(1000);

                            }

                            for (int x = 0; x < maxres; x++)
                            {
                                if (grp.Count > x)
                                {
                                    List<object> video = new List<object>();
                                    video.Add(grp[x].FindElement(By.Id("video-title")).Text);
                                    video.Add(Regex.Match(grp[x].FindElement(By.Id("video-title")).GetAttribute("href"), "v=([^\n&])*(?=[\n&]|)").Value.Replace("v=", ""));
                                    video.Add("https://img.youtube.com/vi/" + Regex.Match(grp[x].FindElement(By.Id("video-title")).GetAttribute("href"), "v=([^\n&])*(?=[\n&]|)").Value.Replace("v=", "") + "/0.jpg");
                                    video.Add(grp[x].FindElement(By.Id("thumbnail")).FindElement(By.TagName("span")).Text);
                                    video.Add("");
                                    if (videos.Where(n=>n[0].ToString()==video[0].ToString()).Count() < 1)
                                    {
                                        all_done = false;
                                        videos.Add(video);
                                        Task.Run(() => { addVideo(video); });
                                    }

                                }
                            }
                            if (all_done)
                            {
                                break;
                            }
                        }
                    }
                    catch (NoSuchElementException)
                    {
                        MessageBox.Show("Unable to parse playlist, try again or give up.");
                        return;
                    }

                        
                });
            }
            //SINGLE SEARCH
            else if (Regex.Matches(query, "\\?v=([\\w\\d-_]*)").Count > 1)
            {
                foreach (Match match in Regex.Matches(query, "\\?v=([\\w\\d-_]*)"))
                {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
                    await Task.Run(async () =>
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
                    {
                        var link = match.Value;
                        link = link.Replace("?v=", "");
                        driver.Navigate().GoToUrl("https://www.youtube.com/results?search_query=" + link);
                        Thread.Sleep(100);

                        IWebElement contents = null;
                        try
                        {
                            contents = driver.FindElement(By.Id("page-manager"))
                                .FindElement(By.Id("container"))
                                .FindElement(By.Id("primary"))
                                .FindElement(By.Id("contents"));
                        }
                        catch (Exception)
                        {
                            MessageBox.Show("Try again.");
                            return;
                        }

                        try
                        {
                            var allgrp = contents.FindElements(By.TagName("ytd-item-section-renderer")); //all sections
                            var resgrp = allgrp[allgrp.Count - 1];//last section
                            var grp = resgrp.FindElement(By.Id("contents")).FindElements(By.TagName("ytd-video-renderer")); //all videos in current last section

                            List<object> video = new List<object>();
                            video.Add(grp[0].FindElement(By.TagName("yt-formatted-string")).Text);
                            video.Add(grp[0].FindElement(By.Id("thumbnail")).GetAttribute("href"));
                            video.Add(grp[0].FindElement(By.Id("thumbnail")).FindElement(By.TagName("img")).GetAttribute("src"));
                            if (video[2] == null || video[2].ToString() == "")
                            {
                                return;
                            }
                            video.Add(grp[0].FindElement(By.TagName("span")).Text);
                            video.Add(grp[0].FindElement(By.Id("metadata-line")).FindElement(By.TagName("span")).Text);
                            if (videos.Where(n => n[0].ToString() == video[0].ToString()).Count() < 1)
                            {
                                videos.Add(video);
                                Task.Run(() => { addVideo(video); });
                            }
                        }
                        catch { }
                    });
                }
            }
            //QUERY SEARCH
            else
            {
                await Task.Run(async () =>
                {
                    driver.Navigate().GoToUrl("https://www.youtube.com/results?search_query=" + HtmlAgilityPack.HtmlEntity.Entitize(query));
                    Thread.Sleep(100);

                    IWebElement contents = null;

                    try
                    {
                        contents = driver.FindElement(By.Id("page-manager"))
                            .FindElement(By.Id("container"))
                            .FindElement(By.Id("primary"))
                            .FindElement(By.Id("contents"));
                    }
                    catch(Exception)
                    {
                        MessageBox.Show("Try again.");
                        return;
                    }

                    while (videos.Count < maxres)
                    {
                        try 
                        {
                            var allgrp = contents.FindElements(By.TagName("ytd-item-section-renderer")); //all sections
                            var resgrp = allgrp[allgrp.Count - 1];//last section
                            var grp = resgrp.FindElement(By.Id("contents")).FindElements(By.TagName("ytd-video-renderer")); //all videos in current last section
                            for (int x = 0; x < Math.Floor((double)grp.Count / 3) + 1; x++)
                            {
                                if (grp.Count > x * 3 && videos.Count <= maxres)
                                {
                                    var element = grp[x * 3];
                                    Actions actions = new Actions(driver);
                                    actions.MoveToElement(element);
                                    actions.Perform();
                                }
                                else break;
                            }
                            bool all_done = true;
                            foreach (var vid in grp)
                            {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
                                await Task.Run(async () =>
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
                                {
                                    if (!grp.Contains(vid))
                                    {
                                        all_done = false;
                                    }
                                    List<object> video = new List<object>();
                                    video.Add(vid.FindElement(By.TagName("yt-formatted-string")).Text);
                                    video.Add(vid.FindElement(By.Id("thumbnail")).GetAttribute("href"));
                                    video.Add(vid.FindElement(By.Id("thumbnail")).FindElement(By.TagName("img")).GetAttribute("src"));
                                    if (video[2] == null || video[2].ToString() == "")
                                    {
                                        return;
                                    }
                                    video.Add(vid.FindElement(By.TagName("span")).Text);
                                    video.Add(vid.FindElement(By.Id("metadata-line")).FindElement(By.TagName("span")).Text);
                                    if (videos.Where(n => n[0].ToString() == video[0].ToString()).Count() < 1)
                                    {
                                        videos.Add(video);
                                        Task.Run(() => { addVideo(video); });
                                    }
                                });
                            }
                            if (all_done)
                            {
                                break;
                            }
                        }
                        catch{}
                    }
                });
            }
        }

        int N = 0;
        //RESULTS HANDLING
        [STAThread]
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task addVideo(List<object> video)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            Application.Current.Dispatcher.Invoke(new Action(() => 
            {
                Grid grid = new Grid();
                grid.Tag = video[1];
                grid.Height = 100;
                grid.Width = 972;
                grid.HorizontalAlignment = HorizontalAlignment.Left;
                grid.VerticalAlignment = VerticalAlignment.Top;
                Thickness margin = grid.Margin; margin.Left = 15; margin.Top = 15 + N * (100 + 15); margin.Bottom = 0; margin.Right = 0;
                N++;
                grid.Margin = margin;
                grid.Background = new SolidColorBrush(Color.FromRgb(64, 64, 64));

                TextBlock textBlock = new TextBlock();
                textBlock.HorizontalAlignment = HorizontalAlignment.Left;
                textBlock.VerticalAlignment = VerticalAlignment.Top;
                textBlock.Width = 794;
                textBlock.Height = 50;
                margin.Left = 178; margin.Top = 0;
                textBlock.Margin = margin;
                textBlock.TextWrapping = TextWrapping.Wrap;
                textBlock.FontFamily = new FontFamily("Arial");
                textBlock.FontWeight = FontWeights.Bold;
                textBlock.FontSize = 20;
                textBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 95, 31));
                Thickness padding = textBlock.Padding;
                padding.Left = 5; padding.Top = 5; padding.Right = 0; padding.Bottom = 0;
                textBlock.Padding = padding;
                textBlock.Text = video[0].ToString();
                grid.Children.Add(textBlock);

                TextBlock textBlock1 = new TextBlock();
                textBlock1.HorizontalAlignment = HorizontalAlignment.Left;
                textBlock1.VerticalAlignment = VerticalAlignment.Top;
                textBlock1.Width = 106;
                textBlock1.Height = 50;
                margin.Top = 50;
                textBlock1.Margin = margin;
                textBlock1.Margin = margin;
                textBlock1.TextWrapping = TextWrapping.NoWrap;
                textBlock1.FontFamily = new FontFamily("Arial");
                textBlock1.FontWeight = FontWeights.Bold;
                textBlock1.FontSize = 20;
                textBlock1.Foreground = new SolidColorBrush(Color.FromRgb(255, 95, 31));
                padding.Left = 15; padding.Top = 15;
                textBlock1.Padding = padding;
                textBlock1.Text = video[3].ToString();
                grid.Children.Add(textBlock1);

                TextBlock textBlock2 = new TextBlock();
                textBlock2.HorizontalAlignment = HorizontalAlignment.Left;
                textBlock2.VerticalAlignment = VerticalAlignment.Top;
                textBlock2.Width = 106;
                textBlock2.Height = 50;
                margin.Top = 50; margin.Left = 286;
                textBlock2.Margin = margin;
                textBlock2.Margin = margin;
                textBlock2.TextWrapping = TextWrapping.NoWrap;
                textBlock2.FontFamily = new FontFamily("Arial");
                textBlock2.FontWeight = FontWeights.Bold;
                textBlock2.FontSize = 20;
                textBlock2.Foreground = new SolidColorBrush(Color.FromRgb(255, 95, 31));
                padding.Left = 15; padding.Top = 15;
                textBlock2.Padding = padding;
                textBlock2.Text = video[4].ToString();
                grid.Children.Add(textBlock2);

                Border border = new Border();
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(96, 96, 96));
                Thickness borderThickness = border.BorderThickness;
                borderThickness.Left = 1; borderThickness.Right = 1; borderThickness.Top = 1; borderThickness.Bottom = 1;
                border.BorderThickness = borderThickness;
                border.HorizontalAlignment = HorizontalAlignment.Left;
                border.VerticalAlignment = VerticalAlignment.Top;
                border.Height = 24;
                border.Width = 92;
                margin.Left = 880; margin.Top = 76;
                border.Margin = margin;
                TextBlock textBlock3 = new TextBlock();
                textBlock3.HorizontalAlignment = HorizontalAlignment.Left;
                textBlock3.VerticalAlignment = VerticalAlignment.Top;
                textBlock3.TextWrapping = TextWrapping.Wrap;
                textBlock3.Text = "Download";
                textBlock3.Height = 22;
                textBlock3.Width = 89;
                textBlock3.FontFamily = new FontFamily("Bauhaus 93");
                textBlock3.Foreground = new SolidColorBrush(Color.FromRgb(255, 95, 31));
                textBlock3.FontSize = 17;
                textBlock3.TextAlignment = TextAlignment.Center;
                textBlock3.MouseDown += SignleDownload_MouseDown;
                textBlock3.Tag = video[1];
                textBlock3.MouseEnter += TextBlock_MouseEnter;
                textBlock3.MouseLeave += TextBlock_MouseLeave;
                border.Child = textBlock3;
                grid.Children.Add(border);

                Image image = new Image();
                image.HorizontalAlignment = HorizontalAlignment.Left;
                image.VerticalAlignment = VerticalAlignment.Top;
                image.Stretch = Stretch.Fill;
                image.Height = 100;
                image.Width = 178;
                byte[] data = new byte[] { 0 };
                BitmapImage b = new BitmapImage();
                using (WebClient client = new WebClient())
                {
                    data = client.DownloadData(video[2].ToString());
                    using (var ms = new System.IO.MemoryStream(data))
                    {
                        b.Dispatcher.Invoke(new Action(() =>
                        {
                            b.BeginInit();
                            b.CacheOption = BitmapCacheOption.OnLoad; // here
                            b.StreamSource = ms;
                            b.EndInit();
                        }));
                    }
                }
                image.Source = b.Clone();
                video.Add(b.Clone());
                grid.Children.Add(image);


                grid.Tag = video[1].ToString();
                if (!DataResults.Children.Contains(grid)) DataResults.Children.Add(grid);
            }));
            Console.WriteLine(N);
        }

        #endregion

        #region results page
        int M = 0;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async void NrDownloads_Button_MouseDown(object sender, MouseButtonEventArgs e)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            int i = 0;
            foreach(var child in DataResults.Children)
            {
                if (i >= int.Parse(NrVideos_TextBox.Text)) break;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                downloadVideoAsync((child as Grid).Tag.ToString());
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                i++;
            }
        }

        private void NrVideos_TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var s = sender as System.Windows.Controls.TextBox;
            if (s.Text == "Nr of videos") s.Text = "";
        }
        private void NrVideos_TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var s = sender as System.Windows.Controls.TextBox;
            if (s.Text == "") s.Text = "Nr of videos";
        }
        private void NrVideos_TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex reg = new Regex("[^0-9]");
            e.Handled = reg.IsMatch(e.Text);
        }

        private void MP4checkbox_Checked(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                MP3checkbox.IsChecked = false;
                QualitiesComboBox.Visibility = Visibility.Visible;
            }
        }
        private void MP4checkbox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                MP3checkbox.IsChecked = true;
                QualitiesComboBox.Visibility = Visibility.Hidden;
            }
        }
        private void MP3checkbox_Checked(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                MP4checkbox.IsChecked = false;
                QualitiesComboBox.Visibility = Visibility.Hidden;
            }
        }
        private void MP3checkbox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                MP4checkbox.IsChecked = true;
                QualitiesComboBox.Visibility = Visibility.Visible;
            }
        }

        private void ChangeDestinationFolder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.SelectedPath = Properties.Settings.Default.destFolder;
                var result = dialog.ShowDialog();
                if (result.ToString() == "OK")
                {
                    Properties.Settings.Default.destFolder = dialog.SelectedPath;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void SignleDownload_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var child = sender as TextBlock;
            string vId = child.Tag.ToString();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            downloadVideoAsync(vId);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }


        #endregion

        #region downloads page

        List<List<object>> downloads = new List<List<object>>(); //0-title, 1-videoid, 2-thumbnail link, 3-duration, 4-viewcount, 5-thumbnail

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async void ProgressBarChanged(object sender, EventArgs e)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var pb = sender as ProgressBar;
            if (pb.Value >= pb.Maximum-0.001)
            {
                foreach(var child in (pb.Parent as Grid).Children)
                {
                    if(child as Border != null)
                    {
                        (child as Border).Visibility = Visibility.Visible;
                    }
                }
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async void Clear(object sender, EventArgs e)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var tb = sender as TextBlock;
            var b = tb.Parent as Border;
            var grid = b.Parent as Grid;
            double marginTop = grid.Margin.Top;
            var papi = grid.Parent as Grid;
            if (grid.Tag != null && downloads.Where(n => n[1].ToString() == grid.Tag.ToString()).Count() > 0) downloads.Remove(downloads.Where(n => n[1].ToString() == grid.Tag.ToString()).First());
            papi.Children.Remove(grid);
            foreach (var child in papi.Children)
            {
                var grid1 = child as Grid;
                if (grid1 != null && grid1.Margin.Top > marginTop)
                {
                    var mar = grid1.Margin;
                    mar.Top -= 115;
                    grid1.Margin = mar;
                }
            }
            M -= 1;
        }
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async void Cancel(object sender, EventArgs e)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var tb = sender as TextBlock;
            ((tb).Tag as CancellationTokenSource).Cancel();
            var b = tb.Parent as Border;
            var grid = b.Parent as Grid;
            double marginTop = grid.Margin.Top;
            var papi = grid.Parent as Grid;
            papi.Children.Remove(grid);
            foreach(var child in papi.Children)
            {
                var grid1 = child as Grid;
                if(grid1 != null && grid1.Margin.Top > marginTop)
                {
                    var mar = grid1.Margin;
                    mar.Top -= 115;
                    grid1.Margin = mar;
                }
            }
            M -= 1;
        }

        [STAThread]
        private async Task downloadVideoAsync(string vId)
        {
            var video = videos.First(n => n[1].ToString() == vId);
            downloads.Add(video);
            Grid grid = new Grid();
            grid.Height = 100;
            grid.Width = 972;
            grid.HorizontalAlignment = HorizontalAlignment.Left;
            grid.VerticalAlignment = VerticalAlignment.Top;
            Thickness margin = grid.Margin; margin.Left = 15; margin.Top = 15 + M * (100 + 15); margin.Bottom = 0; margin.Right = 0;
            M++;
            grid.Margin = margin;
            grid.Background = new SolidColorBrush(Color.FromRgb(64, 64, 64));

            TextBlock textBlock = new TextBlock();
            textBlock.HorizontalAlignment = HorizontalAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Top;
            textBlock.Width = 692;
            textBlock.Height = 50;
            margin.Left = 178; margin.Top = 0;
            textBlock.Margin = margin;
            textBlock.TextWrapping = TextWrapping.Wrap;
            textBlock.FontFamily = new FontFamily("Arial");
            textBlock.FontWeight = FontWeights.Bold;
            textBlock.FontSize = 20;
            textBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 95, 31));
            Thickness padding = textBlock.Padding;
            padding.Left = 5; padding.Top = 5; padding.Right = 0; padding.Bottom = 0;
            textBlock.Padding = padding;
            textBlock.Text = video[0].ToString();
            grid.Children.Add(textBlock);

            Image image = new Image();
            image.HorizontalAlignment = HorizontalAlignment.Left;
            image.VerticalAlignment = VerticalAlignment.Top;
            image.Stretch = Stretch.Fill;
            image.Height = 100;
            image.Width = 178;
            image.Source = (video[5] as BitmapImage).Clone();
            grid.Children.Add(image);

            TextBlock textBlock1 = new TextBlock();
            textBlock1.HorizontalAlignment = HorizontalAlignment.Left;
            textBlock1.VerticalAlignment = VerticalAlignment.Top;
            textBlock1.Width = 106;
            textBlock1.Height = 50;
            margin.Top = 50;
            textBlock1.Margin = margin;
            textBlock1.Margin = margin;
            textBlock1.TextWrapping = TextWrapping.NoWrap;
            textBlock1.FontFamily = new FontFamily("Arial");
            textBlock1.FontWeight = FontWeights.Bold;
            textBlock1.FontSize = 20;
            textBlock1.Foreground = new SolidColorBrush(Color.FromRgb(255, 95, 31));
            padding.Left = 15; padding.Top = 15;
            textBlock1.Padding = padding;
            textBlock1.Text = video[3].ToString();
            grid.Children.Add(textBlock1);

            //HorizontalAlignment="Left" Height="25" Margin="712,65,0,0" VerticalAlignment="Top" Width="250" Minimum="0" Maximum="100"
            ProgressBar progressBar = new ProgressBar();
            progressBar.HorizontalAlignment = HorizontalAlignment.Left;
            progressBar.Height = 22;
            progressBar.Width = 250;
            progressBar.Minimum = 0;
            progressBar.Maximum = 1;
            margin.Left = 712;
            margin.Top = 57;
            progressBar.Margin = margin;
            progressBar.ValueChanged += ProgressBarChanged;
            grid.Children.Add(progressBar);

            Border border = new Border();
            border.Visibility = Visibility.Hidden;
            border.BorderBrush = new SolidColorBrush(Color.FromRgb(96, 96, 96));
            Thickness borderThickness = border.BorderThickness;
            borderThickness.Left = 1; borderThickness.Right = 1; borderThickness.Top = 1; borderThickness.Bottom = 1;
            border.BorderThickness = borderThickness;
            border.HorizontalAlignment = HorizontalAlignment.Left;
            border.VerticalAlignment = VerticalAlignment.Top;
            border.Height = 24;
            border.Width = 92;
            margin.Left = 870; margin.Top = 17;
            border.Margin = margin;
            TextBlock textBlock3 = new TextBlock();
            textBlock3.HorizontalAlignment = HorizontalAlignment.Left;
            textBlock3.VerticalAlignment = VerticalAlignment.Top;
            textBlock3.TextWrapping = TextWrapping.Wrap;
            textBlock3.Text = "Clear";
            textBlock3.Height = 22;
            textBlock3.Width = 89;
            textBlock3.FontFamily = new FontFamily("Bauhaus 93");
            textBlock3.Foreground = new SolidColorBrush(Color.FromRgb(255, 95, 31));
            textBlock3.FontSize = 17;
            textBlock3.TextAlignment = TextAlignment.Center;
            textBlock3.MouseDown += Clear;
            textBlock3.MouseEnter += TextBlock_MouseEnter;
            textBlock3.MouseLeave += TextBlock_MouseLeave;
            border.Child = textBlock3;
            grid.Children.Add(border);

            Border border1 = new Border();
            border1.BorderBrush = new SolidColorBrush(Color.FromRgb(96, 96, 96));
            borderThickness.Left = 1; borderThickness.Right = 1; borderThickness.Top = 1; borderThickness.Bottom = 1;
            border1.BorderThickness = borderThickness;
            border1.HorizontalAlignment = HorizontalAlignment.Left;
            border1.VerticalAlignment = VerticalAlignment.Top;
            border1.Height = 24;
            border1.Width = 92;
            margin.Left = 870; margin.Top = 41;
            border1.Margin = margin;
            TextBlock textBlock4 = new TextBlock();
            textBlock4.HorizontalAlignment = HorizontalAlignment.Left;
            textBlock4.VerticalAlignment = VerticalAlignment.Top;
            textBlock4.TextWrapping = TextWrapping.Wrap;
            textBlock4.Text = "Cancel";
            textBlock4.Height = 22;
            textBlock4.Width = 89;
            textBlock4.FontFamily = new FontFamily("Bauhaus 93");
            textBlock4.Foreground = new SolidColorBrush(Color.FromRgb(255, 95, 31));
            textBlock4.FontSize = 17;
            textBlock4.TextAlignment = TextAlignment.Center;
            textBlock4.MouseDown += Cancel;
            textBlock4.MouseEnter += TextBlock_MouseEnter;
            textBlock4.MouseLeave += TextBlock_MouseLeave;
            var cancelSource = new CancellationTokenSource();
            textBlock4.Tag = cancelSource;
            border1.Child = textBlock4;
            grid.Children.Add(border1);

            DataDownloads.Dispatcher.Invoke(new Action(() => DataDownloads.Children.Add(grid)));

            var youtube = new YoutubeClient();
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(vId);
            string regexSearch = new string(System.IO.Path.GetInvalidFileNameChars()) + new string(System.IO.Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}?]", Regex.Escape(regexSearch)));
            var name = r.Replace(video[0].ToString(), "");
            var pb = new Progress<double>(p => progressBar.Value = p);

            try
            {
                if (MP4checkbox.IsChecked.Value)
                {
                    IStreamInfo audioStreamInfo = streamManifest.GetAudioStreams().GetWithHighestBitrate(); ;
                    IVideoStreamInfo videoStreamInfo = null;
                    int index = QualitiesComboBox.SelectedIndex;
                    try
                    {
                        videoStreamInfo = streamManifest.GetVideoStreams().First(s => s.VideoQuality.ToString() == QualitiesComboBox.SelectedItem.ToString());
                    }
                    catch (InvalidOperationException)
                    {
                        videoStreamInfo = streamManifest.GetVideoStreams().GetWithHighestVideoQuality();
                    }

                    var streamInfos = new IStreamInfo[] { audioStreamInfo, videoStreamInfo };
                    var rq = new ConversionRequestBuilder(Properties.Settings.Default.destFolder + "\\" + name + "." + "mp4").SetContainer("mp4").SetPreset(ConversionPreset.UltraFast).SetFFmpegPath("ffmpeg.exe").Build();
                    await youtube.Videos.DownloadAsync(streamInfos, rq, pb, cancelSource.Token);
                }
                else
                {
                    var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                    var rq = new ConversionRequestBuilder(Properties.Settings.Default.destFolder + "\\" + name + ".mp3").SetContainer("mp3").SetPreset(ConversionPreset.UltraFast).SetFFmpegPath("ffmpeg.exe").Build();
                    var streamInfos = new IStreamInfo[] { streamInfo };
                    await youtube.Videos.DownloadAsync(streamInfos, rq, pb, cancelSource.Token);

                    var tfile = TagLib.File.Create(Properties.Settings.Default.destFolder + "\\" + name + ".mp3");
                    TagLib.Picture pic = new TagLib.Picture();
                    pic.Type = TagLib.PictureType.FrontCover;
                    pic.Description = "Cover";
                    pic.MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg;
                    using (MemoryStream outStream = new MemoryStream()) using (MemoryStream ms = new MemoryStream())
                    {
                        BitmapEncoder enc = new BmpBitmapEncoder();
                        enc.Frames.Add(BitmapFrame.Create(video[5] as BitmapImage));
                        enc.Save(outStream);
                        System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                        ms.Position = 0;
                        TagLib.Id3v2.AttachedPictureFrame cover = new TagLib.Id3v2.AttachedPictureFrame
                        {
                            Type = TagLib.PictureType.FrontCover,
                            Description = "Cover",
                            MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg,
                            Data = TagLib.ByteVector.FromStream(ms),
                            TextEncoding = TagLib.StringType.UTF16
                        };
                        tfile.Tag.Pictures = new TagLib.IPicture[] { cover };

                    }
                    if (name.Contains('-'))
                    {
                        var artist = name.Split('-')[0].Trim();
                        tfile.Tag.Performers.Append(artist);
                        var title = name.Split('-')[1].Trim();
                        tfile.Tag.Title = title;
                    }
                    tfile.Tag.Comment = video[1].ToString();
                    tfile.Tag.Year = (uint)DateTime.Now.Year;
                    tfile.Save();


                }
            }
            catch(IOException)
            {
                return;
            }
            catch(TaskCanceledException)
            {
                return;
            }
        }

        #endregion
    }

}

