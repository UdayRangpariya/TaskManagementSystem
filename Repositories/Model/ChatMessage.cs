using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Model.chat
{
    public class ChatMessage
    {
 public int c_message_id { get; set; }
    public int c_sender_id { get; set; }
    public int c_recipient_id { get; set; }
    public string c_content { get; set; }
    public DateTime c_timestamp { get; set; }
    public bool c_is_read { get; set; } = false;
    }
}