using ASCOM.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ASCOM.photonSeletek.Dome
{
    [ComVisible(false)] // Form not registered for COM!
    public partial class SetupDialogForm : Form
    {
        const string NO_PORTS_MESSAGE = "No COM ports found";
        TraceLogger tl; // Holder for a reference to the driver's trace logger

        public SetupDialogForm(TraceLogger tlDriver)
        {
            InitializeComponent();

            // Save the provided trace logger for use within the setup dialogue
            tl = tlDriver;

            // Initialise current values of user settings from the ASCOM Profile
            InitUI();
        }

        private void CmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            // Place any validation constraint checks here and update the state variables with results from the dialogue

            tl.Enabled = chkTrace.Checked;

      
            if (cmbRoofClosedSensor.SelectedItem is null) // No COM port selected
            {
                tl.LogMessage("Setup OK", $"New configuration values - Roof Closed Sensor: Not selected");
            }
            else // A valid COM port has been selected
            {
                
                tl.LogMessage("Setup OK", $"New configuration values - Roof Closed Sensor: {cmbRoofClosedSensor.SelectedItem}");
                DomeHardware.seletekSensorRoofClosed = int.Parse(cmbRoofClosedSensor.SelectedItem.ToString());
            }

            if (cmbRoofOpenSensor.SelectedItem is null) // No COM port selected
            {
                tl.LogMessage("Setup OK", $"New configuration values - Roof Open Sensor: Not selected");
            }
            else // A valid COM port has been selected
            {
                
                tl.LogMessage("Setup OK", $"New configuration values - Roof Open Sensor: {cmbRoofOpenSensor.SelectedItem}");
                DomeHardware.seletekSensorRoofOpen = int.Parse(cmbRoofOpenSensor.SelectedItem.ToString());
            }

            if (cmbRoofRelayNo.SelectedItem is null) // No COM port selected
            {
                tl.LogMessage("Setup OK", $"New configuration values - Roof Relay No: Not selected");
            }
            else // A valid COM port has been selected
            {
             
                tl.LogMessage("Setup OK", $"New configuration values - Roof Relay No: {cmbRoofRelayNo.SelectedItem}");
                DomeHardware.seletekRelayNo = int.Parse(cmbRoofRelayNo.SelectedItem.ToString());
            }

            if (txtNoMotionTimeout.Text is null) // No COM port selected
            {
                tl.LogMessage("Setup OK", $"New configuration values - No Motion Timeout: Not selected");
            }
            else // A valid COM port has been selected
            {
                
                tl.LogMessage("Setup OK", $"New configuration values - No Motion Timeout: {txtNoMotionTimeout.Text}");
                DomeHardware.seletekNoMotion = int.Parse(txtNoMotionTimeout.Text);
            }

            if (txtTotalTimeout.Text is null) // No COM port selected
            {
                tl.LogMessage("Setup OK", $"New configuration values - Total Timeout: Not selected");
            }
            else // A valid COM port has been selected
            {
                
                tl.LogMessage("Setup OK", $"New configuration values - Total Timeout: {txtTotalTimeout.Text}");
                DomeHardware.seletekTotalTimeout = int.Parse(txtTotalTimeout.Text);
            }   

            if (txtRelayPause.Text is null) // No COM port selected
            {
                tl.LogMessage("Setup OK", $"New configuration values - Relay Pause: Not selected");
            }
            else // A valid COM port has been selected
            {
                
                tl.LogMessage("Setup OK", $"New configuration values - Relay Pause: {txtRelayPause.Text}");
                DomeHardware.seletekRelayPause = int.Parse(txtRelayPause.Text);
            }

            if (txtSensorPolling.Text is null) // No COM port selected
            {
                tl.LogMessage("Setup OK", $"New configuration values - Sensor Polling: Not selected");
            }
            else // A valid COM port has been selected
            {
                
                tl.LogMessage("Setup OK", $"New configuration values - Sensor Polling: {txtSensorPolling.Text}");
                DomeHardware.seletekSensorPolling = int.Parse(txtSensorPolling.Text);
            }
      
        }

        private void CmdCancel_Click(object sender, EventArgs e) // Cancel button event handler
        {
            Close();
        }

        private void BrowseToAscom(object sender, EventArgs e) // Click on ASCOM logo event handler
        {
            try
            {
                System.Diagnostics.Process.Start("https://ascom-standards.org/");
            }
            catch (Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }

        private void InitUI()
        {

            // Set the trace checkbox
            chkTrace.Checked = tl.Enabled;

            

            if (cmbRoofClosedSensor.Items.Contains(DomeHardware.seletekSensorRoofClosed.ToString()))
            {
                cmbRoofClosedSensor.SelectedItem = DomeHardware.seletekSensorRoofClosed.ToString();
            }

            if (cmbRoofOpenSensor.Items.Contains(DomeHardware.seletekSensorRoofOpen.ToString()))
            {
                cmbRoofOpenSensor.SelectedItem = DomeHardware.seletekSensorRoofOpen.ToString();
            }

            if (cmbRoofRelayNo.Items.Contains(DomeHardware.seletekRelayNo.ToString()))
            {
                cmbRoofRelayNo.SelectedItem = DomeHardware.seletekRelayNo.ToString();
            }
            
            txtNoMotionTimeout.Text = DomeHardware.seletekNoMotion.ToString();
            txtTotalTimeout.Text = DomeHardware.seletekTotalTimeout.ToString();
            txtRelayPause.Text = DomeHardware.seletekRelayPause.ToString();
            txtSensorPolling.Text = DomeHardware.seletekSensorPolling.ToString();


            tl.LogMessage("InitUI", $"Set UI controls to Trace: {chkTrace.Checked}");
        }

        private void SetupDialogForm_Load(object sender, EventArgs e)
        {
            // Bring the setup dialogue to the front of the screen
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;
            else
            {
                TopMost = true;
                Focus();
                BringToFront();
                TopMost = false;
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void comboBoxComPort_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label12_Click(object sender, EventArgs e)
        {

        }
    }
}