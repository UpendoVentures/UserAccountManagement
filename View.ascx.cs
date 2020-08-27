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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Connect.Libraries.UserManagement;
using DotNetNuke.Common.Lists;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Data;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Profile;
using DotNetNuke.Entities.Users;
using DotNetNuke.Framework;
using DotNetNuke.Framework.JavaScriptLibraries;
using DotNetNuke.Instrumentation;
using DotNetNuke.Security;
using DotNetNuke.Security.Membership;
using DotNetNuke.Security.Roles;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.Localization;
using DotNetNuke.Web.UI.WebControls;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Telerik.Web.UI;

namespace Connect.Modules.UserManagement.AccountManagement
{
    public partial class View : ConnectUsersModuleBase, IActionable
    {
        public View()
        {
            this.Init += Page_Init;
            this.Load += Page_Load;
        }

        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(View));

        private bool _IsReportResult = false;

        protected void Page_Init(object sender, EventArgs e)
        {
            if (Request.QueryString["RoleId"] is null)
            {
                if (PreSelectRole != Null.NullInteger)
                {
                    Response.Redirect(DotNetNuke.Common.Globals.NavigateURL(TabId, "", "RoleId=" + PreSelectRole.ToString()), false);
                    Context.ApplicationInstance.CompleteRequest();
                }
                else
                {
                    Response.Redirect(DotNetNuke.Common.Globals.NavigateURL(TabId, "", "RoleId=" + PortalSettings.RegisteredRoleId.ToString().ToString()), false);
                    Context.ApplicationInstance.CompleteRequest();
                }
            }

            ProcessQuerystring(); // watch out for querystring actions
            JavaScript.RequestRegistration(CommonJs.DnnPlugins);
            JavaScript.RequestRegistration(CommonJs.jQueryUI);

            InitializeForm();

            var argplhControls = plhUser;
            ProcessFormTemplate(ref argplhControls, GetTemplate(ModuleTheme, Libraries.UserManagement.Constants.TemplateName_AccountForm, CurrentLocale, false), User);
            plhUser = argplhControls;
            var argplhControls1 = plhProfile;
            ProcessFormTemplate(ref argplhControls1, GetTemplate(ModuleTheme, Libraries.UserManagement.Constants.TemplateName_ProfileForm, CurrentLocale, false), User);
            plhProfile = argplhControls1;
            var argplhControls2 = plhCreate;
            ProcessFormTemplate(ref argplhControls2, GetTemplate(ModuleTheme, Libraries.UserManagement.Constants.TemplateName_CreateForm, CurrentLocale, false), null);
            plhCreate = argplhControls2;
        }

