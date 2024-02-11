using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;

namespace Maps
{
    public partial class Form1 : Form
    {
        private GMapControl gMapControl;
        public GMapMarker selectedMarker;
        private Color index = Color.Blue;
        private bool isRightMouseButtonDown = false;
        private PointLatLng previousMousePosition;
        public Form1()
        {
            InitializeComponent();
            InitializeMap();
            InitializeEventHandlers();
            LoadMarkers();
        }

        public class MarkerData
        {
            public decimal Latitude { get; set; }
            public decimal Longitude { get; set; }
            public string Label { get; set; }
            public Color Color { get; set; }

            public MarkerData(decimal latitude, decimal longitude, string label, Color index)
            {
                Latitude = latitude;
                Longitude = longitude;
                Label = label;
                Color = index;
            }
        }


       
        private void SaveMarkers()
        {
            List<MarkerData> markerDataList = new List<MarkerData>();

            foreach (var overlay in gMapControl.Overlays)
            {
                foreach (var marker in overlay.Markers)
                {
                    Color color = index;
                    PointLatLng position = marker.Position;
                    string label = marker.ToolTipText;

                    MarkerData markerData = new MarkerData(latitude: (decimal)position.Lat, longitude: (decimal)position.Lng, label: label, index: color);
                    markerDataList.Add(markerData);
                }
            }

            XNamespace ns = "http://www.opengis.net/kml/2.2";

            XElement kml = new XElement(ns + "kml",
                new XElement(ns + "Document",
                    from markerData in markerDataList
                    select new XElement(ns + "Placemark",
                        new XElement(ns + "latidude", markerData.Latitude.ToString("G17", CultureInfo.InvariantCulture)),
                        new XElement(ns + "longidude", markerData.Longitude.ToString("G17", CultureInfo.InvariantCulture)),
                        new XElement(ns + "name", markerData.Label),
                        new XElement(ns + "Style",
                            new XElement(ns + "IconStyle",
                                new XElement(ns + "color", markerData.Color.ToArgb().ToString("X")))))));

            kml.Save("markers.kml");
        }

        private void LoadMarkers()
        {
            string filePath = "markers.kml";

            if (File.Exists(filePath))
            {
                XDocument kmlDoc = XDocument.Load(filePath);
                List<MarkerData> markerDataList = new List<MarkerData>();

                XNamespace ns = "http://www.opengis.net/kml/2.2";

                foreach (var placemark in kmlDoc.Descendants(ns + "Placemark"))
                {
                    var latitude = placemark.Element(ns + "latidude")?.Value;
                    var longitude = placemark.Element(ns + "longidude")?.Value;
                    var name = placemark.Element(ns + "name")?.Value;
                    var color = placemark.Descendants(ns + "color").FirstOrDefault()?.Value;

                    if (latitude != null && longitude != null && name != null && color != null)
                    {
                        decimal lat = decimal.Parse(latitude, CultureInfo.InvariantCulture);
                        decimal lng = decimal.Parse(longitude, CultureInfo.InvariantCulture);
                        Color markerColor = Color.FromArgb(int.Parse(color, System.Globalization.NumberStyles.HexNumber));

                        MarkerData markerData = new MarkerData(latitude: lat, longitude: lng, label: name, index: markerColor);
                        markerDataList.Add(markerData);
                    }
                }

                foreach (var markerData in markerDataList)
                {
                    PointLatLng position = new PointLatLng((double)markerData.Latitude, (double)markerData.Longitude);
                    string label = markerData.Label;
                    AddMarker(position, label, index);
                }
            }
        }

        private void InitializeMap()
        {
            gMapControl = new GMapControl();
            gMapControl.MapProvider = GMapProviders.GoogleMap;
            gMapControl.MinZoom = 0;
            gMapControl.MaxZoom = 24;
            gMapControl.Dock = DockStyle.Fill;
            gMapControl.Zoom = 10;
            PointLatLng cyprusLocation = new PointLatLng(35.1264, 33.4299);
            gMapControl.Position = cyprusLocation;
            Controls.Add(gMapControl);
            gMapControl.MouseDoubleClick += gMapControl_MouseDoubleClick;
            gMapControl.OnMarkerClick += gMapControl_MarkerClick;
            gMapControl.OnMapZoomChanged += gMapControl_OnMapZoomChanged;
            gMapControl.OnMarkerEnter += gMapControl_MarkerEnter;
            gMapControl.MouseUp +=gMapControl_MouseUp;
            gMapControl.MouseDown += gMapControl_MouseDown;
            gMapControl.MouseMove += gMapControl_MouseMove;
        }
        private void InitializeEventHandlers()
        {
            this.Resize += Form1_Resize;
        }
     

