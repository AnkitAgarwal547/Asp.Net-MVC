using DAL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using ViewModel;
using ViewModel.Innovation;

namespace BAL
{
    public class CustomerCampaignRepository : ICustomerCampaignRepository
    {
        InnovationDBEntities db = new InnovationDBEntities();
        CampaignSteps obj_step = new CampaignSteps();
        INotification obj_notify = new NotificationRepository();

        public IEnumerable<ViewModel_Campaign> getCustomerCampaign(int CustomerId)
        {
            var data = from a in db.INN_Customers_Campaingns
                       join b in db.INN_Customers_Teams on a.ManagerId equals b.Id
                       join c in db.INN_User_Campaign_IdeaSubmission on a.Id equals c.CampaignId
                       into lf
                       from lfj in lf.DefaultIfEmpty()
                       where a.CustomerId == CustomerId
                       select new ViewModel_Campaign
                       {
                           Id = a.Id,
                           EndDate = a.EndDate,
                           CreateDate = a.CreateDate,
                           Description = a.Description,
                           EmailNotification = (bool)a.EmailNotification,
                           SMSNotification = (bool)a.SMSNotification,
                           Name = a.Name,
                           ManagerId = a.ManagerId,
                           ManagerName = b.Name,
                           IsPublish = (bool)a.IsPublish,
                           StartDate = a.StartDate,
                           IsClose = (bool)a.IsClose,
                           IsDrop = (bool)a.IsDrop,
                           totalideas = a.INN_User_Campaign_IdeaSubmission.Count
                       };
            return data.Distinct().ToList();
        }

        public void PublishCampaign(int campaignId)
        {
            INN_Customers_Campaingns camp = db.INN_Customers_Campaingns.FirstOrDefault(x => x.Id == campaignId);
            if (camp != null)
            {
                camp.IsPublish = true;
                db.SaveChanges();
                obj_step.IsStepSkiped(campaignId, 7, false);




                var camp1 = (from t in db.INN_Customers_Campaingns
                             where t.Id == campaignId
                             select new { campaignname = t.Name }).FirstOrDefault();

                //------------Target User --------------------------------------

                var targetusres = (from t in db.INN_Customers_Campaigns_TargetUser
                                   join c in db.INN_Customers_Campaingns on t.CampaignId equals c.Id
                                   where c.Id == campaignId
                                   select t.userid).ToList();

                foreach (var item in targetusres)
                {
                    ViewModel_InnovationNotification objnottu = new ViewModel_InnovationNotification();
                    objnottu.notify = "1 New Campaign " + camp1.campaignname + " is created and you are mapped as Target User";
                    objnottu.entrydate = DateTime.Now;
                    objnottu.url = "/User/User/CampaignDetails?id=" + campaignId + "";
                    objnottu.userid = item.Value;
                    objnottu.campaignid = campaignId;
                    obj_notify.AddNotificationTargetUser(objnottu);
                }


                //------------Specific Regular Evaluators --------------------------------------

                var issre = (from sreteam in db.INN_Campaigns_Specific_Evaluators_Team
                             join sre in db.INN_Campaigns_Specific_Evaluators on sreteam.id equals sre.team_id
                             where sreteam.camp_id == campaignId && sre.type == "SR"
                             select new { id = sre.id, uesrid = (int)sre.userid }).OrderBy(x => x.id).ToList();

                foreach (var item in issre)
                {
                    ViewModel_InnovationNotification objnotsre = new ViewModel_InnovationNotification();
                    objnotsre.notify = "1 New Campaign " + camp1.campaignname + " is created and you are mapped as Specific Regular Evaluator for this Campaign";
                    objnotsre.entrydate = DateTime.Now;
                    objnotsre.url = "/User/User/ViewCampaignDetailsSpecific/" + campaignId + "";
                    objnotsre.campaignid = campaignId;
                    objnotsre.userid = item.uesrid;
                    obj_notify.AddNotificationSpecificRegularEvaluator(objnotsre);
                }

                //------------Specific Cheif Evaluators --------------------------------------

                var issce = (from sreteam in db.INN_Campaigns_Specific_Evaluators_Team
                             join sre in db.INN_Campaigns_Specific_Evaluators on sreteam.id equals sre.team_id
                             where sreteam.camp_id == campaignId && sre.type == "SC"
                             select new { id = sre.id, uesrid = (int)sre.userid }).OrderBy(x => x.id).ToList();

                foreach (var item in issce)
                {
                    ViewModel_InnovationNotification objnotsce = new ViewModel_InnovationNotification();

                    objnotsce.notify = "1 New Campaign " + camp1.campaignname + " is created and you are mapped as Specific Chief Evaluator for this Campaign";
                    objnotsce.entrydate = DateTime.Now;
                    objnotsce.url = "/User/User/Evaluation/"+campaignId+"";
                    objnotsce.campaignid = campaignId;
                    objnotsce.userid = item.uesrid;
                    obj_notify.AddNotificationSpecificChiefEvaluator(objnotsce);
                }

                //------------Specific Campaign Manager --------------------------------------

                var campmang = (from t in db.INN_Customers_Campaingns
                                where t.Id==campaignId select t.ManagerId).ToList();

                foreach (var item in campmang)
                {
                    ViewModel_InnovationNotification objnotcm = new ViewModel_InnovationNotification();

                    objnotcm.notify = "1 New Campaign " + camp1.campaignname + " is created and you are mapped as Campaign Manager for this Campaign";
                    objnotcm.entrydate = DateTime.Now;
                    objnotcm.url = "/User/User/Evaluation/" + campaignId + "";
                    objnotcm.campaignid = campaignId;
                    objnotcm.userid = item.Value;
                    obj_notify.AddNotificationCampaignManager(objnotcm);
                }
            }
        }

        public int SaveCapmaignInfo(ViewModel_Campaign model)
        {
            int CustomerId = 0;
            INN_Customers_Campaingns _campaign = db.INN_Customers_Campaingns.FirstOrDefault(x => x.Id == model.Id);
            if (_campaign == null)
            {
                _campaign = new INN_Customers_Campaingns();
                _campaign.CreateDate = DateTime.Now;
                _campaign.CreatedBy = Convert.ToInt32(model.CreatedBy);
                _campaign.CustomerId = model.CustomerId;
                _campaign.Description = model.Description;
                _campaign.EmailNotification = model.EmailNotification;
                _campaign.EndDate = model.EndDate;
                _campaign.IsPublish = false;
                _campaign.ManagerId = model.ManagerId;
                _campaign.Name = model.Name;
                _campaign.SMSNotification = model.SMSNotification;
                _campaign.StartDate = model.StartDate;
                _campaign.Attacment = model.Attechment;
                db.INN_Customers_Campaingns.Add(_campaign);
                db.SaveChanges();
                CustomerId = _campaign.Id;
                obj_step.IsStepSkiped(CustomerId, 1, false);
            }
            else
            {
                _campaign.ModifiedDate = DateTime.Now;
                _campaign.ModifiedBy = Convert.ToInt32(model.CreatedBy);
                _campaign.CustomerId = model.CustomerId;
                _campaign.Description = model.Description;
                _campaign.EmailNotification = model.EmailNotification;
                _campaign.EndDate = model.EndDate;
                _campaign.ManagerId = model.ManagerId;
                _campaign.Name = model.Name;
                _campaign.SMSNotification = model.SMSNotification;
                _campaign.StartDate = model.StartDate;
                _campaign.Attacment = (model.Attechment == null ? _campaign.Attacment : model.Attechment);
                db.SaveChanges();
                CustomerId = _campaign.Id;
                obj_step.IsStepSkipedUpdate(CustomerId, 1, false);
            }
            return CustomerId;
        }

        public string CloseCampaign(int campaignId)
        {
            int retValu = 0;
            string returnString = "1";


            var IsAllowIMplement = (from t in db.INN_Customers_Campaigns_Implementation
                                    where t.CampaignId == campaignId
                                    select t.AllowIdeas).FirstOrDefault();
            if (IsAllowIMplement == 1)
            {
                var isApprovedChiefEvaluater = (from t in db.INN_User_Campaign_Chief_Evaluation
                                                where t.campaign == campaignId
                                                select t.Isactive).FirstOrDefault();

                var isRagularEvaluaterRecommendToChief = (from t in db.INN_User_Campaign_Evaluation
                                                          where t.campaign == campaignId
                                                          select t.isrecommend).FirstOrDefault();


                if (isRagularEvaluaterRecommendToChief != null)
                {

                    if (!string.IsNullOrEmpty(isApprovedChiefEvaluater))
                    {
                        var totolprojectmanager = (from t in db.INN_User_CampaignManager_Assignment
                                                   join pmang in db.INN_User_ProjectMangerList on t.id equals pmang.camp_assign_id
                                                   where t.campaignid == campaignId
                                                   select t.id).ToList().Count;

                        var totalaudited = (from t in db.INN_Campaigns_Idea_AuditorApprove
                                            join idea in db.INN_User_Campaign_IdeaSubmission on t.IdeaID equals idea.id
                                            join camp in db.INN_Customers_Campaingns on idea.CampaignId equals camp.Id
                                            where camp.Id == campaignId
                                            select t.AuditorApproveID).ToList().Count;

                        var isAllowAudit = (from t in db.INN_Customers_Campaigns_Audit
                                            where t.CampaignId == campaignId
                                            select new
                                            {
                                                AuditId = t.id,
                                                campaignId = t.CampaignId,
                                                isApprove = t.is_approved
                                            }).FirstOrDefault();

                        //if (isAllowAudit.isApprove != null)
                        //{
                        //    returnString = CloseCampaignStatus(campaignId);
                        //}
                        if (totolprojectmanager == totalaudited)
                        {
                            returnString = CloseCampaignStatus(campaignId);
                        }
                        else
                        {
                            returnString = "Campaign under audit process...";
                        }
                    }
                    else
                    {
                        returnString = "Campaign under chief evaluater process...";
                    }
                }
                else
                {
                    returnString = "Campaign under regular evaluater process...";
                }


            }
            else if (IsAllowIMplement == 0)
            {
                var isApprovedChiefEvaluater = (from t in db.INN_User_Campaign_Chief_Evaluation
                                                where t.campaign == campaignId
                                                select t.Isactive).FirstOrDefault();

                var isRagularEvaluaterRecommendToChief = (from t in db.INN_User_Campaign_Evaluation
                                                          where t.campaign == campaignId
                                                          select t.isrecommend).FirstOrDefault();


                if (isRagularEvaluaterRecommendToChief != null)
                {

                    if (!string.IsNullOrEmpty(isApprovedChiefEvaluater))
                    {
                        returnString = CloseCampaignStatus(campaignId);
                    }
                    else
                    {
                        returnString = "Campaign under chief evaluater process...";
                    }
                }
                else
                {
                    returnString = "Campaign under regular evaluater process...";
                }
            }
            return returnString;
        }

