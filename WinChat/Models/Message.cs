using System;
using System.Linq;

namespace WinChat.Models
{
    class Message
    {
        private string _userName;

        public string UserName
        {
            get { return _userName; }
            set { _userName = value; }
        }

        private string _text;

        public string Text
        {
            get { return _text; }
            set { _text = value; }
        }

        public Message(string userName, string text)
        {
            this.UserName = userName;
            this.Text = text;
        }
    }
}
