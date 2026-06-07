using Lus.Application.Common.Services;
using Lus.Application.Images.Entities;
using Lus.Application.Images.Repositories;
using Lus.Authorization;
using Lus.Infrastructure.Persistence;

namespace Lus.Infrastructure.Repositories
{
    public class ImagesRepository : EntityFrameworkRepository<Image, int>, IImagesRepository
    {
        public ImagesRepository(ApplicationContext context, IChangeApplierService changeApplier,
            IUserAccessor userAccessor)
            : base(context, userAccessor)
        {
        }
    }
}
