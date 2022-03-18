﻿using Retro_Achievement_Tracker.Forms;
using Retro_Achievement_Tracker.Models;
using Retro_Achievement_Tracker.Properties;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Retro_Achievement_Tracker.Forms.AlertsWindow;

namespace Retro_Achievement_Tracker.Controllers
{
    public sealed class AlertsController
    {
        private static AlertsController instance = new AlertsController();
        private static AlertsWindow AlertsLayoutWindow;
        public static bool IsOpen;
        private Stopwatch Stopwatch;
        private Task NotificationsTask;
        private readonly ConcurrentQueue<NotificationRequest> NotificationRequests;
        private readonly CancellationTokenSource tokenSource2 = new CancellationTokenSource();
        private bool IsPlaying;
        private bool CanPlay;
        private bool AnimationInPlayed;
        private bool AnimationOutPlayed;
        private bool PlayingAchievement;

        private AlertsController()
        {
            AlertsLayoutWindow = new AlertsWindow();

            if (CustomAchievementEnabled && !File.Exists(CustomAchievementFile))
            {
                CustomAchievementEnabled = false;
            }

            if (CustomMasteryEnabled && !File.Exists(CustomMasteryFile))
            {
                CustomMasteryEnabled = false;
            }

            NotificationRequests = new ConcurrentQueue<NotificationRequest>();

            Stopwatch = new Stopwatch();

            NotificationsTask = Task.Factory.StartNew(() => { });

            CanPlay = false;
            IsOpen = false;
        }
        public void EnqueueAchievementNotifications(List<Achievement> achievements)
        {
            if (CanPlay && !AlertsLayoutWindow.IsDisposed)
            {
                if (CustomAchievementEnabled && string.IsNullOrEmpty(CustomAchievementFile))
                {
                    CustomAchievementEnabled = false;
                }

                foreach (Achievement achievement in achievements)
                {
                    NotificationRequest notificationRequest = new NotificationRequest
                    {
                        Achievement = achievement
                    };

                    NotificationRequests.Enqueue(notificationRequest);
                }
            }
        }

        public void EnqueueMasteryNotification(UserSummary userSummary, GameInfoAndProgress gameInfoAndProgress)
        {
            if (CanPlay && !AlertsLayoutWindow.IsDisposed)
            {
                if (CustomMasteryEnabled && string.IsNullOrEmpty(CustomMasteryFile))
                {
                    CustomMasteryEnabled = false;
                }

                NotificationRequest notificationRequest = new NotificationRequest
                {
                    UserSummary = userSummary,
                    GameInfoAndProgress = gameInfoAndProgress
                };

                NotificationRequests.Enqueue(notificationRequest);
            }
        }
        private NotificationRequest NotificationRequestDequeue()
        {
            NotificationRequests.TryDequeue(out NotificationRequest notificationRequest);

            return notificationRequest;
        }

