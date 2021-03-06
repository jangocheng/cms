﻿using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using SiteServer.Utils;
using SiteServer.BackgroundPages.Settings;
using SiteServer.CMS.Core;
using SiteServer.CMS.Core.Create;
using SiteServer.CMS.Model.Enumerations;

namespace SiteServer.BackgroundPages.Cms
{
    public class PageCreateContent : BasePageCms
    {
        public ListBox LbNodeIdList;
        public DropDownList DdlScope;
        public Button BtnDeleteAll;

        public void Page_Load(object sender, EventArgs e)
        {
            if (IsForbidden) return;

            PageUtils.CheckRequestParameter("siteId");

            if (IsPostBack) return;

            VerifySitePermissions(AppManager.Permissions.WebSite.Create);

            var listitem = new ListItem("所有选中的栏目", "All");
            DdlScope.Items.Add(listitem);
            listitem = new ListItem("一个月内更新的内容", "Month");
            DdlScope.Items.Add(listitem);
            listitem = new ListItem("一天内更新的内容", "Day");
            DdlScope.Items.Add(listitem);
            listitem = new ListItem("2小时内更新的内容", "2Hour");
            DdlScope.Items.Add(listitem);

            ChannelManager.AddListItems(LbNodeIdList.Items, SiteInfo, false, true, Body.AdminName);
            BtnDeleteAll.Attributes.Add("onclick", "return confirm(\"此操作将删除所有已生成的内容页面，确定吗？\");");
        }


        public void Create_OnClick(object sender, EventArgs e)
        {
            if (!Page.IsPostBack || !Page.IsValid) return;

            var nodeIdList = new List<int>();
            var selectedNodeIdArrayList = ControlUtils.GetSelectedListControlValueArrayList(LbNodeIdList);

            var tableName = SiteInfo.TableName;

            if (DdlScope.SelectedValue == "Month")
            {
                var lastEditList = DataProvider.ContentDao.GetChannelIdListCheckedByLastEditDateHour(tableName, SiteId, 720);
                foreach (var nodeId in lastEditList)
                {
                    if (selectedNodeIdArrayList.Contains(nodeId.ToString()))
                    {
                        nodeIdList.Add(nodeId);
                    }
                }
            }
            else if (DdlScope.SelectedValue == "Day")
            {
                var lastEditList = DataProvider.ContentDao.GetChannelIdListCheckedByLastEditDateHour(tableName, SiteId, 24);
                foreach (var nodeId in lastEditList)
                {
                    if (selectedNodeIdArrayList.Contains(nodeId.ToString()))
                    {
                        nodeIdList.Add(nodeId);
                    }
                }
            }
            else if (DdlScope.SelectedValue == "2Hour")
            {
                var lastEditList = DataProvider.ContentDao.GetChannelIdListCheckedByLastEditDateHour(tableName, SiteId, 2);
                foreach (var nodeId in lastEditList)
                {
                    if (selectedNodeIdArrayList.Contains(nodeId.ToString()))
                    {
                        nodeIdList.Add(nodeId);
                    }
                }
            }
            else
            {
                nodeIdList = TranslateUtils.StringCollectionToIntList(TranslateUtils.ObjectCollectionToString(selectedNodeIdArrayList));
            }


            if (nodeIdList.Count == 0)
            {
                FailMessage("请首先选中希望生成内容页面的栏目！");
                return;
            }

            foreach (var nodeId in nodeIdList)
            {
                CreateManager.CreateAllContent(SiteId, nodeId);
            }

            PageCreateStatus.Redirect(SiteId);
        }

        public void BtnDeleteAll_OnClick(object sender, EventArgs e)
        {
            if (!Page.IsPostBack || !Page.IsValid) return;

            var url = PageProgressBar.GetDeleteAllPageUrl(SiteId, ETemplateType.ContentTemplate);
            PageUtils.RedirectToLoadingPage(url);
        }
    }
}
