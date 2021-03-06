﻿using System;
using SiteServer.Utils;
using SiteServer.CMS.Model;
using System.Collections.Generic;
using SiteServer.CMS.Model.Enumerations;
using SiteServer.Utils.Enumerations;

namespace SiteServer.CMS.Core
{
	public class TemplateManager
	{
        private TemplateManager()
		{
        }

        private const string CacheKey = "SiteServer.CMS.Core.TemplateManager";
        private static readonly object SyncRoot = new object();

        public static TemplateInfo GetTemplateInfo(int siteId, int templateId)
        {
            TemplateInfo templateInfo = null;
            var templateInfoDictionary = GetTemplateInfoDictionaryBySiteId(siteId);

            if (templateInfoDictionary != null && templateInfoDictionary.ContainsKey(templateId))
            {
                templateInfo = templateInfoDictionary[templateId];
            }
            return templateInfo;
        }

        public static string GetCreatedFileFullName(int siteId, int templateId)
        {
            var createdFileFullName = string.Empty;

            var templateInfo = GetTemplateInfo(siteId, templateId);
            if (templateInfo != null)
            {
                createdFileFullName = templateInfo.CreatedFileFullName;
            }

            return createdFileFullName;
        }

        public static string GetTemplateName(int siteId, int templateId)
        {
            var templateName = string.Empty;

            var templateInfo = GetTemplateInfo(siteId, templateId);
            if (templateInfo != null)
            {
                templateName = templateInfo.TemplateName;
            }

            return templateName;
        }

        public static TemplateInfo GetTemplateInfoByTemplateName(int siteId, ETemplateType templateType, string templateName)
        {
            TemplateInfo info = null;

            var templateInfoDictionary = GetTemplateInfoDictionaryBySiteId(siteId);
            if (templateInfoDictionary != null)
            {
                foreach (var templateInfo in templateInfoDictionary.Values)
                {
                    if (templateInfo.TemplateType == templateType && templateInfo.TemplateName == templateName)
                    {
                        info = templateInfo;
                        break;
                    }
                }
            }

            return info;
        }

        public static TemplateInfo GetDefaultTemplateInfo(int siteId, ETemplateType templateType)
        {
            TemplateInfo info = null;

            var templateInfoDictionary = GetTemplateInfoDictionaryBySiteId(siteId);
            if (templateInfoDictionary != null)
            {
                foreach (var templateInfo in templateInfoDictionary.Values)
                {
                    if (templateInfo.TemplateType == templateType && templateInfo.IsDefault)
                    {
                        info = templateInfo;
                        break;
                    }
                }
            }

            return info;
        }

        public static int GetDefaultTemplateId(int siteId, ETemplateType templateType)
        {
            var id = 0;

            var templateInfoDictionary = GetTemplateInfoDictionaryBySiteId(siteId);
            if (templateInfoDictionary != null)
            {
                foreach (var templateInfo in templateInfoDictionary.Values)
                {
                    if (templateInfo.TemplateType == templateType && templateInfo.IsDefault)
                    {
                        id = templateInfo.Id;
                        break;
                    }
                }
            }

            return id;
        }

        public static int GetTemplateIdByTemplateName(int siteId, ETemplateType templateType, string templateName)
        {
            var id = 0;

            var templateInfoDictionary = GetTemplateInfoDictionaryBySiteId(siteId);
            if (templateInfoDictionary != null)
            {
                foreach (var templateInfo in templateInfoDictionary.Values)
                {
                    if (templateInfo.TemplateType == templateType && templateInfo.TemplateName == templateName)
                    {
                        id = templateInfo.Id;
                        break;
                    }
                }
            }

            return id;
        }

        public static List<int> GetAllFileTemplateIdList(int siteId)
        {
            var list = new List<int>();

            var templateInfoDictionary = GetTemplateInfoDictionaryBySiteId(siteId);
            if (templateInfoDictionary == null) return list;

            foreach (var templateInfo in templateInfoDictionary.Values)
            {
                if (templateInfo.TemplateType == ETemplateType.FileTemplate)
                {
                    list.Add(templateInfo.Id);
                }
            }

            return list;
        }

