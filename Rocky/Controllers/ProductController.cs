using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rocky.Data;
using Rocky.Models;
using Rocky.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rocky.Controllers
{
    [Authorize(Roles = WC.AdminRole)]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            IEnumerable<Product> objList = _db.Product.Include(p => p.Category).Include(p => p.ApplicationType);

            //foreach (Product product in objList)
            //{
            //    product.Category = _db.Category.FirstOrDefault(c => c.Id == product.CategoryId);
            //    product.ApplicationType = _db.ApplicationType.FirstOrDefault(c => c.Id == product.ApplicationTypeId);
            //}
            return View(objList);
        }
        public IActionResult Upsert(int? id)
        {
            //IEnumerable<SelectListItem> categories = _db.Category
            //    .Select(c => new SelectListItem { Text = c.Name, Value = c.Id.ToString() });
            //ViewData["CategoryDropDown"] = categories;
            //Product product = new Product();
            ProductVM productVM = new ProductVM()
            {
                Product = new Product(),
                CategorySelectList = _db.Category.Select(c => new SelectListItem { Text = c.Name, Value = c.Id.ToString() }),
                ApplicationTypeSelectList = _db.ApplicationType.Select(a => new SelectListItem { Text = a.Name, Value = a.Id.ToString() })
            };
            if (id == null)
            {
                return View(productVM);
            }
            else
            {
                productVM.Product = _db.Product.Find(id);
                if (productVM.Product == null)
                {
                    return NotFound();
                }
                return View(productVM);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductVM productVM)
        {
            if (ModelState.IsValid)
            {
                var files = HttpContext.Request.Form.Files;
                string webRootPath = _webHostEnvironment.WebRootPath;
                if (productVM.Product.Id == 0)
                {
                    //Creating
                    string upload = webRootPath + WC.ImagePath;
                    string fileName = Guid.NewGuid().ToString();
                    string extension = Path.GetExtension(files[0].FileName);

                    using (var fileStream = new FileStream(Path.Combine(upload,fileName + extension),FileMode.Create))
                    {
                        files[0].CopyTo(fileStream);
                    }
                    productVM.Product.Image = fileName + extension;

                    _db.Product.Add(productVM.Product);
                }
                else
                {
                    //Updating
                    Product product = _db.Product.AsNoTracking().FirstOrDefault(p => p.Id == productVM.Product.Id);
                    if (files.Count > 0)
                    {
                        string upload = webRootPath + WC.ImagePath;
                        string fileName = Guid.NewGuid().ToString();
                        string extension = Path.GetExtension(files[0].FileName);
                        var oldFilePath = Path.Combine(upload, product.Image);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                        using (var fileStream = new FileStream(Path.Combine(upload, fileName + extension), FileMode.Create))
                        {
                            files[0].CopyTo(fileStream);
                        }
                        productVM.Product.Image = fileName + extension;
                    }
                    else
                    {
                        productVM.Product.Image = product.Image;
                    }
                    _db.Product.Update(productVM.Product);
                }
                _db.SaveChanges();
                return RedirectToAction("Index", "Product");
            }
            productVM.CategorySelectList = _db.Category.Select(c => new SelectListItem { Text = c.Name, Value = c.Id.ToString() });
            productVM.ApplicationTypeSelectList = _db.ApplicationType.Select(a => new SelectListItem { Text = a.Name, Value = a.Id.ToString() });

            return View(productVM);
        }
        
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            Product product = _db.Product.Include(p => p.Category).Include(p => p.ApplicationType).FirstOrDefault(p => p.Id == id);
            //product.Category = _db.Category.Find(product.CategoryId);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost,ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteProduct(int? id)
        {
            Product product = _db.Product.Find(id);
            if (product == null) return NotFound();
            string upload = _webHostEnvironment.WebRootPath + WC.ImagePath;
            var oldFilePath = Path.Combine(upload, product.Image);
            if (System.IO.File.Exists(oldFilePath))
            {
                System.IO.File.Delete(oldFilePath);
            }
            _db.Product.Remove(product);
            _db.SaveChanges();
            return RedirectToAction("Index", "Product");
        }

    }
}
