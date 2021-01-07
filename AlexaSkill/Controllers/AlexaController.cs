using AlexaSkill.Data;
using AlexaSkill.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Http;

namespace AlexaSkill.Controllers
{
    public class AlexaController : ApiController
    {
        public string skillName;
        public string cardTitle;
        public string launchPhrase;       
        public string repromptPhrase;

        private BurnRepository _burnRepository;

        public AlexaController()
        {
            _burnRepository = new BurnRepository();

            skillName = "Jokester";
            cardTitle = "Welcome";
            launchPhrase = "Welcome to Burn. To dish out a burn, say give me a burn";             
            repromptPhrase = "Please say, ask for a burn, or stop, at any time to exit.";           
        }


        [HttpPost, Route("api/alexa/demo")]
        public dynamic PluralSight(AlexaRequest alexaRequest)
        //public dynamic PluralSight(dynamic request)
        {

            var request = new Requests().Create(new Data.Request
            {
                //MemberId = alexaRequest.Session.Attributes.MemberId,
                Timestamp = alexaRequest.Request.Timestamp,
                Intent = (alexaRequest.Request.Intent.Name == null) ? "" : alexaRequest.Request.Intent.Name,
                AppId = alexaRequest.Session.Application.ApplicationId,
                RequestId = alexaRequest.Request.RequestId,
                SessionId = alexaRequest.Session.SessionId,
                UserId = alexaRequest.Session.User.UserId,
                IsNew = alexaRequest.Session.New,
                Version = alexaRequest.Version,
                Type = alexaRequest.Request.Type,
                Reason = "", //alexaRequest.Request.Reason,
                SlotsList = alexaRequest.Request.Intent.GetSlots(),
                DateCreated = DateTime.UtcNow
            });

            AlexaResponse response = null;

            switch (request.Type)
            {
                case "LaunchRequest": 
                    response = LaunchRequestHandler(request);
                    break;
                case "IntentRequest":
                    response = IntentRequestHandler(request);
                    break;
                case "SessionEndedRequest":
                    response = SessionEndedRequestHandler(request);
                    break;
            }
            
            return response;


            #region BASIC_RESPONSE_SAMPLE
            /*
            return new
            {
                version = "1.0",
                sessionAttributes = new { memberId = request.MemberId },
                response = new
                {
                    outputSpeech = new
                    {
                        type = "PlainText",
                        text = responsePhrase
                    },
                    card = new
                    {
                        type = "Simple",
                        title = cardTitle,
                        content = responsePhrase
                    },
                    reprompt = new 
                    {
                        outputSpeech = new
                        {
                            type = "PlainText",
                            text = repromptPhrase
                        }
                    },
                    shouldEndSession = false
                }
            };
            */
            #endregion
        }

      
        private AlexaResponse LaunchRequestHandler(Request request)
        {
            var response = new AlexaResponse(launchPhrase);
            //response.Session.MemberId = request.MemberId;
            response.Response.Card.Title = cardTitle;
            response.Response.Card.Content = launchPhrase;
            response.Response.Reprompt.OutputSpeech.Text = repromptPhrase;
            response.Response.ShouldEndSession = false;

            return response;
        }

        private AlexaResponse IntentRequestHandler(Request request)
        {
            AlexaResponse response = null;

            switch (request.Intent)
            {
                case "BurnIntent":
                    response = BurnIntentHandler(request);
                    response.Response.Card.Title = "Burn";
                    break;
                case "AMAZON.CancelIntent":
                case "AMAZON.StopIntent":
                    response = CancelOrStopItentHandler(request);
                    break;
                case "AMAZON.HelpIntent": 
                    response = HelpIntentHandler(request);
                    break;
                case "AMAZON.FallbackIntent":
                    response = HelpIntentHandler(request);
                    break;
            }

            return response;

        }        

        private AlexaResponse HelpIntentHandler(Request request)
        {
            string msg;
            msg = "To use this skill, you can say, Alexa, ask " + skillName + " for a burn";
            //"the new courses. You can also say, Alexa, stop or Alexa, cancel, at any time to exit. For now, do you want to hear the Top Courses, or the New Courses?
                        
            var response = new AlexaResponse(msg, false);
            response.Response.Reprompt.OutputSpeech.Text = repromptPhrase;
            return response;
        }

        private AlexaResponse CancelOrStopItentHandler(Request request)
        {
            return new AlexaResponse(
                outputSpeechText: "Ok, talk to you later.", 
                shouldEndSession: true
            );
        }


        private AlexaResponse BurnIntentHandler(Request request)
        {
            var name = "";
            var burn = "";
            var burnMessage = "";
            var ssmlBurn = "";
            var burnType = "soft";
            bool useSSML;

            if (request.SlotsList.Any())
            {
                burnType = GetSlot(request, "BurnType");
                name = GetSlot(request, "Name");
            }

            burn = _burnRepository.GetRandomBurn(burnType);

            if (!string.IsNullOrWhiteSpace(burn))
            {
                burnMessage = ConstructBurnMessage(name, burn);
                ssmlBurn = PersonifyBurn(burnMessage);
                useSSML = true;
            }
            // No results returned from query. Return a default message back to user.
            else
            {
                burnMessage = "I could not find a burn for you.";
                useSSML = false;
            }

            return new AlexaResponse(
                outputSpeechText: burnMessage,
                useSSML: useSSML,
                cardContent: burnMessage,
                shouldEndSession: true
            );
        }

        private AlexaResponse SessionEndedRequestHandler(Request request)
        {
            return null;
        }
        private string ConstructBurnMessage(string name, string burn)
        {
            var message = !string.IsNullOrWhiteSpace(name) &&
                          !name.Equals("mi") &&
                          !name.Equals("me") // exclude "me"
            ? $"{name}, {burn}"
            : burn;

            return message;
        }

        private string GetSlot(Request request, string key)
        {
            var slot = "";
            var burnTypeValue = request.SlotsList.FirstOrDefault(s => s.Key == key).Value;
            if (!string.IsNullOrWhiteSpace(burnTypeValue))
            {
                slot = burnTypeValue;
            }
            return slot;
        }

        private string GetInterjection()
        {
            var interjections = new List<string>
            {
                "Boom!",
                "Bam!",
                "Booya!",
                "Gotcha!",
                "Just kidding!",
                "Oh snap!",
                "Tee hee!"
            };

            int randomPos = new Random().Next(interjections.Count);
            return interjections[randomPos];
        }

        private string PersonifyBurn(string burn)
        {
            var message = new StringBuilder();
            message.Append("<speak><prosody pitch='x-high'>");
            message.Append(burn);
            message.Append("<say-as interpret-as='interjection'>");
            message.Append(GetInterjection());
            message.Append("</say-as></prosody></speak>");
            return message.ToString();
        }
    }
}
