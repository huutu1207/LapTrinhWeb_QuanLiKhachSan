using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Areas.Admin.Controllers
{
    public class PhongController : Controller
    {
        // GET: Admin/Phong
        QL_KhachSanEntities db = new QL_KhachSanEntities();
        public ActionResult DanhSachPhong()
        {
            // Lấy ngày hiện tại
            DateTime currentDate = DateTime.Now.Date;
            var datphongList = db.DATPHONGs.ToList();

            foreach (var datphong in datphongList)
            {
                // Kiểm tra trạng thái và cập nhật cho DATPHONG
                if (datphong.TinhTrang == "Occupied")
                {
                    if (datphong.NgayTra <= currentDate)
                    {
                        datphong.TinhTrang = "Available";
                    }

                }
                else if (datphong.NgayNhan >= currentDate && datphong.TinhTrang!="Occupied")
                {
                    datphong.TinhTrang = "Booked";
                }
                else if (datphong.NgayNhan < currentDate && datphong.TinhTrang != "Occupied")
                {
                    datphong.TinhTrang = "Available";
                }

                // Cập nhật trạng thái cho PHONG dựa trên DATPHONG
                var phong = db.PHONGs.FirstOrDefault(p => p.MaPH == datphong.MaPH);
                if (phong != null)
                {
                    phong.TrangThai = datphong.TinhTrang; // Đồng bộ trạng thái
                }
            }

            // Lưu tất cả các thay đổi vào cơ sở dữ liệu
            db.SaveChanges();
            var danhSachPhong = db.PHONGs.ToList();
            return View(danhSachPhong);
        }
        public ActionResult Chitietphong(string MaPH)
        {
            var phong = db.PHONGs.FirstOrDefault(p => p.MaPH == MaPH);
            if (phong == null)
            {
                return HttpNotFound();
            }

            // Lấy thông tin đặt phòng liên quan
            var datphong = db.DATPHONGs.FirstOrDefault(dp => dp.MaPH == MaPH);
            if (datphong != null)
            {
                // Lấy thông tin khách hàng liên quan
                var khachHang = db.KHACHHANGs.FirstOrDefault(kh => kh.MaKH == datphong.MaKH);
                if (khachHang != null)
                {
                    // Truyền thông tin khách hàng vào ViewBag
                    ViewBag.MaKH = khachHang.MaKH;
                    ViewBag.TenKH = khachHang.HoTen;
                }
            }

            return View(phong);
        }

        [HttpGet]
        public ActionResult Create()
        {
            ViewBag.MaLoai = new SelectList(db.LOAIPHONGs.OrderBy(n => n.TenLoai), "MaLoai", "TenLoai");
            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Create(PHONG phong, HttpPostedFileBase AnhUpload)
        {
            ViewBag.MaLoai = new SelectList(db.LOAIPHONGs.OrderBy(n => n.TenLoai), "MaLoai", "TenLoai");

            if (AnhUpload == null)
            {
                ViewBag.ThongBao = "Hãy chọn ảnh cho phòng.";
                return View(phong);
            }
            var existingPhong = db.PHONGs.FirstOrDefault(p => p.MaPH == phong.MaPH || p.SoPH == phong.SoPH);
            if (existingPhong != null)
            {
                ModelState.AddModelError("", "Mã phòng hoặc số phòng đã tồn tại. Vui lòng chọn mã khác.");
                return View(phong);
            }
            if (ModelState.IsValid)
            {
                try
                {
                    // Xử lý file upload
                    string fileName = Path.GetFileName(AnhUpload.FileName);
                    string path = Path.Combine(Server.MapPath("~/Images"), fileName);

                    // Kiểm tra file đã tồn tại hay chưa
                    if (!System.IO.File.Exists(path))
                    {
                        AnhUpload.SaveAs(path); // Lưu file lên server
                    }

                    phong.Anh = fileName; // Gán tên file vào thuộc tính Anh

                    // Thêm vào cơ sở dữ liệu
                    db.PHONGs.Add(phong);
                    db.SaveChanges();

                    return RedirectToAction("DanhSachPhong");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Đã xảy ra lỗi: {ex.Message}");
                }
            }

            return View(phong);
        }
        [HttpGet]
        public ActionResult Edit(string MaPH)
        {
            var phong = db.PHONGs.SingleOrDefault(n => n.MaPH == MaPH);
            if (phong == null)
            {
                Response.StatusCode = 404;
                return null;
            }

            ViewBag.MaLoai = new SelectList(db.LOAIPHONGs.OrderBy(n => n.TenLoai), "MaLoai", "TenLoai");


            return View(phong);
        }


        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(PHONG phong, HttpPostedFileBase AnhUpload)
        {
            ViewBag.MaLoai = new SelectList(db.LOAIPHONGs.OrderBy(n => n.TenLoai), "MaLoai", "TenLoai");

            if (!ModelState.IsValid)
            {
                return View(phong);
            }

            try
            {
                // Lấy phòng hiện tại từ CSDL
                var phongDb = db.PHONGs.SingleOrDefault(n => n.MaPH == phong.MaPH);
                if (phongDb == null)
                {
                    return HttpNotFound(); // Nếu không tìm thấy phòng, trả về lỗi
                }

                // Kiểm tra ảnh upload nếu có
                if (AnhUpload != null && AnhUpload.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(AnhUpload.FileName);
                    var path = Path.Combine(Server.MapPath("~/Images"), fileName);

                    // Nếu ảnh chưa tồn tại, lưu ảnh mới
                    if (!System.IO.File.Exists(path))
                    {
                        AnhUpload.SaveAs(path); // Lưu ảnh lên server
                    }
                    phong.Anh = fileName; // Cập nhật tên ảnh vào thuộc tính phong.Anh
                }
                else if (string.IsNullOrEmpty(phong.Anh))
                {
                    // Nếu không có ảnh upload mới và ảnh hiện tại null, giữ ảnh cũ
                    phong.Anh = phongDb.Anh;
                }

                // Kiểm tra trùng số phòng
                var existingPhong = db.PHONGs.FirstOrDefault(p => p.SoPH == phong.SoPH && p.MaPH != phong.MaPH);
                if (existingPhong != null)
                {
                    ModelState.AddModelError("", "Số phòng đã tồn tại. Vui lòng chọn mã khác.");
                    return View(phong);
                }

                // Cập nhật thông tin phòng
                phongDb.SoPH = phong.SoPH;
                phongDb.Mota = phong.Mota;
                phongDb.TrangThai = phong.TrangThai;
                phongDb.Gia = phong.Gia;
                phongDb.MaLoai = phong.MaLoai;
                phongDb.NoiThat = phong.NoiThat;
                phongDb.DienTich = phong.DienTich;
                phongDb.Anh = phong.Anh;

                // Cập nhật lại thông tin đặt phòng
                var datphongDB = db.DATPHONGs.SingleOrDefault(n => n.MaPH == phong.MaPH);
                if (datphongDB != null)
                {
                    datphongDB.NgayNhan = phong.CheckIn;
                    datphongDB.NgayTra = phong.CheckOut;
                }

                // Lưu thay đổi vào cơ sở dữ liệu
                db.SaveChanges();

                return RedirectToAction("DanhSachPhong");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Đã xảy ra lỗi: {ex.Message}");
            }

            return View(phong);
        }



        [HttpGet]
        public ActionResult TraPhong(string MaPH)
        {
            // Kiểm tra phòng tồn tại hay không
            var phong = db.PHONGs.SingleOrDefault(p => p.MaPH == MaPH);
            var datphong = db.DATPHONGs.SingleOrDefault(d => d.MaPH == MaPH);
            var kh = db.KHACHHANGs.SingleOrDefault(k => k.MaKH == datphong.MaKH);
            if (phong == null&&phong.TrangThai=="Occupied")
            {
                ModelState.AddModelError("", "Phòng không tồn tại.");
                return RedirectToAction("DanhSachPhong");
            }

            try
            {
                // Lưu dữ liệu vào bảng LichSuTraPhong
                var lichSuTraPhong = new LichSuTraPhong
                {
                    MaPH = MaPH,
                    SoPH = phong.SoPH,
                    MaKH = kh.MaKH, 
                    HoTen = kh.HoTen,
                    DienThoai = kh.DienThoai,
                    CCCD = kh.CCCD,
                    Email = kh.Email,
                    Gia = phong.Gia,
                    NgayNhan = phong.CheckIn,
                    NgayTra = DateTime.Now
                };

                db.LichSuTraPhongs.Add(lichSuTraPhong);

                // Cập nhật trạng thái phòng thành "Available"
                phong.TrangThai = "Available";
                var datPhong = db.DATPHONGs.SingleOrDefault(dp => dp.MaPH == MaPH);
                if (datPhong != null)
                {
                    db.DATPHONGs.Remove(datPhong);
                }

                // Lưu thay đổi
                db.SaveChanges();

                TempData["SuccessMessage"] = "Trả phòng thành công!";
                return RedirectToAction("DanhSachPhong");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Đã xảy ra lỗi: {ex.Message}");
                return RedirectToAction("DanhSachPhong");
            }
        }


    }
}