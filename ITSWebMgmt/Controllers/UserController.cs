﻿using ITSWebMgmt.Caches;
using Microsoft.Exchange.WebServices.Data;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Management;

namespace ITSWebMgmt.Controllers
{
    public class UserController : Controller<UserADcache>
    {
        private SCCMcache SCCMcache;

        public UserController()
        {
            //XXX this is not safe computerName is a use attibute, they might be able to change the value of this
            SCCMcache = new SCCMcache();
        }

        public override string adpath { get => ADcache.adpath; set { ADcache = new UserADcache(value); ADcache.adpath = value; } }
        public string Guid { get => ADcache.DE.Path; }
        public string UserPrincipalName { get => ADcache.getProperty("userPrincipalName"); }
        public string DisplayName { get => ADcache.getProperty("displayName"); }
        public string[] ProxyAddresses
        {
            get
            {
                var temp = ADcache.getProperty("proxyAddresses");
                return temp.GetType().Equals(typeof(string)) ? (new string[] { temp }) : temp;
            }
        }
        public int UserAccountControlComputed { get => ADcache.getProperty("msDS-User-Account-Control-Computed");}
        public int UserAccountControl { get => ADcache.getProperty("userAccountControl"); }
        public string UserPasswordExpiryTimeComputed{ get => ADcache.getProperty("msDS-UserPasswordExpiryTimeComputed"); }
        public string GivenName { get => ADcache.getProperty("givenName"); }
        public string SN { get => ADcache.getProperty("sn"); }
        public string AAUStaffID { get => ADcache.getProperty("aauStaffID").ToString(); }
        public string AAUStudentID { get => ADcache.getProperty("aauStudentID").ToString(); }
        public object Profilepath { get => ADcache.getProperty("profilepath"); }
        public string AAUUserClassification { get => ADcache.getProperty("aauUserClassification"); }
        public string AAUUserStatus { get => ADcache.getProperty("aauUserStatus").ToString(); }
        public string ScriptPath { get => ADcache.getProperty("scriptPath"); }
        public bool IsAccountLocked { get => ADcache.getProperty("IsAccountLocked"); }
        public string AAUAAUID { get => ADcache.getProperty("aauAAUID"); }
        public string AAUUUID { get => ADcache.getProperty("aauUUID"); }
        public string TelephoneNumber { get => ADcache.getProperty("telephoneNumber"); set => ADcache.saveProperty("telephoneNumber", value); }
        public string LastLogon { get => ADcache.getProperty("lastLogon"); }
        public string DistinguishedName { get => ADcache.getProperty("distinguishedName"); }
        public ManagementObjectCollection getUserMachineRelationshipFromUserName(string userName) => SCCMcache.getUserMachineRelationshipFromUserName(userName);

        public string[] getUserInfo()
        {
            return new string[]
            {
                UserPrincipalName,
                AAUAAUID,
                AAUUUID,
                AAUUserStatus,
                AAUStaffID,
                AAUStudentID,
                AAUUserClassification,
                TelephoneNumber,
                LastLogon
            };
        }

        public string globalSearch(string email)
        {
            DirectoryEntry de = new DirectoryEntry("GC://aau.dk");
            string filter = string.Format("(|(proxyaddresses=SMTP:{0})(userPrincipalName={0}))", email);

            DirectorySearcher search = new DirectorySearcher(de, filter);
            SearchResult r = search.FindOne();

            if (r != null)
            {
                //return r.Properties["userPrincipalName"][0].ToString(); //XXX handle if result is 0 (null exception)
                string adpath = r.Properties["ADsPath"][0].ToString();
                return adpath.Replace("aau.dk/", "").Replace("GC:", "LDAP:");
            }
            else
            {
                return null;
            }
        }

        //Searhces on a phone numer (internal or external), and returns a upn (later ADsPath) to a use or null if not found
        public string doPhoneSearch(string numberIn)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            string number = numberIn;
            //If number is a shot internal number, expand it :)
            if (number.Length == 4)
            {
                // format is 3452
                number = string.Format("+459940{0}", number);

            }
            else if (number.Length == 6)
            {
                //format is +453452 
                number = string.Format("+459940{0}", number.Replace("+45", ""));

            }
            else if (number.Length == 8)
            {
                //format is 99403442
                number = string.Format("+45{0}", number);

            } // else format is ok

