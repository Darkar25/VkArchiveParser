using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using CsvHelper;
using EasyJSON;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using VkArchiveParser.Categories;
using VkArchiveParser.Utils;

namespace VkArchiveParser.Messages
{
    public class MessagesCategory : ICategory
    {
        public static ReadOnlyDictionary<string, string> styles = new(new Dictionary<string,string>() {
            { "Красивое", "twilight" },
            { "Розовое", "candy" },
            { "Красное", "crimson" },
            { "Голубое", "lagoon" },
            { "Новогоднее", "new_year" },
            { "Морское", "marine" },
            { "Ретровейв", "retrowave" },
            { "Оранжевое", "sunset" },
            { "Синее", "midnight" },
            { "Диско", "disco" },
            { "Нежное", "unicorn" },
            { "Мистическое", "halloween_violet" },
            { "Зелёное", "emerald" },
            { "Тыквенное", "halloween_orange" }
        });
        public MessagesCategory(string path, VkArchive archive) : base(path, archive) { }
        public override string DisplayName => "Сообщения";
        public override string CodeName => "messages";
        public override int Count => _count ??= Directory.EnumerateDirectories(InputPath).Sum(x => Directory.GetFiles(x).Length);
        public static int UrlToId(string url)
        {
            var str = url[url.LastIndexOf("/")..];
            if (str.Contains("public") || str.Contains("club")) return -int.Parse(Regex.Match(str, @"\d+").Groups[0].Value);
            return int.Parse(Regex.Match(str, @"\d+").Groups[0].Value);
        }
        public JSONObject GetCurrentUser()
        {
            JSONObject ret = new();
            var dirs = DirectoryUtils.GetDirectories(InputPath);
            if (!dirs.Any()) return ret;
            var tmp = Path.Combine(InputPath, dirs.First());
            var doc = Path.Combine(tmp, DirectoryUtils.GetFiles(tmp).First()).ParseHtml();
            var jd = doc.GetElementsByName("jd")[0] as IHtmlMetaElement;
            var b64 = Encoding.UTF8.GetString(Convert.FromBase64String(StringUtils.Base64FixPadding(jd.Content)));
            var b64j = JSON.Parse(b64);
            var currentUserId = b64j["user_id"].AsInt;
            ret["id"] = currentUserId;
            ret["first_name"] = "Вы";
            ret["last_name"] = "";
            if (dirs.Contains(currentUserId.ToString()))
            {
                tmp = Path.Combine(InputPath, currentUserId.ToString());
                var tmp3 = Path.Combine(tmp, DirectoryUtils.GetFiles(tmp).First()).ParseHtml();
                var tmp2 = tmp3.GetElementsByClassName("ui_crumb").Last().TextContent.Split(' ');
                ret["first_name"] = tmp2[0];
                ret["last_name"] = tmp2[1];
            } else
                foreach(var d in dirs)
                    foreach(var f in DirectoryUtils.GetFiles(Path.Combine(InputPath, d)))
                    {
                        var doc2 = Path.Combine(tmp, DirectoryUtils.GetFiles(tmp).First()).ParseHtml();
                        var tmp3 = doc2.QuerySelector<IHtmlAnchorElement>($"a.im_srv_lnk[href='https://vk.com/id{currentUserId}']");
                        if (tmp3 is not null)
                        {
                            var tmp4 = tmp3.TextContent.Split(' ');
                            ret["first_name"] = tmp4[0];
                            ret["last_name"] = tmp4[1];
                            goto ret;
                        }
                    }
            ret:
            return ret;
        }

