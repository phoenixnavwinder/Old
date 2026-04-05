using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Zlib;

namespace WinderProject
{
    public partial class frm_CoilParameters : Form
    {
        private Logz lz; //log file
        private double[] cparray;

        public frm_CoilParameters(Logz myLog)
        {
            InitializeComponent();
            lz = myLog;
            readCoilParamzFile();
        }

        private void frm_CoilParameters_Load(object sender, EventArgs e)
        {

        }

        private bool readCoilParamzFile()
        {
            bool cpzok = true;
            Parmz cpz = new Parmz(lz, @"..\..\Coil.parmz", 5, ref cpzok);
            if (!cpzok)
            {
                lz.Bl();
                lz.Sl("Error reading Coil parameters file");
                MessageBox.Show("Error reading Coil parameters file", "Read Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return false;
            }

            cparray = cpz.Parray;  // retrieve the coil parameters
            int n = 0;
            txtHubDiameter.Text = cparray[n++].ToString();        // inches
            txtFlangeWidth.Text = cparray[n++].ToString();          // flange-to-flange, inches
            txtLayers.Text = cparray[n++].ToString();     // # of layers to wind
            txtBaseTurns.Text = cparray[n++].ToString(); // # turns on layer 1
            txtFiberDiameter.Text = (cparray[n++] / 25400.0).ToString();   // inches (from microns)
            return true;
        }
    }
}
