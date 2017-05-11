using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minio.DataModel;
using System.IO;

namespace Minio.Tests
{
    /// <summary>
    /// Summary description for NotificationTest
    /// </summary>
    [TestClass]
    public class NotificationTest
    {
        [TestMethod]
        public void TestNotificationStringHydration()        {
            
     
            string notificationString = "<NotificationConfiguration xmlns=\"http://s3.amazonaws.com/doc/2006-03-01/\"><TopicConfiguration><Id>YjVkM2Y0YmUtNGI3NC00ZjQyLWEwNGItNDIyYWUxY2I0N2M4 </Id><Arn>arnstring</Arn><Topic> arn:aws:sns:us-east-1:account-id:s3notificationtopic2 </Topic><Event> s3:ReducedRedundancyLostObject </Event><Event> s3:ObjectCreated: *</Event></TopicConfiguration></NotificationConfiguration>";

            try
            {
                var contentBytes = System.Text.Encoding.UTF8.GetBytes(notificationString);
                using (var stream = new MemoryStream(contentBytes))
                {
                    BucketNotification notification = (BucketNotification)(new System.Xml.Serialization.XmlSerializer(typeof(BucketNotification)).Deserialize(stream));
                    Assert.AreEqual(1, notification.TopicConfigs.Count);
                }
            }
            catch (Exception ex)
            {
                Assert.Fail();
            }
        }

       

       
    }
}
