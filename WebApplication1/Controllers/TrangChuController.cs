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
        QL_KhachSanEntities db=new QL_KhachSanEntities();
        public ActionResult Index()
        {
            return View();
        }
        //[ChildActionOnly]
        //public ActionResult NavPartial()
        //{
        //    List<MENU> lst = db.MENUs.Where(m => m.ParentId == null).OrderBy(m => m.OrderNumber).ToList();

        //    int[] a = new int[lst.Count()];

        //    for (int i = 0; i < lst.Count; i++)
        //    {
        //        int id = lst[i].Id;
        //        List<MENU> l = db.MENUs.Where(m => m.ParentId == id).ToList();
        //        //List<MENU> l = (from mn in data.MENUs where mn.ParentId == lst[i].Id select mn).ToList();
        //        int k = l.Count();
        //        a[i] = k;
        //    }

        //    ViewBag.lst = a;

        //    return PartialView(lst);
        //}

        public ActionResult FooterPartial()
        {
            return PartialView("_FooterPartial");
        }

        public ActionResult SliderPartial()
        {
            return PartialView("_SliderPartial");
        }
        
    }
}