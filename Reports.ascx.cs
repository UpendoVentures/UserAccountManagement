// ***********************************************************************************
// Connect UsersLibrary
// 
// Copyright (C) 2013-2014 DNN-Connect Association, Philipp Becker
// http://dnn-connect.org
// 
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
// 
// ***********************************************************************************
using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using Connect.Libraries.UserManagement;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Framework.JavaScriptLibraries;

namespace Connect.Modules.UserManagement.AccountManagement
{
    public partial class Reports : ConnectUsersModuleBase
    {
        public Reports()
        {
            this.Init += Page_Init;
            this.Load += Page_Load;
        }

        private void Page_Init(object sender, EventArgs e)
        {
            JavaScript.RequestRegistration(CommonJs.DnnPlugins);
        }

        private void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                BindReports();
            }
        }

        private void cmdNewReport_Click(object sender, EventArgs e)
        {
            BindEditForm(Null.NullInteger);
        }

        private void cmdAddReport_Click(object sender, EventArgs e)
        {
            AddReport();
            pnlReportForm.Visible = false;
            BindReports();
        }

        private void cmdUpdateReport_Click(object sender, EventArgs e)
        {
            int ReportId = Convert.ToInt32(cmdUpdateReport.CommandArgument);
            UpdateReport(ReportId);
            pnlReportForm.Visible = false;
            BindReports();
        }

        private void cmdCancelReport_Click(object sender, EventArgs e)
        {
            pnlReportForm.Visible = false;
            txtReportName.Text = "";
            txtReportSql.Text = "";
        }

        private void cmdDeleteReport_Click(object sender, EventArgs e)
        {
            int ReportId = Convert.ToInt32(cmdDeleteReport.CommandArgument);
            UserReportsController.DeleteReport(ReportId);
            BindReports();
        }

        public void cmdEditReportFromList_Click(object sender, EventArgs e)
        {
            int ReportId = Convert.ToInt32(((LinkButton)sender).CommandArgument);
            BindEditForm(ReportId);
        }

        public void cmdDeleteReportFromList_Click(object sender, EventArgs e)
        {
            int ReportId = Convert.ToInt32(((LinkButton)sender).CommandArgument);
            UserReportsController.DeleteReport(ReportId);
            BindReports();
        }

        private void cmdBack_Click(object sender, EventArgs e)
        {
            Response.Redirect(DotNetNuke.Common.Globals.NavigateURL(TabId));
        }


        /* TODO ERROR: Skipped RegionDirectiveTrivia */
        private void AddReport()
        {
            var objReport = new UserReportInfo();
            objReport.FriendlyName = txtReportName.Text;
            objReport.PortalId = PortalId;
            objReport.Sql = txtReportSql.Text;
            objReport.NeedsParameters = false;
            UserReportsController.AddReport(objReport);
        }

        private void UpdateReport(int ReportId)
        {
            var objReport = UserReportsController.GetReport(ReportId);
            if (objReport is object)
            {
                objReport.Sql = txtReportSql.Text;
                objReport.FriendlyName = txtReportName.Text;
                UserReportsController.UpdateReport(objReport);
            }
        }

        private void BindReports()
        {
            var reports = new List<UserReportInfo>();
            reports = UserReportsController.GetReports(PortalId);
            rptReports.DataSource = reports;
            rptReports.DataBind();
        }

        private void BindEditForm(int ReportId)
        {
            pnlReportForm.Visible = true;
            txtReportName.Text = "";
            txtReportSql.Text = "";
            cmdUpdateReport.Visible = false;
            cmdDeleteReport.Visible = false;
            cmdAddReport.Visible = true;
            if (ReportId != Null.NullInteger)
            {
                var objReport = UserReportsController.GetReport(ReportId);
                if (objReport is object)
                {
                    txtReportName.Text = objReport.FriendlyName;
                    txtReportSql.Text = objReport.Sql;
                    cmdAddReport.Visible = false;
                    cmdUpdateReport.Visible = true;
                    cmdUpdateReport.CommandArgument = objReport.ReportId.ToString();
                    cmdDeleteReport.Visible = true;
                    cmdDeleteReport.CommandArgument = objReport.ReportId.ToString();
                }
            }
        }

        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
    }
}