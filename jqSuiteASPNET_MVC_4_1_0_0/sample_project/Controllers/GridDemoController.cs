using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using sample_project.Models;
using Trirand.Web.Mvc;

namespace sample_project.Controllers
{
    public class GridController : Controller
    {
        public ActionResult GridDemo()
        {
            // Get the model (setup) of the grid defined in the /Models folder.
            var gridModel = new OrdersJqGridModel();
            var ordersGrid = gridModel.OrdersGrid;

            // customize the default Orders grid model with custom settings
            // NOTE: you need to call this method in the action that fetches the data as well,
            // so that the models match
            SetUpGrid(ordersGrid);

            // Pass the custmomized grid model to the View
            return View(gridModel);
        }

        // This method is called when the grid requests data
        public JsonResult SearchGridDataRequested()
        {
            // Get both the grid Model and the data Model
            // The data model in our case is an autogenerated linq2sql database based on Northwind.
            var gridModel = new OrdersJqGridModel();
            var northWindModel = new NorthwindDataContext();

            // customize the default Orders grid model with our custom settings
            SetUpGrid(gridModel.OrdersGrid);

            // return the result of the DataBind method, passing the datasource as a parameter
            // jqGrid for ASP.NET MVC automatically takes care of paging, sorting, filtering/searching, etc
            return gridModel.OrdersGrid.DataBind(northWindModel.Orders);
        }

        public ActionResult EditRows(Order editedOrder)
        {
            // Get the grid and database (northwind) models
            var gridModel = new OrdersJqGridModel();
            var northWindModel = new NorthwindDataContext();                                    

            // If we are in "Edit" mode
            if (gridModel.OrdersGrid.AjaxCallBackMode == AjaxCallBackMode.EditRow)
            {
                // Get the data from and find the Order corresponding to the edited row
                Order order = (from o in northWindModel.Orders
                               where o.OrderID == editedOrder.OrderID
                               select o).First<Order>(); 

                // update the Order information
                order.OrderDate = editedOrder.OrderDate;
                order.CustomerID = editedOrder.CustomerID;
                order.Freight = editedOrder.Freight;
                order.ShipName = editedOrder.ShipName;

                northWindModel.SubmitChanges();
            }
            if (gridModel.OrdersGrid.AjaxCallBackMode == AjaxCallBackMode.AddRow)
            {
                // since we are adding a new Order, create a new istance
                Order order = new Order();
                // set the new Order information
                order.OrderID = (from o in northWindModel.Orders
                                 select o)
                                .Max<Order>(o => o.OrderID) + 1;
                order.OrderDate = editedOrder.OrderDate;
                order.CustomerID = editedOrder.CustomerID;
                order.Freight = editedOrder.Freight;
                order.ShipName = editedOrder.ShipName;

                northWindModel.Orders.InsertOnSubmit(order);
                northWindModel.SubmitChanges();
            }
            if (gridModel.OrdersGrid.AjaxCallBackMode == AjaxCallBackMode.DeleteRow)
            {                
                Order order = ( from o in northWindModel.Orders
                                where o.OrderID == editedOrder.OrderID
                                select o)
                               .First<Order>();                

                // delete the record                
                northWindModel.Orders.DeleteOnSubmit(order);
                northWindModel.SubmitChanges();
            }

            return RedirectToAction("GridDemo", "Grid");
        }

        private void SetUpGrid(JQGrid ordersGrid)
        {
            // Customize/change some of the default settings for this model
            // ID is a mandatory field. Must by unique if you have several grids on one page.
            ordersGrid.ID = "OrdersGrid";
            // Setting the DataUrl to an action (method) in the controller is required.
            // This action will return the data needed by the grid
            ordersGrid.DataUrl = Url.Action("SearchGridDataRequested");
            ordersGrid.EditUrl = Url.Action("EditRows");
            // show the search toolbar
            ordersGrid.ToolBarSettings.ShowSearchToolBar = true;
            ordersGrid.Columns.Find(c => c.DataField == "OrderID").Searchable = false;
            ordersGrid.Columns.Find(c => c.DataField == "OrderDate").Searchable = false;

            SetUpCustomerIDSearchDropDown(ordersGrid);
            SetUpFreightSearchDropDown(ordersGrid);
            SetShipNameSearchDropDown(ordersGrid);
            
            ordersGrid.ToolBarSettings.ShowEditButton = true;
            ordersGrid.ToolBarSettings.ShowAddButton = true;
            ordersGrid.ToolBarSettings.ShowDeleteButton = true;
            ordersGrid.EditDialogSettings.CloseAfterEditing = true;
            ordersGrid.AddDialogSettings.CloseAfterAdding = true;

            // setup the dropdown values for the CustomerID editing dropdown
            SetUpCustomerIDEditDropDown(ordersGrid);
        }