        private void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {

                // make sure txtSearch is empty on non-postback requests
                txtSearch.Text = string.Empty;
                BindRoles();
                BindSearchColOptions();
                PersonalizeOptions();
                BindReports();
                pnlBackToList.Visible = false;
                if (Request.QueryString["uid"] != null)
                {
                    if (Information.IsNumeric(Request.QueryString["uid"]))
                    {
                        BindUser(Convert.ToInt32(Request.QueryString["uid"]));
                    }
                }

                if (Request.QueryString["Action"] != null)
                {
                    if (Request.QueryString["Action"] == "Messaging")
                    {
                        pnlGrid.Visible = false;
                        pnlCreate.Visible = false;
                        pnlUser.Visible = false;
                        pnlRoleMessaging.Visible = true;
                    }
                }
            }
        }

        protected void grdUsers_PreRender(object sender, EventArgs e)
        {
            grdUsers.ClientSettings.Scrolling.AllowScroll = false;
        }

        protected void btnReport_Click(object sender, EventArgs e)
        {
            _IsReportResult = true;
            if (drpReports.SelectedIndex > 0)
            {
                ctlRoles.UnselectAllNodes();
                grdUsers.Rebind();
            }

            Session["UserReportsId"] = drpReports.SelectedValue;
            pnlGrid.Visible = true;
            pnlUser.Visible = false;
            pnlCreate.Visible = false;
        }

        protected void grdUsers_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
        {
            BindUsers();
        }

        public string GetStatusText(object strStatus)
        {
            switch (strStatus.ToString() ?? "")
            {
                case "1":
                    {
                        return Localization.GetString("RoleStatusApproved", LocalResourceFile);
                    }

                case "0":
                    {
                        return Localization.GetString("RoleStatusNoStatus", LocalResourceFile);
                    }

                case "-1":
                    {
                        return Localization.GetString("RoleStatusPending", LocalResourceFile);
                    }
            }

            return Localization.GetString("RoleStatusUnknown", LocalResourceFile);
        }

        protected void grdUsers_ItemDataBound(object sender, EventArgs eventArgs)
        {
            if (e.Item.ItemType == GridItemType.AlternatingItem | e.Item.ItemType == GridItemType.Item)
            {
                try
                {
                    GridDataItem dataBoundItem = (GridDataItem)e.Item;
                    DataRowView row = (DataRowView)e.Item.DataItem;
                    string intUser = Convert.ToInt32(row["UserID"]).ToString();
                    int intRole = Convert.ToInt32(ctlRoles.SelectedNode.Value);
                    HtmlGenericControl btnHardDelete = (HtmlGenericControl)e.Item.FindControl("btnHardDelete");
                    if (btnHardDelete != null)
                    {
                        btnHardDelete.Visible = false;
                        if (intRole == -2)
                        {
                            btnHardDelete.Visible = true;
                        }
                    }

                    HtmlGenericControl btnRemove = (HtmlGenericControl)e.Item.FindControl("btnRemove");
                    if (btnRemove != null)
                    {
                        btnRemove.Visible = false;
                        if (intRole != -2 && intRole != PortalSettings.RegisteredRoleId)
                        {
                            btnRemove.Visible = true;
                        }
                    }

                    HtmlGenericControl btnSetStatus = (HtmlGenericControl)e.Item.FindControl("btnSetStatus");
                    if (btnSetStatus != null)
                    {
                        btnSetStatus.Visible = false;
                        if (intRole != -2 && intRole != PortalSettings.RegisteredRoleId)
                        {
                            // If intRole <> -2 Then
                            btnSetStatus.Visible = true;
                        }
                    }

                    HtmlGenericControl btnSetDeleted = (HtmlGenericControl)e.Item.FindControl("btnSetDeleted");
                    if (btnSetDeleted != null)
                    {
                        btnSetDeleted.Visible = false;
                        if (AllowDelete && Conversions.ToDouble(intUser) != PortalSettings.AdministratorId &&
                            Conversions.ToDouble(intUser) != UserInfo.UserID && ctlRoles.SelectedNode != null &&
                            ctlRoles.SelectedNode.Value != "-2")
                        {
                            btnSetDeleted.Visible = true;
                        }
                    }

                    HtmlGenericControl btnRestore = (HtmlGenericControl)e.Item.FindControl("btnRestore");
                    if (btnRestore != null)
                    {
                        btnRestore.Visible = false;
                        if (ctlRoles.SelectedNode != null && ctlRoles.SelectedNode.Value == "-2")
                        {
                            btnRestore.Visible = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }

        protected void btnCancelMessaging_Click(object sender, EventArgs e)
        {
            Response.Redirect(DotNetNuke.Common.Globals.NavigateURL(TabId, string.Empty, "RoleId=" + Request.QueryString["RoleId"]), false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void btnApplyOptions_Click(object sender, EventArgs e)
        {
            SaveGridOptions();
            SaveSearchOptions();
            if (Request.QueryString["RoleId"] != null)
            {
                Response.Redirect(DotNetNuke.Common.Globals.NavigateURL(TabId, string.Empty, "RoleId=" + Request.QueryString["RoleId"]), false);
                Context.ApplicationInstance.CompleteRequest();
            }
            else
            {
                Response.Redirect(DotNetNuke.Common.Globals.NavigateURL(TabId), false);
                Context.ApplicationInstance.CompleteRequest();
            }
        }

        protected void cmdBack_Click(object sender, EventArgs e)
        {
            string url = DotNetNuke.Common.Globals.NavigateURL(TabId);
            if (Request.QueryString["RoleId"] != null)
            {
                url = DotNetNuke.Common.Globals.NavigateURL(TabId, string.Empty, "RoleId=" + Request.QueryString["RoleId"]);
            }

            Response.Redirect(url, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void grdUserRoles_NeedDataSource(object source, GridNeedDataSourceEventArgs e)
        {
            var roleController = new RoleController();
            IList userRoles = (IList)roleController.GetUserRoles(PortalId, User.Username, "");
            grdUserRoles.DataSource = userRoles;
        }

        protected void grdUserRoles_ItemDataBound(object sender, EventArgs eventArgs)
        {
            if (e.Item.ItemType == GridItemType.AlternatingItem | e.Item.ItemType == GridItemType.Item)
            {
                GridDataItem dataBoundItem = (GridDataItem)e.Item;
                var expiryDate = DateTime.Parse(dataBoundItem["col_ExpiryDate"].Text);
                if (expiryDate == Null.NullDate)
                {
                    dataBoundItem["col_ExpiryDate"].Text = "-";
                }
                else
                {
                    dataBoundItem["col_ExpiryDate"].Text = expiryDate.ToShortDateString();
                }

                var effectiveDate = DateTime.Parse(dataBoundItem["col_EffectiveDate"].Text);
                if (effectiveDate == Null.NullDate)
                {
                    dataBoundItem["col_EffectiveDate"].Text = User.Membership.CreatedDate.ToShortDateString();
                }
                else
                {
                    dataBoundItem["col_EffectiveDate"].Text = effectiveDate.ToShortDateString();
                }

                ImageButton btnApprove = (ImageButton)dataBoundItem["statusCol"].FindControl("btnApproveUserRole");
                btnApprove.ImageUrl = ResolveUrl("~/images/grant.gif");
                int currentRoleId = Conversions.ToInteger(btnApprove.CommandArgument);
                var roleController = new RoleController();
                IList userRoles = (IList)roleController.GetUserRoles(PortalId, User.Username, "");
                foreach (UserRoleInfo userRole in userRoles)
                {
                    if (userRole.RoleID == currentRoleId)
                    {
                        if (userRole.Status == RoleStatus.Approved)
                        {
                            btnApprove.Visible = false;
                        }
                    }
                }

                ImageButton btn = (ImageButton)dataBoundItem["removeCol"].FindControl("btnDeleteUserRole");
                btn.ImageUrl = ResolveUrl("~/images/delete.gif");
                if ((btn.CommandArgument ?? "") == (PortalSettings.RegisteredRoleId.ToString() ?? ""))
                {
                    btn.Visible = false;
                }

                if ((btn.CommandArgument ?? "") == (PortalSettings.AdministratorRoleId.ToString() ?? ""))
                {
                    btn.Visible = User.UserID != PortalSettings.AdministratorId;
                }
            }
        }

        protected void btnAddToRole_Click(object sender, EventArgs e)
        {
            var roleController = new RoleController();
            var effectiveDate = DateTime.Now;
            if (ctlRoleDatFrom.DbSelectedDate != null)
            {
                effectiveDate = Conversions.ToDate(ctlRoleDatFrom.DbSelectedDate);
            }

            var expiryDate = Null.NullDate;
            if (ctlRoleDateTo.DbSelectedDate != null)
            {
                expiryDate = Conversions.ToDate(ctlRoleDateTo.DbSelectedDate);
            }

            int roleId = Null.NullInteger;
            if (drpRoles.SelectedItem.Value != "-1")
            {
                roleId = Convert.ToInt32(drpRoles.SelectedItem.Value);
            }

            if (roleId != Null.NullInteger)
            {
                roleController.AddUserRole(PortalId, User.UserID, roleId, effectiveDate, expiryDate);
                lblRolesNote.Text = Localization.GetString("lblNotificationNote_Roles", LocalResourceFile);
                BindRoleMembershipChangedNotification(drpRoles.SelectedItem.Text, Libraries.UserManagement.Constants.TemplateName_EmailAddedToRole, effectiveDate, expiryDate);
                DataCache.RemoveCache("DNNWERK_USERLIST_ROLEID" + roleId.ToString());
                pnlRoleChange_Step1.Visible = false;
                pnlRoleChange_Step2.Visible = true;
                btnNotifyRole.CommandArgument = "add";
                btnNotifyRoleSkip.CommandArgument = "add";
            }
        }

        protected void btnDeleteUserRole_Click(object sender, ImageClickEventArgs e)
        {
            int roleId = Convert.ToInt32(((ImageButton)sender).CommandArgument);
            var roleController = new RoleController();
            var role = roleController.GetRole(roleId, PortalId);
            RoleController.DeleteUserRole(User, role, PortalSettings, false);
            DataCache.RemoveCache("DNNWERK_USERLIST_ROLEID" + roleId.ToString());
            string strRole = roleController.GetRole(roleId, PortalId).RoleName;
            lblRolesNote.Text = Localization.GetString("lblNotificationNote_Roles", LocalResourceFile);
            BindRoleMembershipChangedNotification(role.RoleName, Libraries.UserManagement.Constants.TemplateName_EmailRemovedFromRole, Null.NullDate, Null.NullDate);
            pnlRoleChange_Step1.Visible = false;
            pnlRoleChange_Step2.Visible = true;
            btnNotifyRole.CommandArgument = "remove";
            btnNotifyRoleSkip.CommandArgument = "remove";
        }

        protected void btnApproveUserRole_Click(object sender, ImageClickEventArgs e)
        {
            int roleId = Convert.ToInt32(((ImageButton)sender).CommandArgument);
            var roleController = new RoleController();
            var role = roleController.GetRole(roleId, PortalId);
            roleController.UpdateUserRole(PortalId, User.UserID, roleId, RoleStatus.Approved, false, false);
            DataCache.RemoveCache("DNNWERK_USERLIST_ROLEID" + roleId.ToString());
            string strRole = roleController.GetRole(roleId, PortalId).RoleName;
            lblRolesNote.Text = Localization.GetString("lblNotificationNote_Roles", LocalResourceFile);
            BindRoleMembershipChangedNotification(role.RoleName, Libraries.UserManagement.Constants.TemplateName_EmailRoleStatusChanged, DateTime.Now, Null.NullDate);
            pnlRoleChange_Step1.Visible = false;
            pnlRoleChange_Step2.Visible = true;
            btnNotifyRole.CommandArgument = "approve";
            btnNotifyRoleSkip.CommandArgument = "approve";
        }

        protected void btnNotifyRoleSkip_Click(object sender, EventArgs e)
        {
            lblRolesNote.Text = Localization.GetString("lblRolesChanged", LocalResourceFile);
            grdUserRoles.Rebind();
            pnlRoleChange_Step1.Visible = true;
            pnlRoleChange_Step2.Visible = false;
        }

        protected void btnNotifyRole_Click(object sender, EventArgs e)
        {
            string strPassword = Localization.GetString("HiddenPassword", LocalResourceFile);
            if (DotNetNuke.Security.Membership.MembershipProvider.Instance().PasswordRetrievalEnabled)
            {
                strPassword = DotNetNuke.Security.Membership.MembershipProvider.Instance().GetPassword(User, "");
            }

            string strBody = txtNotifyRoleBody.Content.Replace(Localization.GetString("HiddenPassword", LocalResourceFile), strPassword);
            string strSubject = txtNotifyRoleSubject.Text;
            if (string.IsNullOrEmpty(strSubject))
            {
                strSubject = Localization.GetString("txtNotifyRoleSubject", LocalResourceFile);
            }

            DotNetNuke.Services.Mail.Mail.SendEmail(PortalSettings.Email, User.Email, strSubject, strBody);
            lblRolesNote.Text = Localization.GetString("lblRolesChanged", LocalResourceFile);
            grdUserRoles.Rebind();
            pnlRoleChange_Step1.Visible = true;
            pnlRoleChange_Step2.Visible = false;
        }

        protected void cmdUpdateAccount_Click(object sender, EventArgs e)
        {
            UpdateAccount();
            var argplhControls = plhUser;
            ProcessFormTemplate(ref argplhControls, GetTemplate(ModuleTheme, Libraries.UserManagement.Constants.TemplateName_AccountForm, CurrentLocale, false), User);
            plhUser = argplhControls;
        }

        protected void cmdUpdateProfile_Click(object sender, EventArgs e)
        {
            UpdateProfile();
            var argplhControls = plhProfile;
            ProcessFormTemplate(ref argplhControls, GetTemplate(ModuleTheme, Libraries.UserManagement.Constants.TemplateName_ProfileForm, CurrentLocale, false), User);
            plhProfile = argplhControls;
        }

        protected void cmdUpdateSites_Click(object sender, EventArgs e)
        {
            int uid = Convert.ToInt32(Request.QueryString["uid"]);
            var objCurrentUser = UserController.GetUserById(PortalId, uid);
            bool blnErrorOccured = false;
            foreach (ListItem cItem in chkUserSites.Items)
            {
                var pCtrl = new PortalController();
                var objPortal = pCtrl.GetPortal(Convert.ToInt32(cItem.Value));
                if (objPortal != null)
                {
                    var objPortalUser = UserController.GetUserById(objPortal.PortalID, uid);
                    try
                    {
                        if (cItem.Selected)
                        {
                            if (cItem.Enabled)
                            {
                                if (objPortalUser is null)
                                {
                                    UserController.CopyUserToPortal(objCurrentUser, objPortal, false, false);
                                }
                            }
                        }
                        else if (objPortalUser != null)
                        {
                            UserController.RemoveUser(objPortalUser);
                        }
                    }
                    catch (Exception ex)
                    {
                        blnErrorOccured = true;
                    }
                }
            }

            if (blnErrorOccured)
            {
                lblSitesNote.Text = Localization.GetString("SitesError", LocalResourceFile);
            }
            else
            {
                lblSitesNote.Text = Localization.GetString("SitesSuccess", LocalResourceFile);
            }

            BindUserSites(uid);
        }

        protected void cmdForcePasswordChange_Click(object sender, EventArgs e)
        {
            var oUser = User;
            oUser.Membership.UpdatePassword = true;
            UserController.UpdateUser(PortalId, oUser);
            BindUser(oUser.UserID);
        }

        protected void cmdUnlockAccount_Click(object sender, EventArgs e)
        {
            Logger.Debug("Begin cmdUnlockAccount_Click()");
            try
            {
                var oUser = User;
                Logger.Debug("Begin UserController.UnLockUser()");
                UserController.UnLockUser(oUser);
                Logger.Debug("End UserController.UnLockUser()");
                Logger.Debug("Binding User");
                BindUser(oUser.UserID);
                Logger.Debug("Binded User");
                Logger.Debug("No exceptions occurred");
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message, ex);
                Exceptions.ProcessModuleLoadException(this, ex);
            }

            Logger.Debug("End cmdUnlockAccount_Click()");
        }

        protected void cmdAuthorizeAccount_Click(object sender, EventArgs e)
        {
            var oUser = User;
            oUser.Membership.Approved = true;
            UserController.UpdateUser(PortalId, oUser);
            BindUser(oUser.UserID);
        }

        protected void cmdRestoreAccount_Click(object sender, EventArgs e)
        {
            if (Request.IsAuthenticated == false)
            {
                return;
            }

            int TargetUserId = Null.NullInteger;
            if (Request.QueryString["uid"] is null)
            {
                return;
            }
            else if (Information.IsNumeric(Request.QueryString["uid"]))
            {
                TargetUserId = Convert.ToInt32(Request.QueryString["uid"]);
            }

            if (TargetUserId == Null.NullInteger)
            {
                return;
            }

            int TargetRoleId = Null.NullInteger;
            if (Request.QueryString["RoleId"] is null)
            {
                return;
            }
            else if (Information.IsNumeric(Request.QueryString["RoleId"]))
            {
                TargetRoleId = Convert.ToInt32(Request.QueryString["RoleId"]);
            }

            var oUser = UserController.GetUserById(PortalId, TargetUserId);
            if (oUser is null)
            {
                return;
            }

            try
            {
                UserController.RestoreUser(ref oUser);
            }
            catch
            {
            }

            ClearCache();
            Response.Redirect(DotNetNuke.Common.Globals.NavigateURL(TabId, "", "uid=" + oUser.UserID.ToString(), "RoleId=" + TargetRoleId.ToString(), "Action=Edit"), false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void cmdDeleteAccount_Click(object sender, EventArgs e)
        {
            if (Request.IsAuthenticated == false)
            {
                return;
            }

            int TargetUserId = Null.NullInteger;
            if (Request.QueryString["uid"] is null)
            {
                return;
            }
            else if (Information.IsNumeric(Request.QueryString["uid"]))
            {
                TargetUserId = Convert.ToInt32(Request.QueryString["uid"]);
            }

            if (TargetUserId == Null.NullInteger)
            {
                return;
            }

            int TargetRoleId = Null.NullInteger;
            if (Request.QueryString["RoleId"] is null)
            {
                return;
            }
            else if (Information.IsNumeric(Request.QueryString["RoleId"]))
            {
                TargetRoleId = Convert.ToInt32(Request.QueryString["RoleId"]);
            }

            var oUser = UserController.GetUserById(PortalId, TargetUserId);
            if (oUser is null)
            {
                return;
            }

            if (oUser.IsDeleted)
            {
                UserController.RemoveUser(oUser);
            }
            else
            {
                UserController.DeleteUser(ref oUser, false, false);
            }

            ClearCache();
            Response.Redirect(DotNetNuke.Common.Globals.NavigateURL(TabId, string.Empty, "RoleId=" + TargetRoleId.ToString()), false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void cmdCreateAccount_Click(object sender, EventArgs e)
        {
            BindUserCreateForm();
            pnlCreateAccount.Visible = false;
            pnlBackToList.Visible = true;
        }

        protected void cmdAddAccount_Click(object sender, EventArgs e)
        {
            AddAccount();
        }

        protected void btnHardDelete_Click(object sender, EventArgs e)
        {
            UserController.RemoveDeletedUsers(PortalId);
            ClearCache();
            grdUsers.Rebind();
        }

        protected void btnExport_Click(object sender, EventArgs e)
        {
            Export();
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            EnsureSafeSearchText();
            if (!string.IsNullOrEmpty(txtSearch.Text.Trim()))
            {

                // Session("Connect_UserSearchTerm") = txtSearch.Text

                SaveSearchOptions();
                pnlGrid.Visible = true;
                pnlUser.Visible = false;
                pnlCreate.Visible = false;
                grdUsers.Rebind();
            }
        }

        protected void cmdCancelCreate_Click(object sender, EventArgs e)
        {
            Response.Redirect(DotNetNuke.Common.Globals.NavigateURL(TabId, string.Empty, "RoleId=" + Request.QueryString["RoleId"]), false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void cmdBulkDelete_Click(object sender, EventArgs e)
        {
            if (AllowDelete)
            {
                for (int i = 0, loopTo = grdUsers.SelectedItems.Count - 1; i <= loopTo; i++)
                {
                    int intUser = Null.NullInteger;
                    try
                    {
                        var selecteditem = grdUsers.SelectedItems[i];
                        string selectedvalue = Conversions.ToString(selecteditem.OwnerTableView.DataKeyValues[selecteditem.ItemIndex]["UserId"]);
                        intUser = Convert.ToInt32(selectedvalue);
                        if (intUser != Null.NullInteger)
                        {
                            var oUser = UserController.GetUserById(PortalId, intUser);
                            if (oUser != null && !oUser.IsSuperUser && !(oUser.UserID == PortalSettings.AdministratorId))
                            {
                                UserController.DeleteUser(ref oUser, false, false);
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                ClearCache();
                grdUsers.Rebind();
            }
        }

        protected void cmdHardDeleteSelected_Click(object sender, EventArgs e)
        {
            for (int i = 0, loopTo = grdUsers.SelectedItems.Count - 1; i <= loopTo; i++)
            {
                int intUser = Null.NullInteger;
                try
                {
                    var selecteditem = grdUsers.SelectedItems[i];
                    string selectedvalue = Conversions.ToString(selecteditem.OwnerTableView.DataKeyValues[selecteditem.ItemIndex]["UserId"]);
                    intUser = Convert.ToInt32(selectedvalue);
                    if (intUser != Null.NullInteger)
                    {
                        var oUser = UserController.GetUserById(PortalId, intUser);
                        UserController.RemoveUser(oUser);
                    }
                }
                catch
                {
                }
            }

            ClearCache();
            grdUsers.Rebind();
        }

        protected void cmdBulkRemove_Click(object sender, EventArgs e)
        {
            var rc = new RoleController();
            int intRole = Convert.ToInt32(Request.QueryString["RoleId"]);
            var role = rc.GetRole(intRole, PortalId);
            for (int i = 0, loopTo = grdUsers.SelectedItems.Count - 1; i <= loopTo; i++)
            {
                int intUser = Null.NullInteger;
                try
                {
                    var selecteditem = grdUsers.SelectedItems[i];
                    string selectedvalue = Conversions.ToString(selecteditem.OwnerTableView.DataKeyValues[selecteditem.ItemIndex]["UserId"]);
                    intUser = Convert.ToInt32(selectedvalue);
                    if (intUser != Null.NullInteger)
                    {
                        var oUser = UserController.GetUserById(PortalId, intUser);
                        if (oUser != null && !oUser.IsSuperUser && !(oUser.UserID == PortalSettings.AdministratorId & intRole == PortalSettings.AdministratorRoleId))
                        {
                            RoleController.DeleteUserRole(oUser, role, PortalSettings, false);
                        }
                    }
                }
                catch
                {
                }
            }

            ClearCache();
            grdUsers.Rebind();
        }

        protected void btnNotifyUser_Click(object sender, EventArgs e)
        {
            try
            {
                DotNetNuke.Services.Mail.Mail.SendEmail(PortalSettings.Email, User.Email, txtEmailSubjectAll.Text, txtEmailBodyAll.Content);
                lblEmailNote.Text = Localization.GetString("MessageSent", LocalResourceFile);
            }
            catch (Exception ex)
            {
                lblEmailNote.Text = string.Format(Localization.GetString("MessageNotSent", LocalResourceFile), ex.Message);
            }
        }

        /// <summary>
        /// Sends an email to a users
        /// </summary>
        /// <param name="user"></param>
        /// <remarks></remarks>
        private void SendEmail(UserInfo user, string subject, string body)
        {
            string strPassword = Localization.GetString("HiddenPassword", LocalResourceFile);
            if (DotNetNuke.Security.Membership.MembershipProvider.Instance().PasswordRetrievalEnabled)
            {
                strPassword = DotNetNuke.Security.Membership.MembershipProvider.Instance().GetPassword(user, string.Empty);
            }

            string strBody = body.Replace(Localization.GetString("HiddenPassword", LocalResourceFile), strPassword);
            string strSubject = subject;
            DotNetNuke.Services.Mail.Mail.SendEmail(PortalSettings.Email, user.Email, strSubject, strBody);
        }

        protected void btnSendMessage_Click(object sender, EventArgs e)
        {
            try
            {
                var users = new List<UserInfo> {User};
                SendMessage(users, txtMessageSubject.Text, txtMessageBody.Text);
                DotNetNuke.UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("InternalMessagesSent", LocalResourceFile), DotNetNuke.UI.Skins.Controls.ModuleMessage.ModuleMessageType.GreenSuccess);
            }
            catch (Exception ex)
            {
                DotNetNuke.UI.Skins.Skin.AddModuleMessage(this, string.Format(Localization.GetString("InternalMessagesNotSent", LocalResourceFile), ex.Message), DotNetNuke.UI.Skins.Controls.ModuleMessage.ModuleMessageType.RedError);
            }
        }

        protected void btnSendMessages_Click(object sender, EventArgs e)
        {
            var recipients = new List<UserInfo>();
            DataSet ds = null;
            string strError = "";
            if (ctlRoles.SelectedNode is null)
            {
                _IsReportResult = true;
            }

            if (_IsReportResult)
            {
                ds = GetReportResult(ref strError);
            }
            else
            {
                ds = GetUserList();
            }

            switch (rblMessagingMode.SelectedValue ?? "")
            {
                case "e":
                    {
                        string resultMsg = "";
                        foreach (DataRow row in ds.Tables[0].Rows)
                        {
                            try
                            {
                                DotNetNuke.Services.Mail.Mail.SendEmail(PortalSettings.Email, Conversions.ToString(row["EMail"]), txtEmailSubjectAll.Text, txtEmailBodyAll.Content);
                            }
                            catch (Exception ex)
                            {
                                resultMsg += ex.Message + "<br />";
                            }
                        }

                        if (string.IsNullOrEmpty(resultMsg))
                        {
                            DotNetNuke.UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("MessagesSent", LocalResourceFile), DotNetNuke.UI.Skins.Controls.ModuleMessage.ModuleMessageType.GreenSuccess);
                        }
                        else
                        {
                            DotNetNuke.UI.Skins.Skin.AddModuleMessage(this, string.Format(Localization.GetString("MessagesNotSent", LocalResourceFile), resultMsg), DotNetNuke.UI.Skins.Controls.ModuleMessage.ModuleMessageType.RedError);
                        }

                        break;
                    }

                case "m":
                    {
                        foreach (DataRow row in ds.Tables[0].Rows)
                        {
                            var oUser = UserController.GetUserById(PortalId, Conversions.ToInteger(row["UserId"]));
                            if (oUser != null)
                            {
                                recipients.Add(oUser);
                            }
                        }

                        try
                        {
                            SendMessage(recipients, txtEmailSubjectAll.Text, txtEmailBodyAll.Content);
                            DotNetNuke.UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("InternalMessageSent", LocalResourceFile), DotNetNuke.UI.Skins.Controls.ModuleMessage.ModuleMessageType.GreenSuccess);
                        }
                        catch (Exception ex)
                        {
                            DotNetNuke.UI.Skins.Skin.AddModuleMessage(this, string.Format(Localization.GetString("InternalMessageNotSent", LocalResourceFile), ex.Message), DotNetNuke.UI.Skins.Controls.ModuleMessage.ModuleMessageType.RedError);
                        }

                        break;
                    }
            }
        }

        /// <summary>
        /// Sends a message to a list of users
        /// </summary>
        /// <param name="users"></param>
        /// <remarks></remarks>
        private void SendMessage(List<UserInfo> users, string subject, string body)
        {
            string strPassword = Localization.GetString("HiddenPassword", LocalResourceFile);
            if (DotNetNuke.Security.Membership.MembershipProvider.Instance().PasswordRetrievalEnabled)
            {
                strPassword = DotNetNuke.Security.Membership.MembershipProvider.Instance().GetPassword(User, string.Empty);
            }

            string strBody = body.Replace(Localization.GetString("HiddenPassword", LocalResourceFile), strPassword);
            string strSubject = subject;
            var message = new DotNetNuke.Services.Social.Messaging.Message();
            message.Subject = strSubject;
            message.Body = strBody;
            ServiceLocator<DotNetNuke.Services.Social.Messaging.IMessagingController, DotNetNuke.Services.Social.Messaging.MessagingController>.Instance.SendMessage(message, null, users, null);
        }

        protected void cmdResetPasswordLink_Click(object sender, EventArgs e)
        {
            pnlResetButton.Visible = false;
            pnlPassword_Step1.Visible = false;
            pnlPassword_Step2.Visible = true;
            BindPasswordNotification();
        }

        protected void cmdUpdatePassword_Click(object sender, EventArgs e)
        {
            bool blnProceed = true;

            // verifiy passwords have been entered and both passwords match

            if ((txtPassword1.Text ?? "") == (txtPassword2.Text ?? ""))
            {
                if (UserController.ValidatePassword(txtPassword1.Text))
                {
                    blnProceed = true;
                }
                else
                {
                    lblPasswordNote.Text = Localization.GetString("PasswordPoliciesError", LocalResourceFile);
                    blnProceed = false;
                }
            }
            else
            {
                lblPasswordNote.Text = Localization.GetString("PasswordMatchError", LocalResourceFile);
                blnProceed = false;
            }

            if (blnProceed)
            {
                string strPassword = string.Empty;
                strPassword = txtPassword1.Text;
                try
                {
                    if (UserController.ChangePassword(User, string.Empty, strPassword))
                    {
                        lblPasswordNote.Text = Localization.GetString("PasswordSetNotifyQuestion", LocalResourceFile);
                        pnlPassword_Step1.Visible = false;
                        pnlPassword_Step2.Visible = true;
                        BindPasswordNotification();
                    }
                    else
                    {
                        lblPasswordNote.Text = Localization.GetString("PasswordResetError", LocalResourceFile);
                    }
                }
                catch (Exception ex)
                {
                    lblPasswordNote.Text = Localization.GetString("CannotUsePassword", LocalResourceFile);
                }
            }
        }

        protected void btnNotifyPassword_Click(object sender, EventArgs e)
        {
            string strPassword = Localization.GetString("HiddenPassword", LocalResourceFile);
            if (DotNetNuke.Security.Membership.MembershipProvider.Instance().PasswordRetrievalEnabled)
            {
                strPassword = DotNetNuke.Security.Membership.MembershipProvider.Instance().GetPassword(User, string.Empty);
            }

            string strBody = txtNotifyPasswordBody.Content.Replace(Localization.GetString("HiddenPassword", LocalResourceFile), strPassword);
            string strSubject = txtNotifyPasswordSubject.Text;
            if (string.IsNullOrEmpty(strSubject))
            {
                strSubject = Localization.GetString("txtNotifyPasswordSubject", LocalResourceFile);
            }

            try
            {
                DotNetNuke.Services.Mail.Mail.SendEmail(PortalSettings.Email, User.Email, strSubject, strBody);
                lblPasswordNote.Text = Localization.GetString("UserNotifiedPassword", LocalResourceFile);
                if (DotNetNuke.Security.Membership.MembershipProvider.Instance().PasswordRetrievalEnabled == false && DotNetNuke.Security.Membership.MembershipProvider.Instance().PasswordResetEnabled == true)
                {
                    lblPasswordNote.Text = Localization.GetString("UserNotifiedReset", LocalResourceFile);
                }
            }
            catch (Exception ex)
            {
                lblPasswordNote.Text = string.Format(Localization.GetString("MessageNotSent.Text", LocalResourceFile), ex.Message);
            }

            if (DotNetNuke.Security.Membership.MembershipProvider.Instance().PasswordRetrievalEnabled == false && DotNetNuke.Security.Membership.MembershipProvider.Instance().PasswordResetEnabled == true)
            {
                pnlResetButton.Visible = true;
                pnlPassword_Step1.Visible = false;
                pnlPassword_Step2.Visible = false;
                txtPassword1.Text = string.Empty;
                txtPassword2.Text = string.Empty;
            }
            else if (DotNetNuke.Security.Membership.MembershipProvider.Instance().PasswordRetrievalEnabled == true)
            {
                pnlResetButton.Visible = false;
                pnlPassword_Step1.Visible = true;
                pnlPassword_Step2.Visible = false;
                txtPassword1.Text = string.Empty;
                txtPassword2.Text = string.Empty;
            }
        }

        protected void btnNotifyPasswordSkip_Click(object sender, EventArgs e)
        {
            lblPasswordNote.Text = Localization.GetString("PasswordSet", LocalResourceFile);
            pnlPassword_Step1.Visible = true;
            pnlPassword_Step2.Visible = false;
            txtPassword1.Text = string.Empty;
            txtPassword2.Text = string.Empty;
        }

        private void BindPasswordNotification()
        {
            string Locale = CurrentLocale;
            if (!string.IsNullOrEmpty(User.Profile.PreferredLocale))
            {
                Locale = User.Profile.PreferredLocale;
            }

            UserController.ResetPasswordToken(User, 1440);
            string reseturl = string.Format("http://{0}/default.aspx?ctl=PasswordReset&resetToken={1}", PortalSettings.PortalAlias.HTTPAlias, Server.UrlEncode(User.PasswordResetToken.ToString()));
            string strTemplate = GetTemplate(ModuleTheme, Libraries.UserManagement.Constants.TemplateName_EmailPasswordReset, Locale, false);
            strTemplate = strTemplate.Replace("[FIRSTNAME]", User.FirstName);
            strTemplate = strTemplate.Replace("[LASTNAME]", User.LastName);
            strTemplate = strTemplate.Replace("[DISPLAYNAME]", User.DisplayName);
            strTemplate = strTemplate.Replace("[PORTALNAME]", PortalSettings.PortalName);
            strTemplate = strTemplate.Replace("[PORTALURL]", PortalSettings.PortalAlias.HTTPAlias);
            strTemplate = strTemplate.Replace("[USERNAME]", User.Username);
            strTemplate = strTemplate.Replace("[PASSWORD]", Localization.GetString("HiddenPassword", LocalResourceFile));
            strTemplate = strTemplate.Replace("[RESETLINK]", "<a href=\"" + reseturl + "\">" + Localization.GetString("ClickToReset", LocalResourceFile) + "</a>");
            strTemplate = strTemplate.Replace("[RESETLINKURL]", reseturl);
            strTemplate = strTemplate.Replace("[RECIPIENTUSERID]", User.UserID.ToString());
            strTemplate = strTemplate.Replace("[USERID]", User.UserID.ToString());
            txtNotifyPasswordBody.Content = strTemplate;
        }

        private void ProcessQuerystring()
        {
            if (Request.IsAuthenticated == false)
            {
                return;
            }

            int TargetUserId = Null.NullInteger;
            if (Request.QueryString["uid"] is null)
            {
                return;
            }
            else if (Information.IsNumeric(Request.QueryString["uid"]))
            {
                TargetUserId = Convert.ToInt32(Request.QueryString["uid"]);
            }

            if (TargetUserId == Null.NullInteger)
            {
                return;
            }

            int TargetRoleId = Null.NullInteger;
            if (Request.QueryString["RoleId"] is null)
            {
                return;
            }
            else if (Information.IsNumeric(Request.QueryString["RoleId"]))
            {
                TargetRoleId = Convert.ToInt32(Request.QueryString["RoleId"]);
            }

            if (Request.QueryString["Action"] != null)
            {
                switch (Request.QueryString["Action"].ToLower() ?? "")
                {
                    case "approve":
                        {
                            var oUser = UserController.GetUserById(PortalId, TargetUserId);
                            if (oUser is null)
                            {
                                return;
                            }

                            var roleController = new RoleController();
                            var role = roleController.GetRole(TargetRoleId, PortalId);
                            roleController.UpdateUserRole(PortalId, User.UserID, TargetRoleId, RoleStatus.Approved, false, false);
                            DataCache.RemoveCache("DNNWERK_USERLIST_ROLEID" + TargetRoleId.ToString());
                            if (Request.QueryString["Notify"] != null)
                            {
                                if (Request.QueryString["Notify"] == "1")
                                {
                                    string strRole = roleController.GetRole(TargetRoleId, PortalId).RoleName;
                                    lblRolesNote.Text = Localization.GetString("lblNotificationNote_Roles", LocalResourceFile);
                                    BindRoleMembershipChangedNotification(role.RoleName, Libraries.UserManagement.Constants.TemplateName_EmailRoleStatusChanged, DateTime.Now, Null.NullDate);
                                    pnlRoleChange_Step1.Visible = false;
                                    pnlRoleChange_Step2.Visible = true;
                                    btnNotifyRole.CommandArgument = "approve";
                                    btnNotifyRoleSkip.CommandArgument = "approve";
                                }
                                else
                                {
                                    Response.Redirect(DotNetNuke.Common.Globals.NavigateURL(TabId, "", "RoleId=" + TargetRoleId.ToString()), false);
                                    Context.ApplicationInstance.CompleteRequest();
                                }
                            }
                            else
                            {
                                Response.Redirect(DotNetNuke.Common.Globals.NavigateURL(TabId, "", "RoleId=" + TargetRoleId.ToString()), false);
                                Context.ApplicationInstance.CompleteRequest();
                            }

                            break;
                        }

                    case "pending":
                        {
                            var oUser = UserController.GetUserById(PortalId, TargetUserId);
                            if (oUser is null)
                            {
                                return;
                            }

                            var roleController = new RoleController();
                            var role = roleController.GetRole(TargetRoleId, PortalId);
                            roleController.UpdateUserRole(PortalId, User.UserID, TargetRoleId, RoleStatus.Pending, false, false);
                            DataCache.RemoveCache("DNNWERK_USERLIST_ROLEID" + TargetRoleId.ToString());
                            if (Request.QueryString["Notify"] != null)
                            {
                                if (Request.QueryString["Notify"] == "1")
                                {
                                    string strRole = roleController.GetRole(TargetRoleId, PortalId).RoleName;
                                    lblRolesNote.Text = Localization.GetString("lblNotificationNote_Roles", LocalResourceFile);
                                    BindRoleMembershipChangedNotification(role.RoleName, Libraries.UserManagement.Constants.TemplateName_EmailRoleStatusChanged, Null.NullDate, Null.NullDate);
                                    pnlRoleChange_Step1.Visible = false;
                                    pnlRoleChange_Step2.Visible = true;
                                    btnNotifyRole.CommandArgument = "pending";
                                    btnNotifyRoleSkip.CommandArgument = "pending";
                                }
                                else
                                {
                                    Response.Redirect(DotNetNuke.Common.Globals.NavigateURL(TabId, "", "RoleId=" + TargetRoleId.ToString()), false);
                                    Context.ApplicationInstance.CompleteRequest();
                                }
                            }
                            else
                            {
                                Response.Redirect(DotNetNuke.Common.Globals.NavigateURL(TabId, "", "RoleId=" + TargetRoleId.ToString()), false);
                                Context.ApplicationInstance.CompleteRequest();
                            }

                            break;
                        }

                    case "remove":
                        {
                            var oUser = UserController.GetUserById(PortalId, TargetUserId);
                            if (oUser is null)
                            {
                                return;
                            }

                            if (oUser.IsSuperUser)
                            {
                                return;
                            }

                            if (oUser.UserID == PortalSettings.AdministratorId)
                            {
                                return;
                            }

                            var rc = new RoleController();
                            var role = rc.GetRole(TargetRoleId, PortalId);
                            RoleController.DeleteUserRole(oUser, role, PortalSettings, false);
                            ClearCache();
                            Response.Redirect(DotNetNuke.Common.Globals.NavigateURL(TabId, string.Empty, "RoleId=" + TargetRoleId.ToString()), false);
                            Context.ApplicationInstance.CompleteRequest();
                            break;
                        }

                    case "delete":
                        {
                            if (AllowDelete)
                            {
                                var oUser = UserController.GetUserById(PortalId, TargetUserId);
                                if (oUser is null)
                                {
                                    return;
                                }

                                if (oUser.IsSuperUser)
                                {
                                    return;
                                }

                                if (oUser.UserID == PortalSettings.AdministratorId)
                                {
                                    return;
                                }

                                UserController.DeleteUser(ref oUser, false, false);
                                ClearCache();
                                Response.Redirect(DotNetNuke.Common.Globals.NavigateURL(TabId, string.Empty, "RoleId=" + TargetRoleId.ToString()), false);
                                Context.ApplicationInstance.CompleteRequest();
                            }

                            break;
                        }

                    case "harddelete":
                        {
                            var oUser = UserController.GetUserById(PortalId, TargetUserId);
                            if (oUser is null)
                            {
                                return;
                            }

                            UserController.RemoveUser(oUser);
                            ClearCache();
                            Response.Redirect(DotNetNuke.Common.Globals.NavigateURL(TabId, "", "RoleId=-2"), false);
                            Context.ApplicationInstance.CompleteRequest();
                            break;
                        }

                    case "restore":
                        {
                            var oUser = UserController.GetUserById(PortalId, TargetUserId);
                            if (oUser is null)
                            {
                                return;
                            }

                            try
                            {
                                UserController.RestoreUser(ref oUser);
                            }
                            catch
                            {
                            }

                            ClearCache();
                            Response.Redirect(DotNetNuke.Common.Globals.NavigateURL(TabId, string.Empty, "uid=" + oUser.UserID.ToString(), "RoleId=" + PortalSettings.RegisteredRoleId.ToString(), "Action=Edit"), false);
                            Context.ApplicationInstance.CompleteRequest();
                            break;
                        }

                    case "impersonate":
                        {
                            var oUser = UserController.GetUserById(PortalId, TargetUserId);
                            if (oUser is null)
                            {
                                return;
                            }

                            ImpersonateAccount(oUser);
                            break;
                        }
                }
            }
        }

        private void ImpersonateAccount(UserInfo objUser)
        {
            if (DotNetNuke.Security.Membership.MembershipProvider.Instance().PasswordRetrievalEnabled == false)
            {
                return;
            }

            if (objUser != null)
            {
                if (UserInfo != null)
                {
                    DataCache.ClearUserCache(PortalSettings.PortalId, Context.User.Identity.Name);
                }

                var objPortalSecurity = new PortalSecurity();
                objPortalSecurity.SignOut();
                string password = UserController.GetPassword(ref objUser, string.Empty);
                var status = default(UserLoginStatus);
                UserController.UserLogin(PortalSettings.PortalId, objUser.Username, password, string.Empty, PortalSettings.PortalName, Request.UserHostAddress, ref status, false);
                Response.Redirect(DotNetNuke.Common.Globals.NavigateURL(PortalSettings.HomeTabId), false);
                Context.ApplicationInstance.CompleteRequest();
            }
        }

        private void BindUserCreateForm()
        {
            pnlGrid.Visible = false;
            pnlUser.Visible = false;
            pnlCreate.Visible = true;
            if ((ctlRoles.SelectedNode.Value ?? "") != (PortalSettings.RegisteredRoleId.ToString() ?? ""))
            {
                if (Information.IsNumeric(ctlRoles.SelectedNode.Value))
                {
                    int roleid = Convert.ToInt32(ctlRoles.SelectedNode.Value);
                    try
                    {
                        var rc = new RoleController();
                        var role = rc.GetRole(roleid, PortalId);
                        if (role != null)
                        {
                            lblCreateAccountNote.Text = string.Format(Localization.GetString("CreateAccountInRole", LocalResourceFile), rc.GetRole(roleid, PortalId).RoleName);
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void AddAccount()
        {
            string strMessage = string.Empty;
            bool blnUpdateUsername = false;
            bool blnUpdateFirstname = false;
            bool blnUpdateLastname = false;
            bool blnUpdateDisplayname = false;
            bool blnUpdatePassword = false;
            bool blnUpdateEmail = false;
            TextBox txtUsername = (TextBox)FindControlRecursive(plhCreate, plhCreate.ID + "_" + Libraries.UserManagement.Constants.ControlId_Username);
            blnUpdateUsername = txtUsername != null;
            if (blnUpdateUsername)
            {
                if (!IsValidUserAttribute(Libraries.UserManagement.Constants.User_Username, plhCreate))
                {
                    strMessage += "Error";
                    Control argobjControl = plhCreate;
                    AddErrorIndicator(Libraries.UserManagement.Constants.User_Username, ref argobjControl);
                    plhCreate = (PlaceHolder)argobjControl;
                }
            }

            TextBox txtEmail = (TextBox)FindControlRecursive(plhCreate, plhCreate.ID + "_" + Libraries.UserManagement.Constants.ControlId_Email);
            blnUpdateEmail = txtEmail != null;
            if (blnUpdateEmail)
            {
                if (!IsValidUserAttribute(Libraries.UserManagement.Constants.User_Email, plhCreate))
                {
                    strMessage += "Error";
                    Control argobjControl1 = plhCreate;
                    AddErrorIndicator(Libraries.UserManagement.Constants.User_Email, ref argobjControl1);
                    plhCreate = (PlaceHolder)argobjControl1;
                }
            }

            TextBox txtPassword = (TextBox)FindControlRecursive(plhCreate, plhCreate.ID + "_" + Libraries.UserManagement.Constants.ControlId_Password1);
            TextBox txtPassword2 = (TextBox)FindControlRecursive(plhCreate, plhCreate.ID + "_" + Libraries.UserManagement.Constants.ControlId_Password2);
            blnUpdatePassword = txtPassword != null && txtPassword2 != null;
            if (blnUpdatePassword)
            {
                if (!IsValidUserAttribute(Libraries.UserManagement.Constants.User_Password1, plhCreate))
                {
                    strMessage += "Error";
                    Control argobjControl2 = plhCreate;
                    AddErrorIndicator(Libraries.UserManagement.Constants.User_Password1, ref argobjControl2);
                    plhCreate = (PlaceHolder)argobjControl2;
                }

                if (!IsValidUserAttribute(Libraries.UserManagement.Constants.User_Password2, plhCreate))
                {
                    strMessage += "Error";
                    Control argobjControl3 = plhCreate;
                    AddErrorIndicator(Libraries.UserManagement.Constants.User_Password2, ref argobjControl3);
                    plhCreate = (PlaceHolder)argobjControl3;
                }
            }

            TextBox txtFirstName = (TextBox)FindControlRecursive(plhCreate, plhCreate.ID + "_" + Libraries.UserManagement.Constants.ControlId_Firstname);
            blnUpdateFirstname = txtFirstName != null;
            if (blnUpdateFirstname)
            {
                if (!IsValidUserAttribute(Libraries.UserManagement.Constants.User_Firstname, plhCreate))
                {
                    strMessage += "Error";
                    Control argobjControl4 = plhCreate;
                    AddErrorIndicator(Libraries.UserManagement.Constants.User_Firstname, ref argobjControl4);
                    plhCreate = (PlaceHolder)argobjControl4;
                }
            }

            TextBox txtLastName = (TextBox)FindControlRecursive(plhCreate, plhCreate.ID + "_" + Libraries.UserManagement.Constants.ControlId_Lastname);
            blnUpdateLastname = txtLastName != null;
            if (blnUpdateLastname)
            {
                if (!IsValidUserAttribute(Libraries.UserManagement.Constants.User_Lastname, plhCreate))
                {
                    strMessage += "Error";
                    Control argobjControl5 = plhCreate;
                    AddErrorIndicator(Libraries.UserManagement.Constants.User_Lastname, ref argobjControl5);
                    plhCreate = (PlaceHolder)argobjControl5;
                }
            }

            TextBox txtDisplayName = (TextBox)FindControlRecursive(plhCreate, plhCreate.ID + "_" + Libraries.UserManagement.Constants.ControlId_Displayname);
            blnUpdateDisplayname = txtDisplayName != null;
            if (strMessage.Length > 0)
            {
                lblCreateAccountNote.Text = Localization.GetString("FieldsRequired", LocalResourceFile);
                return;
            }

            var oUser = new UserInfo
            {
                Membership = {Approved = true},
                AffiliateID = Null.NullInteger,
                PortalID = PortalSettings.PortalId,
                Username = string.Empty,
                DisplayName = string.Empty,
                Email = string.Empty
            };
            oUser.Membership.Password = string.Empty;
            if (blnUpdateUsername)
            {
                oUser.Username = txtUsername.Text.Trim();
            }

            if (blnUpdateDisplayname)
            {
                oUser.DisplayName = txtDisplayName.Text;
            }

            if (blnUpdateFirstname)
            {
                oUser.FirstName = txtFirstName.Text;
            }

            if (blnUpdateLastname)
            {
                oUser.LastName = txtLastName.Text;
            }

            if (blnUpdateEmail)
            {
                oUser.Email = txtEmail.Text;
            }

            if (string.IsNullOrEmpty(oUser.Username))
            {
                if (blnUpdateEmail)
                {
                    oUser.Username = txtEmail.Text.Trim();
                }
            }

            if (string.IsNullOrEmpty(oUser.DisplayName))
            {
                if (blnUpdateFirstname)
                {
                    oUser.DisplayName = txtFirstName.Text.Trim();
                }

                if (blnUpdateLastname)
                {
                    if (blnUpdateFirstname)
                    {
                        oUser.DisplayName += " ";
                    }

                    oUser.DisplayName += txtLastName.Text.Trim();
                }
            }


            // try updating password
            if (blnUpdatePassword)
            {
                if ((txtPassword.Text ?? "") == (txtPassword2.Text ?? ""))
                {
                    if (!UserController.ValidatePassword(txtPassword.Text))
                    {
                        lblCreateAccountNote.Text = Localization.GetString("PasswordPoliciesError", LocalResourceFile);
                        return;
                    }
                }
                else
                {
                    lblCreateAccountNote.Text = Localization.GetString("PasswordMatchError", LocalResourceFile);
                    return;
                }

                oUser.Membership.Password = txtPassword.Text;
            }
            else
            {
                oUser.Membership.Password = UserController.GeneratePassword();
            }

            // try updating displayname

            if (string.IsNullOrEmpty(oUser.Username) | string.IsNullOrEmpty(oUser.Email) | string.IsNullOrEmpty(oUser.DisplayName) | string.IsNullOrEmpty(oUser.Membership.Password))
            {
                // template must be setup up wrong
                lblCreateAccountNote.Text = Localization.GetString("TemplateError", LocalResourceFile);
                return;
            }

            // set up profile
            oUser.Profile = new UserProfile();
            oUser.Profile.InitialiseProfile(PortalSettings.PortalId, true);

            // see if we can create the account
            var createStatus = UserController.CreateUser(ref oUser);
            if (createStatus != UserCreateStatus.Success)
            {
                switch (createStatus)
                {
                    case UserCreateStatus.UsernameAlreadyExists:
                        {
                            strMessage = Localization.GetString("UsernameAlreadyExists", LocalResourceFile);
                            break;
                        }

                    default:
                        {
                            strMessage = string.Format(Localization.GetString("UserCreateError", LocalResourceFile), createStatus.ToString());
                            break;
                        }
                }

                if (!string.IsNullOrEmpty(strMessage))
                {
                    strMessage = strMessage;
                }
                else
                {
                    strMessage = createStatus.ToString();
                }

                lblCreateAccountNote.Text = strMessage;
                return;
            }

            // try updating firstname
            if (blnUpdateFirstname)
            {
                oUser.FirstName = txtFirstName.Text;
                oUser.Profile.FirstName = txtFirstName.Text;
            }

            // try updating lastname
            if (blnUpdateLastname)
            {
                oUser.LastName = txtLastName.Text;
                oUser.Profile.LastName = txtLastName.Text;
            }

            oUser.Profile.PreferredLocale = PortalSettings.DefaultLanguage;
            oUser.Profile.PreferredTimeZone = PortalSettings.TimeZone;
            UserController.UpdateUser(PortalId, oUser);

            // add to role
            if ((ctlRoles.SelectedNode.Value ?? "") != (PortalSettings.RegisteredRoleId.ToString() ?? ""))
            {
                if (Information.IsNumeric(ctlRoles.SelectedNode.Value))
                {
                    int roleid = Convert.ToInt32(ctlRoles.SelectedNode.Value);
                    try
                    {
                        var rc = new RoleController();
                        var role = rc.GetRole(roleid, PortalId);
                        if (role != null)
                        {
                            rc.AddUserRole(PortalId, oUser.UserID, role.RoleID, Null.NullDate);
                        }
                    }
                    catch
                    {
                    }
                }
            }

            string url = DotNetNuke.Common.Globals.NavigateURL(TabId, string.Empty, "uid=" + oUser.UserID.ToString());
            if ((ctlRoles.SelectedNode.Value ?? "") != (PortalSettings.RegisteredRoleId.ToString() ?? ""))
            {
                if (Information.IsNumeric(ctlRoles.SelectedNode.Value))
                {
                    int roleid = Convert.ToInt32(ctlRoles.SelectedNode.Value);
                    url = DotNetNuke.Common.Globals.NavigateURL(TabId, string.Empty, "RoleId=" + roleid.ToString(), "uid=" + oUser.UserID.ToString());
                }
            }

            ClearCache();
            UserController.ResetPasswordToken(User, 1440);
            Response.Redirect(url, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private void Export()
        {
            var users = new List<UserInfo>();
            DataSet ds = null;
            string strError = string.Empty;
            if (ctlRoles.SelectedNode is null)
            {
                _IsReportResult = true;
            }

            if (_IsReportResult)
            {
                ds = GetReportResult(ref strError);
            }
            else
            {
                ds = GetUserList();
            }

            if (ds != null)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    var oUser = UserController.GetUserById(PortalId, Conversions.ToInteger(row["UserId"]));
                    if (oUser != null)
                    {
                        users.Add(oUser);
                    }
                }
            }

            string sFilename = "Members";
            try
            {
                sFilename = "Members_" + ctlRoles.SelectedNode.Text;
            }
            catch
            {
            }

            Response.ClearHeaders();
            Response.ClearContent();
            Response.ContentType = "application/vnd.ms-excel";
            Response.ContentEncoding = Encoding.GetEncoding("ISO-8859-1");
            Response.AppendHeader("content-disposition", "attachment;filename=" + sFilename + ".xls");
            foreach (string strHeader in ExportFieldHeaders)
                Response.Write(strHeader + ControlChars.Tab);
            Response.Write(Microsoft.VisualBasic.Constants.vbCrLf);
            foreach (UserInfo oUser in users)
            {
                foreach (string strField in ExportFields)
                {
                    switch (strField.ToLower() ?? "")
                    {
                        case "user_userid":
                            {
                                Response.Write(oUser.UserID.ToString() + ControlChars.Tab);
                                break;
                            }

                        case "user_username":
                            {
                                Response.Write(oUser.Username + ControlChars.Tab);
                                break;
                            }

                        case "user_firstname":
                            {
                                Response.Write(oUser.FirstName + ControlChars.Tab);
                                break;
                            }

                        case "user_lastname":
                            {
                                Response.Write(oUser.LastName + ControlChars.Tab);
                                break;
                            }

                        case "user_email":
                            {
                                Response.Write(oUser.Email + ControlChars.Tab);
                                break;
                            }

                        case "user_createddate":
                            {
                                Response.Write(oUser.Membership.CreatedDate.ToShortDateString() + ControlChars.Tab);
                                break;
                            }

                        case "user_lastlogindate":
                            {
                                Response.Write(oUser.Membership.LastLoginDate.ToShortDateString() + ControlChars.Tab);
                                break;
                            }

                        case "user_displayname":
                            {
                                Response.Write(oUser.DisplayName + ControlChars.Tab);
                                break;
                            }

                        default:
                            {
                                string strValue = GetPropertyValue(strField, oUser);
                                Response.Write(strValue + ControlChars.Tab);
                                break;
                            }
                    }
                }

                Response.Write(Microsoft.VisualBasic.Constants.vbCrLf);
            }

            Response.End();
        }

        private string GetPropertyValue(string strProp, UserInfo oUser)
        {
            var prop = ProfileController.GetPropertyDefinitionByName(PortalId, strProp);
            if (prop != null)
            {
                return GetPropertyValue(prop, oUser);
            }
            else
            {
                return string.Empty;
            }
        }

        private string GetPropertyValue(ProfilePropertyDefinition objProp, UserInfo objUser)
        {
            string strValue = string.Empty;
            string strPropertyValue = string.Empty;
            if (!string.IsNullOrEmpty(objUser.Profile.GetPropertyValue(objProp.PropertyName)))
            {
                strPropertyValue = objUser.Profile.GetPropertyValue(objProp.PropertyName);
            }

            string strType = string.Empty;
            var lc = new ListController();
            var entry = lc.GetListEntryInfo(objProp.DataType);
            if (entry != null)
            {
                strType = entry.Value;
            }

            switch (strType.ToLower() ?? "")
            {
                case "truefalse":
                    {
                        if (strValue.ToLower() == "true")
                        {
                            return Localization.GetString("yes", LocalResourceFile);
                        }
                        else
                        {
                            return Localization.GetString("no", LocalResourceFile);
                        }

                        break;
                    }

                case "region":
                    {
                        string strCountry = objUser.Profile.GetPropertyValue("Country");
                        ListEntryInfo country = null;
                        var countries = new List<ListEntryInfo>();
                        countries = GetList("Country");
                        if (countries != null)
                        {
                            foreach (ListEntryInfo checkCountry in countries)
                            {
                                if ((checkCountry.Text ?? "") == (strCountry ?? ""))
                                {
                                    country = checkCountry;
                                    break;
                                }

                                if ((checkCountry.Value ?? "") == (strCountry ?? ""))
                                {
                                    country = checkCountry;
                                    break;
                                }
                            }
                        }

                        if (country != null)
                        {
                            var regions = new List<ListEntryInfo>();
                            regions = GetList("Region", country.Value);
                            if (regions != null)
                            {
                                foreach (ListEntryInfo item in regions)
                                {
                                    if ((item.Value.ToLower() ?? "") == (strPropertyValue.ToLower() ?? ""))
                                    {
                                        strValue = item.Text;
                                        break;
                                    }

                                    if ((item.Text.ToLower() ?? "") == (strPropertyValue.ToLower() ?? ""))
                                    {
                                        strValue = item.Text;
                                        break;
                                    }
                                }
                            }
                        }

                        break;
                    }

                case "country":
                    {
                        var entries = new List<ListEntryInfo>();
                        entries = GetList("Country");
                        if (entries != null)
                        {
                            foreach (ListEntryInfo item in entries)
                            {
                                if ((item.Value.ToLower() ?? "") == (strPropertyValue.ToLower() ?? ""))
                                {
                                    strValue = item.Text;
                                    break;
                                }

                                if ((item.Text.ToLower() ?? "") == (strPropertyValue.ToLower() ?? ""))
                                {
                                    strValue = item.Text;
                                    break;
                                }
                            }
                        }

                        break;
                    }

                case "list":
                    {
                        var entries = new List<ListEntryInfo>();
                        entries = GetList(objProp.PropertyName);
                        if (entries != null)
                        {
                            foreach (ListEntryInfo item in entries)
                            {
                                if ((item.Value.ToLower() ?? "") == (strPropertyValue.ToLower() ?? ""))
                                {
                                    strValue = item.Text;
                                    break;
                                }

                                if ((item.Text.ToLower() ?? "") == (strPropertyValue.ToLower() ?? ""))
                                {
                                    strValue = item.Text;
                                    break;
                                }
                            }
                        }

                        break;
                    }

                default:
                    {
                        strValue = strPropertyValue;
                        break;
                    }
            }

            return strValue.Replace(Microsoft.VisualBasic.Constants.vbCrLf, string.Empty)
                .Replace(Microsoft.VisualBasic.Constants.vbLf, string.Empty)
                .Replace(Microsoft.VisualBasic.Constants.vbNewLine, string.Empty)
                .Replace(Microsoft.VisualBasic.Constants.vbCr, string.Empty);
        }

        private List<ListEntryInfo> GetList(string strListName)
        {
            var entries = new List<ListEntryInfo>();
            var lc = new ListController();
            if (DataCache.GetCache("PROPLIST_" + strListName) != null)
            {
                entries = (List<ListEntryInfo>)DataCache.GetCache("PROPLIST_" + strListName);
            }
            else
            {
                entries = (List<ListEntryInfo>)lc.GetListEntryInfoItems(strListName);
                DataCache.SetCache("PROPLIST_" + strListName, entries);
            }

            return entries;
        }

        private List<ListEntryInfo> GetList(string strListName, string strValue)
        {
            var entries = new List<ListEntryInfo>();
            var lc = new ListController();
            if (DataCache.GetCache("PROPLIST_" + strListName) != null)
            {
                entries = (List<ListEntryInfo>)DataCache.GetCache("PROPLIST_" + strListName);
            }
            else
            {
                entries = lc.GetListEntryInfoItems(strListName, strValue).ToList();
                DataCache.SetCache("PROPLIST_" + strListName, entries);
            }

            return entries;
        }

        private void UpdateAccount()
        {
            string strMessage = string.Empty;
            bool blnUpdateUsername = false;
            bool blnUpdateDisplayname = false;
            bool blnUpdateEmail = false;
            TextBox txtUsername = (TextBox)FindControlRecursive(plhUser, plhUser.ID + "_" + Libraries.UserManagement.Constants.ControlId_Username);
            blnUpdateUsername = txtUsername != null;
            if (blnUpdateUsername)
            {
                if (!IsValidUserAttribute(Libraries.UserManagement.Constants.User_Username, plhUser))
                {
                    strMessage += "Error";
                    Control argobjControl = plhUser;
                    AddErrorIndicator(Libraries.UserManagement.Constants.User_Username, ref argobjControl);
                    plhUser = (PlaceHolder)argobjControl;
                }
                else
                {
                    Control argobjControl1 = plhUser;
                    RemoveErrorIndicator(Libraries.UserManagement.Constants.User_Username, ref argobjControl1, true);
                    plhUser = (PlaceHolder)argobjControl1;
                }
            }

            TextBox txtEmail = (TextBox)FindControlRecursive(plhUser, plhUser.ID + "_" + Libraries.UserManagement.Constants.ControlId_Email);
            blnUpdateEmail = txtEmail != null;
            if (blnUpdateEmail)
            {
                if (!IsValidUserAttribute(Libraries.UserManagement.Constants.User_Email, plhUser))
                {
                    strMessage += "Error";
                    Control argobjControl2 = plhUser;
                    AddErrorIndicator(Libraries.UserManagement.Constants.User_Email, ref argobjControl2);
                    plhUser = (PlaceHolder)argobjControl2;
                }
                else
                {
                    Control argobjControl3 = plhUser;
                    RemoveErrorIndicator(Libraries.UserManagement.Constants.User_Email, ref argobjControl3, true);
                    plhUser = (PlaceHolder)argobjControl3;
                }
            }

            TextBox txtDisplayName = (TextBox)FindControlRecursive(plhUser, plhUser.ID + "_" + Libraries.UserManagement.Constants.ControlId_Displayname);
            blnUpdateDisplayname = txtDisplayName != null;
            if (blnUpdateDisplayname)
            {
                if (!IsValidUserAttribute(Libraries.UserManagement.Constants.User_Displayname, plhUser))
                {
                    strMessage += "Error";
                    Control argobjControl4 = plhUser;
                    AddErrorIndicator(Libraries.UserManagement.Constants.User_Displayname, ref argobjControl4);
                    plhUser = (PlaceHolder)argobjControl4;
                }
                else
                {
                    Control argobjControl5 = plhUser;
                    RemoveErrorIndicator(Libraries.UserManagement.Constants.User_Displayname, ref argobjControl5, true);
                    plhUser = (PlaceHolder)argobjControl5;
                }
            }

            if (strMessage.Length > 0)
            {
                lblAccountNote.Text = Localization.GetString("FieldsRequired", LocalResourceFile);
                return;
            }

            var oUser = User;
            if (blnUpdateDisplayname)
            {
                oUser.DisplayName = txtDisplayName.Text;
            }

            if (blnUpdateEmail)
            {
                oUser.Email = txtEmail.Text;
            }

            if (blnUpdateUsername)
            {
                oUser.Username = txtUsername.Text;
            }

            UserController.UpdateUser(PortalId, oUser);
            lblAccountNote.Text = Localization.GetString("AccountDataUpdated", LocalResourceFile);
        }

        private void UpdateProfile()
        {
            var oUser = User;
            string strMessage = string.Empty;
            foreach (string itemProp in GetPropertiesFromTempate(GetTemplate(ModuleTheme, Libraries.UserManagement.Constants.TemplateName_ProfileForm, CurrentLocale, false)))
            {
                try
                {
                    var prop = ProfileController.GetPropertyDefinitionByName(PortalId, itemProp.Substring(2)); // itemprop comes in the form U:Propertyname or P:Propertyname
                    if (prop != null)
                    {
                        Control argobjControl2 = plhProfile;
                        if (!IsValidProperty(oUser, prop, ref argobjControl2))
                        {
                            strMessage += "Error";
                            Control argobjControl = plhProfile;
                            AddErrorIndicator(prop.PropertyDefinitionId.ToString(), ref argobjControl);
                            plhProfile = (PlaceHolder)argobjControl;
                        }
                        else
                        {
                            Control argobjControl1 = plhProfile;
                            RemoveErrorIndicator(prop.PropertyDefinitionId.ToString(), ref argobjControl1, prop.Required);
                            plhProfile = (PlaceHolder)argobjControl1;
                        }

                        plhProfile = (PlaceHolder)argobjControl2;
                    }
                }
                catch
                {
                }
            }

            if (strMessage.Length > 0)
            {
                lblProfileNote.Text = Localization.GetString("FieldsRequired", LocalResourceFile);
                return;
            }

            UserController.UpdateUser(PortalId, oUser);
            var propertiesCollection = new ProfilePropertyDefinitionCollection();
            Control argContainer = plhProfile;
            UpdateProfileProperties(ref argContainer, ref oUser, ref propertiesCollection,
                GetPropertiesFromTempate(GetTemplate(ModuleTheme,
                    Libraries.UserManagement.Constants.TemplateName_ProfileForm, CurrentLocale, false)));
            plhProfile = (PlaceHolder)argContainer;
            oUser = ProfileController.UpdateUserProfile(oUser, propertiesCollection);
            lblProfileNote.Text = Localization.GetString("ProfileUpdated", LocalResourceFile);
        }

        private void BindUser(int UserId)
        {
            pnlBackToList.Visible = true;
            pnlCreateAccount.Visible = false;
            pnlGrid.Visible = false;
            pnlUser.Visible = true;
            var objController = new UserController();
            var objUser = objController.GetUser(PortalId, UserId);
            if (objUser != null)
            {
                if (objUser.Membership.IsOnLine)
                {
                    lblUserOnlineStatus.Text = Localization.GetString("UserIsOnline", LocalResourceFile);
                }
                else
                {
                    lblUserOnlineStatus.Text = Localization.GetString("UserIsOffline", LocalResourceFile);
                }

                if (objUser.Membership.Approved)
                {
                    lblAuthorizedStatus.Text = Localization.GetString("UserIsApproved", LocalResourceFile);
                }
                else
                {
                    lblAuthorizedStatus.Text = Localization.GetString("UserIsUnApproved", LocalResourceFile);
                }

                if (objUser.Membership.UpdatePassword)
                {
                    lblForcePasswordChange.Text = Localization.GetString("UserMustUpdatePassword", LocalResourceFile);
                    cmdForcePasswordChange.Visible = false;
                }
                else
                {
                    lblForcePasswordChange.Text = Localization.GetString("UserMustNotUpdatePassword", LocalResourceFile);
                    cmdForcePasswordChange.Visible = true;
                }

                if (objUser.Membership.LockedOut)
                {
                    lblLockoutStatus.Text = Localization.GetString("UserIsLockedOut", LocalResourceFile);
                    cmdUnlockAccount.Visible = true;
                }
                else
                {
                    lblLockoutStatus.Text = Localization.GetString("UserIsNotLockedOut", LocalResourceFile);
                    cmdUnlockAccount.Visible = false;
                }

                lblMemberSince.Text = objUser.Membership.CreatedDate.ToShortDateString() + ", " + objUser.Membership.CreatedDate.ToShortTimeString();
                lblLastActivity.Text = objUser.Membership.LastActivityDate.ToShortDateString() + ", " + objUser.Membership.LastActivityDate.ToShortTimeString();
                lblLastLockout.Text = objUser.Membership.LastLockoutDate.ToShortDateString() + ", " + objUser.Membership.LastLockoutDate.ToShortTimeString();
                lblLastLogin.Text = objUser.Membership.LastLoginDate.ToShortDateString() + ", " + objUser.Membership.LastLoginDate.ToShortTimeString();
                lblLastPasswordChange.Text = objUser.Membership.LastPasswordChangeDate.ToShortDateString() + ", " + objUser.Membership.LastPasswordChangeDate.ToShortTimeString();
                if (string.IsNullOrEmpty(lblLastActivity.Text))
                    lblLastActivity.Text = Localization.GetString("Never", LocalResourceFile);
                if (string.IsNullOrEmpty(lblLastLockout.Text))
                    lblLastLockout.Text = Localization.GetString("Never", LocalResourceFile);
                if (string.IsNullOrEmpty(lblLastLogin.Text))
                    lblLastLogin.Text = Localization.GetString("Never", LocalResourceFile);
                if (string.IsNullOrEmpty(lblLastPasswordChange.Text))
                    lblLastPasswordChange.Text = Localization.GetString("Never", LocalResourceFile);
                cmdAuthorizeAccount.Visible = objUser.Membership.Approved == false;
                cmdDeleteAccount.Visible = objUser.UserID != PortalSettings.AdministratorId && AllowDelete && objUser.IsSuperUser == false;
                cmdRestoreAccount.Visible = objUser.IsDeleted == true;
                if (objUser.IsDeleted)
                {
                    cmdDeleteAccount.Text = Localization.GetString("HardDeleteAccount", LocalResourceFile);
                }
                else
                {
                    cmdDeleteAccount.Text = Localization.GetString("DeleteAccount", LocalResourceFile);
                }

                if (objUser.IsDeleted)
                    cmdUpdateAccount.Visible = false;
                if (objUser.IsDeleted)
                    cmdForcePasswordChange.Visible = false;
                if (UserInfo.IsSuperUser)
                {
                    BindUserSites(UserId);
                }
            }
        }

        private void BindRoleMembershipChangedNotification(string strRole, string TemplateName, DateTime EffectiveDate, DateTime ExpiryDate)
        {
            string Locale = CurrentLocale;
            if (!string.IsNullOrEmpty(User.Profile.PreferredLocale))
            {
                Locale = User.Profile.PreferredLocale;
            }

            string strTemplate = GetTemplate(ModuleTheme, TemplateName, Locale, false);
            strTemplate = strTemplate.Replace("[FIRSTNAME]", User.FirstName);
            strTemplate = strTemplate.Replace("[LASTNAME]", User.LastName);
            strTemplate = strTemplate.Replace("[DISPLAYNAME]", User.DisplayName);
            strTemplate = strTemplate.Replace("[PORTALNAME]", PortalSettings.PortalName);
            strTemplate = strTemplate.Replace("[PORTALURL]", PortalSettings.PortalAlias.HTTPAlias);
            strTemplate = strTemplate.Replace("[USERNAME]", User.Username);
            strTemplate = strTemplate.Replace("[PASSWORD]", Localization.GetString("HiddenPassword", LocalResourceFile));
            strTemplate = strTemplate.Replace("[ROLE]", strRole);
            strTemplate = strTemplate.Replace("[RECIPIENTUSERID]", User.UserID.ToString());
            strTemplate = strTemplate.Replace("[USERID]", User.UserID.ToString());
            if (EffectiveDate != Null.NullDate)
            {
                strTemplate = strTemplate.Replace("[EFFECTIVEDATE]", EffectiveDate.ToShortDateString());
            }
            else
            {
                strTemplate = strTemplate.Replace("[EFFECTIVEDATE]", DateTime.Now.ToShortDateString());
            }

            if (ExpiryDate != Null.NullDate)
            {
                strTemplate = strTemplate.Replace("[EXPIRYDATE]", ExpiryDate.ToShortDateString());
            }
            else
            {
                strTemplate = strTemplate.Replace("[EXPIRYDATE]", "-");
            }

            txtNotifyRoleBody.Content = strTemplate;
        }

        private void BindUserSites(int UserId)
        {
            var pCtrl = new PortalController();
            chkUserSites.Items.Clear();
            foreach (PortalInfo objPortal in pCtrl.GetPortals())
            {
                var cItem = new ListItem(objPortal.PortalName, objPortal.PortalID.ToString());
                cItem.Selected = false;
                if (UserController.GetUserById(objPortal.PortalID, UserId) != null)
                {
                    cItem.Selected = true;
                }

                if (objPortal.AdministratorId == UserId)
                {
                    cItem.Enabled = false;
                }

                chkUserSites.Items.Add(cItem);
            }

            chkUserSites.Items[0].Enabled = false;
        }

        private void BindUserNotification()
        {
            string Locale = CurrentLocale;
            if (!string.IsNullOrEmpty(User.Profile.PreferredLocale))
            {
                Locale = User.Profile.PreferredLocale;
            }

            string reseturl = string.Format("http://{0}/default.aspx?ctl=PasswordReset&resetToken={1}", PortalSettings.PortalAlias.HTTPAlias, User.PasswordResetToken);
            string strTemplate = GetTemplate(ModuleTheme, Libraries.UserManagement.Constants.TemplateName_EmailAccountData, Locale, false);
            strTemplate = strTemplate.Replace("[FIRSTNAME]", User.FirstName);
            strTemplate = strTemplate.Replace("[LASTNAME]", User.LastName);
            strTemplate = strTemplate.Replace("[DISPLAYNAME]", User.DisplayName);
            strTemplate = strTemplate.Replace("[PORTALNAME]", PortalSettings.PortalName);
            strTemplate = strTemplate.Replace("[PORTALURL]", PortalSettings.PortalAlias.HTTPAlias);
            strTemplate = strTemplate.Replace("[USERNAME]", User.Username);
            strTemplate = strTemplate.Replace("[PASSWORD]", Localization.GetString("HiddenPassword", LocalResourceFile));
            strTemplate = strTemplate.Replace("[RESETLINK]", "<a href=\"" + reseturl + "\">" + Localization.GetString("ClickToSet", LocalResourceFile) + "</a>");
            strTemplate = strTemplate.Replace("[RESETLINKURL]", reseturl);
            strTemplate = strTemplate.Replace("[RECIPIENTUSERID]", User.UserID.ToString());
            strTemplate = strTemplate.Replace("[USERID]", User.UserID.ToString());
            txtNotifyUserBody.Content = strTemplate;
        }

        private void InitializeForm()
        {
            rblMessagingMode.Items[0].Text = Localization.GetString("MessagingModeMessage", LocalResourceFile);
            rblMessagingMode.Items[1].Text = Localization.GetString("MessagingModeEmail", LocalResourceFile);
            lblMessagingNotes.Text = Localization.GetString("lblMessagingNotes", LocalResourceFile);
            btnMessageUsers.Text = Localization.GetString("btnMessageUsers", LocalResourceFile);
            lblMessagesSubject.Text = Localization.GetString("lblMessagesSubject", LocalResourceFile);
            lblMessagesBody.Text = Localization.GetString("lblMessagesBody", LocalResourceFile);
            btnSendMessages.Text = Localization.GetString("btnSendMessages", LocalResourceFile);
            btnCancelMessaging.Text = Localization.GetString("btnCancelMessaging", LocalResourceFile);
            cmdBulkDelete.Text = Localization.GetString("cmdBulkDelete", LocalResourceFile);
            cmdBulkRemove.Text = Localization.GetString("cmdBulkRemove", LocalResourceFile);
            cmdHardDeleteSelected.Text = Localization.GetString("cmdHardDeleteSelected", LocalResourceFile);
            var reports = new List<UserReportInfo>();
            var ctrlReports = new UserReportsController();
            reports = UserReportsController.GetReports(PortalId);
            if (reports.Count == 0)
            {
                pnlReport.Visible = false;
            }

            txtNotifyRoleBody.ToolsFile = TemplateSourceDirectory + "/Config/Toolsfile.xml";
            txtNotifyUserBody.ToolsFile = TemplateSourceDirectory + "/Config/Toolsfile.xml";
            txtNotifyPasswordBody.ToolsFile = TemplateSourceDirectory + "/Config/Toolsfile.xml";
            txtEmailBodyAll.ToolsFile = TemplateSourceDirectory + "/Config/Toolsfile.xml";
            txtNotifyRoleSubject.Text = string.Format(Localization.GetString("txtNotifyRoleSubject", LocalResourceFile), PortalSettings.PortalName);
            txtNotifyUserSubject.Text = string.Format(Localization.GetString("txtNotifyUserSubject", LocalResourceFile), PortalSettings.PortalName);
            txtNotifyPasswordSubject.Text = string.Format(Localization.GetString("txtNotifyPasswordSubject", LocalResourceFile), PortalSettings.PortalName);
            BindUserNotification();
            lblPasswordNote.Text = Localization.GetString("lblPasswordNote", LocalResourceFile);
            lblAccountNote.Text = Localization.GetString("lblAccountNote", LocalResourceFile);
            lblProfileNote.Text = Localization.GetString("lblProfileNote", LocalResourceFile);
            lblRolesNote.Text = Localization.GetString("lblRolesNote", LocalResourceFile);
            lblEmailNote.Text = Localization.GetString("lblEmailNote", LocalResourceFile);
            lblMessageNote.Text = Localization.GetString("lblMessageNote", LocalResourceFile);
            lblSitesNote.Text = Localization.GetString("lblSitesNote", LocalResourceFile);
            tabAccount.Visible = false;
            pnlAccountTab.Visible = false;
            tabPassword.Visible = false;
            pnlPasswordTab.Visible = false;
            tabProfile.Visible = false;
            pnlProfileTab.Visible = false;
            tabRoles.Visible = false;
            pnlRolesTab.Visible = false;
            tabEmail.Visible = false;
            pnlEmailTab.Visible = false;
            tabMessage.Visible = false;
            pnlMessageTab.Visible = false;
            tabSites.Visible = false;
            pnlSitesTab.Visible = false;
            foreach (string strTab in ShowUserDetailTabs)
            {
                if (strTab.ToLower() == "account")
                {
                    tabAccount.Visible = true;
                    pnlAccountTab.Visible = true;
                }

                if (strTab.ToLower() == "password")
                {
                    tabPassword.Visible = true;
                    pnlPasswordTab.Visible = true;
                }

                if (strTab.ToLower() == "profile")
                {
                    tabProfile.Visible = true;
                    pnlProfileTab.Visible = true;
                }

                if (strTab.ToLower() == "roles")
                {
                    tabRoles.Visible = true;
                    pnlRolesTab.Visible = true;
                }

                if (strTab.ToLower() == "email")
                {
                    tabEmail.Visible = true;
                    pnlEmailTab.Visible = true;
                }

                if (strTab.ToLower() == "message")
                {
                    tabMessage.Visible = true;
                    pnlMessageTab.Visible = true;
                }

                if (strTab.ToLower() == "sites")
                {
                    tabSites.Visible = true;
                    pnlSitesTab.Visible = true;
                }
            }

            if (AdditionalControls.Length > 0)
            {
                string strTabname = "";
                string strControl = "";
                foreach (string objControl in AdditionalControls)
                {
                    try
                    {
                        strTabname = objControl.Split(char.Parse(","))[0];
                        strControl = objControl.Split(char.Parse(","))[1];
                    }
                    catch
                    {
                    }

                    if (strTabname.Length > 0 && strControl.Length > 0)
                    {
                        string relUrl = ResolveUrl("~" + strControl);
                        string path = Server.MapPath(relUrl);
                        if (System.IO.File.Exists(path) && path.EndsWith(".ascx"))
                        {

                            // ok, we've got a tabname and a valid control to load
                            PortalModuleBase objModule = (PortalModuleBase)LoadControl(relUrl);
                            objModule.ModuleConfiguration = ModuleConfiguration;
                            objModule.ID = System.IO.Path.GetFileNameWithoutExtension(relUrl);
                            string strTabLiteral = "<li id=\"" + strTabname.Replace(" ", "") + "\">";
                            var objPanel = new Panel();
                            objPanel.ID = "pnl_" + objModule.ModuleId.ToString();
                            objPanel.Controls.Add(objModule);
                            plhAdditonalControls.Controls.Add(objPanel);
                            strTabLiteral += "<a href=\"#" + objPanel.ClientID + "\">";
                            strTabLiteral += strTabname;
                            strTabLiteral += "</a></li>";
                            plhAdditionalTabs.Controls.Add(new LiteralControl(strTabLiteral));
                        }
                    }
                }
            }

            if (DotNetNuke.Security.Membership.MembershipProvider.Instance().RequiresQuestionAndAnswer)
            {
                tabPassword.Visible = false;
                pnlPasswordTab.Visible = false;
            }

            if (DotNetNuke.Security.Membership.MembershipProvider.Instance().PasswordRetrievalEnabled == false)
            {
                pnlPassword_Step1.Visible = false;
                pnlResetButton.Visible = true;
                lblPasswordNote.Text = Localization.GetString("ResetPasswordNote", LocalResourceFile);
            }

            pnlCreateAccount.Visible = AllowCreate;
            pnlExport.Visible = AllowExport;
            pnlMessageUsers.Visible = AllowMessageUsers;
            btnMessageUsers.NavigateUrl = DotNetNuke.Common.Globals.NavigateURL(TabId, string.Empty, "Action=Messaging", "RoleId=" + Request.QueryString["RoleId"]);
            btnNotifyPassword.Text = Localization.GetString("btnNotifyPassword", LocalResourceFile);
            btnNotifyPasswordSkip.Text = Localization.GetString("btnNotifyPasswordSkip", LocalResourceFile);
            btnNotifyRoleSkip.Text = Localization.GetString("btnNotifyRoleSkip", LocalResourceFile);
            btnNotifyRole.Text = Localization.GetString("btnNotifyRole", LocalResourceFile);
            btnNotifyUser.Text = Localization.GetString("btnNotifyUser", LocalResourceFile);
            btnAddToRole.Text = Localization.GetString("btnAddToRole", LocalResourceFile);
            btnExport.Text = Localization.GetString("btnExport", LocalResourceFile);
            btnHardDelete.Text = Localization.GetString("btnHardDelete", LocalResourceFile);
            btnReport.Text = Localization.GetString("btnReport", LocalResourceFile);
            btnSearch.Text = Localization.GetString("btnSearch", LocalResourceFile);
            btnApplyOptions.Text = Localization.GetString("btnApplyOptions", LocalResourceFile);
            lblGridTab.Text = Localization.GetString("lblGridTab", LocalResourceFile);
            lblPreferencesTab.Text = Localization.GetString("lblPreferencesTab", LocalResourceFile);
            lblGridSetup.Text = Localization.GetString("lblGridSetup", LocalResourceFile);
            lblPageSize.Text = Localization.GetString("lblPageSize", LocalResourceFile);
            lblSearchOptions.Text = Localization.GetString("lblSearchOptions", LocalResourceFile);
            foreach (ListItem item in chkGridOptions.Items)
            {
                string strText = string.Empty;
                strText = Localization.GetString("ProfileProperties_" + item.Value + ".Text", ProfileResourcefile);
                if (string.IsNullOrEmpty(strText) || strText.StartsWith("RESX:"))
                {
                    item.Text = Localization.GetString(item.Value, LocalResourceFile);
                }
                else
                {
                    item.Text = strText.Replace(":", string.Empty);
                }
            }
        }

        private void BindSearchColOptions()
        {
            chkSearchCols.Items.Clear();
            chkSearchCols.Items.Add(new ListItem("Username", "Username"));
            chkSearchCols.Items.Add(new ListItem("DisplayName", "DisplayName"));
            chkSearchCols.Items.Add(new ListItem("Email", "Email"));
            var props = ProfileController.GetPropertyDefinitionsByPortal(PortalSettings.PortalId);
            foreach (ProfilePropertyDefinition prop in props)
            {
                try
                {
                    chkSearchCols.Items.Add(new ListItem(LocalizeProperty(prop).Replace(":", string.Empty), prop.PropertyName));
                }
                catch (Exception ex)
                {
                    chkSearchCols.Items.Add(new ListItem(prop.PropertyName, prop.PropertyName));
                }
            }
        }

        private void PersonalizeOptions()
        {
            foreach (ListItem item in chkSearchCols.Items)
                item.Selected = false;
            foreach (ListItem item in chkGridOptions.Items)
                item.Selected = false;
            string strSearchCols = "FirstName,LastName,City,Email,";
            var searchcols = DotNetNuke.Services.Personalization.Personalization.GetProfile("dnnWerk_Users_ColOptions", "SearchCols_" + UserId.ToString());
            if (searchcols != null)
            {
                if (searchcols.ToString().Length > 0)
                {
                    strSearchCols = searchcols.ToString();
                }
            }

            foreach (string strSearchCol in strSearchCols.Split(char.Parse(",")))
            {
                if (strSearchCol.Length > 0)
                {
                    foreach (ListItem item in chkSearchCols.Items)
                    {
                        if ((item.Value.ToLower() ?? "") == (strSearchCol.ToLower() ?? ""))
                        {
                            item.Selected = true;
                        }
                    }
                }
            }

            string strGridCols = "UserId,DisplayName,Username,Email,Country,CreatedDate";
            var gridcols = DotNetNuke.Services.Personalization.Personalization.GetProfile("dnnWerk_Users_ColOptions", "GridCols_" + UserId.ToString());
            if (gridcols != null)
            {
                if (gridcols.ToString().Length > 0)
                {
                    strGridCols = gridcols.ToString();
                }
            }

            foreach (string strGridCol in strGridCols.Split(char.Parse(",")))
            {
                if (strGridCol.Length > 0)
                {
                    foreach (ListItem item in chkGridOptions.Items)
                    {
                        if ((item.Value.ToLower() ?? "") == (strGridCol.ToLower() ?? ""))
                        {
                            item.Selected = true;
                        }
                    }
                }
            }

            string pagesize = "25";
            try
            {
                pagesize = Conversions.ToString(DotNetNuke.Services.Personalization.Personalization.GetProfile("dnnWerk_Users_ColOptions", "GridPageSize_" + UserId.ToString()));
            }
            catch
            {
            }

            try
            {
                drpPageSize.Items.FindByText(pagesize.ToString()).Selected = true;
            }
            catch
            {
            }

            // set up grid
            foreach (string strGridCol in strGridCols.Split(char.Parse(",")))
            {
                if (strGridCol.ToString().Length > 0)
                {
                    string strCol = strGridCol;
                    try
                    {
                        grdUsers.Columns.FindByDataField(strCol).Visible = true;
                    }
                    catch
                    {
                    }
                }
            }

            if (drpPageSize.SelectedItem.Value == "All")
            {
                grdUsers.AllowPaging = false;
            }
            else
            {
                grdUsers.AllowPaging = true;
                grdUsers.PageSize = Convert.ToInt32(drpPageSize.SelectedItem.Value);
            }

            if (ctlRoles.SelectedNode != null)
            {
                try
                {
                    if (ctlRoles.SelectedNode.Value == "-2" | (ctlRoles.SelectedNode.Value ?? "") == (PortalSettings.RegisteredRoleId.ToString() ?? ""))
                    {
                        grdUsers.Columns.FindByDataField("Status").Visible = false;
                    }
                }
                catch
                {
                }
            }
        }

        private void SaveSearchOptions()
        {
            try
            {
                string strCols = string.Empty;
                foreach (ListItem item in chkSearchCols.Items)
                {
                    if (item.Selected == true)
                    {
                        strCols += item.Value + ",";
                    }
                }

                DotNetNuke.Services.Personalization.Personalization.SetProfile("dnnWerk_Users_ColOptions", "SearchCols_" + UserId.ToString(), strCols);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message, ex);
                throw ex;
            }
        }

        private void SaveGridOptions()
        {
            string strCols = string.Empty;
            foreach (ListItem item in chkGridOptions.Items)
            {
                if (item.Selected == true)
                {
                    strCols += item.Value + ",";
                }
            }

            DotNetNuke.Services.Personalization.Personalization.SetProfile("dnnWerk_Users_ColOptions", "GridCols_" + UserId.ToString(), strCols);
            DotNetNuke.Services.Personalization.Personalization.SetProfile("dnnWerk_Users_ColOptions", "GridPageSize_" + UserId.ToString(), drpPageSize.SelectedItem.Value);
        }

        private void BindRoles()
        {
            var objRoleController = new RoleController();
            var roles = objRoleController.GetPortalRoles(PortalId);
            var groups = RoleController.GetRoleGroups(PortalId);
            if (groups.Count > 0)
            {
                // we have some role groups, add those first
                foreach (RoleGroupInfo objGroup in groups)
                {
                    var groupnode = new DnnTreeNode();
                    groupnode.Text = objGroup.RoleGroupName;
                    groupnode.Value = "-1";
                    groupnode.Attributes.Add("IsGroup", Conversions.ToString(true));
                    groupnode.ImageUrl = TemplateSourceDirectory + "/images/folder.png";
                    groupnode.PostBack = false;
                    var groupItem = new DnnComboBoxItem();
                    groupItem.Text = objGroup.RoleGroupName;
                    groupItem.Value = "-1";
                    groupItem.ImageUrl = TemplateSourceDirectory + "/images/folder.png";
                    groupItem.Attributes.Add("IsGroup", Conversions.ToString(true));
                    groupItem.IsSeparator = true;
                    drpRoles.Items.Add(groupItem);
                    foreach (RoleInfo objRole in roles)
                    {
                        if (objRole.RoleGroupID == objGroup.RoleGroupID)
                        {
                            if (AllowedRoles != null && Array.IndexOf(AllowedRoles, objRole.RoleID.ToString()) > -1 | Array.IndexOf(AllowedRoles, "all") > -1 || AllowedRoles is null)
                            {
                                var rolenode = new DnnTreeNode();
                                rolenode.Text = objRole.RoleName;
                                rolenode.Value = objRole.RoleID.ToString();
                                rolenode.Text = objRole.RoleName;
                                rolenode.Value = objRole.RoleID.ToString();
                                rolenode.ImageUrl = TemplateSourceDirectory + "/images/users.png";
                                rolenode.Attributes.Add("IsGroup", Conversions.ToString(false));
                                rolenode.NavigateUrl = DotNetNuke.Common.Globals.NavigateURL(TabId, string.Empty, "RoleId=" + objRole.RoleID.ToString());
                                groupnode.Nodes.Add(rolenode);
                                var roleItem = new DnnComboBoxItem();
                                roleItem.Text = objRole.RoleName;
                                roleItem.Value = objRole.RoleID.ToString();
                                roleItem.ImageUrl = TemplateSourceDirectory + "/images/users.png";
                                roleItem.Attributes.Add("style", "margin-left: 20px;");
                                drpRoles.Items.Add(roleItem);
                            }
                        }
                    }

                    if (groupnode.Nodes.Count > 0)
                    {
                        ctlRoles.Nodes.Add(groupnode);
                    }
                }

                foreach (RoleInfo objRole in roles)
                {
                    if (objRole.RoleGroupID == Null.NullInteger)
                    {
                        if (AllowedRoles != null && Array.IndexOf(AllowedRoles, objRole.RoleID.ToString()) > -1 | Array.IndexOf(AllowedRoles, "all") > -1 || AllowedRoles is null)
                        {
                            var rolenode = new DnnTreeNode
                            {
                                Text = objRole.RoleName, 
                                Value = objRole.RoleID.ToString()
                            };
                            rolenode.Attributes.Add("IsGroup", Conversions.ToString(false));
                            rolenode.ImageUrl = TemplateSourceDirectory + "/images/users.png";
                            rolenode.NavigateUrl = DotNetNuke.Common.Globals.NavigateURL(TabId, string.Empty, "RoleId=" + objRole.RoleID.ToString());
                            ctlRoles.Nodes.Add(rolenode);
                            if (objRole.RoleID != PortalSettings.RegisteredRoleId)
                            {
                                var roleItem = new DnnComboBoxItem();
                                roleItem.Text = objRole.RoleName;
                                roleItem.Value = objRole.RoleID.ToString();
                                roleItem.ImageUrl = TemplateSourceDirectory + "/images/users.png";
                                drpRoles.Items.Add(roleItem);
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (RoleInfo objRole in roles)
                {
                    if (AllowedRoles != null && Array.IndexOf(AllowedRoles, objRole.RoleID.ToString()) > -1 | Array.IndexOf(AllowedRoles, "all") > -1 || AllowedRoles is null)
                    {
                        var rolenode = new DnnTreeNode
                        {
                            Text = objRole.RoleName,
                            Value = objRole.RoleID.ToString(),
                            ImageUrl = TemplateSourceDirectory + "/images/users.png"
                        };
                        rolenode.Attributes.Add("IsGroup", Conversions.ToString(false));
                        rolenode.NavigateUrl = DotNetNuke.Common.Globals.NavigateURL(TabId, string.Empty, "RoleId=" + objRole.RoleID.ToString());
                        ctlRoles.Nodes.Add(rolenode);
                        if (objRole.RoleID != PortalSettings.RegisteredRoleId)
                        {
                            var roleItem = new RadComboBoxItem
                            {
                                Text = objRole.RoleName,
                                Value = objRole.RoleID.ToString(),
                                ImageUrl = TemplateSourceDirectory + "/images/users.png"
                            };
                            drpRoles.Items.Add(roleItem);
                        }
                    }
                }
            }

            if (AllowedRoles != null && Array.IndexOf(AllowedRoles, "-2") > -1 | Array.IndexOf(AllowedRoles, "all") > -1 || AllowedRoles is null)
            {
                var binnode = new DnnTreeNode();
                binnode.Text = "Deleted Users";
                binnode.Value = "-2";
                binnode.ImageUrl = "~/images/action_delete.gif";
                binnode.Attributes.Add("IsGroup", Conversions.ToString(false));
                binnode.NavigateUrl = DotNetNuke.Common.Globals.NavigateURL(TabId, string.Empty, "RoleId=-2");
                ctlRoles.Nodes.Add(binnode);
            }

            // unauthorized
            var unAuthNode = new DnnTreeNode();
            unAuthNode.Text = "Unauthorized users";
            unAuthNode.Value = "-3";
            unAuthNode.ImageUrl = TemplateSourceDirectory + "/images/users.png";
            unAuthNode.Attributes.Add("IsGroup", Conversions.ToString(false));
            unAuthNode.NavigateUrl = DotNetNuke.Common.Globals.NavigateURL(TabId, string.Empty, "RoleId=-3");
            ctlRoles.Nodes.Add(unAuthNode);
            string SelectedRole = string.Empty;
            if (Request.QueryString["RoleId"] != null)
            {
                SelectedRole = Request.QueryString["RoleId"];
            }
            else if (Request.QueryString["RoleId"] is null && Request.QueryString["ReportsResult"] == "true")
            {
            }
            else if (PreSelectRole != Null.NullInteger)
            {
                SelectedRole = PreSelectRole.ToString();
            }
            else
            {
                SelectedRole = PortalSettings.RegisteredRoleId.ToString();
            }

            try
            {
                ctlRoles.UnselectAllNodes();
                ctlRoles.FindNodeByValue(SelectedRole).Selected = true;
                ctlRoles.FindNodeByValue(SelectedRole).ExpandParentNodes();
            }
            catch
            {
            }

            if (Convert.ToInt32(SelectedRole) == PortalSettings.RegisteredRoleId)
            {
                cmdBulkRemove.Visible = false;
                cmdHardDeleteSelected.Visible = false;
            }

            if (Convert.ToInt32(SelectedRole) == -2)
            {
                cmdBulkRemove.Visible = false;
                cmdBulkDelete.Visible = false;
                cmdHardDeleteSelected.Visible = true;
            }

            if (!AllowDelete)
            {
                cmdBulkDelete.Visible = false;
            }

            if (!AllowHardDelete)
            {
                cmdHardDeleteSelected.Visible = false;
            }
        }

        private void BindUsers()
        {
            if (ctlRoles.SelectedNode is null)
            {
                _IsReportResult = Page.IsPostBack | Request.QueryString["ReportsResult"] == "true";
            }
            else
            {
                _IsReportResult = false;
            }

            if (_IsReportResult)
            {
                string strError = string.Empty;
                grdUsers.DataSource = GetReportResult(ref strError);
                if (strError.Length > 0)
                {
                    grdUsers.MasterTableView.NoMasterRecordsText = "<p style='padding:10px;'>" + strError + "</p>";
                }
                else
                {
                    grdUsers.MasterTableView.NoMasterRecordsText = "<p style='padding:10px;'>The report does not return any data.</p>";
                }
            }
            else
            {
                EnsureSafeSearchText();
                if (txtSearch.Text.Length == 0)
                {
                    grdUsers.MasterTableView.NoMasterRecordsText = "<p style='padding:10px;'>" + Localization.GetString("NoUsersFoundInRole", LocalResourceFile) + "</p>";
                }
                else
                {
                    grdUsers.MasterTableView.NoMasterRecordsText = "<p style='padding:10px;'>" + Localization.GetString("NoResultsFoundInRole", LocalResourceFile) + "</p>";
                }

                grdUsers.DataSource = GetUserList();
            }
        }

        private void EnsureSafeSearchText()
        {
            var security = new PortalSecurity();
            string searchText = security.InputFilter(txtSearch.Text.Trim(), PortalSecurity.FilterFlag.NoMarkup);
            txtSearch.Text = searchText;
        }

        private DataSet GetUserList()
        {
            int intRole = PortalSettings.RegisteredRoleId;
            string strSearch = Null.NullString;
            bool blnUseCache = true;
            if (ctlRoles.SelectedNode != null)
            {
                if (Conversions.ToBoolean(ctlRoles.SelectedNode.Attributes["IsGroup"]) == false)
                {
                    try
                    {
                        intRole = Convert.ToInt32(ctlRoles.SelectedNode.Value);
                    }
                    catch
                    {
                    }
                }
            }

            cmdBulkRemove.Visible = intRole != -2 && intRole != PortalSettings.RegisteredRoleId;
            cmdHardDeleteSelected.Visible = intRole == -2 && AllowHardDelete;
            EnsureSafeSearchText();
            if (txtSearch.Text.Length > 0)
            {
                strSearch = txtSearch.Text;
                blnUseCache = false;
            }

            DataSet ds = null;
            IDataReader dr = null;
            if (blnUseCache)
            {
                ds = GetCachedUserList(intRole);
            }

            if (ds is null)
            {
                ds = new DataSet();
                var dt = new DataTable();
                if (string.IsNullOrEmpty(strSearch))
                {
                    if (intRole == PortalSettings.RegisteredRoleId)
                    {
                        dr = DataProvider.Instance().ExecuteReader("Connect_Accounts_GetRegisteredUsers", intRole, PortalId);
                    }
                    else if (intRole == -1)
                    {
                        dr = DataProvider.Instance().ExecuteReader("Connect_Accounts_GetSuperUsers", (object)intRole);
                    }
                    else if (intRole == -2)
                    {
                        dr = DataProvider.Instance().ExecuteReader("Connect_Accounts_GetDeletedAccounts", intRole, PortalId);
                    }
                    else if (intRole == -3)
                    {
                        dr = DataProvider.Instance().ExecuteReader("Connect_Accounts_GetUnAuthAccounts", intRole, PortalId);
                    }
                    else
                    {
                        dr = DataProvider.Instance().ExecuteReader("Connect_Accounts_GetRoleMembers", intRole, PortalId);
                    }

                    dt.Load(dr);
                }
                else
                {
                    string strCols = string.Empty;
                    foreach (ListItem item in chkSearchCols.Items)
                    {
                        if (item.Selected == true)
                        {
                            strCols += item.Value + ",";
                        }
                    }

                    if (intRole == PortalSettings.RegisteredRoleId)
                    {
                        dr = DataProvider.Instance().ExecuteReader("Connect_Accounts_SearchRegisteredUsers", intRole, PortalId, strSearch, strCols);
                    }
                    else if (intRole == -2)
                    {
                        dr = DataProvider.Instance().ExecuteReader("Connect_Accounts_SearchDeletedUsers", intRole, PortalId, strSearch, strCols);
                    }
                    else
                    {
                        dr = DataProvider.Instance().ExecuteReader("Connect_Accounts_SearchRoleMembers", intRole, strSearch, strCols);
                    }

                    dt.Load(dr);
                }

                ds.Tables.Add(dt);
                if (string.IsNullOrEmpty(strSearch))
                {
                    CacheUserList(intRole, ds);
                }
            }

            btnHardDelete.Visible = intRole == -2 && ds.Tables[0].Rows.Count > 0 && AllowHardDelete;
            return ds;
        }

        private DataSet GetCachedUserList(int RoleId)
        {
            DataSet ds = null;
            if (DataCache.GetCache("DNNWERK_USERLIST_ROLEID" + RoleId.ToString()) != null)
            {
                return (DataSet)DataCache.GetCache("DNNWERK_USERLIST_ROLEID" + RoleId.ToString());
            }

            return ds;
        }

        private void CacheUserList(int RoleId, DataSet ds)
        {
            if (DataCache.GetCache("DNNWERK_USERLIST_ROLEID" + RoleId.ToString()) != null)
            {
                DataCache.RemoveCache("DNNWERK_USERLIST_ROLEID" + RoleId.ToString());
            }

            DataCache.SetCache("DNNWERK_USERLIST_ROLEID" + RoleId.ToString(), ds);
        }

        private void ClearCache()
        {
            var objRoleController = new RoleController();
            var roles = objRoleController.GetPortalRoles(PortalId);
            foreach (RoleInfo role in roles)
            {
                if (DataCache.GetCache("DNNWERK_USERLIST_ROLEID" + role.RoleID.ToString()) != null)
                {
                    DataCache.RemoveCache("DNNWERK_USERLIST_ROLEID" + role.RoleID.ToString());
                }
            }

            DataCache.RemoveCache("DNNWERK_USERLIST_ROLEID-2");
        }

        private void BindReports()
        {
            drpReports.Items.Clear();
            var reports = new List<UserReportInfo>();
            reports = UserReportsController.GetReports(PortalId);
            foreach (UserReportInfo report in reports)
                drpReports.Items.Add(new ListItem(report.FriendlyName, report.ReportId.ToString()));
            try
            {
                drpReports.SelectedValue = Conversions.ToString(Session["UserReportsId"]);
            }
            catch
            {
            }
        }

        private DataSet GetReportResult(ref string strError)
        {
            var ds = new DataSet();
            var userTable = new DataTable("UsersTable");
            ds.Tables.Add(userTable);
            try
            {
                string sql = GetSQL(Convert.ToInt32(drpReports.SelectedValue));
                if (sql != null)
                {
                    if (sql.Length > 0)
                    {
                        try
                        {
                            var dr = DataProvider.Instance().ExecuteSQL(sql);
                            ds.Load(dr, LoadOption.OverwriteChanges, userTable);
                        }
                        catch (Exception ex)
                        {
                            // error in sql syntax (most likely)
                            strError = "Your query contains errors, please check the report settings!";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }

            return ds;
        }

        private string GetSQL(int ReportId)
        {
            string strSql = string.Empty;
            strSql = UserReportsController.GetReport(ReportId).Sql;
            return strSql.Replace("@PortalID", PortalSettings.PortalId.ToString());
        }

        private void LogError(Exception ex)
        {
            if (ex != null)
            {
                Logger.Error(ex.Message, ex);
                if (ex.InnerException != null)
                {
                    LogError(ex.InnerException);
                }
            }
        }

        public DotNetNuke.Entities.Modules.Actions.ModuleActionCollection ModuleActions
        {
            get
            {
                var Actions = new DotNetNuke.Entities.Modules.Actions.ModuleActionCollection();
                Actions.Add(GetNextActionID(), Localization.GetString("ManageTemplates.Action", LocalResourceFile),
                    DotNetNuke.Entities.Modules.Actions.ModuleActionType.AddContent, string.Empty, string.Empty,
                    EditUrl("ManageTemplates"), false, SecurityAccessLevel.Edit, true, false);
                Actions.Add(GetNextActionID(), Localization.GetString("Reports.Action", LocalResourceFile),
                    DotNetNuke.Entities.Modules.Actions.ModuleActionType.AddContent, string.Empty, string.Empty,
                    EditUrl("Reports"), false, SecurityAccessLevel.Edit, true, false);
                return Actions;
            }
        }
    }
}