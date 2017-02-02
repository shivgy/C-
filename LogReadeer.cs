using MSDataAccess;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommsLogReader
{
    class Program
    {
        private static String deviceimeino, uid, ignition, digitalinputone, digitalinputtwo, latitude, longitude, gpsspeed, gpsdirection, gpsodometer, vehiclebatteryvoltage, vehicleinputvoltage, eventtimestamp, events, locationtimestamp, cellullarareacode, idofmobiletower, digitaloutput;
        static void Main(string[] args)
        {
            String line, data=null;
            System.IO.StreamReader file = new System.IO.StreamReader("D:\\...\\test.txt");
            while ((line = file.ReadLine()) != null)
            {
                try
                {
                    if (line.Contains("polling"))
                    {
                        string[] temp = line.Split('$'); 
                        processPacket("$"+temp[1]);
                    } 
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception in Inserting packet >>> " + line);
                }
            } 
                Console.ReadLine();
           }

                 
        private static void processPacket(string dataPacket)
        {
            try
            {
                object[] obj = new object[20];
                String[] dataArray = dataPacket.Split(',');
                if (dataArray[4].Equals("01"))
                {

                    obj[0] = dataArray[0].Replace("$SLU", "");
                    obj[1] = dataArray[2];
                    obj[2] = dataArray[3];
                    obj[3] = dataArray[4];
                    obj[4] = dataArray[5];
                    obj[5] = getLatitude(dataArray[6].Replace("+", ""));
                    obj[6] = getLongitude(dataArray[7].Replace("+", ""));
                    obj[7] = dataArray[8];
                    obj[8] = dataArray[9];
                    obj[9] = dataArray[10];
                    obj[10] = dataArray[11];
                    obj[11] = dataArray[12];
                    obj[12] = dataArray[15];
                    obj[13] = dataArray[16];

                    obj[14] = dataArray[15];
                    obj[15] = dataArray[13];
                    obj[16] = dataArray[14];

                    obj[17] = dataArray[18];
                    obj[18] = dataPacket;
                    obj[19] = System.Threading.Thread.CurrentThread.ManagedThreadId.ToString();

                    DataAccess.ExecuteDataset("Data Source=111.111.111.111;Initial Catalog=DatabaseName;MultipleActiveResultSets=true;Persist Security Info=True;User ID=userID;Password=Password", "Stored_Procedure", obj);
                    Console.WriteLine("Inserting packet >>> " + dataPacket);
                }
            }
            catch (Exception e)
            {

            }
        }

        private static string getLongitude(string longitude)
        {
            try
            {
                double lonSplit = 0.0, lonSplittemp = 0.0;
                lonSplit = Convert.ToDouble(longitude.Substring(0, 3));
                lonSplittemp = Convert.ToDouble(longitude.Substring(3, 7));
                double finallon = (lonSplit + (lonSplittemp / 60));
                return finallon.ToString();
            }
            catch (Exception e)
            {
                return "0";
            }
        }

        private static string getLatitude(string latitude)
        {
            try
            {
                double latSplit = 0, latSplittemp = 0;
                latSplit = Convert.ToDouble(latitude.Substring(0, 2));
                latSplittemp = Convert.ToDouble(latitude.Substring(2, 7));
                double finallat = (latSplit + (latSplittemp / 60));
                return finallat.ToString();
            }
            catch (Exception e)
            {
                return "0";
            }
        }

       
        private static string AlertDatetimetoLocalTime(string alertdatetime)
        {
            String localTimeFormat, localDateFormat = null;
            try
            {
                localTimeFormat = alertdatetime.Substring(6, 2) + ":" + alertdatetime.Substring(8, 2) + ":" + alertdatetime.Substring(10, 2);
                localDateFormat = "20" + alertdatetime.Substring(4, 2) + "-" + alertdatetime.Substring(2, 2) + "-" + alertdatetime.Substring(0, 2);
                string strDate = localDateFormat + " " + localTimeFormat;
                DateTime utcdate = DateTime.ParseExact(strDate, "yyyy-MM-dd h:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                var istdate = TimeZoneInfo.ConvertTimeFromUtc(utcdate, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                DateTime enteredDate = DateTime.Parse(istdate.ToString());
                string a = enteredDate.ToString("yyyy-MM-dd HH:mm:ss");
                Console.WriteLine(a);
                return a.ToString();
            }
            catch (Exception e)
            {
                return "exception";
            }

        }
    }
     
}
