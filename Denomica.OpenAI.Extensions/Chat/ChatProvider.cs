using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace Denomica.OpenAI.Extensions.Chat
{
    public class ChatProvider
    {
        public ChatProvider(ChatClient client, IServiceProvider sp)
        {
            this.Client = client ?? throw new ArgumentNullException(nameof(client));
            this.Provider = sp ?? throw new ArgumentNullException(nameof(sp));
        }
        private readonly IServiceProvider Provider;
        public ChatClient Client { get; private set; }
    }
}
