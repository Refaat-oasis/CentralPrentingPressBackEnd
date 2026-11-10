namespace CentralPress.API.DTOs;

public class PaginationRequestDto
{
    private const int MaxPageSize = 100;
    private int _pageSize = 25;
    private int _page = 1;

    public int Page
    {
        get => _page;
        init => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? 1 : value);
    }
}
