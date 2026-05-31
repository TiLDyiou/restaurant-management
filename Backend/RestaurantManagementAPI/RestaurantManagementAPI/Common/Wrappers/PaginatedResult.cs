using System;
using System.Collections.Generic;

namespace RestaurantManagementAPI.Common.Wrappers
{
    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public static PaginatedResult<T> Create(List<T> items, int count, int pageNumber, int pageSize)
        {
            int totalPages = (int)Math.Ceiling(count / (double)pageSize);
            return new PaginatedResult<T>
            {
                Items = items,
                TotalCount = count,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages == 0 ? 1 : totalPages
            };
        }
    }
}
