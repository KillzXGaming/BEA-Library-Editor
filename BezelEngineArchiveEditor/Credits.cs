using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BezelEngineArchiveEditor
{
    public partial class Credits : Form
    {
        public Credits()
        {
            InitializeComponent();
        }

        private void Credits_Load(object sender, EventArgs e)
        {

        }

        private void linkLabel1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://icons8.com/");
        }
    }
}
