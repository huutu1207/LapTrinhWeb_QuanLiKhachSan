using NgoMinhTri;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net.NetworkInformation;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class DatPhongController : Controller
    {
        private QL_KhachSanEntities1 db = new QL_KhachSanEntities1();

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
        public ActionResult DatPhong(string MaPH, DateTime? NgayDat, DateTime? NgayNhan, DateTime? NgayTra, string MaDV = null, string[] selectedServices = null, string thanhtoan = null)
        {

            var khachHang = (KHACHHANG)Session["User"];
            if (khachHang == null)
            {
                return RedirectToAction("DangNhap", "User");
            }

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

            var isOverlapping = db.DATPHONGs.Any(dp =>
                dp.MaPH == MaPH &&
                ((NgayNhan < dp.NgayTra && NgayTra > dp.NgayNhan)));

            if (isOverlapping)
            {
                ModelState.AddModelError("", "Phòng đã được đặt trong khoảng thời gian này.");
                return View();
            }

            var phong = db.PHONGs.FirstOrDefault(p => p.MaPH == MaPH);
            var dichVuList = db.DICHVUs.ToList();
            if (phong == null)
            {
                return HttpNotFound("Không tìm thấy phòng.");
            }

            // Tính toán giá trị tổng
            float giaDichVu = selectedServices != null
                ? (float)(db.DICHVUs.Where(dv => selectedServices.Contains(dv.MaDV)).Sum(dv => dv.Gia) ?? 0)
                : 0;

            int soNgayO = (NgayTra.Value - NgayNhan.Value).Days;
            float donGiaPhong = (float)(phong.Gia ?? 0) * soNgayO + giaDichVu - 200000;

            string MaKH = khachHang.MaKH;
            string MaDP = "DP" + new Random().Next(1000, 9999);

            TempData["DatPhongInfo"] = new DatPhongInfo
            {
                MaDP = "DP" + new Random().Next(1000, 9999), // Tạo mã đặt phòng
                MaKH = khachHang.MaKH,                      // Mã khách hàng từ Session
                MaPH = MaPH,                                // Mã phòng từ tham số
                NgayDat = DateTime.Now,                     // Ngày đặt hiện tại
                NgayNhan = NgayNhan.Value,                  // Ngày nhận từ người dùng
                NgayTra = NgayTra.Value,                    // Ngày trả từ người dùng
                DonGia = donGiaPhong,
                DatCoc = 200000,                            // Số tiền đặt cọc cố định
                SelectedServices = selectedServices?.ToList() // Chuyển mảng dịch vụ thành danh sách
                
            };

            // Điều hướng đến phương thức thanh toán
            switch (thanhtoan)
            {
                case "vivnpay":
                    return RedirectToAction("PaymentVNPay", "DatPhong", new { MaDP });
                case "vimomo":
                    return RedirectToAction("PaymentMomo", "DatPhong", new { MaDP });
                default:
                    break;
            }

            return View();
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

        public ActionResult PaymentVNPay(string MaDP, string NgayDat)
        {
            string url = ConfigurationManager.AppSettings["Url"];
            string returnUrl = ConfigurationManager.AppSettings["ReturnUrl"];
            string tmnCode = ConfigurationManager.AppSettings["TmnCode"];
            string hashSecret = ConfigurationManager.AppSettings["HashSecret"];
            PayLib pay = new PayLib();

            pay.AddRequestData("vnp_Version", "2.1.0");
            pay.AddRequestData("vnp_Command", "pay");
            pay.AddRequestData("vnp_TmnCode", tmnCode);
            pay.AddRequestData("vnp_Amount", (200000 * 100).ToString());
            pay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            pay.AddRequestData("vnp_CurrCode", "VND");
            pay.AddRequestData("vnp_IpAddr", PayUtil.GetIpAddress());
            pay.AddRequestData("vnp_Locale", "vn");
            pay.AddRequestData("vnp_OrderInfo", "Thanh toán đặt phòng");
            pay.AddRequestData("vnp_OrderType", "other");
            pay.AddRequestData("vnp_ReturnUrl", returnUrl);
            pay.AddRequestData("vnp_TxnRef", MaDP);

            string paymentUrl = pay.CreateRequestUrl(url, hashSecret);
            return Redirect(paymentUrl);
        }

        public ActionResult PaymentConfirm()
        {
            if (Request.QueryString.Count > 0)
            {
                string hashSecret = ConfigurationManager.AppSettings["HashSecret"];
                var vnpayData = Request.QueryString;
                PayLib pay = new PayLib();

                foreach (string s in vnpayData)
                {
                    if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                    {
                        pay.AddResponseData(s, vnpayData[s]);
                    }
                }

                string vnp_ResponseCode = pay.GetResponseData("vnp_ResponseCode");
                string vnp_SecureHash = Request.QueryString["vnp_SecureHash"];
                bool checkSignature = pay.ValidateSignature(vnp_SecureHash, hashSecret);

                if (checkSignature && vnp_ResponseCode == "00")
                {
                    // Thanh toán thành công -> Lưu thông tin đặt phòng
                    var datPhongInfo = TempData["DatPhongInfo"] as DatPhongInfo;
                    if (datPhongInfo != null)
                    {
                        var datPhong = new DATPHONG
                        {
                            MaDP = datPhongInfo.MaDP,
                            MaKH = datPhongInfo.MaKH,
                            MaPH = datPhongInfo.MaPH,
                            NgayDat = datPhongInfo.NgayDat,
                            NgayNhan = datPhongInfo.NgayNhan,
                            NgayTra = datPhongInfo.NgayTra,
                            TinhTrang = "Đã đặt cọc",
                            DonGia = datPhongInfo.DonGia,
                            DatCoc = datPhongInfo.DatCoc
                        };

                        db.DATPHONGs.Add(datPhong);
                        db.SaveChanges();

                        // Lưu thông tin dịch vụ nếu có
                        if (datPhongInfo.SelectedServices != null)
                        {
                            foreach (var maDV in datPhongInfo.SelectedServices)
                            {
                                var service = db.DICHVUs.FirstOrDefault(dv => dv.MaDV == maDV);
                                if (service != null)
                                {
                                    var datDichVu = new DATDICHVU
                                    {
                                        MaDV = maDV,
                                        MaDP = datPhongInfo.MaDP,
                                        NgayDat = DateTime.Now,
                                        NgayNhan = datPhongInfo.NgayNhan,
                                        NgayTra = datPhongInfo.NgayTra
                                    };
                                    db.DATDICHVUs.Add(datDichVu);
                                }
                                else
                                {
                                    var datDichVu = new DATDICHVU
                                    {
                                        MaDV = null,
                                        MaDP = datPhongInfo.MaDP,
                                        NgayDat = DateTime.Now,
                                        NgayNhan = datPhongInfo.NgayNhan,
                                        NgayTra = datPhongInfo.NgayTra
                                    };
                                    db.DATDICHVUs.Add(datDichVu);
                                }
                            }
                            db.SaveChanges();
                        }
                    }

                    ViewBag.Message = "Thanh toán thành công và thông tin đặt phòng đã được lưu.";
                    return RedirectToAction("DatPhongConfirmation", new { MaDP = datPhongInfo.MaDP });
                }
                else
                {
                    ViewBag.Message = "Thanh toán không thành công. Vui lòng thử lại.";
                }
            }
            return View();
        }


    }
}