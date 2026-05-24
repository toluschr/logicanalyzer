using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using LogicAnalyzer.Classes;
using LogicAnalyzer.Interfaces;
using SharedDriver;
using SkiaSharp;
using System;

namespace LogicAnalyzer.Controls
{
    public partial class SamplePreviewer : UserControl, ISampleDisplay
    {
        Bitmap? bmp;
        int sampleCount = 0;

        int viewPosition;
        public int ViewPosition { get { return viewPosition; } set { viewPosition = value; InvalidateVisual(); } }

        public int FirstSample { get; private set; }

        public int VisibleSamples { get; private set; }

        public event EventHandler<PinnedEventArgs>? PinnedChanged;

        bool pinned = false;
        public bool Pinned { get { return pinned; } }

        public SamplePreviewer()
        {
            InitializeComponent();
        }

        public void UpdateSamples(AnalyzerChannel[] Channels, int SampleCount)
        {
            int channelCount = Channels.Length;

            if (channelCount > 24)
                channelCount = 24;

            int width = Math.Max(Math.Min(SampleCount, 4096), 1024);

            float cHeight = 144 / (float)channelCount;
            float sWidth = (float)width / (float)SampleCount;
            float high = cHeight / 6;
            float low = cHeight - high;

            using SKBitmap skb = new SKBitmap(width, 144);

            SKPaint[] colors = new SKPaint[channelCount];

            using (var canvas = new SKCanvas(skb))
            {
                for (int chan = 0; chan < channelCount; chan++)
                {
                    var avColor = AnalyzerColors.GetChannelColor(Channels[chan]);

                    colors[chan] = new SKPaint
                    {
                        Style = SKPaintStyle.Stroke,
                        StrokeWidth = 1,
                        Color = new SKColor(avColor.R, avColor.G, avColor.B)
                    };

                    int fr = 0;
                    int to = 0;

                    for (;;)
                    {
                        if (fr >= SampleCount) break;

                        // While the value is equal, increment [to].
                        do {
                            ++to;
                        } while (to < SampleCount && Channels[chan].Samples[fr] == Channels[chan].Samples[to]);

                        // now we know that just before [to], samples changed or there is no new sample.
                        // draw the line from [fr] to [to]
                        float oldY = chan * cHeight + ((Channels[chan].Samples[fr] != 0) ? high : low);
                        canvas.DrawLine(fr * sWidth, oldY, to * sWidth, oldY, colors[chan]);

                        // Draw the straight line
                        if (to != SampleCount) {
                            float newY = chan * cHeight + ((Channels[chan].Samples[to] != 0) ? high : low);
                            canvas.DrawLine(to * sWidth, oldY, to * sWidth, newY, colors[chan]);
                        }

                        fr = to;
                    }
                }
            }

            using var encoded = skb.Encode(SKEncodedImageFormat.Png, 1);
            using var stream = encoded.AsStream();

            if (bmp != null)
                bmp.Dispose();

            bmp = new Bitmap(stream);
            sampleCount = SampleCount;
        }

        public override void Render(DrawingContext context)
        {
            //base.Render(context);
            var bounds = new Avalonia.Rect(0, 0, this.Bounds.Width, this.Bounds.Height);

            context.FillRectangle(GraphicObjectsCache.GetBrush(Color.Parse("#222222")), bounds);

            if (sampleCount == 0 || bmp == null)
                return;

            //Test quality!!!
            (bmp as IImage).Draw(context, new Avalonia.Rect(bmp.Size), bounds);

            float ratio = (float)bounds.Size.Width / (float)sampleCount;
            float pos = viewPosition * ratio;

            Rect rcVisible = new Rect(FirstSample * ratio, 0, VisibleSamples * ratio, bounds.Height);
            context.FillRectangle(GraphicObjectsCache.GetBrush(Color.FromArgb(32, 255, 255, 255)), rcVisible);

            /*
            context.DrawLine(GraphicObjectsCache.GetPen(Colors.White, 1, DashStyle.Dash), new Avalonia.Point(pos, 0), new Avalonia.Point(pos, 143));
            */
        }

        public void UpdateVisibleSamples(int FirstSample, int VisibleSamples)
        {
            this.FirstSample = FirstSample;
            this.VisibleSamples = VisibleSamples;
            InvalidateVisual();
        }

        private void TextBlock_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            pinned = !pinned;
            lblPin.Text = pinned ? "" : "";

            PinnedChanged?.Invoke(this, new PinnedEventArgs { Pinned = pinned });
        }

        public class PinnedEventArgs : EventArgs
        {
            public bool Pinned { get; set; }
        }
    }
}