	    private static Dictionary<int, TemplateInfo> GetTemplateInfoDictionaryBySiteId(int siteId, bool flush = false)
        {
            var dictionary = GetCacheDictionary();

            Dictionary<int, TemplateInfo> templateInfoDictionary = null;

            if (!flush && dictionary.ContainsKey(siteId))
            {
                templateInfoDictionary = dictionary[siteId];
            }

            if (templateInfoDictionary == null)
            {
                templateInfoDictionary = DataProvider.TemplateDao.GetTemplateInfoDictionaryBySiteId(siteId);

                if (templateInfoDictionary != null)
                {
                    UpdateCache(dictionary, templateInfoDictionary, siteId);
                }
            }
            return templateInfoDictionary;
        }

        private static void UpdateCache(Dictionary<int, Dictionary<int, TemplateInfo>> dictionary, Dictionary<int, TemplateInfo> templateInfoDictionary, int siteId)
        {
            lock (SyncRoot)
            {
                dictionary[siteId] = templateInfoDictionary;
            }
        }

        public static void RemoveCache(int siteId)
        {
            var dictionary = GetCacheDictionary();

            lock (SyncRoot)
            {
                dictionary.Remove(siteId);
            }
        }

        private static Dictionary<int, Dictionary<int, TemplateInfo>> GetCacheDictionary()
        {
            var dictionary = CacheUtils.Get(CacheKey) as Dictionary<int, Dictionary<int, TemplateInfo>>;
            if (dictionary == null)
            {
                dictionary = new Dictionary<int, Dictionary<int, TemplateInfo>>();
                CacheUtils.InsertHours(CacheKey, dictionary, 24);
            }
            return dictionary;
        }

        public static string GetTemplateFilePath(SiteInfo siteInfo, TemplateInfo templateInfo)
        {
            string filePath;
            if (templateInfo.TemplateType == ETemplateType.IndexPageTemplate)
            {
                filePath = PathUtils.Combine(WebConfigUtils.PhysicalApplicationPath, siteInfo.SiteDir, templateInfo.RelatedFileName);
            }
            else if (templateInfo.TemplateType == ETemplateType.ContentTemplate)
            {
                filePath = PathUtils.Combine(WebConfigUtils.PhysicalApplicationPath, siteInfo.SiteDir, DirectoryUtils.PublishmentSytem.Template, DirectoryUtils.PublishmentSytem.Content, templateInfo.RelatedFileName);
            }
            else
            {
                filePath = PathUtils.Combine(WebConfigUtils.PhysicalApplicationPath, siteInfo.SiteDir, DirectoryUtils.PublishmentSytem.Template, templateInfo.RelatedFileName);
            }
            return filePath;
        }

	    public static TemplateInfo GetIndexPageTemplateInfo(int siteId)
	    {
	        var templateId = GetDefaultTemplateId(siteId, ETemplateType.IndexPageTemplate);
            TemplateInfo templateInfo = null;
            if (templateId != 0)
            {
                templateInfo = GetTemplateInfo(siteId, templateId);
            }

            return templateInfo ?? GetDefaultTemplateInfo(siteId, ETemplateType.IndexPageTemplate);
        }

        public static TemplateInfo GetChannelTemplateInfo(int siteId, int channelId)
        {
            var templateId = 0;
            if (siteId == channelId)
            {
                templateId = GetDefaultTemplateId(siteId, ETemplateType.IndexPageTemplate);
            }
            else
            {
                var nodeInfo = ChannelManager.GetChannelInfo(siteId, channelId);
                if (nodeInfo != null)
                {
                    templateId = nodeInfo.ChannelTemplateId;
                }
            }

            TemplateInfo templateInfo = null;
            if (templateId != 0)
            {
                templateInfo = GetTemplateInfo(siteId, templateId);
            }

            return templateInfo ?? GetDefaultTemplateInfo(siteId, ETemplateType.ChannelTemplate);
        }

