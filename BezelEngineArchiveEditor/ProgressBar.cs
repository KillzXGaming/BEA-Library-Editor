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
    public partial class ProgressBar : Form
    {
        public ProgressBar()
        {
            InitializeComponent();
        }

        public int Value
        {
            set
            {
                progressBar1.Value = value;
                if (value >= 100)
                    Close();
                progressBar1.Refresh();
            }
        }
        public string Task
        {
            set
            {
                label1.Text = value;
                label1.Refresh();
            }
        }

        private void ProgressBar_FormClosed(object sender, FormClosedEventArgs e)
        {
        }
    }
}
