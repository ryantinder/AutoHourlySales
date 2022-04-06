using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoHourlySales
{
    class DailySalesEntry
    {
        public static bool operator ==(DailySalesEntry o1, DailySalesEntry o2)
        {
            return (o1.CustCount == o2.CustCount && o1.DriveCustCount == o2.DriveCustCount && o1.HourlySales == o2.HourlySales);
        }
        public static bool operator !=(DailySalesEntry o1, DailySalesEntry o2)
        {
            return (o1.CustCount != o2.CustCount || o1.DriveCustCount != o2.DriveCustCount || o1.HourlySales != o2.HourlySales);

        }
        private int DailySalesId_;
        public int DailySalesId
        {
            get { return DailySalesId_; }
            set { DailySalesId_ = value; }
        }

        private int Hour_;

        public int Hour
        {
            get { return Hour_; }
            set { Hour_ = value; }
        }

        private string Manager_ = "";

        public string Manager
        {
            get { return Manager_; }
            set { Manager_ = value; }
        }

        private int CustCount_;

        public int CustCount
        {
            get { return CustCount_; }
            set { CustCount_ = value; }
        }

        private int DriveCustCount_;

        public int DriveCustCount
        {
            get { return DriveCustCount_; }
            set { DriveCustCount_ = value; }
        }

        private double HourlySales_;

        public double HourlySales
        {
            get { return HourlySales_; }
            set { HourlySales_ = value; }
        }

        private double CumulativeTotalSales_;

        public double CumulativeTotalSales
        {
            get { return CumulativeTotalSales_; }
            set { CumulativeTotalSales_ = value; }
        }

        private double PullRegister1_ = 0;

        public double PullRegister1
        {
            get { return PullRegister1_; }
            set { PullRegister1_ = value; }
        }

        private double PullRegister2_ = 0;

        public double PullRegister2
        {
            get { return PullRegister2_; }
            set { PullRegister2_ = value; }
        }


        private double PullDrive_ = 0;

        public double PullDrive
        {
            get { return PullDrive_; }
            set { PullDrive_ = value; }
        }

        private double ActualMainHours_ = 0;

        public double ActualManHours
        {
            get { return ActualMainHours_; }
            set { ActualMainHours_ = value; }
        }

        private double BudgetedManHours_ = 0;

        public double BudgetedManHours
        {
            get { return BudgetedManHours_; }
            set { BudgetedManHours_ = value; }
        }

        private int DTOvers_ = 0;

        public int DTOvers
        {
            get { return DTOvers_; }
            set { DTOvers_ = value; }
        }

        private double GMLabor_ = 0;

        public double GMLabor
        {
            get { return GMLabor_; }
            set { GMLabor_ = value; }
        }

        private double CrewHrs_ = 0;

        public double CrewHrs
        {
            get { return CrewHrs_; }
            set { CrewHrs_ = value; }
        }











    }
}
