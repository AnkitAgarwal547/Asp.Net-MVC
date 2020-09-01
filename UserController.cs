using BAL;
using PL.Controllers;
using PL.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Security;
using ViewModel;
using ViewModel.Innovation;

namespace PL.Areas.User.Controllers
{
    public class UserController : BaseController
    {

        ICampaignListRepository _obj_camp = new CampaignListRepository();
        ICampaignIdeaSubmissionRepository obj_ideaSub = new CampaignIdeaSubmissionRepository();
        ICategoryRepository obj_cate = new CategoryRepository();
        ISubCategoryRepository obj_subcate = new SubCategoryRepository();
        IManageTeamRepository obj_team = new ManageTeamRepository();
        IProjectManagerRepository obj_proj = new ProjectManagerRepository();
        ITaskManageRepository obj_task = new TaskManageRepository();
        ITaskConvoRepository obj_convo = new TaskConvoRepository();
        IAuditing objAuditing = new Auditing();
        IUserReward objReawrd = new UserReward();
        IMemberPoints objMemberPoint = new MemberPoints();
        ICustomerCampaignRepository _cust = new CustomerCampaignRepository();
        Email _email = new Email();
        ICommonRepository obj_com = new CommonRepository();
        SMS_Notification _sms = new SMS_Notification();


        // GET: User/User
        public ActionResult Index()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            List<ViewModel_Campaign> list = _obj_camp.Campaigns(Convert.ToInt32(Session["LoogedUser"]));
            return View(list);
        }

