using System.Threading.Tasks;

namespace book_data_api.Services
{
    public interface IBookCoverService
    {
        Task<byte[]?> GetBookCoverImageAsync(string searchTerm, int bookReviewId);
    }
} 