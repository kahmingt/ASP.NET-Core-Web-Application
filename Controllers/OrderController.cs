using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text;
using WebApp.DbContext.Entities;
using WebApp.DbContext;
using WebApp.Models;

namespace WebApp.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;

        public OrderController(ApplicationDbContext db)
        {
            _db = db;
        }

        private void TriggerBootstrapAlerts(AlertType Type, string Message)
        {
            string _type;
            switch (Type)
            {
                case AlertType.Info: _type = "alert-info"; break;
                case AlertType.Warning: _type = "alert-warning"; break;
                case AlertType.Danger: _type = "alert-danger"; break;
                case AlertType.Success: _type = "alert-success"; break;
                default: _type = "alert-danger"; break;
            }

            TempData["Alerts"] = new Alerts
            {
                Type = _type,
                Message = Message
            };
        }

        private string Decrypt(string encrypted)
        {
            byte[] data = Convert.FromBase64String(encrypted);
            return Encoding.UTF8.GetString(data);
        }



        [HttpGet]
        public JsonResult GetCustomerDetails(string data)
        {
            string id = Decrypt(data);
            Customers ThisCustomer = _db.Customers.Where(x => x.CustomerId == id && !x.IsDeleted).FirstOrDefault();

            var address = "";
            address += (string.IsNullOrWhiteSpace(ThisCustomer.Address) ? "" : ThisCustomer.Address);
            address += (string.IsNullOrWhiteSpace(ThisCustomer.PostalCode) ? "" : ",\r" + ThisCustomer.PostalCode);
            address += (string.IsNullOrWhiteSpace(ThisCustomer.City) ? "" : ",\r" + ThisCustomer.City);
            address += (string.IsNullOrWhiteSpace(ThisCustomer.Region) ? "" : ", " + ThisCustomer.Region);

            return Json(new
            {
                Address = address,
                Contact = ThisCustomer.Phone
            });
        }

        [HttpGet]
        public JsonResult GetCategoryList()
        {
            return Json(_db.Categories.Select(x => new SelectListItem
            {
                Text = x.CategoryName,
                Value = x.CategoryId.ToString()
            }).ToList()
            );
        }

        [HttpGet]
        public JsonResult GetProductList(string data)
        {
            int id = Convert.ToInt32(Decrypt(data));
            return Json(_db.Products
                            .Where(x => x.CategoryId == id && x.UnitsInStock > 0 && !x.IsDeleted)
                            .Select(x => new SelectListItem
                            {
                                Text = x.ProductName,
                                Value = x.ProductId.ToString()
                            }).ToList()
            );
        }

        [HttpGet]
        public JsonResult GetProductDetails(string data)
        {
            int id = int.Parse(Decrypt(data));
            Products ThisProduct = _db.Products.Where(x => x.ProductId == id && !x.IsDeleted).FirstOrDefault();
            return Json(new
            {
                ProductUnitsInStock = ThisProduct.UnitsInStock,
                ProductUnitPrice = ThisProduct.UnitPrice,
            });
        }

        [HttpGet]
        public ActionResult PartialViewOrderDetails(OrderDetailsModel OrderDetailsModel)
        {
            var NestedOrderModel = new NestedOrderModel();
            ViewBag.DropDownList_CategoryName = _db.Categories.Where(x => !x.IsDeleted).OrderBy(x => x.CategoryId).Select(x => new SelectListItem
            {
                Text = x.CategoryName,
                Value = x.CategoryId.ToString()
            });

            // Update
            if (OrderDetailsModel.CategoryID != 0 && OrderDetailsModel.ProductID != 0)
            {
                ViewBag.DropDownList_ProductName = _db.Products.Where(x => !x.IsDeleted).OrderBy(x => x.ProductId).Select(x => new SelectListItem
                {
                    Text = x.ProductName,
                    Value = x.ProductId.ToString()
                });

                NestedOrderModel = new NestedOrderModel
                {
                    OrderDetailsModel = new OrderDetailsModel
                    {
                        CategoryID = OrderDetailsModel.CategoryID,
                        CategoryName = OrderDetailsModel.CategoryName,
                        OrderID = OrderDetailsModel.OrderID,
                        OrderQuantity = OrderDetailsModel.OrderQuantity,
                        ProductID = OrderDetailsModel.ProductID,
                        ProductName = OrderDetailsModel.ProductName,
                        ProductUnitPrice = OrderDetailsModel.ProductUnitPrice
                    },
                };
            }
            else
            {
                NestedOrderModel = new NestedOrderModel
                {
                    OrderDetailsModel = new OrderDetailsModel
                    {
                        OrderID = OrderDetailsModel.OrderID
                    },
                };
            }

            return PartialView(NestedOrderModel);
        }



        private Orders CreateOrders(NestedOrderModel NestedOrderModel)
        {
            int NewOrderID = 0;
            var mOrder = new Orders
            {
                OrderDate = NestedOrderModel.OrderModel.OrderDate,
                EmployeeId = NestedOrderModel.OrderModel.EmployeeID,
                CustomerId = NestedOrderModel.OrderModel.CustomerID
            };
            _db.Orders.Add(mOrder);

            try
            {
                _db.SaveChanges();
                NewOrderID = mOrder.OrderId;
            }
            catch (DbUpdateException exception)
            {
                Debug.WriteLine(exception.Message);
                TriggerBootstrapAlerts(AlertType.Danger, "500 Internal Server Error. " + exception.Message + ".");
            }

            return _db.Orders.Find(NewOrderID);
        }

        private OrderDetails CreateOrderDetails(NestedOrderModel NestedOrderModel)
        {
            var mOrderDetails = new OrderDetails
            {
                OrderId = NestedOrderModel.OrderDetailsModel.OrderID.Value,
                ProductId = NestedOrderModel.OrderDetailsModel.ProductID,
                Quantity = (short)NestedOrderModel.OrderDetailsModel.OrderQuantity
            };
            _db.OrderDetails.Add(mOrderDetails);

            try
            {
                _db.SaveChanges();
            }
            catch (DbUpdateException exception)
            {
                Debug.WriteLine(exception.Message);
                TriggerBootstrapAlerts(AlertType.Danger, "500 Internal Server Error. " + exception.Message + ".");
            }

            return mOrderDetails;
        }

        private bool DeleteOrders(int OrderID)
        {
            var mOrders = _db.Orders.Where(x => x.OrderId == OrderID && !x.IsDeleted).FirstOrDefault();
            IList<OrderDetails> mOrderDetails = (from x in _db.OrderDetails where x.OrderId == OrderID && !x.IsDeleted select x).ToList();

            if (mOrderDetails != null)
            {
                foreach (OrderDetails OrderDetailsList in mOrderDetails)
                {
                    DeleteOrderDetails(OrderID, OrderDetailsList.ProductId);
                }
                //_db.Orders.Remove(mOrders);
                mOrders.IsDeleted = true;

                try
                {
                    _db.SaveChanges();
                }
                catch (DbUpdateException exception)
                {
                    Debug.WriteLine(exception.Message);
                    TriggerBootstrapAlerts(AlertType.Danger, "500 Internal Server Error. " + exception.Message + ".");
                    return false;
                }
                return true;
            }
            else
            {
                TriggerBootstrapAlerts(AlertType.Danger, "Bad Request. ID not found.");
            }
            return false;
        }

        private bool DeleteOrderDetails(int OrderID, int ProductID)
        {
            var mOrderDetails = _db.OrderDetails.FirstOrDefault(x => x.OrderId == OrderID && x.ProductId == ProductID && !x.IsDeleted);
            if (mOrderDetails != null)
            {
                //_db.OrderDetails.Remove(mOrderDetails);
                mOrderDetails.IsDeleted = true;

                try
                {
                    _db.SaveChanges();
                }
                catch (DbUpdateException exception)
                {
                    Debug.WriteLine(exception.Message);
                    TriggerBootstrapAlerts(AlertType.Danger, "500 Internal Server Error. " + exception.Message + ".");
                    return false;
                }
                return true;
            }
            else
            {
                TriggerBootstrapAlerts(AlertType.Danger, "Bad Request. ID not found.");
            }

            return false;
        }

        private NestedOrderModel RetrieveOrder(int OrderID)
        {
            var mOrder = _db.Orders.Find(OrderID);
            if (mOrder != null)
            {
                var mCustomer = _db.Customers.Find(mOrder.CustomerId);
                var mEmployee = _db.Employees.Find(mOrder.EmployeeId);

                return new NestedOrderModel()
                {
                    OrderModel = new OrderModel()
                    {
                        CustomerAddress = mCustomer.Address + ",\r" +
                                            mCustomer.PostalCode + ",\r" +
                                            mCustomer.City + ", " + mCustomer.Country,
                        CustomerContactNumber = mCustomer.Phone,
                        CustomerID = mOrder.CustomerId,
                        CustomerName = mCustomer.ContactName,
                        EmployeeID = mOrder.EmployeeId,
                        EmployeeName = mEmployee.FirstName + " " +
                                        mEmployee.LastName,
                        OrderDate = mOrder.OrderDate,
                        OrderGrandTotal = _db.OrderDetails
                                            .Where(x => x.OrderId == mOrder.OrderId && !x.IsDeleted)
                                            .Select(m => m.Quantity * m.Product.UnitPrice.Value)
                                            .Sum(),
                        OrderDetailList = _db.OrderDetails
                                            .Where(x => x.OrderId == mOrder.OrderId && !x.IsDeleted)
                                            .OrderBy(x => x.Product.CategoryId)
                                            .ThenBy(x => x.ProductId)
                                            .Select(m => new OrderDetailsModel()
                                            {
                                                CategoryID = m.Product.CategoryId,
                                                CategoryName = m.Product.Category.CategoryName,
                                                OrderTotalCost = m.Quantity * m.Product.UnitPrice.Value,
                                                OrderID = m.OrderId,
                                                OrderQuantity = m.Quantity,
                                                ProductID = m.ProductId,
                                                ProductName = m.Product.ProductName,
                                                ProductUnitPrice = m.Product.UnitPrice.Value,
                                            })
                                            .ToList(),
                    }
                };
            }
            return new NestedOrderModel();
        }

        private Orders UpdateOrders(int OrderID, NestedOrderModel NestedOrderModel)
        {
            var mOrder = _db.Orders.Find(OrderID);
            if (mOrder != null)
            {
                mOrder.OrderDate = NestedOrderModel.OrderModel.OrderDate;
                mOrder.EmployeeId = NestedOrderModel.OrderModel.EmployeeID;
                mOrder.CustomerId = NestedOrderModel.OrderModel.CustomerID;

                try
                {
                    _db.SaveChanges();
                }
                catch (DbUpdateException exception)
                {
                    Debug.WriteLine(exception.Message);
                    TriggerBootstrapAlerts(AlertType.Danger, "500 Internal Server Error. " + exception.Message + ".");
                }
            }

            return mOrder;
        }

        private OrderDetails UpdateOrderDetails(int OrderID, int ProductID, NestedOrderModel NestedOrderModel)
        {
            var mOrderDetails = new OrderDetails();
            if (DeleteOrderDetails(OrderID, ProductID))
            {
                mOrderDetails = new OrderDetails
                {
                    OrderId = OrderID,
                    ProductId = NestedOrderModel.OrderDetailsModel.ProductID,
                    Quantity = (short)NestedOrderModel.OrderDetailsModel.OrderQuantity
                };
                _db.OrderDetails.Add(mOrderDetails);

                try
                {
                    _db.SaveChanges();
                }
                catch (DbUpdateException exception)
                {
                    Debug.WriteLine(exception.Message);
                    TriggerBootstrapAlerts(AlertType.Danger, "500 Internal Server Error. " + exception.Message + ".");
                }
            }
            return mOrderDetails;
        }



        public IActionResult Details(int? OrderID, string Mode)
        {
            if (string.IsNullOrEmpty(Mode) || !(Mode == "Edit" || Mode == "View"))
            {
                TriggerBootstrapAlerts(AlertType.Danger, "Bad Request 3. Invalid model.");
                return RedirectToAction("Index");
            }

            ViewBag.DropDownList_CustomerName = _db.Customers.OrderBy(x => x.ContactName).Select(x => new SelectListItem
            {
                Text = x.ContactName,
                Value = x.CustomerId.ToString()
            });
            ViewBag.DropDownList_EmployeeName = _db.Employees.OrderBy(x => x.FirstName).ThenBy(x => x.LastName).Select(x => new SelectListItem
            {
                Text = x.FirstName + " " + x.LastName,
                Value = x.EmployeeId.ToString()
            });

            var mNestedOrderModel = new NestedOrderModel();
            if (OrderID == null)
            {
                if (Mode == "Edit")
                {
                    mNestedOrderModel = new NestedOrderModel
                    {
                        OrderModel = new OrderModel
                        {
                            OrderDate = DateTime.Now,
                        },
                    };
                }
                else
                {
                    TriggerBootstrapAlerts(AlertType.Danger, "Bad Request 5. Invalid model.");
                    return RedirectToAction("Index");
                }
            }
            else
            {
                mNestedOrderModel = RetrieveOrder(OrderID.Value);
                if (mNestedOrderModel == null)
                {
                    TriggerBootstrapAlerts(AlertType.Danger, "Bad Request 4. Invalid model.");
                    return RedirectToAction("Index");
                }
            }

            return View(mNestedOrderModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Details(int? OrderID, int? ProductID, string Mode, string button, NestedOrderModel NestedOrderModel)
        {
            ModelState.Clear();
            TempData.Clear();

            if (NestedOrderModel is null ||
                string.IsNullOrEmpty(Mode) || Mode != "Edit" ||
                string.IsNullOrEmpty(button) || !(button == "CreateOrders" || button == "UpdateOrders" || button == "DeleteOrders" || button == "CreateOrderDetails" || button == "UpdateOrderDetails" || button == "DeleteOrderDetails"))
            {
                TriggerBootstrapAlerts(AlertType.Danger, "Bad Request 1. Invalid model.");
                return RedirectToAction("Details", new { OrderID = OrderID, Mode = "View" });
            }

            if (((button == "UpdateOrders" || button == "DeleteOrders") && !OrderID.HasValue) ||
                ((button == "CreateOrderDetails" || button == "UpdateOrderDetails" || button == "DeleteOrderDetails") && !(OrderID.HasValue && ProductID.HasValue)))
            {
                TriggerBootstrapAlerts(AlertType.Danger, "Bad Request 2. Invalid model.");
                return RedirectToAction("Details", new { OrderID = OrderID, Mode = "View" });
            }

            int? NewOrderID = null;
            var mOrder = new Orders();
            var mOrderDetails = new OrderDetails();

            if (button.Contains("Orders"))
            {
                if (button == "DeleteOrders")
                {
                    if (DeleteOrders(OrderID.Value))
                    {
                        TriggerBootstrapAlerts(AlertType.Success, "Successfully delete order report.");
                    }
                    else
                    {
                        TriggerBootstrapAlerts(AlertType.Danger, "Failed to delete order report.");
                    }
                    return RedirectToAction("Index");
                }
                else
                {
                    if (ModelState.IsValid && TryValidateModel(NestedOrderModel.OrderModel, "OrderModel"))
                    {
                        if (button == "CreateOrders")
                        {
                            mOrder = CreateOrders(NestedOrderModel);
                            if (mOrder == null)
                            {
                                TriggerBootstrapAlerts(AlertType.Danger, "Failed to create new order report.");
                            }
                            else
                            {
                                NewOrderID = mOrder.OrderId;
                                TriggerBootstrapAlerts(AlertType.Success, "Successfully create new order report.");
                            }
                        }
                        if (button == "UpdateOrders")
                        {
                            mOrder = UpdateOrders(OrderID.Value, NestedOrderModel);
                            if (mOrder == null)
                            {
                                TriggerBootstrapAlerts(AlertType.Danger, "Failed to update order report.");
                            }
                            else
                            {
                                TriggerBootstrapAlerts(AlertType.Success, "Successfully update order report.");
                            }
                            NewOrderID = OrderID.Value;
                        }
                    }
                    else
                    {
                        TriggerBootstrapAlerts(AlertType.Danger, "Bad Request. Invalid model.");
                        return RedirectToAction("Details", new { OrderID = OrderID, Mode = "View" });
                    }
                }
            }

            if (button.Contains("OrderDetails"))
            {
                if (button == "DeleteOrderDetails")
                {
                    if (DeleteOrderDetails(OrderID.Value, ProductID.Value))
                    {
                        TriggerBootstrapAlerts(AlertType.Success, "Successfully delete order details.");
                    }
                    else
                    {
                        TriggerBootstrapAlerts(AlertType.Danger, "Failed to delete order details.");
                    }
                }
                else
                {
                    if (ModelState.IsValid && TryValidateModel(NestedOrderModel.OrderDetailsModel, "OrderDetailsModel"))
                    {
                        if (button == "CreateOrderDetails")
                        {
                            mOrderDetails = CreateOrderDetails(NestedOrderModel);
                            if (mOrderDetails == null)
                            {
                                TriggerBootstrapAlerts(AlertType.Danger, "Failed to create order details.");
                            }
                            else
                            {
                                TriggerBootstrapAlerts(AlertType.Success, "Successfully create order details.");
                            }
                        }
                        if (button == "UpdateOrderDetails")
                        {
                            mOrderDetails = UpdateOrderDetails(OrderID.Value, ProductID.Value, NestedOrderModel);
                            if (mOrderDetails == null)
                            {
                                TriggerBootstrapAlerts(AlertType.Danger, "Failed to update order details.");
                            }
                            else
                            {
                                TriggerBootstrapAlerts(AlertType.Success, "Successfully update order details.");
                            }
                        }
                    }
                    else
                    {
                        TriggerBootstrapAlerts(AlertType.Danger, "Bad Request. Invalid model.");
                        return RedirectToAction("Details", new { OrderID = OrderID, Mode = "View" });
                    }
                }
                NewOrderID = OrderID.Value;
            }

            return RedirectToAction("Details", new { OrderID = NewOrderID, Mode = "View" });
        }

        public IActionResult Index(
            string Sort,
            string Search)
        {
            ViewData["Search"] = Search;
            ViewData["OrderDateSortParm"] = String.IsNullOrEmpty(Sort) ? "OrderDate" : (Sort == "OrderDate" ? "OrderDate_" : "OrderDate");
            ViewData["CustomerNameSortParm"] = Sort == "CustomerName" ? "CustomerName_" : "CustomerName";

            var Model = _db.OrderDetails
                .Where(x => x.IsDeleted == false)
                .GroupBy(x => x.OrderId)
                .Select(x => new OrderDetailsListIndex()
                {
                    OrderID = x.Key,
                    OrderDate = x.FirstOrDefault().Order.OrderDate,
                    CustomerName = x.FirstOrDefault().Order.Customer.ContactName,
                    OrderCount = _db.OrderDetails
                                    .Where(m => m.OrderId == x.Key && !m.IsDeleted)
                                    .Select(m => m.OrderId)
                                    .Count(),
                    TotalCost = _db.OrderDetails
                                    .Where(m => m.OrderId == x.Key && !m.IsDeleted)
                                    .Select(m => m.Quantity * m.Product.UnitPrice.Value)
                                    .Sum(),
                });
            //.OrderByDescending(x => x.OrderDate)
            //.ToList();

            if (!string.IsNullOrWhiteSpace(Search))
            {
                if (DateTime.TryParse(Search, out var dateTime))
                {
                    Model = Model.Where(s => s.OrderDate.ToShortDateString() == dateTime.ToShortDateString());
                }
                else
                {
                    Model = Model.Where(s => s.CustomerName == Search);
                }
            }

            switch (Sort)
            {
                case "OrderDate":
                    Model = Model.OrderBy(s => s.OrderDate).ThenBy(s => s.CustomerName);
                    break;
                case "CustomerName":
                    Model = Model.OrderBy(s => s.CustomerName);
                    break;
                case "CustomerName_":
                    Model = Model.OrderByDescending(s => s.CustomerName);
                    break;
                default:
                    Model = Model.OrderByDescending(s => s.OrderDate).ThenByDescending(s => s.CustomerName);
                    break;
            }

            return View(Model.ToList());
        }
    }
}
