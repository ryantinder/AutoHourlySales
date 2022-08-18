using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Odbc;
using Dapper;
using System.Configuration;
using System.IO;




/*  
 *  File: Program.cs
 *  Author: Ryan Tinder
 *  Date Created: 03/01/2021
 *  Last modified: 04/01/2021
 *  Desc: Program is intended to be ran as a service to collect hourly stats from taco mayo stores and report them back to the taco mayo portal.
 */

namespace AutoHourlySales
{
    class Program
    {

        static List<string> logArray = new List<string>();

        static string sybase = DES.decrypt("Sybase");

        static string target = DES.decrypt(ConfigurationManager.AppSettings.Get("pointedTo"));

        static DateTime date;

        static void Main(string[] args)
        {

            Log("Program Starting...");

            Log(ConfigurationManager.AppSettings.Get("Store" ));


            Log("Program triggered at time: " + date.ToString("HH:mm:ss") + ", hour = " + date.Hour);

            int storeId = Convert.ToInt32(ConfigurationManager.AppSettings.Get("StoreId"));
            string store = Convert.ToString(ConfigurationManager.AppSettings.Get("Store"));

            // Date configuration -----------------------------------------------------------------------------
            date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);

            string dateOverride = ConfigurationManager.AppSettings.Get("dateOverride");
            string hourOverride = ConfigurationManager.AppSettings.Get("hourOverride");
            // If the program is configured to override BOTH date and hour
            if (dateOverride != "false") // dateOVerride must be a number
            {
                date = new DateTime(Convert.ToInt32(dateOverride.Substring(0, 4)),
                                    Convert.ToInt32(dateOverride.Substring(5, 2)),
                                    Convert.ToInt32(dateOverride.Substring(8, 2)),
                                    Convert.ToInt32(hourOverride), 5, 0);

            }

            // If the program is only configured to override the hour
            else if (hourOverride != "false")
            {
                date = new DateTime(date.Year, date.Month, date.Day, Convert.ToInt32(hourOverride), 0, 0);
            }
            // End of date configuration ---------------------------------------------------------------------


            // DSId Generation -------------------------------------------------------------------------------
            Log("Is DSid generation needed? (Hour == 1)?..." + ((date.Hour == 1) ? "yes" : "no"));
            if (date.Hour == 1 || ConfigurationManager.AppSettings.Get("Override1am") == "true")
            {
                try
                {
                    DSIdGenerator(storeId, store, date);
                }
                catch (Exception e)
                {
                    Log(e.Message);
                    Log(e.StackTrace);
                }
            }
            // End of DSid Generation -----------------------------------------------------------------------



            Log("Global parameters: date=" + date + "; storeId=" + storeId);

            /*
                * Step 2: Using the current system hour, grab that stats and fit into packet
                * 
                */
            
            DailySalesEntry realtime = fetchStatsFromSybase(date, storeId, store);
            updateEntry(realtime);

            if (ConfigurationManager.AppSettings.Get("disableReconcilliation") == "false")
            {
                for (int i = 1; i <= date.Hour; i++)
                {
                    DailySalesEntry sybase = fetchStatsFromSybase(new DateTime(date.Year, date.Month, date.Day, i, 0, 0), storeId, store);
                    DailySalesEntry live = fetchStatsFromLive(sybase.DailySalesId, i);
                    printDailySalesEntry(sybase);
                    printDailySalesEntry(live);
                    if (sybase != live && sybase.HourlySales != 0)
                    {
                        Log("Error discovered! ID: " + sybase.DailySalesId + ", hour: " + i);
                        updateEntry(sybase);
                    }
                }
            }
            
