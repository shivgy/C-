using MSDataAccess;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SUN100LogReader
{
    class Program
    {
        private static String YEAR_HEAD = "20";
        public static object[] obj = new object[29];
        
        #region Main Function
        static void Main(string[] args)
        {
            String line, data = null;
            System.IO.StreamReader file = new System.IO.StreamReader("D:\\...\testsat.txt");

            while ((line = file.ReadLine()) != null)
            {
                try
                {
                    if (line.Contains("Recieved packet:"))
                    {
                        string[] temp = line.Split('$');
                        processPacket("$" + temp[1]);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception in Inserting packet >>> " + line);
                }
            }
            Console.ReadLine();
        }
        #endregion

        #region Packet processing
        private static void processPacket(string dataPacket)
        {
            try
            {
			    if(dataPacket.StartsWith("$ABCPOLL"))
			    {
				    try {
					    String[] dataArray = dataPacket.Split('|');
					    obj[26] = dataArray[dataArray.Length-2];    
					    if(Convert.ToDouble(dataArray[dataArray.Length-3])>=1.25) 
                        {
                            obj[0] = dataArray[1];//UID
                            obj[1] = dataArray[2];//Latitude
                            obj[2] = dataArray[3];//Longitude
                            obj[3] = dataArray[4]; //gpstime
                            obj[4] = dataArray[5];//gpsdate 
                            obj[5] = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
                            obj[6] = dataArray[6];//vehiclespeed
                            obj[7] = dataArray[7];//distancebetweentworecords
                            obj[8] = dataArray[8];//gpsdirection
                            obj[9] = dataArray[9];//satellites
                            obj[10] = dataArray[10];//ignition

                            obj[11] = dataArray[11];//overspeedstatus
                            obj[12] = dataArray[12];//GSMsignalquality
                            obj[13] = dataArray[13];//digitalinputone
                            obj[14] = dataArray[14];//digitalinputtwo
                            obj[15] = dataArray[15];//analog1

                            obj[16] = "0.000";//analog2
                            obj[17] = dataArray[16];//internalbatteryvoltage
                            obj[18] = dataArray[17];//Vehiclebatteryvoltage
                            obj[19] = dataArray[18];//GPSodo
                            obj[20] = dataArray[19];//powersource

                            obj[21] = dataArray[20];//pulseodo
                            obj[22] = dataArray[21];//gpsstatus
                            if (Convert.ToInt32(obj[22]) == 0)
                                makeGPSParametersZero();
                            obj[23] = dataArray[22];//packettype
                            obj[24] = dataArray[23];//extradata
                            obj[25] = dataArray[dataArray.Length - 3];//firmwareversion
                            obj[27] = dataPacket;
                            obj[28] = "8400";//Dummy Packet
					}
					
					else
					{
						
					}
				} catch (Exception e) {
                      
				}
			}

            }
            catch (Exception e)
            {

            }
            //Provide server credentials for Database connection
            DataAccess.ExecuteDataset("Data Source=111.111.111.111;Initial Catalog=abcDatabase;MultipleActiveResultSets=true;Persist Security Info=True;User ID=xyz@xyz.xyz ;Password=Xy@12", "Name_of_Stored_Procedure", obj);
            Console.WriteLine("Inserting packet >>> " + dataPacket);
            }
       #endregion
       
       #region Set GPS Parameter as Zero
        private static void makeGPSParametersZero()
        {
            obj[1] = "0.0";//latitude
            obj[2] = "0.0";//longitude
            obj[8] = "0.0";//gpsdirection
            obj[6] = "0.0";//vehiclespeed
            obj[4] = "0";//gpsdate
            obj[3] = "0";//gpstime
            obj[5] = "0";//gpsdatetime
        }
        #endregion
    }
}
