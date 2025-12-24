using System;
using WebApi.Controllers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Linq.Expressions;

namespace QueueManagerBot
{
    class ControllerUser
    {
        private HttpClient httpClient;
        private string apiBaseUrl;
        public ControllerUser(HttpClient httpClient, string apiBaseUrl)
        {
            this.httpClient = httpClient;
            this.apiBaseUrl = apiBaseUrl;
        }

        public async Task<WebApi.Controllers.BotUserController.InfoUserDto?> GetUser(long tgId)
        {
            try
            {
                var userResponse = await httpClient.GetAsync($"{apiBaseUrl}/api/users/user-info?telegramId={tgId}");
                if (!userResponse.IsSuccessStatusCode)
                {
                    return null;
                }

                var user = await userResponse.Content.ReadFromJsonAsync<WebApi.Controllers.BotUserController.InfoUserDto>();
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public async Task<bool> UpdateUserInfo(WebApi.Controllers.BotUserController.BotUserDto user)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync($"{apiBaseUrl}/api/users/update-userinfo", user);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<List<string>?> GetCategoryList(string groupCode)
        {
            try
            {
                var categoriesResponse = await httpClient.GetAsync($"{apiBaseUrl}/api/groups/category-list?groupCode={groupCode}");

                if (!categoriesResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine("Ошибка при получении категорий");
                    return null;
                }

                var categories = await categoriesResponse.Content.ReadFromJsonAsync<List<string>>();
                return categories;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public async Task<List<WebApi.Controllers.BotEventController.BotEventDto>?> GetQueueList(long tgId)
        {
            try
            {
                var response = await httpClient.GetAsync(
                $"{apiBaseUrl}/api/events/user-info-events?telegramId={tgId}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var events = JsonSerializer.Deserialize<List<BotEventController.BotEventDto>>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return events;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }


        public async Task<List<WebApi.Controllers.BotEventController.BotEventDto>?> GetQueueListForGroup(string group)
        {
            try
            {
                var response = await httpClient.GetAsync(
                $"{apiBaseUrl}/api/events/events-for-group?groupCode={group}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var events = JsonSerializer.Deserialize<List<BotEventController.BotEventDto>>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return events;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public async Task<bool> CreateQueue(WebApi.Controllers.BotEventController.CreationDto queue)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync($"{apiBaseUrl}/api/events/create-queue", queue);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> CreateCategory(WebApi.Controllers.BotGroupController.CategoryDto category)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync($"{apiBaseUrl}/api/groups/add-category", category);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> DeleteCategory(WebApi.Controllers.BotGroupController.DeletionDto category)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync($"{apiBaseUrl}/api/events/delete-category", category);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    Console.WriteLine($"{category.GroupCode} {category.CategoryName}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> ConfirmQueue(WebApi.Controllers.BotEventController.ParticipationDto participant)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync($"{apiBaseUrl}/api/events/confirm", participant);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }


        public async Task<List<WebApi.Controllers.BotEventController.BotEventDto>?> DueEventsNotification()
        {
            try
            {
                var response = await httpClient.GetAsync($"{apiBaseUrl}/api/events/due-events-notification");
                if (response.IsSuccessStatusCode)
                {
                    var notificationEvents = await response.Content.ReadFromJsonAsync<List<WebApi.Controllers.BotEventController.BotEventDto>>();
                    return notificationEvents;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public async Task<bool> MarkNotified(List<Guid> eventIds)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync($"{apiBaseUrl}/api/events/mark-notified",
                        new { Ids = eventIds });
                if (response.IsSuccessStatusCode)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
        public async Task<List<long>> GetGroupUsers(string groupCode)
        {
            try
            {
                var response = await httpClient.GetAsync($"{apiBaseUrl}/api/group/users-for-group?groupCode={groupCode}");

                if (response.IsSuccessStatusCode)
                {
                    var tgIds = await response.Content.ReadFromJsonAsync<List<long>>();
                    return tgIds;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

    }
}