            Log("Program Ending");
            DumpLog();
            if (ConfigurationManager.AppSettings.Get("consoleReadkey()") == "true")
            {
                Console.ReadKey();
            }
        }
        public static DailySalesEntry fetchStatsFromSybase(DateTime date, int storeId, string store)
        {
            DailySalesEntry dailySalesEntry = new DailySalesEntry();
            dailySalesEntry.DailySalesId = Program.getDailySalesId(date, storeId, store);
            dailySalesEntry.Hour = date.Hour;

            string dayString = date.ToString("yyyy-MM-dd");
            string time1 = Convert.ToString(date.Hour - 1) + ":00:00.000";
            string time2 = Convert.ToString(date.Hour) + ":00:00.000";
            string paramText = "";
            OdbcCommand cmd;
            object buffer;

            if (date.Hour == 0)
            {
                dayString = date.AddDays(-1).ToString("yyyy-MM-dd");
                time1 = "23:00:00.000";
                time2 = "23:59:59.999";
                dailySalesEntry.DailySalesId = Program.getDailySalesId(date.AddDays(-1), storeId, store);
                dailySalesEntry.Hour = 24;
            }

            OdbcConnection connection = null;
            try
            {
                Log("Connecting to Sybase");
                connection = new OdbcConnection(sybase);
                connection.Open();
            }
            catch (Exception e)
            {
                Log(e.Message);
                Log(e.StackTrace);
                DumpLog();
                System.Environment.Exit(-1);
            }

            Log("Connection state = " + (connection.State == ConnectionState.Open));

            Log(time1 + " -> " + time2);


            dailySalesEntry.CustCount = 0;
            dailySalesEntry.DriveCustCount = 0;
            dailySalesEntry.HourlySales = 0;
            dailySalesEntry.CumulativeTotalSales = 0;

            if (dailySalesEntry.Hour > 5)
            {

                try
                {
                    paramText = "select coalesce(count(numcust), 0) from posheader where numcust > 0 and nettotal > 0 and status = 3 and timeend between '" +
                                    dayString + " " + time1 + "' and '" + dayString + " " + time2 + "' group by numcust";
                    cmd = new OdbcCommand(paramText, connection);
                    Log("CustCount, Executing '" + paramText + "'");
                    buffer = cmd.ExecuteScalar();
                    dailySalesEntry.CustCount = (buffer == null || buffer == DBNull.Value) ? 0 : (int)buffer;
                    Log("-->" + dailySalesEntry.CustCount);
                    Log("Customer Count = " + dailySalesEntry.CustCount);



                    paramText = "select isnull(count(saletypeindex), 0) from posheader where saletypeindex = 1008 and numcust > 0 and nettotal > 0 and status = 3 and timeend between '" +
                                    dayString + " " + time1 + "' and '" + dayString + " " + time2 + "' group by saletypeindex";
                    cmd = new OdbcCommand(paramText, connection);
                    Log("DriveCustCount, Executing '" + paramText + "'");
                    buffer = cmd.ExecuteScalar();
                    dailySalesEntry.DriveCustCount = (buffer == null || buffer == DBNull.Value) ? 0 : (int)buffer;
                    Log("-->" + dailySalesEntry.DriveCustCount);
                    Log("DriveCustCount = " + dailySalesEntry.DriveCustCount);



                    paramText = "select isnull(sum(nettotal), 0) from posheader where status = 3 and timeend between '" +
                                    dayString + " " + time1 + "' and '" + dayString + " " + time2 + "'";
                    cmd = new OdbcCommand(paramText, connection);
                    Log("HourlySales, Executing '" + paramText + "'");
                    buffer = cmd.ExecuteScalar();
                    dailySalesEntry.HourlySales = (buffer == null || buffer == DBNull.Value) ? 0 : (double)buffer;
                    Log("-->" + dailySalesEntry.HourlySales);
                    Log("HourlySales = " + dailySalesEntry.HourlySales);



                    paramText = "select isnull(sum(nettotal), 0) from posheader where status = 3 and timeend between '" +
                                    dayString + " 05:00:00.000' and '" + dayString + " " + time2 + "'";
                    cmd = new OdbcCommand(paramText, connection);
                    Log("HourlySales, Executing '" + paramText + "'");
                    buffer = cmd.ExecuteScalar();
                    dailySalesEntry.CumulativeTotalSales = (buffer == null || buffer == DBNull.Value) ? 0 : (double)buffer;
                    Log("-->" + dailySalesEntry.CumulativeTotalSales);
                    Log("CumulativeTotalSales = " + dailySalesEntry.CumulativeTotalSales);


                }
                catch (Exception e)
                {
                    Log("Failed at Sybase data retrieval 1");
                    Log(e.Message);
                    Log(e.StackTrace);
                }
                finally
                {
                    connection.Close();
                    printDailySalesEntry(dailySalesEntry);
                    Log("DailySalesId | Hour | CustCount | DriveCustCount | HourlySales | Cuml.Sales");
                    Log(dailySalesEntry.DailySalesId + "         " + dailySalesEntry.Hour + "      " + dailySalesEntry.CustCount +
                                            "           " + dailySalesEntry.DriveCustCount + "                " + dailySalesEntry.HourlySales + "              " + dailySalesEntry.CumulativeTotalSales);
                }
            }

            //Early morning checking
            else
            {
                Log("Checking early morning sales");

                int custCount;
                double hourlySales;

                
                paramText = "select count(numcust) from posheader where numcust > 0 and nettotal > 0 and status = 3 and timeend between '" + dayString + " " + time1 + "'and'" + dayString + " " + time2 + "' group by numcust";
                cmd = new OdbcCommand(paramText, connection);
                Log("CustCount, " + paramText);
                buffer = cmd.ExecuteScalar();
                custCount = (buffer == null || buffer == DBNull.Value) ? 0 : (int)buffer;
                Log("--> " + custCount);



                paramText = "select sum(nettotal) from posheader where status = 3 and timeend between '" + dayString + " " + time1 + "'and'" + dayString + " " + time2 + "'";
                cmd = new OdbcCommand(paramText, connection);
                Log("CustCount, " + paramText);
                buffer = cmd.ExecuteScalar();
                hourlySales = (buffer == null || buffer == DBNull.Value) ? 0 : (double)buffer;
                Log("--> " + hourlySales);
                Log("Hourly Sales = " + hourlySales + " CustCount = " + custCount);

                if (custCount > 0 || hourlySales > 0)
                {
                    try
                    {
                        Log("Update necessary");
                        DateTime dayBefore = date.AddDays(-1);

                        dailySalesEntry = new DailySalesEntry();
                        dailySalesEntry.CustCount = 0;
                        dailySalesEntry.DriveCustCount = 0;
                        dailySalesEntry.HourlySales = 0;
                        dailySalesEntry.CumulativeTotalSales = 0;
                        dailySalesEntry.DailySalesId = Program.getDailySalesId(dayBefore, storeId, store);
                        dailySalesEntry.Hour = date.Hour;
                        Log("DailySalesId for yesterday (" + dayBefore.ToString("yyyy-MM-dd") + ") : " + dailySalesEntry.DailySalesId);


                        Log(time1 + " -> " + time2);

                        paramText = "select count(numcust) from posheader where numcust > 0 and nettotal > 0 and status = 3 and timeend between '" +
                                            dayString + " " + time1 + "' and '" + dayString + " " + time2 + "' group by numcust";

                        cmd = new OdbcCommand(paramText, connection);
                        Log("CustCount, Executing '" + paramText + "'");
                        buffer = cmd.ExecuteScalar();
                        dailySalesEntry.CustCount = (buffer == null || buffer == DBNull.Value) ? 0 : (int)buffer;
                        Log("CustCount = " + dailySalesEntry.CustCount);
                        Log("-->" + dailySalesEntry.CustCount);


                        paramText = "select count(saletypeindex) from posheader where saletypeindex = 1008 and numcust > 0 and nettotal > 0 and status = 3 and timeend between '" +
                                            dayString + " " + time1 + "'and'" + dayString + " " + time2 + "' group by saletypeindex";

                        cmd = new OdbcCommand(paramText, connection);
                        Log("DriveCustCount, Executing '" + paramText + "'");
                        buffer = cmd.ExecuteScalar();
                        dailySalesEntry.DriveCustCount = (buffer == null || buffer == DBNull.Value) ? 0 : (int)buffer;
                        Log("DriveCustCount = " + dailySalesEntry.DriveCustCount);
                        Log("-->" + dailySalesEntry.DriveCustCount);



                        paramText = "select sum(nettotal) from posheader where status = 3 and timeend between '" +
                                            dayString + " " + time1 + "'and'" + dayString + " " + time2 + "'";

                        cmd = new OdbcCommand(paramText, connection);
                        Log("HourlySales, Executing '" + paramText + "'");
                        buffer = cmd.ExecuteScalar();
                        dailySalesEntry.HourlySales = (buffer == null || buffer == DBNull.Value) ? 0 : (double)buffer;
                        Log("HourlySales = " + dailySalesEntry.HourlySales);
                        Log("-->" + dailySalesEntry.HourlySales);



                        paramText = "select sum(nettotal) from posheader where status = 3 and timeend between '" +
                                            dayBefore.ToString("yyyy-MM-dd") + " 05:00:00.000' and'" + dayString + " " + time2 + "'";

                        cmd = new OdbcCommand(paramText, connection);
                        Log("CumulativeSales, Executing '" + paramText + "'");
                        buffer = cmd.ExecuteScalar();
                        dailySalesEntry.CumulativeTotalSales = (buffer == null || buffer == DBNull.Value) ? 0 : (double)buffer;
                        Log("CumulutativeTotalSales = " + dailySalesEntry.CumulativeTotalSales);
                        Log("-->" + dailySalesEntry.CumulativeTotalSales);



                        
                        dailySalesEntry.PullRegister1 = 0;
                        dailySalesEntry.PullRegister2 = 0;
                        dailySalesEntry.PullDrive = 0;
                        dailySalesEntry.ActualManHours = 0;
                        dailySalesEntry.BudgetedManHours = 0;
                        dailySalesEntry.DTOvers = 0;
                        dailySalesEntry.GMLabor = 0;
                        dailySalesEntry.CrewHrs = 0;
                        dailySalesEntry.Manager = "";
                    }
                    catch (Exception e)
                    {
                        Log(e.Message);
                        Log(e.StackTrace);
                    }
                    finally
                    {
                        printDailySalesEntry(dailySalesEntry);
                        Log("DailySalesId | Hour | CustCount | DriveCustCount | HourlySales | Cuml.Sales");
                        Log(dailySalesEntry.DailySalesId + "         " + dailySalesEntry.Hour + "      " + dailySalesEntry.CustCount +
                                                "           " + dailySalesEntry.DriveCustCount + "                " + dailySalesEntry.HourlySales + "              " + dailySalesEntry.CumulativeTotalSales);
                    }
                }
                else
                {
                    Log("No update needed");
                }
                connection.Close();
            }

            return dailySalesEntry;
        }
        public static DailySalesEntry fetchStatsFromLive(int dailySalesID, int hour)
        {
            DailySalesEntry dailySalesEntry = new DailySalesEntry();
            object buffer;
            try
            {
                IDbConnection connection = new System.Data.SqlClient.SqlConnection(target);
                connection.Open();


                Log("Executing cust count from live");
                buffer = connection.ExecuteScalar("Select CustCount from dailysalesentry where dailysalesid = " + dailySalesID + " and hour = " + hour);
                dailySalesEntry.CustCount = (buffer == null || buffer == DBNull.Value) ? 0 : (int)buffer;
                Log("CustCount = " + dailySalesEntry.CustCount);
                Log("-->" + dailySalesEntry.CustCount);

                Log("Executing drive cust count from live");
                buffer = connection.ExecuteScalar("Select drivecustcount from dailysalesentry where dailysalesid = " + dailySalesID + " and hour = " + hour);
                dailySalesEntry.DriveCustCount = (buffer == null || buffer == DBNull.Value) ? 0 : (int)buffer;
                Log("DriveCustCount = " + dailySalesEntry.DriveCustCount);
                Log("-->" + dailySalesEntry.DriveCustCount);

                Log("Executing hourly sales from live");
                buffer = connection.ExecuteScalar("Select hourlysales from dailysalesentry where dailysalesid = " + dailySalesID + " and hour = " + hour);
                dailySalesEntry.HourlySales = (buffer == null || buffer == DBNull.Value) ? 0 : Convert.ToDouble(buffer);
                Log("hourlysales = " + dailySalesEntry.HourlySales);
                Log("-->" + dailySalesEntry.HourlySales); 
                
                Log("Executing cumulative sales from live");
                buffer = connection.ExecuteScalar("Select cumulativetotalsales from dailysalesentry where dailysalesid = " + dailySalesID + " and hour = " + hour);
                dailySalesEntry.CumulativeTotalSales = (buffer == null || buffer == DBNull.Value) ? 0 : Convert.ToDouble(buffer);
                Log("CustCount = " + dailySalesEntry.CumulativeTotalSales);
                Log("-->" + dailySalesEntry.CumulativeTotalSales);

                dailySalesEntry.Hour = hour;
                dailySalesEntry.DailySalesId = dailySalesID;


            }
            catch (Exception e) { 
                Log(e.Message);
                Log(e.StackTrace);
            }


            return dailySalesEntry;
        }
        public static void updateEntry(DailySalesEntry dailySalesEntry)
        {
            
            try
            {
                Log("Connecting to target portal");
                IDbConnection connection = new System.Data.SqlClient.SqlConnection(target);
                connection.Open();

                Log("Portal Connection successful = " + (connection.State == ConnectionState.Open));

                Log("Executing 'dbo.updateDailySalesEntry " + dailySalesEntry.DailySalesId + " " + dailySalesEntry.Hour + " " + dailySalesEntry.Manager + " " + dailySalesEntry.CustCount + " " + dailySalesEntry.DriveCustCount + " " + dailySalesEntry.HourlySales + " " + dailySalesEntry.CumulativeTotalSales + "'");
                connection.Execute("dbo.updateDailySalesEntry @DailySalesId, @Hour, @CustCount, @DriveCustCount, @HourlySales, @CumulativeTotalSales", dailySalesEntry);
                Log("Entry added");
                connection.Close();

            }
            catch (Exception e)
            {
                Log(e.Message);
                Log(e.StackTrace);
            }
        }
        public static void printDailySalesEntry(DailySalesEntry dailySalesEntry)
        {
            Log("DailySalesId | Hour | CustCount | DriveCustCount | HourlySales | Cuml.Sales");
            Log(Convert.ToString(dailySalesEntry.DailySalesId).PadRight(15) + Convert.ToString(dailySalesEntry.Hour).PadRight(7) 
               + Convert.ToString(dailySalesEntry.CustCount).PadRight(12) + Convert.ToString(dailySalesEntry.DriveCustCount).PadRight(17) 
               + Convert.ToString(dailySalesEntry.HourlySales).PadRight(14) + Convert.ToString(dailySalesEntry.CumulativeTotalSales) + "\n");
        }
        public static void Generate24Entries(int storeId, string store)
        {
            Log("24 entry generation triggered");

            using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(target))
            {
                Log("Connecting to target to check for DailySalesId");
                connection.Open();
                Log("---Connection success = " + (connection.State == ConnectionState.Open));

                int DSid = getDailySalesId(new DateTime(date.Year, date.Month, date.Day), storeId, store);

                int projectionID = 0;
                int fiscalYear = (date.Month == 12) ? date.Year + 1 : date.Year;
                TimeSpan tDiff = new DateTime(fiscalYear - 1, 12, 1) - date;
                int week = Math.Abs(tDiff.Days / 7) + 1;
                string cmdText = "select ID from projections where FiscalYear = " + fiscalYear + " and week = " + week + " and storeid = " + storeId;
                object buffer = connection.ExecuteScalar(cmdText);
                Log("Projection ID: " + cmdText);
                if (buffer != DBNull.Value && buffer != null && Convert.ToInt32(buffer) != 0)
                {
                    projectionID = Convert.ToInt32(buffer);
                }

                int dayOfWeek = 0;
                DayOfWeek dow = date.DayOfWeek;

                switch (dow.ToString())
                {
                    case "Tuesday":
                        dayOfWeek = 7;
                        break;
                    case "Wednesday":
                        dayOfWeek = 1;
                        break;
                    case "Thursday":
                        dayOfWeek = 2;
                        break;
                    case "Friday":
                        dayOfWeek = 3;
                        break;
                    case "Saturday":
                        dayOfWeek = 4;
                        break;
                    case "Sunday":
                        dayOfWeek = 5;
                        break;
                    case "Monday":
                        dayOfWeek = 6;
                        break;
                    default:
                        dayOfWeek = 0;
                        break;
                }
                Log("---Generating 24 entries for DailySalesId = " + DSid);
                for (int i = 1; i <= 24; i++)
                {
                    DailySalesEntry dse = new DailySalesEntry();
                    dse.BudgetedManHours = 0;
                    if (i >= 9)
                    {

                        Log("Generating Budgeted Man Hours for hour " + i);
                        string paramText = "select Day" + dayOfWeek + " from projectionsideal where projectionsID = " + projectionID + " and hour = " + (i - 8);
                        buffer = connection.ExecuteScalar(paramText);
                        Log("Budgeted hours command: " + paramText);
                        if (buffer != DBNull.Value && buffer != null && Convert.ToDouble(buffer) != 0)
                        {
                            Log("BudgetedManHours = " + Convert.ToDouble(buffer));
                            dse.BudgetedManHours = Convert.ToDouble(buffer);
                        }
                    }
                    dse.DailySalesId = DSid;
                    dse.Hour = i;
                    dse.Manager = "";
                    dse.CustCount = 0;
                    dse.DriveCustCount = 0;
                    dse.HourlySales = 0;
                    dse.CumulativeTotalSales = 0;
                    dse.PullRegister1 = 0;
                    dse.PullRegister2 = 0;
                    dse.PullDrive = 0;
                    dse.ActualManHours = 0;
                    dse.GMLabor = 0.0;
                    dse.CrewHrs = 0.0;

                    Log("Executing 'dbo.addDailySalesEntry " + dse.DailySalesId + " " + dse.Hour + " " + dse.Manager + " " + dse.CustCount + " " + dse.DriveCustCount + " " + dse.HourlySales + " " + dse.CumulativeTotalSales +
                            " " + dse.PullRegister1 + " " + dse.PullRegister2 + " " + dse.PullDrive + " " + dse.ActualManHours + " " + dse.BudgetedManHours + " " + dse.GMLabor + " " + dse.CrewHrs);
                    connection.Execute("dbo.addDailySalesEntry @DailySalesId, @Hour, @Manager, @CustCount, @DriveCustCount, @HourlySales, @CumulativeTotalSales, @PullRegister1, @PullRegister2, @PullDrive, @ActualManHours, @BudgetedManHours, @DTOvers, @GMLabor, @CrewHrs", dse);

                }
                connection.Close();
            }
            Log("Generating 24 Entries complete.");
        }
        public static void DSIdGenerator(int storeId, string store, DateTime date)
        {
            Log("Generating DSId starting...");
            Log("Hour = " + DateTime.Now.Hour + ", DailysSalesId checking triggered");

            Log("Date = " + date.ToString("yyyy-MM-dd"));

            try
            {
                using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(target))
                {
                    Log("Connecting to target to check for DailySalesId");

                    connection.Open();
                    Log("---Connection success = " + (connection.State == ConnectionState.Open));

                    /*
                    * Step 1: Check if entry already exists for date
                    */
                    Log("---Checking if ID exists for " + date);

                    Log("Executing 'dbo.getDailySalesId " + date.ToString("yyyy-MM-dd") + " " + storeId + "'");
                    object buffer = connection.Query<int>("dbo.getDailySalesId @Date, @StoreId", new { Date = date, StoreId = storeId }).FirstOrDefault();
                    if (buffer != null && buffer != DBNull.Value && (int)buffer != 0)
                    {
                        Log("---ID already exists for date, ID = " + (int)buffer);
                        Log("--> " + (int)buffer);
                        Log("Generating DSid complete");
                    }


                    /*
                     * Step 2: Generate new packet
                     */
                    else
                    {
                        Log("--> null");
                        Log("---Previous ID not found, creating new ID");
                        var _date = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
                        DailySales dailySales = new DailySales(_date);
                        dailySales.ManagerId = 0;
                        dailySales.Manager = "";

                        Log("Creating new DailySalesId");
                        Log("DailySalesDate    | StoreId | Store | BaseForDay | ProjectionForDay | SubmitDate");
                        Log(dailySales.DailySalesDate + " " + dailySales.StoreId + " " + dailySales.Store +
                                            " " + dailySales.BaseForDay + " " + dailySales.ProjectionForDay + " " + dailySales.SubmitDate);

                        Log("Executing 'dbo.addDailySales " + dailySales.DailySalesDate + " " + dailySales.StoreId + " " + dailySales.Store + " " + dailySales.ManagerId + " " + dailySales.Manager + " "
                               + dailySales.BaseForDay + " " + dailySales.ProjectionForDay + " " + dailySales.SubmitDate);
                        connection.Execute("dbo.addDailySales @DailySalesDate, @StoreId, @Store, @ManagerId, @Manager, @BaseForDay, @ProjectionForDay, @SubmitDate", dailySales);

                        Log("Generating DSid complete");
                        Log("DailySalesId Generated");

                        //Check to make sure the day is clear before calling 24 hour generator
                        buffer = connection.Query<int>("dbo.getDailySalesId @Date, @StoreId", new { Date = date, StoreId = storeId }).FirstOrDefault();
                        object buf2 = connection.ExecuteScalar("select count(*) from DailySalesEntry where DailySalesId = " + (int)buffer);
                        if (buf2 != null && buf2 != DBNull.Value && (int)buf2 == 0)
                        {                            
                            Generate24Entries(storeId, store);
                        }


                    }
                    connection.Close();
                }
            } catch (Exception e)
            {
                Log("****Failed during Daily Sales Id generation****");
                Log(e.Message);
                Log(e.StackTrace);
            }
            
        }
        public static int getDailySalesId(DateTime date, int StoreId, string Store)
        {

            /*
             * Step 1: Find the DailySalesId assigned to todays date
             */
            Log("Fetching DailySalesId");
            Log("Getting DailySaleId for date = " + date + " { ");
            int DailySalesId = 0;
            Log("Establishing connection to: " + target);
            using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(target))
            {
                Log("Connecting to target to check for DailySalesId");

                connection.Open();
                Log("---Connection state = " + (connection.State == ConnectionState.Open));
                Log("---Connection success = " + (connection.State == ConnectionState.Open));


                DateTime queryDate = new DateTime(date.Year, date.Month, date.Day);
                                
                DailySalesId = connection.Query<int>("dbo.getDailySalesId @DailySalesDate, @StoreId", new { DailySalesDate = queryDate, StoreId = StoreId, Store = Store}).FirstOrDefault();
                Log("---Fetched ID = " + DailySalesId);

                connection.Close();
            }

            Log("Getting DailySaleId for date = " + date + " complete");

            return DailySalesId;
        }
     
        public static void Log(string s)
        {
            Console.WriteLine(s);
            logArray.Add(DateTime.Now.ToString("HH:mm:ss") + " : " + s);
        }
        public static void DumpLog()
        {
            string dayName = DateTime.Now.ToString("yyyy-MM-dd") + ".txt";
            string rootFolder = @"c:\AutoHourlyLogs";
            string pathString = System.IO.Path.Combine(rootFolder, dayName);
            if (!Directory.Exists(rootFolder))
            {
                System.IO.Directory.CreateDirectory(rootFolder);
            }
            if (!File.Exists(pathString))
            {
                using (StreamWriter f = new StreamWriter(pathString))
                {
                    f.WriteLine("****START OF LOG FILE****");
                    f.WriteLine("*File created: " + DateTime.Now.ToString());
                    f.WriteLine("*Day: " + DateTime.Now.ToString("dddd, MMMM dd, yyyy"));
                    f.WriteLine("*This is a log file that collects data from each run of AutoHourlySales.application");
                    f.WriteLine("*\n*\n*\n");
                }
            }
            if (Directory.Exists(rootFolder) && File.Exists(pathString))
            {
                using (StreamWriter f = new StreamWriter(pathString, true))
                {
                    f.WriteLine("LOG CREATED: " + DateTime.Now.ToString());

                    foreach (string s in logArray) {
                        f.WriteLine(s);
                    }
                    f.WriteLine("-------------------------------------------------------------------------------------------------\n");
                }
            }
        }
    }
}
