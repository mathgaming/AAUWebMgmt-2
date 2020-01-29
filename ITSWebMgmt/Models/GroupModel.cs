﻿using ITSWebMgmt.Caches;
using ITSWebMgmt.Controllers;
using ITSWebMgmt.Helpers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Text;
using System.Web;

namespace ITSWebMgmt.Models
{
    public class GroupModel : WebMgmtModel<GroupADcache>
    {
        public string Description { get => ADcache.getProperty("description"); }
        public string Info { get => ADcache.getProperty("info"); }
        public string Name { get => ADcache.getProperty("name"); }
        public string ADManagedBy { get => ADcache.getProperty("managedBy"); }
        public string GroupType { get => ADcache.getProperty("groupType").ToString(); }
        public string DistinguishedName { get => ADcache.getProperty("distinguishedName").ToString(); }
        public string Title { get; set; }
        public string Domain { get; set; }
        public string ManagedBy { get; set; }
        public string SecurityGroup { get; set; }
        public string GroupScope { get; set; }
        public TableModel GroupTable { get; set; }
        public TableModel GroupAllTable { get; set; }
        public TableModel GroupOfTable { get; set; }
        public TableModel GroupOfAllTable { get; set; }
        public bool IsFileShare { get; set; } = false;

        public GroupModel(string adpath)
        {
            ADcache = new GroupADcache(adpath);
            var sb = new StringBuilder();

            var dom = Path.Split(',').Where(s => s.StartsWith("DC=")).ToArray()[0].Replace("DC=", "");
            Domain = dom;

            string managedByString = "none";
            if (ADManagedBy != "")
            {
                var manager = ADManagedBy;

                var ldapSplit = manager.Split(',');
                var name = ldapSplit[0].Replace("CN=", "");
                var domain = ldapSplit.Where(s => s.StartsWith("DC=")).ToArray()[0].Replace("DC=", "");

                managedByString = string.Format("<a href=\"/Home/Redirector?adpath={0}\">{1}</a>", HttpUtility.HtmlEncode("LDAP://" + manager), domain + "\\" + name);
            }
            ManagedBy = managedByString;

            //IsDistributionGroup?
            //ManamgedBy

            var isDistgrp = false;
            string groupType = ""; //domain Local, Global, Universal

            var gt = GroupType;
            switch (gt)
            {
                case "2":
                    isDistgrp = true;
                    groupType = "Global";
                    break;
                case "4":
                    groupType = "Domain local";
                    isDistgrp = true;
                    break;
                case "8":
                    groupType = "Universal";
                    isDistgrp = true;
                    break;
                case "-2147483646":
                    groupType = "Global";
                    break;
                case "-2147483644":
                    groupType = "Domain local";
                    break;
                case "-2147483640":
                    groupType = "Universal";
                    break;
            }

            SecurityGroup = (!isDistgrp).ToString();
            GroupScope = groupType;

            //TODO: IsRequrceGroup (is exchange, fileshare or other resource type group?)
        }
    }
}