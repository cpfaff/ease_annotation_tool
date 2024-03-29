﻿
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using AutoMapper;
using CAFE.Core.Messaging;
using CAFE.Core.Resources;
using CAFE.Core.Security;
using CAFE.Web.Areas.Api.Models.AccessResources;
using Microsoft.AspNet.Identity;
using NLog;
using System.Web;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using System.Net.Http;
using System.Threading.Tasks;
using Messages = CAFE.Core.Misc.Messages;
using CAFE.Web.Areas.Api.Models;
using CAFE.DAL.Interfaces;

namespace CAFE.Web.Areas.Api.Controllers
{
    /// <summary>
    /// API endpoint for manage Access requests and conversations over resources
    /// </summary>
    [Authorize]
    public class AccessResourcesController : ApiControllerBase
    {
        private readonly ISecurityService _securityService;
        private readonly IAccessRequestService _accessRequestService;
        private readonly IConversationService _conversationService;
        private UserManager<User> _userManager;
        private readonly IRepository<DAL.Models.DbAnnotationItem> _annotationRepository;

        /// <summary>
        /// Constructor with dependencies
        /// </summary>
        /// <param name="securityService">Security service</param>
        /// <param name="logger">Common controller logger</param>
        /// <param name="accessRequestService">Access requests service</param>
        /// <param name="conversationService">Conversations service</param>
        public AccessResourcesController(
            ISecurityService securityService,
            ILogger logger,
            IAccessRequestService accessRequestService,
            IRepository<DAL.Models.DbAnnotationItem> annotationRepository,
            IConversationService conversationService, UserManager<User> userManager)
                : base(logger, securityService)
        {
            _annotationRepository = annotationRepository;
            _userManager = userManager;
            _securityService = securityService;
            _accessRequestService = accessRequestService;
            _conversationService = conversationService;
        }

        /// <summary>
        /// Returns conversations related with access request that initiate current loggined user
        /// </summary>
        /// <returns>Collection of conversation model</returns>
        [HttpGet]
        public IEnumerable<ConversationModel> GetRequestsFromMe()
        {
            IEnumerable<ConversationModel> result = new List<ConversationModel>();

            Execute(() =>
            {

                var data = _conversationService.GetConversationsBySender(GetCurrentUser());
                var mappedData =
                    Mapper.Map<IEnumerable<Conversation>, IEnumerable<ConversationModel>>(data);
                return FillResourcesNames(mappedData);

            }, onSuccess: models =>
            {
                result = models;
            }, onFail: e =>
            {
                ThrowInternalServerError(e.Message);
            });

            return result;
        }

        /// <summary>
        /// Returns conversations related with access that addressed to current loggined user
        /// </summary>
        /// <returns>Collection of conversation model</returns>
        [HttpGet]
        public IEnumerable<ConversationModel> GetRequestsToMe()
        {
            IEnumerable<ConversationModel> result = new List<ConversationModel>();

            Execute(() =>
            {

                var mappedData =
                    Mapper.Map<IEnumerable<Conversation>, IEnumerable<ConversationModel>>(
                        _conversationService.GetConversationsByReceiver(GetCurrentUser()));

                return FillResourcesNames(mappedData);

            }, onSuccess: models =>
            {
                result = models;
            }, onFail: e =>
            {
                ThrowInternalServerError(e.Message);
            });

            return result;
        }

        /// <summary>
        /// Returns conversations related with access that addressed to current loggined user
        /// </summary>
        /// <returns>Collection of conversation model</returns>
        [HttpGet]
        public IEnumerable<ConversationModel> GetConversationsWithUnreadMessages()
        {
            IEnumerable<ConversationModel> result = new List<ConversationModel>();

            Execute(() =>
            {
                var currentUser = GetCurrentUser();
                var dbModel = _conversationService.GetConversationsWithUnreadMessages(currentUser);
                var mappedData =
                    Mapper.Map<IEnumerable<Conversation>, IEnumerable<ConversationModel>>(dbModel);

                return FillResourcesNames(mappedData);

            }, onSuccess: models =>
            {
                result = models;
            }, onFail: e =>
            {
                ThrowInternalServerError(e.Message);
            });

            return result;
        }

        /// <summary>
        /// Returns all access request from users
        /// </summary>
        /// <returns>Collection of access requests</returns>
        [HttpGet]
        public List<AccessRequestsViewModel> GetAllAccessRequests()
        {
            List<AccessRequestsViewModel> result = new List<AccessRequestsViewModel>();
            Execute(() =>
            {
                var requestsFromMe = FillResourcesNames(GetRequestsFromMe());
                var requestsToMe = FillResourcesNames(GetRequestsToMe());

                foreach (var request in requestsFromMe)
                {
                    var mappedRequest = Mapper.Map<AccessRequestsViewModel>(request);
                    mappedRequest.Type = AccessRequestsViewModel.AccessRequestsType.Outgoing;
                    result.Add(mappedRequest);
                }

                foreach (var request in requestsToMe)
                {
                    var mappedRequest = Mapper.Map<AccessRequestsViewModel>(request);
                    mappedRequest.Type = AccessRequestsViewModel.AccessRequestsType.Incoming;
                    result.Add(mappedRequest);
                }

                return result;

            }, onSuccess: models =>
            {
                result = models;
            }, onFail: e =>
            {
                ThrowInternalServerError(e.Message);
            });

            return result;
        }

