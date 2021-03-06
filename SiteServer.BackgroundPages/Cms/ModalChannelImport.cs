﻿using System;
using System.Collections.Specialized;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using SiteServer.Utils;
using SiteServer.CMS.Core;
using SiteServer.CMS.ImportExport;
using SiteServer.Utils.Enumerations;

namespace SiteServer.BackgroundPages.Cms
{
	public class ModalChannelImport : BasePageCms
    {
        protected DropDownList DdlParentNodeId;
		public HtmlInputFile HifFile;
		public DropDownList DdlIsOverride;

        private bool[] _isLastNodeArray;

        public static string GetOpenWindowString(int siteId, int nodeId)
        {
            return LayerUtils.GetOpenScript("导入栏目",
                PageUtils.GetCmsUrl(siteId, nameof(ModalChannelImport), new NameValueCollection
                {
                    {"NodeID", nodeId.ToString()}
                }), 600, 300);
        }

        public void Page_Load(object sender, EventArgs e)
        {
            if (IsForbidden) return;

            if (IsPostBack) return;

            var nodeId = Body.GetQueryInt("NodeID", SiteId);
            var nodeIdList = DataProvider.ChannelDao.GetIdListBySiteId(SiteId);
            var nodeCount = nodeIdList.Count;
            _isLastNodeArray = new bool[nodeCount];
            foreach (var theNodeId in nodeIdList)
            {
                var nodeInfo = ChannelManager.GetChannelInfo(SiteId, theNodeId);
                var itemNodeId = nodeInfo.Id;
                var nodeName = nodeInfo.ChannelName;
                var parentsCount = nodeInfo.ParentsCount;
                var isLastNode = nodeInfo.IsLastNode;
                var value = IsOwningNodeId(itemNodeId) ? itemNodeId.ToString() : string.Empty;
                value = (nodeInfo.Additional.IsChannelAddable) ? value : string.Empty;
                if (!string.IsNullOrEmpty(value))
                {
                    if (!HasChannelPermissions(theNodeId, AppManager.Permissions.Channel.ChannelAdd))
                    {
                        value = string.Empty;
                    }
                }
                var listitem = new ListItem(GetTitle(itemNodeId, nodeName, parentsCount, isLastNode), value);
                if (itemNodeId == nodeId)
                {
                    listitem.Selected = true;
                }
                DdlParentNodeId.Items.Add(listitem);
            }
        }

        public string GetTitle(int nodeId, string nodeName, int parentsCount, bool isLastNode)
        {
            var str = "";
            if (nodeId == SiteId)
            {
                isLastNode = true;
            }
            if (isLastNode == false)
            {
                _isLastNodeArray[parentsCount] = false;
            }
            else
            {
                _isLastNodeArray[parentsCount] = true;
            }
            for (var i = 0; i < parentsCount; i++)
            {
                str = string.Concat(str, _isLastNodeArray[i] ? "　" : "│");
            }
            str = string.Concat(str, isLastNode ? "└" : "├");
            str = string.Concat(str, nodeName);
            return str;
        }

        public override void Submit_OnClick(object sender, EventArgs e)
        {
			if (HifFile.PostedFile != null && "" != HifFile.PostedFile.FileName)
			{
				var filePath = HifFile.PostedFile.FileName;
                if (!EFileSystemTypeUtils.IsZip(PathUtils.GetExtension(filePath)))
				{
                    FailMessage("必须上传Zip压缩文件");
					return;
				}

				try
				{
                    var localFilePath = PathUtils.GetTemporaryFilesPath(PathUtils.GetFileName(filePath));

                    HifFile.PostedFile.SaveAs(localFilePath);

					var importObject = new ImportObject(SiteId);
                    importObject.ImportChannelsAndContentsByZipFile(TranslateUtils.ToInt(DdlParentNodeId.SelectedValue), localFilePath, TranslateUtils.ToBool(DdlIsOverride.SelectedValue));

                    Body.AddSiteLog(SiteId, "导入栏目");

                    LayerUtils.Close(Page);
				}
				catch(Exception ex)
				{
                    FailMessage(ex, "导入栏目失败！");
				}
			}
		}
	}
}
