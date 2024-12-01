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
            var khachHang = (KHACHHANG)Session["User"];
            if (khachHang == null)
            {
                return RedirectToAction("DangNhap", "User");
            }

            // Lấy thông tin phòng
            var phong = db.PHONGs.FirstOrDefault(p => p.MaPH == MaPH);
            var dichVuList = db.DICHVUs.ToList();
            if (phong == null)
            {
                return HttpNotFound("Không tìm thấy phòng.");
            }

            if (phong.TrangThai != "Available")
            {
                ViewBag.ErrorMessage = "Phòng đã được đặt.";
                return View();
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
        public ActionResult DatPhong(string MaPH, DateTime NgayNhan, DateTime NgayTra, string MaDV = null)
        {

            // Kiểm tra xem người dùng đã đăng nhập chưa
            var khachHang = (KHACHHANG)Session["User"];
            if (khachHang == null)
            {
                return RedirectToAction("DangNhap", "User");
            }

            // Lấy mã khách hàng từ session
            string MaKH = khachHang.MaKH;

            // Tạo mã đặt phòng
            string MaDP = "DP" + new Random().Next(1000, 9999);

            // Lấy thông tin phòng
            var phong = db.PHONGs.FirstOrDefault(p => p.MaPH == MaPH);
            if (phong == null)
            {
                return HttpNotFound("Không tìm thấy phòng.");
            }
            // Kiểm tra dịch vụ nếu có
            float giaDichVu = 0f;
            string maDVToSave = null;  // Khởi tạo MaDV để sử dụng khi có hoặc không có dịch vụ
            if (!string.IsNullOrEmpty(MaDV))
            {
                var dichVu = db.DICHVUs.FirstOrDefault(dv => dv.MaDV == MaDV);
                if (dichVu != null)
                {
                    giaDichVu = (float)dichVu.Gia; // Ép kiểu từ double? sang float
                    maDVToSave = MaDV;  // Lưu mã dịch vụ nếu có
                }
            }

            // Tính số ngày ở
            int soNgayO = (NgayTra - NgayNhan).Days;
            if (soNgayO <= 0)
            {
                ModelState.AddModelError("", "Ngày trả phải sau ngày nhận.");
                return View();
            }

            phong.TrangThai = "Booked";
            phong.CheckIn = NgayNhan;
            phong.CheckOut = NgayTra;

            // Tính DonGia
            float donGia = (float)(((phong.Gia ?? 0) * soNgayO) + giaDichVu - 200000); // Ép kiểu từ double? sang float

            // Lưu đặt phòng vào cơ sở dữ liệu
            var datPhong = new DATPHONG
            {
                MaDP = MaDP,
                MaKH = MaKH,
                MaPH = MaPH,
                MaDV = maDVToSave, // Lưu MaDV khi có, nếu không thì là null
                NgayDat = DateTime.Now,
                NgayNhan = NgayNhan,
                NgayTra = NgayTra,
                TinhTrang = "Đã đặt",
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