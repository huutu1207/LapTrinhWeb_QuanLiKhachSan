using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Models;
using System.Net.Mail;
using System.Net;
using System.Data.Entity;
using System.IO;
using System.Web.UI.WebControls;
using System.Collections.Specialized;

namespace WebApplication1.Controllers
{
    public class UserController : Controller
    {
        QL_KhachSanEntities db = new QL_KhachSanEntities();

        public ActionResult DangNhap()
        {
            // Tự động điền thông tin đăng nhập nếu cookies tồn tại
            if (Request.Cookies["Username"] != null && Request.Cookies["Password"] != null)
            {
                ViewBag.Username = Request.Cookies["Username"].Value;
                ViewBag.Password = Request.Cookies["Password"].Value;
            }
            return View();
        }

        [HttpPost]
        public ActionResult DangNhap(FormCollection collection)
        {
            // Lấy dữ liệu từ form
            var sUsername = collection["Username"]; // Lấy tên đăng nhập
            var sPassword = collection["Password"]; // Lấy mật khẩu

            // Kiểm tra thông tin đầu vào
            if (string.IsNullOrEmpty(sUsername))
            {
                ViewData["Err1"] = "Bạn chưa nhập tên đăng nhập.";
            }
            else if (string.IsNullOrEmpty(sPassword))
            {
                ViewData["Err2"] = "Bạn chưa nhập mật khẩu.";
            }
            else
            {
                // Mã hóa mật khẩu trước khi kiểm tra
                var hashedPassword = HashPassword(sPassword);

                // Tìm user trong bảng KHACHHANG
                var khachhang = db.KHACHHANGs
                    .FirstOrDefault(kh => kh.Username == sUsername && kh.Password == hashedPassword);

                if (khachhang != null)
                {
                    // Đăng nhập thành công
                    ViewBag.ThongBao = "Đăng Nhập Thành Công!";
                    Session["User"] = khachhang; // Lưu user vào Session

                    // Ghi nhớ đăng nhập qua Cookies
                    if (!string.IsNullOrEmpty(collection["remember"]) && collection["remember"] == "true")
                    {
                        Response.Cookies["Username"].Value = sUsername;
                        Response.Cookies["Password"].Value = sPassword;
                        Response.Cookies["Username"].Expires = DateTime.Now.AddDays(1);
                        Response.Cookies["Password"].Expires = DateTime.Now.AddDays(1);
                    }
                    else
                    {
                        // Xóa cookies nếu không ghi nhớ
                        Response.Cookies["Username"].Expires = DateTime.Now.AddDays(-1);
                        Response.Cookies["Password"].Expires = DateTime.Now.AddDays(-1);
                    }

                    // Chuyển hướng sau khi đăng nhập thành công
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    // Thông báo nếu thông tin đăng nhập không hợp lệ
                    ViewBag.ThongBao = "Tên đăng nhập hoặc mật khẩu không đúng!";
                }
            }

            return View();
        }


