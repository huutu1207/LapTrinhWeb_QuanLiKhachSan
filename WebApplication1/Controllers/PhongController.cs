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
            int iSize = 9;
            int iPageNumber = (page ?? 1);
            return View(listPhong.ToPagedList(iPageNumber, iSize));
        }

        public ActionResult ChiTietPhong(string id)
        {
            var phong = db.PHONGs.FirstOrDefault(s => s.MaPH == id);
            if (phong == null)
            {
                return HttpNotFound(); // Nếu không tìm thấy sách, trả về lỗi 404
            }

            // Lấy danh sách bình luận cùng tên khách hàng
            var binhLuans = (from bl in db.BINHLUANs
                             join kh in db.KHACHHANGs on bl.MaKH equals kh.MaKH
                             where bl.MaPH == id
                             select new CMMD
                             {
                                 NoiDung = bl.NDBL,
                                 DanhGia = (int)bl.DanhGia,
                                 ThoiGian = (DateTime)bl.ThoiGian,
                                 HoTenKhachHang = kh.HoTen
                             }).ToList();

            ViewBag.BinhLuans = binhLuans;
            return View(phong);

        }

        [HttpPost]
        public ActionResult ThemBinhLuan(string maPhong, string NoiDung, int DanhGia)
        {
            // Kiểm tra xem người dùng đã đăng nhập chưa
            var khachHang = (KHACHHANG)Session["User"];

            if (khachHang == null)
            {
                // Nếu chưa đăng nhập, lưu URL hiện tại vào Session và điều hướng đến trang đăng nhập
                Session["ReturnUrl"] = Request.Url.ToString();
                return RedirectToAction("DangNhap", "User");
            }

            if (ModelState.IsValid)
            {
                BINHLUAN binhLuanMoi = new BINHLUAN
                {
                    MaPH = maPhong,
                    MaKH = khachHang.MaKH,
                    NDBL = NoiDung,
                    DanhGia = DanhGia,
                    ThoiGian = DateTime.Now
                };

                db.BINHLUANs.Add(binhLuanMoi);
                db.SaveChanges();
            }

            return RedirectToAction("ChiTietSach", new { id = maPhong});
        }
    }
}