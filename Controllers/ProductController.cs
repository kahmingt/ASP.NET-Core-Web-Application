using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApp.DbContext;
using WebApp.DbContext.Entities;
using WebApp.Extensions;
using WebApp.Models;

namespace WebApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ProductController(ApplicationDbContext db)
        {
            _db = db;
        }

        private void TriggerBootstrapAlerts(BootstrapAlertType Type, string Message)
        {
            string _type;
            switch (Type)
            {
                case BootstrapAlertType.Info: _type = "alert-info"; break;
                case BootstrapAlertType.Warning: _type = "alert-warning"; break;
                case BootstrapAlertType.Danger: _type = "alert-danger"; break;
                case BootstrapAlertType.Success: _type = "alert-success"; break;
                default: _type = "alert-danger"; break;
            }

            TempData.Put("Alerts", new BootstrapAlert
            {
                Type = _type,
                Message = Message
            }
            );
        }


        private bool CreateOrders(ProductDetails productDetails, out int id)
        {
            id = 0;
            var model = new Products
            {
                CategoryId = productDetails.CategoryID,
                ProductName = productDetails.ProductName,
                SupplierId = productDetails.SupplierID,
                UnitsInStock = productDetails.UnitInStock,
                UnitPrice = productDetails.UnitPrice
            };

            try
            {
                _db.SaveChanges();
                id = model.ProductId;
            }
            catch (DbUpdateException exception)
            {
                TriggerBootstrapAlerts(BootstrapAlertType.Danger, "500 Internal Server Error. " + exception.Message + ".");
                return false;
            }

            TriggerBootstrapAlerts(BootstrapAlertType.Success, "Successfully create product.");
            return true;
        }

        private ProductDetails RetrieveProduct(int? id, string? mode)
        {
            var model = new ProductDetails();

            if (id == null)
            {
                PopulateCategoryDropDownList();
                PopulateSupplierDropDownList();

                model = new ProductDetails()
                {
                    Mode = "Edit",
                };
            }
            else
            {
                PopulateCategoryDropDownList(from m in _db.Products where m.ProductId == id select m.CategoryId);
                PopulateSupplierDropDownList(from m in _db.Products where m.ProductId == id select m.SupplierId);

                model = (from m in _db.Products
                         where m.ProductId == id && !m.IsDeleted
                         select new ProductDetails()
                         {
                             Mode = mode,
                             CategoryID = m.CategoryId,
                             CategoryName = m.Category.CategoryName,
                             ProductID = m.ProductId,
                             ProductName = m.ProductName,
                             SupplierID = m.SupplierId.Value,
                             SupplierName = m.Supplier.CompanyName,
                             UnitInStock = m.UnitsInStock,
                             UnitPrice = m.UnitPrice.Value
                         }).FirstOrDefault();
            }
            return model;
        }

        private bool UpdateOrders(int? id, ProductDetails productDetails)
        {
            var model = (from m in _db.Products
                         where m.ProductId == id && !m.IsDeleted
                         select m).FirstOrDefault();

            if (model != null)
            {
                model.CategoryId = productDetails.CategoryID;
                model.ProductName = productDetails.ProductName;
                model.SupplierId = productDetails.SupplierID;
                model.UnitPrice = productDetails.UnitPrice;
                model.UnitsInStock = productDetails.UnitInStock;

                try
                {
                    _db.SaveChanges();
                }
                catch (DbUpdateException exception)
                {
                    TriggerBootstrapAlerts(BootstrapAlertType.Danger, "500 Internal Server Error. " + exception.Message + ".");
                    return false;
                }

                TriggerBootstrapAlerts(BootstrapAlertType.Success, "Successfully update product.");
                return true;
            }
            else
            {
                TriggerBootstrapAlerts(BootstrapAlertType.Danger, "Bad Request!");
            }
            return false;
        }

        private bool DeleteProduct(int? id)
        {
            var model = (from m in _db.Products
                         where m.ProductId == id && !m.IsDeleted
                         select m).FirstOrDefault();

            if (model != null)
            {
                model.IsDeleted = true;

                try
                {
                    _db.SaveChanges();
                }
                catch (DbUpdateException exception)
                {
                    TriggerBootstrapAlerts(BootstrapAlertType.Danger, "500 Internal Server Error. " + exception.Message + ".");
                    return false;
                }

                TriggerBootstrapAlerts(BootstrapAlertType.Success, "Successfully delete product.");
                return true;
            }
            else
            {
                TriggerBootstrapAlerts(BootstrapAlertType.Danger, "Bad Request!");
            }
            return false;
        }

        private void PopulateCategoryDropDownList(object selectedCategory = null)
        {
            ViewBag.CategoryListing = new SelectList(
                _db.Categories.Where(x => !x.IsDeleted).AsNoTracking(),
                "CategoryId",
                "CategoryName",
                selectedCategory);
        }

        private void PopulateSupplierDropDownList(object selectedSupplier = null)
        {
            ViewBag.SupplierListing = new SelectList(
                _db.Suppliers.Where(x => !x.IsDeleted).AsNoTracking(),
                "SupplierId",
                "CompanyName",
                selectedSupplier);
        }


        [HttpGet]
        public IActionResult PartialViewDetails(int? id, string? mode)
        {
            if (string.IsNullOrWhiteSpace(mode) || !(mode == "View" || mode == "Edit"))
            {
                return BadRequest("Bad request!");
            }

            return PartialView(RetrieveProduct(id, mode));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(int? id, string button, string mode, ProductDetails productDetails)
        {
            TempData.Clear();

            if (!ModelState.IsValid && mode != "Edit" && !(button == "ButtonUpdate" || button == "ButtonCreate" || button == "ButtonDelete"))
            {
                return BadRequest("Invalid");
            }

            int newId;

            if (button == "ButtonCreate")
            {
                CreateOrders(productDetails, out newId);
            }

            if (button == "ButtonUpdate")
            {
                UpdateOrders(id, productDetails);
            }

            if (button == "ButtonDelete")
            {
                DeleteProduct(id);
            }

            return RedirectToAction("Index");
        }

        public IActionResult Index()
        {
            var model = (from m in _db.Products
                         where !m.IsDeleted
                         orderby m.CategoryId, m.ProductId
                         select new ProductListing
                         {
                             CategoryID = m.CategoryId,
                             CategoryName = m.Category.CategoryName,
                             ProductID = m.ProductId,
                             ProductName = m.ProductName,
                             UnitInStock = m.UnitsInStock,
                             UnitPrice = m.UnitPrice.Value,
                         })
                         .AsNoTracking()
                         .ToList();

            return View(model);
        }
    }
}