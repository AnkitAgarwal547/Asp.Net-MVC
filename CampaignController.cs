using BAL;
using PL.Controllers;
using PL.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Mvc;
using ViewModel;
using ViewModel.Innovation;

namespace PL.Areas.Innovation.Controllers
{
    public class CampaignController : BaseController
    {
        // GET: Innovation/Campaign
        ICustomerCampaignRepository _cust = new CustomerCampaignRepository();
        ICampaignIdeaSubmissionRepository obj = new CampaignIdeaSubmissionRepository();
        IManageTeamRepository manageTeamRepo = new ManageTeamRepository();
        CampaignIdeaSubmissionRepository obj_ideaSub = new CampaignIdeaSubmissionRepository();
        IProjectManagerRepository obj_proj = new ProjectManagerRepository();
        ITaskManageRepository obj_task = new TaskManageRepository();
        IAuditing objAuditing = new Auditing();
        Email _email = new Email();
        ICommonRepository obj_com = new CommonRepository();
        IEvaluatorsRepository obj_eval = new EvaluatorsRepository();
        SMS_Notification _sms = new SMS_Notification();
        

        public ActionResult Index()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            ViewModel_Campaign camp = new ViewModel_Campaign();
            int CustomerId = Convert.ToInt32(User.Identity.Name);
            camp.MyCampaigns = _cust.getCustomerCampaign(CustomerId);
            return View(camp);
        }

