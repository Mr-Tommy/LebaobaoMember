﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LebaobaoComponents.Repositories.Default;
using log4net;
using LebaobaoComponents.Helpers;
using System.IO;
using System.Text;
using System.Xml;
using LebaobaoComponents.Domains;
using Newtonsoft.Json;

namespace Lebaobao.Controllers
{
    public class WeiChatController : LebaobaoController
    {
        private const string AppId = "wxb4a5e406f48740a2";
        private const string Token = "lebaobao";
        private const string EncryptKey = "Gx99JnWavwV1bA4mLeL2foDJrZsD5cLb5vR3s4O5XOc";
        [HttpGet]
        public ContentResult Message(string signature, string timestamp, string nonce, string echostr)
        {

            if (!String.IsNullOrEmpty(signature))
            {
                string mysig = CryptoHelper.SHA1(Token, timestamp, nonce);

                if (signature.Equals(mysig, StringComparison.OrdinalIgnoreCase))
                {
                    return new ContentResult() { Content = echostr };
                }
            }

            return new ContentResult() { Content = String.Format("signature validate failed") };
        }
        [HttpPost]
        public ContentResult Message(string signature, string timestamp, string nonce, string encrypt_type, string msg_signature)
        {

            //获取消息内容
            string msgContent = "";
            using (StreamReader sr = new StreamReader(Request.InputStream, Encoding.UTF8))
            {
                msgContent = sr.ReadToEnd();
            }


            if (String.IsNullOrEmpty(encrypt_type) || encrypt_type.Equals("raw"))
            {
                string mysig = CryptoHelper.SHA1(Token, timestamp, nonce);
                if (String.IsNullOrEmpty(signature) || !signature.Equals(mysig, StringComparison.OrdinalIgnoreCase))
                {

                    return new ContentResult() { Content = "" };
                }
            }
            else
            {
                //解密消息
                string sMsg = "";  //解析之后的明文
                int ret_code = 0;
                WXBizMsgCrypt wxcpt = new WXBizMsgCrypt(Token, EncryptKey, AppId);
                ret_code = wxcpt.DecryptMsg(msg_signature, timestamp, nonce, msgContent, ref sMsg);
                if (ret_code != 0)
                {


                    return new ContentResult() { Content = "" };
                }

                msgContent = sMsg;
            }


            XmlDocument doc = new XmlDocument();
            doc.LoadXml(msgContent);


            if (doc.DocumentElement.SelectSingleNode("MsgType").InnerText == "text")       //根据回复关键字查询课程
            {
                #region Text

                string content = doc.DocumentElement.SelectSingleNode("Content").InnerText;
                var user = _db.Users.SingleOrDefault(u => u.Phone == content.Trim() && u.UserStatus == UserStatus.Ok);
                string msg = "";
                if (user != null)
                {
                    msg = $@"<xml>
<ToUserName><![CDATA[{doc.DocumentElement.SelectSingleNode("FromUserName").InnerText}]]></ToUserName>
<FromUserName><![CDATA[{doc.DocumentElement.SelectSingleNode("ToUserName").InnerText}]]></FromUserName>
<CreateTime>{doc.DocumentElement.SelectSingleNode("CreateTime").InnerText}</CreateTime>
<MsgType><![CDATA[text]]></MsgType>
<Content><![CDATA[孩子姓名：{user.ChildName}
联系方式：{user.Phone}
推拿剩余次数：{user.CanUseCount}
<a href='https://www.hdlebaobao.cn/Order/GetOrderListByUserPhone?phone={user.Phone}&ordertype=0'>点击查看推拿记录</a>
保健剩余次数：{user.BaoJianCount}
<a href='https://www.hdlebaobao.cn/Order/GetOrderListByUserPhone?phone={user.Phone}&ordertype=1'>点击查看保健记录</a>]]></Content>
</xml>";
                }
                else
                {
                    msg = $@"<xml>
<ToUserName><![CDATA[{doc.DocumentElement.SelectSingleNode("FromUserName").InnerText}]]></ToUserName>
<FromUserName><![CDATA[{doc.DocumentElement.SelectSingleNode("ToUserName").InnerText}]]></FromUserName>
<CreateTime>{doc.DocumentElement.SelectSingleNode("CreateTime").InnerText}</CreateTime>
<MsgType><![CDATA[text]]></MsgType>
<Content><![CDATA[查询有误，请重新输入]]></Content>
</xml>";
                }


                return new ContentResult() { Content = msg, ContentEncoding = Encoding.UTF8, ContentType = "text/xml" };
                #endregion
            }



            return new ContentResult() { Content = "", ContentEncoding = Encoding.UTF8, ContentType = "text/xml" };

        }
    }
}