        public async void RunNotifications()
        {
            if (NotificationRequests.Count > 0 && CanPlay && !IsPlaying && !Stopwatch.IsRunning)
            {
                NotificationRequest notificationRequest = NotificationRequestDequeue();

                if (tokenSource2.Token.IsCancellationRequested)
                {
                    tokenSource2.Token.ThrowIfCancellationRequested();
                }

                if (notificationRequest != null)
                {
                    if (notificationRequest.Achievement != null)
                    {
                        PlayingAchievement = true;
                        await AlertsLayoutWindow.StartAchievementAlert(notificationRequest.Achievement);
                    }
                    else if (notificationRequest.UserSummary != null && notificationRequest.GameInfoAndProgress != null)
                    {
                        PlayingAchievement = false;
                        await AlertsLayoutWindow.StartMasteryAlert(notificationRequest.UserSummary, notificationRequest.GameInfoAndProgress);
                    }
                }
                NotificationsTask = Task.Factory.StartNew(RunNotificationTask, tokenSource2.Token);
            }
        }
        private async void RunNotificationTask()
        {
            Stopwatch = Stopwatch.StartNew();

            AnimationInPlayed = false;
            AnimationOutPlayed = false;

            while (Stopwatch.IsRunning)
            {
                if (tokenSource2.Token.IsCancellationRequested)
                {
                    tokenSource2.Token.ThrowIfCancellationRequested();
                }

                if (PlayingAchievement)
                {
                    if (!AnimationInPlayed && (AlertsLayoutWindow.GetAchievementPlayingTime().Result * 1000) > (CustomAchievementEnabled ? CustomAchievementIn : 25))
                    {
                        await AlertsLayoutWindow.SetAchievementIn(CustomAchievementEnabled ? CustomAchievementInSpeed : 500, (CustomAchievementEnabled ? AchievementAnimationIn : AnimationDirection.STATIC));
                        AnimationInPlayed = true;
                    }
                    else if (!AnimationOutPlayed && (AlertsLayoutWindow.GetAchievementPlayingTime().Result * 1000) > (CustomAchievementEnabled ? CustomAchievementOut : 5500))
                    {
                        await AlertsLayoutWindow.SetAchievementOut(CustomAchievementEnabled ? CustomAchievementOutSpeed : 500, (CustomAchievementEnabled ? AchievementAnimationOut : AnimationDirection.UP));
                        AnimationOutPlayed = true;
                    }
                }
                else
                {
                    if (!AnimationInPlayed && (AlertsLayoutWindow.GetMasteryPlayingTime().Result * 1000) > (CustomMasteryEnabled ? CustomMasteryIn : 25))
                    {
                        await AlertsLayoutWindow.SetMasteryIn(CustomMasteryEnabled ? CustomMasteryInSpeed : 500, CustomMasteryEnabled ? MasteryAnimationIn : AnimationDirection.STATIC);
                        AnimationInPlayed = true;
                    }
                    else if (!AnimationOutPlayed && (AlertsLayoutWindow.GetMasteryPlayingTime().Result * 1000) > (CustomMasteryEnabled ? CustomMasteryOut : 5500))
                    {
                        await AlertsLayoutWindow.SetMasteryOut(CustomMasteryEnabled ? CustomMasteryOutSpeed : 500, CustomMasteryEnabled ? MasteryAnimationOut : AnimationDirection.UP);
                        AnimationOutPlayed = true;
                    }
                }
                if (AnimationInPlayed && AnimationOutPlayed)
                {
                    Stopwatch.Stop();
                }
                await Task.Delay(10);
            }
        }

        public async void SetIsPlaying(bool isPlaying)
        {
            IsPlaying = isPlaying;
            if (!IsPlaying)
            {
                await AlertsLayoutWindow.HideNotifications();

                if (NotificationRequests.Count > 0)
                {
                    RunNotifications();
                }
            }
        }

