using ASCOM.LocalServer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ASCOM.photonSeletek.Dome
{
    public partial class ControlForm : Form
    {
        private const int CP_NOCLOSE_BUTTON = 0x200;
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams myCp = base.CreateParams;
                myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE_BUTTON;
                return myCp;
            }
        }
        public ControlForm()
        {
            InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await Task.Run(() => Firefly.Stop());        
        }

        public void UpdateStatus(string text)
        {
            lblStatus.Text = text;
        }

        public void SetText(string text, bool clear=false)
        {
            //prefix with date and time            

            text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss\t") + " " + text;
            if (clear) 
                txtLog.Text = text + Environment.NewLine;
            else
                txtLog.AppendText(text + Environment.NewLine);
        }

    

        private async void button3_Click(object sender, EventArgs e)
        {
            
           
            try { await Task.Run(() => Firefly.Close()); }
            catch (DriverException)
            {
                DriverException dex = new DriverException("Error closing the roof. Please check the roof state and try again.");
                MessageBox.Show(dex.Message);
            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {            
            try { await Task.Run(() => Firefly.Open());  }
            catch (DriverException)
            {
                DriverException dex = new DriverException("Error opening the roof. Please check the roof state and try again.");
                MessageBox.Show(dex.Message);
            }            
        }
    }
}