        public ActionResult IdeaSubmission(ViewModel_Campaign model)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            BindDropdown();
            //Session["Campaign_Id"] = model.Id;
            ViewModel_CampaignIdeaSubmission model1 = obj_ideaSub.GetCampaignIdea(Convert.ToInt32(Session["Campaign_Id"]));
            return View(model1);
        }

        public void BindDropdown()
        {
            int targetuser = Convert.ToInt32(User.Identity.Name);

            List<ViewModel_Category> catelist = new List<ViewModel_Category>();
            catelist = obj_cate.GetCategoriesForDropdownForTargetUser(targetuser);



            List<ViewModel_Subcategory> subcatelist = new List<ViewModel_Subcategory>();
            subcatelist = obj_subcate.GetSubCategoriesForDropdown(0);


            SelectList list1 = new SelectList(catelist, "id", "name");
            SelectList list2 = new SelectList(subcatelist, "id", "name");


            ViewBag.Category = list1;
            ViewBag.SubCategory = list2;

        }


        [HttpPost]
        public JsonResult IdeaSubmissionByUser(ViewModel_CampaignIdeaSubmission model)
        {
            int CampaignId = (int)Session["Campaign_Id"];
            int UserId = Convert.ToInt32(User.Identity.Name);
            bool result = obj_ideaSub.IdeaSubmission(model, CampaignId, UserId);

            bool allow = _cust.IsEmailNotificationAllowed(CampaignId);
            bool allowsms = _cust.IsSMSNotificationAllowed(CampaignId);
            if (result == true)
            {
                int point = objReawrd.InsertPointAfterImplementation(UserId,0,0, CampaignId);
                if (allow == true)
                {
                    //Email Address
                    string to_targetuser = obj_com.emailforuser(UserId);
                    string to_campaignmanager = obj_com.emails_campaignmanager(CampaignId);
                    string to_inovationmanager = obj_com.InnovationManagerEmail(CampaignId, 0);
                    string to_regularevaluator = obj_com.emails_regularevaluator((int)model.CategoryId, (int)model.SubCategoryId);
                    string to_chiefevaluator = obj_com.emails_chiefevaluator((int)model.CategoryId, (int)model.SubCategoryId);

                    //Email Contents
                    List<string> content_ideasubmission = _cust.CampaignSummary_IdeaSubmission(CampaignId, UserId);

                    string email = to_targetuser + "," + to_campaignmanager + "," + to_inovationmanager + "," + to_regularevaluator + "," + to_chiefevaluator;

                    _email.SendEmail(email, content_ideasubmission[0], content_ideasubmission[1]);
                }
                if (allowsms == true)
                {
                    //Contact No
                    string to_targetuser_sms = obj_com.ContactNo_LoggedUser(UserId);
                    string to_regularevaluator_sms = obj_com.ContactNo_FirstRegularEvaluator((int)model.CategoryId, (int)model.SubCategoryId);
                    string to_campaignmanager_sms = obj_com.ContactNo_CampaignManager(CampaignId);
                    string to_inovationmanager_sms = obj_com.ContactNo_InnovationManager(CampaignId);

                    string targetuser_smstext = obj_com.SMSText_TargetUser_OnIdeaSubmission(UserId);
                    string regularevaluator_smstext = obj_com.SMSText_RegularEvaluator_OnIdeaSubmission((int)model.CategoryId, (int)model.SubCategoryId);
                    string campaignmanager_smstext = obj_com.SMSText_CampaignManager_OnIdeaSubmission(CampaignId);

                    List<SMS_Info> contactnos = new List<SMS_Info>();
                    SMS_Info con1 = new SMS_Info();
                    con1.contactno = to_targetuser_sms;
                    con1.message = targetuser_smstext;
                    contactnos.Add(con1);

                    SMS_Info con2 = new SMS_Info();
                    con2.contactno = to_regularevaluator_sms;
                    con2.message = regularevaluator_smstext;
                    contactnos.Add(con2);

                    SMS_Info con3 = new SMS_Info();
                    con3.contactno = to_campaignmanager_sms;
                    con3.message = campaignmanager_smstext;
                    contactnos.Add(con3);

                    SMS_Info con4 = new SMS_Info();
                    con4.contactno = to_inovationmanager_sms;
                    con4.message = campaignmanager_smstext;
                    contactnos.Add(con4);

                    int innovationmanager = obj_team.GetInnovationManagerByCampaignId(CampaignId);

                    _sms.SendSMSNotification(contactnos, innovationmanager);
                }
            }
            return Json(result);
        }

        [HttpPost]
        public ActionResult UploadFiles()
        {

            ArrayList _file_name_to_save = new ArrayList();
            try
            {
                //string _file_name_to_save = string.Empty;

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
            }
            catch (Exception ex)
            {
                _file_name_to_save.Add(ex.ToString());
            }
            return Json(_file_name_to_save, JsonRequestBehavior.AllowGet);




            //// Checking no of files injected in Request object  
            //if (Request.Files.Count > 0)
            //{
            //    try
            //    {
            //        //  Get all files from Request object  
            //        HttpFileCollectionBase files = Request.Files;
            //        string filepath = string.Empty;
            //        for (int i = 0; i < files.Count; i++)
            //        {
            //            //string path = AppDomain.CurrentDomain.BaseDirectory + "Uploads/";  
            //            //string filename = Path.GetFileName(Request.Files[i].FileName);  

            //            HttpPostedFileBase file = files[i];
            //            string dateTime = DateTime.Now.ToString("_yyyy_MM_dd_HH_mm_ss").ToString();
            //            string fileName = Path.GetFileNameWithoutExtension(files[i].FileName);
            //            fileName = fileName.Replace(" ", "");
            //            string fileExtention = Path.GetExtension(files[i].FileName);
            //            //string _FileName = Path.GetFileNameWithoutExtension(item.FileName + DateTime.Now.ToString("_yyyy_MM_dd_HH_mm_ss")) + Path.GetExtension(item.FileName);
            //            string _FileName = fileName + dateTime + fileExtention;
            //            string _path = Path.Combine(Server.MapPath("~/IdeaSubmissionDocument/"), _FileName);
            //            file.SaveAs(_path);
            //            filepath = "/IdeaSubmissionDocument/" + _FileName; 
            //        }
            //        // Returns message that successfully uploaded  
            //        return Json(filepath);
            //    }
            //    catch (Exception ex)
            //    {
            //        return Json("Error occurred. Error details: " + ex.Message);
            //    }
            //}
            //else
            //{
            //    return Json("No files selected.");
            //}
        }


        public ActionResult CampaignDetails(int? id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");


            if (id != null)
            {
                Session["Campaign_Id"] = id;
            }

            ViewModel_Campaign model = new ViewModel_Campaign();
            model = obj_ideaSub.CampaignDetails(Convert.ToInt32(Session["Campaign_Id"]));
            return View(model);
        }

        [HttpPost]
        public JsonResult Like(int id)
        {
            ViewModel_LikeOrDislike result = obj_ideaSub.Like(id, Convert.ToInt32(Session["LoogedUser"]));
            if(result!=null)
            {
                if (obj_ideaSub.IsCommentExistForCampaign(Convert.ToInt32(Session["Campaign_Id"]),id) == false)
                {
                    int point = objReawrd.InsertPointAfterImplementation(Convert.ToInt32(Session["LoogedUser"]),id, result.currentreaction, Convert.ToInt32(Session["Campaign_Id"]));
                }
            }
            return Json(result);
        }

        [HttpPost]
        public JsonResult Dislike(int id)
        {
            ViewModel_LikeOrDislike result = obj_ideaSub.Dislike(id, Convert.ToInt32(Session["LoogedUser"]));
            if (result != null)
            {
                if (obj_ideaSub.IsCommentExistForCampaign(Convert.ToInt32(Session["Campaign_Id"]),id) == false)
                {
                    int point = objReawrd.InsertPointAfterImplementation(Convert.ToInt32(Session["LoogedUser"]), id, result.currentreaction, Convert.ToInt32(Session["Campaign_Id"]));
                }
            }
            return Json(result);
        }


        public ActionResult IdeaDetails(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            ViewModel_Campaign model = new ViewModel_Campaign();
            int UserId = Convert.ToInt32(User.Identity.Name);
            model = obj_ideaSub.AllCommentsforIdea(id, UserId);
            return View(model);
        }

        [HttpPost]
        public JsonResult AddComment(ViewModel_Comments model)
        {
            int UserId = Convert.ToInt32(User.Identity.Name);
            model = obj_ideaSub.AddComment(model, UserId);
            if (model.id >0)
            {
                if ((obj_ideaSub.IsCommentPointsAllow(Convert.ToInt32(Session["Campaign_Id"]), Convert.ToInt32(Session["LoogedUser"])) == false))
                    {
                    int point = objReawrd.InsertPointAfterImplementation(Convert.ToInt32(Session["LoogedUser"]), model.idea, model.currentaction, Convert.ToInt32(Session["Campaign_Id"]));
                }
            }
            return Json(model);
        }

        [HttpPost]
        public JsonResult LikeComment(int id, int idea)
        {
            return Json(obj_ideaSub.LikeComment(id, idea, (int)Session["LoogedUser"]));
        }

        [HttpPost]
        public JsonResult DislikeComment(int id, int idea)
        {
            return Json(obj_ideaSub.DislikeComment(id, idea, (int)Session["LoogedUser"]));
        }

        [HttpPost]
        public JsonResult IsValidUserForIdea(int ideaid)
        {
            int UserId = Convert.ToInt32(User.Identity.Name);
            return Json(obj_ideaSub.IsValidUserForIdea(ideaid, UserId));
        }

        [HttpPost]
        public JsonResult IsValidUserForComment(int ideaid)
        {
            int UserId = Convert.ToInt32(User.Identity.Name);
            return Json(obj_ideaSub.IsValidUserForComment(ideaid, UserId));
        }

        [HttpPost]
        public JsonResult IsValidUserForCommentLike(int commentid, int ideaid)
        {
            int UserId = Convert.ToInt32(User.Identity.Name);
            return Json(obj_ideaSub.IsValidUserForCommentLike(commentid, ideaid, UserId));
        }

        public ActionResult Evaluators()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            List<ViewModel_CampaignByRole> model = new List<ViewModel_CampaignByRole>();
            int UserId = Convert.ToInt32(User.Identity.Name);
            model = _obj_camp.CampaignsByRole(UserId);
            return View(model);
        }

        public ActionResult ViewCampaignDetails(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            if (id != 0)
            {
                Session["Campaign_Id"] = id;

            }
            int UserId = Convert.ToInt32(User.Identity.Name);
            ViewModel_Campaign model = new ViewModel_Campaign();
            model = obj_ideaSub.CampaignDetailsForEvaluation(Convert.ToInt32(Session["Campaign_Id"]), UserId);
            model.Id = id;
            return View(model);
        }

        public ActionResult ViewCampaignDetailsSpecific(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            if (id != 0)
            {
                Session["Campaign_Id"] = id;

            }
            int UserId = Convert.ToInt32(User.Identity.Name);
            ViewModel_Campaign model = new ViewModel_Campaign();
            model = obj_ideaSub.CampaignDetailsForEvaluation(Convert.ToInt32(Session["Campaign_Id"]), UserId);
            model.Id = id;
            return View(model);
        }
        [HttpGet]
        public JsonResult CampaginImages(int campaginId)
        {
            IEnumerable<ViewModel_CustomerCampaignImages> _CampaignsImages = _cust.getCampaignImages("0", campaginId);
            //IEnumerable<ViewModel_Customers_Campaigns_Album> _CampaignsImages = _gallaryRepository.GetCampaignsAlbumList(gallaryId);
            return Json(_CampaignsImages, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Evaluation(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            if (id != 0)
            {
                Session["Campaign_Id"] = id;
            }
            int UserId = Convert.ToInt32(User.Identity.Name);
            ViewModel_Campaign model = new ViewModel_Campaign();
            model = obj_ideaSub.CampaignDetailsForEvaluation(Convert.ToInt32(Session["Campaign_Id"]), UserId);
            model.Id = id;
            return View(model);
        }

        public ActionResult Implementation(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            if (id != 0)
            {
                Session["Campaign_Id"] = id;
            }
            int UserId = Convert.ToInt32(User.Identity.Name);
            ViewModel_Campaign model = new ViewModel_Campaign();
            model = obj_ideaSub.CampaignDetailsForImplementation(Convert.ToInt32(Session["Campaign_Id"]), UserId);
            model.Id = id;
            return View(model);
        }

        public ActionResult Auditing(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            if (id != 0)
            {
                Session["Campaign_Id"] = id;
            }
            int UserId = Convert.ToInt32(User.Identity.Name);
            ViewModel_Campaign model = new ViewModel_Campaign();
            model = obj_ideaSub.CampaignDetailsForAuditing(Convert.ToInt32(Session["Campaign_Id"]), UserId);
            model.Id = id;
            return View(model);
        }

        public ActionResult ViewIdeaDetails(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            ViewModel_Campaign model = new ViewModel_Campaign();
            int UserId = Convert.ToInt32(User.Identity.Name);
            model = obj_ideaSub.AllCommentsforIdea(id, UserId);
            model.IsEvaluatedForSpecificEvaluator = obj_ideaSub.IsIdeaEvaluatedForSpecificRegularEvaluator(UserId, model.Id, model.idea, "R");
            return View(model);
        }

        public ActionResult ViewIdeaDetailsSpecific(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            ViewModel_Campaign model = new ViewModel_Campaign();
            int UserId = Convert.ToInt32(User.Identity.Name);
            model = obj_ideaSub.AllCommentsforIdea(id, UserId);
            model.IsEvaluatedForSpecificEvaluator = obj_ideaSub.IsIdeaEvaluatedForSpecificRegularEvaluator(UserId, model.Id, model.idea, "SR");
            return View(model);
        }

        [HttpPost]
        public JsonResult RegularEvaluationSubmission(ViewModel_Evaluation model)
        {
            int UserId = Convert.ToInt32(User.Identity.Name);
            int CampaignId = 0;
            if (Session["Campaign_Id"]!=null && Convert.ToInt32(Session["Campaign_Id"])!=0)
            {
                 CampaignId = Convert.ToInt32(Session["Campaign_Id"]);
            }
            else
            {
                CampaignId = obj_ideaSub.GetCampaignId(Convert.ToInt32(model.idea));
            }
            
            bool result = obj_ideaSub.RegularEvaluationSubmission(model, UserId, CampaignId);
            bool allow = _cust.IsEmailNotificationAllowed(CampaignId);
            bool allowsms = _cust.IsSMSNotificationAllowed(CampaignId);
            if (result == true)
            {
                int point = objReawrd.InsertPointAfterImplementation(UserId,0,0,CampaignId);
                if (allow == true)
                {
                    #region Email Address
                    string to_regularevaluator = obj_com.emailforuser(UserId);
                    string to_campaignmanager = obj_com.emails_campaignmanager(CampaignId);
                    string to_inovationmanager = obj_com.InnovationManagerEmail(CampaignId, 0);
                    string to_chiefevaluator = obj_com.emails_chiefevaluator_for_regularevaluator((int)model.idea, UserId);
                    string to_targetuser = obj_com.emails_targetuser_for_idea((int)model.idea);

                    #endregion

                    #region Email Content
                    /*------------------------------Email Contents------------------------*/

                    List<EmailContents> emails_contents = _cust.CampaignSummary_RegularEvaluation((int)model.idea, UserId);

                    #endregion

                    #region Email Send
                    /*----------------------Regular Evaluator Email-------------------------*/
                    _email.SendEmail(to_regularevaluator, emails_contents[0].body, emails_contents[0].subject);

                    /*----------------------Campaign Manager Email-------------------------*/
                    _email.SendEmail(to_campaignmanager, emails_contents[1].body, emails_contents[1].subject);

                    /*----------------------Innovation Manager Email-------------------------*/
                    _email.SendEmail(to_inovationmanager, emails_contents[2].body, emails_contents[2].subject);

                    /*----------------------Chief Evaluator Email-------------------------*/
                    if (emails_contents[3].body != null)
                    {
                        _email.SendEmail(to_chiefevaluator, emails_contents[3].body, emails_contents[3].subject);
                    }

                    /*----------------------Target User Email-------------------------*/
                    _email.SendEmail(to_targetuser, emails_contents[4].body, emails_contents[4].subject);
                    #endregion
                }
                
                if(allowsms==true)
                {
                    //Contact Nos
                    string to_targetuser_sms = obj_com.ContactNo_TargetUser((int)model.idea);
                    string to_regularevaluator_sms = obj_com.ContactNo_LoggedUser(UserId);
                    string to_campaignmanager_sms = obj_com.ContactNo_CampaignManager(CampaignId);
                    string to_chiefevaluator_sms = obj_com.ContactNo_ChiefEvaluator_OnRegularEvaluation(CampaignId, (int)model.idea, UserId);
                    string to_inovationmanager_sms = obj_com.ContactNo_InnovationManager(CampaignId);

                    //SMS Text

                    string targetuser_smstext = obj_com.SMSText_TargetUser_OnRegularEvaluation((int)model.idea);
                    string regularevaluator_smstext = obj_com.SMSText_RegularEvaluator_OnRegularEvaluation(UserId);
                    string campaignmanager_smstext = obj_com.SMSText_CampaignManager_OnRegularEvaluation((int)model.idea);
                    string chiefevaluator_smstext = obj_com.SMSText_ChiefEvaluator_OnRegularEvaluation((int)model.idea);

                    List<SMS_Info> contactnos = new List<SMS_Info>();
                    SMS_Info con1 = new SMS_Info();
                    con1.contactno = to_targetuser_sms;
                    con1.message = targetuser_smstext;
                    contactnos.Add(con1);

                    SMS_Info con2 = new SMS_Info();
                    con2.contactno = to_regularevaluator_sms;
                    con2.message = regularevaluator_smstext;
                    contactnos.Add(con2);

                    SMS_Info con3 = new SMS_Info();
                    con3.contactno = to_campaignmanager_sms;
                    con3.message = campaignmanager_smstext;
                    contactnos.Add(con3);

                    SMS_Info con4 = new SMS_Info();
                    con4.contactno = to_chiefevaluator_sms;
                    con4.message = chiefevaluator_smstext;
                    contactnos.Add(con4);

                    SMS_Info con5 = new SMS_Info();
                    con5.contactno = to_inovationmanager_sms;
                    con5.message = campaignmanager_smstext;
                    contactnos.Add(con5);

                    int innovationmanager = obj_team.GetInnovationManagerByCampaignId(CampaignId);
                    _sms.SendSMSNotification(contactnos, innovationmanager);
                }
            }


            return Json(model.idea);
        }

        public ActionResult LogOut()
        {
            Response.Cookies["UserName"].Expires = DateTime.Now.AddDays(-1);
            Response.Cookies["Password"].Expires = DateTime.Now.AddDays(-1);

            FormsAuthentication.SignOut();
            Session.Abandon();

            // clear authentication cookie
            HttpCookie cookie1 = new HttpCookie(FormsAuthentication.FormsCookieName, "");
            cookie1.Expires = DateTime.Now.AddDays(-1d);
            Response.Cookies.Add(cookie1);

            // clear session cookie (not necessary for your current problem but i would recommend you do it anyway)
            SessionStateSection sessionStateSection = (SessionStateSection)WebConfigurationManager.GetSection("system.web/sessionState");
            HttpCookie cookie2 = new HttpCookie(sessionStateSection.CookieName, "");
            cookie2.Expires = DateTime.Now.AddYears(-1);
            Response.Cookies.Add(cookie2);
            return RedirectToAction("Index", "Login", new { area = "Innovation" });
        }

        public JsonResult GetSubCategory(int cate)
        {
            return Json(obj_subcate.GetSubCategoriesForDropdown(cate));
        }

        public ActionResult EvaluationDetails(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            ViewModel_Campaign model = new ViewModel_Campaign();
            model = obj_ideaSub.AllCommentsforIdeaWithEvaluatonReview(id, Convert.ToInt32(User.Identity.Name));
            model.IsEvaluatedForSpecificEvaluator = obj_ideaSub.IsIdeaEvaluatedForSpecificChiefEvaluator(Convert.ToInt32(User.Identity.Name), model.Id, id);
            return View(model);
        }

        [HttpPost]
        public JsonResult ChiefEvaluationSubmission(ViewModel_ChiefEvaluation model)
        {
            int UserId = Convert.ToInt32(User.Identity.Name);
            int CampaignId = 0;
            if (Session["Campaign_Id"] != null && Convert.ToInt32(Session["Campaign_Id"]) != 0)
            {
                CampaignId = Convert.ToInt32(Session["Campaign_Id"]);
            }
            else
            {
                CampaignId = obj_ideaSub.GetCampaignId(Convert.ToInt32(model.idea));
            }
            bool result = obj_ideaSub.ChiefEvaluationSubmission(model, UserId, CampaignId);
            bool allow = _cust.IsEmailNotificationAllowed(CampaignId);
            bool allowsms = _cust.IsSMSNotificationAllowed(CampaignId);
            int point = objReawrd.InsertPointAfterImplementation(UserId,0,0,CampaignId);
            if (result == true)
            {
                
                if (allow == true)
                {
                    #region Email Address
                    string to_chiefevaluator = obj_com.emailforuser(UserId);
                    string to_campaignmanager = obj_com.emails_campaignmanager(CampaignId);
                    string to_inovationmanager = obj_com.InnovationManagerEmail(CampaignId, 0);
                    string to_regularevaluator = obj_com.emails_regularevaluator_for_idea((int)model.idea);
                    string to_targetuser = obj_com.emails_targetuser_for_idea((int)model.idea);

                    #endregion

                    #region Email Content
                    /*------------------------------Email Contents------------------------*/

                    List<EmailContents> emails_contents = _cust.CampaignSummary_ChiefEvaluation((int)model.idea, UserId);

                    #endregion

                    #region Email Send
                    /*----------------------Regular Evaluator Email-------------------------*/
                    _email.SendEmail(to_regularevaluator, emails_contents[0].body, emails_contents[0].subject);

                    /*----------------------Campaign Manager Email-------------------------*/
                    _email.SendEmail(to_campaignmanager, emails_contents[1].body, emails_contents[1].subject);

                    /*----------------------Innovation Manager Email-------------------------*/
                    _email.SendEmail(to_inovationmanager, emails_contents[2].body, emails_contents[2].subject);

                    /*----------------------Chief Evaluator Email-------------------------*/
                    if (emails_contents[3].body != null)
                    {
                        _email.SendEmail(to_chiefevaluator, emails_contents[3].body, emails_contents[3].subject);
                    }

                    /*----------------------Target User Email-------------------------*/
                    _email.SendEmail(to_targetuser, emails_contents[4].body, emails_contents[4].subject);
                    #endregion
                }
                if(allowsms==true)
                {
                    string to_targetuser_sms = obj_com.ContactNo_TargetUser((int)model.idea);
                    string to_campaignmanager_sms = obj_com.ContactNo_CampaignManager(CampaignId);
                    string to_chiefevaluator_sms = obj_com.ContactNo_LoggedUser(UserId);
                    string to_inovationmanager_sms = obj_com.ContactNo_InnovationManager(CampaignId);
                    string to_regularevaluator_sms = obj_com.ContactNo_RegularEvaluator_OnChiefEvaluation(CampaignId, (int)model.idea,UserId);


                    string targetuser_smstext = obj_com.SMSText_TargetUser_OnChiefEvaluation((int)model.idea);
                    string campaignmanager_smstext = obj_com.SMSText_CampaignManager_OnChiefEvaluation((int)model.idea);
                    string chiefevaluator_smstext = obj_com.SMSText_ChiefEvaluator_OnChiefEvaluation((int)model.idea,UserId);
                    string regularevaluator_smstext = obj_com.SMSText_RegularEvaluator_OnChiefEvaluation((int)model.idea);


                    List<SMS_Info> contactnos = new List<SMS_Info>();
                    SMS_Info con1 = new SMS_Info();
                    con1.contactno = to_targetuser_sms;
                    con1.message = targetuser_smstext;
                    contactnos.Add(con1);

                    SMS_Info con2 = new SMS_Info();
                    con2.contactno = to_campaignmanager_sms;
                    con2.message = campaignmanager_smstext;
                    contactnos.Add(con2);

                    SMS_Info con3 = new SMS_Info();
                    con3.contactno = to_chiefevaluator_sms;
                    con3.message = chiefevaluator_smstext;
                    contactnos.Add(con3);

                    SMS_Info con4 = new SMS_Info();
                    con4.contactno = to_inovationmanager_sms;
                    con4.message = campaignmanager_smstext;
                    contactnos.Add(con4);

                    if(!string.IsNullOrEmpty(to_regularevaluator_sms))
                    {
                        if (!string.IsNullOrEmpty(regularevaluator_smstext))
                        {
                            SMS_Info con5 = new SMS_Info();
                            con5.contactno = to_regularevaluator_sms;
                            con5.message = regularevaluator_smstext;
                            contactnos.Add(con5); 
                        }
                    }

                    int innovationmanager = obj_team.GetInnovationManagerByCampaignId(CampaignId);

                    _sms.SendSMSNotification(contactnos,innovationmanager);
                }
            }

            return Json(model.idea);
        }

        [HttpPost]
        public JsonResult IsEvaluationSubmittedByChiefEvaluator(int idea)
        {
            int UserId = Convert.ToInt32(User.Identity.Name);
            int CampaignId = (int)Session["Campaign_Id"];
            return Json(obj_ideaSub.IsEvaluationSubmittedByChiefEvaluator(idea, UserId, CampaignId));
        }

        public ActionResult CampaignIdeas(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            if (id != 0)
            {

                Session["Campaign_Id"] = id;
            }
            int UserId = Convert.ToInt32(User.Identity.Name);
            ViewModel_Campaign model = new ViewModel_Campaign();
            model = obj_ideaSub.CampaignDetailsForCampaignManager(Convert.ToInt32(Session["Campaign_Id"]), UserId);
            model.Id = id;
            return View(model);
        }

        public ActionResult ImplementingIdea(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            ViewModel_Campaign model = new ViewModel_Campaign();
            model = obj_ideaSub.AllCommentsforIdeaWithEvaluatonReviewForCampaignManager(id);
            model.idea = id;
            return View(model);
        }
        public ActionResult ProjectManager(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");


            ViewModel_ProjectManager model = new ViewModel_ProjectManager();

            model = obj_proj.GetProjectManager(id);
            int campid = 0;
            if (Session["Campaign_Id"] != null && Convert.ToInt32(Session["Campaign_Id"]) != 0)
            {
                campid = Convert.ToInt32(Session["Campaign_Id"]);
            }
            else
            {
                campid = obj_ideaSub.GetCampaignId(Convert.ToInt32(id));
                Session["Campaign_Id"] = campid;
            }
            BindUsers(model.manager, model.auditor);
            if (model != null)
            {
                model.idea = id;

            }

            int customerid = obj_ideaSub.GetCustomerIdByCampaign(Convert.ToInt32(Session["Campaign_Id"]));

           // int campid = Convert.ToInt32(Session["Campaign_Id"]);

            

         

            SelectList list = new SelectList(_cust.GetAuditTypesForAudit(campid, customerid), "id", "typename");
            ViewBag.Type = list;

            return View(model);
        }

        [HttpPost]
        public ActionResult ProjectManager(ViewModel_ProjectManager model)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            int UserId = Convert.ToInt32(User.Identity.Name);

            int CampaignId = 0;
            if (Session["Campaign_Id"] != null && Convert.ToInt32(Session["Campaign_Id"]) != 0)
            {
                CampaignId = Convert.ToInt32(Session["Campaign_Id"]);
            }
            else
            {
                CampaignId = obj_ideaSub.GetCampaignId(Convert.ToInt32(model.idea));
            }

            //int CampaignId = (int)Session["Campaign_Id"];


            model.entry_by = UserId;
            model.campaign = CampaignId;
            bool result = obj_proj.AddOrUpdateProjectManager(model);

            if (result == true)
            {
                #region Email Address
                string to_campaignmanager = obj_com.emailforuser(UserId);
                string to_inovationmanager = obj_com.InnovationManagerEmail(CampaignId, 0);
                List<string> to_projectmanager = obj_com.emails_projectmanager(model.campaign);
                List<string> to_auditor = obj_com.emails_auditor(model.idea,obj_com.GetAuditType(model.entry_by,model.idea));

                #endregion

                #region Email Content
                /*------------------------------Email Contents------------------------*/

                List<EmailContents> emails_contents = _cust.CampaignSummary_Implementation((int)model.idea, UserId);

                #endregion

                #region Email Send
                /*----------------------Campaign Manager Email-------------------------*/
                _email.SendEmail(to_campaignmanager, emails_contents[3].body, emails_contents[3].subject);

                /*----------------------Innovation Manager Email-------------------------*/
                _email.SendEmail(to_inovationmanager, emails_contents[2].body, emails_contents[2].subject);


                /*----------------------Project Manager Email-------------------------*/
                foreach (var item in to_projectmanager)
                {
                    _email.SendEmail(item, emails_contents[0].body, emails_contents[0].subject);
                }
                

                /*----------------------Auditor Email-------------------------*/
                foreach (var item in to_auditor)
                {
                    _email.SendEmail(item, emails_contents[1].body, emails_contents[1].subject);
                }
                
                #endregion
            }

            BindUsers(model.manager, model.auditor);
            ViewBag.Message = "Data Saved Successfully.";


            int customerid = obj_ideaSub.GetCustomerIdByCampaign(Convert.ToInt32(Session["Campaign_Id"]));

            int campid = Convert.ToInt32(Session["Campaign_Id"]);

            SelectList list = new SelectList(_cust.GetAuditTypesForAudit(campid, customerid), "id", "typename");
            ViewBag.Type = list;

            return View(model);
        }

        public void BindUsers(int _manager, int _auditor)
        {
            int innovationmanager = obj_team.GetInnovationManagerByCampaignId(Convert.ToInt32(Session["Campaign_Id"]));
            int campaginnmanager = obj_team.GetCampaginManagerByCampaignId(Convert.ToInt32(Session["Campaign_Id"]));

            List<ViewModel_InnovationTeams> manager = obj_team.GetTeamsForDropdownNew(innovationmanager, campaginnmanager);
            SelectList lstmanager = new SelectList(manager, "Id", "Name");
            ViewBag.manager = lstmanager;

            List<ViewModel_InnovationTeams> auditor = obj_team.GetTeamsForDropdownNew(innovationmanager, campaginnmanager);

            SelectList lstauditor = new SelectList(auditor, "Id", "Name");
            ViewBag.auditor = lstauditor;
        }

        public void BindUsersList()
        {
            int innovationmanager = obj_team.GetInnovationManagerByCampaignId(Convert.ToInt32(Session["Campaign_Id"]));
            int campaginnmanager = obj_team.GetCampaginManagerByCampaignId(Convert.ToInt32(Session["Campaign_Id"]));

            List<ViewModel_InnovationTeams> manager = obj_team.GetTeamsForDropdownNew(innovationmanager, campaginnmanager);
            SelectList lstmanager = new SelectList(manager, "Id", "Name");
            ViewBag.Users = lstmanager;
        }

        public ActionResult ProjectManagerImplementation(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            if (id != 0)
            {
                int campid  =obj_ideaSub.GetCampaignId(id);
                Session["Campaign_Id"] = campid;
            }

            ViewModel_Campaign model = new ViewModel_Campaign();
            int userid = Convert.ToInt32(User.Identity.Name);
            model = obj_ideaSub.GetCampaignIdeaForProjectManager(id, userid);
            return View(model);
        }

        public ActionResult TaskCreation(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");


            ViewModel_TaskCreation model = new ViewModel_TaskCreation();
            BindUsersList();
            model.idea = id;
            model.tasks = obj_task.GetAllTask(Convert.ToInt32(User.Identity.Name), model.idea, Convert.ToInt32(Session["Campaign_Id"]));
            return View(model);
        }

        [HttpPost]
        public JsonResult TaskCreation(ViewModel_TaskCreation model)
        {
            model.campaign = Convert.ToInt32(Session["Campaign_Id"]);
            int userid = Convert.ToInt32(User.Identity.Name);
            model.entry_by = userid;
            int taskid = obj_task.AddTask(model);
            bool result = false;
            if(taskid>0)
            {
                result = true;
            }
            bool allow = _cust.IsEmailNotificationAllowed(model.campaign);
            bool allowsms = _cust.IsEmailNotificationAllowed(model.campaign);
            if (result == true)
            {
                if (allow == true)
                {
                    #region Email Address
                    string to_projectmanager = obj_com.emailforuser(userid);
                    string to_campaignmanager = obj_com.emails_campaignmanager(model.campaign);
                    string to_inovationmanager = obj_com.InnovationManagerEmail(model.campaign, 0);
                    List<string> to_taskpersons = obj_com.emails_taskpersons((int)model.idea, 0);


                    #endregion

                    #region Email Content
                    /*------------------------------Email Contents------------------------*/

                    List<EmailContents> emails_contents = _cust.CampaignSummary_TaskCreation((int)model.idea, userid);

                    #endregion

                    #region Email Send
                    /*----------------------Project Manager Email-------------------------*/
                    _email.SendEmail(to_projectmanager, emails_contents[0].body, emails_contents[0].subject);

                    /*----------------------Task Person Email-------------------------*/
                    foreach (var item in to_taskpersons)
                    {
                        _email.SendEmail(item, emails_contents[1].body, emails_contents[1].subject);
                    }
                    /*----------------------Campaign Manager Email-------------------------*/
                    _email.SendEmail(to_campaignmanager, emails_contents[3].body, emails_contents[3].subject);

                    /*----------------------Innovation Manager Email-------------------------*/
                    _email.SendEmail(to_inovationmanager, emails_contents[2].body, emails_contents[2].subject);


                    #endregion
                }
                if(allowsms==true)
                {
                    List<string> to_taskperson_sms = obj_com.ContactNo_TaskPerson_OnTaskCreation(userid, taskid);

                    string taskperson_smstext = obj_com.SMSText_TaskPerson_OnTaskCreation(userid);

                    List<SMS_Info> contactnos = new List<SMS_Info>();

                    foreach (var item in to_taskperson_sms)
                    {
                        SMS_Info obj = new SMS_Info();
                        obj.contactno = item;
                        obj.message = taskperson_smstext;
                        contactnos.Add(obj);
                    }

                    int innovationmanager = obj_team.GetInnovationManagerByCampaignId(model.campaign);

                    _sms.SendSMSNotification(contactnos,innovationmanager);
                }
            }

            return Json(result);
        }

        [HttpPost]
        public JsonResult GetTasks(int idea)
        {
            return Json(obj_task.GetAllTask(Convert.ToInt32(User.Identity.Name), idea, Convert.ToInt32(Session["Campaign_Id"])));
        }

        [HttpPost]
        public JsonResult GetUsers()
        {
            int innovationmanager = obj_team.GetInnovationManagerByCampaignId(Convert.ToInt32(Session["Campaign_Id"]));
            List<ViewModel_InnovationTeams> team = obj_team.GetTeamsForDropdown(innovationmanager);

            return Json(team);
        }

        [HttpPost]
        public JsonResult UpdateTask(ViewModel_TaskCreation model)
        {
            int userid = Convert.ToInt32(User.Identity.Name);
            model.entry_by = userid;
            return Json(obj_task.UpdateTask(model));
        }




        public ActionResult Tasks()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            int userid = Convert.ToInt32(User.Identity.Name);
            return View(obj_task.GetAllTaskTaskMember(userid));
        }

        public ActionResult TaskConvo(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            int userid = Convert.ToInt32(User.Identity.Name);
            return View(obj_convo.GetTaskConvos(id, userid));
        }

        [HttpPost]
        public JsonResult AddTaskConvo(ViewModel_TaskConvo model)
        {
            int entry_by = Convert.ToInt32(User.Identity.Name);
            model.entry_by = entry_by;
            bool result = obj_convo.AddTaskConvo(model);
            return Json(result);
        }

        [HttpPost]
        public JsonResult GetTaskAction(int id)
        {
            int userid = Convert.ToInt32(User.Identity.Name);
            return Json(obj_convo.GetTaskConvos(id, userid));
        }

        [HttpPost]
        public ActionResult UploadFileTaskAction()
        {
            // Checking no of files injected in Request object  
            if (Request.Files.Count > 0)
            {
                try
                {
                    //  Get all files from Request object  
                    HttpFileCollectionBase files = Request.Files;
                    string filepath = string.Empty;
                    for (int i = 0; i < files.Count; i++)
                    {
                        //string path = AppDomain.CurrentDomain.BaseDirectory + "Uploads/";  
                        //string filename = Path.GetFileName(Request.Files[i].FileName);  

                        HttpPostedFileBase file = files[i];
                        string dateTime = DateTime.Now.ToString("_yyyy_MM_dd_HH_mm_ss").ToString();
                        string fileName = Path.GetFileNameWithoutExtension(files[i].FileName);
                        fileName = fileName.Replace(" ", "");
                        string fileExtention = Path.GetExtension(files[i].FileName);
                        //string _FileName = Path.GetFileNameWithoutExtension(item.FileName + DateTime.Now.ToString("_yyyy_MM_dd_HH_mm_ss")) + Path.GetExtension(item.FileName);
                        string _FileName = fileName + dateTime + fileExtention;
                        string _path = Path.Combine(Server.MapPath("~/Images/Gallary/"), _FileName);
                        file.SaveAs(_path);
                        filepath = "/Images/Gallary/" + _FileName; ;




                    }
                    // Returns message that successfully uploaded  
                    return Json(filepath);
                }
                catch (Exception ex)
                {
                    return Json("Error occurred. Error details: " + ex.Message);
                }
            }
            else
            {
                return Json("No files selected.");
            }
        }

        [HttpPost]
        public ActionResult UploadFile()
        {
            // Checking no of files injected in Request object  
            if (Request.Files.Count > 0)
            {
                try
                {
                    //  Get all files from Request object  
                    HttpFileCollectionBase files = Request.Files;
                    string filepath = string.Empty;
                    for (int i = 0; i < files.Count; i++)
                    {
                        //string path = AppDomain.CurrentDomain.BaseDirectory + "Uploads/";  
                        //string filename = Path.GetFileName(Request.Files[i].FileName);  

                        HttpPostedFileBase file = files[i];
                        string fileName = string.Empty;
                        string fileExten = string.Empty;
                        // Checking for Internet Explorer  
                        if (Request.Browser.Browser.ToUpper() == "IE" || Request.Browser.Browser.ToUpper() == "INTERNETEXPLORER")
                        {
                            string[] testfiles = file.FileName.Split(new char[] { '\\' });
                            fileName = Guid.NewGuid().ToString();
                            fileExten = Path.GetExtension(file.FileName);
                        }
                        else
                        {
                            fileName = Guid.NewGuid().ToString();
                            fileExten = Path.GetExtension(file.FileName);
                        }

                        filepath = "/TaskDocs/" + fileName + fileExten;
                        var path = Path.Combine(Server.MapPath("/TaskDocs/"), fileName + fileExten);
                        file.SaveAs(path);


                        // Get the complete folder path and store the file inside it.  
                        //fname = Path.Combine(Server.MapPath("~/Uploads/"), fname);
                        //file.SaveAs(fname);
                    }
                    // Returns message that successfully uploaded  
                    return Json(filepath);
                }
                catch (Exception ex)
                {
                    return Json("Error occurred. Error details: " + ex.Message);
                }
            }
            else
            {
                return Json("No files selected.");
            }
        }

        public ActionResult Auditor(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            ViewModel_Campaign model = new ViewModel_Campaign();
            int userid = Convert.ToInt32(User.Identity.Name);
            model = obj_ideaSub.GetCampaignForAuditor(id, userid);
            //Session["Campaign_Id"] = id;
            return View(model);
        }

        public ActionResult StartAuditing(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");


            Session["Campaign_Id"] = obj_ideaSub.GetCampaignId(id);
            //string isAudit = objAuditing.IsAuditApprove(Convert.ToInt16(Session["Campaign_Id"]));
            string isAudit = string.Empty;
            if (string.IsNullOrEmpty(isAudit.ToString()))
            {
                ViewModel_CampaginAudit _audit = new ViewModel_CampaginAudit();
                _audit = objAuditing.GetTaskDetail(id, Convert.ToInt32(User.Identity.Name));
                _audit.IdeaID = id;
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
                return RedirectToAction("Auditor", "User", new { id = campid });
            }
        }
        [HttpPost]
        public JsonResult GetAuditFormsFields(int id)
        {
            return Json(objAuditing.GetFormsFields(id));
        }
        public ActionResult AuditorTaskComment(ViewModel_CampaginAudit audit)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            int userid = Convert.ToInt32(User.Identity.Name);
            int TotalTaskCount = objAuditing.GetTaskCount(audit.TaskcommentList[0].IdeaID, userid);
            int isAllRejected = 0;
            if (TotalTaskCount == audit.TaskcommentList.Count)
            {
                for (int i = 0; i < audit.TaskcommentList.Count; i++)
                {
                    if (audit.TaskcommentList[i].IsApprove == true)
                    {
                        isAllRejected = 0;
                        break;
                    }
                    else if (audit.TaskcommentList[i].IsApprove == false)
                    {
                        isAllRejected = 1;
                    }
                }
            }
            for (int i = 0; i < audit.TaskcommentList.Count; i++)
            {
                ViewModel_AuditorTaskComment _data = new ViewModel_AuditorTaskComment();
                _data.AutherID = userid;
                _data.Comment = audit.TaskcommentList[i].Comment;
                _data.TaskID = audit.TaskcommentList[i].TaskID;
                _data.IsApprove = audit.TaskcommentList[i].IsApprove;
                _data.IdeaID = audit.TaskcommentList[i].IdeaID;
                _data.AuditorTaskCommentID = audit.TaskcommentList[i].AuditorTaskCommentID;
                _data.isAllRejected = isAllRejected;
                int res = objAuditing.AddAuditorTaskComment(_data);
            }
            return Json(isAllRejected);
            //TempData["IsdataSave"] = "1";
            //return RedirectToAction("StartAuditing", "User", new { id = 1 });
        }

        [HttpPost]
        public JsonResult AuditorApprovalAndCustomeForm(ViewModel_AuditorApprovalCustomeForm model)
        {

            int CampaignId = Convert.ToInt32(Session["Campaign_Id"]);
            int UserId = Convert.ToInt32(User.Identity.Name);
            int res = objAuditing.AuditorApproveAndCustomeForm(model, CampaignId, UserId);
            bool allow = _cust.IsEmailNotificationAllowed(CampaignId);
            bool allowsms = _cust.IsEmailNotificationAllowed(CampaignId);
            if (res > 0)
            {
                int point = objReawrd.InsertPointAfterImplementation(UserId,0,0,CampaignId);

                if (allowsms==true)
                {
                    string to_projectmanager_sms = obj_com.ContactNo_ProjectManager_OnAuditing(UserId, model.IdeaID);
                    string to_campaignmanager_sms = obj_com.ContactNo_InnovationManager(CampaignId);
                    string to_inovationmanager_sms = obj_com.ContactNo_CampaignManager(CampaignId);


                    string projectmanager_smstext = obj_com.SMSText_ProjectManager_OnAuditing(UserId, model.IdeaID);
                    string campaignmanager_smstext = obj_com.SMSText_CampaignManager_OnAuditing(UserId, model.IdeaID);
                    string inovationmanager_smstext = obj_com.SMSText_Innovation_OnAuditing(UserId, model.IdeaID);

                    List<SMS_Info> contactnos = new List<SMS_Info>();
                    SMS_Info obj1 = new SMS_Info();
                    obj1.contactno = to_projectmanager_sms;
                    obj1.message = projectmanager_smstext;
                    contactnos.Add(obj1);

                    SMS_Info obj2 = new SMS_Info();
                    obj2.contactno = to_campaignmanager_sms;
                    obj2.message = campaignmanager_smstext;
                    contactnos.Add(obj2);

                    if(!string.IsNullOrEmpty(inovationmanager_smstext))
                    {
                        SMS_Info obj3 = new SMS_Info();
                        obj3.contactno = to_inovationmanager_sms;
                        obj3.message = inovationmanager_smstext;
                        contactnos.Add(obj3);
                    }

                    int innovationmanager = obj_team.GetInnovationManagerByCampaignId(CampaignId);
                    _sms.SendSMSNotification(contactnos,innovationmanager);
                }


                if (model.IsApproved == 1)
                {
                    //int camMgrId = objReawrd.GetInnovationmanagerId(UserId);
                    //objReawrd.AllotReward(CampaignId, camMgrId);
                }
            }
            return Json(res.ToString(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult Reward()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            //List<ViewModel_CampaignByRole> model = new List<ViewModel_CampaignByRole>();
            //ViewModel_User_Reward _obj = new ViewModel_User_Reward();
            //int UserId = Convert.ToInt32(User.Identity.Name);
            //ViewModel_UserPointsAndRewards _objPointAndReward = new ViewModel_UserPointsAndRewards();
            //model = _obj_camp.CampaignsByRole(UserId);
            //int camMgrId = objReawrd.GetInnovationmanagerId(UserId);
            //if (model != null)
            //{
            //    _obj = objReawrd.GetUserRankAndReward(model, camMgrId);
            //    objReawrd.AddPoints(UserId, _obj.TotalPoint);
            //}
            //_objPointAndReward = objReawrd.GetuserTotalPoint(UserId, camMgrId);
            //_obj.UserEarcnTotalPoint = _objPointAndReward.userEarnPoints;
            //_obj.Rank = _objPointAndReward.Rank;
            //_obj.Belt = _obj.Belt;


            ViewModel_UserPointsAndRewards _objPointAndReward = new ViewModel_UserPointsAndRewards();
            int UserId = Convert.ToInt32(User.Identity.Name);
            int camMgrId = objReawrd.GetInnovationmanagerId(UserId);
            //int res = objReawrd.InsertPointAfterImplementation(UserId);
            ViewModel_User_Reward _obj = new ViewModel_User_Reward();
            _obj.MemberPoint = objReawrd.GetUserRankAndReward(UserId);
            _objPointAndReward = objReawrd.GetuserTotalPoint(UserId, camMgrId);

            _obj.Rank = _objPointAndReward.Rank;
            _obj.UserEarcnTotalPoint = _objPointAndReward.userEarnPoints;
            _obj.Belt = _objPointAndReward.BeltImage;
            return View(_obj);
        }
        public ActionResult GetPointsAndRewards()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            int userid = Convert.ToInt32(User.Identity.Name);
            int camMgrId = objReawrd.GetInnovationmanagerId(userid);

            ViewModel_UserPointsAndRewards _objPointAndReward = new ViewModel_UserPointsAndRewards();
            _objPointAndReward = objReawrd.GetuserTotalPoint(userid, camMgrId);

            List<ViewModel_PointsAndRewards> _memberRewardsPoint = objMemberPoint.GetUserPointsAndRewards(camMgrId, userid);
            ViewModel_UserPointsAndRewards _obj = new ViewModel_UserPointsAndRewards();
            _obj.userTotalPoints = _objPointAndReward.userEarnPoints;
            _obj.userEarnPoints = _objPointAndReward.userEarnPoints;
            _obj.PointAndReward = _memberRewardsPoint;
            return PartialView("_RewardAndPoints", _obj);
        }
        public ActionResult ReedemPoint(int reedemid, int point)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            int userid = Convert.ToInt32(User.Identity.Name);
            int RedeemId = objReawrd.ReedemdPoint(reedemid, point, userid);
            if(RedeemId > 0)
            {
                ViewModel_RewardRequest model = objReawrd.Get_UserDetailByRedeemedId(RedeemId);
                if (model != null)
                {
                    string body = "Hi " + "" + model.InnovationName + "" + "<br/>" + "<br/>" + "You Received a new request from" + "  " + model.UserName + " " +
                                   " for Reward item  "+""+model.RewardItem+"   with Redeemed Points:  "+""+model.RedeemPoints+"  "+
                                   "<br/>" + "<br/>" + "<br/>" + "Thank You" + "<br/>" + "<br/>" + "<br/>" + "Dean Infotech Pvt Lmt.....";
                    _email.Send_RewardMailtoManager(model.InnovationMail, body);
                }

            }
            return GetPointsAndRewards();
        }

        [HttpPost]
        public JsonResult CloseTask(int id)
        {
            bool result = obj_task.CloseTask(id);
            int idea = obj_task.IdeaForTask(id);
            int CampaignId = Convert.ToInt32(Session["Campaign_Id"]);
            bool allow = _cust.IsEmailNotificationAllowed(CampaignId);
            bool allowsms = _cust.IsSMSNotificationAllowed(CampaignId);
            int userid = Convert.ToInt32(User.Identity.Name);
            if (result == true)
            {
                string auditor_smstext = obj_com.SMSText_Auditor_OnTaskClose(userid);
                if(!string.IsNullOrEmpty(auditor_smstext))
                {
                    int point = objReawrd.InsertPointAfterImplementation(userid,0,0,CampaignId);
                    int point_task_person = objReawrd.InsertPointAfterImplementation(userid, idea,0,CampaignId);
                }
                
                #region Task Details
                ViewModel_TaskCreation model = obj_com.taskdetails(id);
                #endregion

                if (allow == true)
                {
                    #region Email Address
                    string to_projectmanager = obj_com.emailforuser(model.entry_by);
                    string to_campaignmanager = obj_com.emails_campaignmanager(model.campaign);
                    string to_inovationmanager = obj_com.InnovationManagerEmail(model.campaign, 0);
                    List<string> to_taskpersons = obj_com.emails_taskpersons((int)model.idea, model.id);
                    List<string> to_auditor = obj_com.emails_auditor(model.idea, obj_com.GetAuditType(model.entry_by, model.idea));

                    #endregion

                    #region Email Content
                    /*------------------------------Email Contents------------------------*/

                    List<EmailContents> emails_contents = _cust.CampaignSummary_TaskClosed((int)model.idea, model.entry_by);

                    #endregion

                    #region Email Send
                    /*----------------------Project Manager Email-------------------------*/
                    _email.SendEmail(to_projectmanager, emails_contents[0].body, emails_contents[0].subject);

                    /*----------------------Task Person Email-------------------------*/
                    foreach (var item in to_taskpersons)
                    {
                        _email.SendEmail(item, emails_contents[1].body, emails_contents[1].subject);
                    }
                    /*----------------------Campaign Manager Email-------------------------*/
                    _email.SendEmail(to_campaignmanager, emails_contents[3].body, emails_contents[3].subject);

                    /*----------------------Innovation Manager Email-------------------------*/
                    _email.SendEmail(to_inovationmanager, emails_contents[2].body, emails_contents[2].subject);

                    /*----------------------Innovation Manager Email-------------------------*/
                    foreach (var item in to_auditor)
                    {
                        _email.SendEmail(item, emails_contents[4].body, emails_contents[4].subject);
                    }


                    #endregion
                }
                if(allowsms==true)
                {
                    string to_auditor_sms = obj_com.ContactNo_Auditor_OnTaskClose(userid, model.idea);
                    string to_campaignmanager_sms = obj_com.ContactNo_CampaignManager(CampaignId);
                    string to_inovationmanager_sms = obj_com.ContactNo_InnovationManager(CampaignId);

                    
                    string campaignmanager_smstext = obj_com.SMSText_CampaignManager_OnTaskClose(model.idea,userid);

                    List<SMS_Info> contactnos = new List<SMS_Info>();
                    if(!string.IsNullOrEmpty(auditor_smstext))
                    {
                        SMS_Info obj1 = new SMS_Info();
                        obj1.contactno = to_auditor_sms;
                        obj1.message = auditor_smstext;
                        contactnos.Add(obj1);

                        SMS_Info obj2 = new SMS_Info();
                        obj2.contactno = to_campaignmanager_sms;
                        obj2.message = campaignmanager_smstext;
                        contactnos.Add(obj2);

                        SMS_Info obj3 = new SMS_Info();
                        obj3.contactno = to_inovationmanager_sms;
                        obj3.message = campaignmanager_smstext;
                        contactnos.Add(obj3);
                    }

                    if(contactnos.Count>0)
                    {
                        int innovationmanager = obj_team.GetInnovationManagerByCampaignId(CampaignId);

                        _sms.SendSMSNotification(contactnos,innovationmanager);
                    }

                }
            }
            return Json(result);
        }

        public ActionResult MyTask()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            List<ViewModel_TaskList> tasks = obj_task.MyTasks(Convert.ToInt32(User.Identity.Name));
            return View(tasks);
        }
        public ActionResult RewardDetails(string type)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectPermanent("/Innovation/Login");

            int userid = Convert.ToInt32(User.Identity.Name);
            List<ViewModel_User_RewardDetail> _RewardDetail = objReawrd.GetRewardDetail(userid, type);
            return View(_RewardDetail);
        }
        public ActionResult GetIdeaLikeCount(int ideaId)
        {
            ViewModel_LikeOrDislike _LikedislikeCount = obj_ideaSub.GetIdesTotalLikeAndDislike(ideaId);
            return PartialView("_IdealikesAndDislike", _LikedislikeCount);
        }
        public ActionResult GetIdeaComments(int ideaId)
        {
            List<ViewModel_Comments> _IdeaComment = obj_ideaSub.GetIdeaComments(ideaId);
            return PartialView("_ideaComments", _IdeaComment);

        }
        public ActionResult MyProfile()
        {
            int ManagerId = 0;
            if (Session["InnovationManagerId"] != null)
                ManagerId = Convert.ToInt32(Session["InnovationManagerId"].ToString());
            return View(obj_team.UserProfile(Convert.ToInt32(User.Identity.Name), ManagerId));
        }
        [HttpPost]
        public ActionResult MyProfile(ViewModel_InnovationTeams model, HttpPostedFileBase Logo)
        {
            if (string.IsNullOrEmpty(model.Password) && string.IsNullOrEmpty(model.NewPassword) && string.IsNullOrEmpty(model.ConfirmPassword))
            {
                if (Logo != null)
                {
                    //string _FileName = Path.GetFileNameWithoutExtension(Logo.FileName + DateTime.Now.ToString("_yyyy_MM_dd_HH_mm_ss")) + Path.GetExtension(Logo.FileName);
                    string _FileName = Guid.NewGuid().ToString() + Path.GetExtension(Logo.FileName);
                    string _path = Path.Combine(Server.MapPath("~/Images/CustomerLogo"), _FileName);
                    Logo.SaveAs(_path);
                    model.Logo = "/Images/CustomerLogo/" + _FileName;
                    Session["UserLogo"] = "/Images/CustomerLogo/" + _FileName;
                }
                else
                {
                    model.Logo = null;
                }

                ViewBag.Message = "Success. Record Updated";
                obj_team.UpdateUserProfile(model, false);
            }
            else
            {
                if (model.Password == obj_team.GetTeamById(Convert.ToInt32(User.Identity.Name)).Password)
                {
                    if (!string.IsNullOrEmpty(model.NewPassword) && !string.IsNullOrEmpty(model.ConfirmPassword))
                    {
                        if ((model.NewPassword == model.ConfirmPassword))
                        {
                            if (Logo != null)
                            {
                                string _FileName = Path.GetFileNameWithoutExtension(Logo.FileName + DateTime.Now.ToString("_yyyy_MM_dd_HH_mm_ss")) + Path.GetExtension(Logo.FileName);
                                string _path = Path.Combine(Server.MapPath("~/Images/CustomerLogo"), _FileName);
                                Logo.SaveAs(_path);
                                model.Logo = "/Images/CustomerLogo/" + _FileName;
                                Session["UserLogo"] = "/Images/CustomerLogo/" + _FileName;
                            }
                            else
                            {
                                model.Logo = null;
                            }
                            obj_team.UpdateUserProfile(model, true);
                            ViewBag.Message = "Success. Record Updated";
                        }
                        else
                        {
                            ViewBag.Message = "New Password & Confirm Password do not match.";
                        }
                    }
                    else
                    {
                        ViewBag.Message = "New Password or Confirm Password do not leave blank.";
                    }
                }
                else
                {
                    ViewBag.Message = "Current Password do not match. Please enter current Password.";
                }
            }
            int ManagerId = 0;
            if (Session["InnovationManagerId"] != null)
                ManagerId = Convert.ToInt32(Session["InnovationManagerId"].ToString());
            return View(obj_team.UserProfile(Convert.ToInt32(User.Identity.Name), ManagerId));
        }

        [HttpPost]
        public JsonResult ReopenMyIdea(int idea)
        {
            return Json(obj_ideaSub.ReopenMyIdea(idea));
        }

        [HttpPost]
        public JsonResult ReopenIdea(int idea)
        {
            return Json(obj_ideaSub.ReopenIdea(idea));
        }

        [HttpPost]
        public JsonResult AddProjectManager(ViewModel_ProjectManager model)
        {
            int userid = Convert.ToInt32(User.Identity.Name);
            int campid = Convert.ToInt16(Session["Campaign_Id"]);
            model.entry_by = userid;
            model.entry_date = DateTime.Now;
            model.campaign = campid;
            int campaignid = campid;
            int idea = model.idea;
            bool result = obj_proj.AddProjectManager(model);
            // model = new ViewModel_ProjectManager();
            bool allow = _cust.IsEmailNotificationAllowed(model.campaign);
            bool allowsms = _cust.IsSMSNotificationAllowed(model.campaign);

            if (result == true)
            {
                model = obj_proj.GetAuditor(model.idea);


                if (allow == true)
                {
                    #region Email Address
                    string to_campaignmanager = obj_com.emailforuser(userid);
                    string to_inovationmanager = obj_com.InnovationManagerEmail(campaignid, 0);
                    List<string> to_projectmanager = obj_com.emails_projectmanager(campaignid);
                    

                    #endregion

                    #region Email Content
                    /*------------------------------Email Contents------------------------*/

                    List<EmailContents> emails_contents = _cust.CampaignSummary_Implementation(idea, userid);

                    #endregion

                    #region Email Send
                    /*----------------------Campaign Manager Email-------------------------*/
                    _email.SendEmail(to_campaignmanager, emails_contents[3].body, emails_contents[3].subject);

                    /*----------------------Innovation Manager Email-------------------------*/
                    _email.SendEmail(to_inovationmanager, emails_contents[2].body, emails_contents[2].subject);


                    /*----------------------Project Manager Email-------------------------*/
                    foreach (var item in to_projectmanager)
                    {
                        _email.SendEmail(item, emails_contents[0].body, emails_contents[0].subject);
                    }
                    

                    

                    #endregion
                }

                if(allowsms==true)
                {
                    List<SMS_Info> contactnos = new List<SMS_Info>();

                    List<string> to_projectmanagers_sms = obj_com.ContactNo_ProjectManager_OnCampaignManagerAction(idea);
                    string to_campaignmanager_sms = obj_com.ContactNo_CampaignManager(campaignid);
                    string to_inovationmanager_sms = obj_com.ContactNo_InnovationManager(campaignid);

                    string projectmanagers_smstext = obj_com.SMSText_ProjectManager_OnCampaignManager(idea);
                    string campaignmanager_smstext = obj_com.SMSText_CampaignMangerForProjectManager_OnCampaignManager(idea);
                    string inovationmanager_sms = obj_com.SMSText_InnovationManagerForProjectManager_OnCampaignManager(idea);



                    foreach (var item in to_projectmanagers_sms)
                    {
                        SMS_Info obj = new SMS_Info();
                        obj.contactno = item;
                        obj.message = projectmanagers_smstext;
                        contactnos.Add(obj);
                    }

                    SMS_Info obj1 = new SMS_Info();
                    obj1.contactno = to_campaignmanager_sms;
                    obj1.message = campaignmanager_smstext;
                    contactnos.Add(obj1);

                    SMS_Info obj2 = new SMS_Info();
                    obj2.contactno = to_inovationmanager_sms;
                    obj2.message = inovationmanager_sms;
                    contactnos.Add(obj2);

                    int innovationmanager = obj_team.GetInnovationManagerByCampaignId(campaignid);
                    _sms.SendSMSNotification(contactnos,innovationmanager);
                }
            }
            return Json(model);
        }


        [HttpPost]
        public JsonResult AddAuditor(ViewModel_ProjectManager model)
        {
            int userid = Convert.ToInt32(User.Identity.Name);
            int campid = Convert.ToInt16(Session["Campaign_Id"]);
            model.entry_by = userid;
            model.entry_date = DateTime.Now;
            model.campaign = campid;
            int campaignid = campid;
            int idea = model.idea;

            bool result = obj_proj.AddAuditor(model);
            // model = new ViewModel_ProjectManager();
            bool allow = _cust.IsEmailNotificationAllowed(model.campaign);
            bool allowsms = _cust.IsSMSNotificationAllowed(model.campaign);
            if (result == true)
            {
                int point = objReawrd.InsertPointAfterImplementation(userid,0,0,campid);
                if (allow == true)
                {
                    #region Email Address
                    string to_campaignmanager = obj_com.emailforuser(userid);
                    string to_inovationmanager = obj_com.InnovationManagerEmail(model.campaign, 0);
                    List<string> to_auditor = obj_com.emails_auditor(model.idea, obj_com.GetAuditType(model.entry_by, model.idea));

                    #endregion

                    #region Email Content
                    /*------------------------------Email Contents------------------------*/

                    List<EmailContents> emails_contents = _cust.CampaignSummary_Implementation((int)model.idea, userid);

                    #endregion

                    #region Email Send
                    /*----------------------Campaign Manager Email-------------------------*/
                    _email.SendEmail(to_campaignmanager, emails_contents[3].body, emails_contents[3].subject);

                    /*----------------------Innovation Manager Email-------------------------*/
                    _email.SendEmail(to_inovationmanager, emails_contents[2].body, emails_contents[2].subject);


                    /*----------------------Auditor Email-------------------------*/
                    foreach (var item in to_auditor)
                    {
                        _email.SendEmail(item, emails_contents[1].body, emails_contents[1].subject);
                    }

                    #endregion
                }
                if (allowsms == true)
                {
                    List<SMS_Info> contactnos = new List<SMS_Info>();

                    List<string> to_auditors_sms = obj_com.ContactNo_Auditor_OnCampaignManagerAction(idea);
                    string to_campaignmanager_sms = obj_com.ContactNo_CampaignManager(campaignid);
                    string to_inovationmanager_sms = obj_com.ContactNo_InnovationManager(campaignid);

                    string auditors_smstext = obj_com.SMSText_Auditor_OnCampaignManager(idea);
                    string campaignmanager_smstext = obj_com.SMSText_CampaignMangerForAuditor_OnCampaignManager(idea);
                    string inovationmanager_sms = obj_com.SMSText_InnovationManagerForAuditor_OnCampaignManager(idea);



                    foreach (var item in to_auditors_sms)
                    {
                        SMS_Info obj = new SMS_Info();
                        obj.contactno = item;
                        obj.message = auditors_smstext;
                        contactnos.Add(obj);
                    }

                    SMS_Info obj1 = new SMS_Info();
                    obj1.contactno = to_campaignmanager_sms;
                    obj1.message = campaignmanager_smstext;
                    contactnos.Add(obj1);

                    SMS_Info obj2 = new SMS_Info();
                    obj2.contactno = to_inovationmanager_sms;
                    obj2.message = inovationmanager_sms;
                    contactnos.Add(obj2);

                    int innovationmanager = obj_team.GetInnovationManagerByCampaignId(campaignid);
                    _sms.SendSMSNotification(contactnos,innovationmanager);
                }
            }
            return Json(model);
        }
        public JsonResult GetTypes()
        {
            return Json(_cust.GetAuditTypes(Convert.ToInt32(User.Identity.Name)));
        }



        public JsonResult GetTypesForAudit()
        {
            int createdby = obj_team.GetInnovationManagerByCampaignId(Convert.ToInt32(Session["Campaign_Id"]));
            int campid = Convert.ToInt32(Session["Campaign_Id"]);
            return Json(_cust.GetAuditTypesForAudit(campid,createdby));
        }

        public ActionResult NoAudit()
        {
            return View();
        }

        public ActionResult IdeaStatus()
        {
            

            return View(obj_ideaSub.IdeaStatus(Convert.ToInt32(User.Identity.Name)));
        }

        [HttpPost]
        public JsonResult IsTaskCreated(int manager,int idea)
        {
            return Json(obj_task.IsTaskCreated(manager,idea));
        }

        [HttpPost]
        public JsonResult IsIdeaRejectedNow(int id)
        {
            return Json(obj_ideaSub.IsIdeaRejected(id));
        }

        [HttpPost]
        public JsonResult CheckRedeem(int redeemid,int point)
        {
            return Json(objReawrd.CheckRedeem(redeemid,point,Convert.ToInt32(User.Identity.Name)));
        }
    }
}