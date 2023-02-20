/*
 * Project:     Serial Port Interface with C#
 * Company:     StudentCompanion
 * Created:     April 2013
 * Updated:    February 2020
 * Visual Studio: 2019
 * .NET Framework: 4.6.2
 * Notes:       This is a simple demontration on how to use the SerialPort control for
 *              communicating with your PC's  COM Port. 
 *              This is for educational purposes only and not for any commercial use. 
 *              For more on this, please visit: https://www.studentcompanion.co.za/creating-a-serial-port-interface-with-c/
 * */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;  //This is a namespace that contains the SerialPort class


namespace ComPortInterfaceCode
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            updatePorts();           //Call this function everytime the page load 
                                     //to update port names
            CheckForIllegalCrossThreadCalls = false;
        }
        private void updatePorts()
        {
            // Retrieve the list of all COM ports on your Computer
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                cmbPortName.Items.Add(port);
            }
        }
        private SerialPort ComPort = new SerialPort();  //Initialise ComPort Variable as SerialPort
        private void connect()
        {
            bool error = false;

            // Check if all settings have been selected

            if (cmbPortName.SelectedIndex != -1 & cmbBaudRate.SelectedIndex != -1 & cmbParity.SelectedIndex != -1 & cmbDataBits.SelectedIndex != -1 & cmbStopBits.SelectedIndex != -1)
            {
                //if yes than Set The Port's settings
                ComPort.PortName = cmbPortName.Text;
                ComPort.BaudRate = int.Parse(cmbBaudRate.Text);      //convert Text to Integer
                ComPort.Parity = (Parity)Enum.Parse(typeof(Parity), cmbParity.Text); //convert Text to Parity
                ComPort.DataBits = int.Parse(cmbDataBits.Text);        //convert Text to Integer
                ComPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), cmbStopBits.Text);  //convert Text to stop bits

                try  //always try to use this try and catch method to open your port. 
                     //if there is an error your program will not display a message instead of freezing.
                {
                    //Open Port
                    ComPort.Open();
                    ComPort.DataReceived += SerialPortDataReceived;  //Check for received data. When there is data in the receive buffer,
                                                                     //it will raise this event, we need to subscribe to it to know when there is data
                }
                catch (UnauthorizedAccessException) { error = true; }
                catch (System.IO.IOException) { error = true; }
                catch (ArgumentException) { error = true; }

                if (error) MessageBox.Show(this, "Could not open the COM port. Most likely it is already in use, has been removed, or is unavailable.", "COM Port unavailable", MessageBoxButtons.OK, MessageBoxIcon.Stop);

            }
            else
            {
                MessageBox.Show("Please select all the COM Serial Port Settings", "Serial Port Interface", MessageBoxButtons.OK, MessageBoxIcon.Stop);

            }
            //if the port is open, Change the Connect button to disconnect, enable the send button.
            //and disable the groupBox to prevent changing configuration of an open port.
            if (ComPort.IsOpen)
            {
                btnConnect.Text = "Disconnect";
                btnSend.Enabled = true;
                if (!rdText.Checked & !rdHex.Checked)  //if no data mode is selected, then select text mode by default
                {
                    rdText.Checked = true;
                }
                groupBox1.Enabled = false;


            }
        }
        // Call this function to close the port.
        private void disconnect()
        {
            ComPort.Close();
            btnConnect.Text = "Connect";
            btnSend.Enabled = false;
            groupBox1.Enabled = true;

        }
        //whenever the connect button is clicked, it will check if the port is already open, call the disconnect function.
        // if the port is closed, call the connect function.
        private void btnConnect_Click(object sender, EventArgs e)

        {
            if (ComPort.IsOpen)
            {
                disconnect();
            }
            else
            {
                connect();
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            //Clear the screen
            rtxtDataArea.Clear();
            txtSend.Clear();
        }
        // Function to send data to the serial port
        private void sendData()
        {
            bool error = false;
            if (rdText.Checked == true)        //if text mode is selected, send data as tex
            {
                // Send the user's text straight out the port 
                ComPort.Write(txtSend.Text);

                // Show in the terminal window 
                rtxtDataArea.ForeColor = Color.Green;    //write sent text data in green colour              
                txtSend.Clear();                       //clear screen after sending data

            }
            else                    //if Hex mode is selected, send data in hexadecimal
            {
                try
                {
                    // Convert the user's string of hex digits (example: E1 FF 1B) to a byte array
                    byte[] data = HexStringToByteArray(txtSend.Text);

                    // Send the binary data out the port
                    ComPort.Write(data, 0, data.Length);

                    // Show the hex digits on in the terminal window
                    rtxtDataArea.ForeColor = Color.Blue;   //write Hex data in Blue
                    rtxtDataArea.AppendText(txtSend.Text.ToUpper() + "\n");
                    txtSend.Clear();                       //clear screen after sending data
                }
                catch (FormatException) { error = true; }

                // Inform the user if the hex string was not properly formatted
                catch (ArgumentException) { error = true; }

                if (error) MessageBox.Show(this, "Not properly formatted hex string: " + txtSend.Text + "\n" + "example: E1 FF 1B", "Format Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);

            }
        }
        //Convert a string of hex digits (example: E1 FF 1B) to a byte array. 
        //The string containing the hex digits (with or without spaces)
        //Returns an array of bytes. </returns>
        private byte[] HexStringToByteArray(string s)
        {
            s = s.Replace(" ", "");
            byte[] buffer = new byte[s.Length / 2];
            for (int i = 0; i < s.Length; i += 2)
                buffer[i / 2] = (byte)Convert.ToByte(s.Substring(i, 2), 16);
            return buffer;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            sendData();
        }
        //This event will be raised when the form is closing.
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (ComPort.IsOpen) ComPort.Close();  //close the port if open when exiting the application.
        }
        //Data recived from the serial port is coming from another thread context than the UI thread.
        //Instead of reading the content directly in the SerialPortDataReceived, we need to use a delegate.
        delegate void SetTextCallback(string text);
        private void SetText(string text)
        {
            //invokeRequired required compares the thread ID of the calling thread to the thread of the creating thread.
            // if these threads are different, it returns true
            if (this.rtxtDataArea.InvokeRequired)
            {
                rtxtDataArea.ForeColor = Color.Green;    //write text data in Green colour

                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.rtxtDataArea.AppendText(text);
            }
        }
        private void SerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var serialPort = (SerialPort)sender;
            var data = serialPort.ReadExisting();
            SetText(data);
        }
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            updatePorts();           //Call this function to update port names
        }
    }
}