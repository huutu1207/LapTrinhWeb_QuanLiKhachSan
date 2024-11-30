using Newtonsoft.Json;
using PagedList;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using WebApplication1.Models;
using PagedList.Mvc;
using static System.Net.WebRequestMethods;

namespace WebApplication1.Controllers
{
    public class PhongController : Controller
    {
        // GET: Phong
       private QL_KhachSanEntities db = new QL_KhachSanEntities();

        private List<PHONG> LayPhong(int count)
        {
            return db.PHONGs.OrderBy(a => a.SoPH).Take(count).ToList();
        }
        public ActionResult DanhSachPhong(int? page, string selectedLoaiPhong)
        {
            // Lấy danh sách loại phòng từ bảng LOAIPHONG để gửi vào ViewBag cho filter
            var loaiPhongList = db.LOAIPHONGs.ToList();
            ViewBag.LoaiPhongList = new SelectList(loaiPhongList, "MaLoai", "TenLoai"); // Hiển thị tên loại phòng

            // Lấy danh sách phòng và lọc theo loại phòng nếu có
            var listPhongQuery = db.PHONGs.AsQueryable();

            // Nếu có chọn loại phòng thì lọc theo loại phòng
            if (!string.IsNullOrEmpty(selectedLoaiPhong))
            {
                listPhongQuery = listPhongQuery.Where(p => p.MaLoai == selectedLoaiPhong);
            }

            // Lấy các phòng với phân trang
            int iSize = 9;
            int iPageNumber = (page ?? 1);
            var listPhong = listPhongQuery.ToList();

            // Truyền danh sách phòng đã lọc và phân trang vào View
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
            var dichVuList = db.DICHVUs.ToList();
            ViewBag.DichVuList = dichVuList;

            return View(phong);

        }

        [HttpPost]
        public ActionResult ThemBinhLuan(string MaPH, string NoiDung, int DanhGia)
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
                    MaPH = MaPH,
                    MaKH = khachHang.MaKH,
                    NDBL = NoiDung,
                    DanhGia = DanhGia,
                    ThoiGian = DateTime.Now
                };

                db.BINHLUANs.Add(binhLuanMoi);
                db.SaveChanges();
            }

            return RedirectToAction("ChiTietPhong", new { id = MaPH});
        }
        [HttpPost]
        public ActionResult CheckAvailability(string MaPH, DateTime CheckIn, DateTime CheckOut)
        {
            var phong = db.PHONGs.FirstOrDefault(s => s.MaPH == MaPH);
            if (phong == null)
            {
                return HttpNotFound(); // Nếu không tìm thấy phòng, trả về lỗi 404
            }

            // Kiểm tra ngày check-in và check-out
            var overlappingBookings = db.BINHLUANs
                .Where(b => b.MaPH == MaPH &&
                            (b.ThoiGian >= CheckIn && b.ThoiGian < CheckOut)) // Kiểm tra chồng lấp ngày
                .ToList();

            if (overlappingBookings.Any())
            {
                ViewBag.Message = "The room is not available for the selected dates.";
                return View("ChiTietPhong", phong); // Trả về trang ChiTietPhong với thông báo lỗi
            }

            // Nếu không có sự trùng lặp, cho phép đặt phòng
            ViewBag.Message = "The room is available for the selected dates!";
            return View("ChiTietPhong", phong);
        }



    }
}