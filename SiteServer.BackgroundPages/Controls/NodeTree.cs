﻿using System.Text;
using System.Web.UI;
using SiteServer.BackgroundPages.Core;
using SiteServer.CMS.Core;
using SiteServer.CMS.Core.Security;
using SiteServer.CMS.Model;
using SiteServer.CMS.Model.Enumerations;
using SiteServer.CMS.Plugin.Model;

namespace SiteServer.BackgroundPages.Controls
{
    public class NodeTree : Control
    {
        private SiteInfo _siteInfo;

        protected override void Render(HtmlTextWriter writer)
        {
            var builder = new StringBuilder();

            var context = new RequestContext();

            var siteId = int.Parse(Page.Request.QueryString["SiteId"]);
            _siteInfo = SiteManager.GetSiteInfo(siteId);
            var scripts = ChannelLoading.GetScript(_siteInfo, ELoadingType.ContentTree, null);
            builder.Append(scripts);
            if (Page.Request.QueryString["SiteId"] != null)
            {
                var nodeIdList = DataProvider.ChannelDao.GetIdListByParentId(_siteInfo.Id, 0);
                foreach (var nodeId in nodeIdList)
                {
                    var nodeInfo = ChannelManager.GetChannelInfo(_siteInfo.Id, nodeId);
                    var enabled = AdminUtility.IsOwningNodeId(context.AdminName, nodeInfo.Id);
                    if (!enabled)
                    {
                        if (!AdminUtility.IsHasChildOwningNodeId(context.AdminName, nodeInfo.Id)) continue;
                    }

                    builder.Append(ChannelLoading.GetChannelRowHtml(_siteInfo, nodeInfo, enabled, ELoadingType.ContentTree, null, context.AdminName));
                }
            }
            writer.Write(builder);
        }
    }
}