        // Output columns: Id,PeerId,FromId,Out,UpdateTime,Date,Action,ActionStyle,ActionMId,ActionText,Deleted,Text
        public override void ConvertToCSV(bool merged = false)
        {
            var count = 0;
            foreach (var d in DirectoryUtils.GetDirectories(InputPath))
            {
                var peerId = int.Parse(d);
                string outputPath = Path.Combine(Parent.ParsedPath, "messages", peerId + "");
                Directory.CreateDirectory(outputPath);
                CsvWriter writer = null;
                if(merged)
                {
                    writer = new(new StreamWriter(new FileStream(Path.Combine(outputPath, "merged.csv"), FileMode.OpenOrCreate)), CultureInfo.InvariantCulture);
                    writer.WriteField("Id");
                    writer.WriteField("PeerId");
                    writer.WriteField("FromId");
                    writer.WriteField("Out");
                    writer.WriteField("UpdateTime");
                    writer.WriteField("Date");
                    writer.WriteField("Action");
                    writer.WriteField("ActionStyle");
                    writer.WriteField("ActionMId");
                    writer.WriteField("ActionText");
                    writer.WriteField("Deleted");
                    writer.WriteField("Text");
                    writer.NextRecord();
                }
                foreach (var f in DirectoryUtils.GetFiles(Path.Combine(InputPath, d)).OrderByDescending(x => int.Parse(Regex.Match(x, @"messages(\d+).html").Groups[1].Value)))
                {
                    if (!merged)
                    {
                        writer = new(new StreamWriter(new FileStream(Path.Combine(outputPath, Path.ChangeExtension(f, ".csv")), FileMode.OpenOrCreate)), CultureInfo.InvariantCulture);
                        writer.WriteField("Id");
                        writer.WriteField("PeerId");
                        writer.WriteField("FromId");
                        writer.WriteField("Out");
                        writer.WriteField("UpdateTime");
                        writer.WriteField("Date");
                        writer.WriteField("Action");
                        writer.WriteField("ActionStyle");
                        writer.WriteField("ActionMId");
                        writer.WriteField("ActionText");
                        writer.WriteField("Deleted");
                        writer.WriteField("Text");
                        writer.NextRecord();
                    }
                    var doc = Path.Combine(InputPath, d, f).ParseHtml();
                    var jd = doc.GetElementsByName("jd")[0] as IHtmlMetaElement;
                    var b64 = Encoding.UTF8.GetString(Convert.FromBase64String(StringUtils.Base64FixPadding(jd.Content)));
                    var b64j = JSON.Parse(b64);
                    var currentUserId = b64j["user_id"].AsInt;
                    var messages = doc.GetElementsByClassName("message").OrderBy(x => int.Parse(x.GetAttribute("data-id")));
                    foreach (var m in messages)
                    {
                        writer.WriteField(int.Parse(m.GetAttribute("data-id"))); //Id
                        writer.WriteField(peerId); //PeerId
                        var header = m.GetElementsByClassName("message__header")[0];
                        var links = header.GetElementsByTagName("a");
                        if (links.Length > 0)
                        {
                            var lnk = links.OfType<IHtmlAnchorElement>().First();
                            var fromId = UrlToId(lnk.Href);
                            writer.WriteField(fromId); //FromId
                            writer.WriteField(0); //Out
                        }
                        else
                        {
                            writer.WriteField(currentUserId); //FromId
                            writer.WriteField(1); //Out
                        }
                        var edited = header.GetElementsByClassName("message-edited");
                        var dateCulture = CultureInfo.CreateSpecificCulture("ru-RU");
                        //Archive month format differs from builtin version
                        dateCulture.DateTimeFormat.AbbreviatedMonthGenitiveNames = dateCulture.DateTimeFormat.AbbreviatedMonthNames = new string[] { "Янв", "Фев", "Мар", "Апр", "Мая", "Июн", "Июл", "Авг", "Сен", "Окт", "Ноя", "Дек", "" };
                        if (edited.Length > 0)
                        {
                            writer.WriteField(((DateTimeOffset)DateTime.ParseExact(edited.OfType<IHtmlSpanElement>().First().Title.Trim(), "d MMM yyyy в H:mm:ss", dateCulture)).ToUnixTimeSeconds()); //UpdateTime
                            edited[0].Remove();
                        } else
                            writer.WriteField(""); //UpdateTime
                        writer.WriteField(((DateTimeOffset)DateTime.ParseExact(header.TextContent[(header.TextContent.LastIndexOf(",") + 1)..].Trim(), "d MMM yyyy в H:mm:ss", dateCulture)).ToUnixTimeSeconds()); //Date
                        var attach = m.LastElementChild.GetElementsByClassName("kludges");
                        if (attach.Length > 0)
                        {
                            var kludg = attach[0];
                            var anchors = kludg.QuerySelectorAll<IHtmlAnchorElement>("a.im_srv_lnk");
                            if (anchors.Any())
                            {
                                var b = kludg.QuerySelectorAll("b.im_srv_lnk");
                                if (b.Length > 0)
                                {
                                    if (Regex.IsMatch(kludg.TextContent, @"создала? беседу")) writer.WriteField("chat_create", true); //Action
                                    else if (Regex.IsMatch(kludg.TextContent, @"изменила? название беседы")) writer.WriteField("chat_title_update", true); //Action
                                    writer.WriteField(""); //ActionStyle
                                    writer.WriteField(""); //ActionMId
                                    writer.WriteField(b.Last().TextContent, true); //ActionText
                                }
                                else if (anchors.Count() > 1)
                                {
                                    //Documentation has no info about this action type
                                    if (Regex.IsMatch(kludg.TextContent, @"изменила? оформление чата на"))
                                    {
                                        writer.WriteField("conversation_style_update", true); //Action
                                        writer.WriteField(styles[Regex.Match(kludg.TextContent, "«(.*)»").Groups[1].Value], true); //ActionStyle
                                        writer.WriteField(""); //ActionMId
                                        writer.WriteField(""); //ActionText
                                    }
                                    else if (Regex.IsMatch(kludg.TextContent, @"сбросила? оформление чата\."))
                                    {
                                        writer.WriteField("conversation_style_update", true); //Action
                                        writer.WriteField(""); //ActionStyle
                                        writer.WriteField(""); //ActionMId
                                        writer.WriteField(""); //ActionText
                                    }
                                    else
                                    {
                                        var lnk = anchors.Last();
                                        if (Regex.IsMatch(kludg.TextContent, @"исключила?")) writer.WriteField("chat_kick_user", true); //Action
                                        else if (Regex.IsMatch(kludg.TextContent, @"пригласила?")) writer.WriteField("chat_invite_user", true); //Action
                                        writer.WriteField(""); //ActionStyle
                                        writer.WriteField(UrlToId(lnk.Href)); //ActionMId
                                        writer.WriteField(""); //ActionText
                                    }
                                }
                                else if (Regex.IsMatch(kludg.TextContent, @"закрепила? сообщение"))
                                {
                                    writer.WriteField("chat_pin_message", true); //Action
                                    writer.WriteField(""); //ActionStyle
                                    var lnk = anchors.Last();
                                    writer.WriteField(UrlToId(lnk.Href)); //ActionMId
                                    writer.WriteField(""); //ActionText
                                }
                                else if (Regex.IsMatch(kludg.TextContent, @"открепила? сообщение"))
                                {
                                    writer.WriteField("chat_unpin_message", true); //Action
                                    writer.WriteField(""); //ActionStyle
                                    var lnk = anchors.Last();
                                    writer.WriteField(UrlToId(lnk.Href)); //ActionMId
                                    writer.WriteField(""); //ActionText
                                }
                                else if (Regex.IsMatch(kludg.TextContent, @"обновила? фотографию беседы"))
                                {
                                    writer.WriteField("chat_photo_update", true); //Action
                                    writer.WriteField(""); //ActionStyle
                                    writer.WriteField(""); //ActionMId
                                    writer.WriteField(""); //ActionText
                                }
                                else if (Regex.IsMatch(kludg.TextContent, @"удалила? фотографию беседы"))
                                {
                                    writer.WriteField("chat_photo_remove", true); //Action
                                    writer.WriteField(""); //ActionStyle
                                    writer.WriteField(""); //ActionMId
                                    writer.WriteField(""); //ActionText
                                }
                                else if (Regex.IsMatch(kludg.TextContent, @"выше?ла? из беседы"))
                                {
                                    writer.WriteField("chat_kick_user", true); //Action
                                    writer.WriteField(""); //ActionStyle
                                    var lnk = anchors.Last();
                                    writer.WriteField(UrlToId(lnk.Href)); //ActionMId
                                    writer.WriteField(""); //ActionText
                                }
                                else if (Regex.IsMatch(kludg.TextContent, @"вернула?сь?я? в беседу"))
                                {
                                    writer.WriteField("chat_invite_user", true); //Action
                                    writer.WriteField(""); //ActionStyle
                                    var lnk = anchors.Last();
                                    writer.WriteField(UrlToId(lnk.Href)); //ActionMId
                                    writer.WriteField(""); //ActionText
                                }
                                else if (Regex.IsMatch(kludg.TextContent, @"присоединила?сь?я? к беседе по ссылке"))
                                {
                                    writer.WriteField("chat_invite_user_by_link", true); //Action
                                    writer.WriteField(""); //ActionStyle
                                    writer.WriteField(""); //ActionMId
                                    writer.WriteField(""); //ActionText
                                }
                            } else
                            {
                                writer.WriteField(""); //Action
                                writer.WriteField(""); //ActionStyle
                                writer.WriteField(""); //ActionMId
                                writer.WriteField(""); //ActionText
                            }
                            var atts = kludg.QuerySelectorAll<IHtmlDivElement>(".attachment");
                            var del = false;
                            foreach (var att in atts)
                            {
                                var desc = att.QuerySelector<IHtmlDivElement>(".attachment__description");
                                if (desc.TextContent.Contains("Сообщение удалено")) {
                                    del = true;
                                    break;
                                }
                            }
                            writer.WriteField(del); //Deleted
                            kludg.Remove();
                        }
                        writer.WriteField(m.LastElementChild.TextContent, true); //Text
                        writer.NextRecord();
                    }
                    if (!merged)
                        writer.Flush();
                    ConvertProgress?.Report((Count, count++, Path.Combine(d, f)));
                }
                if (merged)
                    writer.Flush();
            }
        }