        public string DropCampaign(int campaignId)
        {
            INN_Customers_Campaingns _campign = db.INN_Customers_Campaingns.FirstOrDefault(x => x.Id == campaignId);
            if (_campign != null)
            {
                _campign.IsDrop = true;
                _campign.ModifiedDate = DateTime.Now;
                db.Entry(_campign).State = EntityState.Modified;
                int res = db.SaveChanges();
                return "1";
            }
            else
            {
                return "0";
            }
        }

        public void SaveCampaignImages(ViewModel_CustomerCampaignImages model)
        {
            INN_Customers_Campaigns_Images _images = new INN_Customers_Campaigns_Images();
            _images.CampaignId = model.CampaignId;
            _images.CreateDate = DateTime.Now;
            _images.CreatedBy = model.CreatedBy;
            _images.ImagePath = model.ImagePath;
            _images.IsPrimary = model.IsPrimary.ToString();
            db.INN_Customers_Campaigns_Images.Add(_images);
            db.SaveChanges();
        }

        public void deleteCampaignInage(int campaignId, int imageId)
        {
            INN_Customers_Campaigns_Images image = db.INN_Customers_Campaigns_Images.FirstOrDefault(x => x.Id == imageId);
            if (image != null)
            {
                db.INN_Customers_Campaigns_Images.Remove(image);
                db.SaveChanges();
            }
        }

        public void SaveCampaignTargetUsers(ViewModel_Customers_Campaigns_TargetUser model)
        {
            INN_Customers_Campaigns_TargetUser _tu = db.INN_Customers_Campaigns_TargetUser.FirstOrDefault(x => x.userid == model.userid && x.CampaignId == model.CampaignId);
            if (_tu != null)
            {
                _tu.userid = model.userid;
                _tu.CampaignId = model.CampaignId;
                _tu.CreateDate = DateTime.Now;
                _tu.CreatedBy = model.CreateBy;
                db.Entry(_tu).State = EntityState.Modified;
                db.SaveChanges();
            }
            else
            {
                _tu = new INN_Customers_Campaigns_TargetUser();
                _tu.userid = model.userid;
                _tu.CampaignId = model.CampaignId;
                _tu.CreateDate = DateTime.Now;
                _tu.CreatedBy = model.CreateBy;
                db.INN_Customers_Campaigns_TargetUser.Add(_tu);
                db.SaveChanges();
            }
        }

        public void SaveSocialStage(IEnumerable<ViewModel_Customers_Campaigns_SocialStep> data, int CampaignId, int UserId)
        {


            using (DbContextTransaction transaction = db.Database.BeginTransaction())
            {
                List<INN_Customers_Campaigns_SocialStage> exitingdata = db.INN_Customers_Campaigns_SocialStage.Where(x => x.CampaignId == CampaignId).ToList();

                db.INN_Customers_Campaigns_SocialStage.RemoveRange(exitingdata);
                db.SaveChanges();

                foreach (var item in data.ToList())
                {
                    INN_Customers_Campaigns_SocialStage sStage = new INN_Customers_Campaigns_SocialStage();
                    sStage.AllowComments = item.AllowComments;
                    sStage.AllowVoting = item.AllowVoting;
                    sStage.CampaignId = CampaignId;
                    sStage.CreateDate = DateTime.Now;
                    sStage.CreatedBy = UserId;
                    sStage.TimePeriodForComments = item.TimePeriodForComments;
                    sStage.AllowAudit = item.AllowAudit;
                    db.INN_Customers_Campaigns_SocialStage.Add(sStage);
                    db.SaveChanges();
                }
                obj_step.IsStepSkipedUpdate(CampaignId, 3, false);
                transaction.Commit();
            }





            //foreach (ViewModel_Customers_Campaigns_SocialStep item in data.ToList())
            //{
            //    INN_Customers_Campaigns_SocialStage _d = db.INN_Customers_Campaigns_SocialStage.Where(x => x.CampaignId == CampaignId).FirstOrDefault();

            //    if (_d == null)
            //    {

            //        INN_Customers_Campaigns_SocialStage sStage = new INN_Customers_Campaigns_SocialStage();
            //        sStage.AllowComments = item.AllowComments;
            //        sStage.AllowVoting = item.AllowVoting;
            //        sStage.CampaignId = CampaignId;
            //        sStage.CreateDate = DateTime.Now;
            //        sStage.CreatedBy = UserId;
            //        sStage.TimePeriodForComments = item.TimePeriodForComments;
            //        sStage.AllowAudit = item.AllowAudit;
            //        db.INN_Customers_Campaigns_SocialStage.Add(sStage);
            //        db.SaveChanges();
            //    }
            //    else
            //    {
            //        _d.AllowComments = item.AllowComments;
            //        _d.AllowVoting = item.AllowVoting;
            //        _d.CampaignId = CampaignId;
            //        _d.CreateDate = DateTime.Now;
            //        _d.CreatedBy = UserId;
            //        _d.TimePeriodForComments = item.TimePeriodForComments;
            //        db.Entry(_d).State = EntityState.Modified;
            //        db.SaveChanges();
            //    }
            //    obj_step.IsStepSkipedUpdate(CampaignId, 3, false);
            //}

        }

        public void SaveImplementationStage(IEnumerable<ViewModel_Customers_Campaigns_Implementation> data, int CampaignId, int UserId)
        {
            foreach (ViewModel_Customers_Campaigns_Implementation item in data.ToList())
            {
                INN_Customers_Campaigns_Implementation _d = db.INN_Customers_Campaigns_Implementation.Where(x => x.CampaignId == CampaignId).FirstOrDefault();
                if (_d == null)
                {

                    INN_Customers_Campaigns_Implementation implementation = new INN_Customers_Campaigns_Implementation();

                    implementation.AllowIdeas = item.AllowIdeas;
                    implementation.CampaignId = CampaignId;
                    implementation.CreateDate = DateTime.Now;
                    implementation.CreatedBy = UserId;
                    implementation.repeat = (bool)item.repeat;
                    implementation.ImplementationTime = item.ImplementationTime;
                    db.INN_Customers_Campaigns_Implementation.Add(implementation);
                    db.SaveChanges();
                }
                else
                {
                    _d.AllowIdeas = item.AllowIdeas;
                    _d.CampaignId = CampaignId;
                    _d.CreateDate = DateTime.Now;
                    _d.CreatedBy = UserId;
                    _d.repeat = (bool)item.repeat;
                    _d.ImplementationTime = item.ImplementationTime;
                    db.Entry(_d).State = EntityState.Modified;
                    db.SaveChanges();
                }
                obj_step.IsStepSkipedUpdate(CampaignId, 5, false);
            }
        }

        public void SaveRewardStage(ViewModel_Customers_Campaigns_Rewards model)
        {
            INN_Customers_Campaigns_Rewards _d = db.INN_Customers_Campaigns_Rewards.Where(x => x.CampaignId == model.CampaignId).FirstOrDefault();
            if (_d == null)
            {
                INN_Customers_Campaigns_Rewards reward = new INN_Customers_Campaigns_Rewards();
                reward.AllowRewards = model.AllowRewards;
                reward.CampaignId = model.CampaignId;
                reward.CreateDate = DateTime.Now;
                reward.CreatedBy = model.CreatedBy;
                db.INN_Customers_Campaigns_Rewards.Add(reward);
                db.SaveChanges();
            }
            else
            {
                _d.AllowRewards = model.AllowRewards;
                _d.CampaignId = model.CampaignId;
                _d.CreateDate = DateTime.Now;
                _d.CreatedBy = model.CreatedBy;
                db.Entry(_d).State = EntityState.Modified;
                db.SaveChanges();
            }
            obj_step.IsStepSkipedUpdate(model.CampaignId, 7, false);
        }

        public ViewModel_Campaign getCustomerCampaigninfo(int campaignId_Id)
        {
            ViewModel_Campaign model = new ViewModel_Campaign();
            var data = (from a in db.INN_Customers_Campaingns
                        where a.Id == campaignId_Id
                        select a).FirstOrDefault();
            if (data != null)
            {
                model.Id = data.Id;
                model.CreateDate = data.CreateDate;
                model.CreatedBy = data.CreatedBy;
                model.CustomerId = data.CustomerId;
                model.Description = data.Description;
                model.EmailNotification = (bool)data.EmailNotification;
                model.EndDate = data.EndDate;
                model.ManagerId = data.ManagerId;
                model.ModifiedBy = data.ModifiedBy;
                model.ModifiedDate = data.ModifiedDate;
                model.Name = data.Name;
                model.SMSNotification = (bool)data.SMSNotification;
                model.StartDate = data.StartDate;
                model.Attechment = data.Attacment;
                model.MyCampaignPrimaryImages = getCampaignImages("1", data.Id);
                model.MyCampaignImages = getCampaignImages("0", data.Id);
                model.TargetUsers = getTargerUsers(data.Id);
            }
            return model;
        }

        private string getTargerUsers(int id)
        {
            string res = "";
            try
            {
                var data = db.INN_Customers_Campaigns_TargetUser.Where(x => x.CampaignId == id).ToList();

                foreach (var item in data)
                {
                    res = res + item.userid.ToString() + ",";
                }
                res = res.Remove(res.Length - 1, 1);

            }
            catch (Exception ex)
            {

            }
            return res;
        }

        public IEnumerable<ViewModel_CustomerCampaignImages> getCampaignImages(string IsPrimary, int CampaignId)
        {
            var data = (from a in db.INN_Customers_Campaigns_Images
                        where a.IsPrimary == IsPrimary && a.CampaignId == CampaignId
                        select new ViewModel_CustomerCampaignImages
                        {
                            Id = a.Id,
                            ImagePath = a.ImagePath,
                        }).ToList();
            return data.ToList();
        }

        public bool RemoveExistingUser(int campaignId)
        {
            bool result = false;
            try
            {
                IEnumerable<INN_Customers_Campaigns_TargetUser> data = (from t in db.INN_Customers_Campaigns_TargetUser.Where(x => x.CampaignId == campaignId) select t).ToList();
                db.INN_Customers_Campaigns_TargetUser.RemoveRange(data);
                result = true;
            }
            catch (Exception ex)
            {
                result = false;
            }
            return result;
        }

        public bool CampaignSkipStep(int camp_id, int step)
        {
            return obj_step.IsStepSkipedUpdate(camp_id, step, true);
        }

