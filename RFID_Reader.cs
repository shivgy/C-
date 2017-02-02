using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Management;
using System.IO.Ports;
using System.Configuration;
using System.Threading;
using System.Data.SqlClient;
using MSDataAccess;



namespace RFID_Reader
{
    public partial class Form1 : Form
    {
                public static byte[] objBytes = new byte[1024];


        #region Server Connection
        public string getConnectionString()
        {
            string strcon = null;
            //Define the connection String in App.config file
            //<appSettings>
		        //<add key="PORT" value="COM3"/>
		        //<add key="CONNECTIONSTRING" value="Data Source =111.111.111.111; Initial Catalog=DatabaseName;Persist Security Info=True;User ID=userID;Password=Password"/>
            //</appSettings>
            strcon = System.Configuration.ConfigurationManager.AppSettings["CONNECTIONSTRING"];
            return strcon;
        }

        public bool IsServerConnected()
        {
            using (var l_oConnection = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["CONNECTIONSTRING"]))
            {
                try
                {
                    l_oConnection.Open();
                    return true;
                }
                catch (SqlException)
                {
                    return false;
                    this.serialPort.Close();
                }
            }
        }
        #endregion

        public Form1()
        {
            InitializeComponent();
            string[] portnames = SerialPort.GetPortNames();
            cboPortName.Items.Clear();
            foreach (string s in portnames) //add this names to comboboxPort items
            {
                cboPortName.Items.Add(s);
            }

            this.serialPort.PortName = cboPortName.ToString();
            this.serialPort.BaudRate = 9600;
            this.serialPort.DataBits = 8;
            this.serialPort.Parity = Parity.None;
            this.serialPort.StopBits = StopBits.One;
            this.serialPort.DtrEnable = true;
            this.serialPort.RtsEnable = true;
            this.serialPort.Handshake = Handshake.RequestToSend;
                this.serialPort.NewLine = System.Environment.NewLine;
                this.serialPort.Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (!this.serialPort.IsOpen)
            {
                try
                {
                    string[] portnames = SerialPort.GetPortNames();
                    this.serialPort.PortName = portnames[0];
                    cboPortName.Text = portnames[0];
                    this.lblConnectionStatus.Text = "Port Not Connected";
                    this.serialPort.Open();
                }
                catch (InvalidOperationException ex)
                {
                    this.serialPort.Close();
                    MessageBox.Show("Please connect the USB and Restart the Program", "Port Not Connected", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;

                }
            }
            this.serialPort.DiscardInBuffer();
            this.serialPort.DiscardOutBuffer();


            cboParity.Items.Add("NONE");
            cboParity.Items.Add("Even");
            cboParity.Items.Add("Odd");
            cboParity.SelectedItem = "NONE";

            cboStopBits.Items.Add("One");
            cboStopBits.Items.Add("Two");
            cboStopBits.SelectedItem = "One";

            cboDataBits.Items.Add("7");
            cboDataBits.Items.Add("8");
            cboDataBits.SelectedItem = "8";

            cboBaudRate.Items.Add("75");
            cboBaudRate.Items.Add("110");
            cboBaudRate.Items.Add("300");
            cboBaudRate.Items.Add("2400");
            cboBaudRate.Items.Add("4800");
            cboBaudRate.Items.Add("9600");
            cboBaudRate.Items.Add("19200");
            cboBaudRate.Items.Add("38400");
            cboBaudRate.Items.Add("57600");
            cboBaudRate.Items.Add("115200");
            cboBaudRate.SelectedItem = "9600";

            cboReadTimeOut.Items.Add("5000");
            cboReadTimeOut.SelectedItem = "5000";

            cboWriteTimeOut.Items.Add("5000");
            cboWriteTimeOut.SelectedItem = "5000";
        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.serialPort.Close();
            e.Cancel = false;
        }

        private void readCardNO()
        {
            int iRet = 0;
            String recvData;
            if (serialPort.IsOpen && serialPort.BytesToRead != 0)
            {
                try
                {
                    iRet = serialPort.Read(objBytes, 0, 1024);
                    recvData = System.Text.Encoding.ASCII.GetString(objBytes, 0, iRet);
                    lblCardNo.Text = recvData;
                    Array.Clear(objBytes, 0, 20);
                }
                catch (InvalidOperationException ex)
                {
                    this.serialPort.Close();
                    MessageBox.Show(ex.ToString(), "No Data Received", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            this.serialPort.DiscardInBuffer();
            this.serialPort.DiscardOutBuffer();
        }
        private void button4_Click(object sender, EventArgs e)
        {
            button4.Enabled = false;
            if (this.serialPort.IsOpen)
            {
                readCardNO();
                txtCardNo.Text = lblCardNo.Text;
                button4.Enabled = true;
            }
            else
            {
                MessageBox.Show("Please connect the USB", "Port Not Connected", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                btnConnect.Enabled = true;
            }
            
        }

        private void btnFetch_Click(object sender, EventArgs e)
        {
            try
            {
                txtEmpID.ReadOnly = true;
                txtName.ReadOnly = true;
                txtCardNo.ReadOnly = true;
                if (txtEmpID1.Text == "")
                {
                    MessageBox.Show("Please Enter EmployeeId");
                }
                else
                {
                    DataSet ds = new DataSet();
                    object[] obj = new object[1];
                    obj[0] = txtEmpID1.Text;
                    ds = MSDataAccess.DataAccess.ExecuteDataset(getConnectionString(), "StoredProcedureName", obj);
                    if (ds != null)
                    {
                        if (ds.Tables[0].Rows[0]["Status"].ToString() == "True")
                        {
                            txtEmpID.Text = ds.Tables[0].Rows[0]["EmpCode"].ToString();
                            txtName.Text = ds.Tables[0].Rows[0]["Name"].ToString();
                            txtCardNo.Text = ds.Tables[0].Rows[0]["CardNo"].ToString();
                        }
                        else
                        {
                            MessageBox.Show(ds.Tables[0].Rows[0]["Message"].ToString());
                            txtEmpID.Text = "";
                            txtName.Text = "";
                            txtCardNo.Text = "";

                        }

                    }
                }
            }catch(SqlException ex){
                this.serialPort.Close();
                MessageBox.Show("Could Not Connect Database, Check your Internet Connection. Check if any firewall is there.", "Internet Connection Problem", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
              DataSet ds = new DataSet();
                object[] obj = new object[2];
                obj[0] = txtEmpID.Text;
                obj[1] = txtCardNo.Text;
                ds = MSDataAccess.DataAccess.ExecuteDataset(getConnectionString(), "StoredProcedureName2", obj);
                if (ds != null)
                {
                    MessageBox.Show(ds.Tables[0].Rows[0]["Response"].ToString());
                }
                else
                {
                    MessageBox.Show("Please try again!");
                }
            
        }

        #region Port Connection
        private void btnConnect_Click(object sender, EventArgs e)
        {
            int count = 0;
            if (!string.IsNullOrEmpty(cboPortName.Text))
            {
                while (count <= 3)
                {

                    if (!this.serialPort.IsOpen)
                    {
                        this.lblConnectionStatus.Text = "Not Connected";
                        this.serialPort.PortName = cboPortName.Text;
                        try
                        {
                            this.serialPort.Open();
                        }
                        catch(InvalidOperationException ex){
                            this.serialPort.Close();
                            MessageBox.Show("Please connect the USB and Restart the Program", "Port Not Selected", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                            return;
                        }
                    }

                    if (this.serialPort.IsOpen)
                    {
                        this.lblConnectionStatus.Text = "Connected";
                        btnConnect.Enabled = false;
                        break;
                    }
                    else
                        count = count++;
                    serialPort.ReadTimeout = 5000;
                }

            }
            else
                MessageBox.Show("Check the port", "Port Not Selected", MessageBoxButtons.OK, MessageBoxIcon.Hand);
        }
        #endregion
    }
}
