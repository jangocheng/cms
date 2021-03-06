﻿using SiteServer.Utils;

namespace SiteServer.CMS.Controllers.Sys.Stl
{
    public class ActionsResumeAdd
    {
        public const string Route = "sys/stl/actions/resume_add/{siteId}";

        public static string GetUrl(string apiUrl, int siteId)
        {
            apiUrl = PageUtils.Combine(apiUrl, Route);
            apiUrl = apiUrl.Replace("{siteId}", siteId.ToString());
            return apiUrl;
        }
    }
}