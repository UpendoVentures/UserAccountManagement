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
using System.Web.UI;
using System.Web.UI.WebControls;
using Connect.Libraries.UserManagement;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Framework.JavaScriptLibraries;
using DotNetNuke.Services.Localization;
using DotNetNuke.UI.Skins.Controls;
using Microsoft.VisualBasic.CompilerServices;

namespace Connect.Modules.UserManagement.AccountManagement
{
    public partial class Templates : ConnectUsersModuleBase
    {
        public Templates()
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
            LocalizeForm();
            if (!Page.IsPostBack)
            {
                BindThemes();
                if (Settings.Contains("ModuleTheme"))
                {
                    try
                    {
                        SelectTheme(Conversions.ToString(Settings["ModuleTheme"]));
                    }
                    catch
                    {
                    }
                }

                BindSelectedTheme();
                VerifyPasswordSettings();
            }
        }

        private void drpThemes_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindSelectedTheme();
        }

        private void cmdUpdate_Click(object sender, EventArgs e)
        {
            bool blnSucess = false;
            SaveTemplates(ref blnSucess);
            if (blnSucess)
            {
                UpdateSettings();
            }
        }

        private void cmdUpdateExit_Click(object sender, EventArgs e)
        {
            bool blnSucess = false;
            SaveTemplates(ref blnSucess);
            if (blnSucess)
            {
                UpdateSettings();
            }

            Response.Redirect(DotNetNuke.Common.Globals.NavigateURL(TabId));
        }

        private void cmdDeleteSelected_Click(object sender, EventArgs e)
        {
            try
            {
                DeleteTheme();
            }
            catch (Exception ex)
            {
                DotNetNuke.UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("lblDeleteThemeError", LocalResourceFile), ModuleMessage.ModuleMessageType.RedError);
            }
        }

        private void SelectTheme(string ThemeName)
        {
            drpThemes.Items.FindByText(ThemeName).Selected = true;
        }

        private void drpLocales_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindSelectedTheme();
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            Response.Redirect(DotNetNuke.Common.Globals.NavigateURL());
        }

        private void VerifyPasswordSettings()
        {
            if (DotNetNuke.Security.Membership.MembershipProvider.Instance().PasswordRetrievalEnabled == false)
            {
                string strNote = Localization.GetString("lblPasswordRetrievalDisabled", LocalResourceFile);
                if (DotNetNuke.Security.Membership.MembershipProvider.Instance().RequiresQuestionAndAnswer)
                {
                    strNote += Localization.GetString("lblRequiresQuestionAndAnswer", LocalResourceFile);
                }

                DotNetNuke.UI.Skins.Skin.AddModuleMessage(this, strNote, ModuleMessage.ModuleMessageType.BlueInfo);
            }
        }

        private void LocalizeForm()
        {
            cmdCancel.Text = Localization.GetString("cmdCancel", LocalResourceFile);
            cmdUpdate.Text = Localization.GetString("cmdUpdate", LocalResourceFile);
            cmdUpdateExit.Text = Localization.GetString("cmdUpdateExit", LocalResourceFile);
            cmdCopySelected.Text = Localization.GetString("cmdCopySelected", LocalResourceFile);
            cmdDeleteSelected.Text = Localization.GetString("cmdDeleteSelected", LocalResourceFile);
        }

        private void cmdCopySelected_Click(object sender, EventArgs e)
        {
            pnlTemplateName.Visible = true;
        }

        private void BindThemes()
        {
            drpThemes.Items.Clear();
            string basepath = Server.MapPath(TemplateSourceDirectory + "/templates/");
            foreach (string folder in System.IO.Directory.GetDirectories(basepath))
            {
                string foldername = folder.Substring(folder.LastIndexOf(@"\") + 1);
                drpThemes.Items.Add(new ListItem(foldername, folder));
            }
        }

        private void BindSelectedTheme()
        {
            cmdDeleteSelected.Visible = drpThemes.SelectedIndex != 0;
            if (Settings.Contains("ModuleTheme"))
            {
                try
                {
                    if ((Conversions.ToString(Settings["ModuleTheme"]) ?? "") == (drpThemes.SelectedItem.Text ?? ""))
                    {
                        chkUseTheme.Checked = true;
                        DotNetNuke.UI.Utilities.ClientAPI.AddButtonConfirm(cmdDeleteSelected, Localization.GetSafeJSString(Localization.GetString("lblThemeInUse", LocalResourceFile)));
                    }
                    else
                    {
                        chkUseTheme.Checked = false;
                        DotNetNuke.UI.Utilities.ClientAPI.AddButtonConfirm(cmdDeleteSelected, Localization.GetSafeJSString(Localization.GetString("lblConfirmDelete", LocalResourceFile)));
                    }
                }
                catch
                {
                }
            }
            else
            {
                DotNetNuke.UI.Utilities.ClientAPI.AddButtonConfirm(cmdDeleteSelected, Localization.GetSafeJSString(Localization.GetString("lblConfirmDelete", LocalResourceFile)));
                chkUseTheme.Checked = false;
            }

            string path = drpThemes.SelectedValue;
            foreach (string file in System.IO.Directory.GetFiles(path))
            {
                if (file.EndsWith(Libraries.UserManagement.Constants.TemplateName_CreateForm))
                {
                    txtCreateFormTemplate.Text = GetTemplate(drpThemes.SelectedItem.Value, Libraries.UserManagement.Constants.TemplateName_CreateForm, drpLocales.SelectedValue, true);
                }

                if (file.EndsWith(Libraries.UserManagement.Constants.TemplateName_EmailAccountCreated))
                {
                    txtEmailAccountCreated.Text = GetTemplate(drpThemes.SelectedItem.Value, Libraries.UserManagement.Constants.TemplateName_EmailAccountCreated, drpLocales.SelectedValue, true);
                }

                if (file.EndsWith(Libraries.UserManagement.Constants.TemplateName_EmailAccountData))
                {
                    txtEmailAccountData.Text = GetTemplate(drpThemes.SelectedItem.Value, Libraries.UserManagement.Constants.TemplateName_EmailAccountData, drpLocales.SelectedValue, true);
                }

                if (file.EndsWith(Libraries.UserManagement.Constants.TemplateName_EmailAddedToRole))
                {
                    txtEmailAddedToRole.Text = GetTemplate(drpThemes.SelectedItem.Value, Libraries.UserManagement.Constants.TemplateName_EmailAddedToRole, drpLocales.SelectedValue, true);
                }

                if (file.EndsWith(Libraries.UserManagement.Constants.TemplateName_EmailRemovedFromRole))
                {
                    txtEmailRemovedFromRole.Text = GetTemplate(drpThemes.SelectedItem.Value, Libraries.UserManagement.Constants.TemplateName_EmailRemovedFromRole, drpLocales.SelectedValue, true);
                }

                if (file.EndsWith(Libraries.UserManagement.Constants.TemplateName_EmailRoleStatusChanged))
                {
                    txtEmailRoleStatusChanged.Text = GetTemplate(drpThemes.SelectedItem.Value, Libraries.UserManagement.Constants.TemplateName_EmailRoleStatusChanged, drpLocales.SelectedValue, true);
                }

                if (file.EndsWith(Libraries.UserManagement.Constants.TemplateName_EmailPasswordReset))
                {
                    txtEmailPasswordReset.Text = GetTemplate(drpThemes.SelectedItem.Value, Libraries.UserManagement.Constants.TemplateName_EmailPasswordReset, drpLocales.SelectedValue, true);
                }

                if (file.EndsWith(Libraries.UserManagement.Constants.TemplateName_ProfileForm))
                {
                    txtProfileFormTemplate.Text = GetTemplate(drpThemes.SelectedItem.Value, Libraries.UserManagement.Constants.TemplateName_ProfileForm, drpLocales.SelectedValue, true);
                }

                if (file.EndsWith(Libraries.UserManagement.Constants.TemplateName_AccountForm))
                {
                    txtAccountFormTemplate.Text = GetTemplate(drpThemes.SelectedItem.Value, Libraries.UserManagement.Constants.TemplateName_AccountForm, drpLocales.SelectedValue, true);
                }
            }
        }

        private void SaveTemplate(string SelectedTheme, string TemplateName, string Locale)
        {
            string path = SelectedTheme + @"\" + TemplateName.Replace(Libraries.UserManagement.Constants.TemplateName_Extension, "." + Locale + Libraries.UserManagement.Constants.TemplateName_Extension);
            if ((PortalSettings.DefaultLanguage.ToLower() ?? "") == (Locale.ToLower() ?? "") | string.IsNullOrEmpty(Locale))
            {
                path = SelectedTheme + @"\" + TemplateName;
            }

            var sw = new System.IO.StreamWriter(path, false);
            if ((TemplateName ?? "") == Libraries.UserManagement.Constants.TemplateName_CreateForm)
            {
                sw.Write(txtCreateFormTemplate.Text);
            }

            if ((TemplateName ?? "") == Libraries.UserManagement.Constants.TemplateName_EmailAccountCreated)
            {
                sw.Write(txtEmailAccountCreated.Text);
            }

            if ((TemplateName ?? "") == Libraries.UserManagement.Constants.TemplateName_EmailAccountData)
            {
                sw.Write(txtEmailAccountData.Text);
            }

            if ((TemplateName ?? "") == Libraries.UserManagement.Constants.TemplateName_EmailAddedToRole)
            {
                sw.Write(txtEmailAddedToRole.Text);
            }

            if ((TemplateName ?? "") == Libraries.UserManagement.Constants.TemplateName_EmailRemovedFromRole)
            {
                sw.Write(txtEmailRemovedFromRole.Text);
            }

            if ((TemplateName ?? "") == Libraries.UserManagement.Constants.TemplateName_EmailRoleStatusChanged)
            {
                sw.Write(txtEmailRoleStatusChanged.Text);
            }

            if ((TemplateName ?? "") == Libraries.UserManagement.Constants.TemplateName_EmailPasswordReset)
            {
                sw.Write(txtEmailPasswordReset.Text);
            }

            if ((TemplateName ?? "") == Libraries.UserManagement.Constants.TemplateName_AccountForm)
            {
                sw.Write(txtAccountFormTemplate.Text);
            }

            if ((TemplateName ?? "") == Libraries.UserManagement.Constants.TemplateName_ProfileForm)
            {
                sw.Write(txtProfileFormTemplate.Text);
            }

            sw.Close();
            sw.Dispose();
        }

        private void SaveTemplates(ref bool blnSucess)
        {
            string basepath = drpThemes.SelectedValue;
            if (pnlTemplateName.Visible)
            {
                if (string.IsNullOrEmpty(txtTemplateName.Text))
                {
                    DotNetNuke.UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("lblMustEnterTemplateName", LocalResourceFile), ModuleMessage.ModuleMessageType.RedError);
                    blnSucess = false;
                    return;
                }

                if (string.IsNullOrEmpty(txtProfileFormTemplate.Text) | string.IsNullOrEmpty(txtAccountFormTemplate.Text))
                {
                    DotNetNuke.UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("lblMustEnterTemplate", LocalResourceFile), ModuleMessage.ModuleMessageType.RedError);
                    blnSucess = false;
                    return;
                }

                string newpath = Server.MapPath(TemplateSourceDirectory + "/templates/") + txtTemplateName.Text;
                try
                {
                    System.IO.Directory.CreateDirectory(newpath);
                }
                catch
                {
                    DotNetNuke.UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("lblInvalidFolderName", LocalResourceFile), ModuleMessage.ModuleMessageType.RedError);
                    blnSucess = false;
                    return;
                }

                try
                {
                    foreach (string file in System.IO.Directory.GetFiles(basepath))
                    {
                        string destinationpath = newpath + @"\" + file.Substring(file.LastIndexOf(@"\") + 1);
                        System.IO.File.Copy(file, destinationpath);
                    }

                    basepath = newpath;
                }
                catch (Exception ex)
                {
                    DotNetNuke.UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("lblCouldNotCopyTheme", LocalResourceFile), ModuleMessage.ModuleMessageType.RedError);
                    blnSucess = false;
                    return;
                }

                pnlTemplateName.Visible = false;
                BindThemes();
                SelectTheme(txtTemplateName.Text);
                cmdDeleteSelected.Visible = true;
            }

            try
            {
                foreach (string file in System.IO.Directory.GetFiles(basepath))
                {
                    if (file.EndsWith(Libraries.UserManagement.Constants.TemplateName_CreateForm))
                    {
                        SaveTemplate(drpThemes.SelectedValue, Libraries.UserManagement.Constants.TemplateName_CreateForm, drpLocales.SelectedValue);
                    }

                    if (file.EndsWith(Libraries.UserManagement.Constants.TemplateName_EmailAccountCreated))
                    {
                        SaveTemplate(drpThemes.SelectedValue, Libraries.UserManagement.Constants.TemplateName_EmailAccountCreated, drpLocales.SelectedValue);
                    }

                    if (file.EndsWith(Libraries.UserManagement.Constants.TemplateName_EmailAccountData))
                    {
                        SaveTemplate(drpThemes.SelectedValue, Libraries.UserManagement.Constants.TemplateName_EmailAccountData, drpLocales.SelectedValue);
                    }

                    if (file.EndsWith(Libraries.UserManagement.Constants.TemplateName_EmailAddedToRole))
                    {
                        SaveTemplate(drpThemes.SelectedValue, Libraries.UserManagement.Constants.TemplateName_EmailAddedToRole, drpLocales.SelectedValue);
                    }

                    if (file.EndsWith(Libraries.UserManagement.Constants.TemplateName_EmailRemovedFromRole))
                    {
                        SaveTemplate(drpThemes.SelectedValue, Libraries.UserManagement.Constants.TemplateName_EmailRemovedFromRole, drpLocales.SelectedValue);
                    }

                    if (file.EndsWith(Libraries.UserManagement.Constants.TemplateName_EmailRoleStatusChanged))
                    {
                        SaveTemplate(drpThemes.SelectedValue, Libraries.UserManagement.Constants.TemplateName_EmailRoleStatusChanged, drpLocales.SelectedValue);
                    }

                    if (file.EndsWith(Libraries.UserManagement.Constants.TemplateName_EmailPasswordReset))
                    {
                        SaveTemplate(drpThemes.SelectedValue, Libraries.UserManagement.Constants.TemplateName_EmailPasswordReset, drpLocales.SelectedValue);
                    }

                    if (file.EndsWith(Libraries.UserManagement.Constants.TemplateName_AccountForm))
                    {
                        SaveTemplate(drpThemes.SelectedValue, Libraries.UserManagement.Constants.TemplateName_AccountForm, drpLocales.SelectedValue);
                    }

                    if (file.EndsWith(Libraries.UserManagement.Constants.TemplateName_ProfileForm))
                    {
                        SaveTemplate(drpThemes.SelectedValue, Libraries.UserManagement.Constants.TemplateName_ProfileForm, drpLocales.SelectedValue);
                    }
                }
            }
            catch (Exception ex)
            {
                DotNetNuke.UI.Skins.Skin.AddModuleMessage(this, Localization.GetString("lblCouldNotWriteTheme", LocalResourceFile), ModuleMessage.ModuleMessageType.RedError);
                blnSucess = false;
                return;
            }

            blnSucess = true;
        }

        private void UpdateSettings()
        {
            var ctrl = new ModuleController();
            ctrl.UpdateTabModuleSetting(TabModuleId, "ModuleTheme", drpThemes.SelectedItem.Text);
        }

        private void DeleteTheme()
        {
            string basepath = drpThemes.SelectedValue;
            foreach (string file in System.IO.Directory.GetFiles(basepath))
                System.IO.File.Delete(file);
            System.IO.Directory.Delete(basepath);
            BindThemes();
            UpdateSettings();
            BindSelectedTheme();
        }

        private void BindLocales()
        {
            var dicLocales = DotNetNuke.ComponentModel.ComponentBase<ILocaleController, LocaleController>.Instance.GetLocales(PortalId);
            if (dicLocales.Count > 1)
            {
                pnlLocales.Visible = true;
            }

            foreach (Locale objLocale in dicLocales.Values)
            {
                var item = new ListItem();
                item.Text = objLocale.Text;
                item.Value = objLocale.Code;
                drpLocales.Items.Add(item);
            }

            try
            {
                drpLocales.Items[0].Selected = true;
            }
            catch
            {
            }
        }
    }
}