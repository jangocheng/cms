﻿using System.Text;
using System.Web;
using System.Web.Http;
using SiteServer.Utils;
using SiteServer.CMS.Controllers.Sys.Stl;
using SiteServer.CMS.Core;
using SiteServer.CMS.StlParser.StlElement;

namespace SiteServer.API.Controllers.Sys.Stl
{
    [RoutePrefix("api")]
    public class StlActionsLoadingChannelsController : ApiController
    {
        [HttpPost, Route(ActionsLoadingChannels.Route)]
        public void Main()
        {
            var builder = new StringBuilder();

            try
            {
                var form = HttpContext.Current.Request.Form;
                var siteId = TranslateUtils.ToInt(form["siteID"]);
                var parentId = TranslateUtils.ToInt(form["parentID"]);
                var target = form["target"];
                var isShowTreeLine = TranslateUtils.ToBool(form["isShowTreeLine"]);
                var isShowContentNum = TranslateUtils.ToBool(form["isShowContentNum"]);
                var currentFormatString = form["currentFormatString"];
                var topNodeId = TranslateUtils.ToInt(form["topNodeID"]);
                var topParentsCount = TranslateUtils.ToInt(form["topParentsCount"]);
                var currentNodeId = TranslateUtils.ToInt(form["currentNodeID"]);

                var siteInfo = SiteManager.GetSiteInfo(siteId);
                var nodeIdList = DataProvider.ChannelDao.GetIdListByParentId(siteId, parentId);

                foreach (var nodeId in nodeIdList)
                {
                    var nodeInfo = ChannelManager.GetChannelInfo(siteId, nodeId);

                    builder.Append(StlTree.GetChannelRowHtml(siteInfo, nodeInfo, target, isShowTreeLine, isShowContentNum, TranslateUtils.DecryptStringBySecretKey(currentFormatString), topNodeId, topParentsCount, currentNodeId, false));
                }
            }
            catch
            {
                // ignored
            }

            HttpContext.Current.Response.Write(builder);
            HttpContext.Current.Response.End();
        }
    }
}