        /// <summary>
        /// Creates access request and returns model of new created access request
        /// </summary>
        /// <param name="model">Creation model</param>
        /// <returns>Access request</returns>
        [HttpPost]
        public AccessRequestModel CreateRequest([FromBody] CreationAccessRequestModel model)
        {
            AccessRequestModel result = default(AccessRequestModel);

            Execute(() =>
            {

                var resources =
                    Mapper.Map<IEnumerable<AccessibleResourceModel>, IEnumerable<AccessibleResource>>(
                        model.RequestedResources);

                var requester = GetCurrentUser();

                var request = _accessRequestService.MakeRequest(
                    model.RequestSubject, model.RequestMessage, resources, requester);

                var mappedRequest = Mapper.Map<AccessRequest, AccessRequestModel>(request);

                return mappedRequest;

            }, onSuccess: m =>
            {
                result = m;
            }, onFail: e =>
            {
                ThrowInternalServerError(e.Message);
            });

            return result;
        }

        /// <summary>
        /// Accepts conversation related with access request
        /// </summary>
        /// <param name="model">Conversation identifier</param>
        /// <returns>Status</returns>
        [HttpPost]
        public IHttpActionResult AcceptRequest([FromBody] long model)
        {
            return ExecuteExplicit(() =>
           {
               var acceptingResult = _conversationService.AcceptConversation(model);
               var conversation = acceptingResult.Key;
               var resourcesNames = acceptingResult.Value;

               _userManager.SendEmail(
                   conversation.Receiver.Id,
                   Messages.AcessRequestAccepted_Subject,
                   System.String.Format(
                       Messages.AcessRequestAccepted,
                       conversation.Receiver.Name,
                       conversation.Receiver.Surname,
                       resourcesNames,
                       Url.Link("Default", new { Controller = "Account", Action = "Login" })
                   ));

           }, onSuccess: Ok, onFail: Fail);
        }

        /// <summary>
        /// Declines conversation related with access request
        /// </summary>
        /// <param name="model">Conversation declining model</param>
        /// <returns>Status</returns>
        [HttpPost]
        public IHttpActionResult DeclineRequest([FromBody] DeclineAccessRequestModel model)
        {
            return ExecuteExplicit(() =>
            {
                var acceptingResult = _conversationService.DeclineConversation(model.ConversationId, model.Reason);
                var conversation = acceptingResult.Key;
                var resourcesNames = acceptingResult.Value;

                _userManager.SendEmail(
                   conversation.Receiver.Id,
                   Messages.AcessRequestRejected_Subject,
                   System.String.Format(
                       Messages.AcessRequestRejected,
                       conversation.Receiver.Name,
                       conversation.Receiver.Surname,
                       resourcesNames,
                       Url.Link("Default", new { Controller = "Account", Action = "Login" })
                   ));

            }, onSuccess: Ok, onFail: Fail);
        }

        /// <summary>
        /// Returns conversations of access request
        /// </summary>
        /// <param name="requestId">Access request</param>
        /// <returns>Collection of conversation model</returns>
        [HttpGet]
        public IEnumerable<MessageModel> GetMessages([FromUri] long conversationId)
        {

            IEnumerable<MessageModel> result = new List<MessageModel>();

            Execute(() =>
            {

                var data = _conversationService.GetMessages(conversationId, System.Guid.Parse(User.Identity.GetUserId()));

                var mappedData =
                    Mapper.Map<IEnumerable<Message>, IEnumerable<MessageModel>>(data);

                return mappedData;

            }, onSuccess: models =>
            {
                result = models;
            }, onFail: e =>
            {
                ThrowInternalServerError(e.Message);
            });

            return result;

        }


        /// <summary>
        /// Adds new message to opened conversation
        /// </summary>
        /// <param name="model">Creation message model</param>
        /// <returns>New created message</returns>
        [HttpPost]
        public MessageModel AddMessage([FromBody] CreationMessageModel model)
        {

            MessageModel result = default(MessageModel);

            Execute(() =>
            {
                var conversation = _conversationService.GetConversationById(model.ConversationId);

                var receiver = _securityService.GetUserById(User.Identity.GetUserId() == conversation.Receiver.Id ? conversation.Requester.Id : conversation.Receiver.Id);
                var sender = GetCurrentUser();

                var message = _conversationService.AddNewMessageToConversation(model.ConversationId, sender, receiver, model.Text, User.Identity.GetUserId() == conversation.Receiver.Id);

                var mappedData = Mapper.Map(message, new MessageModel());

                return mappedData;

            }, onSuccess: m =>
            {
                result = m;
            }, onFail: e =>
            {
                ThrowInternalServerError(e.Message);
            });

            return result;

        }

        //Fill resource names array for conversation view model. In UI this resource view as simple text
        private IEnumerable<ConversationModel> FillResourcesNames(IEnumerable<ConversationModel> conversations)
        {
            var conversationsList = conversations.ToList();
            foreach (var conversationModel in conversationsList)
            {
                var accessRequest = _accessRequestService.GetAccessRequest(conversationModel.RequestId);
                var resources =
                    accessRequest.
                    RequestedResources.
                    Where(r => r.Content.OwnerId == conversationModel.ReceiverId && r.Kind == AccessibleResourceKind.AnnotationItem).
                    Select(r => new { key = r.ResourceId.ToString(), value = _annotationRepository.Find(ai => ai.Id == r.ResourceId).Object.References.Descriptions?[0]?.Title }).
                    Distinct().
                    ToDictionary(d => d.key, d => d.value);

                conversationModel.Resources = resources;
            }
            return conversationsList;
        }
    }
}
