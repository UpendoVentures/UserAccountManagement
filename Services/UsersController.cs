using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DotNetNuke.Data;
using DotNetNuke.Web.Api;

namespace Connect.Modules.UserManagement.AccountManagement.Services
{
    public class UsersController : DnnApiController
    {
        [AllowAnonymous]
        [HttpPost]
        public HttpResponseMessage AutoSuggestResult(RequestDTO postData)
        {
            HttpResponseMessage httpResponseMessage;
            try
            {
                var resultList = new List<AutoSuggestResponseDTO>();
                IDataReader dr = null;
                if (postData.RoleId == PortalSettings.RegisteredRoleId)
                {
                    dr = DataProvider.Instance().ExecuteReader("Connect_Accounts_SearchRegisteredUsers", postData.RoleId, postData.PortalId, postData.SearchText, postData.SearchCols);
                }
                else if (postData.RoleId == -2)
                {
                    dr = DataProvider.Instance().ExecuteReader("Connect_Accounts_SearchDeletedUsers", postData.RoleId, postData.PortalId, postData.SearchText, postData.SearchCols);
                }
                else
                {
                    dr = DataProvider.Instance().ExecuteReader("Connect_Accounts_SearchRoleMembers", postData.RoleId, postData.SearchText, postData.SearchCols);
                }

                if (dr is object)
                {
                    while (dr.Read())
                    {
                        var result = new AutoSuggestResponseDTO();
                        result.EntryIcon = "";
                        result.EntryName = Convert.ToString(dr["Displayname"]);
                        result.EntryUrl = DotNetNuke.Common.Globals.NavigateURL(postData.TabId, "", "uid=" + Convert.ToString(dr["UserId"]), "RoleId=" + postData.RoleId.ToString(), "Action=edit");
                        resultList.Add(result);
                    }

                    dr.Close();
                    dr.Dispose();
                }

                httpResponseMessage = Request.CreateResponse(HttpStatusCode.OK, resultList);
            }
            catch (Exception ex)
            {
                httpResponseMessage = Request.CreateResponse(HttpStatusCode.OK, ex.Message);
            }

            return httpResponseMessage;
        }
    }

    public class RequestDTO
    {
        public int TabId { get; set; }
        public int PortalId { get; set; }
        public int RoleId { get; set; }
        public string SearchText { get; set; }
        public string SearchCols { get; set; }
    }

    public class AutoSuggestResponseDTO
    {
        public string EntryName { get; set; }
        public string EntryUrl { get; set; }
        public string EntryIcon { get; set; }
    }
}