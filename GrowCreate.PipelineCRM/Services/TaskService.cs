using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Core;
using GrowCreate.PipelineCRM.Models;
using Umbraco.Web;
using GrowCreate.PipelineCRM.DataServices;

namespace GrowCreate.PipelineCRM.Services
{
    public class TaskService
    {
        private Task GetTaskUser(Task task)
        {
            if (task.UserId >= 0)
            {
                var userService = ApplicationContext.Current.Services.UserService;
                var user = userService.GetByProviderKey(task.UserId);
                if (user != null)
                {
                    task.UserName = user.Name;
                    task.UserAvatar = umbraco.library.md5(user.Email);
                }
            }
            else if (task.ContactId > 0)
            {
                var contact = new ContactService().GetById(task.ContactId, false);
                if (contact != null)
                {
                    task.UserName = contact.Name;
                    task.UserAvatar = umbraco.library.md5(contact.Email);
                }
            }

            if (!String.IsNullOrEmpty(task.Files) && task.Files.Split(',').Count() > 0)
            {
                var media = ApplicationContext.Current.Services.MediaService.GetByIds(from f in task.Files.Split(',') select int.Parse(f)); //umbracoHelper.TypedMedia(task.Files.Split(','));
                var mediaList = new List<MediaFile>();

                foreach (var file in media)
                {
                    var mediaFile = new MediaFile()
                    {
                        Id = file.Id,
                        Name = file.Name,
                        //Url = file.Url
                    };
                    mediaList.Add(mediaFile);
                }

                task.Media = mediaList;
            }

            task.Overdue = !task.Done && task.DateDue < DateTime.Now ? (DateTime.Now - task.DateDue).Days : 0;

            return task;
        }

        public Task GetById(int id)
        {
            var query = new Sql().Select("*").From("pipelineTask").Where<Task>(x => x.Id == id);
            var task = DbService.db().Fetch<Task>(query).FirstOrDefault();

            task = GetTaskUser(task);

            return task;
        }

        public IEnumerable<Task> GetByPipelineId(int id)
        {
            var query = new Sql().Select("*").From("pipelineTask").Where<Task>(x => x.PipelineId == id).OrderByDescending("DateDue").OrderByDescending("DateCreated");
            var tasks = DbService.db().Fetch<Task>(query).ToList();

            for (int i = 0; i < tasks.Count; i++)
            {
                tasks[i] = GetTaskUser(tasks[i]);
            }

            return tasks;
        }

        public IEnumerable<Task> GetByContactId(int id)
        {
            var query = new Sql().Select("*").From("pipelineTask").Where<Task>(x => x.ContactId == id).OrderByDescending("DateDue").OrderByDescending("DateCreated");
            var tasks = DbService.db().Fetch<Task>(query);

            for (int i = 0; i < tasks.Count; i++)
            {
                tasks[i] = GetTaskUser(tasks[i]);
                tasks[i].Pipeline = new PipelineService().GetById(tasks[i].PipelineId);
            }

            return tasks;
        }

        public IEnumerable<Task> GetByOrganisationId(int id)
        {
            var query = new Sql().Select("*").From("pipelineTask").Where<Task>(x => x.OrganisationId == id).OrderByDescending("DateDue").OrderByDescending("DateCreated");
            var tasks = DbService.db().Fetch<Task>(query);

            for (int i = 0; i < tasks.Count; i++)
            {
                tasks[i] = GetTaskUser(tasks[i]);
                tasks[i].Pipeline = new PipelineService().GetById(tasks[i].PipelineId);
            }

            return tasks;
        }

        public Task Toggle(int id)
        {
            var task = GetById(id);
            if (!task.Done)
            {
                task.Done = true;
                task.DateComplete = DateTime.Now;
            }
            else
            {
                task.Done = false;
            }
            DbService.db().Update(task);
            return task;
        }

        public Task Save(Task task)
        {
            return TaskDbService.Instance.SaveTask(task);
        }

        public int Delete(int id)
        {
            return DbService.db().Delete<Task>(id);
        }

    }
}