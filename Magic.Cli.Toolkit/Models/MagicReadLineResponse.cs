using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.Cli.Toolkit
{
    public class MagicReadLineResponse<T>
    {
        public T? Response { get; }
        public bool Canceled { get; }

        public MagicReadLineResponse(T? response)
        {
            Response = response;
            Canceled = false;
        }

        public MagicReadLineResponse()
        {
            Canceled = true;
        }
    }
}
