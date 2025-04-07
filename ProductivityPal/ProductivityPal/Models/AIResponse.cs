using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductivityPal.Models
{
    public class AIResponse {
        public required List<AIChatChoice> Choices { get; set; }
    }

    public class AIChatChoice {
        public required AIMessage Message { get; set; }
    }

    public class AIMessage {
        public required string Content { get; set; }
    }
}
