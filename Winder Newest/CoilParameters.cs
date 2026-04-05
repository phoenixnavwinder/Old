/*------------------------------------------------------------------------------
File: CoilParameters.cs                                         Updated: 06/04/2013

Objective: assists in reading and editing parameter files
See Also:
Problems: No saving implimented yet
Modifications:
 Date       Who   Comments
--------------------------------------------------------------------------------
This form calls the Paramz.cs class to read the contents of the coil.paramz text file.
 * It displays the contents of the text file and will eventually allow editing.
 * 
 * Alot of this code was copied from Winder.CoilParmzRead()
------------------------------------------------------------------------------*/
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
        private Logz lz; //log file object
        private double[] cparray; //an array that will hold the values in the paramz file after reading
        bool cpzok; //is the paramz file ok or in error state
        Parmz cpz;  //local instance of paramz class

        //These instance variables hold the original values in the coil.paramz file incase changes need to be undone
        private string instHubDiameter;
        private string instFlangeWidth;
        private string instLayers;
        private string instBaseTurns;
        private string instFiberDiameter;

        public frm_CoilParameters(Logz myLog)  //Constructor method that takes in a log file object
        {
            InitializeComponent();
            lz = myLog;
        }

        private void frm_CoilParameters_Load(object sender, EventArgs e)
        {
            readCoilParamzFile();
        }

        private bool readCoilParamzFile() //this method reads the coil.paramz file and fills in the forms text boxes
        {
            cpzok = true;  
            cpz = new Parmz(lz, @"..\..\Coil.parmz", 5, ref cpzok); //create new paramz object passing path to coil.paramz file
            if (!cpzok) //if error
            {
                lz.Bl();
                lz.Sl("Error reading Coil parameters file"); //Log error
                MessageBox.Show("Error reading Coil parameters file", "Read Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return false; //exit with error
            }

            cparray = cpz.Parray;  // retrieve the coil parameters
            int n = 0; // counter

            //read through each value of the array and populate the text boxes
            txtHubDiameter.Text = cparray[n++].ToString();        // inches
            txtFlangeWidth.Text = cparray[n++].ToString();          // flange-to-flange, inches
            txtLayers.Text = cparray[n++].ToString();     // # of layers to wind
            txtBaseTurns.Text = cparray[n++].ToString(); // # turns on layer 1
            txtFiberDiameter.Text = cparray[n++].ToString();   // microns

            //set instance variable values
            instHubDiameter = txtHubDiameter.Text;
            instFlangeWidth = txtFlangeWidth.Text;
            instLayers = txtLayers.Text;
            instBaseTurns = txtBaseTurns.Text;
            instFiberDiameter = txtFiberDiameter.Text;

            return true; //exit without error
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            double[] newParamz = new double[5];

            try
            {
                newParamz[0] = double.Parse(txtHubDiameter.Text);
                newParamz[1] = double.Parse(txtFlangeWidth.Text);
                newParamz[2] = double.Parse(txtLayers.Text);
                newParamz[3] = double.Parse(txtBaseTurns.Text);
                newParamz[4] = double.Parse(txtFiberDiameter.Text);

                if (cpz.Save(@"..\..\Coil.parmz", newParamz))
                {
                    MessageBox.Show("Coil parameters saved succesfully", "Save Complete", MessageBoxButtons.OK, MessageBoxIcon.None);
                }
                else
                {
                    MessageBox.Show("There was an error saving the parameters", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                lz.Sl(" -- improper value in paramz textbox");
                lz.Sl(" -- exception is: " + ex.ToString());
            }

        }

        private void btnUndo_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure you want to cancel your changes?", "Cancel", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                txtHubDiameter.Text = instHubDiameter;
                txtFlangeWidth.Text = instFlangeWidth;
                txtLayers.Text = instLayers;
                txtBaseTurns.Text = instBaseTurns;
                txtFiberDiameter.Text = instFiberDiameter;
            }
            //else if (dialogResult == DialogResult.No)
            //{
            //    //do something else
            //}
        }
    }
}
