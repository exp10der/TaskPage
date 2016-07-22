using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using StructureMap;

namespace PageSample
{
    internal class Program
    {
        private static void Main()
        {
            IoC.Container.Configure(cfg => { cfg.AddRegistry(new AutoMapperRegistry()); });


            using (var container = IoC.Container.GetNestedContainer())
            {
                var models = new List<Model>
                {
                    new Model {Id = 1, FirstName = "Test1", LastName = "Test1"},
                    new Model {Id = 2, FirstName = "Test2", LastName = "Test2"},
                    new Model {Id = 3, FirstName = "Test3", LastName = "Test3"},
                    new Model {Id = 3, FirstName = "Test4", LastName = "Test4"}
                };

                var q = models.AsQueryable();

                var pagelist = q.ProjectToPagedList<ViewModel>(container.GetInstance<MapperConfiguration>(), 2, 2);

                Debugger.Break();
            }
        }
    }

    public class Model
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class ViewModel
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class PaginatedList<T> : List<T>
    {
        public PaginatedList(IQueryable<T> source, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            PageSize = pageSize;
            TotalCount = source.Count();
            TotalPages = (int) Math.Ceiling(TotalCount/(double) PageSize);

            AddRange(pageIndex == 1
                ? source.Skip(0).Take(pageSize).ToList()
                : source.Skip((pageIndex - 1)*pageSize).Take(pageSize).ToList());
        }

        public int PageIndex { get; }
        public int PageSize { get; }
        public int TotalCount { get; }
        public int TotalPages { get; }

        public bool HasPreviousPage => PageIndex > 0;

        public bool HasNextPage => PageIndex + 1 < TotalPages;
    }

    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Model, ViewModel>();
        }
    }

    public static class MapperExtensions
    {
        public static PaginatedList<TDestination> ProjectToPagedList<TDestination>(this IQueryable queryable,
            MapperConfiguration config,
            int pageNumber, int pageSize)
        {
            return queryable.ProjectTo<TDestination>(config)
                .ToPagedList(pageNumber, pageSize);
        }

        public static PaginatedList<T> ToPagedList<T>(this IQueryable<T> superset, int pageNumber, int pageSize)
        {
            return new PaginatedList<T>(superset, pageNumber, pageSize);
        }
    }

    public class AutoMapperRegistry : Registry
    {
        public AutoMapperRegistry()
        {
            var profiles =
                typeof(AutoMapperRegistry).Assembly.GetTypes()
                    .Where(t => typeof(Profile).IsAssignableFrom(t))
                    .Select(t => (Profile) Activator.CreateInstance(t));

            var config = new MapperConfiguration(cfg =>
            {
                foreach (var profile in profiles)
                {
                    cfg.AddProfile(profile);
                }
            });

            For<MapperConfiguration>().Use(config);
            For<IMapper>().Use(ctx => ctx.GetInstance<MapperConfiguration>().CreateMapper(ctx.GetInstance));
        }
    }

    public static class IoC
    {
        static IoC()
        {
            Container = new Container();
        }

        public static IContainer Container { get; set; }
    }
}