﻿using ContosoMoments.Common.Notification;
using ContosoMoments.MobileServer.DataLogic;
using ContosoMoments.MobileServer.Models;
using Microsoft.Azure.Mobile.Server.Config;
using Microsoft.Azure.NotificationHubs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace ContosoMoments.MobileServer.Controllers.WebAPI
{
    [MobileAppController]
    public class LikeController : ApiController
    {
        // POST: api/Like
        public async Task<bool> Post([FromBody]Dictionary<string,string> image)
        {
            var logic = new ImageBusinessLogic();

            var img = logic.GetImage(image["imageId"]);
            if (null != img)
            {
                var ctx = new MobileServiceContext();
                var registrations = ctx.DeviceRegistrations.Where(x => x.UserId == img.UserId);

                //Send plat-specific message to all installation one by one
                string message = "Someone has liked your image";
                foreach (var registration in registrations)
                {
                    var tags = new string[1] { string.Format("$InstallationId:{{{0}}}", registration.InstallationId) };
                    switch (registration.Platform)
                    {
                        case NotificationPlatform.Wns:
                            await Notifier.Instance.sendWindowsStoreNotification(message, tags);
                            break;
                        case NotificationPlatform.Apns:
                            await Notifier.Instance.sendIOSNotification(message, tags);
                            break;
                        case NotificationPlatform.Mpns:
                            await Notifier.Instance.sendWPNotification(message, tags);
                            break;
                        case NotificationPlatform.Gcm:
                            await Notifier.Instance.sendGCMNotification(message, tags);
                            break;
                        case NotificationPlatform.Adm:
                            //NOT SUPPORTED
                            break;
                        default:
                            break;
                    }
                }
            }

            return true;
        }
    }
}