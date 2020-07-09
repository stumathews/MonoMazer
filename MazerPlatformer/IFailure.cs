using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MazerPlatformer
{
    /// <summary>
    /// 
    /// </summary>
    public interface IFailure
    {
        /// <summary>
        /// Nature of the failure
        /// </summary>
        string Reason { get; set; }
    }
}