        public List<ViewModel_Campaignsteps> GetCampaignSteps(int campid)
        {
            if (campid != 0)
            {


                List<ViewModel_Campaignsteps> model = new List<ViewModel_Campaignsteps>();
                try
                {
                    var data = (from t in db.Inn_Campaign_Steps
                                where t.campaign == campid
                                select new
                                {
                                    t.step1,
                                    t.step2,
                                    t.step3,
                                    t.step4,
                                    t.step5,
                                    t.step6,
                                    t.step7,
                                }).ToList();

                    int step = 1;

                    ArrayList _list = new ArrayList();
                    _list.Add(data.FirstOrDefault().step1 == true ? "active" : (data.FirstOrDefault().step1 == false ? "done" : ""));
                    _list.Add(data.FirstOrDefault().step2 == true ? "active" : (data.FirstOrDefault().step2 == false ? "done" : ""));
                    _list.Add(data.FirstOrDefault().step3 == true ? "active" : (data.FirstOrDefault().step3 == false ? "done" : ""));
                    _list.Add(data.FirstOrDefault().step4 == true ? "active" : (data.FirstOrDefault().step4 == false ? "done" : ""));
                    _list.Add(data.FirstOrDefault().step5 == true ? "active" : (data.FirstOrDefault().step5 == false ? "done" : ""));
                    _list.Add(data.FirstOrDefault().step6 == true ? "active" : (data.FirstOrDefault().step6 == false ? "done" : ""));
                    _list.Add(data.FirstOrDefault().step7 == true ? "active" : (data.FirstOrDefault().step7 == false ? "done" : ""));


                    foreach (var item in _list)
                    {
                        ViewModel_Campaignsteps _obj = new ViewModel_Campaignsteps();
                        _obj.step = step;

                        _obj._class = item.ToString();
                        model.Add(_obj);
                        step = step + 1;
                    }
                }
                catch (Exception ex)
                {

                    throw;
                }
                return model;
            }
            else
            {
                List<ViewModel_Campaignsteps> model = new List<ViewModel_Campaignsteps>();
                return model;
            }
        }

        public bool IsPublished(int campid)
        {
            bool result = false;
            try
            {
                var data = (from t in db.INN_Customers_Campaingns
                            where t.Id == campid && t.IsPublish == true
                            select t.Id).ToList();
                if (data.Count > 0)
                {
                    result = true;
                }
            }
            catch (Exception)
            {

                throw;
            }
            return result;
        }

        public ViewModel_Campaign GetCampEndDate(int campaignId)
        {


            var data = (from t in db.INN_Customers_Campaingns
                        where t.Id == campaignId
                        select new ViewModel_Campaign
                        {
                            EndDate = t.EndDate,
                            StartDate = t.StartDate
                        }).ToList();

            ViewModel_Campaign _data = new ViewModel_Campaign();
            _data.StartDate = data.First().StartDate;
            _data.EndDate = data.First().EndDate;
            return _data;
        }

        public List<ViewModel_Customers_Campaigns_SocialStep> EditSocialStage(int innovationid)
        {
            var data = (from t in db.INN_Customers_Campaigns_SocialStage
                        where t.CampaignId == innovationid
                        select new ViewModel_Customers_Campaigns_SocialStep
                        {
                            AllowComments = (int)t.AllowComments,
                            CampaignId = (int)t.CampaignId,
                            AllowVoting = (int)t.AllowVoting,
                            Id = t.Id,
                            TimePeriodForComments = (DateTime)t.TimePeriodForComments
                        }).ToList();
            return data;
        }

        public ViewModel_Customers_Campaigns_Implementation EditImplementation(int innovationId)
        {
            var data = (from t in db.INN_Customers_Campaigns_Implementation
                        where t.CampaignId == innovationId
                        select new ViewModel_Customers_Campaigns_Implementation
                        {
                            AllowIdeas = (int)t.AllowIdeas,
                            CampaignId = (int)t.CampaignId,
                            id = t.Id,
                            ImplementationTime = (DateTime)t.ImplementationTime,
                            repeat = (bool)t.repeat
                        }).FirstOrDefault();
            return data;
        }

        public ViewModel_Customers_Campaigns_Rewards EditReward(int innovationId)
        {
            var data = (from t in db.INN_Customers_Campaigns_Rewards
                        where t.CampaignId == innovationId
                        select new ViewModel_Customers_Campaigns_Rewards
                        {
                            AllowRewards = (bool)t.AllowRewards
                        }).FirstOrDefault();
            return data;
        }

