using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class TrangChuController : Controller
    {
        // GET: TrangChu
        QL_KhachSanEntities1 db =new QL_KhachSanEntities1();
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult _NavPartial()
        {
            var trangTinList = db.TRANGTINs.ToList();
            return PartialView(trangTinList);
        }
        

        public ActionResult FooterPartial()
        {
            return PartialView("_FooterPartial");
        }

        public ActionResult SliderPartial()
        {
            return PartialView("_SliderPartial");
        }
        public ActionResult TrangTin(string metatitle)
        {
            var tt = (from t in db.TRANGTINs where t.MetaTitle == metatitle select t).Single();
            return View(tt);
        }
    }
}