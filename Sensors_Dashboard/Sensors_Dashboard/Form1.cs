using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Sensors_Dashboard
{
    public partial class Form1 : Form
    {
        private HttpClient httpClient;
        private System.Windows.Forms.Timer updateTimer;
        private const string API_BASE_URL = "http://localhost:3000/api";
        private bool isConnected = false;

       
        private TextBox txtLogNew;

        
        private Panel pnlTempStats;
        private Panel pnlHumidityStats;
        private Panel pnlPressureStats;

        
        private Panel pnlTempGaugeCard;
        private Panel pnlHumidityGaugeCard;
        private Panel pnlPressureGaugeCard;

        
        private Panel pnlChartCard;
        private Chart tempChart;
        private Chart humidChart;
        private Chart pressChart;

        
        private double currentTemp = 22.5;
        private double currentHumidity = 45.0;
        private double currentPressure = 1013.25;

        // Stats
        private double tempAvg = 21.8, tempMin = 18.2, tempMax = 25.6, tempPeak = 27.1;
        private double humidAvg = 48.3, humidMin = 42.0, humidMax = 55.0, humidPeak = 58.5;
        private double pressAvg = 1012.5, pressMin = 1008.0, pressMax = 1018.0, pressPeak = 1020.0;

        private ComboBox cmbTimeRange;

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.BackColor = Color.FromArgb(240, 242, 245);
            this.Size = new Size(1100, 900);
            this.AutoScroll = true;

            HideOriginalControls();
            InitializeModernUI();
            InitializeHttpClient();
            InitializeTimer();
        }

        private void HideOriginalControls()
        {
            lblTemperature.Visible = false;
            lblHumidity.Visible = false;
            lblPressure.Visible = false;
            txtLog.Visible = false;
        }

        private void InitializeHttpClient()
        {
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);
        }

        private void InitializeTimer()
        {
            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 3000;
            updateTimer.Tick += UpdateTimer_Tick;
        }

        private void InitializeModernUI()
        {
            //header
            panel1.Size = new Size(this.Width, 80);
            label1.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            label1.Location = new Point(30, 22);

            txtLogNew = new TextBox();
            txtLogNew.Location = new Point(980, 90);
            txtLogNew.Size = new Size(300, 32);
            txtLogNew.Multiline = false;
            txtLogNew.ReadOnly = true;
            txtLogNew.BackColor = Color.Black;
            txtLogNew.ForeColor = Color.Lime;
            txtLogNew.Font = new Font("Consolas", 8);
            txtLogNew.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(txtLogNew);
            txtLogNew.BringToFront();

            //Time range selector
            Label lblTimeRange = new Label();
            lblTimeRange.Text = "Time Range:";
            lblTimeRange.Font = new Font("Segoe UI", 9);
            lblTimeRange.ForeColor = Color.FromArgb(100, 116, 139);
            lblTimeRange.Location = new Point(30, 95);
            lblTimeRange.AutoSize = true;
            this.Controls.Add(lblTimeRange);
            lblTimeRange.BringToFront();

            cmbTimeRange = new ComboBox();
            cmbTimeRange.Font = new Font("Segoe UI", 9);
            cmbTimeRange.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTimeRange.Location = new Point(120, 92);
            cmbTimeRange.Size = new Size(150, 25);
            cmbTimeRange.Items.AddRange(new object[] {
                "Last 15 minutes", "Last 30 minutes", "Last 1 hour",
                "Last 3 hours", "Last 6 hours", "Last 12 hours", "Last 24 hours"
            });
            cmbTimeRange.SelectedIndex = 2;
            cmbTimeRange.SelectedIndexChanged += CmbTimeRange_SelectedIndexChanged;
            this.Controls.Add(cmbTimeRange);
            cmbTimeRange.BringToFront();

            lblStatus.Location = new Point(300, 95);
            lblStatus.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnConnect.Location = new Point(500, 90);
            btnConnect.Size = new Size(100, 32);
            btnDisconnect.Location = new Point(610, 90);
            btnDisconnect.Size = new Size(100, 32);

            pnlTempStats = CreateStatsCard(30, 130, "Temperature", Color.FromArgb(34, 197, 94),
                currentTemp, "°C", tempAvg, tempMin, tempMax, tempPeak);
            pnlHumidityStats = CreateStatsCard(380, 130, "Humidity", Color.FromArgb(168, 85, 247),
                currentHumidity, "%", humidAvg, humidMin, humidMax, humidPeak);
            pnlPressureStats = CreateStatsCard(730, 130, "Pressure", Color.FromArgb(251, 146, 60),
                currentPressure, " hPa", pressAvg, pressMin, pressMax, pressPeak);

            txtLog.Location = new Point(730, 320);
            txtLog.Size = new Size(330, 180);
            txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Right;
           
            pnlTempGaugeCard = CreateGaugeCard(30, 330, "Temperature", Color.FromArgb(34, 197, 94),
                currentTemp, -10, 50, "°C");
            pnlHumidityGaugeCard = CreateGaugeCard(380, 330, "Humidity", Color.FromArgb(168, 85, 247),
                currentHumidity, 0, 100, "%");
            pnlPressureGaugeCard = CreateGaugeCard(730, 330, "Pressure", Color.FromArgb(251, 146, 60),
                currentPressure, 950, 1050, " hPa");

            //Chart card
            CreateChartCard();

            
            this.AutoScrollMinSize = new Size(1100, 1100);
        }

        private Panel CreateStatsCard(int x, int y, string title, Color accentColor,
            double current, string unit, double avg, double min, double max, double peak)
        {
            Panel panel = new Panel();
            panel.Location = new Point(x, y);
            panel.Size = new Size(330, 180);
            panel.BackColor = accentColor;
            panel.Paint += (s, e) => DrawRoundedCard(e.Graphics, panel.ClientRectangle, accentColor, 0);
            this.Controls.Add(panel);
            panel.BringToFront();

            //Header
            Label lblHeader = new Label();
            lblHeader.Text = $"{title} Stats";
            lblHeader.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            lblHeader.ForeColor = Color.FromArgb(255, 255, 255, 200);
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            panel.Controls.Add(lblHeader);

            //Main value
            Label lblMain = new Label();
            lblMain.Text = $"{current:F1}";
            lblMain.Font = new Font("Segoe UI", 48, FontStyle.Bold);
            lblMain.ForeColor = Color.White;
            lblMain.Location = new Point(20, 40);
            lblMain.Size = new Size(200, 70);
            lblMain.Tag = "main_value";
            panel.Controls.Add(lblMain);

            Label lblMainUnit = new Label();
            lblMainUnit.Text = unit;
            lblMainUnit.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            lblMainUnit.ForeColor = Color.White;
            lblMainUnit.Location = new Point(current >= 100 ? 260 : 230, 70);
            lblMainUnit.AutoSize = true;
            panel.Controls.Add(lblMainUnit);

            //Bottom row stats
            int bottomY = 120;
            CreateSmallStat(panel, 20, bottomY, "Avg", $"{avg:F1}{unit}");
            CreateSmallStat(panel, 20, bottomY + 25, "Min", $"{min:F1}{unit}");
            CreateSmallStat(panel, 180, bottomY, "Max", $"{max:F1}{unit}");
            CreateSmallStat(panel, 180, bottomY + 25, "Peak", $"{peak:F1}{unit}");

            return panel;
        }

        private void CreateSmallStat(Panel parent, int x, int y, string label, string value)
        {
            Label lblLabel = new Label();
            lblLabel.Text = label;
            lblLabel.Font = new Font("Segoe UI", 8, FontStyle.Regular);
            lblLabel.ForeColor = Color.FromArgb(255, 255, 255, 180);
            lblLabel.Location = new Point(x, y);
            lblLabel.AutoSize = true;
            parent.Controls.Add(lblLabel);

            Label lblValue = new Label();
            lblValue.Text = value;
            lblValue.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblValue.ForeColor = Color.White;
            lblValue.Location = new Point(x + 50, y - 1);
            lblValue.AutoSize = true;
            lblValue.Tag = label;
            parent.Controls.Add(lblValue);

            //Arrow icon
            Label lblArrow = new Label();
            lblArrow.Text = "→";
            lblArrow.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblArrow.ForeColor = Color.White;
            lblArrow.Location = new Point(x + 130, y - 2);
            lblArrow.AutoSize = true;
            parent.Controls.Add(lblArrow);
        }

        private Panel CreateGaugeCard(int x, int y, string title, Color accentColor,
            double value, double min, double max, string unit)
        {
            Panel panel = new Panel();
            panel.Location = new Point(x, y);
            panel.Size = new Size(330, 240);
            panel.BackColor = Color.White;
            panel.Tag = new GaugeData { Value = value, Min = min, Max = max, Color = accentColor, Unit = unit };
            panel.Paint += (s, e) => {
                DrawRoundedCard(e.Graphics, panel.ClientRectangle, Color.White, 1);
                var data = (GaugeData)panel.Tag;
                DrawSpeedometerGauge(e.Graphics, panel.Width, panel.Height - 40, data.Value, data.Min, data.Max, data.Color);
            };
            this.Controls.Add(panel);
            panel.BringToFront();

            //Title
            Label lblTitle = new Label();
            lblTitle.Text = $"{title} Sensor";
            lblTitle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(51, 65, 85);
            lblTitle.Location = new Point(20, 15);
            lblTitle.AutoSize = true;
            panel.Controls.Add(lblTitle);

            //Timestamp
            Label lblTime = new Label();
            lblTime.Text = DateTime.Now.ToString("dd MMM - HH:mm:ss");
            lblTime.Font = new Font("Segoe UI", 7, FontStyle.Regular);
            lblTime.ForeColor = Color.FromArgb(148, 163, 184);
            lblTime.Location = new Point(20, 32);
            lblTime.AutoSize = true;
            lblTime.Tag = "timestamp";
            panel.Controls.Add(lblTime);

            return panel;
        }

        
        private void CreateChartCard()
        {
            pnlChartCard = new Panel();
            pnlChartCard.Location = new Point(30, 590);
            pnlChartCard.Size = new Size(1030, 480); // Plus grand pour 3 graphiques
            pnlChartCard.BackColor = Color.White;
            pnlChartCard.Paint += (s, e) => DrawRoundedCard(e.Graphics, pnlChartCard.ClientRectangle, Color.White, 1);
            this.Controls.Add(pnlChartCard);
            pnlChartCard.BringToFront();

            // Title
            Label lblTitle = new Label();
            lblTitle.Text = "Sensor History";
            lblTitle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(51, 65, 85);
            lblTitle.Location = new Point(20, 15);
            lblTitle.AutoSize = true;
            pnlChartCard.Controls.Add(lblTitle);

            //Graphique 1 : Température
            tempChart = CreateSingleChart("Temperature", Color.FromArgb(34, 197, 94), "°C");
            tempChart.Location = new Point(10, 45);
            tempChart.Size = new Size(1010, 130);
            pnlChartCard.Controls.Add(tempChart);

            //Graphique 2 : Humidité
            humidChart = CreateSingleChart("Humidity", Color.FromArgb(168, 85, 247), "%");
            humidChart.Location = new Point(10, 185);
            humidChart.Size = new Size(1010, 130);
            pnlChartCard.Controls.Add(humidChart);

            //Graphique 3 : Pression
            pressChart = CreateSingleChart("Pressure", Color.FromArgb(251, 146, 60), "hPa");
            pressChart.Location = new Point(10, 325);
            pressChart.Size = new Size(1010, 130);
            pnlChartCard.Controls.Add(pressChart);
        }

        
        private Chart CreateSingleChart(string seriesName, Color color, string unit)
        {
            Chart chart = new Chart();
            chart.BackColor = Color.White;
            chart.BorderlineWidth = 0;

            ChartArea chartArea = new ChartArea("Main");
            chartArea.BackColor = Color.White;
            chartArea.AxisX.MajorGrid.LineColor = Color.FromArgb(241, 245, 249);
            chartArea.AxisX.LabelStyle.Font = new Font("Segoe UI", 7);
            chartArea.AxisX.LabelStyle.ForeColor = Color.FromArgb(148, 163, 184);
            chartArea.AxisX.LabelStyle.Format = "HH:mm";
            chartArea.AxisX.LineColor = Color.FromArgb(226, 232, 240);
            chartArea.AxisX.MajorTickMark.Enabled = false;
            chartArea.AxisY.MajorGrid.LineColor = Color.FromArgb(241, 245, 249);
            chartArea.AxisY.LabelStyle.Font = new Font("Segoe UI", 7);
            chartArea.AxisY.LabelStyle.ForeColor = Color.FromArgb(148, 163, 184);
            chartArea.AxisY.LineColor = Color.FromArgb(226, 232, 240);
            chartArea.AxisY.MajorTickMark.Enabled = false;
            chartArea.AxisY.Title = unit;
            chartArea.AxisY.TitleFont = new Font("Segoe UI", 8, FontStyle.Bold);
            chartArea.AxisY.TitleForeColor = color;
            chart.ChartAreas.Add(chartArea);

            //Série unique
            Series series = new Series(seriesName);
            series.ChartArea = "Main";
            series.ChartType = SeriesChartType.Line;
            series.Color = color;
            series.BorderWidth = 3;
            series.XValueType = ChartValueType.DateTime;
            chart.Series.Add(series);

            //Legend
            Legend legend = new Legend();
            legend.Docking = Docking.Top;
            legend.Alignment = StringAlignment.Far;
            legend.Font = new Font("Segoe UI", 7);
            legend.ForeColor = color;
            chart.Legends.Add(legend);

            return chart;
        }

        private void DrawRoundedCard(Graphics g, Rectangle rect, Color bgColor, int borderStyle)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (borderStyle == 1)
            {
                using (GraphicsPath shadowPath = GetRoundedRect(new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 1, rect.Height - 1), 8))
                using (PathGradientBrush shadowBrush = new PathGradientBrush(shadowPath))
                {
                    shadowBrush.CenterColor = Color.FromArgb(15, 0, 0, 0);
                    shadowBrush.SurroundColors = new Color[] { Color.FromArgb(0, 0, 0, 0) };
                    g.FillPath(shadowBrush, shadowPath);
                }
            }

            using (GraphicsPath path = GetRoundedRect(new Rectangle(rect.X, rect.Y, rect.Width - 1, rect.Height - 1), 8))
            {
                g.FillPath(new SolidBrush(bgColor), path);
            }
        }

        private GraphicsPath GetRoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        
        private void DrawSpeedometerGauge(Graphics g, int width, int height, double value, double min, double max, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int centerX = width / 2;
            int centerY = height - 30;
            int radius = Math.Min(width, height) / 2 - 40;

            DrawColoredArcSegments(g, centerX, centerY, radius);
            DrawScaleMarkers(g, centerX, centerY, radius, min, max);

            
            double valuePercent = Math.Max(0, Math.Min(1, (value - min) / (max - min)));
            float angle = 180 - 140 + (float)(valuePercent * 280);
            DrawNeedle(g, centerX, centerY, radius - 10, angle, color);

            using (Font valueFont = new Font("Segoe UI", 14, FontStyle.Bold))
            using (Brush valueBrush = new SolidBrush(color))
            {
                string valueText = value.ToString("F2");
                SizeF textSize = g.MeasureString(valueText, valueFont);
                g.DrawString(valueText, valueFont, valueBrush,
                    centerX - textSize.Width / 2, centerY + 10);
            }

            using (Font labelFont = new Font("Segoe UI", 7, FontStyle.Regular))
            using (Brush labelBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
            {
                g.DrawString(min.ToString("F0"), labelFont, labelBrush, 25, centerY + 10);
                SizeF maxSize = g.MeasureString(max.ToString("F0"), labelFont);
                g.DrawString(max.ToString("F0"), labelFont, labelBrush, width - maxSize.Width - 25, centerY + 10);
            }
        }

        private void DrawColoredArcSegments(Graphics g, int centerX, int centerY, int radius)
        {
            Color[] segmentColors = {
                Color.FromArgb(34, 197, 94),
                Color.FromArgb(234, 179, 8),
                Color.FromArgb(251, 146, 60),
                Color.FromArgb(239, 68, 68)
            };

            float startAngle = 140;
            float sweepPerSegment = 70;

            for (int i = 0; i < segmentColors.Length; i++)
            {
                using (Pen pen = new Pen(segmentColors[i], 8))
                {
                    g.DrawArc(pen, centerX - radius, centerY - radius, radius * 2, radius * 2,
                        startAngle + (i * sweepPerSegment), sweepPerSegment);
                }
            }
        }

        private void DrawScaleMarkers(Graphics g, int centerX, int centerY, int radius, double min, double max)
        {
            int numMarkers = 11;
            for (int i = 0; i <= numMarkers; i++)
            {
                float angle = 180- 140 + (i * 280f / numMarkers);
                double radians = angle * Math.PI / 180;

                int x1 = centerX + (int)((radius - 15) * Math.Cos(radians));
                int y1 = centerY + (int)((radius - 15) * Math.Sin(radians));
                int x2 = centerX + (int)((radius - 5) * Math.Cos(radians));
                int y2 = centerY + (int)((radius - 5) * Math.Sin(radians));

                using (Pen pen = new Pen(Color.FromArgb(203, 213, 225), i % 2 == 0 ? 2 : 1))
                {
                    g.DrawLine(pen, x1, y1, x2, y2);
                }
            }
        }

        private void DrawNeedle(Graphics g, int centerX, int centerY, int length, float angle, Color color)
        {
            double radians = angle * Math.PI / 180;
            int endX = centerX + (int)(length * Math.Cos(radians));
            int endY = centerY + (int)(length * Math.Sin(radians));

            using (Pen needlePen = new Pen(color, 3))
            {
                needlePen.StartCap = LineCap.Round;
                needlePen.EndCap = LineCap.ArrowAnchor;
                g.DrawLine(needlePen, centerX, centerY, endX, endY);
            }

            g.FillEllipse(new SolidBrush(color), centerX - 6, centerY - 6, 12, 12);
            g.FillEllipse(Brushes.White, centerX - 3, centerY - 3, 6, 6);
        }

        private async void CmbTimeRange_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isConnected)
            {
                await LoadHistoricalData();
            }
        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                btnConnect.Enabled = false;
                LogMessage("Connecting...");

                var response = await httpClient.GetAsync($"{API_BASE_URL}/health");

                if (response.IsSuccessStatusCode)
                {
                    isConnected = true;
                    lblStatus.Text = "Status: Connected";
                    lblStatus.ForeColor = Color.FromArgb(34, 197, 94);
                    btnConnect.Enabled = false;
                    btnDisconnect.Enabled = true;

                    LogMessage("✓ Connected to STM32");
                    await LoadHistoricalData();
                    updateTimer.Start();
                }
                else
                {
                    throw new Exception("Server not responding");
                }
            }
            catch (Exception ex)
            {
                isConnected = false;
                lblStatus.Text = "Status: Failed";
                lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
                btnConnect.Enabled = true;
                LogMessage($"✗ Connection failed: {ex.Message}");
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            Disconnect();
        }

        private void Disconnect()
        {
            isConnected = false;
            updateTimer.Stop();
            lblStatus.Text = "Status: Disconnected";
            lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            LogMessage("Disconnected from STM32");
        }

        private async void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (isConnected)
            {
                await UpdateLatestReadings();
                await LoadHistoricalData();
            }
        }

        private async Task UpdateLatestReadings()
        {
            try
            {
                var response = await httpClient.GetAsync($"{API_BASE_URL}/latest");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<LatestReadings>(json);

                    if (data.temperature != null)
                    {
                        currentTemp = data.temperature.value;
                        UpdateStatsCard(pnlTempStats, currentTemp, "°C");
                        UpdateGaugeCard(pnlTempGaugeCard, currentTemp);
                    }

                    if (data.humidity != null)
                    {
                        currentHumidity = data.humidity.value;
                        UpdateStatsCard(pnlHumidityStats, currentHumidity, "%");
                        UpdateGaugeCard(pnlHumidityGaugeCard, currentHumidity);
                    }

                    if (data.pressure != null)
                    {
                        currentPressure = data.pressure.value;
                        UpdateStatsCard(pnlPressureStats, currentPressure, " hPa");
                        UpdateGaugeCard(pnlPressureGaugeCard, currentPressure);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Update error: {ex.Message}");
            }
        }

        private void UpdateStatsCard(Panel panel, double newValue, string unit)
        {
            foreach (Control ctrl in panel.Controls)
            {
                if (ctrl is Label lbl && lbl.Tag != null && lbl.Tag.ToString() == "main_value")
                {
                    lbl.Text = $"{newValue:F1}";
                    break;
                }
            }
        }

        private void UpdateGaugeCard(Panel panel, double newValue)
        {
            var data = (GaugeData)panel.Tag;
            data.Value = newValue;
            panel.Tag = data;
            panel.Invalidate();

            foreach (Control ctrl in panel.Controls)
            {
                if (ctrl is Label lbl && lbl.Tag != null && lbl.Tag.ToString() == "timestamp")
                {
                    lbl.Text = DateTime.Now.ToString("dd MMM - HH:mm:ss");
                    break;
                }
            }
        }

        private async Task LoadHistoricalData()
        {
            try
            {
                double hours = GetHoursFromSelection();
                var response = await httpClient.GetAsync($"{API_BASE_URL}/history?hours={hours}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<HistoricalData>(json);

                    UpdateMultiLineChart(data);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Data load error: {ex.Message}");
            }
        }

        
        private void UpdateMultiLineChart(HistoricalData data)
        {
            if (data == null) return;

            
            tempChart.Series[0].Points.Clear();
            humidChart.Series[0].Points.Clear();
            pressChart.Series[0].Points.Clear();

            //TEMPÉRATURE
            if (data.temperature != null && data.temperature.Count > 0)
            {
                var displayReadings = data.temperature.Skip(Math.Max(0, data.temperature.Count - 50)).ToList();

                double minY = double.MaxValue;
                double maxY = double.MinValue;

                foreach (var reading in displayReadings)
                {
                    tempChart.Series["Temperature"].Points.AddXY(reading.timestamp, reading.value);
                    if (reading.value < minY) minY = reading.value;
                    if (reading.value > maxY) maxY = reading.value;
                }

                if (displayReadings.Count > 0)
                {
                    tempAvg = displayReadings.Average(r => r.value);
                    tempMin = displayReadings.Min(r => r.value);
                    tempMax = displayReadings.Max(r => r.value);
                    tempPeak = tempMax;
                    UpdateStatsCardValues(pnlTempStats, tempAvg, tempMin, tempMax, tempPeak, "°C");
                }

                if (minY != double.MaxValue && maxY != double.MinValue)
                {
                    double range = maxY - minY;
                    double padding = range > 0 ? range * 0.1 : 1;
                    tempChart.ChartAreas[0].AxisY.Minimum = minY - padding;
                    tempChart.ChartAreas[0].AxisY.Maximum = maxY + padding;
                }
            }

            //HUMIDITÉ
            if (data.humidity != null && data.humidity.Count > 0)
            {
                var displayReadings = data.humidity.Skip(Math.Max(0, data.humidity.Count - 50)).ToList();

                double minY = double.MaxValue;
                double maxY = double.MinValue;

                foreach (var reading in displayReadings)
                {
                    humidChart.Series["Humidity"].Points.AddXY(reading.timestamp, reading.value);
                    if (reading.value < minY) minY = reading.value;
                    if (reading.value > maxY) maxY = reading.value;
                }

                if (displayReadings.Count > 0)
                {
                    humidAvg = displayReadings.Average(r => r.value);
                    humidMin = displayReadings.Min(r => r.value);
                    humidMax = displayReadings.Max(r => r.value);
                    humidPeak = humidMax;
                    UpdateStatsCardValues(pnlHumidityStats, humidAvg, humidMin, humidMax, humidPeak, "%");
                }

                if (minY != double.MaxValue && maxY != double.MinValue)
                {
                    double range = maxY - minY;
                    double padding = range > 0 ? range * 0.1 : 1;
                    humidChart.ChartAreas[0].AxisY.Minimum = minY - padding;
                    humidChart.ChartAreas[0].AxisY.Maximum = maxY + padding;
                }
            }

            //PRESSION
            if (data.pressure != null && data.pressure.Count > 0)
            {
                var displayReadings = data.pressure.Skip(Math.Max(0, data.pressure.Count - 50)).ToList();

                double minY = double.MaxValue;
                double maxY = double.MinValue;

                foreach (var reading in displayReadings)
                {
                    pressChart.Series["Pressure"].Points.AddXY(reading.timestamp, reading.value);
                    if (reading.value < minY) minY = reading.value;
                    if (reading.value > maxY) maxY = reading.value;
                }

                if (displayReadings.Count > 0)
                {
                    pressAvg = displayReadings.Average(r => r.value);
                    pressMin = displayReadings.Min(r => r.value);
                    pressMax = displayReadings.Max(r => r.value);
                    pressPeak = pressMax;
                    UpdateStatsCardValues(pnlPressureStats, pressAvg, pressMin, pressMax, pressPeak, " hPa");
                }

                if (minY != double.MaxValue && maxY != double.MinValue)
                {
                    double range = maxY - minY;
                    double padding = range > 0 ? range * 0.1 : 1;
                    pressChart.ChartAreas[0].AxisY.Minimum = minY - padding;
                    pressChart.ChartAreas[0].AxisY.Maximum = maxY + padding;
                }
            }
        }

        private void UpdateStatsCardValues(Panel panel, double avg, double min, double max, double peak, string unit)
        {
            foreach (Control ctrl in panel.Controls)
            {
                if (ctrl is Label lbl && lbl.Tag != null)
                {
                    string tag = lbl.Tag.ToString();
                    if (tag == "Avg")
                        lbl.Text = $"{avg:F1}{unit}";
                    else if (tag == "Min")
                        lbl.Text = $"{min:F1}{unit}";
                    else if (tag == "Max")
                        lbl.Text = $"{max:F1}{unit}";
                    else if (tag == "Peak")
                        lbl.Text = $"{peak:F1}{unit}";
                }
            }
        }

        private double GetHoursFromSelection()
        {
            switch (cmbTimeRange.SelectedIndex)
            {
                case 0: return 0.25;
                case 1: return 0.5;
                case 2: return 1;
                case 3: return 3;
                case 4: return 6;
                case 5: return 12;
                case 6: return 24;
                default: return 1;
            }
        }

        private void LogMessage(string message)
        {
            if (txtLogNew.InvokeRequired)
            {
                txtLogNew.Invoke(new Action(() => LogMessage(message)));
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            txtLogNew.Text = $"[{timestamp}] {message}";
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Disconnect();
            httpClient?.Dispose();
            updateTimer?.Dispose();
        }
    }

    public class GaugeData
    {
        public double Value { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public Color Color { get; set; }
        public string Unit { get; set; }
    }

    public class SensorReading
    {
        public string sensor_type { get; set; }
        public double value { get; set; }
        public DateTime timestamp { get; set; }
    }

    public class LatestReadings
    {
        public SensorReading temperature { get; set; }
        public SensorReading humidity { get; set; }
        public SensorReading pressure { get; set; }
    }

    public class HistoricalData
    {
        public List<SensorReading> temperature { get; set; }
        public List<SensorReading> humidity { get; set; }
        public List<SensorReading> pressure { get; set; }
    }
}