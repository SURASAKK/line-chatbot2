using Line.Messaging;
using Line.Messaging.Webhooks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using centralloggerbot.CloudStorage;
using centralloggerbot.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace centralloggerbot
{
    internal class LineBotApp : WebhookApplication
    {
        private LineMessagingClient messagingClient { get; }
        private TableStorage<EventSourceState> sourceState { get; }
        private BlobStorage blobStorage { get; }
        private string text;

        public LineBotApp(string text, LineMessagingClient lineMessagingClient, TableStorage<EventSourceState> tableStorage, BlobStorage blobStorage)
        {
            this.text = text;
            this.messagingClient = lineMessagingClient;
            this.sourceState = tableStorage;
            this.blobStorage = blobStorage;
        }
        protected override async Task OnMessageAsync(MessageEvent ev)
        {
            switch (ev.Message.Type)
            {
                case EventMessageType.Text:
                    await HandleTextAsync(ev.ReplyToken, ((TextEventMessage)ev.Message).Text, ev.Source.UserId);
                    break;
                case EventMessageType.Sticker:
                    await ReplyRandomStickerAsync(ev.ReplyToken);
                    break;
            }
        }
        private async Task ReplyRandomStickerAsync(string replyToken)
        {
            //Sticker ID of bssic stickers (packge ID =1)
            //see https://devdocs.line.me/files/sticker_list.pdf
            var stickerids = Enumerable.Range(1, 17)
                .Concat(Enumerable.Range(21, 1))
                .Concat(Enumerable.Range(100, 139 - 100 + 1))
                .Concat(Enumerable.Range(401, 430 - 400 + 1)).ToArray();

            var rand = new Random(Guid.NewGuid().GetHashCode());
            var stickerId = stickerids[rand.Next(stickerids.Length - 1)].ToString();
            await messagingClient.ReplyMessageAsync(replyToken, new[] {
                        new StickerMessage("1", stickerId)
                    });
        }
        private async Task HandleTextAsync(string replyToken, string userMessage, string userId)
        {
            var replyMessage = new TextMessage($"ขอบคุณสำหรับข้อความ! ขออภัย เราไม่สามารถตอบกลับผู้ใช้ เป็นส่วนตัวได้จากบัญชีนี้ ถ้าหากคุณต้องการสมัครการแจ้งเตือน log ให้พิมพ์ว่า  \"sub\" และถ้าหากจะยกเลิกการติดตามให้พิมพ์  \"unsub\" ขอบคุณครับ ");
            if (userMessage.ToLower() == "hello")
            {
                replyMessage.Text = "Hi!!";
            }
            if (userMessage.ToLower() == "message")
            {
                replyMessage.Text = $"You say{userMessage}";
            }
            if (userMessage.ToLower() == "test unit")
            {
                var text = userMessage.Substring(userMessage.IndexOf(' ') + 1, userMessage.Length);
                replyMessage.Text = $"You say{text}";
            }
            if (userMessage.ToLower() == "text")
            {
                replyMessage.Text = text;
            }
            if (userMessage.ToLower() == "หวัดดี" || userMessage.ToLower() == "สวัสดี")
            {
                replyMessage.Text = "หวัดเด้";
            }
            if (userMessage.ToLower() == "sub")
            {
                try
                {
                    var message = new
                    {
                        LineId = userId
                    };
                    var client = new HttpClient();
                    var data = JsonConvert.SerializeObject(message);
                    var fullUrl = $"https://centralloggerazure.azurewebsites.net/api/line/AddLine";
                    var fullUrl2 = $"http://central-logger.azurewebsites.net/api/line/AddLine";

                    var response = await client.PostAsync(fullUrl, new StringContent(data, Encoding.UTF8, "application/json"));
                    var response2 = await client.PostAsync(fullUrl2, new StringContent(data, Encoding.UTF8, "application/json"));
                    if (response.IsSuccessStatusCode)
                    {
                        replyMessage.Text = $"ขอบคุณที่สมัครข้อมูล เมื่อเราตรวจพบ Critical เราแจ้งเตือนหาท่านให้เร็วที่สุด ขอบคุณครับ";
                    }
                    else if ((int)response.StatusCode == 500)
                    {
                        replyMessage.Text = "พบข้อผิดพลาดในการสมัครข้อมูล";

                    }
                }
                catch (Exception)
                {
                    replyMessage.Text = $"พบข้อผิดพลาดในการสมัครข้อมูล กรุณาติดต่อผู้ดูแล";
                }
            }
            if (userMessage.ToLower() == "unsub")
            {
                try
                {
                    var message = new
                    {
                        LineId = userId
                    };
                    var client = new HttpClient();
                    var data = JsonConvert.SerializeObject(message);
                    var fullUrl = $"https://centralloggerazure.azurewebsites.net/api/line/DeleteLine";
                    var response = await client.PostAsync(fullUrl, new StringContent(data, Encoding.UTF8, "application/json"));
                    if (response.IsSuccessStatusCode)
                    {
                        replyMessage.Text = "เราได้ยกเลิกการแจ้งเตือน log เรียบร้อยแล้ว ขอบคุณครับ";
                    }
                    else if ((int)response.StatusCode == 500)
                    {
                        replyMessage.Text = "พบข้อผิดพลาดในการลบ";

                    }
                }
                catch (Exception)
                {
                    replyMessage.Text = $"พบข้อผิดพลาดในการลบ กรุณาติดต่อผู้ดูแล";
                }

            }
            await messagingClient.ReplyMessageAsync(replyToken, new List<ISendMessage> { replyMessage });
        }
    }
}