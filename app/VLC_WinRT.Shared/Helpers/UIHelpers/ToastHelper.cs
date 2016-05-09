﻿using System.Linq;
using System.Reflection;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace VLC_WinRT.Helpers
{
    public static class ToastHelper
    {
        public static void Basic(string msg, bool playJingle = false, string toastId = "")
        {
            ToastTemplateType toastTemplate = ToastTemplateType.ToastText01;
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);
            XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");
            toastTextElements[0].AppendChild(toastXml.CreateTextNode(msg));
            if (!playJingle)
            {
                IXmlNode toastNode = toastXml.SelectSingleNode("/toast"); 
                XmlElement audio = toastXml.CreateElement("audio"); 
                audio.SetAttribute("silent", "true");
                toastNode?.AppendChild(audio);
            }

            ToastNotification toast = new ToastNotification(toastXml);
            var nameProperty = toast.GetType().GetRuntimeProperties().FirstOrDefault(x => x.Name == "Tag");
            if (nameProperty != null && !string.IsNullOrEmpty(toastId))
            {
               nameProperty.SetValue(toast, toastId);
            }
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        public static void ToastImageAndText04(string t1, string t2, string t3, string imgsrc, string imgalt = "")
        {
            ToastTemplateType toastTemplate = ToastTemplateType.ToastImageAndText04;
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);
            XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");
            toastTextElements[0].AppendChild(toastXml.CreateTextNode(t1));
            if(t2 != null)
                toastTextElements[1].AppendChild(toastXml.CreateTextNode(t2));
            if(t3 != null)
                toastTextElements[2].AppendChild(toastXml.CreateTextNode(t3));

            if (!string.IsNullOrEmpty(imgsrc))
            {
                XmlNodeList toastImgElement = toastXml.GetElementsByTagName("image");
                ((XmlElement)toastImgElement[0]).SetAttribute("src", imgsrc);
                ((XmlElement)toastImgElement[0]).SetAttribute("alt", imgalt);
            }
            IXmlNode toastNode = toastXml.SelectSingleNode("/toast");
            XmlElement audio = toastXml.CreateElement("audio");
            audio.SetAttribute("silent", "true");
            toastNode?.AppendChild(audio);

            ToastNotification toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
    }
}
