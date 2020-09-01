using BAL;
using PL.Controllers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ViewModel;

namespace PL.Areas.Innovation.Controllers
{
    public class GallaryController : BaseController
    {
        ICustomerGallaryRepository _gallaryRepository = new CustomerGallaryRepository();
        // GET: Innovation/Gallary
        public ActionResult Index()
        {
            int CustomerId = Convert.ToInt32(User.Identity.Name);
            IEnumerable<ViewModel_Customers_Gallery> _gallaryImage = _gallaryRepository.GetCampaignsGallaryImageList(CustomerId);
            return View(_gallaryImage);
        }
        //Add :Innovation/Gallary
        public ActionResult AddImage()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            int CustomerId = Convert.ToInt32(User.Identity.Name);
            List<ViewModel_Campaigns> _Campaigns = _gallaryRepository.GetCampaingnsList(CustomerId).ToList();
            SelectList list1 = new SelectList(_Campaigns, "id", "name");
            ViewBag.Campaingns = list1;
            return View();

        }
        [HttpPost]
        public ActionResult AddImage(ViewModel_Customers_Gallery model, IEnumerable<HttpPostedFileBase> ImagePath, string command)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            int gallaryid = 0;
            bool result = true;
            model.Gallary_Images = new ViewModel_Customer_Gallary_Images();
            if (ModelState.IsValid)
            {
                model.IsActive = 1;
                model.IsDeleted = 0;
                if (command.Equals("Publish"))
                {
                    model.IsPublish = true;
                }
                else
                {
                    model.IsPublish = false;
                }
                model.CreatedBy = Convert.ToInt32(User.Identity.Name);
                model.CustomerId = Convert.ToInt32(User.Identity.Name);
                if (command.Equals("Update"))
                {
                    model.IsPublish = false;
                    int res = _gallaryRepository.UpdateGallary(model);
                    gallaryid = model.Id;
                }
                else if (command.Equals("Update and publish"))
                {
                    model.IsPublish = true;
                    int res = _gallaryRepository.UpdateGallary(model);
                    gallaryid = model.Id;
                }
                else if(command.Equals("Save") || command.Equals("Publish"))
                {
                    if (model.Id == 0)
                    {
                        gallaryid = _gallaryRepository.AddGallary(model);
                    }
                    else
                    {
                        gallaryid = model.Id;
                        _gallaryRepository.UpdateGallary(model);
                    }
                }
                foreach (var item in ImagePath)
                {
                    Guid newid = Guid.NewGuid();
                    if (item != null)
                    {
                        string fileExtention = Path.GetExtension(item.FileName);
                        string _FileName = newid + fileExtention;
                        string _path = Path.Combine(Server.MapPath("~/Images/Gallary/"), _FileName);
                        item.SaveAs(_path);
                        model.Gallary_Images.ImagePath = "/Images/Gallary/" + _FileName;
                        model.Gallary_Images.CustomerGallaryId = gallaryid;
                        result = _gallaryRepository.AddGallaryImages(model);
                    }
                    else
                    {
                        model.Gallary_Images.ImagePath = null;
                    }
                }
                TempData["IsAdded"] = result == true ? "1" : "0";
                return RedirectToAction("Index");
            }
            return View();

        }

        [HttpGet]
        public JsonResult CampaginAlbum(int gallaryId)
        {
            IEnumerable<ViewModel_Customers_Campaigns_Album> _CampaignsImages = _gallaryRepository.GetCampaignsAlbumList(gallaryId);
            return Json(_CampaignsImages, JsonRequestBehavior.AllowGet);
        }

        public int DeleteCampaginAlbum(int gallaryId)
        {
            int CustomerId = Convert.ToInt32(User.Identity.Name);
            int res = _gallaryRepository.DeleteCampaginAlbum(gallaryId);
            return res;
        }
        public JsonResult EditGallary(int gallaryId)
        {
            int CustomerId = Convert.ToInt32(User.Identity.Name);
            List<ViewModel_Campaigns> _Campaigns = _gallaryRepository.GetCampaingnsList(CustomerId).ToList();
            SelectList list1 = new SelectList(_Campaigns, "id", "name");
            ViewBag.Campaingns = list1;
            IEnumerable<ViewModel_Customers_Gallery> _gallaryImage = _gallaryRepository.GetGallaryDetail(gallaryId);
            return Json(_gallaryImage, JsonRequestBehavior.AllowGet);
        }
        public JsonResult DeleteGallaryImage(int Id)
        {
            bool res = _gallaryRepository.DeleteImage(Id);
            return Json(res.ToString(), JsonRequestBehavior.AllowGet);
        }
    }
}