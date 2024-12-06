﻿using System;
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
using System.Data.Entity.Validation;
using System.Net.Http;
using System.Threading.Tasks;
using Facebook;
using System.Text.RegularExpressions;

namespace WebApplication1.Controllers
{
    public class UserController : Controller
    {
        QL_KhachSanEntities1 db = new QL_KhachSanEntities1();

        public ActionResult DangNhap()
        {
            // Tự động điền thông tin đăng nhập nếu cookies tồn tại
            if (Request.Cookies["Username"] != null && Request.Cookies["Password"] != null)
            {
                ViewBag.Username = Request.Cookies["Username"].Value;
                ViewBag.Password = Request.Cookies["Password"].Value;
            }

            var clientId = "236329445042-ao0n55mptkk6fftaeu8mactg13feu683.apps.googleusercontent.com";
            var url = "https://localhost:44354/User/LoginGoogle";  // Đảm bảo URL này là chính xác.
            string response = GenerateGoogleOAuthUrl(clientId, url);
            ViewBag.response = response;

            return View();
        }

        private string GenerateGoogleOAuthUrl(string clientId, string redirectUri)
        {
            string googleOAuthUrl = "https://accounts.google.com/o/oauth2/v2/auth";

            var queryParams = new List<KeyValuePair<string, string>>
          {
              new KeyValuePair<string, string>("response_type", "code"),
              new KeyValuePair<string, string>("client_id", clientId),
              new KeyValuePair<string, string>("redirect_uri", redirectUri),
              new KeyValuePair<string, string>("scope", "openid email profile"),
              new KeyValuePair<string, string>("access_type", "online")
          };

            // Create URL by concatenating parameters
            string queryString = string.Join("&", queryParams.Select(q => $"{q.Key}={Uri.EscapeDataString(q.Value)}"));
            return $"{googleOAuthUrl}?{queryString}";
        }

        private async Task<string> ExchangeCodeForTokenAsync(string code, string clientId, string redirectUri, string clientSecret)
        {
            string tokenEndpoint = "https://oauth2.googleapis.com/token";

            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("redirect_uri", redirectUri),
            new KeyValuePair<string, string>("grant_type", "authorization_code")
        });

                try
                {
                    // Gửi yêu cầu POST đến token endpoint
                    var response = await client.PostAsync(tokenEndpoint, content);
                    var responseString = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        // Nếu yêu cầu không thành công, throw ra lỗi với phản hồi chi tiết
                        throw new Exception($"Error exchanging code: {responseString}");
                    }

