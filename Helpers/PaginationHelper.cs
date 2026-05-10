namespace panelapp.Helpers
{
    public static class PaginationHelper
    {
        public static int GetTotalPages(int totalCount, int pageSize)
        {
            return Math.Max(1,
                (int)Math.Ceiling(totalCount / (double)pageSize));
        }

        public static int NormalizePage(int page, int totalPages)
        {
            if (page < 1)
                return 1;

            if (page > totalPages)
                return totalPages;

            return page;
        }
    }
}