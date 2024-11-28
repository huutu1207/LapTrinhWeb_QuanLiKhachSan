using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Areas.Admin.Controllers
{
    public class AdminController : Controller
    {
        QL_KhachSanEntities db = new QL_KhachSanEntities();
        public ActionResult Index()
        {

            return View();
        }
        [HttpGet]
        public ActionResult DangNhap()
        {
            return View();
        }
        [HttpPost]
        public ActionResult DangNhap(FormCollection f)
        {
            //Gán các giá trị người dùng nhập liệu cho các biến 
            var sTenDN = f["UserName"];
            var sMatKhau = f["Password"];
            //Gán giá trị cho đối tượng được tạo mới (ad)  
            NHANVIEN ad = db.NHANVIENs.SingleOrDefault(n => n.Username == sTenDN && n.Password == sMatKhau);
            if (ad != null)
            {
                Session["Admin"] = ad;
                return RedirectToAction("Index", "Admin");
            }
            else
            {
                ViewBag.ThongBao = "Tên đăng nhập hoặc mật khẩu không đúng";
            }
            return View();
        }
        
        public ActionResult Logout()
        {
            Session["Admin"] = null;
            return RedirectToAction("DangNhap");
        }
    }
}