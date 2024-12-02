using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebApplication1.Controllers
{
    public class TrangChuController : Controller
    {
        // GET: TrangChu
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult NavPartial()
        {
            return PartialView("_NavPartial");
        }

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