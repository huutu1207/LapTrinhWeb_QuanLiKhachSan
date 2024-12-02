using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class DatPhongController : Controller
    {
        private QL_KhachSanEntities db = new QL_KhachSanEntities();

        public ActionResult DatPhong(string MaPH)
        {
            // Lấy thông tin phòng
            var phong = db.PHONGs.FirstOrDefault(p => p.MaPH == MaPH);
            var dichVuList = db.DICHVUs.ToList();
            if (phong == null)
            {
                return HttpNotFound("Không tìm thấy phòng.");
            }

            // Lấy danh sách dịch vụ
            if (dichVuList == null || !dichVuList.Any())
            {
                ViewBag.ErrorMessage = "Không có dịch vụ nào trong hệ thống.";
            }

            ViewBag.Phong = phong;
            ViewBag.DichVuList = dichVuList;

            return View();
        }


        [HttpPost]
        public ActionResult DatPhong(string MaPH, DateTime? NgayNhan, DateTime? NgayTra, string MaDV = null)
        {

            // Kiểm tra xem người dùng đã đăng nhập chưa
            var khachHang = (KHACHHANG)Session["User"];
            if (khachHang == null)
            {
                return RedirectToAction("DangNhap", "User");
            }

            // Kiểm tra dữ liệu đầu vào
            if (!NgayNhan.HasValue || !NgayTra.HasValue)
            {
                ModelState.AddModelError("", "Vui lòng nhập ngày nhận và ngày trả.");
                return View();
            }

            if (NgayTra <= NgayNhan)
            {
                ModelState.AddModelError("", "Ngày trả phải sau ngày nhận.");
                return View();
            }

            // Kiểm tra trùng thời gian đặt phòng
            var isOverlapping = db.DATPHONGs.Any(dp =>
                dp.MaPH == MaPH &&
                ((NgayNhan < dp.NgayTra && NgayTra > dp.NgayNhan)));

            if (isOverlapping)
            {
                ModelState.AddModelError("", "Phòng đã được đặt trong khoảng thời gian này.");
                return View();
            }

            // Các bước tiếp theo như lấy thông tin phòng, tính giá, và lưu thông tin đặt phòng
            string MaKH = khachHang.MaKH;
            string MaDP = "DP" + new Random().Next(1000, 9999);
            var phong = db.PHONGs.FirstOrDefault(p => p.MaPH == MaPH);
            if (phong == null)
            {
                return HttpNotFound("Không tìm thấy phòng.");
            }

            float giaDichVu = 0f;
            if (!string.IsNullOrEmpty(MaDV))
            {
                var dichVu = db.DICHVUs.FirstOrDefault(dv => dv.MaDV == MaDV);
                if (dichVu != null)
                {
                    giaDichVu = (float)dichVu.Gia;
                }
            }

            int soNgayO = (NgayTra.Value - NgayNhan.Value).Days;
            float donGia = (float)(((phong.Gia ?? 0) * soNgayO) + giaDichVu - 200000);
            phong.TrangThai = "Booked";

            var datPhong = new DATPHONG
            {
                MaDP = MaDP,
                MaKH = MaKH,
                MaPH = MaPH,
                MaDV = MaDV,
                NgayDat = DateTime.Now,
                NgayNhan = NgayNhan.Value,
                NgayTra = NgayTra.Value,
                TinhTrang = "Booked",
                DatCoc = 200000,
                DonGia = donGia
            };

            db.DATPHONGs.Add(datPhong);
            db.SaveChanges();

            return RedirectToAction("DatPhongConfirmation", new { MaDP = MaDP });
        }

        public ActionResult DatPhongConfirmation(string MaDP)
        {
            var datPhong = db.DATPHONGs.FirstOrDefault(dp => dp.MaDP == MaDP);
            if (datPhong == null)
            {
                return HttpNotFound("Không tìm thấy thông tin đặt phòng.");
            }

            return View(datPhong);
        }

    }
}