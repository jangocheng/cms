﻿using System;
using System.Collections.Specialized;
using System.Web.UI.WebControls;
using SiteServer.Utils;
using SiteServer.CMS.Plugin;

namespace SiteServer.BackgroundPages.Plugins
{
    public class PageView : BasePage
    {
        public Button BtnInstall;
        public PlaceHolder PhSuccess;
        public PlaceHolder PhFailure;
        public Literal LtlErrorMessage;

        private string _pluginId;
        private string _version;

        public static string GetRedirectUrl(string pluginId, string version)
        {
            return PageUtils.GetPluginsUrl(nameof(PageView), new NameValueCollection
            {
                {"pluginId", pluginId},
                {"version", version}
            });
        }

        public void Page_Load(object sender, EventArgs e)
        {
            if (IsForbidden) return;

            _pluginId = Body.GetQueryString("pluginId");
            _version = Body.GetQueryString("version");

            if (Page.IsPostBack) return;

            VerifyAdministratorPermissions(AppManager.Permissions.Plugins.Add, AppManager.Permissions.Plugins.Management);

            if (PluginManager.IsExists(_pluginId))
            {
                BtnInstall.Text = "插件已安装";
                BtnInstall.Enabled = false;
            }
        }

        public void BtnInstall_Click(object sender, EventArgs e)
        {
            string errorMessage;
            var isSuccess = PluginManager.GetAndInstall(_pluginId, _version, out errorMessage);
            if (isSuccess)
            {
                PhSuccess.Visible = true;
            }
            else
            {
                PhFailure.Visible = true;
                LtlErrorMessage.Text = errorMessage;
            }
        }

        public void Return_Click(object sender, EventArgs e)
        {
            PageUtils.Redirect(PageAdd.GetRedirectUrl());
        }
    }
}