        [HttpPost]
        public ActionResult Index(ViewModel_Campaign camp)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            int CampaignId_Id = Convert.ToInt32(camp.Id);
            Session["Campaign_Id"] = null;
            return RedirectToAction("CampaignInfo");
        }

        public ActionResult CampaignInfo(string Id = null)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            ViewModel_Campaign model = new ViewModel_Campaign();
            if (Session["Campaign_Id"] != null && Id != null)
            {
                //int CampaignId_Id = Convert.ToInt32(Session["Campaign_Id"]);
                if (Id != "0")
                {
                    Session["Campaign_Id"] = Id;
                }
                int CampaignId_Id = Convert.ToInt32(Session["Campaign_Id"]);
                if (Convert.ToInt32(Id) != 0)
                {
                    CampaignId_Id = Convert.ToInt32(Id);
                }
                model = _cust.getCustomerCampaigninfo(CampaignId_Id);
                IsAuditAllowed(Convert.ToInt32(Session["Campaign_Id"]));
            }
            else
            {
                if (Id != null)
                {
                    int CampaignId_Id = Convert.ToInt32(Id);
                    Session["Campaign_Id"] = Id;
                    model = _cust.getCustomerCampaigninfo(CampaignId_Id);
                    IsAuditAllowed(Convert.ToInt32(Session["Campaign_Id"]));
                }
            }
            model.MyTeams = manageTeamRepo.GetTeamsListForDropdown(Convert.ToInt32(User.Identity.Name));
           
            return View(model);
        }

        [HttpPost]
        public ActionResult CampaignInfo(ViewModel_Campaign model, HttpPostedFileBase PrimaryImage, HttpPostedFileBase[] AdditionalImages, HttpPostedFileBase Attechment)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            model.ModifiedBy = Convert.ToInt32(User.Identity.Name);
            int userId = Convert.ToInt32(User.Identity.Name);
            model.CustomerId = userId;
            bool isnewcamp = _cust.CheckCampaginname(model.Name, userId, 0);
            if(model.Id==0 && isnewcamp==true)
            {
                model.Id = Convert.ToInt32(Session["Campaign_Id"]);
            }


            bool ret = _cust.CheckCampaginname(model.Name, userId, model.Id);
            if (ret == false)
            {

                if (Attechment != null)
                {
                    Guid newid = Guid.NewGuid();
                    string fileExtention = Path.GetExtension(Attechment.FileName);
                    string _FileName = newid + fileExtention;
                    string _path = Path.Combine(Server.MapPath("~/Images/CustomerLogo/"), _FileName);
                    Attechment.SaveAs(_path);
                    model.Attechment = "/Images/CustomerLogo/" + _FileName;

                }
                int CampaignId = _cust.SaveCapmaignInfo(model);



                if (CampaignId != 0)
                {
                    Session["Campaign_Id"] = CampaignId;
                    if (PrimaryImage != null)
                    {
                        _cust.DeletePrimaryImage(CampaignId);
                        Guid newid = Guid.NewGuid();
                        string fileExtention = Path.GetExtension(PrimaryImage.FileName);
                        string _FileName = newid + fileExtention;
                        string _path = Path.Combine(Server.MapPath("~/Images/CustomerLogo/"), _FileName);
                        PrimaryImage.SaveAs(_path);
                        ViewModel_CustomerCampaignImages image = new ViewModel_CustomerCampaignImages();
                        image.CampaignId = CampaignId;
                        image.CreatedBy = Convert.ToInt32(User.Identity.Name);
                        image.ImagePath = "/Images/CustomerLogo/" + _FileName;
                        image.IsPrimary = 1;
                        _cust.SaveCampaignImages(image);
                    }
                    foreach (var _image in AdditionalImages)
                    {
                        if (_image != null)
                        {
                            Guid neid = Guid.NewGuid();
                            string newid = neid.ToString();
                            string fileExtention = Path.GetExtension(_image.FileName);
                            string _FileName = newid + fileExtention;
                            string _path = Path.Combine(Server.MapPath("~/Images/CustomerLogo/"), _FileName);
                            _image.SaveAs(_path);
                            ViewModel_CustomerCampaignImages image = new ViewModel_CustomerCampaignImages();
                            image.CampaignId = CampaignId;
                            image.CreatedBy = Convert.ToInt32(User.Identity.Name);
                            image.ImagePath = "/Images/CustomerLogo/" + _FileName;
                            image.IsPrimary = 0;
                            _cust.SaveCampaignImages(image);
                        }
                    }
                    _cust.RemoveExistingUser(CampaignId);

                    if (model.TargetUsers != "")
                    {

                        string[] TU = model.TargetUsers.Split(',');
                        foreach (string item in TU)
                        {
                            ViewModel_Customers_Campaigns_TargetUser _tu = new ViewModel_Customers_Campaigns_TargetUser();
                            _tu.CampaignId = CampaignId;
                            _tu.CreateBy = Convert.ToInt32(User.Identity.Name);
                            _tu.userid = Convert.ToInt32(item);
                            _cust.SaveCampaignTargetUsers(_tu);
                        }

                        TempData["Campiagn1"] = "Data Saved Successfully.";
                    }


                    

                    

                    return RedirectToAction("CampaignIdeaSubmission");
                }
                model.MyTeams = manageTeamRepo.GetTeamsList(userId);
                return View(model);
            }
            else
            {
                TempData["Campiagnerr"] = "Campaign already exists.";
                return RedirectToAction("CampaignInfo");
            }

        }

        public JsonResult CloseCampaign(int campaignId)
        {
            return Json(_cust.CloseCampaign(campaignId));
        }

        public JsonResult DropCampaign(int campaignId)
        {
            return Json(_cust.DropCampaign(campaignId));
        }

        public ActionResult CampaignIdeaSubmission()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            if (TempData["Campiagn1"] != null)
            {
                ViewBag.Campiagn1 = TempData["Campiagn1"].ToString();
               
            }
            BindDropdown();
            ViewModel_CampaignIdeaSubmission _dates = obj.GetInnovation(Convert.ToInt32(Session["Campaign_Id"].ToString()));
            IsAuditAllowed(Convert.ToInt32(Session["Campaign_Id"]));
            ViewBag.StartDate = _dates.CampStartDate.AddDays(1);
            ViewBag.EndDate = _dates.CampEndDate;
            return View();
        }

        public ActionResult EvaluationStage()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            ViewModel_CampaignIdeaSubmission _dates = obj.GetInnovation(Convert.ToInt32(Session["Campaign_Id"].ToString()));
            //ViewBag.StartDate = _dates.CampStartDate;
            //ViewBag.EndDate = _dates.CampEndDate;

            ViewModel_Campaign data = _cust.GetSocialStageEndDate(Convert.ToInt32(Session["Campaign_Id"].ToString()));

            IsAuditAllowed(Convert.ToInt32(Session["Campaign_Id"].ToString()));

            ViewBag.StartDate = ((DateTime)data.StartDate).AddDays(1);
            ViewBag.EndDate = data.EndDate;

            ViewBag.EvaluationTeam = _dates.IsEvaluationTeamCreated;
            return View();
        }

        [HttpPost]
        public JsonResult SubmitEvaluation(ViewModel_EvaluationStage model)
        {
            int CampaignId = Convert.ToInt32(Session["Campaign_Id"]);
            int UserId = Convert.ToInt32(User.Identity.Name);
            return Json(obj.AddEvaluation(model, CampaignId, UserId));
        }

        //[HttpPost]
        //public ActionResult CampaignIdeaSubmission(ViewModel_CampaignIdeaSubmission model, IEnumerable<HttpPostedFileBase> files)
        //{
        //    //if (file.ContentLength > 0)
        //    //{
        //    //    var fileName = Path.GetFileName(file.FileName);
        //    //    var path = Path.Combine(Server.MapPath("~/IdeaSubmissionDocument"), fileName);
        //    //    file.SaveAs(path);
        //    //}
        //    List<ViewModel_CampaignSubmissionFiles> uploads = new List<ViewModel_CampaignSubmissionFiles>();
        //    foreach (var file in files)
        //    {
        //        if (file.ContentLength > 0)
        //        {
        //            var fileName = Guid.NewGuid().ToString();
        //            FileInfo fi = new FileInfo(file.FileName);
        //            var extension = fi.Extension;
        //            var path = Server.MapPath("~/IdeaSubmissionDocument/" + fileName + "." + extension);
        //            file.SaveAs(path);
        //            ViewModel_CampaignSubmissionFiles data = new ViewModel_CampaignSubmissionFiles();
        //            data.CampaignId = 1;
        //            data.FilePath = path;
        //            uploads.Add(data);
        //        }
        //    }
        //    model.uploadfiles = uploads;
        //    obj.AddCampaignIdea(model);
        //    BindDropdown();
        //    return View();
        //}

        public void BindDropdown()
        {
            List<ViewModel_Category> catelist = new List<ViewModel_Category>();

            for (int i = 0; i < 6; i++)
            {
                ViewModel_Category cate = new ViewModel_Category();
                cate.id = i + 1;
                cate.name = "Category " + (i + 1);
                catelist.Add(cate);
            }

            List<ViewModel_Subcategory> subcatelist = new List<ViewModel_Subcategory>();

            for (int i = 0; i < 6; i++)
            {
                ViewModel_Subcategory subcate = new ViewModel_Subcategory();
                subcate.id = i + 1;
                subcate.name = "Subcategory " + (i + 1);
                subcatelist.Add(subcate);
            }

            List<ViewModel_TimePeriod> timeperiodlist = new List<ViewModel_TimePeriod>();

            for (int i = 0; i < 6; i++)
            {
                ViewModel_TimePeriod timeperiod = new ViewModel_TimePeriod();
                timeperiod.id = i + 1;
                timeperiod.time = (i + 1) + "Day(s) ";
                timeperiodlist.Add(timeperiod);
            }

            SelectList list1 = new SelectList(catelist, "id", "name");
            SelectList list2 = new SelectList(subcatelist, "id", "name");
            SelectList list3 = new SelectList(timeperiodlist, "id", "time");

            ViewBag.Category = list1;
            ViewBag.SubCategory = list2;
            ViewBag.TimePeriod = list3;
        }

        public void TimePeriodDropdown()
        {
            List<ViewModel_TimePeriod> list = new List<ViewModel_TimePeriod>();
            for (int i = 1; i < 6; i++)
            {
                ViewModel_TimePeriod data = new ViewModel_TimePeriod();
                data.id = i;
                data.time = "Days -" + i;
                list.Add(data);
            }

            SelectList li = new SelectList(list, "id", "time");
            ViewBag.TimePeriod = li;
        }

        [HttpPost]
        public JsonResult SaveForm(List<ViewModel_DynamicForm> models)
        {
            int innovationid = Convert.ToInt32(User.Identity.Name);
            return Json(obj.AddFrom(models, innovationid));
        }

        [HttpPost]
        public JsonResult SaveAuditForm(List<ViewModel_CampaignsAuditDynamicFormFields> models)
        {
            int innovationid = Convert.ToInt32(User.Identity.Name);
            return Json(obj.AddAuditFrom(models, innovationid));
        }

        [HttpPost]
        public JsonResult GetForms()
        {
            int innovation = Convert.ToInt32(User.Identity.Name);
            return Json(obj.GetForms(innovation));
        }

        [HttpPost]
        public JsonResult GetFormsFields(int id)
        {
            return Json(obj.GetFormsFields(id));
        }

        [HttpPost]
        public JsonResult SubmitForm(ViewModel_CampaignIdeaSubmission model)
        {
            int CampaignId = Convert.ToInt32(Session["Campaign_Id"]);
            int UserId = Convert.ToInt32(User.Identity.Name);
            return Json(obj.AddCampaignIdea(model, CampaignId, UserId));
        }







        public ActionResult SocialStage()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            ViewModel_Campaign data = _cust.GetIdeaEndDate(Convert.ToInt32(Session["Campaign_Id"].ToString()));
            IsAuditAllowed(Convert.ToInt32(Session["Campaign_Id"].ToString()));
            ViewBag.StartDate = ((DateTime)data.StartDate).AddDays(1);
            ViewBag.EndDate = data.EndDate;
            return View();
        }

        [HttpPost]
        public ActionResult CampaignDeleteImage(string Id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            string result = "Error";
            try
            {

                int ImageId = Convert.ToInt32(Id);
                _cust.deleteCampaignInage(1, ImageId);
                result = "Success";
            }
            catch { }
            return Json(new { Message = result, JsonRequestBehavior.AllowGet });
        }

        [HttpPost]
        public ActionResult CampaignDeleteAttachment(string Id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            string result = "Error";
            try
            {

                int campaginId = Convert.ToInt32(Id);
                _cust.DeleteCampaignAttachment(campaginId);
                result = "Success";
            }
            catch { }
            return Json(new { Message = result, JsonRequestBehavior.AllowGet });
        }

        [HttpPost]
        public ActionResult SocialStage(List<ViewModel_Customers_Campaigns_SocialStep> model)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            string result = "Error";
            try
            {
                int CampaignId = Convert.ToInt32(Session["Campaign_Id"]);
                int UserId = Convert.ToInt32(User.Identity.Name);
                _cust.SaveSocialStage(model, CampaignId, UserId);
                result = "Success";
            }
            catch { }
            return Json(new { Message = result, JsonRequestBehavior.AllowGet });
        }

        public ActionResult ImplementationStage()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            ViewModel_Campaign data = _cust.GetEvaluationEndDate(Convert.ToInt32(Session["Campaign_Id"].ToString()));

            IsAuditAllowed(Convert.ToInt32(Session["Campaign_Id"].ToString()));

            ViewBag.StartDate = ((DateTime)data.StartDate).AddDays(1);
            ViewBag.EndDate = data.EndDate;
            return View();
        }

        [HttpPost]
        public JsonResult CheckCampaginName(string campname, int campId)
        {
            int UserId = Convert.ToInt32(User.Identity.Name);          

            bool ret = _cust.CheckCampaginname(campname, UserId, campId);
            return Json(ret);
        }


        [HttpPost]
        public ActionResult ImplementationStage(List<ViewModel_Customers_Campaigns_Implementation> model)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            string result = "Error";
            try
            {
                int CampaignId = Convert.ToInt32(Session["Campaign_Id"]);
                int UserId = Convert.ToInt32(User.Identity.Name);
                _cust.SaveImplementationStage(model, CampaignId, UserId);
                result = "Success";
            }
            catch { }
            return Json(new { Message = result, JsonRequestBehavior.AllowGet });
        }

        public ActionResult RewardStage()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            ViewModel_Customers_Campaigns_Implementation data = _cust.EditImplementation(Convert.ToInt32(Session["Campaign_Id"].ToString()));

            IsAuditAllowed(Convert.ToInt32(Session["Campaign_Id"].ToString()));

            ViewBag.IsAllow = (data == null ? 0 : data.AllowIdeas);
            return View();
        }

        [HttpPost]
        public ActionResult RewardStage(ViewModel_Customers_Campaigns_Rewards model, string submit)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            int CampaignId = Convert.ToInt32(Session["Campaign_Id"]);
            int UserId = Convert.ToInt32(User.Identity.Name);

            switch (submit)
            {
                case "Save":
                    model.CampaignId = CampaignId;
                    model.CreatedBy = UserId;

                    _cust.SaveRewardStage(model);
                    TempData["CampaignSaved"] = "Campaign Saved Successfully";
                    break;
                case "Publish":
                    model.CampaignId = CampaignId;
                    model.CreatedBy = UserId;
                    _cust.SaveRewardStage(model);
                    _cust.PublishCampaign(CampaignId);
                    TempData["CampaignSaved"] = "Campaign Published Successfully";
                    break;
            }

            if (submit == "Save")
            {

                return RedirectToAction("Success");
            }
            else
            {
                bool allow = _cust.IsEmailNotificationAllowed(CampaignId);
                bool allowsms = _cust.IsSMSNotificationAllowed(CampaignId);
                if (allow)
                {
                    string to_targetuser = obj_com.emails_targetuser(CampaignId);
                    string to_campaignmanager = obj_com.emails_campaignmanager(CampaignId);
                    List<string> content_targetuser = _cust.CampaignSummary_TargetUser(CampaignId);
                    List<string> content_campaignmanager = _cust.CampaignSummary_CampaignManager(CampaignId);

                    _email.SendEmail(to_targetuser, content_targetuser[0], content_targetuser[1]);
                    _email.SendEmail(to_campaignmanager, content_campaignmanager[0], content_campaignmanager[1]);
                }

                if(allowsms==true)
                {
                    //SMS
                    string to_campaignmanager_sms = obj_com.ContactNo_CampaignManager(CampaignId);
                    List<string> to_targetusers_sms = obj_com.ContactNo_TargetUsers(CampaignId);

                    string campaignmanager_smstext = obj_com.SMSText_CampaignManager_OnCampaignCreation(CampaignId);
                    string targetusers_smstext = obj_com.SMSText_TargetUser_OnCampaignCreation(CampaignId);

                    List<SMS_Info> contactnos = new List<SMS_Info>();
                    foreach (var item in to_targetusers_sms)
                    {
                        SMS_Info obj = new SMS_Info();
                        obj.contactno = item;
                        obj.message = targetusers_smstext;
                        contactnos.Add(obj);
                    }

                    SMS_Info obj1 = new SMS_Info();
                    obj1.contactno = to_campaignmanager_sms;
                    obj1.message = campaignmanager_smstext;
                    contactnos.Add(obj1);

                    _sms.SendSMSNotification(contactnos, UserId);
                }


                

                return RedirectToAction("Published");
            }

        }

        public ActionResult Success()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            return View();
        }

        public ActionResult Published()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            return View();
        }

        [HttpPost]
        public ActionResult UploadFiles()
        {
            //string _file_name_to_save = string.Empty;
            ArrayList _file_name_to_save = new ArrayList();
            string path = "/IdeaSubmissionDocument/";
            HttpFileCollectionBase files = Request.Files;
            for (int i = 0; i < files.Count; i++)
            {
                HttpPostedFileBase file = files[i];
                var fileName = Guid.NewGuid().ToString();
                FileInfo fi = new FileInfo(file.FileName);
                var extension = fi.Extension;
                _file_name_to_save.Add(path + fileName + extension);
                file.SaveAs(Server.MapPath("~/IdeaSubmissionDocument/" + fileName + extension));
            }
            return Json(_file_name_to_save);
        }

        [HttpPost]
        public JsonResult GetIdea()
        {
            return Json(obj.GetCampaignIdea(Convert.ToInt32(Session["Campaign_Id"])));
        }

        [HttpPost]
        public JsonResult RemoveFile(int id)
        {
            return Json(obj.RemoveFile(id));
        }

        [HttpPost]
        public JsonResult GetEvaluation()
        {
            int user = Convert.ToInt32(Session["Campaign_Id"]);
            return Json(obj.GetEvaluation(user));
        }


        [HttpPost]
        public JsonResult GetAuditDetail()
        {
            return Json(obj.GetAuditDetail(Convert.ToInt32(Session["Campaign_Id"])));
        }

        public ActionResult Audit()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            ViewModel_Campaign data = _cust.GetImplementationEndDate(Convert.ToInt32(Session["Campaign_Id"].ToString()));

            IsAuditAllowed(Convert.ToInt32(Session["Campaign_Id"].ToString()));

            ViewBag.StartDate = ((DateTime)data.StartDate).AddDays(1);
            ViewBag.EndDate = data.EndDate;
            SelectList list = new SelectList(_cust.GetAuditTypes(Convert.ToInt32(User.Identity.Name)),"id","typename");
            ViewBag.Type = list;

            return View();
        }

        [HttpPost]
        public JsonResult AddAudit(ViewModel_Campaign_Audit model)
        {
            int CampaignId = Convert.ToInt32(Session["Campaign_Id"]);
            int UserId = Convert.ToInt32(User.Identity.Name);
            ViewBag.IsAllow = 1;
            return Json(obj.AddAudit(model, CampaignId, UserId));
        }

        [HttpPost]
        public JsonResult CampaignSkipStep(int step)
        {
            int CampaignId = Convert.ToInt32(Session["Campaign_Id"]);
            return Json(_cust.CampaignSkipStep(CampaignId, step));
        }

        [HttpPost]
        public JsonResult GetCampaignSteps()
        {
            int CampaignId = Convert.ToInt32(Session["Campaign_Id"]);
            return Json(_cust.GetCampaignSteps(CampaignId));
        }

        [HttpPost]
        public JsonResult GetCampaignStepsAtFirst(int campid)
        {
            return Json(_cust.GetCampaignSteps(campid));
        }

        [HttpPost]
        public JsonResult IsPublished()
        {
            int CampaignId = Convert.ToInt32(Session["Campaign_Id"]);
            return Json(_cust.IsPublished(CampaignId));
        }

        [HttpPost]
        public JsonResult EditSocialStage()
        {
            return Json(_cust.EditSocialStage(Convert.ToInt32(Session["Campaign_Id"])));
        }

        [HttpPost]
        public JsonResult EditImplementation()
        {
            return Json(_cust.EditImplementation(Convert.ToInt32(Session["Campaign_Id"])));
        }

        [HttpPost]
        public JsonResult EditReward()
        {
            return Json(_cust.EditReward(Convert.ToInt32(Session["Campaign_Id"])));
        }

        public ActionResult Auditor(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            ViewModel_Campaign model = new ViewModel_Campaign();
            model = obj_ideaSub.AllCommentsforIdeaWithEvaluatonReviewForCampaignManager(id);
            model.idea = id;
            Session["Campaign_Id"] = model.Id;
            return View(model);
        }

        public ActionResult StartAuditing(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            ViewModel_CampaginAudit _audit = new ViewModel_CampaginAudit();
            _audit = objAuditing.GetTaskDetail(id,Convert.ToInt32(User.Identity.Name));
            _audit.IdeaID = id;
            _audit.campaginId = Convert.ToInt32(Session["Campaign_Id"]);
            return View(_audit);
        }

        [HttpPost]
        public JsonResult GetAuditFormsFields(int id)
        {
            return Json(objAuditing.GetFormsFields(id));
        }

        [HttpPost]
        public ActionResult AuditorTaskComment(List<string> txtArea, List<string> taskId, ViewModel_CampaginAudit model)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            int userid = Convert.ToInt32(User.Identity.Name);
            for (int i = 0; i < txtArea.Count; i++)
            {
                ViewModel_AuditorTaskComment _data = new ViewModel_AuditorTaskComment();
                _data.AutherID = userid;
                _data.Comment = txtArea[i];
                _data.TaskID = Convert.ToInt16(taskId[i]);
                _data.IdeaID = model.IdeaID;
                int res = objAuditing.AddAuditorTaskComment(_data);
            }
            TempData["IsdataSave"] = "1";
            return RedirectToAction("StartAuditing", "User", new { id = model.IdeaID });
        }

        public ActionResult ArchivedCampaigns()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            ViewModel_Campaign camp = new ViewModel_Campaign();
            int CustomerId = Convert.ToInt32(User.Identity.Name);
            camp.MyCampaigns = _cust.getArchivedCustomerCampaign(CustomerId);
            return View(camp);
        }

        public ActionResult CampaignDetails(int Id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");


            if (Id != 0)
            {
                int CampaignId_Id = Convert.ToInt32(Id);
                Session["Campaign_Id"] = Id;
            }

            int userid = Convert.ToInt32(User.Identity.Name);
            int campid = Convert.ToInt32(Session["Campaign_Id"].ToString());
            return View(_cust.CampaignDetails(userid, campid));
        }

        public ActionResult EvaluationDetails(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            ViewModel_Campaign model = new ViewModel_Campaign();
            model = obj_ideaSub.AllCommentsforIdeaWithEvaluatonReviewForCampaignDetails(id);
            return View(model);
        }
        public ActionResult ProjectManagerDetails(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            ViewModel_ProjectManager model = new ViewModel_ProjectManager();

            model = obj_proj.GetProjectManager(id);
            if (model != null)
            {
                model.idea = id;

            }
            return View(model);
        }
        public ActionResult TaskCreation(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");


            ViewModel_TaskCreation model = new ViewModel_TaskCreation();
            model.idea = id;
            return View(model);
        }
        [HttpPost]
        public JsonResult GetAllTaskForInnovationMaanger(int idea)
        {
            return Json(obj_task.GetAllTaskForInnovationMaanger(idea));
        }


        #region Campain specific evaluators
        [HttpPost]
        public JsonResult UpdateEvaluators(ViewModel_Evaluators model)
        {
            int modify_by = Convert.ToInt32(User.Identity.Name);
            model.modify_by = modify_by;

            bool result = obj_eval.UpdateCampaignSpecificEvaluators(model);

            if (result == true)
            {
                // EmailToEvaluators(model);
            }
            return Json(result);
        }

        [HttpPost]
        public JsonResult IsRegularEvaluatorsExists(List<ViewModel_Evaluators> model)
        {
            int entry_by = Convert.ToInt32(User.Identity.Name);
            int campid = Convert.ToInt32(Session["Campaign_Id"].ToString());
            return Json(obj.IsCampaignSpecificRegularEvaluatorsExists(campid, entry_by, model));
        }

        [HttpPost]
        public JsonResult IsChiefEvaluatorsExists(int chiefeval)
        {
            int entry_by = Convert.ToInt32(User.Identity.Name);
            int campid = Convert.ToInt32(Session["Campaign_Id"].ToString());
            return Json(obj.IsCampaignSpecificChiefEvaluatorsExists(campid, entry_by, chiefeval));
        }


        [HttpPost]
        public JsonResult IsRegularEvaluatorsExistsForUpdate(List<ViewModel_Evaluators> model)
        {
            int entry_by = Convert.ToInt32(User.Identity.Name);
            int campid = Convert.ToInt32(Session["Campaign_Id"].ToString());
            return Json(obj.IsCampaignSpecificRegularEvaluatorsExistsForUpdate(campid, entry_by, model));
        }

        [HttpPost]
        public JsonResult IsChiefEvaluatorsExistsForUpdate(int chiefeval,int teamid)
        {
            int entry_by = Convert.ToInt32(User.Identity.Name);
            int campid = Convert.ToInt32(Session["Campaign_Id"].ToString());
            return Json(obj.IsCampaignSpecificChiefEvaluatorsExistsForUpdate(campid, entry_by, chiefeval,teamid));
        }

        [HttpPost]
        public JsonResult AddCampaignSpecificEvaluators(ViewModel_Evaluators model)
        {
            int entry_by = Convert.ToInt32(User.Identity.Name);
            model.entry_by = entry_by;
            int campid = Convert.ToInt32(Session["Campaign_Id"].ToString());
            model.camp_id = campid;

            bool result = obj.AddCampaignSpecificEvaluators(model);

            if (result == true)
            {
                //EmailToEvaluators(model);
            }

            return Json(result);
        }


        [HttpPost]
        public JsonResult GetEvaluators()
        {
            int entry_by = Convert.ToInt32(User.Identity.Name);
            int cmapid = Convert.ToInt32(Session["Campaign_Id"].ToString());
            return Json(obj.GetCampaignSpecificEvaluators(entry_by, cmapid));

        }

        [HttpPost]
        public JsonResult GetRegularUsers()
        {
            int cmapid = Convert.ToInt32(Session["Campaign_Id"].ToString());
            int entry_by = Convert.ToInt32(User.Identity.Name);
            return Json(manageTeamRepo.GetTeamsListRegularEvaluation(cmapid, entry_by));
        }

        [HttpPost]
        public JsonResult GetChiefUsers()
        {
            int cmapid = Convert.ToInt32(Session["Campaign_Id"].ToString());
            int entry_by = Convert.ToInt32(User.Identity.Name);
            return Json(manageTeamRepo.GetTeamsListChiefEvaluation(cmapid,entry_by));
        }

        [HttpPost]
        public JsonResult DeleteRegularEvaluator(int id)
        {
            int entry_by = Convert.ToInt32(User.Identity.Name);
            return Json(obj_eval.DeleteCampaignSpecificEvaluators(id, entry_by));
        }

        [HttpPost]
        public JsonResult IsTeamExists(string teamname)
        {
            int cmapid = Convert.ToInt32(Session["Campaign_Id"].ToString());
            int entry_by = Convert.ToInt32(User.Identity.Name);
            return Json(obj_eval.IsTeamExists(teamname,entry_by, cmapid));
        }

        [HttpPost]
        public JsonResult IsTeamExistsForUpdate(string teamname,int id)
        {
            int cmapid = Convert.ToInt32(Session["Campaign_Id"].ToString());
            int entry_by = Convert.ToInt32(User.Identity.Name);
            return Json(obj_eval.IsTeamExistsForUpdate(teamname, entry_by, cmapid,id));
        }
        
        #endregion

        public void IsAuditAllowed(int campid)
        {
            bool allow = obj_com.IsAuditAllowed(campid);
            ViewBag.Isalloewd = allow;
        }

        public ActionResult TaskCreationDetails(int userid,int idea,int campid)
        {
            int campaignid = 0;
            if (campid > 0)
            {
                campaignid = campid;
            }
            else
            {
                //campaignid = Convert.ToInt32(Session["Campaign_Id"].ToString());
            }
            List<ViewModel_TaskCreation> model=  obj_task.GetAllTask(userid, idea, campaignid);
            return View(model);
        }

        public ActionResult StartAuditingDetails(int userid, int idea)
        {
            string isAudit = string.Empty;
            if (string.IsNullOrEmpty(isAudit.ToString()))
            {
                ViewModel_CampaginAudit _audit = new ViewModel_CampaginAudit();
                _audit = objAuditing.GetTaskDetail(idea, userid);
                _audit.IdeaID = idea;
                //_audit.campaginId = (int)Session["Campaign_Id"];

                if (_audit.AllTask.Count > 0)
                {
                    return View(_audit);
                }
                else
                {
                    return RedirectToAction("NoAudit");
                }
            }
            else
            {
                int campid = Convert.ToInt16(Session["Campaign_Id"]);
                return RedirectToAction("StartAuditingDetails", "User", new { id = campid });
            }
        }
    }
}

