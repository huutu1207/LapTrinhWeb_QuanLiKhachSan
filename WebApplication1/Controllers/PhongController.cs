using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class PhongController : Controller
    {
        // GET: Phong
        QL_KhachSanEntities db = new QL_KhachSanEntities();

        private List<PHONG> LayPhong(int count)
        {
            return db.PHONGs.OrderBy(a => a.SoPH).Take(count).ToList();
        }
        public ActionResult DanhSachPhong(int ?page)
        {
            var listPhong = LayPhong(20);
            int iSize = 2;
            int iPageNumber = (page ?? 1);
            return View(listPhong.ToPagedList(iPageNumber, iSize));
        }
        public ActionResult ChiTietPhong()
        {
            return View();

        }
    }
}