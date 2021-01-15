using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _UnitOfWork;

        public CategoryController(IUnitOfWork unitOfWork)
        {
            _UnitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Upsert(int? id)
        {
            Category category = new Category();

            if (id==null)
            {
                //create
                return View(category);
            }
            //edit
            category = await _UnitOfWork.Category.GetAsync(id.GetValueOrDefault());
            if(category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(Category category)
        {
            if(ModelState.IsValid)
            {
                if(category.Id == 0)
                {
                    await _UnitOfWork.Category.AddAsync(category);
                }
                else
                {
                    _UnitOfWork.Category.Update(category);
                }
                _UnitOfWork.Save();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        #region API CALLS
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var allObj = await _UnitOfWork.Category.GetAllAsync();
            return Json(new { data = allObj });
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var objFormDb = await _UnitOfWork.Category.GetAsync(id);
            if(objFormDb == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            await _UnitOfWork.Category.RemoveAsync(objFormDb);
            _UnitOfWork.Save();

            return Json(new { success = true, message = "Delete Successful" });
        }

        #endregion
    }
}
