using System.Threading.Tasks;
using WebApit4s.Models;

public interface IChildContextService
{
    Task<string> GetUserIdAsync();          // ✅ Ends with ;
    Task<int?> GetActiveChildIdAsync();     // ✅ Ends with ;
    Task<Child?> GetActiveChildAsync();     // ✅ Ends with ;
}
