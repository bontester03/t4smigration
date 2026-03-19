namespace WebApit4s.ViewModels
{
    public class PagedUserListViewModel
    {
        public List<UserListItemViewModel> Users { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
