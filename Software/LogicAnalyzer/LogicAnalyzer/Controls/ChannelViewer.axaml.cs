using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Interactivity;
using LogicAnalyzer.Classes;
using SharedDriver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace LogicAnalyzer.Controls
{
    public partial class ChannelViewer : UserControl
    {
        TextBox[] boxes;

        AnalyzerChannel[] channels;
        public AnalyzerChannel[] Channels
        {
            get { return channels; }
            set
            {
                channels = value;
                CreateControls();
            }
        }

        public event EventHandler<ChannelEventArgs> ChannelClick;
        public event EventHandler UpdateChannels;
        public int pressedBuc = -1;

        public void PointerPressed(object? sender, PointerPressedEventArgs args)
        {
            var newChannelGrid = sender as Grid;
            this.pressedBuc = newChannelGrid.GetValue(Grid.RowProperty);
            args.Pointer.Capture(null);
            args.Handled = true;
        }

        public void PointerEntered(object? sender, PointerEventArgs args)
        {
            if (!args.GetCurrentPoint(null).Properties.IsLeftButtonPressed) {
                this.pressedBuc = -1;
            }

            if (this.pressedBuc == -1) {
                return;
            }

            var newChannelGrid = sender as Grid;
            var enteredBuc = newChannelGrid.GetValue(Grid.RowProperty);

            (channels[this.pressedBuc], channels[enteredBuc]) = (channels[enteredBuc], channels[this.pressedBuc]);
            this.pressedBuc = enteredBuc;

            if (UpdateChannels != null)
                UpdateChannels(this, EventArgs.Empty);

            args.Handled = true;
        }

        private void CreateControls()
        {
            ChannelGrid.Children.Clear();

            if (channels == null || channels.Length == 0)
                return;

            ChannelGrid.RowDefinitions.Clear();

            List<TextBox> newBoxes = new List<TextBox>();

            //ChannelGrid.BeginBatchUpdate();

            for (int vis = 0, buc = 0; buc < channels.Length; buc++)
            {
                //Create channel grid
                var newRowDefinition = new RowDefinition(channels[buc].Hidden ? GridLength.Auto : GridLength.Star);
                var newChannelGrid = new Grid();

                //Create new row
                ChannelGrid.RowDefinitions.Add(newRowDefinition);
                ChannelGrid.Children.Add(newChannelGrid);

                newChannelGrid.SetValue(Grid.RowProperty, buc);
                newChannelGrid.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
                newChannelGrid.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
                newChannelGrid.RowDefinitions = new RowDefinitions("*,*");
                newChannelGrid.IsVisible = !channels[buc].Hidden;
                newChannelGrid.Background = GraphicObjectsCache.GetBrush(AnalyzerColors.BgChannelColors[vis % 2]);
                newChannelGrid.PointerPressed += PointerPressed;
                newChannelGrid.PointerEntered += PointerEntered;
                if (!channels[buc].Hidden) vis++;

                var headerGrid = new Grid();
                headerGrid.ColumnDefinitions = new ColumnDefinitions("32,*");

                //Create eye icon
                var newChannelVisibility = new TextBlock();
                newChannelVisibility.FontFamily= FontFamily.Parse("avares://LogicAnalyzer/Assets/Fonts#Font Awesome 6 Free");
                newChannelVisibility.Text = "";
                newChannelVisibility.Margin = new Thickness(5, 0, 0, 0);
                newChannelVisibility.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
                newChannelVisibility.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
                newChannelVisibility.Foreground = GraphicObjectsCache.GetBrush(Colors.White);
                newChannelVisibility.Tag = channels[buc];
                newChannelVisibility.PointerPressed += (o, e) =>
                {
                    var channel = (o as TextBlock)?.Tag as AnalyzerChannel;

                    if (channel == null)
                        return;

                    channel.Hidden = true;

                    if (UpdateChannels != null)
                        UpdateChannels(this, EventArgs.Empty);

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                        e.Pointer.Capture(null);
                };

                headerGrid.Children.Add(newChannelVisibility);

                //Create label
                var newChannelLabel = new TextBlock();

                newChannelLabel.SetValue(Grid.RowProperty, 0);
                newChannelLabel.SetValue(Grid.ColumnProperty, 1);

                newChannelLabel.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
                newChannelLabel.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;

                newChannelLabel.Text = channels[buc].TextualChannelNumber;

                newChannelLabel.Foreground = GraphicObjectsCache.GetBrush(AnalyzerColors.GetChannelColor(channels[buc]));

                newChannelLabel.Tag = channels[buc];
                newChannelLabel.PointerPressed += NewChannelLabel_PointerPressed;

                headerGrid.Children.Add(newChannelLabel);


                newChannelGrid.Children.Add(headerGrid);

                //Create textbox
                var newChannelTextbox = new TextBox();
                newBoxes.Add(newChannelTextbox);

                newChannelTextbox.SetValue(Grid.RowProperty, 1);

                newChannelTextbox.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
                newChannelTextbox.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
                newChannelTextbox.Margin = new Thickness(5, 0, 5, 0);

                newChannelTextbox.Background = GraphicObjectsCache.GetBrush(AnalyzerColors.BgChannelColors[buc % 2]);
                newChannelTextbox.Foreground = GraphicObjectsCache.GetBrush(AnalyzerColors.TxtColor);

                newChannelTextbox.MinHeight = newChannelTextbox.MaxHeight = newChannelTextbox.Height = 18;
                newChannelTextbox.Padding = new Thickness(2);
                newChannelTextbox.BorderThickness = new Thickness(0);
                newChannelTextbox.FontSize = 10;
                newChannelTextbox.TextAlignment = TextAlignment.Center;
                newChannelTextbox.Text = channels[buc].ChannelName;
                newChannelTextbox.Tag = channels[buc];
                newChannelTextbox.GetPropertyChangedObservable(TextBox.TextProperty).Subscribe(NewChannelTextbox_TextChanged);
                newChannelGrid.Children.Add(newChannelTextbox);
            }

            boxes = newBoxes.ToArray();
        }

        public void UpdateChannelVisibility()
        {
            CreateControls();
        }

        private void NewChannelLabel_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            e.Handled = true;

            var label = sender as TextBlock;

            if (label == null)
                return;

            var channel = label.Tag as AnalyzerChannel;

            if(channel == null)
                return;

            if (ChannelClick == null)
                return;

            ChannelClick(sender , new ChannelEventArgs { Channel = channel });

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                e.Pointer.Capture(null);
        }

        void NewChannelTextbox_TextChanged(AvaloniaPropertyChangedEventArgs e)
        {
            ((e.Sender as TextBox).Tag as AnalyzerChannel).ChannelName = e.NewValue?.ToString();
        }

        public ChannelViewer()
        {
            InitializeComponent();
        }
    }

    public class ChannelEventArgs : EventArgs
    {
        public required AnalyzerChannel Channel { get; set; }
    }

    public class RegionEventArgs : EventArgs
    {
        public SampleRegion? Region { get; set; }
    }

    public class SamplesEventArgs : EventArgs
    {
        public int FirstSample { get; set; }
        public int SampleCount { get; set; }
    }

    public class SampleEventArgs : EventArgs
    {
        public int Sample { get; set; }
    }

    public class UserMarkerEventArgs : EventArgs
    {
        public int? Position { get; set; }
    }
}
