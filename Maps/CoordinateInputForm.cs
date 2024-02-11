using System;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Maps
{
    public partial class CoordinateInputForm : Form
    {
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }

        public CoordinateInputForm()
        {
            InitializeComponent();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (double.TryParse(textBox1.Text, out double latitude) &&
                double.TryParse(textBox2.Text, out double longitude))
            {
                Latitude = latitude;
                Longitude = longitude;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show("Geçersiz koordinatlar. Lütfen doğru bir şekilde doldurun.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();

        }
    }
}