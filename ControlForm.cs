﻿using ASCOM.LocalServer;
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
        public ControlForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Firefly.Stop();
        }

        public void SetStatus(string text)
        {
            lblStatus.Text = text;
        }

        public void SetText(string text)
        {
            lblText.Text = text;
        }
    }
}
