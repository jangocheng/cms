﻿using System;
using System.Web.UI.WebControls;
using SiteServer.CMS.Core;
using SiteServer.Utils;

namespace SiteServer.BackgroundPages.Settings
{
    public class PageUtilityCache : BasePage
    {
        public Literal LtlCount;
        public Literal LtlPercentage;
        public Repeater RptContents;

        public void Page_Load(object sender, EventArgs e)
        {
            if (IsForbidden) return;

            if (IsPostBack) return;

            VerifyAdministratorPermissions(AppManager.Permissions.Settings.Utility);

            LtlCount.Text = CacheUtils.Count.ToString();

            RptContents.DataSource = CacheUtils.AllKeys;
            RptContents.ItemDataBound += RptContents_ItemDataBound;
            RptContents.DataBind();
        }

        private void RptContents_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem) return;

            var key = (string) e.Item.DataItem;
            var value = CacheUtils.Get(key);

            if (value == null) return;
            var valueType = value.GetType().FullName;

            var ltlKey = (Literal)e.Item.FindControl("ltlKey");
            var ltlValue = (Literal)e.Item.FindControl("ltlValue");

            ltlKey.Text = key;
                
            if (valueType == "System.String")
            {
                ltlValue.Text = $"string, length:{value.ToString().Length}";
            } else if (valueType == "System.Int32")
            {
                ltlValue.Text = value.ToString();
            }
            else
            {
                ltlValue.Text = valueType;
            }
        }

        public override void Submit_OnClick(object sender, EventArgs e)
        {
            if (!Page.IsPostBack || !Page.IsValid) return;

            CacheUtils.ClearAll();
            CacheDbUtils.Clear();
            PageUtils.Redirect(PageUtils.GetSettingsUrl(nameof(PageUtilityCache), null));
        }

    }
}
