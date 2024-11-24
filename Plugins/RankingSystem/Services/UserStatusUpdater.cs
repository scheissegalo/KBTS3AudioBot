// KBTS3AudioBot - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2024 KBTS3AudioBot contributors
// https://github.com/scheissegalo/KBTS3AudioBot
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using System;
using System.Linq;
using System.Threading.Tasks;
using TSLib.Full;
using RankingSystem.Interfaces;
using TSLib;
using RankingSystem.Models;
using System.Collections.Generic;
using static RankingSystem.RankingModule;
//using NLog.Fluent;

namespace RankingSystem.Services
{
	public class UserStatusUpdater : IUserStatusUpdater
	{
		private readonly IUserRepository _userRepository;
		private readonly IServerGroupManager _serverGroupManager;
		private readonly IChannelManager _channelManager;
		private readonly ILocalizationManager _localizationManager;
		private readonly TsFullClient _tsFullClient;
		private readonly Constants constants = new Constants();
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		private bool isChecking = false;

		public UserStatusUpdater(
			IUserRepository userRepository,
			IServerGroupManager serverGroupManager,
			IChannelManager channelManager,
			ILocalizationManager localizationManager,
			TsFullClient tsFullClient
		)
		{
			_userRepository = userRepository;
			_serverGroupManager = serverGroupManager;
			_tsFullClient = tsFullClient;
			_channelManager = channelManager;
			_localizationManager = localizationManager;
		}

