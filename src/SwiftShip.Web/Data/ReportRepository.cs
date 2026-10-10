using SwiftShip.Web.Reporting;

namespace SwiftShip.Web.Data
{
    /// <summary>
    /// Every report is one stored procedure plus one method here. The method keeps the
    /// result sets the procedure answered with and, when a chart makes sense, fills the
    /// labels and the series that Chart.js draws.
    /// </summary>
    public class ReportRepository
    {
        // ------------------------------------------------------------ statistical

        public ReportData ShipmentsPerStatus()
        {
            var data = Build("dbo.usp_Report_ShipmentsPerStatus", null,
                             "Shipments per status",
                             "How many shipments are sitting in each status right now.",
                             "doughnut", "shipments-per-status");
            AddSeries(data, 0, "StatusName", "ShipmentsCount", "Shipments");
            return data;
        }

        public ReportData ShipmentsPerMonth(DateTime? from, DateTime? to)
        {
            var data = Build("dbo.usp_Report_ShipmentsPerMonth", p =>
            {
                p.Add("@FromDate", from);
                p.Add("@ToDate", to);
            }, "Shipments per month",
               "Booked shipments and delivered shipments, month by month.",
               "bar", "shipments-per-month");
            AddSeries(data, 0, "MonthKey", "ShipmentsCount", "Booked");
            AddSeries(data, 0, null, "DeliveredCount", "Delivered");
            return data;
        }

        public ReportData RevenuePerMonth(DateTime? from, DateTime? to)
        {
            var data = Build("dbo.usp_Report_RevenuePerMonth", p =>
            {
                p.Add("@FromDate", from);
                p.Add("@ToDate", to);
            }, "Revenue per month",
               "Money actually paid, by the month of the payment.",
               "line", "revenue-per-month");
            AddSeries(data, 0, "MonthKey", "Revenue", "Revenue (EGP)");
            return data;
        }

        public ReportData ShipmentsByServiceLevel()
        {
            var data = Build("dbo.usp_Report_ShipmentsByServiceLevel", null,
                             "Shipments by service level",
                             "Which speed customers choose, and what it earns.",
                             "bar", "shipments-by-service-level");
            AddSeries(data, 0, "LevelName", "ShipmentsCount", "Shipments");
            AddSeries(data, 0, null, "Revenue", "Revenue (EGP)");
            return data;
        }

        public ReportData TopDestinationCities(int topN)
        {
            var data = Build("dbo.usp_Report_TopDestinationCities", p => p.Add("@TopN", topN),
                             "Top " + topN + " destination cities",
                             "The cities that receive the most parcels.",
                             "bar", "top-destination-cities");
            AddSeries(data, 0, "CityName", "ShipmentsCount", "Shipments");
            return data;
        }

        public ReportData BusiestRoutes(int topN)
        {
            var data = Build("dbo.usp_Report_BusiestRoutes", p => p.Add("@TopN", topN),
                             "Busiest routes (zone to zone)",
                             "The zone pairs that carry the most parcels.",
                             "bar", "busiest-routes");
            AddSeries(data, 0, "RouteName", "ShipmentsCount", "Shipments");
            return data;
        }

        // ------------------------------------------------------------ detailed

        public ReportData DelayedShipments()
        {
            var data = Build("dbo.usp_Report_DelayedShipments", null,
                             "Delayed shipments",
                             "Shipments that passed their promised date and are not finished.",
                             "bar", "delayed-shipments");
            AddSeries(data, 0, "TrackingNumber", "DaysLate", "Days late");
            return data;
        }

        public ReportData CourierPerformance(int courierId)
        {
            var data = Build("dbo.usp_CourierPerformance", p => p.Add("@CourierID", courierId),
                             "Courier performance",
                             "Deliveries, failed attempts, ratings and cash collected, then the daily numbers.",
                             "line", "courier-performance");
            if (data.Tables.Count > 1)
            {
                data.Tables[1].Title = "Deliveries per day";
                AddSeries(data, 1, "DayDate", "Deliveries", "Deliveries");
            }
            return data;
        }

        public ReportData BranchActivity(int branchId, DateTime? from, DateTime? to)
        {
            var data = Build("dbo.usp_Report_BranchActivity", p =>
            {
                p.Add("@BranchID", branchId);
                p.Add("@FromDate", from);
                p.Add("@ToDate", to);
            }, "Branch activity",
               "What this branch handled, when it arrived, and how its couriers are doing.",
               "line", "branch-activity");
            if (data.Tables.Count > 0) data.Tables[0].Title = "Summary";
            if (data.Tables.Count > 1) data.Tables[1].Title = "Transfers per month";
            if (data.Tables.Count > 2) data.Tables[2].Title = "Couriers of this branch";
            AddSeries(data, 1, "MonthKey", "TransfersIn", "Transfers in");
            AddSeries(data, 1, null, "Received", "Received");
            return data;
        }