        public bool DeletePrimaryImage(int innovationid)
        {
            bool result = false;
            try
            {
                INN_Customers_Campaigns_Images data = db.INN_Customers_Campaigns_Images.Where(x => x.CampaignId == innovationid && x.IsPrimary == "1").FirstOrDefault();
                db.INN_Customers_Campaigns_Images.Remove(data);
                db.SaveChanges();
                result = true;
            }
            catch (Exception ex)
            {
                result = false;
            }
            return result;
        }
        public void DeleteCampaignAttachment(int campaignId)
        {
            INN_Customers_Campaingns _campaign = db.INN_Customers_Campaingns.FirstOrDefault(x => x.Id == campaignId);
            if (_campaign != null)
            {
                _campaign.Attacment = null;
                db.SaveChanges();
            }
        }
        public int GetIdeaCount(int innovationid)
        {
            return db.INN_User_Campaign_IdeaSubmission.Where(x => x.CampaignId == innovationid).ToList().Count();
        }
        public bool CheckCampaginname(string name, int UserId, int campaginId)
        {
            if (campaginId == 0)
            {
                INN_Customers_Campaingns _campaign = db.INN_Customers_Campaingns.FirstOrDefault(x => x.Name == name && x.CustomerId == UserId);
                if (_campaign != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                INN_Customers_Campaingns _campaign = db.INN_Customers_Campaingns.FirstOrDefault(x => x.Name == name && x.CustomerId == UserId && x.Id != campaginId);
                if (_campaign != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        private string CloseCampaignStatus(int campaignId)
        {
            INN_Customers_Campaingns _campign = db.INN_Customers_Campaingns.FirstOrDefault(x => x.Id == campaignId);
            if (_campign != null)
            {
                _campign.IsClose = true;
                _campign.ModifiedDate = DateTime.Now;
                db.Entry(_campign).State = EntityState.Modified;
                int res = db.SaveChanges();
                return "1";
            }
            else
            {
                return "0";
            }
        }

        public IEnumerable<ViewModel_Campaign> getArchivedCustomerCampaign(int CustomerId)
        {
            var data = from a in db.INN_Customers_Campaingns
                       join b in db.INN_Customers_Teams on a.ManagerId equals b.Id
                       join c in db.INN_User_Campaign_IdeaSubmission on a.Id equals c.CampaignId
                       into lf
                       from lfj in lf.DefaultIfEmpty()
                       where a.CustomerId == CustomerId && a.IsDrop == true
                       select new ViewModel_Campaign
                       {
                           Id = a.Id,
                           EndDate = a.EndDate,
                           CreateDate = a.CreateDate,
                           Description = a.Description,
                           EmailNotification = (bool)a.EmailNotification,
                           SMSNotification = (bool)a.SMSNotification,
                           Name = a.Name,
                           ManagerId = a.ManagerId,
                           ManagerName = b.Name,
                           IsPublish = (bool)a.IsPublish,
                           StartDate = a.StartDate,
                           ModifiedDate = a.ModifiedDate,
                           totalideas = a.INN_User_Campaign_IdeaSubmission.Count
                       };
            return data.Distinct().ToList();
        }

        public ViewModel_Campaign CampaignDetails(int userid, int campid)
        {
            ViewModel_Campaign model = new ViewModel_Campaign();
            List<ViewModel_CampaignIdeaSubmission> ideasubmission = new List<ViewModel_CampaignIdeaSubmission>();
            var data = (from camp in db.INN_Customers_Campaingns
                        join idea in db.INN_Customers_Campaign_IdeaSubmissionStage on camp.Id equals idea.CampaignId
                        join image in db.INN_Customers_Campaigns_Images on camp.Id equals image.CampaignId
                        join team in db.INN_Customers_Teams on camp.ManagerId equals team.Id
                        join social in db.INN_Customers_Campaigns_SocialStage on camp.Id equals social.CampaignId

                        where camp.Id == campid && image.IsPrimary == "1"
                        select new ViewModel_Campaign
                        {
                            Name = camp.Name,
                            Description = camp.Description,
                            StartDate = camp.StartDate,
                            ImagePath = image.ImagePath,
                            ManagerName = team.Name,
                            EndDate = camp.EndDate,
                            Id = camp.Id,
                            Attechment = camp.Attacment
                        }).ToList().FirstOrDefault();


            int daydiff = (int)((DateTime)data.EndDate - (DateTime)data.StartDate).TotalDays;

            if (data != null)
            {
                var idea = (from t in db.INN_User_Campaign_IdeaSubmission
                            join team in db.INN_Customers_Teams on t.CreatedBy equals team.Id
                            where t.CampaignId == campid
                            select new ViewModel_CampaignIdeaSubmission
                            {
                                Id = t.id,
                                CreatedByName = team.Name,
                                Title = t.Title,
                                description = t.description,
                                CreateDate = t.CreateDate,
                                profilepic = team.Logo
                            }).ToList();

                foreach (var item in idea)
                {
                    var likes = (from t in db.INN_User_Campaign_SocialStage_Like_Or_Dislike
                                 where t.idea == item.Id && (t.post_like != 0 && t.post_like != null)
                                 select t.id).ToList().Count;

                    var dislikes = (from t in db.INN_User_Campaign_SocialStage_Like_Or_Dislike
                                    where t.idea == item.Id && (t.post_dislike != 0 && t.post_dislike != null)
                                    select t.id).ToList().Count;

                    ViewModel_CampaignIdeaSubmission viewmodel_campaignideasubmission = new ViewModel_CampaignIdeaSubmission();

                    viewmodel_campaignideasubmission.Id = item.Id;
                    viewmodel_campaignideasubmission.CreatedByName = item.CreatedByName;
                    viewmodel_campaignideasubmission.Title = item.Title;
                    viewmodel_campaignideasubmission.description = item.description;
                    viewmodel_campaignideasubmission.CreateDate = item.CreateDate;
                    viewmodel_campaignideasubmission.likes = likes;
                    viewmodel_campaignideasubmission.dislikes = dislikes;
                    viewmodel_campaignideasubmission.profilepic = item.profilepic;
                    ideasubmission.Add(viewmodel_campaignideasubmission);
                }
            }

            model = data;
            model.daydiff = daydiff;
            model.CampaignIdeas = ideasubmission;
            return model;
        }

        public List<string> CampaignSummary_TargetUser(int campId)
        {
            List<string> CampaignDetails = new List<string>();
            string body = string.Empty;
            string subject = string.Empty;
            var data = (from t in db.INN_Customers_Campaingns
                        where t.Id == campId
                        select new ViewModel_Campaign
                        {
                            Name = t.Name,
                            StartDate = t.StartDate,
                            EndDate = t.EndDate
                        }).FirstOrDefault();

            body = "Your are mapped as Target User for Campaign - " + data.Name + ". This Campaign is published now it's will start from " + data.StartDate + " and end on " + data.EndDate + "";
            CampaignDetails.Add(body);

            subject = "A new Campaign -" + data.Name + " is published now.";

            CampaignDetails.Add(subject);

            return CampaignDetails;
        }

        public List<string> CampaignSummary_CampaignManager(int campId)
        {
            List<string> CampaignDetails = new List<string>();
            string body = string.Empty;
            string subject = string.Empty;
            var data = (from t in db.INN_Customers_Campaingns
                        where t.Id == campId
                        select new ViewModel_Campaign
                        {
                            Name = t.Name,
                            StartDate = t.StartDate,
                            EndDate = t.EndDate
                        }).FirstOrDefault();

            body = "Your are mapped as Campaign Manager for Campaign - " + data.Name + ". This Campaign is published now it's will start from " + data.StartDate + " and end on " + data.EndDate + "";
            CampaignDetails.Add(body);

            subject = "A new Campaign -" + data.Name + " is published now.";

            CampaignDetails.Add(subject);

            return CampaignDetails;
        }

        public List<string> CampaignSummary_RegularEvaluator(int cateid, int subcateid)
        {
            List<string> CampaignDetails = new List<string>();
            string body = string.Empty;
            string subject = string.Empty;

            var data_cate = (from t in db.INN_Category_Campaingns
                             where t.CategoryID == cateid
                             select new ViewModel_Campaign
                             {
                                 Name = t.Name,
                             }).FirstOrDefault();

            var data_sucate = (from t in db.INN_SubCategory_Campaingns
                               where t.SubCategoryID == subcateid
                               select new ViewModel_Campaign
                               {
                                   Name = t.Name,
                               }).FirstOrDefault();


            body = "Your are mapped as Regular Evaluator for Category - " + data_cate.Name + " and Subcategory " + data_sucate.Name;
            CampaignDetails.Add(body);

            subject = "Your are mapped as Regular Evaluator";

            CampaignDetails.Add(subject);

            return CampaignDetails;
        }

        public List<string> CampaignSummary_ChiefEvaluator(int cateid, int subcateid)
        {
            List<string> CampaignDetails = new List<string>();
            string body = string.Empty;
            string subject = string.Empty;

            var data_cate = (from t in db.INN_Category_Campaingns
                             where t.CategoryID == cateid
                             select new ViewModel_Campaign
                             {
                                 Name = t.Name,
                             }).FirstOrDefault();

            var data_sucate = (from t in db.INN_SubCategory_Campaingns
                               where t.SubCategoryID == subcateid
                               select new ViewModel_Campaign
                               {
                                   Name = t.Name,
                               }).FirstOrDefault();


            body = "Your are mapped as Chief Evaluator for Category - " + data_cate.Name + " and Subcategory " + data_sucate.Name;
            CampaignDetails.Add(body);

            subject = "Your are mapped as Chief Evaluator";

            CampaignDetails.Add(subject);

            return CampaignDetails;
        }

        public List<string> CampaignSummary_ProjectManager(int campId)
        {
            throw new NotImplementedException();
        }

        public List<string> CampaignSummary_Auditor(int campId)
        {
            throw new NotImplementedException();
        }

        public List<string> CampaignSummary_TaskPerson(int campId)
        {
            throw new NotImplementedException();
        }

        public List<string> CampaignSummary_IdeaSubmission(int campId, int userid)
        {
            List<string> content = new List<string>();
            var data = (from t in db.INN_Customers_Campaingns
                        join idea in db.INN_User_Campaign_IdeaSubmission on t.Id equals idea.CampaignId
                        join cate in db.INN_Category_Campaingns on idea.CategoryId equals cate.CategoryID
                        join subcate in db.INN_SubCategory_Campaingns on idea.SubCategoryId equals subcate.SubCategoryID
                        where t.Id == campId && idea.CreatedBy == userid
                        select new ViewModel_CampaignIdeaSubmission
                        {
                            CampaignName = t.Name,
                            CateName = cate.Name,
                            SubCateName = subcate.Name,
                            Title = idea.Title,
                            description = idea.description,
                            Id = t.Id
                        }).OrderByDescending(x => x.Id).ToList().FirstOrDefault();

            string body = "A new Idea - " + data.Title + " is submitted with Category - " + data.CateName + " and SubCategory" + data.SubCateName + " . \n Having following description \n " + data.description;

            string subject = "A new Idea for Campaign - " + data.CampaignName + " is submitted";

            content.Add(body);
            content.Add(subject);
            return content;
        }

        public List<EmailContents> CampaignSummary_RegularEvaluation(int idea, int regulareval_id)
        {
            List<EmailContents> content = new List<EmailContents>();
            var data = (from t in db.INN_User_Campaign_Evaluation
                        join eval in db.INN_Campaigns_Evaluators on t.entry_by equals eval.userid
                        join camp in db.INN_Customers_Campaingns on t.campaign equals camp.Id
                        join idea_sub in db.INN_User_Campaign_IdeaSubmission on t.idea equals idea_sub.id
                        join eval_stage in db.INN_Customers_Campaigns_EvaluationStage on camp.Id equals eval_stage.CampaignId
                        where t.idea == idea
                        select new ViewModel_Evaluation
                        {
                            CampaignName = camp.Name,
                            IdeaTitle = idea_sub.Title,
                            EvalEndDate = eval_stage.EvaluationTimePeriod.ToString(),
                            isrecommend = t.isrecommend
                        }).FirstOrDefault();
            /*RE*/
            EmailContents content_regularevaluator = new EmailContents();
            string body_re = "You have Submitted your review for Idea - " + data.IdeaTitle + " of campaign - " + data.CampaignName + " and finally you " + (data.isrecommend == true ? "Approved and forward to Chief Evaluator" : "Rejected the Idea");

            string subject_re = "Your review submitted";

            content_regularevaluator.body = body_re;
            content_regularevaluator.subject = subject_re;

            content.Add(content_regularevaluator);

            /*CM*/
            EmailContents content_campaignmanager = new EmailContents();
            string body_cm = "A Regular Evaluator Submitted his review for Idea - " + data.IdeaTitle + " of campaign - " + data.CampaignName + " and finally he " + (data.isrecommend == true ? "Approved and forward to Chief Evaluator" : "Rejected the Idea");

            string subject_cm = "A Regular Evaluator review submitted";

            content_campaignmanager.body = body_cm;
            content_campaignmanager.subject = subject_cm;

            content.Add(content_campaignmanager);

            /*IM*/
            EmailContents content_innovationmanager = new EmailContents();
            string body_im = "A Regular Evaluator Submitted his review for Idea - " + data.IdeaTitle + " of campaign - " + data.CampaignName + " and finally he " + (data.isrecommend == true ? "Approved and forward to Chief Evaluator" : "Rejected the Idea");

            string subject_im = "A Regular Evaluator review submitted";

            content_innovationmanager.body = body_im;
            content_innovationmanager.subject = subject_im;

            content.Add(content_innovationmanager);

            /*CE*/
            EmailContents content_chiefevaluator = new EmailContents();
            if (data.isrecommend == true)
            {
                string body_ce = "Regular Evaluation done for Idea - " + data.IdeaTitle + " of campaign - " + data.CampaignName + " and finally he " + (data.isrecommend == true ? "Approved and forward to Chief Evaluator" : "Rejected the Idea") + " Last date of evaluation is " + data.EvalEndDate;

                string subject_ce = "A Regular Evaluator review submitted";

                content_chiefevaluator.body = body_ce;
                content_chiefevaluator.subject = subject_ce;
            }
            content.Add(content_chiefevaluator);

            /*TU*/
            EmailContents content_targetuser = new EmailContents();
            string body_tu = "Your Idea - " + data.IdeaTitle + " is " + (data.isrecommend == true ? "Approved and forward to Chief Evaluator" : "Rejected by Regular Evaluator");

            string subject_tu = "Idea Evaluated by Regular Evaluator";

            content_targetuser.body = body_tu;
            content_targetuser.subject = subject_tu;

            content.Add(content_targetuser);

            return content;
        }

        public List<EmailContents> CampaignSummary_ChiefEvaluation(int idea, int chiefeval_id)
        {
            List<EmailContents> content = new List<EmailContents>();
            var data = (from t in db.INN_User_Campaign_Chief_Evaluation
                        join eval in db.INN_Campaigns_Evaluators on t.created_by equals eval.userid
                        join camp in db.INN_Customers_Campaingns on t.campaign equals camp.Id
                        join idea_sub in db.INN_User_Campaign_IdeaSubmission on t.idea equals idea_sub.id
                        join eval_stage in db.INN_Customers_Campaigns_EvaluationStage on camp.Id equals eval_stage.CampaignId
                        where t.created_by == chiefeval_id && t.idea == idea
                        select new ViewModel_ChiefEvaluation
                        {
                            CampaignName = camp.Name,
                            IdeaTitle = idea_sub.Title,
                            EvalEndDate = eval_stage.EvaluationTimePeriod.ToString(),
                            IsApproved = t.Isactive
                        }).FirstOrDefault();
            /*RE*/
            EmailContents content_regularevaluator = new EmailContents();
            string body_re = "Your regular evaluation review is evaluated by Chief Evaluator for Idea - " + data.IdeaTitle + " of campaign - " + data.CampaignName + " and finally he " + (data.IsApproved == "A" ? "approved your review" : "rejected your review");

            string subject_re = "Your regular evaluation reviewed by Chief Evaluator";

            content_regularevaluator.body = body_re;
            content_regularevaluator.subject = subject_re;

            content.Add(content_regularevaluator);

            /*CM*/
            EmailContents content_campaignmanager = new EmailContents();
            string body_cm = "A Chief Evaluator Submitted his review for Idea - " + data.IdeaTitle + " of campaign - " + data.CampaignName + " and finally he " + (data.IsApproved == "A" ? "Approved the idea " : "rejected the idea");

            string subject_cm = "A Chief Evaluator Submitted his review.";

            content_campaignmanager.body = body_cm;
            content_campaignmanager.subject = subject_cm;

            content.Add(content_campaignmanager);

            /*IM*/
            EmailContents content_innovationmanager = new EmailContents();
            string body_im = "A Chief Evaluator Submitted his review for Idea - " + data.IdeaTitle + " of campaign - " + data.CampaignName + " and finally he " + (data.IsApproved == "A" ? "Approved the idea " : "rejected the idea");

            string subject_im = "A Chief Evaluator Submitted his review.";

            content_innovationmanager.body = body_im;
            content_innovationmanager.subject = subject_im;

            content.Add(content_innovationmanager);

            /*CE*/
            EmailContents content_chiefevaluator = new EmailContents();
            string body_ce = "You reviewed a regular evaluator's review for Idea - " + data.IdeaTitle + " of campaign - " + data.CampaignName + " and finally you " + (data.IsApproved == "A" ? "Approved the Idea " : "Rejected the Idea");

            string subject_ce = "You reviewed a regular evaluator's review";

            content_chiefevaluator.body = body_ce;
            content_chiefevaluator.subject = subject_ce;
            content.Add(content_chiefevaluator);

            /*TU*/
            EmailContents content_targetuser = new EmailContents();
            string body_tu = "Your Idea - " + data.IdeaTitle + " is " + (data.IsApproved == "A" ? "Approved by  Chief Evaluator" : " Rejected by Chief Evaluator");

            string subject_tu = "Idea Evaluated by Chief Evaluator";

            content_targetuser.body = body_tu;
            content_targetuser.subject = subject_tu;

            content.Add(content_targetuser);

            return content;
        }

        public List<EmailContents> CampaignSummary_Implementation(int idea, int camp_mang_id)
        {
            List<EmailContents> content = new List<EmailContents>();
            var data = (from camp in db.INN_Customers_Campaingns
                        join idea_sub in db.INN_User_Campaign_IdeaSubmission on camp.Id equals idea_sub.CampaignId
                        join cmang in db.INN_User_CampaignManager_Assignment on idea_sub.id equals cmang.idea
                        join pmang in db.INN_User_ProjectMangerList on cmang.id equals pmang.camp_assign_id
                        //join pm in db.INN_User_Campaign_ProjectManager on camp.Id equals pm.campaign
                        where idea_sub.id == idea && camp.ManagerId == camp_mang_id
                        select new ViewModel_ProjectManager
                        {
                            CampaignName = camp.Name,
                            IdeaTitle = idea_sub.Title,
                            Implementdate = pmang.startdate
                        }).FirstOrDefault();
            /*PM*/
            EmailContents content_regularevaluator = new EmailContents();
            string body_re = "You are mapped as Project Manager for Idea - " + data.IdeaTitle + " of campaign - " + data.CampaignName + " and your target date is -  " + data.Implementdate;

            string subject_re = "You are mapped as Project Manager";

            content_regularevaluator.body = body_re;
            content_regularevaluator.subject = subject_re;

            content.Add(content_regularevaluator);

            /*AUDITOR*/
            EmailContents content_campaignmanager = new EmailContents();
            string body_cm = "You are mapped as Auditor for Idea - " + data.IdeaTitle + " of campaign - " + data.CampaignName + " and your target date is -  " + data.Implementdate;

            string subject_cm = "You are mapped as Auditor";

            content_campaignmanager.body = body_cm;
            content_campaignmanager.subject = subject_cm;

            content.Add(content_campaignmanager);

            /*IM*/
            EmailContents content_innovationmanager = new EmailContents();
            string body_im = "Project Manager and Auditor assigned for Idea - " + data.IdeaTitle + " of campaign - " + data.CampaignName + " having target date - " + data.Implementdate;

            string subject_im = "Project Manager and Auditor assigned for Idea";

            content_innovationmanager.body = body_im;
            content_innovationmanager.subject = subject_im;

            content.Add(content_innovationmanager);



            /*CM*/
            EmailContents content_targetuser = new EmailContents();
            string body_tu = "Project Manager and Auditor assigned for Idea - " + data.IdeaTitle + " of campaign - " + data.CampaignName + " having target date - " + data.Implementdate;

            string subject_tu = "Project Manager and Auditor assigned for Idea";

            content_targetuser.body = body_tu;
            content_targetuser.subject = subject_tu;

            content.Add(content_targetuser);

            return content;
        }

        public List<EmailContents> CampaignSummary_TaskCreation(int idea, int proj_mang_id)
        {
            List<EmailContents> content = new List<EmailContents>();
            var data = (from camp in db.INN_Customers_Campaingns
                        join idea_sub in db.INN_User_Campaign_IdeaSubmission on camp.Id equals idea_sub.CampaignId
                        // join pm in db.INN_User_Campaign_ProjectManager on camp.Id equals pm.campaign
                        join cmang in db.INN_User_CampaignManager_Assignment on idea_sub.id equals cmang.idea
                        join pmang in db.INN_User_ProjectMangerList on cmang.id equals pmang.camp_assign_id
                        join tc in db.INN_User_ProjectManager_Task_Creation on cmang.idea equals tc.idea
                        where idea_sub.id == idea && pmang.userid == proj_mang_id
                        select new ViewModel_TaskCreationExten
                        {
                            CampaignName = camp.Name,
                            IdeaTitle = idea_sub.Title,
                            target_date = tc.target_date.ToString(),
                        }).FirstOrDefault();
            /*PM*/
            EmailContents content_regularevaluator = new EmailContents();
            string body_re = "New Task(s) created for Idea - " + data.IdeaTitle + " of campaign - " + data.CampaignName + " having target date is -  " + data.target_date;

            string subject_re = "New Task(s) created";

            content_regularevaluator.body = body_re;
            content_regularevaluator.subject = subject_re;

            content.Add(content_regularevaluator);

            /*TP*/
            EmailContents content_campaignmanager = new EmailContents();
            string body_cm = "New Task assigned for Idea - " + data.IdeaTitle + " of campaign - " + data.CampaignName + " having target date is -  " + data.target_date + ", Visit My Task of Innovation Portal";

            string subject_cm = "New Task assigned.";

            content_campaignmanager.body = body_cm;
            content_campaignmanager.subject = subject_cm;

            content.Add(content_campaignmanager);

            /*IM*/
            EmailContents content_innovationmanager = new EmailContents();
            string body_im = "New Task(s) created for Idea - " + data.IdeaTitle + " of campaign - " + data.CampaignName + " having target date is -  " + data.target_date;

            string subject_im = "New Task(s) created for Idea.";

            content_innovationmanager.body = body_im;
            content_innovationmanager.subject = subject_im;

            content.Add(content_innovationmanager);



            /*CM*/
            EmailContents content_targetuser = new EmailContents();
            string body_tu = "New Task(s) created for Idea - " + data.IdeaTitle + " of campaign - " + data.CampaignName + " having target date is -  " + data.target_date;

            string subject_tu = "New Task(s) created for Idea.";

            content_targetuser.body = body_tu;
            content_targetuser.subject = subject_tu;

            content.Add(content_targetuser);

            return content;
        }

        public List<EmailContents> CampaignSummary_TaskClosed(int idea, int proj_mang_id)
        {
            List<EmailContents> content = new List<EmailContents>();
            var data = (from camp in db.INN_Customers_Campaingns
                        join idea_sub in db.INN_User_Campaign_IdeaSubmission on camp.Id equals idea_sub.CampaignId
                        //join pm in db.INN_User_Campaign_ProjectManager on camp.Id equals pm.campaign
                        join cmang in db.INN_User_CampaignManager_Assignment on idea_sub.id equals cmang.idea
                        join pmang in db.INN_User_ProjectMangerList on cmang.id equals pmang.camp_assign_id
                        join tc in db.INN_User_ProjectManager_Task_Creation on cmang.idea equals tc.idea
                        where idea_sub.id == idea && pmang.userid == proj_mang_id
                        select new ViewModel_TaskCreationExten
                        {
                            CampaignName = camp.Name,
                            IdeaTitle = idea_sub.Title,
                            target_date = tc.target_date.ToString(),
                        }).FirstOrDefault();
            /*PM*/
            EmailContents content_regularevaluator = new EmailContents();
            string body_re = "Task closed for Idea - " + data.IdeaTitle + " of campaign - " + data.CampaignName + " having target date is -  " + data.target_date;

            string subject_re = "One Task closed";

            content_regularevaluator.body = body_re;
            content_regularevaluator.subject = subject_re;

            content.Add(content_regularevaluator);

            /*TP*/
            EmailContents content_campaignmanager = new EmailContents();
            string body_cm = "One Task closed for Idea - " + data.IdeaTitle + " of campaign - " + data.CampaignName + " having target date is -  " + data.target_date + ", Visit My Task of Innovation Portal";

            string subject_cm = "One Task closed.";

            content_campaignmanager.body = body_cm;
            content_campaignmanager.subject = subject_cm;

            content.Add(content_campaignmanager);

            /*IM*/
            EmailContents content_innovationmanager = new EmailContents();
            string body_im = "One Task closed for Idea - " + data.IdeaTitle + " of campaign - " + data.CampaignName + " having target date is -  " + data.target_date;

            string subject_im = "One Task closed.";

            content_innovationmanager.body = body_im;
            content_innovationmanager.subject = subject_im;

            content.Add(content_innovationmanager);



            /*CM*/
            EmailContents content_targetuser = new EmailContents();
            string body_tu = "One Task closed for Idea - " + data.IdeaTitle + " of campaign - " + data.CampaignName + " having target date is -  " + data.target_date;

            string subject_tu = "One Task closed.";

            content_targetuser.body = body_tu;
            content_targetuser.subject = subject_tu;

            content.Add(content_targetuser);

            /*AUDITOR*/
            EmailContents content_auditor = new EmailContents();
            string body_au = "One Task closed for Idea - " + data.IdeaTitle + " of campaign - " + data.CampaignName + " having target date is -  " + data.target_date + ", Visit My Task of Innovation Portal";

            string subject_au = "One Task closed.";

            content_auditor.body = body_au;
            content_auditor.subject = subject_au;

            content.Add(content_auditor);

            return content;
        }

        //Get:/Campaings Report
        public IEnumerable<ViewModel_Campaign> Get_UserCampaignsReport(int CustomerId)
        {

            IEnumerable<ViewModel_Campaign> data = (from Campaigns in db.INN_Customers_Campaingns
                                                    where Campaigns.CustomerId == CustomerId
                                                    select new ViewModel_Campaign
                                                    {
                                                        Id = Campaigns.Id,
                                                        CustomerId = Campaigns.CustomerId,
                                                        Name = Campaigns.Name,
                                                        IsClose = Campaigns.IsClose,
                                                        IsDrop = Campaigns.IsDrop,
                                                        StartDate = Campaigns.StartDate,
                                                        TotalNumberOfCampaign = db.INN_Customers_Campaingns.Count(x => x.CustomerId == CustomerId),
                                                        EndDate = Campaigns.EndDate,
                                                        CreateDate = Campaigns.CreateDate,
                                                        SubmittedIdea = Campaigns.INN_User_Campaign_IdeaSubmission.Count(x => x.CampaignId == Campaigns.Id),
                                                        ApprovedIdea = Campaigns.INN_User_Campaign_Chief_Evaluation.Distinct().Count(x => x.campaign == Campaigns.Id && x.Isactive == "A"),
                                                        RejectedIdea = Campaigns.INN_User_Campaign_Chief_Evaluation.Distinct().Count(x => x.campaign == Campaigns.Id && x.Isactive == "R"),
                                                    }).ToList();

            foreach (var item in data)
            {
                item.ImplementedIdea = Get_ImplementedIdea(item.Id);
            }

            foreach (var item in data)
            {
                item.AuditingIdea = Get_AuditingIdea(item.Id);
            }

            return data;
        }

        //Get:/Total Implemented Idea by CampaignId
        public int Get_ImplementedIdea(int CampaignId)
        {
            int totalimplemented = 0;
            var Ideas = db.INN_User_Campaign_IdeaSubmission.Where(x => x.CampaignId == CampaignId).ToList();
            foreach (var item in Ideas)
            {
                var dd = db.INN_User_ProjectManager_Task_Creation.Where(x => x.campaign == CampaignId && x.idea == item.id).ToList();
                if (dd.Count > 0)
                {
                    bool result = dd.Any(x => x.task_completion_date == null);
                    if (result != true)
                    {
                        totalimplemented = totalimplemented + 1;
                    }
                }
            }
            return totalimplemented;
        }

        //Get:/Total Auditing Idea by CampaignId
        public int Get_AuditingIdea(int CampaignId)
        {
            int AudtingIdea = 0;
            var Ideas = db.INN_User_Campaign_IdeaSubmission.Where(x => x.CampaignId == CampaignId).ToList();
            foreach (var item in Ideas)
            {
                var dd = db.INN_Campaigns_Idea_AuditorApprove.Where(x => x.IdeaID == item.id && x.IsApprove == 1).FirstOrDefault();
                if (dd != null)
                {
                    AudtingIdea = AudtingIdea + 1;
                }
            }
            return AudtingIdea;
        }

        //Get:/Active Campaigns
        public IEnumerable<ViewModel_Campaign> Get_Campaigns()
        {
            var data = (from t in db.INN_Customers_Campaingns
                        where t.IsDrop == null && t.IsClose == null && t.IsPublish == true
                        select new ViewModel_Campaign
                        {
                            Id = t.Id,
                            Name = t.Name
                        }).ToList();
            return data;

        }

        public IEnumerable<ViewModel_Users_by_Campaign> GetUsers_By_Campaign(int CampaignId)
        {
            List<ViewModel_Users_by_Campaign> model = new List<ViewModel_Users_by_Campaign>();
            try
            {
                #region Regular Eavluator
                var data_regularevaluator = (from ev in db.INN_Campaigns_Evaluators
                                             join idea in db.INN_User_Campaign_IdeaSubmission on ev.category equals idea.CategoryId
                                             join idea2 in db.INN_User_Campaign_IdeaSubmission on ev.subcategory equals idea2.SubCategoryId
                                             join ct in db.INN_Customers_Teams on ev.userid equals ct.Id
                                             join camp in db.INN_Customers_Campaingns on idea.CampaignId equals camp.Id
                                             where idea.CampaignId == CampaignId && ev.type == "R"
                                             select new ViewModel_Users_by_Campaign
                                             {
                                                 CampaignId = (int)idea.CampaignId,
                                                 UserId = ct.Id,
                                                 UserName = ct.Name,
                                                 Role = "Regular Evaluator",
                                                 CampaignName = camp.Name,
                                                 StartDate = (DateTime)camp.StartDate,
                                                 EndDate = (DateTime)camp.EndDate,
                                             }).Distinct().ToList();





                foreach (var item in data_regularevaluator)
                {
                    ViewModel_Users_by_Campaign users_ByCampaign = new ViewModel_Users_by_Campaign();
                    users_ByCampaign.CampaignId = item.CampaignId;
                    users_ByCampaign.UserId = item.UserId;
                    users_ByCampaign.StartDate = item.StartDate;
                    users_ByCampaign.EndDate = item.EndDate;
                    users_ByCampaign.UserName = item.UserName;
                    users_ByCampaign.Role = item.Role;
                    model.Add(users_ByCampaign);
                }

                #endregion

                #region Chief Evaluator
                var data_chiefevaluator = (from ev in db.INN_Campaigns_Evaluators
                                           join idea in db.INN_User_Campaign_IdeaSubmission on ev.category equals idea.CategoryId
                                           join idea2 in db.INN_User_Campaign_IdeaSubmission on ev.subcategory equals idea2.SubCategoryId
                                           join ct in db.INN_Customers_Teams on ev.userid equals ct.Id
                                           join camp in db.INN_Customers_Campaingns on idea.CampaignId equals camp.Id
                                           where idea.CampaignId == CampaignId && ev.type == "C"
                                           select new ViewModel_Users_by_Campaign
                                           {
                                               CampaignId = (int)idea.CampaignId,
                                               UserId = ct.Id,
                                               UserName = ct.Name,
                                               Role = "Cheif Evaluator",
                                               CampaignName = camp.Name,
                                               StartDate = (DateTime)camp.StartDate,
                                               EndDate = (DateTime)camp.EndDate,
                                           }).Distinct().ToList();

                foreach (var item in data_chiefevaluator)
                {
                    ViewModel_Users_by_Campaign users = new ViewModel_Users_by_Campaign();
                    users.CampaignId = item.CampaignId;
                    users.StartDate = item.StartDate;
                    users.EndDate = item.EndDate;
                    users.Role = item.Role;
                    users.UserName = item.UserName;
                    users.UserId = item.UserId;
                    model.Add(users);
                }

                #endregion

                #region Campaign Manager

                var data_campaignmanager = (from t in db.INN_Customers_Campaingns
                                            join ct in db.INN_Customers_Teams on t.ManagerId equals ct.Id
                                            where t.Id == CampaignId
                                            select new ViewModel_Users_by_Campaign
                                            {
                                                CampaignId = t.Id,
                                                CampaignName = t.Name,
                                                StartDate = (DateTime)t.StartDate,
                                                EndDate = (DateTime)t.EndDate,
                                                UserName = ct.Name,
                                                Role = "Campaign Manager",
                                                UserId = (int)t.ManagerId
                                            }).ToList();


                foreach (var item in data_campaignmanager)
                {
                    ViewModel_Users_by_Campaign users = new ViewModel_Users_by_Campaign();
                    users.CampaignId = item.CampaignId;
                    users.UserId = item.UserId;
                    users.UserName = item.UserName;
                    users.StartDate = item.StartDate;
                    users.EndDate = item.StartDate;
                    users.Role = item.Role;
                    users.CampaignName = item.CampaignName;
                    model.Add(users);
                }
                #endregion

                #region Project Manager
                var data_projectmanager = (from c in db.INN_Customers_Campaingns
                                           join pm in db.INN_User_Campaign_ProjectManager on c.Id equals pm.campaign
                                           join ct in db.INN_Customers_Teams on pm.manager equals ct.Id
                                           where c.Id == CampaignId
                                           select new ViewModel_Users_by_Campaign
                                           {
                                               CampaignId = c.Id,
                                               CampaignName = c.Name,
                                               UserId = ct.Id,
                                               UserName = ct.Name,
                                               StartDate = (DateTime)c.StartDate,
                                               EndDate = (DateTime)c.EndDate,
                                               Role = "Project Manager"
                                           }).ToList();


                foreach (var item in data_projectmanager)
                {
                    ViewModel_Users_by_Campaign users = new ViewModel_Users_by_Campaign();
                    users.CampaignId = item.CampaignId;
                    users.UserId = item.UserId;
                    users.StartDate = item.StartDate;
                    users.EndDate = item.EndDate;
                    users.UserName = item.UserName;
                    users.Role = item.Role;
                    users.CampaignName = item.CampaignName;
                    model.Add(users);
                }
                #endregion

                #region Task Member
                var data_taskmember = (from c in db.INN_Customers_Campaingns
                                       join tc in db.INN_User_ProjectManager_Task_Creation on c.Id equals tc.campaign
                                       join tp in db.INN_User_Task_Person on tc.id equals tp.task_id
                                       join ct in db.INN_Customers_Teams on tp.task_person equals ct.Id
                                       where c.Id == CampaignId
                                       select new ViewModel_Users_by_Campaign
                                       {
                                           CampaignId = c.Id,
                                           CampaignName = c.Name,
                                           StartDate = (DateTime)c.StartDate,
                                           EndDate = (DateTime)c.EndDate,
                                           UserName = ct.Name,
                                           Role = "Task Member",
                                           UserId = (int)ct.Id
                                       }).Distinct().ToList();


                foreach (var item in data_taskmember)
                {
                    ViewModel_Users_by_Campaign users = new ViewModel_Users_by_Campaign();
                    users.CampaignId = item.CampaignId;
                    users.CampaignName = item.CampaignName;
                    users.UserId = item.UserId;
                    users.UserName = item.UserName;
                    users.StartDate = item.StartDate;
                    users.EndDate = item.EndDate;
                    users.Role = item.Role;
                    model.Add(users);
                }
                #endregion

                #region Auditor
                var data_Auditor = (from c in db.INN_Customers_Campaingns
                                    join pm in db.INN_User_Campaign_ProjectManager on c.Id equals pm.campaign
                                    join ct in db.INN_Customers_Teams on pm.auditor equals ct.Id
                                    where c.Id == CampaignId
                                    select new ViewModel_Users_by_Campaign
                                    {
                                        CampaignId = c.Id,
                                        CampaignName = c.Name,
                                        StartDate = (DateTime)c.StartDate,
                                        EndDate = (DateTime)c.EndDate,
                                        UserName = ct.Name,
                                        Role = "Auditor",
                                        UserId = (int)ct.Id,
                                    }).ToList();


                foreach (var item in data_Auditor)
                {
                    ViewModel_Users_by_Campaign users = new ViewModel_Users_by_Campaign();
                    users.CampaignId = item.CampaignId;
                    users.StartDate = item.StartDate;
                    users.EndDate = item.EndDate;
                    users.CampaignName = item.CampaignName;
                    users.UserName = item.UserName;
                    users.UserId = item.UserId;
                    users.Role = item.Role;
                    model.Add(users);
                }
                #endregion

            }
            catch (Exception ex)
            {

                throw;
            }
            return model;
        }

        public IEnumerable<ViewModel_AuditType> GetAuditTypes(int customerid)
        {
            List<ViewModel_AuditType> model = new List<ViewModel_AuditType>();
            try
            {
                var data = (from t in db.INN_AuditType
                            where t.customerid == customerid
                            select new ViewModel_AuditType
                            {
                                id = t.id,
                                typename = t.name
                            }).OrderBy(x => x.typename).ToList();
                model = data;
            }
            catch (Exception ex)
            {

                throw;
            }
            return model;
        }

        public IEnumerable<ViewModel_AuditType> GetAuditTypesForAudit(int campid, int createdby)
        {
            List<ViewModel_AuditType> model = new List<ViewModel_AuditType>();
            try
            {
                var data = (from aud in db.INN_Customers_Campaigns_Audit
                            join aud_type in db.INN_Customers_Campaigns_Audit_Types on aud.id equals aud_type.auditid
                            join type in db.INN_AuditType on aud_type.type equals type.id
                            where aud.CreatedBy == createdby && aud.CampaignId == campid
                            select new ViewModel_AuditType
                            {
                                id = type.id,
                                typename = type.name
                            }).OrderBy(x => x.typename).ToList();
                model = data;
            }
            catch (Exception ex)
            {

                throw;
            }
            return model;
        }

        public ViewModel_Campaign Get_DepartmentwiseIdea(int depid, int campid, string _date)
        {
            int month = 0;
            int year = 0;
            int days = 0;
            DateTime? datefrom = null;
            DateTime? dateto = null;
            if (!string.IsNullOrEmpty(_date))
            {
                month = Convert.ToInt32(_date.Split('/')[0]);
                year = Convert.ToInt32(_date.Split('/')[1]);
                days = DateTime.DaysInMonth(year, month);
                datefrom = new DateTime(year, month, 1);
                dateto = new DateTime(year, month, days);
            }


            ViewModel_Campaign model = new ViewModel_Campaign();


            var totalidea_data = (from t1 in db.INN_User_Campaign_IdeaSubmission
                                  join t3 in db.INN_Customers_Teams on t1.CreatedBy equals t3.Id
                                  join t4 in db.INN_TeamDepartment on t3.Department equals t4.DepartmentID
                                  where t4.DepartmentID == depid
                                  select new { t1.id, t1.CampaignId }).ToList();

            if (campid > 0)
            {
                totalidea_data = totalidea_data.Where(x => x.CampaignId == campid).ToList();
            }


            var totalideaapproved_data = (from t1 in db.INN_User_Campaign_IdeaSubmission
                                          join t3 in db.INN_Customers_Teams on t1.CreatedBy equals t3.Id
                                          join t4 in db.INN_TeamDepartment on t3.Department equals t4.DepartmentID
                                          join t5 in db.INN_Campaigns_Idea_AuditorApprove on t1.id equals t5.IdeaID
                                          where t4.DepartmentID == depid && t5.IsApprove == 1
                                          select new { t1.id, t1.CampaignId, t5.CreatedOn }).ToList();

            if (campid > 0)
            {
                totalideaapproved_data = totalideaapproved_data.Where(x => x.CampaignId == campid).ToList();
            }
            if (!string.IsNullOrEmpty(_date))
            {
                totalideaapproved_data = totalideaapproved_data.Where(x => x.CreatedOn >= datefrom && x.CreatedOn <= dateto).ToList();
            }

            var totalidearejected_data = (from t1 in db.INN_User_Campaign_IdeaSubmission
                                          join t3 in db.INN_Customers_Teams on t1.CreatedBy equals t3.Id
                                          join t4 in db.INN_TeamDepartment on t3.Department equals t4.DepartmentID
                                          join t5 in db.INN_Campaigns_Idea_AuditorApprove on t1.id equals t5.IdeaID
                                          where t4.DepartmentID == depid && t5.IsApprove != 1
                                          select new { t1.id, t1.CampaignId, t5.CreatedOn }).ToList();

            if (campid > 0)
            {
                totalidearejected_data = totalidearejected_data.Where(x => x.CampaignId == campid).ToList();
            }
            if (!string.IsNullOrEmpty(_date))
            {
                totalidearejected_data = totalidearejected_data.Where(x => x.CreatedOn >= datefrom && x.CreatedOn <= dateto).ToList();
            }

            var totalideawithoutaudit_data = totalidea_data.Count() - totalideaapproved_data.Count() - totalidearejected_data.Count();


            model._totalidea = totalidea_data.Count();
            model._approvedidea = totalideaapproved_data.Count();
            model._rejectedidea = totalidearejected_data.Count();
            model._noaction = totalideawithoutaudit_data;

            return model;
        }

        public List<ViewModel_Campaign> Get_CampaignsForType(int depid)
        {
            var data = (from t1 in db.INN_User_Campaign_IdeaSubmission
                        join t2 in db.INN_Customers_Campaingns on t1.CampaignId equals t2.Id
                        join t3 in db.INN_Customers_Teams on t1.CreatedBy equals t3.Id
                        join t4 in db.INN_TeamDepartment on t3.Department equals t4.DepartmentID
                        where t4.DepartmentID == depid
                        select new ViewModel_Campaign
                        {
                            Name = t2.Name,
                            Id = t2.Id
                        }).Distinct().ToList();
            return data;
        }

        public bool IsEmailNotificationAllowed(int campid)
        {
            bool result = false;

            var data = (from t in db.INN_Customers_Campaingns
                        where t.Id == campid && t.EmailNotification == true
                        select t.Id).FirstOrDefault();
            if (data > 0)
            {
                result = true;
            }

            return result;
        }

        public bool IsSMSNotificationAllowed(int campid)
        {
            bool result = false;

            var data = (from t in db.INN_Customers_Campaingns
                        where t.Id == campid && t.SMSNotification == true
                        select t.Id).FirstOrDefault();
            if (data > 0)
            {
                result = true;
            }

            return result;
        }

        public ViewModel_Campaign GetIdeaEndDate(int campaignId)
        {
            var data = (from t in db.INN_Customers_Campaingns
                        join idea in db.INN_Customers_Campaign_IdeaSubmissionStage on t.Id equals idea.CampaignId
                        where t.Id == campaignId
                        select new ViewModel_Campaign
                        {
                            EndDate = t.EndDate,
                            StartDate = idea.TimePeriod
                        }).ToList();

            ViewModel_Campaign _data = new ViewModel_Campaign();
            _data.StartDate = data.First().StartDate;
            _data.EndDate = data.First().EndDate;
            return _data;
        }

        public ViewModel_Campaign GetAllowedSocialStageEndDate(int campaignId)
        {
            var data = (from t in db.INN_Customers_Campaingns
                        join idea in db.INN_Customers_Campaign_IdeaSubmissionStage on t.Id equals idea.CampaignId
                        join social in db.INN_Customers_Campaigns_SocialStage on t.Id equals social.CampaignId
                        where t.Id == campaignId
                        select new ViewModel_Campaign
                        {
                            StartDate = idea.TimePeriod,
                            EndDate = social.TimePeriodForComments
                        }).ToList();

            ViewModel_Campaign _data = new ViewModel_Campaign();
            _data.StartDate = data.First().StartDate;
            _data.EndDate = data.First().EndDate;
            return _data;
        }
        public ViewModel_Campaign GetAllowedRegularEvaluatorEndDate(int campaignId)
        {
            var data = (from t in db.INN_Customers_Campaingns
                        join eval in db.INN_Customers_Campaigns_EvaluationStage on t.Id equals eval.CampaignId
                        join social in db.INN_Customers_Campaigns_SocialStage on t.Id equals social.CampaignId
                        where t.Id == campaignId
                        select new ViewModel_Campaign
                        {
                            StartDate = social.TimePeriodForComments,
                            EndDate = eval.EvaluationTimePeriod,
                            Id = social.Id
                        }).OrderByDescending(x => x.Id).First();

            ViewModel_Campaign _data = new ViewModel_Campaign();
            _data.StartDate = data.StartDate;
            _data.EndDate = data.EndDate;
            return _data;
        }

        public ViewModel_Campaign GetAllowedSpecificEvaluationEndDate(int campaignId)
        {
            var data = (from t in db.INN_Customers_Campaingns
                        join eval in db.INN_Customers_Campaigns_EvaluationStage on t.Id equals eval.CampaignId
                        where t.Id == campaignId
                        select new ViewModel_Campaign
                        {
                            StartDate = eval.EvaluationTimePeriod,
                            EndDate = eval.ChiefEvaluationTimePeriod
                        }).ToList();

            ViewModel_Campaign _data = new ViewModel_Campaign();
            _data.StartDate = data.First().StartDate;
            _data.EndDate = data.First().EndDate;
            return _data;
        }

        public ViewModel_Campaign GetSocialStageEndDate(int campaignId)
        {
            ViewModel_Campaign _data = new ViewModel_Campaign();
            var data = (from t in db.INN_Customers_Campaingns
                        join soci in db.INN_Customers_Campaigns_SocialStage on t.Id equals soci.CampaignId
                        where t.Id == campaignId
                        select new ViewModel_Campaign
                        {
                            EndDate = t.EndDate,
                            StartDate = soci.TimePeriodForComments,
                            Id = soci.Id
                        }).ToList().OrderByDescending(x => x.Id).ToList();

            if (data.Count == 0)
            {
                ViewModel_Campaign data1 = GetIdeaEndDate(campaignId);
                _data.StartDate = data1.StartDate;
                _data.EndDate = data1.EndDate;
            }
            else
            {
                _data.StartDate = data.First().StartDate;
                _data.EndDate = data.First().EndDate;
            }


            return _data;
        }

        public ViewModel_Campaign GetEvaluationEndDate(int campaignId)
        {
            ViewModel_Campaign _data = new ViewModel_Campaign();
            var data = (from t in db.INN_Customers_Campaingns
                        join eval in db.INN_Customers_Campaigns_EvaluationStage on t.Id equals eval.CampaignId
                        where t.Id == campaignId
                        select new ViewModel_Campaign
                        {
                            EndDate = t.EndDate,
                            StartDate = eval.ChiefEvaluationTimePeriod
                        }).ToList();
            if (data.Count == 0)
            {
                ViewModel_Campaign data1 = GetSocialStageEndDate(campaignId);

                _data.StartDate = data1.StartDate;
                _data.EndDate = data1.EndDate;
            }
            else
            {
                _data.StartDate = data.First().StartDate;
                _data.EndDate = data.First().EndDate;
            }


            return _data;
        }

        public ViewModel_Campaign GetImplementationEndDate(int campaignId)
        {
            var data = (from t in db.INN_Customers_Campaingns
                        join impl in db.INN_Customers_Campaigns_Implementation on t.Id equals impl.CampaignId
                        where t.Id == campaignId
                        select new ViewModel_Campaign
                        {
                            EndDate = t.EndDate,
                            StartDate = impl.ImplementationTime
                        }).ToList();

            ViewModel_Campaign _data = new ViewModel_Campaign();
            _data.StartDate = data.First().StartDate;
            _data.EndDate = data.First().EndDate;
            return _data;
        }

        public bool AddAuditType(ViewModel_AuditType model)
        {
            bool result = false;
            try
            {

                INN_AuditType data = new INN_AuditType();
                data.customerid = model.customerid;
                data.entry_date = DateTime.Now;
                data.name = model.typename;
                data.isactive = true;
                db.INN_AuditType.Add(data);
                db.SaveChanges();
                result = true;
            }
            catch (Exception ex)
            {
                result = false;
            }

            return result;
        }

        public bool UpdateAuditType(ViewModel_AuditType model)
        {
            bool result = false;
            try
            {

                INN_AuditType data = db.INN_AuditType.Find(model.id);
                data.name = model.typename;
                db.Entry(data).State = EntityState.Modified;
                db.SaveChanges();
                result = true;
            }
            catch (Exception ex)
            {
                result = false;
            }

            return result;
        }

        public bool DeleteAuditType(int id)
        {
            bool result = false;
            try
            {

                INN_AuditType data = db.INN_AuditType.Find(id);
                db.Entry(data).State = EntityState.Deleted;
                db.SaveChanges();
                result = true;
            }
            catch (Exception ex)
            {
                result = false;
            }

            return result;
        }

        public bool IsExitsAuditType(string name, int cutomerid)
        {
            bool result = false;

            try
            {
                var data = (from t in db.INN_AuditType
                            where t.name.ToLower() == name && t.customerid == cutomerid
                            select t.id).FirstOrDefault();
                if (data > 0)
                {
                    result = true;
                }

            }
            catch (Exception ex)
            {

                throw;
            }

            return result;
        }

        public bool ISExitsAuditTypeForEdit(int id, string name, int cutomerid)
        {
            bool result = false;

            try
            {
                var data = (from t in db.INN_AuditType
                            where t.name.ToLower() == name && t.customerid == cutomerid && t.id != id
                            select t.id).FirstOrDefault();
                if (data > 0)
                {
                    result = true;
                }

            }
            catch (Exception ex)
            {

                throw;
            }

            return result;
        }

        public bool IsTypeUsed(int id)
        {
            bool result = false;

            var data = (from t in db.INN_AuditType
                        join type in db.INN_Customers_Campaigns_Audit_Types on t.id equals type.type
                        where t.id == id
                        select type.id).FirstOrDefault();
            if (data > 0)
            {
                result = true;
            }

            return result;
        }

        public ViewModel_Campaign UserReport(int userid)
        {
            ViewModel_Campaign model = new ViewModel_Campaign();
            TargetUserReport targetUser = new TargetUserReport();
            RegularEvaluatorReport regularEvaluatorReport = new RegularEvaluatorReport();
            ChiefEvaluatorReport chiefEvaluatorReport = new ChiefEvaluatorReport();
            SpecificRegularEvaluatorReport specificRegularEvaluatorReport = new SpecificRegularEvaluatorReport();
            SpecificChiefEvaluatorReport specificChiefEvaluatorReport = new SpecificChiefEvaluatorReport();
            CampaignManagerReport campaignManagerReport = new CampaignManagerReport();
            ProjectManagerReport projectManagerReport = new ProjectManagerReport();
            AuditorReport auditorReport = new AuditorReport();

            #region Target User Records
            var tu_total_idea_submitted = (from t in db.INN_User_Campaign_IdeaSubmission
                                           where t.CreatedBy == userid
                                           select t.id).ToList().Count;

            var tu_idea_approved = (from t in db.INN_User_Campaign_Chief_Evaluation
                                    join idea in db.INN_User_Campaign_IdeaSubmission on t.idea equals idea.id
                                    where idea.CreatedBy == userid && t.Isactive == "A"
                                    select t.id).ToList().Count;

            var tu_idea_rejected = (from t in db.INN_User_Campaign_Chief_Evaluation
                                    join idea in db.INN_User_Campaign_IdeaSubmission on t.idea equals idea.id
                                    where idea.CreatedBy == userid && t.Isactive != "A"
                                    select t.id).ToList().Count;

            targetUser._totalidea = tu_total_idea_submitted;
            targetUser._approvedidea = tu_idea_approved;
            targetUser._rejectedidea = tu_idea_rejected;
            targetUser._noaction = tu_total_idea_submitted - (tu_idea_approved + tu_idea_rejected);
            model.TargetUser = targetUser;

            #endregion

            #region Regular Evaluators

            var re_total_idea_for_evaluate = (from t in db.INN_Campaigns_Evaluators
                                              join idea in db.INN_User_Campaign_IdeaSubmission on t.category equals idea.CategoryId
                                              where t.userid == userid && t.subcategory == idea.SubCategoryId
                                              && t.type == "R"
                                              select idea.id).ToList().Count;

            var re_idea_evaluated = (from t in db.INN_User_Campaign_Evaluation
                                     join idea in db.INN_User_Campaign_IdeaSubmission on t.idea equals idea.id
                                     where t.entry_by == userid
                                     select t.id).ToList().Count;

            var re_idea_evaluation_pending = re_total_idea_for_evaluate - re_idea_evaluated;



            regularEvaluatorReport._totalidea = re_total_idea_for_evaluate;
            regularEvaluatorReport._approvedidea = re_idea_evaluated;
            regularEvaluatorReport._noaction = re_idea_evaluation_pending;
            model.RegularEvaluator = regularEvaluatorReport;
            #endregion

            #region Chief Evaluators
            var ce_total_idea_for_approval = (from t in db.INN_Campaigns_Evaluators
                                              join idea in db.INN_User_Campaign_IdeaSubmission on t.category equals idea.CategoryId
                                              where t.userid == userid && t.subcategory == idea.SubCategoryId
                                              && t.type == "C"
                                              select idea.id).ToList().Count;

            var ce_idea_approved = (from t in db.INN_User_Campaign_Chief_Evaluation
                                    join idea in db.INN_User_Campaign_IdeaSubmission on t.idea equals idea.id
                                    where t.created_by == userid && t.Isactive == "A"
                                    select t.id).ToList().Count;

            var ce_idea_rejected = (from t in db.INN_User_Campaign_Chief_Evaluation
                                    join idea in db.INN_User_Campaign_IdeaSubmission on t.idea equals idea.id
                                    where t.created_by == userid && t.Isactive != "A"
                                    select t.id).ToList().Count;

            var ce_idea_evaluation_pending = ce_total_idea_for_approval - (ce_idea_approved + ce_idea_rejected);


            chiefEvaluatorReport._totalidea = ce_total_idea_for_approval;
            chiefEvaluatorReport._approvedidea = ce_idea_approved;
            chiefEvaluatorReport._rejectedidea = ce_idea_rejected;
            chiefEvaluatorReport._noaction = ce_idea_evaluation_pending;
            model.ChiefEvaluator = chiefEvaluatorReport;

            #endregion

            #region Specific Regular Evaluator
            var sre_total_idea_for_evaluate = (from t in db.INN_Campaigns_Specific_Evaluators
                                               join team in db.INN_Campaigns_Specific_Evaluators_Team on t.team_id equals team.id
                                               join camp in db.INN_Customers_Campaingns on team.camp_id equals camp.Id
                                               join idea in db.INN_User_Campaign_IdeaSubmission on camp.Id equals idea.CampaignId
                                               where t.userid == userid && t.type == "SR"
                                               select idea.id).ToList().Count;

            var sre_idea_evaluated = (from t in db.INN_User_Campaign_Evaluation
                                      join idea in db.INN_User_Campaign_IdeaSubmission on t.idea equals idea.id
                                      where t.entry_by == userid
                                      select t.id).ToList().Count;

            var sre_idea_evaluation_pending = sre_total_idea_for_evaluate - sre_idea_evaluated;



            specificRegularEvaluatorReport._totalidea = sre_total_idea_for_evaluate;
            specificRegularEvaluatorReport._approvedidea = sre_idea_evaluated;
            specificRegularEvaluatorReport._noaction = sre_idea_evaluation_pending;
            model.SpecificRegularEvaluator = specificRegularEvaluatorReport;
            #endregion

            #region Specific Chief Evaluator

            var sce_total_idea_for_approval = (from t in db.INN_Campaigns_Specific_Evaluators
                                               join team in db.INN_Campaigns_Specific_Evaluators_Team on t.team_id equals team.id
                                               join camp in db.INN_Customers_Campaingns on team.camp_id equals camp.Id
                                               join idea in db.INN_User_Campaign_IdeaSubmission on camp.Id equals idea.CampaignId
                                               where t.userid == userid && t.type == "SC"
                                               select idea.id).ToList().Count;

            var sce_idea_approved = (from t in db.INN_User_Campaign_Chief_Evaluation
                                     join idea in db.INN_User_Campaign_IdeaSubmission on t.idea equals idea.id
                                     where t.created_by == userid && t.Isactive == "A"
                                     select t.id).ToList().Count;

            var sce_idea_rejected = (from t in db.INN_User_Campaign_Chief_Evaluation
                                     join idea in db.INN_User_Campaign_IdeaSubmission on t.idea equals idea.id
                                     where t.created_by == userid && t.Isactive != "A"
                                     select t.id).ToList().Count;

            var sce_idea_evaluation_pending = sce_total_idea_for_approval - (sce_idea_approved + sce_idea_rejected);


            specificChiefEvaluatorReport._totalidea = sce_total_idea_for_approval;
            specificChiefEvaluatorReport._approvedidea = sce_idea_approved;
            specificChiefEvaluatorReport._rejectedidea = sce_idea_rejected;
            specificChiefEvaluatorReport._noaction = sce_idea_evaluation_pending;
            model.SpecificChiefEvaluator = specificChiefEvaluatorReport;

            #endregion

            #region Campaign Manager

            var cm_total_idea = (from t in db.INN_Customers_Campaingns
                                 join idea in db.INN_User_Campaign_IdeaSubmission on t.Id equals idea.CampaignId
                                 where t.ManagerId == userid
                                 select idea.id).ToList().Count;

            var cm_idea_assigned = (from t in db.INN_User_CampaignManager_Assignment
                                    join idea in db.INN_User_Campaign_IdeaSubmission on t.idea equals idea.id
                                    where t.entry_by == userid
                                    select t.id).ToList().Count;

            var cm_idea_pending_assign = cm_total_idea - cm_idea_assigned;

            campaignManagerReport._totalidea = cm_total_idea;
            campaignManagerReport._approvedidea = cm_idea_assigned;
            campaignManagerReport._noaction = cm_idea_pending_assign;
            model.CampaignManager = campaignManagerReport;

            #endregion

            #region Project Manager

            var pm_total_idea = (from t in db.INN_User_CampaignManager_Assignment
                                 join pmang in db.INN_User_ProjectMangerList on t.id equals pmang.camp_assign_id
                                 where pmang.userid == userid
                                 select pmang.id).ToList().Count;


            var pm_total_task_created = (from t in db.INN_User_ProjectManager_Task_Creation
                                         where t.entry_by == userid
                                         select t.id).ToList().Count;

            var pm_total_task_closed = (from t in db.INN_User_ProjectManager_Task_Creation
                                        where t.entry_by == userid && t.task_completion_date != null
                                        select t.id).ToList().Count;


            var pm_total_task_opened = (from t in db.INN_User_ProjectManager_Task_Creation
                                        where t.entry_by == userid && t.task_completion_date == null
                                        select t.id).ToList().Count;

            projectManagerReport._totalidea = pm_total_idea;
            projectManagerReport._totaltask = pm_total_task_created;
            projectManagerReport._closedtask = pm_total_task_closed;
            projectManagerReport._openedtask = pm_total_task_opened;

            model.ProjectManager = projectManagerReport;


            #endregion

            #region Auditor

            var au_total_idea = (from t in db.INN_Customers_Campaigns_Audit_Types_Auditors
                                 where t.auditor == userid
                                 select t.id).ToList().Count;

            var au_approved_idea = (from t in db.INN_Campaigns_Idea_AuditorApprove
                                    where t.AuditorID == userid && t.IsApprove == 1
                                    select t.AuditorApproveID).ToList().Count;

            var au_rejected_idea = (from t in db.INN_Campaigns_Idea_AuditorApprove
                                    where t.AuditorID == userid && t.IsApprove != 1
                                    select t.AuditorApproveID).ToList().Count;
            var au_idea_pending = au_total_idea - (au_approved_idea + au_rejected_idea);

            auditorReport._totalidea = au_total_idea;
            auditorReport._approvedidea = au_approved_idea;
            auditorReport._rejectedidea = au_rejected_idea;
            auditorReport._noaction = au_idea_pending;
            model.Auditor = auditorReport;

            #endregion
            return model;
        }

        public int GetInnovationManager(int campid)
        {
            return 0;
            //var innovationmanager = (from t in db.INN_Customers_Campaingns
            //                         where t.Id == campid
            //                         select t.CustomerId).FirstOrDefault();
            //return (int)innovationmanager;
        }
    }
    public class EmailContents
    {
        public string subject { get; set; }
        public string body { get; set; }
        public List<EmailContents> ContentsList { get; set; }
    }
}
