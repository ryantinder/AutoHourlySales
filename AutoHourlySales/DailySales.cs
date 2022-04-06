using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Data;
using Dapper;

namespace AutoHourlySales
{
    class DailySales
    {
        public DailySales(DateTime date)
        {
            DailySalesDate_ = date;

            try
            {
                using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(DES.decrypt("liveTacomayo")))
                {
                    Console.WriteLine("DailySale Constructor called");
                    connection.Open();
                    Console.Out.WriteLine("---Connection state = " + (connection.State == ConnectionState.Open));
                    DateTime queryDate = new DateTime(DailySalesDate_.Year, DailySalesDate_.Month, DailySalesDate_.Day);
                    object buffer = connection.Query<int>("dbo.getBaseForDay @StoreId, @DailySalesDate", new { StoreId = Convert.ToInt32(ConfigurationManager.AppSettings.Get("StoreId")), DailySalesDate = queryDate }).FirstOrDefault();


                    BaseForDay_ = (buffer != null && buffer != DBNull.Value) ? (int)buffer : 0;
                    Console.WriteLine("DailySale Constructor Complete");
                }
            }
            catch (Exception e)
            {
                BaseForDay_ = 0;
            }
        }
        private DateTime DailySalesDate_;

        public DateTime DailySalesDate
        {
            get { return DailySalesDate_; }
            set { DailySalesDate_ = value; }
        }


        public int StoreId
        {
            get { return Convert.ToInt32(ConfigurationManager.AppSettings["StoreId"]); }
            
        }

        
        public string Store
        {
            get { return ConfigurationManager.AppSettings["Store"]; }    
        }

        private int  ManagerId_;

        public int  ManagerId
        {
            get { return ManagerId_; }
            set { ManagerId_ = value; }
        }

        private string Manager_;

        public string Manager
        {
            get { return Manager_; }
            set { Manager_ = value; }
        }

        private int BaseForDay_;

        public int BaseForDay
        {
            get { return BaseForDay_; }
            set { BaseForDay_ = value; }
        }

        private double ProjectionForDay_;

        public double ProjectionForDay
        {
            get { return ProjectionForDay_; }
            set { ProjectionForDay_ = value; }
        }

        private DateTime SubmitDate_;

        public DateTime SubmitDate
        {
            get { return DateTime.Now; }

        }














    }
}
