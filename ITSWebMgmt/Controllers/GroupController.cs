﻿using ITSWebMgmt.Caches;
using System;

namespace ITSWebMgmt.Controllers
{
    public class GroupController : Controller<GroupADcache>
    {
        public string Description { get => ADcache.getProperty("description"); }
        public string Info { get => ADcache.getProperty("info"); }
        public string Name { get => ADcache.getProperty("name"); }
        public string ManagedBy { get => ADcache.getProperty("managedBy"); }
        public string GroupType { get => ADcache.getProperty("groupType").ToString(); }

        public GroupController(string adpath)
        {
            ADcache = new GroupADcache(adpath);
        }

        public bool isGroup()
        {
            ///XXX we expect a group check its a group
            return ADcache.DE.SchemaEntry.Name.Equals("group", StringComparison.CurrentCultureIgnoreCase);
        }
    }
}