            DirectoryEntry de = new DirectoryEntry("GC://aau.dk");
            //string filter = string.Format("(&(objectCategory=person)(telephoneNumber={0}))", number);
            string filter = string.Format("(&(objectCategory=person)(objectClass=user)(telephoneNumber={0}))", number);

            //logger.Debug(filter);

            DirectorySearcher search = new DirectorySearcher(de, filter);
            search.PropertiesToLoad.Add("userPrincipalName"); //Load something to speed up the object get?
            SearchResult r = search.FindOne();

            watch.Stop();
            System.Diagnostics.Debug.WriteLine("phonesearch took: " + watch.ElapsedMilliseconds);

            if (r != null)
            {
                //return r.Properties["ADsPath"][0].ToString(); //XXX handle if result is 0 (null exception)
                return r.Properties["ADsPath"][0].ToString().Replace("GC:", "LDAP:").Replace("aau.dk/", "");
            }
            else
            {
                return null;
            }
        }

        public bool userIsInRightOU()
        {
            string dn = DistinguishedName;
            string[] dnarray = dn.Split(',');

            string[] ou = dnarray.Where(x => x.StartsWith("ou=", StringComparison.CurrentCultureIgnoreCase)).ToArray();

            int count = ou.Count();
            if (count < 2)
            {
                return false;
            }
            //Check root is people
            if (!(ou[count - 1]).Equals("ou=people", StringComparison.CurrentCultureIgnoreCase))
            {
                //Error user is not placed in people!!!!! Cant move the user (might not be a real user or admin or computer)
                return false;
            }
            string[] okplaces = new string[3] { "ou=staff", "ou=guests", "ou=students" };
            if (!okplaces.Contains(ou[count - 2], StringComparer.OrdinalIgnoreCase))
            {
                //Error user is not in out staff, people or student, what is gowing on here?
                return false;
            }
            if (count > 2)
            {
                return false;
            }
            return true;
        }

        public bool fixUserOu()
        {
            if (userIsInRightOU()) { return false; }

            //See if it can be fixed!
            string dn = DistinguishedName;
            string[] dnarray = dn.Split(',');

            string[] ou = dnarray.Where(x => x.StartsWith("ou=", StringComparison.CurrentCultureIgnoreCase)).ToArray();

            int count = ou.Count();

            if (count < 2)
            {
                //This cant be in people/{staff,student,guest}
                return false;
            }
            //Check root is people
            if (!(ou[count - 1]).Equals("ou=people", StringComparison.CurrentCultureIgnoreCase))
            {
                //Error user is not placed in people!!!!! Cant move the user (might not be a real user or admin or computer)
                return false;
            }
            string[] okplaces = new string[3] { "ou=staff", "ou=guests", "ou=students" };
            if (!okplaces.Contains(ou[count - 2], StringComparer.OrdinalIgnoreCase))
            {
                //Error user is not in out staff, people or student, what is gowing on here?
                return false;
            }
            if (count > 2)
            {
                //User is not placed in people/{staff,student,guest}, but in a sub ou, we need to do somthing!
                //from above check we know the path is people/{staff,student,guest}, lets generate new OU

                //Format ldap://DOMAIN/pathtoOU
                //return false; //XX Return false here?

                string[] adpathsplit = adpath.ToLower().Replace("ldap://", "").Split('/');
                string protocol = "LDAP://";
                string domain = adpathsplit[0];
                string[] dcpath = (adpathsplit[0].Split(',')).Where<string>(s => s.StartsWith("dc=", StringComparison.CurrentCultureIgnoreCase)).ToArray<string>();

                string newOU = string.Format("{0},{1}", ou[count - 2], ou[count - 1]);
                string newPath = string.Format("{0}{1}/{2},{3}", protocol, string.Join(".", dcpath).Replace("dc=", ""), newOU, string.Join(",", dcpath));

                logger.Info("user " + System.Web.HttpContext.Current.User.Identity.Name + " changed OU on user to: " + newPath + " from " + adpath + ".");

                var newLocaltion = new DirectoryEntry(newPath);
                ADcache.DE.MoveTo(newLocaltion);

                return true;
            }
            //We don't need to do anything, user is placed in the right ou! (we think, can still be in wrong ou fx a guest changed to staff, we cant check that here) 
            logger.Debug("no need to change user {0} out, all is good", adpath);
            return true;
        }