        private void SetUpCustomerIDSearchDropDown(JQGrid ordersGrid)
        {
            // setup the grid search criteria for the columns
            JQGridColumn customersColumn = ordersGrid.Columns.Find(c => c.DataField == "CustomerID");
            customersColumn.Searchable = true;

            // DataType must be set in order to use searching
            customersColumn.DataType = typeof(string);
            customersColumn.SearchToolBarOperation = SearchOperation.IsEqualTo;
            customersColumn.SearchType = SearchType.DropDown;

            // Populate the search dropdown only on initial request, in order to optimize performance
            if (ordersGrid.AjaxCallBackMode == AjaxCallBackMode.RequestData)
            {
                var northWindModel = new NorthwindDataContext();
                var searchList = from customers in northWindModel.Customers
                                 select new SelectListItem
                                 {
                                     Text = customers.CustomerID,
                                     Value = customers.CustomerID
                                 };

                customersColumn.SearchList = searchList.ToList<SelectListItem>();
                customersColumn.SearchList.Insert(0, new SelectListItem { Text = "All", Value = "" });
            }
        }

        private void SetUpFreightSearchDropDown(JQGrid ordersGrid)
        {
            // setup the grid search criteria for the columns
            JQGridColumn freightColumn = ordersGrid.Columns.Find(c => c.DataField == "Freight");
            freightColumn.Searchable = true;

            // DataType must be set in order to use searching
            freightColumn.DataType = typeof(decimal);
            freightColumn.SearchToolBarOperation = SearchOperation.IsGreaterOrEqualTo;
            freightColumn.SearchType = SearchType.DropDown;

            // Populate the search dropdown only on initial request, in order to optimize performance
            if (ordersGrid.AjaxCallBackMode == AjaxCallBackMode.RequestData)
            {
                List<SelectListItem> searchList = new List<SelectListItem>();
                searchList.Add(new SelectListItem { Text = "> 10", Value = "10" });
                searchList.Add(new SelectListItem { Text = "> 30", Value = "30" });
                searchList.Add(new SelectListItem { Text = "> 50", Value = "50" });
                searchList.Add(new SelectListItem { Text = "> 100", Value = "100" });

                freightColumn.SearchList = searchList.ToList<SelectListItem>();
                freightColumn.SearchList.Insert(0, new SelectListItem { Text = "All", Value = "" });
            }
        }

        private void SetShipNameSearchDropDown(JQGrid ordersGrid)
        {
            JQGridColumn freightColumn = ordersGrid.Columns.Find(c => c.DataField == "ShipName");
            freightColumn.Searchable = true;
            freightColumn.DataType = typeof(string);
            freightColumn.SearchToolBarOperation = SearchOperation.Contains;
            freightColumn.SearchType = SearchType.TextBox;
        }

        private void SetUpCustomerIDEditDropDown(JQGrid ordersGrid)
        {
            // setup the grid search criteria for the columns
            JQGridColumn customersColumn = ordersGrid.Columns.Find(c => c.DataField == "CustomerID");
            customersColumn.Editable = true;
            customersColumn.EditType = EditType.DropDown;

            // Populate the search dropdown only on initial request, in order to optimize performance
            if (ordersGrid.AjaxCallBackMode == AjaxCallBackMode.RequestData)
            {
                var northWindModel = new NorthwindDataContext();
                var editList = from customers in northWindModel.Customers
                               select new SelectListItem
                               {
                                   Text = customers.CustomerID,
                                   Value = customers.CustomerID
                               };

                customersColumn.EditList = editList.ToList<SelectListItem>();
            }
        }        

    }
}