        public ReportData CustomerHistory(int customerId)
        {
            var data = Build("dbo.usp_CustomerHistory", p => p.Add("@CustomerID", customerId),
                             "Customer history",
                             "Who the customer is, what he booked, and what he spent per month.",
                             "bar", "customer-history");
            if (data.Tables.Count > 0) data.Tables[0].Title = "Customer";
            if (data.Tables.Count > 1) data.Tables[1].Title = "Shipments";
            if (data.Tables.Count > 2) data.Tables[2].Title = "Spending per month";
            if (data.Tables.Count > 3) data.Tables[3].Title = "Phone numbers";
            AddSeries(data, 2, "MonthKey", "Billed", "Billed (EGP)");
            return data;
        }

        // ------------------------------------------------------------ managerial

        public ManagerialKpis Kpis()
        {
            var kpi = new ManagerialKpis();
            Db.ReadMultiple("dbo.usp_Report_ManagerialKPIs", null, r =>
            {
                kpi.TotalShipments = r.Int("TotalShipments");
                kpi.DeliveredShipments = r.Int("DeliveredShipments");
                kpi.DeliveredPercent = r.Dec("DeliveredPercent");
                kpi.Revenue = r.Dec("Revenue");
                kpi.AverageDeliveryHours = r.Dec("AverageDeliveryHours");
                kpi.OpenComplaints = r.Int("OpenComplaints");
                kpi.ActiveCouriers = r.Int("ActiveCouriers");
                kpi.Customers = r.Int("Customers");
                kpi.DelayedShipments = r.Int("DelayedShipments");
                kpi.AverageRating = r.Dec("AverageRating");
                kpi.OnTimePercent = r.Dec("OnTimePercent");
            });
            return kpi;
        }

        public ReportData RevenueByBranch(DateTime? from, DateTime? to)
        {
            var data = Build("dbo.usp_Report_RevenueByBranch", p =>
            {
                p.Add("@FromDate", from);
                p.Add("@ToDate", to);
            }, "Revenue by branch",
               "What each branch billed and what was actually collected.",
               "bar", "revenue-by-branch");
            AddSeries(data, 0, "BranchName", "Collected", "Collected (EGP)");
            AddSeries(data, 0, null, "Billed", "Billed (EGP)");
            return data;
        }

        public ReportData CourierLeaderboard()
        {
            var data = Build("dbo.usp_Report_CourierLeaderboard", null,
                             "Best and worst couriers",
                             "Success rate, deliveries and average rating, best first.",
                             "bar", "courier-leaderboard");
            AddSeries(data, 0, "CourierName", "SuccessRatePercent", "Success rate %");
            return data;
        }

        public ReportData ComplaintStats()
        {
            var data = Build("dbo.usp_Report_ComplaintStats", null,
                             "Complaint rate and resolution time",
                             "How many complaints we get, on what subject, and how long they take.",
                             "bar", "complaint-stats");
            if (data.Tables.Count > 0) data.Tables[0].Title = "Totals";
            if (data.Tables.Count > 1) data.Tables[1].Title = "By subject";
            AddSeries(data, 1, "Subject", "ComplaintsCount", "Complaints");
            return data;
        }

        public ReportData OnTimeDelivery(DateTime? from, DateTime? to)
        {
            var data = Build("dbo.usp_Report_OnTimeDelivery", p =>
            {
                p.Add("@FromDate", from);
                p.Add("@ToDate", to);
            }, "On-time delivery",
               "How often we keep the promised date, month by month.",
               "line", "on-time-delivery");
            AddSeries(data, 0, "MonthKey", "OnTimePercent", "On time %");
            return data;
        }

        /// <summary>The CSV export of the customer's own shipments.</summary>
        public ReportData MyShipmentsCsv(int customerId)
        {
            return Build("dbo.usp_Export_MyShipmentsCsv", p => p.Add("@CustomerID", customerId),
                         "My shipments", "Everything you have booked.", null, "my-shipments");
        }

        // ------------------------------------------------------------ helpers

        private static ReportData Build(string proc, Action<SqlParameterCollection> addParams,
                                        string title, string description, string chartType,
                                        string csvName)
        {
            return new ReportData
            {
                Title = title,
                Description = description,
                ChartType = chartType,
                CsvFileName = csvName,
                Tables = Db.ReadAll(proc, addParams)
            };
        }

        /// <summary>
        /// Fills the chart from two columns of one result set: one column becomes the labels
        /// (only the first time, so several series can share them) and one becomes the values.
        /// </summary>
        private static void AddSeries(ReportData data, int tableIndex, string labelColumn,
                                      string valueColumn, string seriesName)
        {
            if (tableIndex >= data.Tables.Count)
                return;
            var table = data.Tables[tableIndex];
            int labelIndex = labelColumn == null ? -1 : table.Columns.IndexOf(labelColumn);
            int valueIndex = table.Columns.IndexOf(valueColumn);
            if (valueIndex < 0)
                return;

            var series = new ReportSeries { Name = seriesName };
            foreach (var row in table.Rows)
            {
                if (labelIndex >= 0)
                    data.Labels.Add(row.Count > labelIndex ? row[labelIndex] : "");
                decimal value = 0;
                if (row.Count > valueIndex)
                    decimal.TryParse(row[valueIndex], out value);
                series.Values.Add(value);
            }
            data.Series.Add(series);
        }
    }
}
