﻿using System.Collections.Generic;
using System.Collections.Specialized;
using Atom.Core;
using SiteServer.Utils;
using SiteServer.CMS.Core;
using SiteServer.CMS.Model;

namespace SiteServer.CMS.ImportExport.Components
{
	internal class TableIe
	{
		private readonly string _directoryPath;

		public TableIe(string directoryPath)
		{
			_directoryPath = directoryPath;
		}

		public void ExportAuxiliaryTable(string tableName)
		{
            var tableInfo = DataProvider.TableDao.GetTableCollectionInfo(tableName);
			if (tableInfo != null)
			{
                var metaInfoList = TableMetadataManager.GetTableMetadataInfoList(tableInfo.TableName);
				var filePath = _directoryPath + PathUtils.SeparatorChar + tableInfo.TableName + ".xml";

				var feed = GetAtomFeed(tableInfo);

				foreach (var metaInfo in metaInfoList)
				{
					var entry = GetAtomEntry(metaInfo);
					feed.Entries.Add(entry);
				}
				feed.Save(filePath);
			}
		}

		private static AtomFeed GetAtomFeed(TableInfo tableInfo)
		{
			var feed = AtomUtility.GetEmptyFeed();

			AtomUtility.AddDcElement(feed.AdditionalElements, "TableName", tableInfo.TableName);
			AtomUtility.AddDcElement(feed.AdditionalElements, "DisplayName", tableInfo.DisplayName);
			AtomUtility.AddDcElement(feed.AdditionalElements, "AttributeNum", tableInfo.AttributeNum.ToString());
            AtomUtility.AddDcElement(feed.AdditionalElements, "IsCreatedInDB", tableInfo.IsCreatedInDb.ToString());
            AtomUtility.AddDcElement(feed.AdditionalElements, "IsChangedAfterCreatedInDB", tableInfo.IsChangedAfterCreatedInDb.ToString());
			AtomUtility.AddDcElement(feed.AdditionalElements, "Description", tableInfo.Description);
            //表唯一序列号
            AtomUtility.AddDcElement(feed.AdditionalElements, "SerializedString", TableMetadataManager.GetSerializedString(tableInfo.TableName));

			return feed;
		}

		private static AtomEntry GetAtomEntry(TableMetadataInfo metaInfo)
		{
			var entry = AtomUtility.GetEmptyEntry();

			AtomUtility.AddDcElement(entry.AdditionalElements, "Id", metaInfo.Id.ToString());
			AtomUtility.AddDcElement(entry.AdditionalElements, "TableName", metaInfo.TableName);
			AtomUtility.AddDcElement(entry.AdditionalElements, "AttributeName", metaInfo.AttributeName);
			AtomUtility.AddDcElement(entry.AdditionalElements, "DataType", metaInfo.DataType.Value);
			AtomUtility.AddDcElement(entry.AdditionalElements, "DataLength", metaInfo.DataLength.ToString());
			AtomUtility.AddDcElement(entry.AdditionalElements, "Taxis", metaInfo.Taxis.ToString());
            AtomUtility.AddDcElement(entry.AdditionalElements, "IsSystem", metaInfo.IsSystem.ToString());

			return entry;
		}


        /// <summary>
        /// 将频道模板中的辅助表导入发布系统中，返回修改了的表名对照
        /// 在导入辅助表的同时检查发布系统辅助表并替换对应表
        /// </summary>
        public NameValueCollection ImportAuxiliaryTables(int siteId, bool isUserTables)
		{
			if (!DirectoryUtils.IsDirectoryExists(_directoryPath)) return null;

            var siteInfo = SiteManager.GetSiteInfo(siteId);

            var nameValueCollection = new NameValueCollection();
            var tableNamePrefix = siteInfo.SiteDir + "_";

			var filePaths = DirectoryUtils.GetFilePaths(_directoryPath);

            foreach (var filePath in filePaths)
            {
                var feed = AtomFeed.Load(FileUtils.GetFileStreamReadOnly(filePath));

                var tableName = AtomUtility.GetDcElementContent(feed.AdditionalElements, "TableName");

                if (!isUserTables)
                {
                    nameValueCollection[tableName] = siteInfo.TableName;
                    continue;
                }

                var displayName = AtomUtility.GetDcElementContent(feed.AdditionalElements, "DisplayName");
                var serializedString = AtomUtility.GetDcElementContent(feed.AdditionalElements, "SerializedString");

                var tableNameToInsert = string.Empty;//需要增加的表名，空代表不需要添加辅助表

                var tableInfo = DataProvider.TableDao.GetTableCollectionInfo(tableName);
                if (tableInfo == null)//如果当前系统无此表名
                {
                    tableNameToInsert = tableName;
                }
                else
                {
                    var serializedStringForExistTable = TableMetadataManager.GetSerializedString(tableName);

                    if (!string.IsNullOrEmpty(serializedString))
                    {
                        if (serializedString != serializedStringForExistTable)//仅有此时，导入表需要修改表名
                        {
                            tableNameToInsert = tableNamePrefix + tableName;
                            displayName = tableNamePrefix + displayName;
                            nameValueCollection[tableName] = tableNameToInsert;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(tableNameToInsert))//需要添加
                {
                    if (!DataProvider.DatabaseDao.IsTableExists(tableNameToInsert))
                    {
                        tableInfo = new TableInfo
                        {
                            TableName = tableNameToInsert,
                            DisplayName = displayName,
                            AttributeNum = 0,
                            IsCreatedInDb = false,
                            IsChangedAfterCreatedInDb = false,
                            Description = AtomUtility.GetDcElementContent(feed.AdditionalElements, "Description")
                        };

                        var metadataInfoList = new List<TableMetadataInfo>();
                        foreach (AtomEntry entry in feed.Entries)
                        {
                            var metaInfo = new TableMetadataInfo
                            {
                                TableName = tableNameToInsert,
                                AttributeName = AtomUtility.GetDcElementContent(entry.AdditionalElements, nameof(TableMetadataInfo.AttributeName)),
                                DataType = DataTypeUtils.GetEnumType(AtomUtility.GetDcElementContent(entry.AdditionalElements, nameof(TableMetadataInfo.DataType))),
                                DataLength = TranslateUtils.ToInt(AtomUtility.GetDcElementContent(entry.AdditionalElements, nameof(TableMetadataInfo.DataLength))),
                                Taxis = TranslateUtils.ToInt(AtomUtility.GetDcElementContent(entry.AdditionalElements, nameof(TableMetadataInfo.Taxis))),
                                IsSystem = TranslateUtils.ToBool(AtomUtility.GetDcElementContent(entry.AdditionalElements, nameof(TableMetadataInfo.IsSystem)))
                            };

                            if (string.IsNullOrEmpty(metaInfo.AttributeName) ||
                                ContentAttribute.AllAttributesLowercase.Contains(metaInfo.AttributeName.ToLower()))
                                continue;

                            metadataInfoList.Add(metaInfo);
                        }

                        DataProvider.TableDao.Insert(tableInfo, metadataInfoList);

                        DataProvider.TableDao.CreateDbTable(tableNameToInsert);
                    }
                }

                var tableNameToChange = !string.IsNullOrEmpty(tableNameToInsert) ? tableNameToInsert : tableName;
                //更新发布系统后台内容表及栏目表
                siteInfo.TableName = tableNameToChange;
                DataProvider.SiteDao.Update(siteInfo);
            }

            return nameValueCollection;
		}

	}
}
