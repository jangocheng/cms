﻿using System;
using System.Collections;
using System.Collections.Specialized;
using System.Web.UI.WebControls;
using SiteServer.Utils;
using SiteServer.BackgroundPages.Core;
using SiteServer.CMS.Core;
using SiteServer.CMS.Core.Security;
using SiteServer.CMS.Model;
using SiteServer.CMS.Model.Enumerations;

namespace SiteServer.BackgroundPages.Cms
{
	public class ModalChannelMultipleSelect : BasePageCms
    {
        public PlaceHolder PhSiteId;
        public DropDownList DdlSiteId;
        public Literal LtlChannelName;
        public Repeater RptChannel;

        private int _targetSiteId;
        private bool _isSiteSelect;
        private string _jsMethod;

        public static string GetOpenWindowString(int siteId, bool isSiteSelect,
            string jsMethod)
        {
            return LayerUtils.GetOpenScript("选择目标栏目",
                PageUtils.GetCmsUrl(siteId, nameof(ModalChannelMultipleSelect), new NameValueCollection
                {
                    {"isSiteSelect", isSiteSelect.ToString()},
                    {"jsMethod", jsMethod}
                }), 650, 580);
        }

        public static string GetOpenWindowString(int siteId, bool isSiteSelect)
        {
            return GetOpenWindowString(siteId, isSiteSelect, "translateNodeAdd");
        }

        public string GetRedirectUrl(string targetSiteId, string targetNodeId)
        {
            return PageUtils.GetCmsUrl(SiteId, nameof(ModalChannelMultipleSelect), new NameValueCollection
            {
                {"isSiteSelect", _isSiteSelect.ToString()},
                {"jsMethod", _jsMethod},
                {"targetSiteId", targetSiteId},
                {"targetNodeID", targetNodeId}
            });
        }

        public void Page_Load(object sender, EventArgs e)
        {
            if (IsForbidden) return;

            _isSiteSelect = Body.GetQueryBool("isSiteSelect");
            _jsMethod = Body.GetQueryString("jsMethod");

            _targetSiteId = Body.GetQueryInt("TargetSiteId");
            if (_targetSiteId == 0)
            {
                _targetSiteId = SiteId;
            }

            if (!IsPostBack)
            {
                PhSiteId.Visible = _isSiteSelect;

                var siteIdList = ProductPermissionsManager.Current.SiteIdList;

                var mySystemInfoArrayList = new ArrayList();
                var parentWithChildren = new Hashtable();
                foreach (var siteId in siteIdList)
                {
                    var siteInfo = SiteManager.GetSiteInfo(siteId);
                    if (siteInfo.ParentId == 0)
                    {
                        mySystemInfoArrayList.Add(siteInfo);
                    }
                    else
                    {
                        var children = new ArrayList();
                        if (parentWithChildren.Contains(siteInfo.ParentId))
                        {
                            children = (ArrayList)parentWithChildren[siteInfo.ParentId];
                        }
                        children.Add(siteInfo);
                        parentWithChildren[siteInfo.ParentId] = children;
                    }
                }
                foreach (SiteInfo siteInfo in mySystemInfoArrayList)
                {
                    AddSite(DdlSiteId, siteInfo, parentWithChildren, 0);
                }
                ControlUtils.SelectSingleItem(DdlSiteId, _targetSiteId.ToString());

                var targetNodeId = Body.GetQueryInt("TargetNodeID");
                if (targetNodeId > 0)
                {
                    var siteName = SiteManager.GetSiteInfo(_targetSiteId).SiteName;
                    var nodeNames = ChannelManager.GetChannelNameNavigation(_targetSiteId, targetNodeId);
                    if (_targetSiteId != SiteId)
                    {
                        nodeNames = siteName + "：" + nodeNames;
                    }
                    string value = $"{_targetSiteId}_{targetNodeId}";
                    if (!_isSiteSelect)
                    {
                        value = targetNodeId.ToString();
                    }
                    string scripts = $"window.parent.{_jsMethod}('{nodeNames}', '{value}');";
                    LayerUtils.CloseWithoutRefresh(Page, scripts);
                }
                else
                {
                    var nodeInfo = ChannelManager.GetChannelInfo(_targetSiteId, _targetSiteId);
                    var linkUrl = GetRedirectUrl(_targetSiteId.ToString(), _targetSiteId.ToString());
                    LtlChannelName.Text = $"<a href='{linkUrl}'>{nodeInfo.ChannelName}</a>";

                    var additional = new NameValueCollection
                    {
                        ["linkUrl"] = GetRedirectUrl(_targetSiteId.ToString(), string.Empty)
                    };
                    ClientScriptRegisterClientScriptBlock("NodeTreeScript", ChannelLoading.GetScript(SiteManager.GetSiteInfo(_targetSiteId), ELoadingType.ChannelSelect, additional));

                    RptChannel.DataSource = DataProvider.ChannelDao.GetIdListByParentId(_targetSiteId, _targetSiteId);
                    RptChannel.ItemDataBound += RptChannel_ItemDataBound;
                    RptChannel.DataBind();
                }
            }
        }

        private void RptChannel_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            var nodeId = (int)e.Item.DataItem;
            var enabled = IsOwningNodeId(nodeId);
            if (!enabled)
            {
                if (!IsHasChildOwningNodeId(nodeId)) e.Item.Visible = false;
            }
            var nodeInfo = ChannelManager.GetChannelInfo(_targetSiteId, nodeId);

            var ltlHtml = (Literal)e.Item.FindControl("ltlHtml");

            var additional = new NameValueCollection
            {
                ["linkUrl"] = GetRedirectUrl(_targetSiteId.ToString(), string.Empty)
            };

            ltlHtml.Text = ChannelLoading.GetChannelRowHtml(SiteInfo, nodeInfo, enabled, ELoadingType.ChannelSelect, additional, Body.AdminName);
        }

        public void DdlSiteId_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            var redirectUrl = GetRedirectUrl(DdlSiteId.SelectedValue, string.Empty);
            PageUtils.Redirect(redirectUrl);
        }

        private void AddSite(ListControl listControl, SiteInfo siteInfo, Hashtable parentWithChildren, int level)
        {
            var padding = string.Empty;
            for (var i = 0; i < level; i++)
            {
                padding += "　";
            }
            if (level > 0)
            {
                padding += "└ ";
            }

            if (parentWithChildren[siteInfo.Id] != null)
            {
                var children = (ArrayList)parentWithChildren[siteInfo.Id];

                var listitem = new ListItem(padding + siteInfo.SiteName +
                                                 $"({children.Count})", siteInfo.Id.ToString());
                if (siteInfo.Id == SiteId) listitem.Selected = true;

                listControl.Items.Add(listitem);
                level++;
                foreach (SiteInfo subSiteInfo in children)
                {
                    AddSite(listControl, subSiteInfo, parentWithChildren, level);
                }
            }
            else
            {
                var listitem = new ListItem(padding + siteInfo.SiteName, siteInfo.Id.ToString());
                if (siteInfo.Id == SiteId) listitem.Selected = true;

                listControl.Items.Add(listitem);
            }
        }
	}
}