        public ActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        public ActionResult DangKy(FormCollection collection)
        {
            var hoTen = collection["HoTen"];
            var diaChi = collection["DiaChi"];
            var dienThoai = collection["DienThoai"];
            var gioiTinh = collection["GioiTinh"];
            var ngaySinh = collection["NgaySinh"];
            var quocTich = collection["QuocTich"];
            var email = collection["Email"];
            var tenDangNhap = collection["Username"];
            var matKhau = collection["Password"];
            var xacNhanMatKhau = collection["ConfirmPassword"];
            var cccd = collection["CCCD"];

            if (string.IsNullOrEmpty(tenDangNhap))
            {
                ViewData["Err1"] = "Vui lòng nhập tên đăng nhập.";
            }
            else if (string.IsNullOrEmpty(matKhau))
            {
                ViewData["Err2"] = "Vui lòng nhập mật khẩu.";
            }
            else if (matKhau != xacNhanMatKhau)
            {
                ViewData["Err3"] = "Mật khẩu xác nhận không khớp.";
            }
            else if (string.IsNullOrEmpty(email))
            {
                ViewData["Err4"] = "Vui lòng nhập email.";
            }
            else if (string.IsNullOrEmpty(cccd))
            {
                ViewData["Err5"] = "Vui lòng nhập số CCCD.";
            }
            else
            {
                try
                {
                    // Kiểm tra tài khoản đã tồn tại
                    var existingUser = db.KHACHHANGs.FirstOrDefault(u => u.Username == tenDangNhap || u.Email == email || u.CCCD == cccd);
                    if (existingUser != null)
                    {
                        ViewData["Err1"] = "Tên đăng nhập, email hoặc CCCD đã tồn tại.";
                    }
                    else
                    {
                        // Tạo mã xác nhận
                        var verificationCode = new Random().Next(100000, 999999).ToString();

                        // Tạo người dùng mới
                        KHACHHANG khachHang = new KHACHHANG
                        {
                            MaKH = "KH" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                            HoTen = hoTen,
                            DiaChi = diaChi,
                            DienThoai = dienThoai,
                            GioiTinh = gioiTinh,
                            NgaySinh = !string.IsNullOrEmpty(ngaySinh) ? DateTime.Parse(ngaySinh) : (DateTime?)null,
                            QuocTich = quocTich,
                            Email = email,
                            Username = tenDangNhap,
                            Password = HashPassword(matKhau),
                            CCCD = cccd,
                            EmailVerificationCode = verificationCode,
                            IsEmailVerified = false
                        };

                        db.KHACHHANGs.Add(khachHang);
                        db.SaveChanges();

                        // Gửi mã xác nhận qua email
                        GuiEmailXacNhan(email, verificationCode);

                        // Lưu thông tin mã xác nhận
                        TempData["Email"] = email;
                        ViewBag.ThongBao = "Mã xác nhận đã được gửi đến email của bạn. Vui lòng kiểm tra!";
                        return RedirectToAction("XacNhanEmail");
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.ThongBao = "Có lỗi xảy ra: " + ex.Message;
                }
            }

            return View();
        }


        private void GuiEmailXacNhan(string emailNguoiDung, string verificationCode)
        {
            try
            {
                var fromAddress = new MailAddress("2224802010314@student.tdmu.edu.vn", "Lucky Hotel");
                var toAddress = new MailAddress(emailNguoiDung);
                const string fromPassword = "hbpk bhtz zvic aysp";
                const string subject = "Xác nhận email";

                string body = $"<h3>Chào bạn!</h3>" +
                              $"<p>Đây là mã xác nhận của bạn:</p>" +
                              $"<h2>{verificationCode}</h2>" +
                              $"<p>Vui lòng nhập mã này vào trang xác nhận để hoàn tất đăng ký.</p>" +
                              $"<p>Trân trọng,</p>" +
                              $"<p><em>Web Sách Online</em></p>";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                {
                    smtp.Send(message);
                }
            }
            catch (Exception ex)
            {
                ViewBag.ThongBao = "Có lỗi khi gửi email xác nhận: " + ex.Message;
            }
        }

        public ActionResult XacNhanEmail()
        {
            return View();
        }

        [HttpPost]
        public ActionResult XacNhanEmail(string verificationCode)
        {
            var email = TempData["Email"]?.ToString(); // Lấy email từ TempData đã lưu trong quá trình đăng ký

            if (!string.IsNullOrEmpty(email))
            {
                // Tìm người dùng theo email
                var user = db.KHACHHANGs.FirstOrDefault(u => u.Email == email);

                if (user != null && user.EmailVerificationCode == verificationCode)
                {
                    // Nếu mã xác nhận đúng, cập nhật trạng thái xác nhận email
                    user.IsEmailVerified = true;
                    user.EmailVerificationCode = null; // Xóa mã xác nhận
                    db.SaveChanges();

                    ViewBag.ThongBao = "Xác nhận email thành công! Bạn đã có thể đăng nhập.";
                    return RedirectToAction("DangNhap");
                }
                else
                {
                    // Nếu mã xác nhận sai
                    ViewBag.ThongBao = "Mã xác nhận không hợp lệ. Vui lòng thử lại.";
                }
            }
            else
            {
                ViewBag.ThongBao = "Không tìm thấy thông tin email để xác nhận.";
            }

            return View();
        }



        public string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        public ActionResult DangXuat()
        {
            Session["User"] = null;
            HttpCookie ckTaiKhoan = new HttpCookie("UserName"), ckMatKhau = new HttpCookie("Password");
            ckTaiKhoan.Expires = DateTime.Now.AddDays(-1);
            ckMatKhau.Expires = DateTime.Now.AddDays(-1);
            Response.Cookies.Add(ckTaiKhoan);
            Response.Cookies.Add(ckMatKhau);
            return RedirectToAction("Index", "TrangChu");
        }

        public ActionResult ThongTinCaNhan()
        {
            // Lấy thông tin người dùng từ Session (giả sử người dùng đã đăng nhập)
            var user = (KHACHHANG)Session["User"];
            if (user == null)
            {
                return RedirectToAction("DangNhap", "User"); // Nếu người dùng chưa đăng nhập, chuyển hướng đến trang đăng nhập
            }
            var kh = db.KHACHHANGs.FirstOrDefault(s => s.MaKH == user.MaKH);
            return View(kh); // Trả về View và gửi dữ liệu người dùng tới View
        }

        public ActionResult SuaProfile(string id)
        {
            var cus = db.KHACHHANGs.FirstOrDefault(s => s.MaKH == id);
            if (cus == null)
            {
                TempData["ErrorMessage"] = "Khách hàng không tồn tại.";
                return RedirectToAction("ThongTinCaNhan");
            }

            return View(cus); // Trả về View với dữ liệu khách hàng
        }

        [HttpPost]
        public ActionResult SuaProfile(FormCollection f, HttpPostedFileBase fileUpload)
        {
            string id = f["Id"];
            var user = db.KHACHHANGs.FirstOrDefault(s => s.MaKH == id);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Khách hàng không tồn tại.";
                return RedirectToAction("ThongTinCaNhan");
            }

            if (ModelState.IsValid)
            {
                user.HoTen = f["Name"];
                user.Email = f["Email"];
                user.DienThoai = f["Phone"];
                user.DiaChi = f["Address"];

                // Kiểm tra nếu người dùng tải ảnh mới
                if (fileUpload != null && fileUpload.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(fileUpload.FileName);
                    var path = Path.Combine(Server.MapPath("~/Images"), fileName);

                    // Nếu ảnh đã tồn tại, xoá đi và lưu ảnh mới
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }

                    fileUpload.SaveAs(path); // Lưu ảnh vào thư mục Images
                    user.Avatar = fileName; // Cập nhật tên ảnh trong cơ sở dữ liệu
                }

                db.SaveChanges();
                return RedirectToAction("ThongTinCaNhan");
            }