        public string getADPathFromUsername(string username)
        {
            //XXX, this is a use input, might not be save us use in log 
            logger.Info("User {0} lookedup user {1}", System.Web.HttpContext.Current.User.Identity.Name, username);

            if (username.Contains("\\"))
            {
                //Form is domain\useranme
                var tmp = username.Split('\\');
                if (!tmp[0].Equals("AAU", StringComparison.CurrentCultureIgnoreCase))
                {
                    username = string.Format("{0}@{1}.aau.dk", tmp[1], tmp[0]);
                }
                else //IS AAU domain 
                {
                    username = string.Format("{0}@{1}.dk", tmp[1], tmp[0]);
                }
            }

            var adpath = globalSearch(username);
            if (adpath == null)
            {
                //Show user not found
                return null;
            }
            else
            {
                //We got ADPATH lets build the GUI
                return adpath;
            }
        }

        public void unlockUserAccount()
        {
            logger.Info("User {0} unlocked useraccont {1}", System.Web.HttpContext.Current.User.Identity.Name, adpath);
            
            ADcache.DE.Properties["LockOutTime"].Value = 0; //unlock account
            ADcache.DE.CommitChanges(); //may not be needed but adding it anyways
            ADcache.DE.Close();
        }

        public GetUserAvailabilityResults getFreeBusyResults()
        {
            ExchangeService service = new ExchangeService(ExchangeVersion.Exchange2010_SP2);
            service.UseDefaultCredentials = true; // Use domain account for connecting 
            //service.Credentials = new WebCredentials("user1@contoso.com", "password"); // used if we need to enter a password, but for now we are using domain credentials
            //service.AutodiscoverUrl("kyrke@its.aau.dk");  //XXX we should use the service user for webmgmt!
            service.Url = new Uri("https://mail.aau.dk/EWS/exchange.asmx");

            List<AttendeeInfo> attendees = new List<AttendeeInfo>();

            attendees.Add(new AttendeeInfo()
            {
                SmtpAddress = UserPrincipalName,
                AttendeeType = MeetingAttendeeType.Organizer
            });

            // Specify availability options.
            AvailabilityOptions myOptions = new AvailabilityOptions();

            myOptions.MeetingDuration = 30;
            myOptions.RequestedFreeBusyView = FreeBusyViewType.FreeBusy;

            // Return a set of free/busy times.
            DateTime dayBegin = DateTime.Now.Date;
            var window = new TimeWindow(dayBegin, dayBegin.AddDays(1));
            return service.GetUserAvailability(attendees, window, AvailabilityData.FreeBusy, myOptions);
        }

        public void toggle_userprofile()
        {
            //XXX log what the new value of profile is :)
            logger.Info("User {0} toggled romaing profile for user  {1}", System.Web.HttpContext.Current.User.Identity.Name, adpath);

            //string profilepath = (string)(ADcache.DE.Properties["profilePath"])[0];

            if (ADcache.DE.Properties.Contains("profilepath"))
            {
                ADcache.DE.Properties["profilePath"].Clear();
                ADcache.DE.CommitChanges();
            }
            else
            {
                string upn = UserPrincipalName;
                var tmp = upn.Split('@');

                string path = string.Format("\\\\{0}\\profiles\\{1}", tmp[1], tmp[0]);

                ADcache.DE.Properties["profilePath"].Add(path);
                ADcache.DE.CommitChanges();
            }
        }
    }
}