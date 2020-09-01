using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewModel;
using ViewModel.Innovation;

namespace BAL
{
    public interface ICustomerCampaignRepository
    {
        int SaveCapmaignInfo(ViewModel_Campaign model);
        void SaveCampaignImages(ViewModel_CustomerCampaignImages model);
        string CloseCampaign(int campaignId);
        string DropCampaign(int campaignId);
        void SaveCampaignTargetUsers(ViewModel_Customers_Campaigns_TargetUser model);
        void SaveSocialStage(IEnumerable<ViewModel_Customers_Campaigns_SocialStep> data, int CampaignId, int UserId);
        void SaveImplementationStage(IEnumerable<ViewModel_Customers_Campaigns_Implementation> data, int CampaignId, int UserId);
        void SaveRewardStage(ViewModel_Customers_Campaigns_Rewards model);
        IEnumerable<ViewModel_Campaign> getCustomerCampaign(int CustomerId);
        IEnumerable<ViewModel_Campaign> getArchivedCustomerCampaign(int CustomerId);
        ViewModel_Campaign getCustomerCampaigninfo(int campaignId_Id);
        void deleteCampaignInage(int campaignId, int imageId);
        void DeleteCampaignAttachment(int campaignId);
        void PublishCampaign(int campaignId);
        bool RemoveExistingUser(int campaignId);
        bool CampaignSkipStep(int camp_id, int step);
        List<ViewModel_Campaignsteps> GetCampaignSteps(int campid);
        bool IsPublished(int campid);
        ViewModel_Campaign GetCampEndDate(int campaignId);
        ViewModel_Campaign GetIdeaEndDate(int campaignId);
        ViewModel_Campaign GetSocialStageEndDate(int campaignId);        
        ViewModel_Campaign GetEvaluationEndDate(int campaignId);
        ViewModel_Campaign GetImplementationEndDate(int campaignId);
        List<ViewModel_Customers_Campaigns_SocialStep> EditSocialStage(int innovationid);
        ViewModel_Customers_Campaigns_Implementation EditImplementation(int innovationId);
        ViewModel_Customers_Campaigns_Rewards EditReward(int innovationId);
        bool DeletePrimaryImage(int innovationid);
        ViewModel_Campaign CampaignDetails(int userid,int campid);
        bool CheckCampaginname(string name, int userId, int campaginId);
		IEnumerable<ViewModel_CustomerCampaignImages> getCampaignImages(string IsPrimary, int CampaignId);
        List<string> CampaignSummary_TargetUser(int campId);
        List<string> CampaignSummary_CampaignManager(int campId);
        List<string> CampaignSummary_RegularEvaluator(int cateid,int subcateid);
        List<string> CampaignSummary_ChiefEvaluator(int cateid, int subcateid);
        List<string> CampaignSummary_ProjectManager(int campId);
        List<string> CampaignSummary_Auditor(int campId);
        List<string> CampaignSummary_TaskPerson(int campId);
        List<string> CampaignSummary_IdeaSubmission(int campId,int userid);


        List<EmailContents> CampaignSummary_RegularEvaluation(int idea, int regulareval_id);
        List<EmailContents> CampaignSummary_ChiefEvaluation(int idea, int chiefeval_id);
        List<EmailContents> CampaignSummary_Implementation(int idea, int chiefeval_id);
        List<EmailContents> CampaignSummary_TaskCreation(int idea, int proj_mang_id);
        List<EmailContents> CampaignSummary_TaskClosed(int idea, int proj_mang_id);


       //Added by Vishnu
        IEnumerable<ViewModel_Campaign> Get_UserCampaignsReport(int CustomerId);
        IEnumerable<ViewModel_Campaign> Get_Campaigns();
        IEnumerable<ViewModel_Users_by_Campaign> GetUsers_By_Campaign(int CampaignId);
        IEnumerable<ViewModel_AuditType> GetAuditTypes(int customerid);
        IEnumerable<ViewModel_AuditType> GetAuditTypesForAudit(int campid, int createdby);

        ViewModel_Campaign Get_DepartmentwiseIdea(int depid, int campid, string _date);

        List<ViewModel_Campaign> Get_CampaignsForType(int depid);
        bool IsEmailNotificationAllowed(int campid);
        bool IsSMSNotificationAllowed(int campid);

        bool AddAuditType(ViewModel_AuditType model);
        bool UpdateAuditType(ViewModel_AuditType model);
        bool DeleteAuditType(int id);
        bool IsExitsAuditType(string name,int cutomerid);
        bool ISExitsAuditTypeForEdit(int id,string name, int cutomerid);
        bool IsTypeUsed(int id);

        ViewModel_Campaign UserReport(int userid);

        ViewModel_Campaign GetAllowedSocialStageEndDate(int campaignId);

        ViewModel_Campaign GetAllowedRegularEvaluatorEndDate(int campaignId);

        ViewModel_Campaign GetAllowedSpecificEvaluationEndDate(int campaignId);

        int GetInnovationManager(int campid);
    }
}
