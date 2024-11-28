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
        QL_KhachSanEntities db=new QL_KhachSanEntities();
        public ActionResult DanhSachPhong()
        {
            var phongList = db.PHONGs.ToList();
            return View(phongList);
        }
        public ActionResult Chitietphong(string MaPH)
        {
            // Tìm sách theo mã sách (id)
            var phong = db.PHONGs.FirstOrDefault(s => s.MaPH == MaPH);

            if (phong == null)
            {
                return HttpNotFound(); // Nếu không tìm thấy sách, trả về lỗi 404
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

            ViewBag.Maloai = new SelectList(db.LOAIPHONGs.ToList().OrderBy(n => n.TenLoai),
                                          "MaLoai", "TenLoai", phong.MaLoai);

            return View(phong);
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(FormCollection f, HttpPostedFileBase fFileUpload)
        {
            string MaPH = f["sMaPhong"];
            var phong = db.PHONGs.SingleOrDefault(n => n.MaPH == MaPH);

            ViewBag.Maloai = new SelectList(db.LOAIPHONGs.ToList().OrderBy(n => n.TenLoai),
                                          "MaLoai", "TenLoai", phong.MaLoai);

            if (ModelState.IsValid)
            {
                if (fFileUpload != null) // Kiểm tra xem có file ảnh được upload lên không
                {
                    // Lấy tên file
                    var sFileName = Path.GetFileName(fFileUpload.FileName);

                    // Lấy đường dẫn lưu file
                    var path = Path.Combine(Server.MapPath("~/Images"), sFileName);

                    // Kiểm tra xem file đã tồn tại chưa
                    if (!System.IO.File.Exists(path))
                    {
                        // Lưu file lên server
                        fFileUpload.SaveAs(path);
                    }
                    phong.Anh = sFileName;
                }
                // Lưu Sách vào CSDL
                phong.SoPH = f["iSoPhong"];
                phong.Mota = f["sMoTa"];
                phong.TrangThai = f["sTrangThai"];
                phong.Gia = float.Parse(f["iGia"]);
                phong.MaLoai = f["MaLoai"];
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(phong);
        }
    }
}