                    // Parse phản hồi JSON
                    dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseString);

                    // Kiểm tra lỗi trong phản hồi từ Google
                    if (jsonResponse.error != null)
                    {
                        throw new Exception($"Error exchanging code: {jsonResponse.error_description}");
                    }

                    // Trả về access token
                    return jsonResponse.access_token;
                }
                catch (Exception ex)
                {
                    // Log lỗi chi tiết để dễ dàng debug
                    Console.WriteLine($"Exception: {ex.Message}");
                    throw;
                }
            }
        }

        private async Task<dynamic> GetGoogleUserInfoAsync(string accessToken)
        {
            string userInfoEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo";
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var response = await client.GetAsync(userInfoEndpoint);
                var responseString = await response.Content.ReadAsStringAsync();

                dynamic userInfo = Newtonsoft.Json.JsonConvert.DeserializeObject(responseString.ToString());

                if (userInfo.error != null)
                {
                    throw new Exception($"Error fetchung user info: {userInfo.error.message}");
                }
                return userInfo;
            }
        }
        public async Task<ActionResult> LoginGoogle(string code, string scope, string authuser, string prompt)
        {
            string redirectUri = "https://localhost:44354/User/LoginGoogle";
            var clientID = "236329445042-ao0n55mptkk6fftaeu8mactg13feu683.apps.googleusercontent.com";
            var clientSecret = "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";

            if (string.IsNullOrEmpty(code))
            {
                return RedirectToAction("DangNhap", "User");
            }
            try
            {
                var accessToken = await ExchangeCodeForTokenAsync(code, clientID, redirectUri, clientSecret);
                var userInfo = await GetGoogleUserInfoAsync(accessToken);

                string name = userInfo.name?.ToString();
                string email = userInfo.email?.ToString();

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email))
                {
                    ViewBag.Error = "Incomplete user information returned by google";
                    return RedirectToAction("DangNhap");

                }
                

                KHACHHANG kh = db.KHACHHANGs.SingleOrDefault(u => u.Email == email);
                if (kh == null)
                {
                    string pass = Guid.NewGuid().ToString("N").Substring(0, 10);
                    KHACHHANG khNew = new KHACHHANG
                    {
                        MaKH = "KH" + (db.KHACHHANGs.Max(k => k.MaKH.Substring(2)) + 1).ToString(),  // Tạo mã khách hàng theo định dạng KH + số tự tăng
                        HoTen = name,
                        Email = email,
                        Username = email.Length > 13 ? email.Substring(0, 13) : email,  // Đảm bảo username hợp lệ
                        Password = pass,
                        DienThoai = "0000",
                        NgaySinh = DateTime.Now,
                    };
                    db.KHACHHANGs.Add(khNew);
                    db.SaveChanges();
                    Session["User"] = khNew;
                    ViewBag.ThongBao = "Chúc mừng bạn đăng nhập thành công";
                }
                else
                {
                    Session["User"] = kh;
                }
                return RedirectToAction("Index", "TrangChu");

            }
            catch (DbEntityValidationException dbEx)
            {
                var errorMessages = dbEx.EntityValidationErrors.SelectMany(validationResult => validationResult.ValidationErrors)
                    .Select(error => $"Property: {error.PropertyName}, Error: {error.ErrorMessage}");
                var fullErrorMessage = string.Join(", ", errorMessages);

                return Content($"Validation Errors: {fullErrorMessage}");
            }
        }


        [HttpPost]
        public ActionResult DangNhap(FormCollection collection)
        {
            // Lấy dữ liệu từ form
            string sUsername = collection["username"];
            string sPassword = collection["password"];
            string remember = collection["remember"]; // Giá trị checkbox remember

            // Kiểm tra thông tin đầu vào
            if (string.IsNullOrEmpty(sUsername))
            {
                ViewData["Err1"] = "Bạn chưa nhập tên đăng nhập.";
                return View();
            }

            if (string.IsNullOrEmpty(sPassword))
            {
                ViewData["Err2"] = "Bạn chưa nhập mật khẩu.";
                return View();
            }

            // Mã hóa mật khẩu trước khi kiểm tra
            var hashedPassword = HashPassword(sPassword);

            // Kiểm tra thông tin đăng nhập
            var khachhang = db.KHACHHANGs
                .FirstOrDefault(kh => kh.Username == sUsername && kh.Password == hashedPassword);

            if (khachhang != null)
            {
                // Đăng nhập thành công
                Session["User"] = khachhang; // Lưu user vào Session

                if (!string.IsNullOrEmpty(remember) && remember == "on")
                {
                    // Tạo cookie lưu thông tin đăng nhập trong 30 ngày
                    HttpCookie usernameCookie = new HttpCookie("Username", sUsername)
                    {
                        Expires = DateTime.Now.AddDays(30) // Thời hạn 30 ngày
                    };

                    HttpCookie passwordCookie = new HttpCookie("Password", sPassword)
                    {
                        Expires = DateTime.Now.AddDays(30) // Thời hạn 30 ngày
                    };

                    Response.Cookies.Add(usernameCookie);
                    Response.Cookies.Add(passwordCookie);
                }
                else
                {
                    // Xóa cookies nếu người dùng không chọn "Remember"
                    if (Request.Cookies["Username"] != null)
                    {
                        Response.Cookies["Username"].Expires = DateTime.Now.AddDays(-1);
                    }

                    if (Request.Cookies["Password"] != null)
                    {
                        Response.Cookies["Password"].Expires = DateTime.Now.AddDays(-1);
                    }
                }

                return RedirectToAction("Index", "TrangChu"); // Chuyển hướng về trang chính
            }
            else
            {
                ViewBag.ThongBao = "Tên đăng nhập hoặc mật khẩu không đúng!";
                return View();
            }
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
                    ThanhTien = dp.DonGia
                })
                .AsEnumerable().Select(m => 
                    new LichSuView()
                    {
                        MaDatPhong = m.MaDatPhong,
                        TenPhong = m.TenPhong,
                        NgayDat = m.NgayDat.Value.ToString("dd/MM/yyyy"),
                        NgayNhan = m.NgayNhan.Value.ToString("dd/MM/yyyy"),
                        NgayTra = m.NgayTra.Value.ToString("dd/MM/yyyy"),
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

            // Tìm đặt phòng theo mã MaDP
            var room = db.DATPHONGs.FirstOrDefault(s => s.MaDP == maDP);
            if (room == null)
            {
                return HttpNotFound();
            }

            // Xóa các bản ghi trong bảng DATDICHVU liên quan đến MaDP
            var dichVuLienQuan = db.DATDICHVUs.Where(dv => dv.MaDP == maDP).ToList();
            if (dichVuLienQuan.Any())
            {
                db.DATDICHVUs.RemoveRange(dichVuLienQuan);
            }

            // Xóa bản ghi trong bảng DATPHONG
            db.DATPHONGs.Remove(room);

            // Lưu thay đổi vào cơ sở dữ liệu
            db.SaveChanges();

            // Điều hướng lại trang lịch sử
            return RedirectToAction("LichSuDatPhong");
        }

        [HttpGet]
        public ActionResult QuenMatKhau()
        {
            return View(); // Trả về view QuenMatKhau.cshtml
        }

        [HttpPost]
        public ActionResult QuenMatKhau(FormCollection collection)
        {
            string email = collection["email"];

            // Kiểm tra email có hợp lệ hay không
            if (string.IsNullOrEmpty(email))
            {
                ViewData["ErrEmail"] = "Email không được để trống!";
                return View();
            }

            // Kiểm tra email có tồn tại trong hệ thống không
            var khachhang = db.KHACHHANGs.FirstOrDefault(k => k.Email == email);

            if (khachhang == null)
            {
                ViewData["ErrEmail"] = "Email này không tồn tại trong hệ thống!";
                return View();
            }

            // Mã xác nhận gửi qua email
            string verificationCode = GenerateVerificationCode();

            // Gửi mã xác nhận qua email
            SendResetPasswordEmail(email, verificationCode);

            // Lưu mã xác nhận vào TempData để sử dụng sau này
            TempData["VerificationCode"] = verificationCode;
            TempData["Email"] = email;

            ViewBag.ThongBao = "Mã xác nhận đã được gửi đến email của bạn. Vui lòng kiểm tra email.";

            return RedirectToAction("XacNhanEmailQuenMK");
        }

        // Phương thức xử lý liên kết đặt lại mật khẩu
        public ActionResult DatLaiMatKhau(string email)
        {
            // Xử lý đặt lại mật khẩu
            return View(); // Trả về view để người dùng nhập mật khẩu mới
        }

        [HttpPost]
        public ActionResult DatLaiMatKhau(string email, string newPassword, string confirmPassword)
        {
            // Kiểm tra đầu vào
            if (string.IsNullOrEmpty(newPassword))
            {
                TempData["ErrPassword"] = "Mật khẩu không được để trống!";
                return View();
            }

            // Kiểm tra mật khẩu nhập lại có khớp không
            if (newPassword != confirmPassword)
            {
                TempData["ErrConfirmPassword"] = "Mật khẩu nhập lại không khớp!";
                return View();
            }

            // Kiểm tra độ dài mật khẩu
            if (newPassword.Length < 6)
            {
                TempData["ErrPassword"] = "Mật khẩu mới phải có ít nhất 6 ký tự!";
                return View();
            }

            // Kiểm tra định dạng email có hợp lệ không
            if (!Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            {
                TempData["ErrEmail"] = "Địa chỉ email không hợp lệ!";
                return View();
            }

            var khachhang = db.KHACHHANGs.FirstOrDefault(k => k.Email == email);

            if (khachhang != null)
            {
                // Mã hóa mật khẩu mới trước khi lưu vào CSDL
                khachhang.Password = HashPassword(newPassword);

                // Lưu mật khẩu mới vào CSDL
                db.SaveChanges();

                TempData["ThongBao"] = "Mật khẩu của bạn đã được thay đổi thành công!";
                return RedirectToAction("DangNhap", "User");
            }

            TempData["ThongBao"] = "Không tìm thấy người dùng với email này!";
            return View();
        }

        [HttpGet]
        public ActionResult ThayDoiMatKhau()
        {
            return View(); // Trả về view ThayDoiMatKhau.cshtml
        }
        [HttpPost]
        public ActionResult ThayDoiMatKhau(string email, string oldPassword, string newPassword, string confirmPassword)
        {
            // Kiểm tra đầu vào
            if (string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                TempData["ErrPassword"] = "Tất cả các trường không được để trống!";
                return View();
            }

            // Kiểm tra email có hợp lệ không
            if (!Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            {
                TempData["ErrEmail"] = "Địa chỉ email không hợp lệ!";
                return View();
            }

            // Kiểm tra mật khẩu cũ
            var khachhang = db.KHACHHANGs.FirstOrDefault(k => k.Email == email);
            if (khachhang == null)
            {
                TempData["ErrEmail"] = "Không tìm thấy người dùng với email này!";
                return View();
            }

            // So sánh mật khẩu cũ
            if (khachhang.Password != HashPassword(oldPassword))
            {
                TempData["ErrOldPassword"] = "Mật khẩu cũ không đúng!";
                return View();
            }

            // Kiểm tra mật khẩu mới và mật khẩu xác nhận có khớp không
            if (newPassword != confirmPassword)
            {
                TempData["ErrConfirmPassword"] = "Mật khẩu nhập lại không khớp!";
                return View();
            }

            // Kiểm tra độ dài mật khẩu mới
            if (newPassword.Length < 6)
            {
                TempData["ErrPassword"] = "Mật khẩu mới phải có ít nhất 6 ký tự!";
                return View();
            }

            // Mã hóa mật khẩu mới trước khi lưu vào CSDL
            khachhang.Password = HashPassword(newPassword);

            // Lưu mật khẩu mới vào CSDL
            db.SaveChanges();

            TempData["ThongBao"] = "Mật khẩu của bạn đã được thay đổi thành công!";
            return RedirectToAction("ThongTinCaNhan", "User");
        }


        private void SendResetPasswordEmail(string emailNguoiDung, string verificationCode)
        {
            try
            {
                // Địa chỉ email gửi
                var fromAddress = new MailAddress("2224802010314@student.tdmu.edu.vn", "Lucky Hotel");

                // Địa chỉ email nhận
                var toAddress = new MailAddress(emailNguoiDung);

                const string fromPassword = "hbpk bhtz zvic aysp";
                const string subject = "Mã xác nhận đặt lại mật khẩu";

                string body =$"<p>Mã xác nhận của bạn để đặt lại mật khẩu là: <strong>{verificationCode}</strong></p>" +
                              $"<p>Vui lòng nhập mã xác nhận trên trang web của chúng tôi.</p>" +
                              $"<p>Trân trọng,</p>" +
                              $"<p><em>Lucky Hotel</em></p>";

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
                throw new InvalidOperationException("Có lỗi khi gửi email: " + ex.Message, ex);
            }
        }

        public ActionResult XacNhanEmailQuenMK()
        {
            return View(); // Hiển thị form để người dùng nhập mã xác nhận
        }

        [HttpPost]
        public ActionResult XacNhanEmailQuenMK(string verificationCode)
        {
            var email = TempData["Email"]?.ToString(); // Lấy email từ TempData

            if (!string.IsNullOrEmpty(email))
            {
                // Kiểm tra mã xác nhận
                if (TempData["VerificationCode"]?.ToString() == verificationCode)
                {
                    // Mã xác nhận đúng, hiển thị form để người dùng thay đổi mật khẩu
                    return RedirectToAction("DatLaiMatKhau", new { email = email });
                }
                else
                {
                    ViewBag.ThongBao = "Mã xác nhận không hợp lệ. Vui lòng thử lại.";
                }
            }
            else
            {
                ViewBag.ThongBao = "Không tìm thấy thông tin email để xác nhận.";
            }

            return View();
        }


        private string GenerateVerificationCode()
        {
            var random = new Random();
            var verificationCode = random.Next(100000, 999999).ToString(); // Mã xác nhận 6 chữ số
            return verificationCode;
        }


        public ActionResult LoginWithFacebook()
        {
            var appId = System.Configuration.ConfigurationManager.AppSettings["FacebookAppId"];
            var redirectUri = Url.Action("LoginFacebookCallback", "User", null, protocol: Request.Url.Scheme);
            var facebookLoginUrl = $"https://www.facebook.com/v21.0/dialog/oauth?client_id={appId}&redirect_uri={redirectUri}&scope=email,public_profile";

            return Redirect(facebookLoginUrl);
        }

        public ActionResult LoginFacebookCallback(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return RedirectToAction("DangNhap", "User");
            }

            var appId = System.Configuration.ConfigurationManager.AppSettings["FacebookAppId"];
            var appSecret = System.Configuration.ConfigurationManager.AppSettings["FacebookAppSecret"];
            var redirectUri = Url.Action("LoginFacebookCallback", "User", null, protocol: Request.Url.Scheme);

            try
            {
                // Lấy token từ Facebook
                var tokenUrl = $"https://graph.facebook.com/v21.0/oauth/access_token?client_id={appId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&client_secret={appSecret}&code={code}";

                var client = new System.Net.Http.HttpClient();
                var response = client.GetStringAsync(tokenUrl).Result;
                var tokenData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response);

                // Kiểm tra nếu tokenData không null và có trường access_token
                if (tokenData == null || tokenData.access_token == null)
                {
                    throw new Exception("Không thể lấy mã truy cập từ Facebook.");
                }

                string accessToken = tokenData.access_token.ToString(); // Chắc chắn là chuỗi

                // Kiểm tra accessToken
                if (string.IsNullOrEmpty(accessToken))
                {
                    throw new Exception("Không thể lấy mã truy cập từ Facebook.");
                }

                // Dùng access token để lấy thông tin người dùng
                var fbClient = new FacebookClient(accessToken);
                dynamic userInfo = fbClient.Get("me?fields=id,name,email");

                if (userInfo == null)
                {
                    throw new Exception("Không thể lấy thông tin người dùng từ Facebook.");
                }

                var name = userInfo.name;
                string email = userInfo.email.ToString(); // Ép kiểu rõ ràng sang string

                // Kiểm tra người dùng trong cơ sở dữ liệu
                KHACHHANG kh = db.KHACHHANGs.SingleOrDefault(u => u.Email == email);
                if (kh == null)
                {
                    string pass = Guid.NewGuid().ToString("N").Substring(0, 10);
                    KHACHHANG khNew = new KHACHHANG
                    {
                        MaKH = "KH" + (db.KHACHHANGs.Max(k => k.MaKH.Substring(2)) + 1).ToString(),  // Tạo mã khách hàng theo định dạng KH + số tự tăng
                        HoTen = name,
                        Email = email,
                        Username = email.Length > 13 ? email.Substring(0, 13) : email,  // Đảm bảo username hợp lệ
                        Password = pass,
                        DienThoai = "0000",
                        NgaySinh = DateTime.Now,
                    };
                    db.KHACHHANGs.Add(khNew);
                    db.SaveChanges();
                    Session["User"] = khNew;
                    ViewBag.ThongBao = "Chúc mừng bạn đăng nhập thành công";
                }
                else
                {
                    Session["User"] = kh;
                }

                // Điều hướng về trang chính
                return RedirectToAction("Index", "TrangChu");
            }
            catch (Exception ex)
            {
                // Nếu có lỗi, quay lại trang đăng nhập
                ViewBag.Error = "Đã có lỗi xảy ra khi đăng nhập bằng Facebook: " + ex.Message;
                return RedirectToAction("DangNhap", "User");
            }
        }
    }

}