        public override void ConvertToHTML(bool merged = false)
        {
            // TODO: Make the HTML converter...
            // well, yes this is the same format as the original archive, but original archive lacks semantics and fancy stuff :3
            base.ConvertToHTML(merged);
        }

        //Output reference: https://dev.vk.com/method/messages.getHistory and https://dev.vk.com/reference/objects/message
        public override void ConvertToJSON(bool merged = false)
        {
            var count = 0;
            var currentUser = GetCurrentUser();
            foreach(var d in DirectoryUtils.GetDirectories(InputPath))
            {
                var peerId = int.Parse(d);
                string outputPath = Path.Combine(Parent.ParsedPath, "messages", peerId+"");
                Directory.CreateDirectory(outputPath);
                JSONObject result = new();
                JSONObject response = new();
                JSONArray arr = new();
                Dictionary<int, JSONObject> profiles = new();
                Dictionary<int, JSONObject> groups = new();
                Dictionary<int, JSONObject> conversations = new();
                foreach (var f in DirectoryUtils.GetFiles(Path.Combine(InputPath, d)).OrderByDescending(x => int.Parse(Regex.Match(x,@"messages(\d+).html").Groups[1].Value)))
                {
                    if (!merged)
                    {
                        result = new();
                        response = new();
                        arr = new();
                        profiles = new();
                        groups = new();
                        conversations = new();
                    }
                    var doc = Path.Combine(InputPath, d, f).ParseHtml();
                    var messages = doc.GetElementsByClassName("message").OrderBy(x => int.Parse(x.GetAttribute("data-id")));
                    if (!conversations.ContainsKey(peerId))
                    {
                        var lastmsg = int.Parse(messages.Last().GetAttribute("data-id"));
                        var conversation = new JSONObject
                        {
                            ["is_marked_unread"] = false,
                            ["important"] = false,
                            ["last_message_id"] = lastmsg,
                            ["in_read"] = lastmsg,
                            ["out_read"] = lastmsg,
                            ["sort_id"] = new JSONObject
                            {
                                ["major_id"] = 0,
                                ["minor_id"] = lastmsg
                            }
                        };
                        var peer = new JSONObject
                        {
                            ["id"] = peerId
                        };
                        if (peerId < 0)
                        {
                            peer["type"] = "group";
                            peer["local_id"] = -peerId;
                        }
                        else if (peerId > 2000000000)
                        {
                            peer["type"] = "chat";
                            peer["local_id"] = peerId - 2000000000;
                            conversation["chat_settings"] = new JSONObject
                            {
                                ["title"] = doc.GetElementsByClassName("ui_crumb").Last().TextContent,
                                ["active_ids"] = new JSONArray()
                            };
                        }
                        else
                        {
                            peer["type"] = "user";
                            peer["local_id"] = peerId;
                        }
                        conversation["peer"] = peer;
                        conversations.Add(peerId, conversation);
                    }
                    foreach (var m in messages) {
                        var node = new JSONObject
                        {
                            ["id"] = int.Parse(m.GetAttribute("data-id")),
                            ["peer_id"] = peerId,
                            ["random_id"] = 0, //Never changes
                            ["fwd_messages"] = 0, //Should be array, but we don`t get the messages themselves - only the amount of forwarded messages...
                            ["important"] = false, //Never changes
                            ["is_hidden"] = false, //Never changes
                            ["deleted"] = false //Obsolete, but must use this to indincate deleted messages
                        };
                        var header = m.GetElementsByClassName("message__header")[0];
                        var links = header.GetElementsByTagName("a");
                        if (links.Length > 0)
                        {
                            var lnk = links.OfType<IHtmlAnchorElement>().First();
                            var fromId = UrlToId(lnk.Href);
                            node["from_id"] = fromId;
                            node["out"] = 0;
                            if(fromId > 0)
                            {
                                if (!profiles.ContainsKey(fromId)) {
                                    var parts = lnk.Text.Split(" ");
                                    var profile = new JSONObject
                                    {
                                        ["id"] = fromId,
                                        ["first_name"] = parts[0],
                                        ["last_name"] = parts.Length > 1 ? parts[1] : ""
                                    };
                                    if (parts[0].Trim() == "DELETED" && parts.Length == 1)
                                    {
                                        profile["deactivated"] = "deleted";
                                        profile["online"] = 0;
                                        profile["photo_50"] = "https://vk.com/images/deactivated_50.png";
                                        profile["photo_100"] = "https://vk.com/images/deactivated_100.png";
                                        profile["online_info"] = new JSONObject
                                        {
                                            ["visible"] = true,
                                            ["is_online"] = false,
                                            ["is_mobile"] = false
                                        };
                                    }
                                    profiles.Add(fromId, profile);
                                }
                            } else if (!groups.ContainsKey(-fromId))
                                    groups.Add(-fromId, new JSONObject
                                    {
                                        ["id"] = -fromId,
                                        ["name"] = lnk.Text,
                                        ["type"] = lnk.Href switch
                                        {
                                            string a when a.Contains("public") => "page",
                                            string b when b.Contains("club") => "group",
                                            _ => throw new ArgumentException("Unknown group type")
                                        }
                                    });
                        }
                        else
                        {
                            node["from_id"] = currentUser["id"];
                            node["out"] = 1;
                            if (!profiles.ContainsKey(currentUser["id"]))
                                profiles.Add(currentUser["id"], currentUser);
                        }
                        var edited = header.GetElementsByClassName("message-edited");
                        var dateCulture = CultureInfo.CreateSpecificCulture("ru-RU");
                        //Archive month format differs from builtin version
                        dateCulture.DateTimeFormat.AbbreviatedMonthGenitiveNames = dateCulture.DateTimeFormat.AbbreviatedMonthNames = new string[] { "Янв", "Фев", "Мар", "Апр", "Мая", "Июн", "Июл", "Авг", "Сен", "Окт", "Ноя", "Дек", "" };
                        if (edited.Length > 0)
                        {
                            node["update_time"] = ((DateTimeOffset)DateTime.ParseExact(edited.OfType<IHtmlSpanElement>().First().Title.Trim(), "d MMM yyyy в H:mm:ss", dateCulture)).ToUnixTimeSeconds();
                            edited[0].Remove();
                        }
                        node["date"] = ((DateTimeOffset)DateTime.ParseExact(header.TextContent[(header.TextContent.LastIndexOf(",")+1)..].Trim(), "d MMM yyyy в H:mm:ss", dateCulture)).ToUnixTimeSeconds();
                        var attach = m.LastElementChild.GetElementsByClassName("kludges");
                        if (attach.Length > 0)
                        {
                            var kludg = attach[0];
                            var anchors = kludg.QuerySelectorAll<IHtmlAnchorElement>("a.im_srv_lnk");
                            if (anchors.Any())
                            {
                                var action = new JSONObject();
                                var b = kludg.QuerySelectorAll("b.im_srv_lnk");
                                switch (kludg.TextContent)
                                {
                                    case string a when Regex.IsMatch(a, @"закрепила? сообщение"):
                                        action["type"] = "chat_pin_message";
                                        goto member;
                                    case string a when Regex.IsMatch(a, @"открепила? сообщение"):
                                        action["type"] = "chat_unpin_message";
                                        goto member;
                                    case string a when Regex.IsMatch(a, @"сделала? скриншот беседы"):
                                        action["type"] = "chat_screenshot";
                                        goto member;
                                    case string a when Regex.IsMatch(a, @"начала? групповой звонок"):
                                        // NOTE: According to official API there is no such thing as chat_start_call action
                                        // this is JUST A PLACEHOLDER to let the parser know what this attachment is...
                                        // this attachment probably CANNOT be parsed into any of any VK library object
                                        action["type"] = "chat_start_call";
                                        goto member;
                                    case string a when Regex.IsMatch(a, @"выше?ла? из беседы"):
                                    case string c when Regex.IsMatch(c, @"исключила?"):
                                        action["type"] = "chat_kick_user";
                                        goto member;
                                    case string a when Regex.IsMatch(a, @"вернула?сь?я? в беседу"):
                                    case string c when Regex.IsMatch(c, @"пригласила?"):
                                        action["type"] = "chat_invite_user";
                                        member: action["member_id"] = UrlToId(anchors.Last().Href);
                                        break;
                                    case string a when Regex.IsMatch(a, @"обновила? фотографию беседы"):
                                        action["type"] = "chat_photo_update";
                                        break;
                                    case string a when Regex.IsMatch(a, @"удалила? фотографию беседы"):
                                        action["type"] = "chat_photo_remove";
                                        break;
                                    case string a when Regex.IsMatch(a, @"присоединила?сь?я? к беседе по ссылке"):
                                        action["type"] = "chat_invite_user_by_link";
                                        break;
                                    case string a when Regex.IsMatch(a, @"создала? беседу"):
                                        action["type"] = "chat_create";
                                        goto text;
                                    case string a when Regex.IsMatch(a, @"изменила? название беседы"):
                                        action["type"] = "chat_title_update";
                                        text: action["text"] = b.LastOrDefault()?.TextContent;
                                        break;
                                    case string a when Regex.IsMatch(a, @"изменила? оформление чата на"):
                                        action["style"] = styles[Regex.Match(kludg.TextContent, "«(.*)»").Groups[1].Value];
                                        goto style; // Wish there was fallthroughs in C#
                                    case string c when Regex.IsMatch(c, @"сбросила? оформление чата\."):
                                        style: action["type"] = "conversation_style_update";
                                        break;
                                    default:
                                        break;
                                }
                                node["action"] = action;
                            }
                            var atts = kludg.QuerySelectorAll<IHtmlDivElement>(".attachment");
                            var attachments = new JSONArray();
                            //Attachment object are incomplete, archive does not provide enough data to build full attahment objects
                            foreach (var att in atts)
                            {
                                var desc = att.QuerySelector<IHtmlDivElement>(".attachment__description");
                                Match match;
                                var attachment = new JSONObject();
                                if ((match = Regex.Match(desc.TextContent, @"(\d+) прикреплённ[оеых]+ сообщени[еяй]")).Success)
                                {
                                    node["fwd_messages"] = int.Parse(match.Groups[1].Value);
                                    continue;
                                }
                                switch(desc.TextContent)
                                {
                                    case var a when a.Contains("Фотография"):
                                        attachment[aKey: attachment["type"] = "photo"] = att.QuerySelector<IHtmlAnchorElement>("a").Href;
                                        break;
                                    case var a when a.Contains("Стикер"):
                                        attachment[aKey: attachment["type"] = "sticker"] = new JSONObject();
                                        break;
                                    case var a when a.Contains("Запись на стене"):
                                        attachment[aKey: attachment["type"] = "wall"] = new JSONObject();
                                        break;
                                    case var a when a.Contains("Подарок"):
                                        attachment[aKey: attachment["type"] = "gift"] = new JSONObject();
                                        break;
                                    case var a when a.Contains("Аудиозапись"):
                                        attachment[aKey: attachment["type"] = "audio"] = new JSONObject();
                                        break;
                                    case var a when a.Contains("Опрос"):
                                        attachment[aKey: attachment["type"] = "poll"] = new JSONObject();
                                        break;
                                    case var a when a.Contains("История"):
                                        attachment[aKey: attachment["type"] = "story"] = new JSONObject();
                                        break;
                                    case var a when a.Contains("Товар"):
                                        attachment[aKey: attachment["type"] = "market"] = new JSONObject();
                                        break;
                                    case var a when a.Contains("Комментарий на стене"):
                                        attachment[aKey: attachment["type"] = "wall_reply"] = new JSONObject();
                                        break;
                                    case var a when a.Contains("Ссылка"):
                                        attachment[aKey: attachment["type"] = "link"] = new JSONObject
                                        {
                                            ["url"] = att.QuerySelector<IHtmlAnchorElement>("a").Href
                                        };
                                        break;
                                    case var a when a.Contains("Видеозапись"):
                                        attachment[aKey: attachment["type"] = "video"] = new JSONObject
                                        {
                                            ["url"] = att.QuerySelector<IHtmlAnchorElement>("a").Href
                                        };
                                        break;
                                    case var a when a.Contains("Сообщение удалено"):
                                        node["deleted"] = true;
                                        break;
                                    case var a when a.Contains("Карта"):
                                        node["geo"] = new JSONObject
                                        {
                                            ["coordinates"] = new JSONObject(),
                                            ["type"] = "point"
                                        };
                                        break;
                                    case var a when a.Contains("Файл"):
                                        var link = att.QuerySelector<IHtmlAnchorElement>("a");
                                        var document = new JSONObject();
                                        attachment["type"] = "doc";
                                        if (link is not null)
                                        {
                                            Match fdata = Regex.Match(link.Text, @"https?://vk.com/doc(-?\d*)_(\d*)");
                                            document["url"] = link.Text;
                                            document["owner_id"] = int.Parse(fdata.Groups[1].Value);
                                            document["id"] = int.Parse(fdata.Groups[2].Value);
                                            if (link.Text.Contains(link.Href))
                                                document["type"] = 8;
                                            else if (link.Href.Contains("/amsg/"))
                                            {
                                                document["ext"] = "ogg";
                                                document["type"] = 5;
                                                document["preview"] = new JSONObject
                                                {
                                                    ["audio_message"] = new JSONObject
                                                    {
                                                        ["link_ogg"] = link.Href
                                                    }
                                                };
                                                node["was_listened"] = false;
                                            }
                                        }
                                        attachment["doc"] = document;
                                        break;
                                    case var a when a.Contains("Статья"):
                                        attachment[aKey: attachment["type"] = "link"] = new JSONObject
                                        {
                                            ["description"] = "Статья"
                                        };
                                        break;
                                    case var a when a.Contains("Сюжет"):
                                        attachment[aKey: attachment["type"] = "link"] = new JSONObject
                                        {
                                            ["caption"] = "Сюжет",
                                            ["description"] = ""
                                        };
                                        break;
                                    case var a when a.Contains("Виджет"):
                                        // NOTE: This attachment type is not guaranteed to be valid!
                                        // if you have any information about this attachment type, please open an issue on the github repo.
                                        attachment[aKey: attachment["type"] = "widget"] = new JSONObject();
                                        break;
                                    case var a when a.Contains("Плейлист"):
                                        attachment[aKey: attachment["type"] = "link"] = new JSONObject
                                        {
                                            ["caption"] = "Плейлист",
                                            ["description"] = ""
                                        };
                                        break;
                                    case var a when a.Contains("Запрос на денежный перевод"):
                                        attachment[aKey: attachment["type"] = "link"] = new JSONObject
                                        {
                                            ["caption"] = "Запрос перевода",
                                            ["description"] = ""
                                        };
                                        break;
                                    case var a when a.Contains("Группа"):
                                        attachment[aKey: attachment["type"] = "event"] = new JSONObject();
                                        break;
                                    case var a when a.Contains("Звонок"):
                                        attachment[aKey: attachment["type"] = "call"] = new JSONObject
                                        {
                                            ["initiator_id"] = node["from_id"],
                                            ["receiver_id"] = peerId > 2000000000 ? peerId : ((int)node["from_id"] == (int)currentUser["id"] ? (int)node["from_id"] : (int)currentUser["id"]),
                                            ["time"] = node["date"]
                                        };
                                        break;
                                    case var a when a.Contains("Подкаст"):
                                        var link1 = att.QuerySelector<IHtmlAnchorElement>("a");
                                        var document1 = new JSONObject();
                                        attachment["type"] = "podcast";
                                        if (link1 is not null)
                                        {
                                            Match fdata = Regex.Match(link1.Text, @"https?://vk.com/podcast(-?\d*)_(\d*)");
                                            document1["url"] = link1.Text;
                                            document1["owner_id"] = int.Parse(fdata.Groups[1].Value);
                                            document1["id"] = int.Parse(fdata.Groups[2].Value);
                                            document1["podcast_info"] = new JSONObject();
                                        }
                                        attachment["podcast"] = document1;
                                        break;
                                    default:
                                        break;
                                }
                                attachments.Add(attachment);
                            }
                            node["attachments"] = attachments;
                            kludg.Remove();
                        }
                        node["text"] = m.LastElementChild.TextContent;
                        arr.Add(node);
                    }
                    if (!merged)
                    {
                        response["count"] = arr.Count;
                        response["items"] = arr;
                        if (conversations.Any()) response["conversations"] = new JSONArray(conversations.Values);
                        if (profiles.Any()) response["profiles"] = new JSONArray(profiles.Values);
                        if (groups.Any()) response["groups"] = new JSONArray(groups.Values);
                        result["response"] = response;
                        File.WriteAllText(Path.Combine(outputPath, Path.ChangeExtension(f, ".json")), result.ToString());
                    }
                    ConvertProgress?.Report((Count, ++count, Path.Combine(d, f)));
                }
                if (merged)
                {
                    response["count"] = arr.Count;
                    response["items"] = arr;
                    if (conversations.Any()) response["conversations"] = new JSONArray(conversations.Values);
                    if (profiles.Any()) response["profiles"] = new JSONArray(profiles.Values);
                    if (groups.Any()) response["groups"] = new JSONArray(groups.Values);
                    result["response"] = response;
                    File.WriteAllText(Path.Combine(outputPath, "merged.json"), result.ToString());
                }
                // Force the GC to clean up the mess, otherwise the RAM will go trhough the roof
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
}