﻿using System;
using System.Text;
using SiteServer.Utils;
using SiteServer.CMS.Controllers.Sys.Stl;
using SiteServer.CMS.Core;
using SiteServer.CMS.Model;
using SiteServer.CMS.Model.Enumerations;
using SiteServer.CMS.Plugin;
using SiteServer.CMS.Plugin.Model;
using SiteServer.CMS.StlParser.Model;
using SiteServer.CMS.StlParser.Utility;
using SiteServer.Utils.Enumerations;

namespace SiteServer.CMS.StlParser
{
	public class Parser
	{
		private Parser()
		{
		}

        public static void Parse(SiteInfo siteInfo, PageInfo pageInfo, ContextInfo contextInfo, StringBuilder contentBuilder, string filePath, bool isDynamic)
        {
            if (contentBuilder.Length > 0)
            {
                StlParserManager.ParseTemplateContent(contentBuilder, pageInfo, contextInfo);
            }

            if (EFileSystemTypeUtils.IsHtml(PathUtils.GetExtension(filePath)))
            {
                if (isDynamic)
                {
                    var pageUrl = PageUtils.AddProtocolToUrl(PageUtils.ParseNavigationUrl($"~/{PathUtils.GetPathDifference(WebConfigUtils.PhysicalApplicationPath, filePath)}"));
                    string templateString = $@"
<base href=""{pageUrl}"" />";
                    StringUtils.InsertAfter(new[] { "<head>", "<HEAD>" }, contentBuilder, templateString);
                }

                if (pageInfo.SiteInfo.Additional.IsCreateBrowserNoCache)
                {
                    const string templateString = @"
<META HTTP-EQUIV=""Pragma"" CONTENT=""no-cache"">
<META HTTP-EQUIV=""Expires"" CONTENT=""-1"">";
                    StringUtils.InsertAfter(new[] { "<head>", "<HEAD>" }, contentBuilder, templateString);
                }

                if (pageInfo.SiteInfo.Additional.IsCreateIe8Compatible)
                {
                    const string templateString = @"
<META HTTP-EQUIV=""x-ua-compatible"" CONTENT=""ie=7"" />";
                    StringUtils.InsertAfter(new[] { "<head>", "<HEAD>" }, contentBuilder, templateString);
                }

                if (pageInfo.SiteInfo.Additional.IsCreateJsIgnoreError)
                {
                    const string templateString = @"
<script type=""text/javascript"">window.onerror=function(){return true;}</script>";
                    StringUtils.InsertAfter(new[] { "<head>", "<HEAD>" }, contentBuilder, templateString);
                }

                if (pageInfo.PageContentId > 0 && pageInfo.SiteInfo.Additional.IsCountHits && !pageInfo.IsPageScriptsExists(PageInfo.Const.JsAdStlCountHits))
                {
                    pageInfo.AddPageEndScriptsIfNotExists(PageInfo.Const.JsAdStlCountHits, $@"
<script src=""{ActionsAddContentHits.GetUrl(pageInfo.ApiUrl, pageInfo.SiteId, pageInfo.PageNodeId, pageInfo.PageContentId)}"" type=""text/javascript""></script>");
                }

                var isShowPageInfo = pageInfo.SiteInfo.Additional.IsCreateShowPageInfo;

                if (!pageInfo.IsLocal)
                {
                    if (pageInfo.SiteInfo.Additional.IsCreateDoubleClick)
                    {
                        var fileTemplateId = 0;
                        if (pageInfo.TemplateInfo.TemplateType == ETemplateType.FileTemplate)
                        {
                            fileTemplateId = pageInfo.TemplateInfo.Id;
                        }

                        var apiUrl = pageInfo.ApiUrl;
                        var ajaxUrl = ActionsTrigger.GetUrl(apiUrl, pageInfo.SiteId, contextInfo.ChannelId,
                            contextInfo.ContentId, fileTemplateId, true);
                        pageInfo.AddPageEndScriptsIfNotExists("CreateDoubleClick", $@"
<script type=""text/javascript"" language=""javascript"">document.ondblclick=function(x){{location.href = '{ajaxUrl}&returnUrl=' + encodeURIComponent(location.search);}}</script>");
                    }
                }
                else
                {
                    isShowPageInfo = true;
                }

                if (isShowPageInfo)
                {
                    contentBuilder.Append($@"
<!-- {pageInfo.TemplateInfo.RelatedFileName}({ETemplateTypeUtils.GetText(pageInfo.TemplateInfo.TemplateType)}) -->");
                }

                var headScripts = StlParserManager.GetPageInfoHeadScript(pageInfo, contextInfo);
                if (!string.IsNullOrEmpty(headScripts))
                {
                    if (contentBuilder.ToString().IndexOf("</head>", StringComparison.Ordinal) != -1 || contentBuilder.ToString().IndexOf("</HEAD>", StringComparison.Ordinal) != -1)
                    {
                        StringUtils.InsertBefore(new[] { "</head>", "</HEAD>" }, contentBuilder, headScripts);
                    }
                    else
                    {
                        contentBuilder.Insert(0, headScripts);
                    }
                }

                var afterBodyScripts = StlParserManager.GetPageInfoScript(pageInfo, true);
                if (!string.IsNullOrEmpty(afterBodyScripts))
                {
                    if (contentBuilder.ToString().IndexOf("<body", StringComparison.Ordinal) != -1 || contentBuilder.ToString().IndexOf("<BODY", StringComparison.Ordinal) != -1)
                    {
                        var index = contentBuilder.ToString().IndexOf("<body", StringComparison.Ordinal);
                        if (index == -1)
                        {
                            index = contentBuilder.ToString().IndexOf("<BODY", StringComparison.Ordinal);
                        }
                        index = contentBuilder.ToString().IndexOf(">", index, StringComparison.Ordinal);
                        contentBuilder.Insert(index + 1, StringUtils.Constants.ReturnAndNewline + afterBodyScripts + StringUtils.Constants.ReturnAndNewline);
                    }
                    else
                    {
                        contentBuilder.Insert(0, afterBodyScripts);
                    }
                }

                var beforeBodyScripts = StlParserManager.GetPageInfoScript(pageInfo, false);
                if (!string.IsNullOrEmpty(beforeBodyScripts))
                {
                    if (contentBuilder.ToString().IndexOf("</body>", StringComparison.Ordinal) != -1 || contentBuilder.ToString().IndexOf("</BODY>", StringComparison.Ordinal) != -1)
                    {
                        var index = contentBuilder.ToString().IndexOf("</body>", StringComparison.Ordinal);
                        if (index == -1)
                        {
                            index = contentBuilder.ToString().IndexOf("</BODY>", StringComparison.Ordinal);
                        }
                        contentBuilder.Insert(index, StringUtils.Constants.ReturnAndNewline + beforeBodyScripts + StringUtils.Constants.ReturnAndNewline);
                    }
                    else
                    {
                        contentBuilder.Append(beforeBodyScripts);
                    }
                }

                if (pageInfo.PageEndScriptKeys.Count > 0)
                {
                    var endScriptBuilder = new StringBuilder();
                    foreach (string scriptKey in pageInfo.PageEndScriptKeys)
                    {
                        endScriptBuilder.Append(pageInfo.GetPageEndScripts(scriptKey));
                    }
                    endScriptBuilder.Append(StringUtils.Constants.ReturnAndNewline);

                    //contentBuilder.Append(endScriptBuilder.ToString());
                    //StringUtils.InsertBeforeOrAppend(new string[] { "</body>", "</BODY>" }, contentBuilder, endScriptBuilder.ToString());
                    StringUtils.InsertAfterOrAppend(new[] { "</html>", "</html>" }, contentBuilder, endScriptBuilder.ToString());
                }

                var renders = PluginManager.GetRenders();
                if (renders.Count <= 0) return;

                var html = contentBuilder.ToString();
                foreach (var pluginId in renders.Keys)
                {
                    var render = renders[pluginId];
                    try
                    {
                        var context = new PluginRenderContext(html, pageInfo.SiteId, pageInfo.PageNodeId, pageInfo.PageContentId);
                        html = render(context);
                    }
                    catch (Exception ex)
                    {
                        LogUtils.AddPluginErrorLog(pluginId, ex, "Render");
                    }
                }

                contentBuilder.Clear();
                contentBuilder.Append(html);
            }
        }
    }
}
