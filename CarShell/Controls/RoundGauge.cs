using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CarShell.Controls
{
    public class RoundGauge : FrameworkElement
    {
        private const double StartAngle = 135.0;
        private const double SweepAngle = 270.0;

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(double),
                typeof(RoundGauge),
                new FrameworkPropertyMetadata(
                    0.0,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum),
                typeof(double),
                typeof(RoundGauge),
                new FrameworkPropertyMetadata(
                    240.0,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(RoundGauge),
                new FrameworkPropertyMetadata(
                    0.0,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MajorStepProperty =
            DependencyProperty.Register(
                nameof(MajorStep),
                typeof(double),
                typeof(RoundGauge),
                new FrameworkPropertyMetadata(
                    20.0,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MinorStepProperty =
            DependencyProperty.Register(
                nameof(MinorStep),
                typeof(double),
                typeof(RoundGauge),
                new FrameworkPropertyMetadata(
                    5.0,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty RedZoneStartProperty =
            DependencyProperty.Register(
                nameof(RedZoneStart),
                typeof(double),
                typeof(RoundGauge),
                new FrameworkPropertyMetadata(
                    double.NaN,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.Register(
                nameof(Unit),
                typeof(string),
                typeof(RoundGauge),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ValueDivisorProperty =
            DependencyProperty.Register(
                nameof(ValueDivisor),
                typeof(double),
                typeof(RoundGauge),
                new FrameworkPropertyMetadata(
                    1.0,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ShowCenterValueProperty =
            DependencyProperty.Register(
                nameof(ShowCenterValue),
                typeof(bool),
                typeof(RoundGauge),
                new FrameworkPropertyMetadata(
                    true,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty LabelDivisorProperty =
            DependencyProperty.Register(
                nameof(LabelDivisor),
                typeof(double),
                typeof(RoundGauge),
                new FrameworkPropertyMetadata(
                    1.0,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double MajorStep
        {
            get => (double)GetValue(MajorStepProperty);
            set => SetValue(MajorStepProperty, value);
        }

        public double MinorStep
        {
            get => (double)GetValue(MinorStepProperty);
            set => SetValue(MinorStepProperty, value);
        }

        public double RedZoneStart
        {
            get => (double)GetValue(RedZoneStartProperty);
            set => SetValue(RedZoneStartProperty, value);
        }

        public string Unit
        {
            get => (string)GetValue(UnitProperty);
            set => SetValue(UnitProperty, value);
        }

        public double ValueDivisor
        {
            get => (double)GetValue(ValueDivisorProperty);
            set => SetValue(ValueDivisorProperty, value);
        }

        public bool ShowCenterValue
        {
            get => (bool)GetValue(ShowCenterValueProperty);
            set => SetValue(ShowCenterValueProperty, value);
        }

        public double LabelDivisor
        {
            get => (double)GetValue(LabelDivisorProperty);
            set => SetValue(LabelDivisorProperty, value);
        }

        public void AnimateTo(double value)
        {
            value = Math.Clamp(value, Minimum, Maximum);

            var animation = new DoubleAnimation
            {
                To = value,
                Duration = TimeSpan.FromMilliseconds(220),
                EasingFunction = new QuadraticEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            BeginAnimation(
                ValueProperty,
                animation,
                HandoffBehavior.SnapshotAndReplace);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            double width = ActualWidth;
            double height = ActualHeight;

            if (width <= 0 || height <= 0)
                return;

            double size = Math.Min(width, height);
            double scale = size / 400.0;

            Point center = new(width / 2.0, height / 2.0);

            double outerRadius = size * 0.465;
            double tickOuterRadius = size * 0.425;
            double majorTickInnerRadius = size * 0.365;
            double minorTickInnerRadius = size * 0.392;
            double labelRadius = size * 0.315;
            double needleLength = size * 0.315;

            Brush backgroundBrush =
                new RadialGradientBrush(
                    Color.FromRgb(24, 28, 34),
                    Color.FromRgb(4, 7, 10));

            drawingContext.DrawEllipse(
                backgroundBrush,
                new Pen(
                    new SolidColorBrush(Color.FromRgb(52, 59, 67)),
                    Math.Max(1.5, 2.4 * scale)),
                center,
                outerRadius,
                outerRadius);

            drawingContext.DrawEllipse(
                null,
                new Pen(
                    new SolidColorBrush(Color.FromRgb(229, 39, 47)),
                    Math.Max(1.0, 1.5 * scale)),
                center,
                outerRadius - 5 * scale,
                outerRadius - 5 * scale);

            DrawTicks(
                drawingContext,
                center,
                tickOuterRadius,
                majorTickInnerRadius,
                minorTickInnerRadius,
                labelRadius,
                scale);

            DrawRedZone(
                drawingContext,
                center,
                tickOuterRadius,
                majorTickInnerRadius,
                scale);

            DrawNeedle(
                drawingContext,
                center,
                needleLength,
                scale);

            if (ShowCenterValue)
            {
                DrawCenterText(
                    drawingContext,
                    center,
                    scale);
            }
        }

        private void DrawTicks(
            DrawingContext dc,
            Point center,
            double outerRadius,
            double majorInnerRadius,
            double minorInnerRadius,
            double labelRadius,
            double scale)
        {
            if (Maximum <= Minimum ||
                MajorStep <= 0 ||
                MinorStep <= 0)
            {
                return;
            }

            Typeface typeface = new(
                new FontFamily("Segoe UI"),
                FontStyles.Normal,
                FontWeights.Normal,
                FontStretches.Normal);

            Brush majorBrush = Brushes.White;
            Brush minorBrush =
                new SolidColorBrush(Color.FromRgb(190, 197, 204));

            double epsilon = MinorStep / 10.0;

            for (double tickValue = Minimum;
                 tickValue <= Maximum + epsilon;
                 tickValue += MinorStep)
            {
                bool isMajor = IsMajorTick(tickValue);

                double angle = ValueToAngle(tickValue);

                Point outer = PointOnCircle(
                    center,
                    outerRadius,
                    angle);

                Point inner = PointOnCircle(
                    center,
                    isMajor
                        ? majorInnerRadius
                        : minorInnerRadius,
                    angle);

                Brush tickBrush = IsInRedZone(tickValue)
                    ? new SolidColorBrush(Color.FromRgb(244, 43, 51))
                    : isMajor
                        ? majorBrush
                        : minorBrush;

                double thickness = isMajor
                    ? Math.Max(1.6, 2.8 * scale)
                    : Math.Max(0.8, 1.2 * scale);

                dc.DrawLine(
                    new Pen(tickBrush, thickness),
                    inner,
                    outer);

                if (!isMajor)
                    continue;

                double labelValue =
                    tickValue / Math.Max(0.0001, LabelDivisor);

                string label = Math.Abs(labelValue -
                                        Math.Round(labelValue)) < 0.001
                    ? Math.Round(labelValue).ToString(
                        CultureInfo.InvariantCulture)
                    : labelValue.ToString(
                        "0.0",
                        CultureInfo.InvariantCulture);

                double fontSize =
                    Math.Max(11, 20 * scale);

                FormattedText text = new(
                    label,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    fontSize,
                    IsInRedZone(tickValue)
                        ? new SolidColorBrush(
                            Color.FromRgb(244, 43, 51))
                        : Brushes.White,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                Point labelPoint = PointOnCircle(
                    center,
                    labelRadius,
                    angle);

                dc.DrawText(
                    text,
                    new Point(
                        labelPoint.X - text.Width / 2.0,
                        labelPoint.Y - text.Height / 2.0));
            }
        }

        private void DrawRedZone(
            DrawingContext dc,
            Point center,
            double outerRadius,
            double innerRadius,
            double scale)
        {
            if (double.IsNaN(RedZoneStart) ||
                RedZoneStart >= Maximum)
            {
                return;
            }

            double start = ValueToAngle(
                Math.Max(RedZoneStart, Minimum));

            double end = ValueToAngle(Maximum);

            Brush brush =
                new SolidColorBrush(Color.FromRgb(235, 34, 43));

            int segments = 40;

            for (int i = 0; i < segments; i++)
            {
                double t1 = (double)i / segments;
                double t2 = (double)(i + 1) / segments;

                double a1 = start + (end - start) * t1;
                double a2 = start + (end - start) * t2;

                Point p1 = PointOnCircle(
                    center,
                    outerRadius - 3 * scale,
                    a1);

                Point p2 = PointOnCircle(
                    center,
                    outerRadius - 3 * scale,
                    a2);

                dc.DrawLine(
                    new Pen(
                        brush,
                        Math.Max(3, 7 * scale)),
                    p1,
                    p2);
            }
        }

        private void DrawNeedle(
            DrawingContext dc,
            Point center,
            double length,
            double scale)
        {
            double value =
                Math.Clamp(Value, Minimum, Maximum);

            double angle = ValueToAngle(value);

            Point needleTip = PointOnCircle(
                center,
                length,
                angle);

            Point rearPoint = PointOnCircle(
                center,
                size: 28 * scale,
                angle + 180);

            Vector direction =
                needleTip - rearPoint;

            direction.Normalize();

            Vector perpendicular =
                new(-direction.Y, direction.X);

            double halfWidth =
                Math.Max(3, 5.5 * scale);

            Point left =
                rearPoint + perpendicular * halfWidth;

            Point right =
                rearPoint - perpendicular * halfWidth;

            StreamGeometry needleGeometry =
                new();

            using (StreamGeometryContext geometry =
                   needleGeometry.Open())
            {
                geometry.BeginFigure(
                    needleTip,
                    true,
                    true);

                geometry.LineTo(left, true, false);
                geometry.LineTo(right, true, false);
            }

            needleGeometry.Freeze();

            dc.DrawGeometry(
                new SolidColorBrush(Color.FromRgb(247, 35, 44)),
                null,
                needleGeometry);

            dc.DrawEllipse(
                new SolidColorBrush(Color.FromRgb(218, 37, 44)),
                null,
                center,
                Math.Max(8, 12 * scale),
                Math.Max(8, 12 * scale));

            dc.DrawEllipse(
                Brushes.White,
                null,
                center,
                Math.Max(3, 4.5 * scale),
                Math.Max(3, 4.5 * scale));
        }

        private void DrawCenterText(
            DrawingContext dc,
            Point center,
            double scale)
        {
            Typeface valueTypeface = new(
                new FontFamily("Segoe UI"),
                FontStyles.Normal,
                FontWeights.SemiBold,
                FontStretches.Normal);

            Typeface unitTypeface = new(
                new FontFamily("Segoe UI"),
                FontStyles.Normal,
                FontWeights.Normal,
                FontStretches.Normal);

            double divisor =
                Math.Max(0.0001, ValueDivisor);

            double displayedValue =
                Value / divisor;

            string valueText;

            if (divisor >= 1000)
            {
                valueText = Math.Round(Value)
                    .ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                valueText = Math.Round(displayedValue)
                    .ToString(CultureInfo.InvariantCulture);
            }

            double valueFont =
                Math.Max(32, 62 * scale);

            double unitFont =
                Math.Max(12, 17 * scale);

            FormattedText valueFormatted = new(
                valueText,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                valueTypeface,
                valueFont,
                Brushes.White,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            FormattedText unitFormatted = new(
                Unit ?? string.Empty,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                unitTypeface,
                unitFont,
                new SolidColorBrush(
                    Color.FromRgb(190, 197, 205)),
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            dc.DrawText(
                valueFormatted,
                new Point(
                    center.X - valueFormatted.Width / 2,
                    center.Y - valueFormatted.Height / 2 - 2 * scale));

            dc.DrawText(
                unitFormatted,
                new Point(
                    center.X - unitFormatted.Width / 2,
                    center.Y + 45 * scale));
        }

        private bool IsMajorTick(double tickValue)
        {
            if (MajorStep <= 0)
                return false;

            double relative =
                (tickValue - Minimum) / MajorStep;

            return Math.Abs(relative -
                            Math.Round(relative)) < 0.001;
        }

        private bool IsInRedZone(double value)
        {
            return !double.IsNaN(RedZoneStart) &&
                   value >= RedZoneStart;
        }

        private double ValueToAngle(double value)
        {
            double normalized =
                (value - Minimum) /
                Math.Max(0.0001, Maximum - Minimum);

            return StartAngle +
                   normalized * SweepAngle;
        }

        private static Point PointOnCircle(
            Point center,
            double size,
            double angleDegrees)
        {
            double radians =
                angleDegrees * Math.PI / 180.0;

            return new Point(
                center.X + Math.Cos(radians) * size,
                center.Y + Math.Sin(radians) * size);
        }
    }
}