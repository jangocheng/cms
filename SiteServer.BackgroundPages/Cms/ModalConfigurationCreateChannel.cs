﻿using System;
using System.Collections.Specialized;
using System.Web.UI.WebControls;
using SiteServer.Utils;
using SiteServer.CMS.Core;
using SiteServer.Utils.Enumerations;

namespace SiteServer.BackgroundPages.Cms
{
    public class ModalConfigurationCreateChannel : BasePageCms
    {
        public DropDownList DdlIsCreateChannelIfContentChanged;

        protected ListBox LbNodeId;

		private int _nodeId;

        public static string GetOpenWindowString(int siteId, int nodeId)
        {
            return LayerUtils.GetOpenScript("栏目生成设置",
                PageUtils.GetCmsUrl(siteId, nameof(ModalConfigurationCreateChannel), new NameValueCollection
                {
                    {"NodeID", nodeId.ToString()}
                }), 550, 500);
        }

        public void Page_Load(object sender, EventArgs e)
        {
            if (IsForbidden) return;

            PageUtils.CheckRequestParameter("siteId", "NodeID");
            _nodeId = Body.GetQueryInt("NodeID");

			if (!IsPostBack)
			{
                var nodeInfo = ChannelManager.GetChannelInfo(SiteId, _nodeId);

                EBooleanUtils.AddListItems(DdlIsCreateChannelIfContentChanged, "生成", "不生成");
                ControlUtils.SelectSingleItemIgnoreCase(DdlIsCreateChannelIfContentChanged, nodeInfo.Additional.IsCreateChannelIfContentChanged.ToString());

                //NodeManager.AddListItemsForAddContent(this.NodeIDCollection.Items, base.SiteInfo, false);
                ChannelManager.AddListItemsForCreateChannel(LbNodeId.Items, SiteInfo, false, Body.AdminName);
                ControlUtils.SelectMultiItems(LbNodeId, TranslateUtils.StringCollectionToStringList(nodeInfo.Additional.CreateChannelIDsIfContentChanged));
			}
		}

        public override void Submit_OnClick(object sender, EventArgs e)
        {
            var isSuccess = false;

            try
            {
                var nodeInfo = ChannelManager.GetChannelInfo(SiteId, _nodeId);

                nodeInfo.Additional.IsCreateChannelIfContentChanged = TranslateUtils.ToBool(DdlIsCreateChannelIfContentChanged.SelectedValue);
                nodeInfo.Additional.CreateChannelIDsIfContentChanged = ControlUtils.GetSelectedListControlValueCollection(LbNodeId);

                DataProvider.ChannelDao.Update(nodeInfo);

                Body.AddSiteLog(SiteId, _nodeId, 0, "设置栏目变动生成页面", $"栏目:{nodeInfo.ChannelName}");
                isSuccess = true;
            }
            catch (Exception ex)
            {
                FailMessage(ex, ex.Message);
            }

            if (isSuccess)
            {
                LayerUtils.CloseAndRedirect(Page, PageConfigurationCreateTrigger.GetRedirectUrl(SiteId, _nodeId));
            }
        }
	}
}