        private void Form1_Load(object sender, EventArgs e)
        {
            gMapControl.Size = new Size(this.ClientSize.Width, this.ClientSize.Height);
        }
        private void AddMarker(PointLatLng point, string label, Color index)
        {
            Color[] predefinedColors = { Color.Blue, Color.Yellow, Color.Orange, Color.Purple, Color.Pink, Color.Red };

            Random random = new Random();
            Color randomColor = predefinedColors[random.Next(0, predefinedColors.Length)];

            GMarkerGoogleType markerType = GetMarkerTypeByColor(randomColor);
            int suffix = 2;
            string originalLabel = label;
            while (listBox1.Items.Cast<string>().Any(item => item.StartsWith(label)))
            {
                label = $"{originalLabel} #{suffix}";
                suffix++;
            }

            GMapMarker marker = new GMarkerGoogle(point, markerType);
            marker.Tag = randomColor;
            GMapOverlay markersOverlay = new GMapOverlay("markers");
            markersOverlay.Markers.Add(marker);
            gMapControl.Overlays.Add(markersOverlay);
            listBox1.Items.Add(label);
            marker.ToolTipText = label;
        }
      
        private void gMapControl_MarkerEnter(GMapMarker item)
        {
            ToolTip tooltip = new ToolTip();
            tooltip.SetToolTip(gMapControl, $"Marker: {item.ToolTipText}\nLat: {item.Position.Lat}\nLng: {item.Position.Lng}");

        }
        private void gMapControl_OnMapZoomChanged()
        {
            const int minZoomLevel = 4;
            const int maxZoomLevel = 20;

            if (gMapControl.Zoom < minZoomLevel)
                gMapControl.Zoom = minZoomLevel;

            if (gMapControl.Zoom > maxZoomLevel)
                gMapControl.Zoom = maxZoomLevel;
        }
        private void gMapControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                isRightMouseButtonDown = true;
                previousMousePosition = gMapControl.FromLocalToLatLng(e.X, e.Y);
            }
        }

        private void gMapControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (isRightMouseButtonDown)
            {
                PointLatLng currentMousePosition = gMapControl.FromLocalToLatLng(e.X, e.Y);
                double deltaX = currentMousePosition.Lng - previousMousePosition.Lng;
                double deltaY = currentMousePosition.Lat - previousMousePosition.Lat;

                PointLatLng newPosition = new PointLatLng(
                    gMapControl.Position.Lat - deltaY,
                    gMapControl.Position.Lng - deltaX
                );
                newPosition.Lat = Math.Max(Math.Min(newPosition.Lat, 90), -90);
                newPosition.Lng = Math.Max(Math.Min(newPosition.Lng, 180), -180);

                gMapControl.Position = newPosition;

                previousMousePosition = currentMousePosition;
            }
        }

        private void gMapControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                isRightMouseButtonDown = false;
            }
        }

        private GMarkerGoogleType GetMarkerTypeByColor(Color color)
        {
            if (color == Color.Blue) return GMarkerGoogleType.blue;
            else if (color == Color.Yellow) return GMarkerGoogleType.yellow;
            else if (color == Color.Orange) return GMarkerGoogleType.orange;
            else if (color == Color.Purple) return GMarkerGoogleType.purple;
            else if (color == Color.Pink) return GMarkerGoogleType.pink;
            else if (color == Color.Red) return GMarkerGoogleType.red;
            else return GMarkerGoogleType.orange;
        }
        private void Form1_Resize(object sender, EventArgs e)
        {
            gMapControl.Size = new System.Drawing.Size(this.ClientSize.Width - 20, this.ClientSize.Height - 20);
        }
        private void gMapControl_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            PointLatLng point = gMapControl.FromLocalToLatLng(e.X, e.Y);
            AddMarker(point, "Yeni Konum",Color.Red);
        }
        private bool zoomInProgress = false;
        private void gMapControl_OnMarkerEnter(GMapMarker item)
        {
            selectedMarker = item;
            GMapOverlay temporaryOverlay = new GMapOverlay("temporaryOverlay");

            double initialRadius = 0.002;
            int circlesCount = 5;
            if (gMapControl.Zoom < 10)
            {
                gMapControl.Zoom = 10;
            }

            for (int i = 1; i <= circlesCount; i++)
            {
                double circleRadius = initialRadius * i;

                GMapPolygon circle = new GMapPolygon(CalculateCirclePoints(selectedMarker.Position, circleRadius), "Circle");
                circle.Fill = new SolidBrush(Color.FromArgb(50 - i * 10, Color.Blue));
                circle.Stroke = new Pen(Color.Blue, 1);
                temporaryOverlay.Polygons.Add(circle);
            }

            gMapControl.Overlays.Add(temporaryOverlay);
            gMapControl.Position = selectedMarker.Position;
            gMapControl.Zoom = gMapControl.Zoom;
            Thread.Sleep(500);
            gMapControl.Overlays.Remove(temporaryOverlay);
        }

        private void gMapControl_MarkerClick(GMapMarker item, MouseEventArgs e)
        {
            selectedMarker = item;

            if (e.Button == MouseButtons.Left)
            {
                ShowMarkerEditorForm();
            }
            else if (!zoomInProgress)
            {
                zoomInProgress = true;

                try
                {
                 
                }
                finally
                {
                    zoomInProgress = false;
                }
            }

            int selectedIndex = gMapControl.Overlays.SelectMany(o => o.Markers).ToList().IndexOf(selectedMarker);
            listBox1.SelectedIndex = selectedIndex;
        }

        private List<PointLatLng> CalculateCirclePoints(PointLatLng center, double radius)
        {
            const int pointsCount = 72;

            List<PointLatLng> points = new List<PointLatLng>();
            double slice = 2 * Math.PI / pointsCount;

            for (int i = 0; i < pointsCount; i++)
            {
                double angle = slice * i;
                double latitude = center.Lat + radius * Math.Sin(angle);
                double longitude = center.Lng + radius * Math.Cos(angle);
                points.Add(new PointLatLng(latitude, longitude));
            }

            return points;
        }

        private void ShowMarkerEditorForm()
        {
            using (MarkerEditorForm markerEditorForm = new MarkerEditorForm(selectedMarker))
            {
                markerEditorForm.label1.Text = $"Enlem(Latitude): {selectedMarker.Position.Lat}";
                markerEditorForm.label2.Text = $"Boylam(Longitude): {selectedMarker.Position.Lng}";

                if (markerEditorForm.ShowDialog() == DialogResult.OK)
                {
                    selectedMarker.ToolTipText = markerEditorForm.MarkerLabel;
                    selectedMarker.Tag = markerEditorForm.MarkerColor;
                    UpdateListBox();
                }
                else if (markerEditorForm.DialogResult == DialogResult.Abort)
                {
                    var overlay = gMapControl.Overlays.FirstOrDefault(o => o.Markers.Contains(selectedMarker));
                    if (overlay != null)
                    {
                        overlay.Markers.Remove(selectedMarker);
                        gMapControl.Refresh();
                        UpdateListBox();
                    }
                }

                int selectedIndex = gMapControl.Overlays.SelectMany(o => o.Markers).ToList().IndexOf(selectedMarker);
                listBox1.SelectedIndex = selectedIndex;
            }
        }

        private void UpdateListBox()
        {
            listBox1.Items.Clear();
            var markerNames = gMapControl.Overlays.SelectMany(o => o.Markers).Select(marker => marker.ToolTipText).OrderBy(name => name);
            foreach (var name in markerNames)
            {
                listBox1.Items.Add(name);
            }
        }



        private void button1_Click(object sender, EventArgs e)
        {
            using (CoordinateInputForm coordinateInputForm = new CoordinateInputForm())
            {
                if (coordinateInputForm.ShowDialog() == DialogResult.OK)
                {
                    double latitude = coordinateInputForm.Latitude;
                    double longitude = coordinateInputForm.Longitude;

                    PointLatLng point = new PointLatLng(latitude, longitude);
                    AddMarker(point, "Yeni Marker", Color.Red);
                }
            }
        }
        private GMapMarker FindMarkerByLabel(string label)
        {
            foreach (var overlay in gMapControl.Overlays)
            {
                foreach (var marker in overlay.Markers)
                {
                    if (marker.ToolTipText == label)
                    {
                        return marker;
                    }
                }
            }
            return null;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SaveMarkers();
            this.Close();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1)
            {
                string selectedLabel = listBox1.SelectedItem.ToString();
                GMapMarker selectedMarker = FindMarkerByLabel(selectedLabel);

                if (selectedMarker != null)
                {
                    gMapControl_OnMarkerEnter(selectedMarker);
                }
            }
        }
        public void CheckAndUpdateListBox(string markerLabel)
        {
            int suffix = 2;
            string originalLabel = markerLabel;
            while (listBox1.Items.Cast<string>().Any(item => item.StartsWith(markerLabel)))
            {
                markerLabel = $"{originalLabel} #{suffix}";
                suffix++;
            }

            var items = listBox1.Items.Cast<string>().ToList();
            items.Sort();

            listBox1.Items.Clear();
            foreach (var item in items)
            {
                listBox1.Items.Add(item);
            }
        }


    }
    public class MarkerEditorForm : Form
    {
        private TextBox txtLabel;
        public System.Windows.Forms.Label label1,label2;
        private Button btnSave;
        private Button btnDelete;
        private Panel colorPanel;

        public string MarkerLabel { get; private set; }
        public Color MarkerColor { get; private set; }

        public MarkerEditorForm(GMapMarker marker)
        {
            InitializeComponents();
            MarkerLabel = marker.ToolTipText;
            MarkerColor = marker.Tag is Color ? (Color)marker.Tag : colorPanel.BackColor;
            txtLabel.Text = MarkerLabel;
            colorPanel.BackColor = MarkerColor; 
        }


        private void InitializeComponents()
        {
            this.Text = "Marker Düzenle";
            this.Width = 300;
            this.Height = 200;

            txtLabel = new TextBox();
            txtLabel.Dock = DockStyle.Top;

            label1 = new System.Windows.Forms.Label();
            label1.Dock = DockStyle.Top;
            label1.Click += label1_Click;
            label2 = new System.Windows.Forms.Label();
            label2.Dock = DockStyle.Top;
            label2.Click += label2_Click;
            colorPanel = new Panel();
            colorPanel.Dock = DockStyle.Top;
            colorPanel.Height = 30;
            colorPanel.Click += colorPanel_Click;
            colorPanel.Visible = false;

            btnSave = new Button();
            btnSave.Text = "Kaydet";
            btnSave.Dock = DockStyle.Bottom;
            btnSave.Click += btnSave_Click;

            btnDelete = new Button();
            btnDelete.Text = "Sil";
            btnDelete.Dock = DockStyle.Bottom;
            btnDelete.Click += btnDelete_Click;

       
            this.Controls.Add(txtLabel);
            this.Controls.Add(label1);
            this.Controls.Add(label2);
            this.Controls.Add(colorPanel);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnDelete);
           
        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            MarkerLabel = txtLabel.Text;
            (Owner as Form1)?.CheckAndUpdateListBox(MarkerLabel);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }


        private void btnDelete_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Abort;
            this.Close();
        }
        private void label1_Click(object sender, EventArgs e)
        {
            if (sender is System.Windows.Forms.Label clickedLabel)
            {
                string labelText = clickedLabel.Text;
                int colonIndex = labelText.IndexOf(':');
                if (colonIndex != -1 && colonIndex < labelText.Length - 1)
                {
                    string afterColon = labelText.Substring(colonIndex + 1).Trim();
                    Clipboard.SetText(afterColon);
                    MessageBox.Show("Enlem(Latitude) panoya kopyalandı.");
                }
                else
                {
                    MessageBox.Show("Metin içinde ':' işareti bulunamadı veya ':' işaretinden sonrası yok.");
                }
            }
        }
        private void label2_Click(object sender, EventArgs e)
        {
            if (sender is System.Windows.Forms.Label clickedLabel)
            {
                string labelText = clickedLabel.Text;
                int colonIndex = labelText.IndexOf(':');
                if (colonIndex != -1 && colonIndex < labelText.Length - 1)
                {
                    string afterColon = labelText.Substring(colonIndex + 1).Trim();
                    Clipboard.SetText(afterColon);
                    MessageBox.Show("Boylam(Longitude) panoya kopyalandı.");
                }
                else
                {
                    MessageBox.Show("Metin içinde ':' işareti bulunamadı veya ':' işaretinden sonrası yok.");
                }
            }
        }
      
        private void colorPanel_Click(object sender, EventArgs e)
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    MarkerColor = colorDialog.Color;
                    colorPanel.BackColor = MarkerColor;
                }
            }
        }

    }

}
