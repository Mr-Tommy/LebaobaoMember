﻿using LebaobaoComponents.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Webdiyer.WebControls.Mvc;

namespace LebaobaoMember.Controllers
{
    public class OrderController : LebaobaoController
    {
        // GET: Order
        public ActionResult Index(string childname, string phone, OrderType? ordertype, int userid = 0, int index = 1)
        {
            var orderlist = _db.Orders.ToList();
            if (userid != 0)
            {
                orderlist = orderlist.Where(o => o.UserId == userid).ToList();
            }
            if (!string.IsNullOrEmpty(childname))
            {
                orderlist = orderlist.Where(o => o.User.ChildName.Contains(childname.Trim())).ToList();
            }
            if (!string.IsNullOrEmpty(phone))
            {
                orderlist = orderlist.Where(o => o.User.Phone.Contains(phone.Trim())).ToList();
            }
            if (ordertype != null)
            {
                orderlist = orderlist.Where(o => o.OrderType == ordertype).ToList();
            }


            ViewBag.OrderCount = orderlist.Count();
            var list = orderlist.OrderByDescending(o => o.Id).ToPagedList(index, 5);
            return View(list);
        }
        public ActionResult OrderAddView(int userid)
        {
            var user = _db.Users.Find(userid);
            return View(user);
        }
        public ActionResult GetOrderListByUserPhone(string phone)
        {
            var list = _db.Orders.Where(u => u.User.Phone == phone).OrderByDescending(o => o.CreateTime).ToList();

            return View(list);
        }
        #region Post请求

        /// <summary>
        /// 添加来访记录
        /// </summary>
        /// <param name="userid">用户ID</param>
        /// <param name="time">来访时间</param>
        /// <param name="type">服务内容（推拿，保健，等）</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult OrderAdd(int userid, string time, OrderType type)
        {
            try
            {
                var order = new Orders
                {
                    UserId = userid,
                    CreateTime = Convert.ToDateTime(time),
                    OrderType = type
                };
                _db.Orders.Add(order);
                var user = _db.Users.Find(userid);

                if (type == OrderType.TuiNa)
                {
                    if (user.CanUseCount == 0)
                    {
                        logger.Debug($"{DateTime.Now}:可推拿次数不足");
                        return Json(new { success = false, msg = "可推拿次数不足" });
                    }

                    user.CanUseCount--;
                }
                if (type == OrderType.BaoJian)
                {

                    if (user.BaoJianCount == 0)
                    {
                        logger.Debug($"{DateTime.Now}:可保健次数不足");
                        return Json(new { success = false, msg = "可保健次数不足" });
                    }
                    user.BaoJianCount--;
                }
                user.LastTime = DateTime.Now;
                _db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {

                logger.Debug(ex.Message);
                return Json(new { success = false, msg = ex.Message });
            }

        }

        /// <summary>
        /// 删除来访记录
        /// </summary>
        /// <param name="orderid">来访记录ID</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult OrderDelete(int orderid)
        {
            try
            {
                var order = _db.Orders.Find(orderid);
                if (order == null)
                {
                    return Json(new { success = false, msg = "来访记录不存在" });
                }

                _db.Orders.Remove(order);
                var user = _db.Users.Find(order.UserId);
                if (user == null)
                {
                    return Json(new { success = false, msg = "未找到与来访记录匹配的用户" });

                }
                if (order.OrderType == OrderType.TuiNa)
                {
                    user.CanUseCount++;
                }
                if (order.OrderType == OrderType.BaoJian)
                {
                    user.BaoJianCount++;
                }

                _db.SaveChanges();
                return Json(new { success = true, msg = "删除成功" });
            }
            catch (Exception ex)
            {

                logger.Debug(ex.Message);
                return Json(new { success = false, msg = ex.Message });
            }

        }
        [HttpPost]
        public JsonResult GetOrderListByUserPhone(string phone, int index)
        {
            var list = _db.Orders.Where(o => o.User.Phone.Contains(phone.Trim())).OrderByDescending(o => o.Id).ToPagedList(index, 20);
            return Json(new { success = true, list });
        }
        #endregion
    }
}