            // Trả về view nếu có lỗi
            return View(user);
        }

        public ActionResult LichSuDatPhong()
        {
            var user = (KHACHHANG)Session["User"];
            if (user == null)
            {
                return Redirect("DangNhap");
            }
            DateTime dateHomNay = DateTime.Now;
            var lst = db.DATPHONGs.Where(dp => dp.MaKH == user.MaKH)
                .Join(db.PHONGs, dp => dp.MaPH, p => p.MaPH, (dp, p) => new
                {
                    MaDatPhong = dp.MaDP,
                    TenPhong = p.SoPH,
                    NgayDat = dp.NgayDat,
                    NgayNhan = dp.NgayNhan,
                    NgayTra = dp.NgayTra,
                    DichVu = dp.MaDV,
                    ThanhTien = dp.DonGia
                }).AsEnumerable().Select(m => 
                    new LichSuView()
                    {
                        MaDatPhong = m.MaDatPhong,
                        TenPhong = m.TenPhong,
                        NgayDat = m.NgayDat.Value.ToString("dd/MM/yyyy"),
                        NgayNhan = m.NgayNhan.Value.ToString("dd/MM/yyyy"),
                        NgayTra = m.NgayTra.Value.ToString("dd/MM/yyyy"),
                        DichVu = m.DichVu,
                        ThanhTien = m.ThanhTien,
                        CoTheHuy = m.NgayNhan > dateHomNay ? true : false
                    }
                ).ToList();
            return View(lst);
        }

        [HttpGet]
        public ActionResult HuyDatPhong(string maDP)
        {
            if (string.IsNullOrEmpty(maDP))
            {
                return HttpNotFound();
            }

            var room = db.DATPHONGs.FirstOrDefault(s => s.MaDP == maDP);
            if (room == null)
            {
                return HttpNotFound();
            }

            // Xóa bản ghi đặt phòng
            db.DATPHONGs.Remove(room);
            db.SaveChanges();

            // Điều hướng lại trang lịch sử
            return RedirectToAction("LichSuDatPhong");
        }



    }

}