        public void SetCanPlay()
        {
            CanPlay = true;
        }
        public static AlertsController Instance
        {
            get
            {
                return instance;
            }
        }
        public void Close()
        {
            AlertsLayoutWindow.Close();
        }
        public void Show()
        {
            if (!IsOpen)
            {
                AlertsLayoutWindow = new AlertsWindow();
                AlertsLayoutWindow.Show();
            }
        }
        public void Reset()
        {
            if (IsOpen)
            {
                AlertsLayoutWindow.SetupBrowser();
            }
        }
        public async void SetAllSettings()
        {
            if (IsOpen)
            {
                await AlertsLayoutWindow.AssignJavaScriptVariables().ContinueWith(async (result) =>
                {
                    await AlertsLayoutWindow.SetBorderBackgroundColor(BorderBackgroundColor);
                    await AlertsLayoutWindow.SetWindowBackgroundColor(WindowBackgroundColor);

                    if (BorderEnabled)
                    {
                        await AlertsLayoutWindow.EnableBorder();
                    }
                    else
                    {
                        await AlertsLayoutWindow.DisableBorder();
                    }
                    if (AdvancedSettingsEnabled)
                    {
                        SetAdvancedSettings();
                    }
                    else
                    {
                        SetSimpleSettings();
                    }

                    SetAchievementSettings();
                    SetMasterySettings();
                });
            }
        }
        public async void SetSimpleSettings()
        {
            await AlertsLayoutWindow.SetSimpleFontFamily(SimpleFontFamily);
            await AlertsLayoutWindow.SetSimpleFontColor(SimpleFontColor);
            await AlertsLayoutWindow.SetSimpleFontOutline(SimpleFontOutlineEnabled ? SimpleFontOutlineColor + " " + SimpleFontOutlineSize + "px" : "0px", SimpleFontOutlineEnabled ? SimpleFontOutlineSize + "px solid " + SimpleFontOutlineColor : "0px");
        }
        public async void SetAdvancedSettings()
        {
            await AlertsLayoutWindow.SetTitleFontFamily(TitleFontFamily);
            await AlertsLayoutWindow.SetTitleColor(TitleColor);
            await AlertsLayoutWindow.SetTitleOutline(TitleOutlineEnabled ? TitleOutlineColor + " " + TitleOutlineSize + "px" : "0px");

            await AlertsLayoutWindow.SetDescriptionFontFamily(DescriptionFontFamily);
            await AlertsLayoutWindow.SetDescriptionColor(DescriptionColor);
            await AlertsLayoutWindow.SetDescriptionOutline(DescriptionOutlineEnabled ? DescriptionOutlineColor + " " + DescriptionOutlineSize + "px" : "0px");

            await AlertsLayoutWindow.SetPointsFontFamily(PointsFontFamily);
            await AlertsLayoutWindow.SetPointsColor(PointsColor);
            await AlertsLayoutWindow.SetPointsOutline(PointsOutlineEnabled ? PointsOutlineColor + " " + PointsOutlineSize + "px" : "0px");

            await AlertsLayoutWindow.SetLineColor(LineColor);
            await AlertsLayoutWindow.SetLineOutline(LineOutlineEnabled ? LineOutlineSize + "px solid " + LineOutlineColor : "0px");
        }
        public void SetAchievementSettings()
        {
            AlertsLayoutWindow.SetAchievementLeft(CustomAchievementEnabled ? CustomAchievementX : -25);
            AlertsLayoutWindow.SetAchievementTop(CustomAchievementEnabled ? CustomAchievementY : 5);
            AlertsLayoutWindow.SetAchievementWidth(CustomAchievementEnabled ? Convert.ToInt32(CustomAchievementScale * MediaHelper.GetVideoWidth(CustomAchievementFile)) : 1028);
        }
        public void SetMasterySettings()
        {
            AlertsLayoutWindow.SetMasteryLeft(CustomMasteryEnabled ? CustomMasteryX : -25);
            AlertsLayoutWindow.SetMasteryTop(CustomMasteryEnabled ? CustomMasteryY : 5);
            AlertsLayoutWindow.SetMasteryWidth(CustomMasteryEnabled ? Convert.ToInt32(CustomMasteryScale * MediaHelper.GetVideoWidth(CustomMasteryFile)) : 1028);
        }
        public async void SendAchievementNotification(Achievement achievement)
        {
            if (IsOpen)
            {
                await AlertsLayoutWindow.StartAchievementAlert(achievement);
            }
        }
        public async void EnableAchievementEdit()
        {
            if (IsOpen)
            {
                await AlertsLayoutWindow.EnableAchievementEdit();
            }
        }
        public async void DisableAchievementEdit()
        {
            if (IsOpen)
            {
                await AlertsLayoutWindow.DisableAchievementEdit();
                IsPlaying = false;
            }
        }
        public async void EnableMasteryEdit()
        {
            if (IsOpen)
            {
                await AlertsLayoutWindow.EnableMasteryEdit();
            }
        }
        public async void DisableMasteryEdit()
        {
            if (IsOpen)
            {
                await AlertsLayoutWindow.DisableMasteryEdit();
                IsPlaying = false;
            }
        }
        /**
         * Variables
         */
        public bool AdvancedSettingsEnabled
        {
            get
            {
                return Settings.Default.alerts_advanced_options_enabled;
            }
            set
            {
                Settings.Default.alerts_advanced_options_enabled = value;
                Settings.Default.Save();

                if (IsOpen)
                {
                    SetAllSettings();
                }
            }
        }
        public FontFamily SimpleFontFamily
        {
            get
            {
                FontFamily[] familyArray = FontFamily.Families.ToArray();

                foreach (FontFamily font in familyArray)
                {
                    if (font.Name.Equals(Settings.Default.notification_font_family_name))
                    {
                        return font;
                    }
                }
                Settings.Default.notification_font_family_name = familyArray[0].Name;

                return familyArray[0];
            }
            set
            {
                Settings.Default.notification_font_family_name = value.Name;
                Settings.Default.Save();

                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.AssignJavaScriptVariables();
                        await AlertsLayoutWindow.SetSimpleFontFamily(SimpleFontFamily);
                    });
                }
            }
        }
        public string SimpleFontColor
        {
            get
            {
                return Settings.Default.notification_font_color_hex_code;
            }
            set
            {
                Settings.Default.notification_font_color_hex_code = value;
                Settings.Default.Save();

                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.AssignJavaScriptVariables();
                        await AlertsLayoutWindow.SetSimpleFontColor(value);
                    });
                }
            }
        }
        public string SimpleFontOutlineColor
        {
            get
            {
                return Settings.Default.notification_font_outline_color_hex;
            }
            set
            {
                Settings.Default.notification_font_outline_color_hex = value;
                Settings.Default.Save();
                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.AssignJavaScriptVariables();
                        await AlertsLayoutWindow.SetSimpleFontOutline(SimpleFontOutlineEnabled ? SimpleFontOutlineColor + " " + SimpleFontOutlineSize + "px" : "0px", SimpleFontOutlineEnabled ? SimpleFontOutlineSize + "px solid " + SimpleFontOutlineColor : "0px");
                    });
                }
            }
        }
        public int SimpleFontOutlineSize
        {
            get
            {
                return Settings.Default.notification_font_outline_size;
            }
            set
            {
                Settings.Default.notification_font_outline_size = value;
                Settings.Default.Save();

                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.AssignJavaScriptVariables();
                        await AlertsLayoutWindow.SetSimpleFontOutline(SimpleFontOutlineEnabled ? SimpleFontOutlineColor + " " + SimpleFontOutlineSize + "px" : "0px", SimpleFontOutlineEnabled ? SimpleFontOutlineSize + "px solid " + SimpleFontOutlineColor : "0px");
                    });
                }
            }
        }
        public bool SimpleFontOutlineEnabled
        {
            get
            {
                return Settings.Default.notification_font_outline_enabled;
            }
            set
            {
                Settings.Default.notification_font_outline_enabled = value;
                Settings.Default.Save();
                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.AssignJavaScriptVariables();
                        await AlertsLayoutWindow.SetSimpleFontOutline(SimpleFontOutlineEnabled ? SimpleFontOutlineColor + " " + SimpleFontOutlineSize + "px" : "0px", SimpleFontOutlineEnabled ? SimpleFontOutlineSize + "px solid " + SimpleFontOutlineColor : "0px");
                    });
                }
            }
        }
        public FontFamily TitleFontFamily
        {
            get
            {
                FontFamily[] familyArray = FontFamily.Families.ToArray();

                foreach (FontFamily font in familyArray)
                {
                    if (font.Name.Equals(Settings.Default.alerts_title_font_family))
                    {
                        return font;
                    }
                }
                Settings.Default.alerts_title_font_family = familyArray[0].Name;

                return familyArray[0];
            }
            set
            {
                Settings.Default.alerts_title_font_family = value.Name;
                Settings.Default.Save();
                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.AssignJavaScriptVariables();
                        await AlertsLayoutWindow.SetTitleFontFamily(TitleFontFamily);
                    });
                }
            }
        }
        public FontFamily DescriptionFontFamily
        {
            get
            {
                FontFamily[] familyArray = FontFamily.Families.ToArray();

                foreach (FontFamily font in familyArray)
                {
                    if (font.Name.Equals(Settings.Default.alerts_description_font_family))
                    {
                        return font;
                    }
                }
                Settings.Default.alerts_description_font_family = familyArray[0].Name;

                return familyArray[0];
            }
            set
            {
                Settings.Default.alerts_description_font_family = value.Name;
                Settings.Default.Save();
                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.AssignJavaScriptVariables();
                        await AlertsLayoutWindow.SetDescriptionFontFamily(DescriptionFontFamily);
                    });
                }
            }
        }

        public FontFamily PointsFontFamily
        {
            get
            {
                FontFamily[] familyArray = FontFamily.Families.ToArray();

                foreach (FontFamily font in familyArray)
                {
                    if (font.Name.Equals(Settings.Default.alerts_points_font_family))
                    {
                        return font;
                    }
                }
                Settings.Default.alerts_points_font_family = familyArray[0].Name;

                return familyArray[0];
            }
            set
            {
                Settings.Default.alerts_points_font_family = value.Name;
                Settings.Default.Save();

                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.AssignJavaScriptVariables();
                        await AlertsLayoutWindow.SetPointsFontFamily(PointsFontFamily);
                    });
                }
            }
        }

        public string TitleColor
        {
            get
            {
                return Settings.Default.alerts_title_color;
            }
            set
            {
                Settings.Default.alerts_title_color = value;
                Settings.Default.Save();

                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.AssignJavaScriptVariables();
                        await AlertsLayoutWindow.SetTitleColor(value);
                    });
                }
            }
        }
        public string DescriptionColor
        {
            get
            {
                return Settings.Default.alerts_description_color;
            }
            set
            {
                Settings.Default.alerts_description_color = value;
                Settings.Default.Save();

                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.AssignJavaScriptVariables();
                        await AlertsLayoutWindow.SetDescriptionColor(value);
                    });
                }
            }
        }
        public string PointsColor
        {
            get
            {
                return Settings.Default.alerts_points_color;
            }
            set
            {
                Settings.Default.alerts_points_color = value;
                Settings.Default.Save();

                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.AssignJavaScriptVariables();
                        await AlertsLayoutWindow.SetPointsColor(value);
                    });
                }
            }
        }
        public string LineColor
        {
            get
            {
                return Settings.Default.alerts_line_color;
            }
            set
            {
                Settings.Default.alerts_line_color = value;
                Settings.Default.Save();

                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.AssignJavaScriptVariables();
                        await AlertsLayoutWindow.SetLineColor(value);
                    });
                }
            }
        }
        public bool TitleOutlineEnabled
        {
            get
            {
                return Settings.Default.alerts_title_outline_enabled;
            }
            set
            {
                Settings.Default.alerts_title_outline_enabled = value;
                Settings.Default.Save();

                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.AssignJavaScriptVariables();
                        await AlertsLayoutWindow.SetTitleOutline(TitleOutlineEnabled ? TitleOutlineColor + " " + TitleOutlineSize + "px" : "0px");
                    });
                }
            }
        }
        public bool DescriptionOutlineEnabled
        {
            get
            {
                return Settings.Default.alerts_description_outline_enabled;
            }
            set
            {
                Settings.Default.alerts_description_outline_enabled = value;
                Settings.Default.Save();

                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.AssignJavaScriptVariables();
                        await AlertsLayoutWindow.SetDescriptionOutline(DescriptionOutlineEnabled ? DescriptionOutlineColor + " " + DescriptionOutlineSize + "px" : "0px");
                    });
                }
            }
        }
        public bool PointsOutlineEnabled
        {
            get
            {
                return Settings.Default.alerts_points_outline_enabled;
            }
            set
            {
                Settings.Default.alerts_points_outline_enabled = value;
                Settings.Default.Save();

                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.AssignJavaScriptVariables();
                        await AlertsLayoutWindow.SetPointsOutline(PointsOutlineEnabled ? PointsOutlineColor + " " + PointsOutlineSize + "px" : "0px");
                    });
                }
            }
        }
        public bool LineOutlineEnabled
        {
            get
            {
                return Settings.Default.alerts_line_outline_enabled;
            }
            set
            {
                Settings.Default.alerts_line_outline_enabled = value;
                Settings.Default.Save();

                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.AssignJavaScriptVariables();
                        await AlertsLayoutWindow.SetLineOutline(LineOutlineEnabled ? LineOutlineSize + "px solid " + LineOutlineColor : "0px");
                    });
                }
            }
        }
        public string TitleOutlineColor
        {
            get
            {
                return Settings.Default.alerts_title_outline_color;
            }
            set
            {
                Settings.Default.alerts_title_outline_color = value;
                Settings.Default.Save();
                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.AssignJavaScriptVariables();
                        await AlertsLayoutWindow.SetTitleOutline(TitleOutlineEnabled ? TitleOutlineColor + " " + TitleOutlineSize + "px" : "0px");
                    });
                }
            }
        }
        public string DescriptionOutlineColor
        {
            get
            {
                return Settings.Default.alerts_description_outline_color;
            }
            set
            {
                Settings.Default.alerts_description_outline_color = value;
                Settings.Default.Save();
                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.AssignJavaScriptVariables();
                        await AlertsLayoutWindow.SetDescriptionOutline(DescriptionOutlineEnabled ? DescriptionOutlineColor + " " + DescriptionOutlineSize + "px" : "0px");
                    });
                }
            }
        }
        public string PointsOutlineColor
        {
            get
            {
                return Settings.Default.alerts_points_outline_color;
            }
            set
            {
                Settings.Default.alerts_points_outline_color = value;
                Settings.Default.Save();
                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.AssignJavaScriptVariables();
                        await AlertsLayoutWindow.SetPointsOutline(PointsOutlineEnabled ? PointsOutlineColor + " " + PointsOutlineSize + "px" : "0px");
                    });
                }
            }
        }
        public string LineOutlineColor
        {
            get
            {
                return Settings.Default.alerts_line_outline_color;
            }
            set
            {
                Settings.Default.alerts_line_outline_color = value;
                Settings.Default.Save();
                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.AssignJavaScriptVariables();
                        await AlertsLayoutWindow.SetLineOutline(LineOutlineEnabled ? LineOutlineSize + "px solid " + LineOutlineColor : "0px");
                    });
                }
            }
        }
        public int TitleOutlineSize
        {
            get
            {
                return Settings.Default.alerts_title_outline_size;
            }
            set
            {
                Settings.Default.alerts_title_outline_size = value;
                Settings.Default.Save();
                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.AssignJavaScriptVariables();
                        await AlertsLayoutWindow.SetTitleOutline(TitleOutlineEnabled ? TitleOutlineColor + " " + TitleOutlineSize + "px" : "0px");
                    });
                }
            }
        }
        public int DescriptionOutlineSize
        {
            get
            {
                return Settings.Default.alerts_description_outline_size;
            }
            set
            {
                Settings.Default.alerts_description_outline_size = value;
                Settings.Default.Save();
                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.AssignJavaScriptVariables();
                        await AlertsLayoutWindow.SetDescriptionOutline(DescriptionOutlineEnabled ? DescriptionOutlineColor + " " + DescriptionOutlineSize + "px" : "0px");
                    });
                }
            }
        }
        public int PointsOutlineSize
        {
            get
            {
                return Settings.Default.alerts_points_outline_size;
            }
            set
            {
                Settings.Default.alerts_points_outline_size = value;
                Settings.Default.Save();

                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.AssignJavaScriptVariables();
                        await AlertsLayoutWindow.SetPointsOutline(PointsOutlineEnabled ? PointsOutlineColor + " " + PointsOutlineSize + "px" : "0px");
                    });
                }
            }
        }
        public int LineOutlineSize
        {
            get
            {
                return Settings.Default.alerts_line_outline_size;
            }
            set
            {
                Settings.Default.alerts_line_outline_size = value;
                Settings.Default.Save();

                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.AssignJavaScriptVariables();
                        await AlertsLayoutWindow.SetLineOutline(LineOutlineEnabled ? LineOutlineSize + "px solid " + LineOutlineColor : "0px");
                    });
                }
            }
        }
        public bool BorderEnabled
        {
            get
            {
                return Settings.Default.notifications_border_enable;
            }
            set
            {
                Settings.Default.notifications_border_enable = value;
                Settings.Default.Save();
                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.AssignJavaScriptVariables();
                        if (value)
                        {
                            await AlertsLayoutWindow.EnableBorder();
                        }
                        else
                        {
                            await AlertsLayoutWindow.DisableBorder();
                        }
                    });
                }
            }
        }
        public string BorderBackgroundColor
        {
            get
            {
                return Settings.Default.notifications_background_color;
            }
            set
            {
                Settings.Default.notifications_background_color = value;
                Settings.Default.Save();
                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.SetBorderBackgroundColor(value);
                    });
                }
            }
        }
        public string WindowBackgroundColor
        {
            get
            {
                return Settings.Default.alerts_window_background_color;
            }
            set
            {
                Settings.Default.alerts_window_background_color = value;
                Settings.Default.Save();
                if (IsOpen)
                {
                    Task.Run(async () =>
                    {
                        await AlertsLayoutWindow.SetWindowBackgroundColor(value);
                    });
                }
            }
        }
        public bool CustomAchievementEnabled
        {
            get
            {
                return Settings.Default.notification_custom_achievement_enable;
            }
            set
            {
                Settings.Default.notification_custom_achievement_enable = value;
                Settings.Default.Save();
            }
        }
        public bool CustomMasteryEnabled
        {
            get
            {
                return Settings.Default.notification_custom_mastery_enable;
            }
            set
            {
                Settings.Default.notification_custom_mastery_enable = value;
                Settings.Default.Save();
            }
        }
        public int CustomAchievementX
        {
            get
            {
                return Settings.Default.notification_custom_achievement_x;
            }
            set
            {
                Settings.Default.notification_custom_achievement_x = value;
                Settings.Default.Save();

                if (IsOpen)
                {
                    AlertsLayoutWindow.SetAchievementLeft(value);
                }
            }
        }
        public int CustomAchievementY
        {
            get
            {
                return Settings.Default.notification_custom_achievement_y;
            }
            set
            {
                Settings.Default.notification_custom_achievement_y = value;
                Settings.Default.Save();

                if (IsOpen)
                {
                    AlertsLayoutWindow.SetAchievementTop(value);
                }
            }
        }
        public int CustomMasteryX
        {
            get
            {
                return Settings.Default.notification_custom_mastery_x;
            }
            set
            {
                Settings.Default.notification_custom_mastery_x = value;
                Settings.Default.Save();

                if (IsOpen)
                {
                    AlertsLayoutWindow.SetMasteryLeft(value);
                }
            }
        }
        public int CustomMasteryY
        {
            get
            {
                return Settings.Default.notification_custom_mastery_y;
            }
            set
            {
                Settings.Default.notification_custom_mastery_y = value;
                Settings.Default.Save();

                if (IsOpen)
                {
                    AlertsLayoutWindow.SetMasteryTop(value);
                }
            }
        }
        public decimal CustomAchievementScale
        {
            get
            {
                return Settings.Default.notification_custom_achievement_scale;
            }
            set
            {
                Settings.Default.notification_custom_achievement_scale = value;
                Settings.Default.Save();

                if (IsOpen)
                {
                    AlertsLayoutWindow.SetAchievementWidth(CustomAchievementEnabled ? Convert.ToInt32(CustomAchievementScale * MediaHelper.GetVideoWidth(CustomAchievementFile)) : 1028);
                }
            }
        }
        public decimal CustomMasteryScale
        {
            get
            {
                return Settings.Default.notification_custom_mastery_scale;
            }
            set
            {
                Settings.Default.notification_custom_mastery_scale = value;
                Settings.Default.Save();

                if (IsOpen)
                {
                    AlertsLayoutWindow.SetMasteryWidth(CustomMasteryEnabled ? Convert.ToInt32(CustomMasteryScale * MediaHelper.GetVideoWidth(CustomMasteryFile)) : 1028);
                }
            }
        }
        public int CustomAchievementIn
        {
            get
            {
                return Settings.Default.notification_custom_achievement_fade_in;
            }
            set
            {
                Settings.Default.notification_custom_achievement_fade_in = value;
                Settings.Default.Save();
            }
        }
        public int CustomAchievementOut
        {
            get
            {
                return Settings.Default.notification_custom_achievement_fade_out;
            }
            set
            {
                Settings.Default.notification_custom_achievement_fade_out = value;
                Settings.Default.Save();
            }
        }
        public int CustomMasteryIn
        {
            get
            {
                return Settings.Default.notification_custom_mastery_fade_in;
            }
            set
            {
                Settings.Default.notification_custom_mastery_fade_in = value;
                Settings.Default.Save();
            }
        }
        public int CustomMasteryOut
        {
            get
            {
                return Settings.Default.notification_custom_mastery_fade_out;
            }
            set
            {
                Settings.Default.notification_custom_mastery_fade_out = value;
                Settings.Default.Save();
            }
        }
        public int CustomAchievementInSpeed
        {
            get
            {
                return Settings.Default.alerts_custom_achievement_in_speed;
            }
            set
            {
                Settings.Default.alerts_custom_achievement_in_speed = value;
                Settings.Default.Save();
            }
        }
        public int CustomAchievementOutSpeed
        {
            get
            {
                return Settings.Default.alerts_custom_achievement_out_speed;
            }
            set
            {
                Settings.Default.alerts_custom_achievement_out_speed = value;
                Settings.Default.Save();
            }
        }
        public int CustomMasteryInSpeed
        {
            get
            {
                return Settings.Default.alerts_custom_mastery_in_speed;
            }
            set
            {
                Settings.Default.alerts_custom_mastery_in_speed = value;
                Settings.Default.Save();
            }
        }
        public int CustomMasteryOutSpeed
        {
            get
            {
                return Settings.Default.alerts_custom_mastery_out_speed;
            }
            set
            {
                Settings.Default.alerts_custom_mastery_out_speed = value;
                Settings.Default.Save();
            }
        }
        public AnimationDirection AchievementAnimationIn
        {
            get
            {
                switch (Settings.Default.notifications_achievement_in_animation)
                {
                    case "DOWN":
                        return AnimationDirection.DOWN;
                    case "LEFT":
                        return AnimationDirection.LEFT;
                    case "RIGHT":
                        return AnimationDirection.RIGHT;
                    case "UP":
                        return AnimationDirection.UP;
                }
                return AnimationDirection.STATIC;
            }
            set
            {
                Settings.Default.notifications_achievement_in_animation = value.ToString();
                Settings.Default.Save();
            }
        }
        public AnimationDirection AchievementAnimationOut
        {
            get
            {
                switch (Settings.Default.notifications_achievement_out_animation)
                {
                    case "DOWN":
                        return AnimationDirection.DOWN;
                    case "LEFT":
                        return AnimationDirection.LEFT;
                    case "RIGHT":
                        return AnimationDirection.RIGHT;
                    case "UP":
                        return AnimationDirection.UP;
                }
                return AnimationDirection.STATIC;
            }
            set
            {
                Settings.Default.notifications_achievement_out_animation = value.ToString();
                Settings.Default.Save();
            }
        }
        public AnimationDirection MasteryAnimationIn
        {
            get
            {
                switch (Settings.Default.notifications_mastery_in_animation)
                {
                    case "DOWN":
                        return AnimationDirection.DOWN;
                    case "LEFT":
                        return AnimationDirection.LEFT;
                    case "RIGHT":
                        return AnimationDirection.RIGHT;
                    case "UP":
                        return AnimationDirection.UP;
                }
                return AnimationDirection.STATIC;
            }
            set
            {
                Settings.Default.notifications_mastery_in_animation = value.ToString();
                Settings.Default.Save();
            }
        }
        public AnimationDirection MasteryAnimationOut
        {
            get
            {
                switch (Settings.Default.notifications_mastery_out_animation)
                {
                    case "DOWN":
                        return AnimationDirection.DOWN;
                    case "LEFT":
                        return AnimationDirection.LEFT;
                    case "RIGHT":
                        return AnimationDirection.RIGHT;
                    case "UP":
                        return AnimationDirection.UP;
                }
                return AnimationDirection.STATIC;
            }
            set
            {
                Settings.Default.notifications_mastery_out_animation = value.ToString();
                Settings.Default.Save();
            }
        }
        public string CustomAchievementFile
        {
            get
            {
                return Settings.Default.notification_custom_achievement_file;
            }
            set
            {
                Settings.Default.notification_custom_achievement_file = value;
                Settings.Default.Save();
            }
        }
        public string CustomMasteryFile
        {
            get
            {
                return Settings.Default.notification_custom_mastery_file;
            }
            set
            {
                Settings.Default.notification_custom_mastery_file = value;
                Settings.Default.Save();
            }
        }
        public bool AutoLaunch
        {
            get
            {
                return Settings.Default.auto_notifications;
            }
            set
            {
                Settings.Default.auto_notifications = value;
                Settings.Default.Save();
            }
        }
    }
    public class NotificationRequest
    {
        public Achievement Achievement { get; set; }
        public UserSummary UserSummary { get; set; }
        public GameInfoAndProgress GameInfoAndProgress { get; set; }
    }
}
