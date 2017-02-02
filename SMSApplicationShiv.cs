using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;

using System.IO.Ports;
using System.Configuration;
using System.Threading;
using System.Data.SqlClient;
using MSDataAccess;


namespace SMSApplicationShiv
{
    public partial class Form1 : Form
    { 

        #region Variables

        public static byte[] objAtCMD;
        public static byte[] objBytes = new byte[1024];
        String RxString;
        int msgNum, mobileNum;

        #endregion

        #region Constructor
        public Form1()
        { 

            InitializeComponent();

            string[] portnames = SerialPort.GetPortNames();
            cboPortName.Items.Clear();

            foreach (string s in portnames)                 //add this names to comboboxPort items
            {
                cboPortName.Items.Add(s);
            }
            this.serialPort.PortName = cboPortName.ToString();
            this.serialPort.DtrEnable = true;
            this.serialPort.RtsEnable = true;
            this.serialPort.Handshake = Handshake.RequestToSend;
            this.serialPort.NewLine = System.Environment.NewLine;
            this.serialPort.Close();
        }
        #endregion

        #region Server Connection
        public string getConnectionString()
        {
            string strcon = null;
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
                }
            }
        }
        #endregion

        #region Write Status Bar
        private void WriteStatusBar(string status)
        {
            try
            {
                statusBar1.Text = "Message: " + status;
            }
            catch (Exception ex)
            {

            }
        }
        #endregion

        #region Private Event

        private void btn_start_Click(object sender, EventArgs e)
        {
            
            if (!serialPort.IsOpen)
            {
                MessageBox.Show("Connect to the port", "Port Not Connected", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            if (serialPort.IsOpen)
            {
                timer2.Enabled = true;
            }
        }
         
        public string execute_ATCommand(string command, int sleeptime)
        {
            string recvData;
            int iRet = 0;

            if (!serialPort.IsOpen)
                serialPort.Open();

            if (serialPort.IsOpen)
            {
                try
                {
                    objAtCMD = System.Text.Encoding.ASCII.GetBytes(command);
                    serialPort.Write(objAtCMD, 0, objAtCMD.Length);
                    Thread.Sleep(sleeptime);

                    iRet = serialPort.Read(objBytes, 0, 1024);
                    recvData = System.Text.Encoding.ASCII.GetString(objBytes, 0, iRet);

                    if ((recvData.Length == 0) || ((!recvData.EndsWith("\r\n> ")) && (!recvData.EndsWith("\r\nOK\r\n"))))
                    {
                        WriteStatusBar("No Success message was received");
                    }
                    readModemResponse(recvData);
                    WriteStatusBar(recvData);
                      
                }
                catch (Exception e)
                {
                    recvData = "ERROR" + Environment.NewLine;
                }
                 
                return recvData;
                //End of If statement (serial port is open)
            }
            return "PORT NOT OPEN";
            //END class execute_ATCommand
        }

        public void readSimcardMessage()
        {
            if (!serialPort.IsOpen)
                serialPort.Open();

            RxString = execute_ATCommand("AT\r\n", 1000);
            RxString = execute_ATCommand("AT+CPMS?\r\n", 1000);
            readModemResponse(RxString);
            WriteStatusBar(msgNum + " SMS present in Inbox");
            if (msgNum == 0)
            {
                serialPort.Close();
                WriteStatusBar("Press Start Button to run again");
            }
            else
            {
                bool b = true;
                while (b)
                {
                    RxString = execute_ATCommand("AT+CMGL=\"ALL\"\r\n", 1000);
                    readModemResponse(RxString);
                    //Read messages one by one
                    for (int count = 1; count <= msgNum; count++)//Sim card can store only 25 messages
                    {
                        RxString = execute_ATCommand("AT+CMGR=" + count + "\r\n", 1000);
                        readModemResponse(RxString);

                        //Delete read messages to free storage space
                        RxString = execute_ATCommand("AT+CMGD=" + count + "\r\n", 1000);
                        readModemResponse(RxString);

                        WriteStatusBar(count + " SMS Read");
                        //END of for loop
                    }
                    RxString = execute_ATCommand("AT+CPMS?\r\n", 1000);
                    readModemResponse(RxString);
                    if (RxString == "0")
                    {
                        b = false;
                        WriteStatusBar("NO SMS in Inbox");
                    }
                    else
                        b = true;
                }
                //check if no message exist
                 
            }
            
            //END of class readSimcardMessage
        }

        public string readModemResponse(string modemResponse)
        {
            modemResponse = modemResponse.Replace("\r", " ").Trim();
            if (modemResponse.Contains("+CMT:"))
            {
                ReadData(modemResponse);
            }
            else
            {
                string[] respArr = modemResponse.Split('\n');
                for (int j = 0; j < respArr.Length; j++)
                {
                    if (respArr[j].Contains("+CPMS"))
                    {
                        string[] nOfMsg = respArr[0].Split(',');
                        RxString = nOfMsg[1];
                        msgNum = Int32.Parse(RxString);
                        break;
                    }
                    else if (respArr[j].Contains("+CMGR"))
                    {
                        string[] nOfMsg = respArr[0].Split(',');
                        string mobile = nOfMsg[1].Replace("\"", " ").Trim();
                        mobile = mobile.Substring(3, 10);
                        string response = respArr[j + 1];
                        DataSet ds = new DataSet();
                        object[] ob = new object[2];
                        ob[0] = mobile;
                        ob[1] = response;
                        ds = DataAccess.ExecuteDataset(getConnectionString(), "InsertSimcardResponse", ob);
                        RxString = "0";
                        listBox2.Items.Add(mobile + ">>>" + response);
                        textBox2.Text = listBox2.Items.Count.ToString();
                    }
                    else if (respArr[j].Contains("+CMGL"))
                    {

                    }

                }
            }
           
            return RxString;
            //END class readModemResponse
        }

        public string sendSMS(int id, string mobileNo, string msg)
        {
            

            if (!this.serialPort.IsOpen)
                this.serialPort.Open();

            if (serialPort.IsOpen)
            {
                //readModemResponse(RxString);
                RxString = execute_ATCommand("AT\r\n", 300);
                if (RxString != "ERROR")
                {
                    //Presentation format of SMS from Modem; 1 = text mode
                    RxString = execute_ATCommand("AT+CMGF=1\r\n", 1000);
                    WriteStatusBar("Set SMS format to TEXT mode " + RxString);
                    readModemResponse(RxString);

                    //Send SMS from the modem to the network
                    RxString = execute_ATCommand("AT+CMGS=\"" + mobileNo + "\"\r\n", 500);
                    
                    if (RxString.Contains(">"))
                    {
                        WriteStatusBar("Sending message...");
                        RxString = execute_ATCommand(msg + "\x1A", 5000);//ASCII code for ctrl+z is \x1A
                        Thread.Sleep(1000);
                        if (RxString.Equals("\r"))
                        {
                            RxString = "OK";
                            WriteStatusBar("Message Sent");
                            listBox1.Items.Add(mobileNo+">>>"+"Message Sent");
                            
                            updateSMSMobileId(id);
                        } 
                    }
                    else if (RxString.Contains("+CME") && RxString.Contains("ERROR"))
                    {
                        WriteStatusBar("ERROR");
                        //Do nothing when sms is not sent
                    }
                    if (RxString.Contains("+CMGS") && RxString.Contains("OK"))
                    {
                        updateSMSMobileId(id);
                        WriteStatusBar("Message Sent");
                        listBox1.Items.Add(mobileNo + ">>>" + "Message Sent");
                    }
                     if (RxString.Contains("+CMTI"))
                    {
                        readModemResponse(RxString);
                    }
                }
            }
            return RxString;
            //END class sendSMS
        }

        private void updateSMSMobileId(int id)
        {
            try
            {
                DataSet ds = new DataSet();
                object[] ob = new object[1];
                ob[0] = id;
                ds = DataAccess.ExecuteDataset(getConnectionString(), "UpdateSimCardDetails", ob);
            }
            catch (Exception d)
            {
                Console.Write(d.ToString());
            }
            //END class updateSMSMobileId
        }
 
        //END partial class Form1

        #endregion

        #region Button Disconnect
        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            
            this.serialPort.Close();
            WriteStatusBar("Port Disconnected");
            this.lblConnectionStatus.Text = "Not Connected";
            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
        }
        #endregion

        #region Excel Upload
        private void tabPage2_Click(object sender, EventArgs e)
        {

        }
        #endregion

        private void btnStop_Click(object sender, EventArgs e)
        {
            this.serialPort.Close();
            btn_start.Enabled = true;
            btn_Stop.Enabled = false;
            timer2.Enabled = false;
            WriteStatusBar("STOPPED");
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (!this.serialPort.IsOpen)
            {
                try
                {
                    this.lblConnectionStatus.Text = "Not Connected";
                    this.serialPort.Open();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Port Not Connected", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            this.serialPort.DiscardInBuffer();
            this.serialPort.DiscardOutBuffer();

            //Stop command echo from GSM Modem
            RxString = execute_ATCommand("ATE0\r\n", 1000);
            serialPort.ReadTimeout = 5000;
            
            WriteStatusBar("Stop Echo" + RxString);
             

            // Phone functionality; 1 = full functionality
            RxString = execute_ATCommand("AT+CFUN=1\r\n", 1000);
            WriteStatusBar("Full Functionality" + RxString);

            //Presentation format of SMS from Modem; 1 = text mode
            RxString = execute_ATCommand("AT+CMGF=1\r", 1000);
            WriteStatusBar("SMS Format TEXT mode" + RxString);

            //Set SMS storage to SIM memory

            //Set New message indication to buffer
            RxString = execute_ATCommand("AT+CNMI=2,2,0,0,0\r", 1000);
            WriteStatusBar("New message indication" + RxString);

            if (IsServerConnected())
                WriteStatusBar("SQL Connected");
            else
                WriteStatusBar("SQL NOT Connected");

            int id;
            string SimcardNo = "";
            string msg = "";
            DataSet ds = new DataSet();

            ds = DataAccess.ExecuteDataset(getConnectionString(), "GetSimCardDetails", null);

            int rowCount = ds.Tables[0].Rows.Count;
            for (int count = 0; count < rowCount; count++)
            {
                try
                {
                    id = Convert.ToInt32(ds.Tables[0].Rows[count]["slno"].ToString().TrimEnd());
                    SimcardNo = ds.Tables[0].Rows[count]["SimcardNo"].ToString().TrimEnd();
                    msg = ds.Tables[0].Rows[count]["MsgContent"].ToString().TrimEnd();
                    RxString = sendSMS(id, SimcardNo, msg);
                    if (RxString.Contains("OK"))
                        WriteStatusBar(count++ + "SMS Sent");
                    textBox1.Text = count++.ToString();
                }
                catch (Exception d)
                {
                    Console.Write(d.ToString());
                    WriteStatusBar(d.ToString());
                }
                //End of for loop to read the list of mobile number where SMS has to be sent
            }

            if (rowCount == 0)
                this.serialPort.Close();
        }
        
        #region Read Data
        private void ReadData(string dataString)
        {
            dataString = dataString.Replace('\n', ' ');
            dataString = dataString.Replace('\r', ' ');
            string[] rawData = dataString.Replace("+CMT:", "~!").Split('~');

            for (int v = 0; v < rawData.Length; v++)
            {
                WriteStatusBar(rawData.ToString());
                string[] data = rawData[v].Split('"');
                if (data[0].Contains("!"))
                {
      try
                    {
                        string mobile = data[1];
                        string response = data[data.Length - 1];


                        DataSet ds = new DataSet();
                        object[] ob = new object[2];
                        ob[0] = mobile;
                        ob[1] = response;
                        ds = DataAccess.ExecuteDataset(getConnectionString(), "InsertSimcardResponseDetails", ob);
            }
                    catch (Exception d)
                    {

                    }

                }
            }
     }

        
        #endregion

         
        private void Form1_Load(object sender, EventArgs e)
        { 
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

            this.cboBaudRate.Items.AddRange(new Level[]{
                new Level(){ Name = "one", Value = 75},
                new Level(){ Name = "two", Value = 110},
                new Level(){ Name = "three", Value = 300},
                new Level(){ Name = "four", Value = 2400},
                new Level(){ Name = "five", Value = 4800},
                new Level(){ Name = "six", Value = 9600},
                new Level(){ Name = "seven", Value = 19200},
                new Level(){ Name = "eight", Value = 38400},
                new Level(){ Name = "nine", Value = 57600},
                new Level(){ Name = "ten", Value = 115200},
            });
            cboBaudRate.DisplayMember = "Value";
            cboBaudRate.SelectedText = "9600";

            cboReadTimeOut.Items.Add("5000");
            cboReadTimeOut.SelectedItem = "5000";

            cboWriteTimeOut.Items.Add("5000");
            cboWriteTimeOut.SelectedItem = "5000";

            string[] portnames = SerialPort.GetPortNames();
            cboPortName.Items.Clear();
            foreach (string s in portnames)                 //add this names to comboboxPort items
            {
                cboPortName.Items.Add(s);
            }

            this.serialPort.PortName = cboPortName.ToString();
            

        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.serialPort.Close();
            btn_start.Enabled = true;
            btn_Stop.Enabled = false;
            timer2.Enabled = false;
            btnDisconnect.Enabled = true;
            btnConnect.Enabled = true;
            // You may decide to prompt to user else just kill. 
            e.Cancel = false;
            //Process.GetCurrentProcess().Goose();
            //ProcessTabKey.GetCurrentProcess().Kill();
        }

        #region Port Connection
        private void btnConnect_Click(object sender, EventArgs e)
        {
            int count = 0;
            if(!string.IsNullOrEmpty(cboPortName.Text))
            {
                while (count <= 3)
                {

                    if (!this.serialPort.IsOpen)
                    {
                        this.lblConnectionStatus.Text = "Not Connected";
                        this.serialPort.PortName = cboPortName.Text;
                        this.serialPort.Open();
                    }

                    if (this.serialPort.IsOpen)
                    {
                        this.lblConnectionStatus.Text = "Connected";
                        btnDisconnect.Enabled = true;
                        WriteStatusBar("Connected");
                        btnConnect.Enabled = false;
                        break;
                    }
                    else
                        count = count++;
                    serialPort.ReadTimeout = 5000;
                }
               
            }
            else
                MessageBox.Show("Arrh, Check the port", "Port Not Selected", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            
                
            
        }
        #endregion

        public class Level
        {
            public string Name { get; set; }
            public int Value { get; set; }
        }
    }
}
