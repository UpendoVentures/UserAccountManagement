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
using DotNetNuke.Entities.Profile;
using DotNetNuke.Security.Roles;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.Localization;
using Microsoft.VisualBasic.CompilerServices;

namespace Connect.Modules.UserManagement.AccountManagement
{
    public partial class Settings : DotNetNuke.Entities.Modules.ModuleSettingsBase
    {
        public override void LoadSettings()
        {
            try
            {
                if (Page.IsPostBack == false)
                {
                    LocalizeForm();
                    BindAllowedRoles();
                    BindPreselectedRole();
                    if (Settings.Contains("AllowReports"))
                        chkAllowReports.Checked = Conversions.ToBoolean(Settings["AllowReports"]);
                    if (Settings.Contains("AllowCreate"))
                        chkAllowCreate.Checked = Conversions.ToBoolean(Settings["AllowCreate"]);
                    if (Settings.Contains("AllowDelete"))
                        chkAllowDelete.Checked = Conversions.ToBoolean(Settings["AllowDelete"]);
                    if (Settings.Contains("AllowHardDelete"))
                        chkAllowHardDelete.Checked = Conversions.ToBoolean(Settings["AllowHardDelete"]);
                    if (Settings.Contains("AllowExport"))
                    {
                        bool blnAllowExport = Conversions.ToBoolean(Settings["AllowExport"]);
                        if (blnAllowExport)
                        {
                            chkAllowExport.Checked = true;
                            dvExportFields.Visible = true;
                            BindExportFields();
                        }
                        else
                        {
                            chkAllowExport.Checked = false;
                            dvExportFields.Visible = false;
                        }
                    }

                    if (Settings.Contains("AllowMessageUsers"))
                        chkAllowSendMessages.Checked = Conversions.ToBoolean(Settings["AllowMessageUsers"]);
                    if (Settings.Contains("ShowUserDetailTabs"))
                    {
                        foreach (string detailtab in Conversions.ToString(Settings["ShowUserDetailTabs"]).Split(char.Parse(",")))
                        {
                            foreach (ListItem item in chkUserTabs.Items)
                            {
                                if ((item.Value.ToLower() ?? "") == (detailtab.ToLower() ?? ""))
                                {
                                    item.Selected = true;
                                }
                            }
                        }
                    }

                    if (Settings.Contains("AdditionalControls"))
                        txtAditionalControls.Text = Conversions.ToString(Settings["AdditionalControls"]);
                }
            }
            catch (Exception exc)           // Module failed to load
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        public override void UpdateSettings()
        {
            try
            {
                var objModules = new DotNetNuke.Entities.Modules.ModuleController();
                objModules.UpdateTabModuleSetting(TabModuleId, "AllowReports", chkAllowReports.Checked.ToString());
                objModules.UpdateTabModuleSetting(TabModuleId, "AllowCreate", chkAllowCreate.Checked.ToString());
                objModules.UpdateTabModuleSetting(TabModuleId, "AllowDelete", chkAllowDelete.Checked.ToString());
                objModules.UpdateTabModuleSetting(TabModuleId, "AllowHardDelete", chkAllowHardDelete.Checked.ToString());
                objModules.UpdateTabModuleSetting(TabModuleId, "AllowExport", chkAllowExport.Checked.ToString());
                if (chkAllowExport.Checked)
                {
                    objModules.UpdateTabModuleSetting(TabModuleId, "ExportFields", txtExportFields.Text);
                }
                else
                {
                    objModules.DeleteTabModuleSetting(TabModuleId, "ExportFields");
                }

                objModules.UpdateTabModuleSetting(TabModuleId, "AllowMessageUsers", chkAllowSendMessages.Checked.ToString());
                objModules.UpdateTabModuleSetting(TabModuleId, "AllowReports", chkAllowReports.Checked.ToString());
                string strAllowedRoles = string.Empty;
                foreach (ListItem item in chkAllowedRoles.Items)
                {
                    if (item.Selected)
                    {
                        strAllowedRoles += item.Value + ";";
                    }
                }

                if (strAllowedRoles.Length == 0)
                {
                    strAllowedRoles = "all;";
                }

                objModules.UpdateTabModuleSetting(TabModuleId, "AllowedRoles", strAllowedRoles);
                if (string.IsNullOrEmpty(drpPreselectRole.SelectedValue))
                {
                    objModules.UpdateTabModuleSetting(TabModuleId, "PreSelectRole", PortalSettings.RegisteredRoleId.ToString());
                }
                else
                {
                    objModules.UpdateTabModuleSetting(TabModuleId, "PreSelectRole", drpPreselectRole.SelectedValue);
                }

                string strShowUserDetailTabs = string.Empty;
                foreach (ListItem item in chkUserTabs.Items)
                {
                    if (item.Selected)
                    {
                        strShowUserDetailTabs += item.Value + ",";
                    }
                }

                objModules.UpdateTabModuleSetting(TabModuleId, "ShowUserDetailTabs", strShowUserDetailTabs);
                objModules.UpdateTabModuleSetting(TabModuleId, "AdditionalControls", txtAditionalControls.Text);
            }
            catch (Exception exc)           // Module failed to load
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        private void BindAllowedRoles()
        {
            var rc = new RoleController();
            var roles = rc.GetPortalRoles(PortalId);
            chkAllowedRoles.DataSource = roles;
            chkAllowedRoles.DataBind();
            chkAllowedRoles.Items.Add(new ListItem(Localization.GetString("DeletedAccounts", LocalResourceFile), "-2"));
            chkAllowedRoles.Items.Add(new ListItem(Localization.GetString("AllRoles", LocalResourceFile), "all"));
            if (Settings.Contains("AllowedRoles"))
            {
                foreach (string allowedrole in Conversions.ToString(Settings["AllowedRoles"]).Split(char.Parse(";")))
                {
                    foreach (ListItem item in chkAllowedRoles.Items)
                    {
                        if ((item.Value.ToLower() ?? "") == (allowedrole.ToLower() ?? ""))
                        {
                            item.Selected = true;
                        }
                    }
                }
            }

            if (chkAllowedRoles.SelectedIndex == -1)
            {
                chkAllowedRoles.Items.FindByValue("all").Selected = true;
            }
        }

        private void BindPreselectedRole()
        {
            var rc = new RoleController();
            var roles = rc.GetPortalRoles(PortalId);
            var preselectRoles = new List<RoleInfo>();
            if (chkAllowedRoles.Items.FindByValue("all").Selected)
            {
                foreach (RoleInfo role in roles)
                    preselectRoles.Add(role);
            }
            else
            {
                foreach (RoleInfo role in roles)
                {
                    if (chkAllowedRoles.Items.FindByValue(role.RoleID.ToString()).Selected)
                    {
                        preselectRoles.Add(role);
                    }
                }
            }

            drpPreselectRole.DataSource = preselectRoles;
            drpPreselectRole.DataBind();

            if (chkAllowedRoles.Items.FindByValue("all").Selected | chkAllowedRoles.Items.FindByValue("-2").Selected)
            {
                drpPreselectRole.Items.Add(new ListItem(Localization.GetString("DeletedAccounts", LocalResourceFile), "-2"));
            }

            if (Settings.Contains("PreSelectRole"))
            {
                try
                {
                    drpPreselectRole.SelectedValue = Conversions.ToString(Settings["PreSelectRole"]);
                }
                catch
                {
                }
            }
        }

        private void BindExportFields()
        {
            string strExportFields = "";
            if (Settings.Contains("ExportFields"))
            {
                strExportFields = Convert.ToString(Settings["ExportFields"]);
            }

            if (string.IsNullOrEmpty(strExportFields.Trim()))
            {
                strExportFields = "User_UserId,User_Username,User_Firstname,User_Lastname,User_Email,User_CreatedDate,User_LastLoginDate,";
                var props = ProfileController.GetPropertyDefinitionsByPortal(PortalSettings.PortalId);
                foreach (ProfilePropertyDefinition prop in props)
                    strExportFields += prop.PropertyName + ",";
            }

            txtExportFields.Text = strExportFields;
        }

        private void LocalizeForm()
        {
            foreach (ListItem item in chkUserTabs.Items)
                item.Text = Localization.GetString("tab" + item.Value, LocalResourceFile);
        }

        protected void chkAllowExport_CheckedChanged(object sender, EventArgs e)
        {
            dvExportFields.Visible = chkAllowExport.Checked;
        }

        protected void chkAllowedRoles_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindPreselectedRole();
        }
    }
}