		public async Task CheckUser()
		{
			if (isChecking)
			{
				return;
			}
			if (_userRepository == null)
			{
				return;
			}
			isChecking = true;
			try
			{
				var allUsers = await _tsFullClient.ClientList();

				//Console.WriteLine($"Allusers: {allUsers.Value.Length}");

				foreach (var user in allUsers.Value)
				{
					if (user == null)
						{ continue; }

					//Check if user is not a query user
					if (user.ClientType.Equals(ClientType.Full))
					{
						var fulluser = await _tsFullClient.ClientInfo(user.ClientId);
						// Check if is a bot
						ServerGroupId[] userServerGroups = fulluser.Value.ServerGroups;
						if (!userServerGroups.Any(sg => constants.BotGroupsE.Contains(sg)))
						{
							//Console.WriteLine($"User {user.Name} {fulluser.Value.Uid} has ServerGroups: {String.Join(",", fulluser.Value.ServerGroups)}");

							//If user exists in database
							if (_userRepository.FindOne(fulluser.Value.Uid) == null)
							{
								// User does not exist, so add them to the database

								//DBusers.Insert(user);
								TSUser tSUser = new TSUser
								{
									UserID = fulluser.Value.Uid,
									Name = fulluser.Value.Name,
									ClientID = user.ClientId,
									LastUpdate = DateTime.Now,
									OnlineTime = TimeSpan.Zero

								};
								_userRepository.Insert(tSUser);
								if (constants.onboardingEnabled)
								{
									await StartUserOnboarding(tSUser);
								}
								Log.Info($"New user {tSUser.Name} created");

							}
							else
							{
								//Console.WriteLine("User Found");

								int channelUserCount = await _channelManager.GetUserCountFromChannelId(fulluser.Value.ChannelId);
								TSUser? tsuser = _userRepository.FindOne(fulluser.Value.Uid);
								if (tsuser != null)
								{

									//check if username is null or not eqal to current, then add actual name
									if (tsuser.Name != fulluser.Value.Name || tsuser.Name == "" || tsuser.Name == null)
									{
										var username = fulluser.Value.Name;
										if (username.Length > 0)
										{
											tsuser.Name = username;
											_userRepository.Update(tsuser);
										}
									}

									//Console.WriteLine("User Found and not null");
									// Check for notifications
									if (tsuser.ClientID != user.ClientId)
									{
										//Console.WriteLine("ClientID Updated!");
										tsuser.ClientID = user.ClientId;
										_userRepository.Update(tsuser);
									}

									if (!tsuser.NotificationSend && constants.SendDaylyMessage)
									{
										TimeSpan timeDiff = tsuser.LastNotification - DateTime.Now;
										// Check if we are within 12 hours (positive or negative)
										if (Math.Abs(timeDiff.TotalHours) <= 12)
										{
											//Console.WriteLine("We are within 12 hours of the target date.");
										}
										else
										{
											//Console.WriteLine("We are more than 12 hours away from the target date.");

											if (tsuser.SetupDone && tsuser.DailyStatusEnabled)
											{
												string TranslateBool(bool condition) => _localizationManager.GetTranslation(tsuser.CountryCode, condition ? "yes" : "no");
												string localizedYes = _localizationManager.GetTranslation(tsuser.CountryCode, "yes").ToLower();
												string localizedNo = _localizationManager.GetTranslation(tsuser.CountryCode, "no").ToLower();

												string acceptedRules = TranslateBool(tsuser.AcceptedRules);
												string rankingDisabled = TranslateBool(tsuser.RankingEnabled);
												string setupDone = TranslateBool(tsuser.SetupDone);
												string skippedSetup = TranslateBool(tsuser.SkipSetup);
												string hasOwnChannel = TranslateBool(tsuser.WantsOwnChannel);
												string TSCountryCode = await GetUserCountryCodeFromTS(tsuser);
												string userCountryCode = tsuser.CountryCode;

												//yourTime disableStatusMessage
												string ChannelString;
												if (tsuser.WantsOwnChannel && tsuser.ChannelIDInt != 0)
												{
													var chanInfo = await _tsFullClient.ChannelInfo((ChannelId)tsuser.ChannelIDInt);
													ChannelString = $"[color=green]Your Channel[/color][b]: [color=red]{chanInfo.Value[0].Name} ({tsuser.ChannelIDInt})[/color][/b]";
												}
												else
												{
													ChannelString = "No own Channel";
												}
												//await _tsFullClient.SendPrivateMessage(_localizationManager.GetTranslation(userCountryCode, "welcomeBack") + " " + user.Name + "!", user.ClientID);
												await SendPrivateMessage(@$"[b][color=blue]═══════════- Personal Daily Status -══════════[/color][/b] 

[b]•[/b] Username: [color=#aa4400]{tsuser.Name}[/color]  
[b]•[/b] Online Time: [color=#00FF00]{FormatTimeSpan(tsuser.OnlineTime, userCountryCode)}[/color]  
[b]•[/b] Credits: [color=#00FF00]{(float)Math.Round(tsuser.Score, 2)}[/color]  
[b]•[/b] Level: [color=cyan]{GetUserLevel(tsuser.OnlineTime)}[/color]  
[b]•[/b] Channel: [color=white]{ChannelString} [/color]

{_localizationManager.GetTranslation(tsuser.CountryCode, "noDailyStatus")}", user.ClientId);

												tsuser.NotificationSend = true;
												tsuser.LastNotification = DateTime.Now;
												_userRepository.Update(tsuser);
											}
											else
											{
												if (!tsuser.DailyStatusEnabled)
												{
													//send to own channel etc noDailyStatus
													await SendPrivateMessage("Hey you have still not Completed the onboarding!\ntype anything here to get started", tsuser.ClientID, true);
													tsuser.NotificationSend = true;
													tsuser.LastNotification = DateTime.Now;
													_userRepository.Update(tsuser);
												}
											}

										}
									}


									//check if user over 30 min and wants a channel
									if (HasUserSurpassedTimeThreshold(constants.timeToAllowChannelCreation, tsuser) && tsuser.WantsOwnChannel && tsuser.ChannelIDValue != 0 && !tsuser.WantsOwnChannelNotificationSend)
									{
										await SendPrivateMessage(_localizationManager.GetTranslation(tsuser.CountryCode, "enoughTime"), tsuser.ClientID, true);
										tsuser.WantsOwnChannelNotificationSend = true;
										_userRepository.Update(tsuser);
										Log.Info($"User {tsuser.Name} has passed time to allow channel creation, the user has been notified");
									}

									if (HasUserSurpassedTimeThreshold(TimeSpan.FromHours(5), tsuser) && !tsuser.NotificationChannelsUnlocked && tsuser.OnlineTime <= TimeSpan.FromHours(6))
									{
										await SendPrivateMessage(_localizationManager.GetTranslation(tsuser.CountryCode, "enterAndUseChannel"), tsuser.ClientID, true);
										tsuser.NotificationChannelsUnlocked = true;
										_userRepository.Update(tsuser);
										Log.Info($"User {tsuser.Name} has passed time to allow channel with lock, the user has been notified");
									}

									if (!tsuser.RankingEnabled)
									{
										continue;
									}

									if (fulluser.Value.ChannelId == constants.AfkChannel)
									{
										//Console.WriteLine($"User: {fulluser.Value.Name} is in AFK Channel");
										continue;
									}

									if (fulluser.Value.ChannelId == (ChannelId)1)
									{
										//Console.WriteLine($"User: {fulluser.Value.Name} is in SPAWN Channel");
										continue;
									}

									if (fulluser.Value.ServerGroups.Contains(constants.NoAfkGroup))
									{
										//Console.WriteLine($"User: {fulluser.Value.Name} Has NoAFK");
										continue;
									}

									if (channelUserCount < 2)
									{
										//Console.WriteLine($"User: {fulluser.Value.Name} is in a channel with less than 2 clients. Count{channelUserCount}");
										continue;
									}

									DateTime currentTime = DateTime.Now;

									// Calculate the difference between the current time and the last update time
									TimeSpan timeDifference = currentTime - tsuser.LastUpdate;

									// Check if the difference is within 5 minutes
									if (timeDifference.TotalMinutes <= 3 && timeDifference.TotalMinutes >= 0)
									{
										//Console.WriteLine("The last update was within the last 5 minutes. Counting user");

										tsuser.OnlineTime += (DateTime.Now - tsuser.LastUpdate);
										tsuser.LastUpdate = DateTime.Now;
										tsuser.ClientID = user.ClientId;
										tsuser.Score += constants.ScorePerTick;

										// Save the updated timestamp back to the database
										_userRepository.Update(tsuser);
									}
									else
									{
										tsuser.LastUpdate = DateTime.Now;
										tsuser.ClientID = user.ClientId;
										_userRepository.Update(tsuser);
										//Console.WriteLine("The last update was more than 5 minutes ago. set user for counting");
									}

									//Console.WriteLine($"User {tsuser.Name} Credits: {tsuser.Score}. Last Checked: {tsuser.LastUpdate.ToString("HH:mm:ss")} Online Time: {tsuser.OnlineTime}");
								}
								else
								{
									Log.Warn($"User is null: {fulluser.Value.Name}, continue.");
									continue;
								}
								//Checking for adding or removing group
								var userGroups = await _tsFullClient.ServerGroupsByClientDbId(fulluser.Value.DatabaseId);

								// Get the current group(s) the user is in
								var currentGroups = userGroups.Value.Select(g => g.ServerGroupId).ToList();

								// Get the new group based on the online time
								ServerGroupId newGroup = GetServerGroup(tsuser.OnlineTime);

								// Check if the user is not in the correct group
								if (!currentGroups.Contains(newGroup))
								{
									//Console.WriteLine($"User {tsuser.Nickname} does not have the right group");

									// Get a list of the groups the user should be in based on their online time
									var groupsToRemove = currentGroups
										.Where(group => constants._serverGroupList.Any(sg => sg.ServerGroup == group))
										.ToList();

									// Remove the user from their old group(s) that are in _serverGroupList but not the correct group
									foreach (var group in groupsToRemove)
									{
										if (group != newGroup)  // Only remove if it's not the new group
										{
											await _tsFullClient.ServerGroupDelClient(group, fulluser.Value.DatabaseId);
											//Console.WriteLine($"User {tsuser.Nickname} removed from group {group}");
										}
									}

									// Add the user to the new group if they're not already in it
									if (!currentGroups.Contains(newGroup))
									{
										await _tsFullClient.ServerGroupAddClient(newGroup, fulluser.Value.DatabaseId);
										//Console.WriteLine($"User {tsuser.Nickname} added to new group {newGroup}");
									}
								}
								else
								{
									//Console.WriteLine($"User {tsuser.Nickname} is already in the correct group.");
								}
							}
						}
						else
						{
							//Console.WriteLine($"User {user.Name} is a Bot");
						}
					}
					else
					{
						//Console.WriteLine($"User {user.Name} is a query");
					}
				}

				if (_userRepository != null)
				{
					//DBusers.Insert(user);
					//Console.WriteLine("Access to database!");
				}

			}
			catch (Exception ex)
			{
				//Console.WriteLine(ex.ToString());
				Log.Error(ex.Message);
			}
			isChecking = false;
		}

		public int GetUserLevel(TimeSpan onlineTime)
		{
			// Convert online time to total days
			double days = onlineTime.TotalMinutes;

			// You can tweak these values as necessary
			// Let's assume 10 days for Level 1, 50 days for Level 2, and so on, but increasing less and less.
			double level = Math.Log10(days + 1);  // +1 to avoid log(0)

			// To make sure levels start from 0 and scale well, we can multiply by a constant, for example 5
			int userLevel = (int)(level * 5);

			return userLevel;
		}

		public string FormatTimeSpan(TimeSpan timeSpan, string userCountryCode)
		{
			List<string> list = new List<string>();
			if (timeSpan.Days >= 365)
			{
				int num = timeSpan.Days / 365;
				list.Add($"{num} {((num > 1) ? _localizationManager.GetTranslation(userCountryCode, "years") : _localizationManager.GetTranslation(userCountryCode, "year"))}");
			}
			if (timeSpan.Days % 365 > 0)
			{
				int num2 = timeSpan.Days % 365;
				list.Add($"{num2} {((num2 > 1) ? _localizationManager.GetTranslation(userCountryCode, "days") : _localizationManager.GetTranslation(userCountryCode, "day"))}");
			}
			if (timeSpan.Hours > 0)
			{
				list.Add($"{timeSpan.Hours} {((timeSpan.Hours > 1) ? _localizationManager.GetTranslation(userCountryCode, "hours") : _localizationManager.GetTranslation(userCountryCode, "hour"))}");
			}
			if (timeSpan.Minutes > 0)
			{
				list.Add($"{timeSpan.Minutes} {((timeSpan.Minutes > 1) ? _localizationManager.GetTranslation(userCountryCode, "minutes") : _localizationManager.GetTranslation(userCountryCode, "minute"))}");
			}
			if (timeSpan.Seconds > 0)
			{
				list.Add($"{timeSpan.Seconds} {((timeSpan.Seconds > 1) ? _localizationManager.GetTranslation(userCountryCode, "seconds") : _localizationManager.GetTranslation(userCountryCode, "second"))}");
			}
			//Console.WriteLine($"Result: {string.Join(" ", list)} | Orig: {timeSpan}");
			return string.Join(" ", list);
		}

		public ServerGroupId GetServerGroup(TimeSpan onlineTime)
		{
			foreach (var serverGroupInfo in constants._serverGroupList)
			{
				if (onlineTime < serverGroupInfo.OnlineTimeThreshold)
				{
					return serverGroupInfo.ServerGroup;
				}
			}

			// If the online time exceeds all thresholds, return the last server group in the list
			//Console.WriteLine("Online Time Exceedet");
			Log.Warn("Online Time Exceedet! We need higher servergroups!");
			return constants._serverGroupList.Last().ServerGroup;
		}

		public async Task<string> GetUserCountryCodeFromTS(TSUser tsuser)
		{
			//Console.WriteLine($"Trying to get Country code from user {tsuser.Name} ID: {tsuser.ClientID}");
			var fulluser = await _tsFullClient.ClientInfo(tsuser.ClientID);
			if (fulluser.Ok)
			{
				return fulluser.Value.CountryCode;
			}
			else
			{
				return "en";
			}
		}

		public async Task StartUserOnboarding(TSUser tsUser)
		{
			Log.Info($"Automatic Onboarding started for user: {tsUser.Name}.");
			string message;
			string messageEN = "";
			//string answer;
			string TSCountryCode = await GetUserCountryCodeFromTS(tsUser);

			if (tsUser.CountryCode != "en")
			{

				//message = _localizationManager.GetTranslation(TSCountryCode, "welcomeMessage");
				//await _tsFullClient.SendPrivateMessage($"{_localizationManager.GetTranslation(TSCountryCode, "hello")} {user.Name}!\n {message}", user.ClientID);
				message = _localizationManager.GetTranslation(TSCountryCode, "welcomeMessage") + " " + _localizationManager.GetTranslation(TSCountryCode, "whatIsYourLanguage");
				await SendPrivateMessage($"{_localizationManager.GetTranslation(TSCountryCode, "skipSetup")} {message} [b]{TSCountryCode}[/b]!", tsUser.ClientID);
			}
			else
			{
				if (TSCountryCode != "en")
				{
					messageEN = "Backup English:" + _localizationManager.GetTranslation("en", "welcomeMessage");
					messageEN += _localizationManager.GetTranslation("en", "whatIsYourLanguage");
					//await _tsFullClient.SendPrivateMessage($"Backup English:{_localizationManager.GetTranslation("en", "skipSetup")}\n{messageEN} [b]{TSCountryCode}[/b]!", user.ClientID);
				}
				message = $"{messageEN} | " + _localizationManager.GetTranslation(TSCountryCode, "welcomeMessage") + _localizationManager.GetTranslation(TSCountryCode, "whatIsYourLanguage");
				await SendPrivateMessage($"{_localizationManager.GetTranslation(TSCountryCode, "skipSetup")}\n{message} [b]{TSCountryCode}[/b]!", tsUser.ClientID);
			}

			//Send user to Select Language step
			tsUser.SetupStep = (int)SetupStep.AskPreferredLanguage;
			_userRepository.Update(tsUser);
		}

		public bool HasUserSurpassedTimeThreshold(TimeSpan threshold, TSUser tsuser)
		{
			return tsuser.OnlineTime >= threshold;
		}

		private async Task<bool> SendPrivateMessage(string message, ClientId client, bool format = false)
		{
			if (format)
			{
				message = $@"[b][color=red]{message}[/color][/b]";
			}

			string formattetMessage = $@"{constants.messageHeader} {message} {constants.messageFooter}";
			var result = await _tsFullClient.SendPrivateMessage(formattetMessage, client);

			if (result.Ok)
			{
				return true;
			}

			return false;
		}
	}
}
