using TL;
using WTelegram;

namespace HappyNewYearApp.Telegram
{
    public class TelegramManager
    {
        private const string PHONE_NUMBER = "";
        private const int APIL_ID = 0;
        private const string API_HASH = "";

        private Client _client;

        public async Task<bool> DoLogin(Func<string, Task<string>> verificationStepCallback)
        {
            try
            {
                _client = new Client(APIL_ID, API_HASH);

                var loginInfo = PHONE_NUMBER;

                while (_client.User == null)
                {
                    var loginResult = await _client.Login(loginInfo);

                    switch (loginResult)
                    {
                        case "verification_code": loginInfo = await verificationStepCallback(loginResult); break;
                        case "password": loginInfo = await verificationStepCallback(loginResult); break;
                        default: loginInfo = null; break;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Application.Current.MainPage.DisplayAlert(ex.GetType().ToString(), ex.Message, "Ok");
            }

            return false;
        }

        public async Task<IList<Dialog>> GetDialogs()
        {
            if (_client is null)
            {
                return null;
            }

            var allDialogs = new List<Dialog>();
            var dialogs = await _client.Messages_GetAllDialogs();

            var tasks = dialogs.Dialogs.Select(async dialog =>
            {
                switch (dialogs.UserOrChat(dialog))
                {
                    case User user when user.IsActive && !user.IsBot:
                        {
                            using var stream = new MemoryStream();
                            var fileType = await _client.DownloadProfilePhotoAsync(user, stream);
                            var byteArray = stream.ToArray();
                            
                            lock (allDialogs)
                            {
                                allDialogs.Add(new Dialog
                                {
                                    Id = user.ID,
                                    Peer = user,
                                    PublicName = $"{user.first_name} {user.last_name}",
                                    Username = user.username,
                                    ImageBytes = byteArray,
                                });
                            }

                            break;
                        }
                    case ChatBase chat when chat.IsActive && chat.IsGroup:
                        {
                            using var stream = new MemoryStream();
                            var fileType = await _client.DownloadProfilePhotoAsync(chat, stream);
                            var byteArray = stream.ToArray();

                            lock (allDialogs)
                            {
                                allDialogs.Add(new Dialog
                                {
                                    Id = chat.ID,
                                    Peer = chat,
                                    PublicName = chat.Title,
                                    Username = chat.MainUsername,
                                    ImageBytes = byteArray,
                                });
                            }

                            break;
                        }
                }
            });

            await Task.WhenAll(tasks);

            return allDialogs;
        }

        public async Task<bool> SendMessages(string message, IEnumerable<Dialog> selectedDialogs)
        {
            if (_client is null)
            {
                return false;
            }
            var rand = new Random();

            try
            {
                foreach (var dialog in selectedDialogs)
                {
                    if (dialog.Peer is User user)
                    {
                        await _client.Messages_SendMessage(user, message, rand.Next());
                    }
                    else if (dialog.Peer is ChatBase chat)
                    {
                        await _client.Messages_SendMessage(chat, message, rand.Next());
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Application.Current.MainPage.DisplayAlert(ex.GetType().ToString(), ex.Message, "Ok");
            }

            return false;
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}