        public static TemplateInfo GetContentTemplateInfo(int siteId, int channelId)
        {
            var templateId = 0;
            var nodeInfo = ChannelManager.GetChannelInfo(siteId, channelId);
            if (nodeInfo != null)
            {
                templateId = nodeInfo.ContentTemplateId;
            }

            TemplateInfo templateInfo = null;
            if (templateId != 0)
            {
                templateInfo = GetTemplateInfo(siteId, templateId);
            }

            return templateInfo ?? GetDefaultTemplateInfo(siteId, ETemplateType.ContentTemplate);
        }

        public static TemplateInfo GetFileTemplateInfo(int siteId, int fileTemplateId)
        {
            var templateId = fileTemplateId;

            TemplateInfo templateInfo = null;
            if (templateId != 0)
            {
                templateInfo = GetTemplateInfo(siteId, templateId);
            }

            return templateInfo ?? GetDefaultTemplateInfo(siteId, ETemplateType.FileTemplate);
        }

        public static void WriteContentToTemplateFile(SiteInfo siteInfo, TemplateInfo templateInfo, string content, string administratorName)
        {
            if (content == null) content = string.Empty;
            var filePath = GetTemplateFilePath(siteInfo, templateInfo);
            FileUtils.WriteText(filePath, templateInfo.Charset, content);

            if (templateInfo.Id > 0)
            {
                var logInfo = new TemplateLogInfo(0, templateInfo.Id, templateInfo.SiteId, DateTime.Now, administratorName, content.Length, content);
                DataProvider.TemplateLogDao.Insert(logInfo);
            }
        }

        public static void UpdateChannelTemplateId(int siteId, int nodeId, int channelTemplateId)
        {
            DataProvider.ChannelDao.UpdateChannelTemplateId(nodeId, channelTemplateId);
        }

        public static void UpdateContentTemplateId(int siteId, int nodeId, int contentTemplateId)
        {
            DataProvider.ChannelDao.UpdateContentTemplateId(nodeId, contentTemplateId);
        }

        public static int GetIndexTempalteId(int siteId)
        {
            return GetDefaultTemplateId(siteId, ETemplateType.IndexPageTemplate);
        }

        public static int GetChannelTempalteId(int siteId, int nodeId)
        {
            var templateId = 0;

            var nodeInfo = ChannelManager.GetChannelInfo(siteId, nodeId);
            if (nodeInfo != null)
            {
                templateId = nodeInfo.ChannelTemplateId;
            }

            if (templateId == 0)
            {
                templateId = GetDefaultTemplateId(siteId, ETemplateType.ChannelTemplate);
            }

            return templateId;
        }

        public static int GetContentTempalteId(int siteId, int nodeId)
        {
            var templateId = 0;

            var nodeInfo = ChannelManager.GetChannelInfo(siteId, nodeId);
            if (nodeInfo != null)
            {
                templateId = nodeInfo.ContentTemplateId;
            }

            if (templateId == 0)
            {
                templateId = GetDefaultTemplateId(siteId, ETemplateType.ContentTemplate);
            }

            return templateId;
        }

        public static string GetTemplateContent(SiteInfo siteInfo, TemplateInfo templateInfo)
        {
            var filePath = GetTemplateFilePath(siteInfo, templateInfo);
            return GetContentByFilePath(filePath, templateInfo.Charset);
        }

        public static string GetIncludeContent(SiteInfo siteInfo, string file, ECharset charset)
        {
            var filePath = PathUtility.MapPath(siteInfo, PathUtility.AddVirtualToPath(file));
            return GetContentByFilePath(filePath, charset);
        }

        public static string GetContentByFilePath(string filePath, ECharset charset = ECharset.utf_8)
        {
            try
            {
                if (CacheUtils.Get(filePath) != null) return CacheUtils.Get(filePath) as string;

                var content = string.Empty;
                if (FileUtils.IsFileExists(filePath))
                {
                    content = FileUtils.ReadText(filePath, charset);
                }

                CacheUtils.Insert(filePath, content, TimeSpan.FromHours(12), filePath);
                return content;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
