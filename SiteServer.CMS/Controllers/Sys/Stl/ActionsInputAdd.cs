﻿using SiteServer.Utils;

namespace SiteServer.CMS.Controllers.Sys.Stl
{
    public class ActionsInputAdd
    {
        public const string Route = "sys/stl/actions/input_add/{siteId}/{inputId}";

        public static string GetUrl(string apiUrl, int siteId, int inputId)
        {
            apiUrl = PageUtils.Combine(apiUrl, Route);
            apiUrl = apiUrl.Replace("{siteId}", siteId.ToString());
            apiUrl = apiUrl.Replace("{inputId}", inputId.ToString());
            return apiUrl;
        }
    }
}