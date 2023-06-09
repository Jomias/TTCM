﻿using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OganiShop.Entities;
using OganiShop.Helpers;
using OganiShop.Models;
using OganiShop.Utils;
using System.Security.Claims;

namespace OganiShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class CategoryController : Controller
    {
        private readonly OganiShopContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IFileStorageService _fileStorageService;
        private readonly string containerName = "category";
        public CategoryController(OganiShopContext dbContext, IMapper mapper, IFileStorageService fileStorageService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _fileStorageService = fileStorageService;
        }
        public IActionResult Index()
        {
            var temp =_dbContext.Categories.Where(x => x.IsDeleted == false).ToList();
            return View(temp);
        }

        public string GetAccount()
        {
            var claims = HttpContext.User.Identity as ClaimsIdentity;
            if (claims != null)
            {
                var account = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (account != null)
                {
                    return account;
                }
            }
            return string.Empty;
        }
        public IActionResult AddOrEdit(int Id = 0)
        {
            ViewBag.Id = Id;
            var temp = _dbContext.Categories.Find(Id);
            if (temp == null)
            {
                return View(new CategoryModel());
            }

            return View(_mapper.Map<CategoryModel>(temp));
        }
        public IActionResult Delete(int Id)
        {

            var entity = _dbContext.Categories.Find(Id);
            if (entity == null) 
            {
                TempData["Message"] = "Can't Remove";
                return RedirectToAction("Index");
            }
            entity.IsDeleted = true;
            entity.UpdatedDate = DateTime.Now;
            entity.UpdatedBy = GetAccount();
            _dbContext.Update(entity);
            _dbContext.SaveChanges();
            TempData["Message"] = "Remove Category Successfully";
            return RedirectToAction("Index");

        }
        [HttpPost]
        public async Task<IActionResult> AddOrEdit(CategoryModel model, IFormFile? ImageFile)
        {
            ViewBag.Id = model.Id == null ? 0 : model.Id;
            if (ImageFile != null) 
            {
                if (model.Image == null)
                {
                    model.Image = await _fileStorageService.SaveFile(containerName, ImageFile);
                }
                else
                {
                    model.Image = await _fileStorageService.EditFile(containerName, ImageFile, model.Image);
                }
                ModelState["Image"].ValidationState = ModelValidationState.Valid;
                ModelState["Image"].RawValue = model.Image;
            }
            if (model.Slug == null && model.Name != null)
            {
                model.Slug = Slug.ToUrlSlug(model.Name);
                ModelState.SetModelValue("Slug", new ValueProviderResult(model.Slug));

            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var temp = _mapper.Map<Category>(model);
            if (model.Id == null)
            {
                _dbContext.Add(temp);
                _dbContext.SaveChanges();
                TempData["Message"] = "Add Category Successfully";
                return RedirectToAction("Index");
            }
            var account = GetAccount();

            temp.UpdatedDate = DateTime.Now;
            temp.UpdatedBy = account;
            _dbContext.Update(temp);
            _dbContext.SaveChanges();
            TempData["Message"] = "Edit Category Successfully";
            return RedirectToAction("Index");

        }
    }
}
