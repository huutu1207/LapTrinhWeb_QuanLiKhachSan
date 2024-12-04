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

            string MaKH = khachHang.MaKH;
            string MaDP = "DP" + new Random().Next(1000, 9999);
            var phong = db.PHONGs.FirstOrDefault(p => p.MaPH == MaPH);
            if (phong == null)
            {
                return HttpNotFound("Không tìm thấy phòng.");
            }

            var datPhong = new DATPHONG
            {
                MaDP = MaDP,
                MaKH = MaKH,
                MaPH = MaPH,
                NgayDat = DateTime.Now,
                NgayNhan = NgayNhan.Value,
                NgayTra = NgayTra.Value,
                TinhTrang = "Đã đặt cọc",
                DatCoc = 200000
            };

            db.DATPHONGs.Add(datPhong);
            db.SaveChanges();

            if (selectedServices.Any())
            {
                foreach (var maDV in selectedServices)
                {
                    // Kiểm tra xem dịch vụ đã tồn tại trong cơ sở dữ liệu chưa
                    var service = db.DICHVUs.FirstOrDefault(dv => dv.MaDV == maDV);
                    if (service != null)
                    {
                        var datDichVu = new DATDICHVU
                        {
                            MaDV = maDV,
                            MaDP = MaDP,
                            NgayDat = DateTime.Now, // Ngày đặt dịch vụ là thời gian hiện tại
                            NgayNhan = NgayNhan.Value,  // Đảm bảo không có null
                            NgayTra = NgayTra.Value   // Đảm bảo không có null
                        };
                        db.DATDICHVUs.Add(datDichVu);
                    }
                }
                db.SaveChanges();
            }


            // Tính giá dịch vụ sau khi lưu các dịch vụ
            float giaDichVu = (float)(db.DATDICHVUs
     .Where(ddv => ddv.MaDP == MaDP && ddv.NgayNhan >= NgayNhan && ddv.NgayTra <= NgayTra)  // Điều kiện lọc
     .Join(db.DICHVUs, ddv => ddv.MaDV, dv => dv.MaDV, (ddv, dv) => dv.Gia)  // Kết nối với bảng DICHVUs và lấy giá trị Gia
     .Sum() ?? 0);  // Tính tổng và sử dụng 0 nếu kết quả là null


            // Tính tổng giá trị
            int soNgayO = (NgayTra.Value - NgayNhan.Value).Days;

            // Tính giá phòng
            float donGiaPhong = (float)(phong.Gia ?? 0) * soNgayO + giaDichVu - 200000;
            
            datPhong.DonGia = donGiaPhong;
            db.Entry(datPhong).State = EntityState.Modified;  // Đảm bảo cập nhật lại bản ghi DATPHONG
            db.SaveChanges(); // Lưu lại thay đổi

            switch (thanhtoan)
            {
                case "vivnpay":
                    // Xử lý thanh toán qua VNPay
                    return RedirectToAction("PaymentVNPay", "DatPhong", new { MaDP = MaDP, NgayDat = NgayDat });
                case "vimomo":
                    // Xử lý thanh toán qua MoMo
                    return RedirectToAction("PaymentMomo", "DatPhong");
                default:
                    // Xử lý cho trường hợp không hợp lệ
                    break;
            }

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

        public ActionResult PaymentVNPay(string MaDP, string NgayDat)
        {
            string url = ConfigurationManager.AppSettings["Url"];
            string returnUrl = ConfigurationManager.AppSettings["ReturnUrl"];
            string tmnCode = ConfigurationManager.AppSettings["TmnCode"];
            string hashSecret = ConfigurationManager.AppSettings["HashSecret"];
            PayLib pay = new PayLib();

            pay.AddRequestData("vnp_Version", "2.1.0"); //Phiên bản api mà merchant kết nối.Phiên bản hiện tại là 2.1.0
            pay.AddRequestData("vnp_Command", "pay"); //Mã API sử dụng, mã cho giao dịch thanh toán là 'pay'
            pay.AddRequestData("vnp_TmnCode", tmnCode); //Mã website của merchant trên hệ thống của VNPAY(khi đăng ký tài khoản sẽ có trong mail VNPAY gửi về)
            pay.AddRequestData("vnp_Amount", 200000.ToString() + "00"); //số tiền cần thanh toán, công thức: số tiền *100 - ví dụ 10.000(mười nghìn đồng)-- > 1000000
            //TotalAmount() là phương thức trả về tổng tiền của đơn hàng.
            pay.AddRequestData("vnp_BankCode", "NCB"); //Mã Ngân hàng thanh toán (tham khảo: https://sandbox.vnpayment.vn/apis/danh-sach-ngan-hang/), có thể để trống, người dùng có thể chọn trên cổng thanh toán VNPAY
            pay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss")); //ngày thanh toán theo định dạng yyyyMMddHHmmss
            pay.AddRequestData("vnp_CurrCode", "VND"); //Đơn vị tiền tệ sử dụng thanh toán. Hiện tại chỉ hỗ trợ VND
            pay.AddRequestData("vnp_IpAddr", PayUtil.GetIpAddress()); //Địa chỉ IP của khách hàng thực hiện giao dịch
            pay.AddRequestData("vnp_Locale", "vn"); //Ngôn ngữ giao diện hiển thị - Tiếng Việt(vn), Tiếng Anh(en)
            pay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang"); //Thông tin mô tả nội dung thanh toán
            pay.AddRequestData("vnp_OrderType", "other"); //topup: Nạp tiền điện thoại - billpayment: Thanh toán hóa đơn -fashion: Thời trang -other: Thanh toán trực tuyến
            pay.AddRequestData("vnp_ReturnUrl", returnUrl); //URL thông báo kết quả giao dịch khi Khách hàng kết thúc thanh toán
            pay.AddRequestData("vnp_TxnRef", DateTime.Now.Ticks.ToString()); //mã hóa đơn
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
                //lấy toàn bộ dữ liệu được trả về
                foreach (string s in vnpayData)
                {
                    if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                    {
                        pay.AddResponseData(s, vnpayData[s]);
                    }
                }
                long orderId = Convert.ToInt64(pay.GetResponseData("vnp_TxnRef")); //mã hóa đơn
                long vnpayTranId = Convert.ToInt64(pay.GetResponseData("vnp_TransactionNo")); //mã giao dịch tại hệ thống VNPAY
                string vnp_ResponseCode = pay.GetResponseData("vnp_ResponseCode");
                //response code: 00 - thành công, khác 00 - xem thêm https://sandbox.vnpayment.vn/apis/docs/bang-ma-loi/
                string vnp_SecureHash = Request.QueryString["vnp_SecureHash"]; //hash của dữ liệu trả về
                bool checkSignature = pay.ValidateSignature(vnp_SecureHash, hashSecret);
                //check chữ ký đúng hay không?
                if (checkSignature)
                {
                    if (vnp_ResponseCode == "00")
                    {
                        //Thanh toán thành công
                        ViewBag.Message = "Thanh toán thành công hóa đơn " + orderId + " | Mã giao dịch: " + vnpayTranId;
                    }
                    else
                    {
                        //Thanh toán không thành công. Mã lỗi: vnp_ResponseCode
                        ViewBag.Message = "Có lỗi xảy ra trong quá trình xử lý hóa đơn " + orderId
                        + " | Mã giao dịch: " + vnpayTranId + " | Mã lỗi: " + vnp_ResponseCode;
                    }
                }
                else
                {
                    ViewBag.Message = "Có lỗi xảy ra trong quá trình xử lý";
                }
            }
            return View();
        }
    }
}