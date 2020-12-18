using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyBook.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _UnitOfWork;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment hostEnvironment)
        {
            _UnitOfWork = unitOfWork;
            _hostEnvironment = hostEnvironment;

        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new ProductVM()
            {
                Product = new Product(),
                CategoryList = _UnitOfWork.Category.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
                CoverTypeList = _UnitOfWork.CoverType.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                })
            };

            if (id==null)
            {
                //create
                return View(productVM);
            }
            //edit
            productVM.Product = _UnitOfWork.Product.Get(id.GetValueOrDefault());
            if(productVM.Product == null)
            {
                return NotFound();
            }
            return View(productVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductVM productVM)
        {
            if (ModelState.IsValid)
            {
                string webRootPath = _hostEnvironment.WebRootPath;
                var files = HttpContext.Request.Form.Files;
                if(files.Count > 0)
                {
                    string fileName = Guid.NewGuid().ToString();
                    var uploads = Path.Combine(webRootPath, @"images\products");
                    var extenstion = Path.GetExtension(files[0].FileName);

                    if (productVM.Product.ImageUrl != null)
                    {
                        //ceci est une modification et nous devons supprimer l'ancienne image
                        var imagePath = Path.Combine(webRootPath, productVM.Product.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }
                    using (var filesStreams = new FileStream(Path.Combine(uploads, fileName + extenstion), FileMode.Create))
                    {
                        files[0].CopyTo(filesStreams);
                    }
                    productVM.Product.ImageUrl = @"\images\products\" + fileName + extenstion;
                }
                else
                {
                    // mise à jour quand on ne change pas l'image
                    if (productVM.Product.Id != 0)
                    {
                        Product objFromDb = _UnitOfWork.Product.Get(productVM.Product.Id);
                        productVM.Product.ImageUrl = objFromDb.ImageUrl;
                    }
                }
                if (productVM.Product.Id == 0)
                {
                    _UnitOfWork.Product.Add(productVM.Product);

                }
                else
                {
                    _UnitOfWork.Product.Update(productVM.Product);
                }
                _UnitOfWork.Save();
                return RedirectToAction(nameof(Index));
            }
            else
            {
                //IEnumerable<Category> CatList = await _UnitOfWork.Category.GetAllAsync();
                //productVM.CategoryList = CatList.Select(i => new SelectListItem
                productVM.CategoryList = _UnitOfWork.Category.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                });
                productVM.CoverTypeList = _UnitOfWork.CoverType.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                });
                if (productVM.Product.Id != 0)
                {
                    productVM.Product = _UnitOfWork.Product.Get(productVM.Product.Id);
                }
            }
            return View(productVM);
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var allObj = _UnitOfWork.Product.GetAll(includeProperties: "Category,CoverType");
            return Json(new { data = allObj });
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var objFromDb = _UnitOfWork.Product.Get(id);
            if (objFromDb == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            string webRootPath = _hostEnvironment.WebRootPath;
            var imagePath = Path.Combine(webRootPath, objFromDb.ImageUrl.TrimStart('\\'));
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
            _UnitOfWork.Product.Remove(objFromDb);
            _UnitOfWork.Save();
            return Json(new { success = true, message = "Delete Successful" });

        }

        #endregion
    }
}
