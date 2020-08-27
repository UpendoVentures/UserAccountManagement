<%@ Control Language="c#" AutoEventWireup="true" Inherits="Connect.Modules.UserManagement.AccountManagement.Settings" CodeBehind="Settings.ascx.cs" %>
<%@ Register TagPrefix="dnn" Assembly="DotNetNuke.Web" Namespace="DotNetNuke.Web.UI.WebControls" %>
<%@ Register TagName="label" TagPrefix="dnn" Src="~/controls/labelcontrol.ascx" %>

<div class="dnnFormItem">
    <dnn:Label ID="lblAllowReports" runat="server" resourcekey="lblAllowReports" />
    <asp:CheckBox ID="chkAllowReports" runat="server" />
</div>

<div class="dnnFormItem">
    <dnn:Label ID="lblAllowCreate" runat="server" resourcekey="lblAllowCreate" />
    <asp:CheckBox ID="chkAllowCreate" runat="server" />
</div>

<div class="dnnFormItem">
    <dnn:Label ID="lblAllowDelete" runat="server" resourcekey="lblAllowDelete" />
    <asp:CheckBox ID="chkAllowDelete" runat="server" />
</div>

<div class="dnnFormItem">
    <dnn:Label ID="lblAllowHardDelete" runat="server" resourcekey="lblAllowHardDelete" />
    <asp:CheckBox ID="chkAllowHardDelete" runat="server" />
</div>

<div class="dnnFormItem">
    <dnn:Label ID="lblAllowExport" runat="server" resourcekey="lblAllowExport" />
    <asp:CheckBox ID="chkAllowExport" runat="server" AutoPostBack="true" OnCheckedChanged="chkAllowExport_CheckedChanged" />
</div>

<div class="dnnFormItem" runat="server" id="dvExportFields">
    <dnn:Label ID="lblExportFields" runat="server" resourcekey="lblExportFields" />
    <asp:TextBox ID="txtExportFields" TextMode="MultiLine" runat="server" />
</div>

<div class="dnnFormItem">
    <dnn:Label ID="lblAllowSendMessages" runat="server" resourcekey="lblAllowSendMessages" />
    <asp:CheckBox ID="chkAllowSendMessages" runat="server" />
</div>

<div class="dnnFormItem">
    <dnn:Label ID="lblAllowedRoles" runat="server" resourcekey="lblAllowedRoles" />
    <asp:CheckBoxList ID="chkAllowedRoles" runat="server" RepeatColumns="2" DataTextField="RoleName" DataValueField="RoleId" AutoPostBack="true" OnSelectedIndexChanged="chkAllowedRoles_SelectedIndexChanged" />
</div>

<div class="dnnFormItem">
    <dnn:Label ID="lblPreselectRole" runat="server" resourcekey="lblPreselectRole" />
    <asp:DropDownList ID="drpPreselectRole" runat="server" DataTextField="RoleName" DataValueField="RoleId" />
</div>

<div class="dnnFormItem">
    <dnn:Label ID="lblUserTabs" runat="server" resourcekey="lblUserTabs" />
    <asp:CheckBoxList ID="chkUserTabs" runat="server" RepeatColumns="2">
        <asp:ListItem Text="Account" Value="account" />
        <asp:ListItem Text="Password" Value="password" />
        <asp:ListItem Text="Profile" Value="profile" />
        <asp:ListItem Text="Roles" Value="roles" />
        <asp:ListItem Text="E-Mail" Value="email" />
        <asp:ListItem Text="Message" Value="message" />
        <asp:ListItem Text="Sites" Value="sites" />
    </asp:CheckBoxList>
</div>

<div class="dnnFormItem">
    <dnn:Label ID="lblAdditionalControls" runat="server" resourcekey="lblAdditionalControls" />
    <asp:TextBox ID="txtAditionalControls" TextMode="MultiLine" runat="server" />
</div>