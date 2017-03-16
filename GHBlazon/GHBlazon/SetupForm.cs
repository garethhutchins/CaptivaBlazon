using Emc.InputAccel.CaptureClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GHBlazon
{
    public partial class SetupForm : Form
    {
        
        private  IValueAccessor StepConfig;
        public SetupForm(IValueAccessor sc)
        {
            InitializeComponent();
            StepConfig = sc;
            txt_url.Text = sc.ReadString("BlazonURL", "");
            txt_temp.Text = sc.ReadString("BlazonTempDIR", "");
            numericUpDown1.Value = Convert.ToDecimal(sc.ReadDouble("WaitTime", 5));
            chk_Clean.Checked = sc.ReadBoolean("CleanUp", false);
        }
       
        private void btn_ok_Click(object sender, EventArgs e)
        {
            //Save the changes back
            StepConfig.WriteString("BlazonURL", txt_url.Text);
            StepConfig.WriteString("BlazonTempDIR", txt_temp.Text);
            StepConfig.WriteDouble("WaitTime", Convert.ToDouble(numericUpDown1.Value));
            StepConfig.WriteBoolean("CleanUp",chk_Clean.Checked);
            this.Close();
        }

        private void btn_cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btn_browse_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog();
            if (folderBrowserDialog1.SelectedPath != "")
            {
                txt_temp.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void SetupForm_Load(object sender, EventArgs e)
        {

        }
    }
}
