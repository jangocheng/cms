﻿using System;
using System.Web.UI.WebControls;
using SiteServer.CMS.Core;
using SiteServer.Utils;
using SiteServer.CMS.Core.Security;
using SiteServer.Utils.Enumerations;

namespace SiteServer.BackgroundPages.Settings
{
	public class PageAdminRole : BasePage
    {
		public Repeater RptContents;
        public Button BtnAdd;

        public static string GetRedirectUrl()
        {
            return PageUtils.GetSettingsUrl(nameof(PageAdminRole), null);
        }

        public void Page_Load(object sender, EventArgs e)
        {
            if (IsForbidden) return;

			if (Body.IsQueryExists("Delete"))
			{
				var roleName = Body.GetQueryString("RoleName");
				try
				{
                    DataProvider.PermissionsInRolesDao.Delete(roleName);
                    DataProvider.RoleDao.DeleteRole(roleName);

                    Body.AddAdminLog("删除管理员角色", $"角色名称:{roleName}");

					SuccessDeleteMessage();
				}
				catch(Exception ex)
				{
					FailDeleteMessage(ex);
				}
			}

            if (IsPostBack) return;

            VerifyAdministratorPermissions(AppManager.Permissions.Settings.Admin);

            var permissioins = PermissionsManager.GetPermissions(Body.AdminName);
            RptContents.DataSource = permissioins.IsConsoleAdministrator
                ? DataProvider.RoleDao.GetAllRoles()
                : DataProvider.RoleDao.GetAllRolesByCreatorUserName(Body.AdminName);
            RptContents.ItemDataBound += RptContents_ItemDataBound;
            RptContents.DataBind();

            BtnAdd.Attributes.Add("onclick", $"location.href = '{PageAdminRoleAdd.GetRedirectUrl()}';return false;");
        }

        private static void RptContents_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem) return;

            var roleName = (string)e.Item.DataItem;
            e.Item.Visible = !EPredefinedRoleUtils.IsPredefinedRole(roleName);

            var ltlRoleName = (Literal) e.Item.FindControl("ltlRoleName");
            var ltlDescription = (Literal)e.Item.FindControl("ltlDescription");
            var ltlEdit = (Literal)e.Item.FindControl("ltlEdit");
            var ltlDelete = (Literal)e.Item.FindControl("ltlDelete");

            ltlRoleName.Text = roleName;
            ltlDescription.Text = DataProvider.RoleDao.GetRoleDescription(roleName);
            ltlEdit.Text = $@"<a href=""{PageAdminRoleAdd.GetRedirectUrl(roleName)}"">修改</a>";
            ltlDelete.Text = $@"<a href=""javascript:;"" onClick=""{AlertUtils.ConfirmDelete("删除角色", $"此操作将会删除角色“{roleName}”，确认吗？", $"{GetRedirectUrl()}?Delete={true}&RoleName={roleName}")}"">删除</a>";
        }
	}
}
