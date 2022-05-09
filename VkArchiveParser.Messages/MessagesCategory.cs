using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using CsvHelper;
using EasyJSON;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
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
        public static int? UrlToId(string? url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            var str = url[url.LastIndexOf("/")..];
            if (str.Contains("public") || str.Contains("club")) return -int.Parse(Regex.Match(str, @"\d+").Groups[0].Value);
            return int.Parse(Regex.Match(str, @"\d+").Groups[0].Value);
        }
        public override void PopulateCurrentUserInfo()
        {
            base.PopulateCurrentUserInfo();
            if (Parent.CurrentUser["first_name"].Tag == JSONNodeType.None || Parent.CurrentUser["last_name"].Tag == JSONNodeType.None)
            {
                Parent.CurrentUser["first_name"] = "Вы";
                Parent.CurrentUser["last_name"] = "";
                var dirs = DirectoryUtils.GetDirectories(InputPath);
                if (dirs.Contains(Parent.CurrentUser["id"].Value))
                {
                    var tmp = Path.Combine(InputPath, Parent.CurrentUser["id"].Value);
                    var tmp3 = Directory.GetFiles(tmp).First().PathHtml();
                    var tmp2 = tmp3.GetElementsByClassName("ui_crumb").Last().TextContent.Split(' ');
                    Parent.CurrentUser["first_name"] = tmp2[0];
                    Parent.CurrentUser["last_name"] = tmp2[1];
                }
                else
                    foreach (var d in dirs)
                        foreach (var f in DirectoryUtils.GetFiles(Path.Combine(InputPath, d)))
                        {
                            var doc2 = f.PathHtml();
                            var tmp3 = doc2.QuerySelector<IHtmlAnchorElement>($"a.im_srv_lnk[href='https://vk.com/id{Parent.CurrentUser["id"]}']");
                            if (tmp3 is not null)
                            {
                                var tmp4 = tmp3.TextContent.Split(' ');
                                Parent.CurrentUser["first_name"] = tmp4[0];
                                Parent.CurrentUser["last_name"] = tmp4[1];
                                return;
                            }
                        }
            }
        }

        // Output columns: Id,PeerId,FromId,FirstName,LastName,Out,UpdateTime,Date,Action,ActionStyle,ActionMId,ActionText,Deleted,Text
        public override void ConvertToCSV(bool merged = false)
        {
            var count = 0;
            foreach (var d in DirectoryUtils.GetDirectories(InputPath))
            {
                var peerId = int.Parse(d);
                string outputPath = Path.Combine(Parent.ParsedPath, Folder, peerId + "");
                Directory.CreateDirectory(outputPath);
                CsvWriter writer = null;
                if(merged)
                {
                    writer = new(new StreamWriter(new FileStream(Path.Combine(outputPath, "merged.csv"), FileMode.OpenOrCreate)), CultureInfo.InvariantCulture);
                    writer.WriteField("Id");
                    writer.WriteField("PeerId");
                    writer.WriteField("FromId");
                    writer.WriteField("FirstName");
                    writer.WriteField("LastName");
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
                        writer.WriteField("FirstName");
                        writer.WriteField("LastName");
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
                    var doc = Path.Combine(InputPath, d, f).PathHtml();
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
                            if (fromId > 0)
                            {
                                var parts = lnk.Text.Split(" ");
                                writer.WriteField(parts[0], true); //FirstName
                                writer.WriteField(parts.Length > 1 ? parts[1] : "", true); //LastName
                            }
                            else
                            {
                                writer.WriteField(lnk.Text, true); //FirstName
                                writer.WriteField(""); //LastName
                            }
                            writer.WriteField(0); //Out
                        }
                        else
                        {
                            writer.WriteField(Parent.CurrentUser["id"]); //FromId
                            writer.WriteField(Parent.CurrentUser["first_name"], true); //FirstName
                            writer.WriteField(Parent.CurrentUser["last_name"], true); //LastName
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
            // well, yes this is the same format as the original archive, but original archive lacks semantics and fancy stuff :3
            var count = 0;
            var files = DirectoryUtils.GetDirectories(InputPath).ToDictionary(x => int.Parse(x), x => DirectoryUtils.GetFiles(Path.Combine(InputPath, x)).OrderBy(x => int.Parse(Regex.Match(x, @"messages(\d+).html").Groups[1].Value)).ToArray());
            var firstMessage = files.Keys.ToDictionary(x => x, x =>
            {
                var a = files[x].Last();
                //Since we need only one single field from the entire document we don`t need to pass the entire thing through HTML parser
                return new JSONObject() {
                    ["href"] = a + "#" + Regex.Match(File.ReadAllText(Path.Combine(InputPath, x + "", a)), @"data-id=""(\d +)""", RegexOptions.RightToLeft).Groups[1].Value
                };
            });
            var lastMessage = files.Keys.ToDictionary(x => x, x =>
            {
                var a = files[x].First();
                var msg = Path.Combine(InputPath, x+"", a).PathHtml().GetElementsByClassName("message");
                var ms = msg[0];
                var header = ms.GetElementsByClassName("message__header")[0];
                header.QuerySelector(".message-edited")?.Remove();
                var u = new JSONArray();
                foreach (var i in msg.Select(x => UrlToId(x.QuerySelector<IHtmlAnchorElement>(".message__header a")?.Href) ?? Parent.CurrentUser["id"].AsInt).Append(Parent.CurrentUser["id"].AsInt).Distinct())
                    u.Add(i);
                return new JSONObject()
                {
                    ["href"] = a + "#" + ms.GetAttribute("data-id"),
                    ["text"] = ms.LastElementChild.TextContent,
                    ["hasAttachment"] = ms.LastElementChild.GetElementsByClassName("kludges").Any(),
                    ["sender"] = UrlToId(header.QuerySelector<IHtmlAnchorElement>("a")?.Href) ?? Parent.CurrentUser["id"].AsInt,
                    ["users"] = u,
                    ["date"] = ((DateTimeOffset)DateTime.ParseExact(header.TextContent[(header.TextContent.LastIndexOf(",") + 1)..].Trim(), "d MMM yyyy в H:mm:ss", VkArchive.DateCulture)).ToUnixTimeSeconds()
                };
            });
            var templatePeerList = Properties.Resources.template_index_messages.StringHtml();
            (templatePeerList.GetElementsByName("user_id").First() as IHtmlMetaElement).Content = Parent.CurrentUser["id"];
            var peerList = templatePeerList.QuerySelector<IHtmlUnorderedListElement>("ul");
            foreach (var p in lastMessage.OrderByDescending(x => (long)x.Value["date"]))
            {
                var peer = templatePeerList.CreateElement<IHtmlListItemElement>();
                var a = templatePeerList.CreateElement<IHtmlAnchorElement>();
                a.Id = "peer";
                a.Href = Path.Combine(p.Key+"", p.Value["href"]);
                a.SetAttribute("data-peer_id", p.Key+"");
                var ic = templatePeerList.CreateElement<IHtmlDivElement>();
                ic.ClassName = "im_peer_icon rowspan2 " + (p.Key > 2000000000 ? (p.Value["users"].AsArray.Count switch {
                    2 => "chat_2",
                    3 => "chat_3",
                    _ => "chat_4",
                }) : "");
                ic.SetAttribute("style", p.Key > 2000000000 ? string.Join(";", p.Value["users"].AsArray.Linq.Take(4).Select((x, i) => "--user-id" + (i + 1) + ": " + x.Value.AsInt)) : ("--user-id1: " + p.Key));
                var strong = templatePeerList.CreateElement("strong");
                strong.Id = "peer_title";
                strong.TextContent = Path.Combine(InputPath, p.Key+"", files[p.Key].First()).PathHtml().GetElementsByClassName("ui_crumb").Last().TextContent;
                var time = templatePeerList.CreateElement<IHtmlTimeElement>();
                var dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds((long)p.Value["date"]).ToLocalTime();
                time.DateTime = dt.ToString("yyyy-MM-dd HH:mm:ss");
                time.TextContent = dt.ToString("dd MMM yyyy", VkArchive.DateCulture);
                var message = templatePeerList.CreateElement<IHtmlDivElement>();
                message.ClassName = "im_peer_message";
                message.SetAttribute("data-id", p.Value["href"].Value.Split('#')[1]);
                message.SetAttribute("data-user_id", p.Value["sender"].Value);
                if (p.Value["sender"].AsInt != p.Key) {
                    var message_icon = templatePeerList.CreateElement<IHtmlDivElement>();
                    message_icon.ClassName = "im_peer_icon im_peer_message_icon";
                    message_icon.SetAttribute("style", "--user-id1: " + p.Value["sender"].Value);
                    message.AppendChild(message_icon);
                }
                var text = templatePeerList.CreateElement<IHtmlSpanElement>();
                text.ClassName = "im_message_text";
                text.TextContent = p.Value["text"].Value;
                message.AppendChild(text);
                a.Append(ic , strong, time, message);
                peer.Append(a);
                peerList.AppendChild(peer);
                var template = Properties.Resources.template_messages.StringHtml();
                var index = 0;
                string[] pages = new string[files[p.Key].Length];
                var e = files[p.Key].GetEnumerator();
                DateOnly currentDayGroup = new();
                var currentSender = 0;
                IHtmlDivElement currentDayGroupElement = null;
                IHtmlDivElement currentMessageGroupElement = null;
                foreach (var f in files[p.Key])
                {
                    if (!merged)
                    {
                        template = Properties.Resources.template_messages.StringHtml();
                        currentDayGroup = new();
                        currentSender = 0;
                        currentDayGroupElement = null;
                        currentMessageGroupElement = null;
                    }
                    template.QuerySelector<IHtmlMetaElement>("meta[name=user_id]").Content = Parent.CurrentUser["id"];
                    template.QuerySelector<IHtmlMetaElement>("meta[name=peer_id]").Content = p.Key+"";
                    var pic = template.QuerySelector<IHtmlDivElement>("#peer_icon");
                    pic.ClassName = ic.ClassName;
                    pic.SetAttribute("style", ic.GetAttribute("style"));
                    template.QuerySelector("#peer_title").TextContent = strong.TextContent;
                    template.QuerySelector<IHtmlAnchorElement>("#totop_button").Href = firstMessage[p.Key]["href"];
                    template.QuerySelector<IHtmlAnchorElement>("#tobottom_button").Href = p.Value["href"];
                    if (!merged)
                    {
                        var nav = template.GetElementsByTagName("nav")[0];
                        var pg = (0..(files[p.Key].Length - 1)).GetPagination(index);
                        foreach (var page in pg)
                        {
                            var pag = template.CreateElement<IHtmlAnchorElement>();
                            pag.Href = pages[page] ??= (files[p.Key][page] + "#" + Path.Combine(InputPath, p.Key + "", files[p.Key][page]).PathHtml().QuerySelector(".message").GetAttribute("data-id"));
                            pag.TextContent = page + 1 + "";
                            if (page == index) pag.ClassName = "active";
                            nav.AppendChild(pag);
                        }
                    }
                    var doc = Path.Combine(InputPath, p.Key + "", f).PathHtml();
                    var messages = doc.GetElementsByClassName("message").OrderBy(x => int.Parse(x.GetAttribute("data-id")));
                    foreach (var m in messages)
                    {
                        var node = template.CreateElement<IHtmlListItemElement>();
                        node.Id = m.GetAttribute("data-id");
                        var header = m.GetElementsByClassName("message__header")[0];
                        var edited = header.GetElementsByClassName("message-edited");
                        if (edited.Length > 0)
                        {
                            var edit = template.CreateElement<IHtmlTimeElement>();
                            var edittime = DateTime.ParseExact(edited.OfType<IHtmlSpanElement>().First().Title.Trim(), "d MMM yyyy в H:mm:ss", VkArchive.DateCulture);
                            edit.DateTime = edittime.ToString("yyyy-MM-dd HH:mm:ss");
                            edit.TextContent = "(ред.)";
                            edit.Title = edittime.ToString("Изменено dd MMM yyyy в HH:mm:ss", VkArchive.DateCulture);
                            node.AppendChild(edit);
                            edited[0].Remove();
                        }
                        var date = DateTime.ParseExact(header.TextContent[(header.TextContent.LastIndexOf(",") + 1)..].Trim(), "d MMM yyyy в H:mm:ss", VkArchive.DateCulture);
                        var links = header.GetElementsByTagName("a");
                        if (links.Length > 0)
                        {
                            var lnk = links.OfType<IHtmlAnchorElement>().First();
                            var fromId = UrlToId(lnk.Href).Value;
                            if (fromId != currentSender || DateOnly.FromDateTime(date) != currentDayGroup)
                            {
                                currentSender = fromId;
                                if (currentMessageGroupElement is not null && currentMessageGroupElement.QuerySelector<IHtmlUnorderedListElement>("#messages").ChildElementCount > 0)
                                    currentDayGroupElement.AppendChild(currentMessageGroupElement);
                                currentMessageGroupElement = NewMessageGroup(currentSender, lnk.Href, lnk.TextContent, int.Parse(node.Id), date);
                            }
                        }
                        else
                        {
                            if (Parent.CurrentUser["id"].AsInt != currentSender || DateOnly.FromDateTime(date) != currentDayGroup)
                            {
                                currentSender = Parent.CurrentUser["id"].AsInt;
                                if (currentMessageGroupElement is not null && currentMessageGroupElement.QuerySelector<IHtmlUnorderedListElement>("#messages").ChildElementCount > 0)
                                    currentDayGroupElement.AppendChild(currentMessageGroupElement);
                                currentMessageGroupElement = NewMessageGroup(currentSender, "https://vk.com/id" + Parent.CurrentUser["id"].Value, Parent.CurrentUser["first_name"] + " " + Parent.CurrentUser["last_name"], int.Parse(node.Id), date);
                            }
                        }
                        if (DateOnly.FromDateTime(date) != currentDayGroup)
                        {
                            currentDayGroup = DateOnly.FromDateTime(date);
                            if (currentDayGroupElement is not null)
                                template.QuerySelector("main").AppendChild(currentDayGroupElement);
                            currentDayGroupElement = template.CreateElement<IHtmlDivElement>();
                            currentDayGroupElement.ClassName = "im_day_group";
                            var timee = template.CreateElement<IHtmlTimeElement>();
                            timee.ClassName = "peer_time_sticky";
                            timee.DateTime = currentDayGroup.ToString("yyyy-MM-dd");
                            timee.TextContent = currentDayGroup.ToString("dd MMM yyyy", VkArchive.DateCulture);
                            currentDayGroupElement.AppendChild(timee);
                        }
                        var attach = m.LastElementChild.GetElementsByClassName("kludges");
                        if (attach.Length > 0)
                        {
                            var kludg = attach[0];
                            var anchors = kludg.QuerySelectorAll<IHtmlAnchorElement>("a.im_srv_lnk");
                            if (anchors.Any())
                            {
                                if (currentMessageGroupElement is not null && currentMessageGroupElement.QuerySelector<IHtmlUnorderedListElement>("#messages").ChildElementCount > 0)
                                    currentDayGroupElement.AppendChild(currentMessageGroupElement);
                                currentMessageGroupElement = null;
                                var act = template.CreateElement<IHtmlDivElement>();
                                act.ClassName = "action";
                                act.Id = m.GetAttribute("data-id");
                                var acta = template.CreateElement<IHtmlAnchorElement>();
                                var acnhor1 = anchors.First();
                                acta.Href = acnhor1.Href;
                                act.SetAttribute("data-user_id", UrlToId(acnhor1.Href).ToString());
                                acta.Target = "_root";
                                var actab = template.CreateElement("strong");
                                actab.TextContent = acnhor1.TextContent;
                                actab.Id = "action_sender_title";
                                anchors = anchors.Skip(1);
                                acnhor1.Remove();
                                acta.AppendChild(actab);
                                var lastAnchor = anchors.LastOrDefault();
                                if (lastAnchor is not null) lastAnchor.Remove();
                                var b = kludg.QuerySelectorAll("b.im_srv_lnk");
                                act.TextContent = kludg.TextContent;
                                switch (kludg.TextContent)
                                {
                                    case string s when Regex.IsMatch(s, @"закрепила? сообщение"):
                                        act.SetAttribute("data-action", "chat_pin_message");
                                        goto member;
                                    case string s when Regex.IsMatch(s, @"открепила? сообщение"):
                                        act.SetAttribute("data-action", "chat_unpin_message");
                                        goto member;
                                    case string s when Regex.IsMatch(s, @"сделала? скриншот беседы"):
                                        act.SetAttribute("data-action", "chat_screenshot");
                                        goto member;
                                    case string s when Regex.IsMatch(s, @"начала? групповой звонок"):
                                        // NOTE: According to official API there is no such thing as chat_start_call action
                                        // this is JUST A PLACEHOLDER to let the parser know what this attachment is...
                                        // this attachment probably CANNOT be parsed into any of any VK library object
                                        act.SetAttribute("data-action", "chat_start_call");
                                        goto member;
                                    case string s when Regex.IsMatch(s, @"выше?ла? из беседы"):
                                    case string s2 when Regex.IsMatch(s2, @"исключила?"):
                                        act.SetAttribute("data-action", "chat_kick_user");
                                        goto member;
                                    case string s when Regex.IsMatch(s, @"вернула?сь?я? в беседу"):
                                    case string s2 when Regex.IsMatch(s2, @"пригласила?"):
                                        act.SetAttribute("data-action", "chat_invite_user");
                                    member: if (lastAnchor is not null)
                                        {
                                            var member = template.CreateElement<IHtmlAnchorElement>();
                                            member.Href = lastAnchor.Href;
                                            member.Id = "action_member";
                                            member.SetAttribute("data-action-member", UrlToId(lastAnchor.Href).ToString());
                                            var mb = template.CreateElement("strong");
                                            mb.TextContent = lastAnchor.TextContent;
                                            mb.Id = "action_member_title";
                                            member.AppendChild(mb);
                                            act.AppendChild(member);
                                        }
                                        break;
                                    case string s when Regex.IsMatch(s, @"обновила? фотографию беседы"):
                                        act.SetAttribute("data-action", "chat_photo_update");
                                        break;
                                    case string s when Regex.IsMatch(s, @"удалила? фотографию беседы"):
                                        act.SetAttribute("data-action", "chat_photo_remove");
                                        break;
                                    case string s when Regex.IsMatch(s, @"присоединила?сь?я? к беседе по ссылке"):
                                        act.SetAttribute("data-action", "chat_invite_user_by_link");
                                        break;
                                    case string s when Regex.IsMatch(s, @"создала? беседу"):
                                        act.SetAttribute("data-action", "chat_create");
                                        goto text;
                                    case string s when Regex.IsMatch(s, @"изменила? название беседы"):
                                        act.SetAttribute("data-action", "chat_title_update");
                                    text: act.InnerHtml = Regex.Replace(kludg.TextContent, "«([^«»]*)»", "«<strong id=\"action_text\">$1</strong>»");
                                        break;
                                    case string s when Regex.IsMatch(s, @"изменила? оформление чата на"):
                                        act.InnerHtml = Regex.Replace(kludg.TextContent, "«([^«»]*)»", "«<strong id=\"action_style\" data-action-style=\"" + styles[Regex.Match(kludg.TextContent, "«(.*)»").Groups[1].Value] + "\">$1</strong>»");
                                        goto style; // Wish there was fallthroughs in C#
                                    case string s when Regex.IsMatch(s, @"сбросила? оформление чата\."):
                                    style: act.SetAttribute("data-action", "conversation_style_update");
                                        break;
                                    default:
                                        break;
                                }
                                act.Prepend(acta);
                                currentDayGroupElement.AppendChild(act);
                                goto skipattachments;
                            }
                            var atts = kludg.QuerySelectorAll<IHtmlDivElement>(".attachment");
                            var attachments = template.CreateElement<IHtmlDivElement>();
                            attachments.ClassName = "attachments";
                            //Attachment object are incomplete, archive does not provide enough data to build full attahment objects
                            // TODO: HTML Attachments
                            foreach (var att in atts)
                            {
                                var desc = att.QuerySelector<IHtmlDivElement>(".attachment__description");
                                Match match;
                                if ((match = Regex.Match(desc.TextContent, @"(\d+) прикреплённ[оеых]+ сообщени[еяй]")).Success) {
                                    var forwarded = template.CreateElement("strong");
                                    forwarded.ClassName = "forwarded";
                                    forwarded.TextContent = match.Groups[1].Value + " Прикреплённых сообщений";
                                    attachments.AppendChild(forwarded);
                                }
                                switch (desc.TextContent)
                                {
                                    case var s when s.Contains("Фотография"):
                                        var src = att.QuerySelector<IHtmlAnchorElement>("a").Href;
                                        var photo = template.CreateElement<IHtmlAnchorElement>();
                                        photo.Target = "_root";
                                        photo.ClassName = "attach_img";
                                        photo.Href = src;
                                        var image = template.CreateElement<IHtmlImageElement>();
                                        image.Source = src;
                                        photo.AppendChild(image);
                                        attachments.AppendChild(photo);
                                        break;
                                    case var s when s.Contains("Стикер"):
                                        var sticker = template.CreateElement<IHtmlDivElement>();
                                        sticker.ClassName = "attach_icon generic";
                                        sticker.TextContent = "Стикер";
                                        attachments.AppendChild(sticker);
                                        break;
                                    case var s when s.Contains("Запись на стене"):
                                        var wall = template.CreateElement<IHtmlDivElement>();
                                        wall.ClassName = "attach_icon generic";
                                        wall.TextContent = "Запись на стене";
                                        attachments.AppendChild(wall);
                                        break;
                                    case var s when s.Contains("Подарок"):
                                        var gift = template.CreateElement<IHtmlDivElement>();
                                        gift.ClassName = "attach_icon generic";
                                        gift.TextContent = "Подарок";
                                        attachments.AppendChild(gift);
                                        break;
                                    case var s when s.Contains("Аудиозапись"):
                                        var audio = template.CreateElement<IHtmlDivElement>();
                                        audio.ClassName = "attach_icon audio";
                                        audio.TextContent = "Аудиозапись";
                                        attachments.AppendChild(audio);
                                        break;
                                    case var s when s.Contains("Опрос"):
                                        var poll = template.CreateElement<IHtmlDivElement>();
                                        poll.ClassName = "attach_icon generic";
                                        poll.TextContent = "Опрос";
                                        attachments.AppendChild(poll);
                                        break;
                                    case var s when s.Contains("История"):
                                        var story = template.CreateElement<IHtmlDivElement>();
                                        story.ClassName = "attach_icon video";
                                        story.TextContent = "История";
                                        attachments.AppendChild(story);
                                        break;
                                    case var s when s.Contains("Товар"):
                                        var item = template.CreateElement<IHtmlDivElement>();
                                        item.ClassName = "attach_icon generic";
                                        item.TextContent = "Товар";
                                        attachments.AppendChild(item);
                                        break;
                                    case var s when s.Contains("Комментарий на стене"):
                                        var comment = template.CreateElement<IHtmlDivElement>();
                                        comment.ClassName = "attach_icon generic";
                                        comment.TextContent = "Комментарий на стене";
                                        attachments.AppendChild(comment);
                                        break;
                                    case var s when s.Contains("Ссылка"):
                                        var url = template.CreateElement<IHtmlAnchorElement>();
                                        url.Href = att.QuerySelector<IHtmlAnchorElement>("a").Href;
                                        url.Target = "_root";
                                        url.ClassName = "attach_icon generic";
                                        url.TextContent = "Ссылка";
                                        attachments.AppendChild(url);
                                        break;
                                    case var s when s.Contains("Видеозапись"):
                                        var video = template.CreateElement<IHtmlAnchorElement>();
                                        video.Target = "_root";
                                        video.ClassName = "attach_icon video";
                                        video.TextContent = "Видеозапись";
                                        video.Href = att.QuerySelector<IHtmlAnchorElement>("a").Href;
                                        attachments.AppendChild(video);
                                        break;
                                    case var s when s.Contains("Сообщение удалено"):
                                        node.SetAttribute("data-deleted", "true");
                                        node.InnerHtml = "<span style=\"color: red\">Сообщение удалено</span>";
                                        break;
                                    case var s when s.Contains("Карта"):
                                        var map = template.CreateElement<IHtmlDivElement>();
                                        map.ClassName = "attach_icon generic";
                                        map.TextContent = "Карта";
                                        attachments.AppendChild(map);
                                        break;
                                    case var s when s.Contains("Файл"):
                                        var link = att.QuerySelector<IHtmlAnchorElement>("a");
                                        if (link is not null)
                                        {
                                            Match fdata = Regex.Match(link.Text, @"https?://vk.com/doc(-?\d*)_(\d*)");
                                            if (link.Text.Contains(link.Href))
                                            {
                                                var file = template.CreateElement<IHtmlAnchorElement>();
                                                file.Target = "_root";
                                                file.Href = link.Href;
                                                file.ClassName = "attach_icon doc";
                                                file.TextContent = "Файл";
                                                file.SetAttribute("data-owder_id", fdata.Groups[1].Value);
                                                file.SetAttribute("data-id", fdata.Groups[2].Value);
                                                attachments.AppendChild(file);
                                            }
                                            else if (link.Href.Contains("/amsg/"))
                                            {
                                                var amsg = template.CreateElement<IHtmlAudioElement>();
                                                amsg.Source = link.Href;
                                                amsg.IsShowingControls = true;
                                                amsg.SetAttribute("data-owder_id", fdata.Groups[1].Value);
                                                amsg.SetAttribute("data-id", fdata.Groups[2].Value);
                                                attachments.AppendChild(amsg);
                                            }
                                        }
                                        break;
                                    case var s when s.Contains("Статья"):
                                        var book = template.CreateElement<IHtmlDivElement>();
                                        book.ClassName = "attach_icon book";
                                        book.TextContent = "Статья";
                                        attachments.AppendChild(book);
                                        break;
                                    case var s when s.Contains("Сюжет"):
                                        var lnk = template.CreateElement<IHtmlDivElement>();
                                        lnk.ClassName = "attach_icon video";
                                        lnk.TextContent = "Сюжет";
                                        attachments.AppendChild(lnk);
                                        break;
                                    case var s when s.Contains("Виджет"):
                                        var widget = template.CreateElement<IHtmlDivElement>();
                                        widget.ClassName = "attach_icon generic";
                                        widget.TextContent = "Виджет";
                                        attachments.AppendChild(widget);
                                        break;
                                    case var s when s.Contains("Плейлист"):
                                        var playlist = template.CreateElement<IHtmlDivElement>();
                                        playlist.ClassName = "attach_icon generic";
                                        playlist.TextContent = "Плейлист";
                                        attachments.AppendChild(playlist);
                                        break;
                                    case var s when s.Contains("Запрос на денежный перевод"):
                                        var money = template.CreateElement<IHtmlDivElement>();
                                        money.ClassName = "attach_icon generic";
                                        money.TextContent = "Запрос на денежный перевод";
                                        attachments.AppendChild(money);
                                        break;
                                    case var s when s.Contains("Группа"):
                                        var group = template.CreateElement<IHtmlDivElement>();
                                        group.ClassName = "attach_icon generic";
                                        group.TextContent = "Группа";
                                        attachments.AppendChild(group);
                                        break;
                                    case var s when s.Contains("Звонок"):
                                        var call = template.CreateElement<IHtmlDivElement>();
                                        call.ClassName = "attach_icon generic";
                                        call.TextContent = "Звонок";
                                        attachments.AppendChild(call);
                                        break;
                                    case var s when s.Contains("Подкаст"):
                                        var link1 = att.QuerySelector<IHtmlAnchorElement>("a");
                                        if (link1 is not null)
                                        {
                                            var podcast = template.CreateElement<IHtmlAnchorElement>();
                                            Match fdata = Regex.Match(link1.Text, @"https?://vk.com/podcast(-?\d*)_(\d*)");
                                            podcast.Target = "_root";
                                            podcast.ClassName = "attach_icon audio";
                                            podcast.Href = link1.Text;
                                            podcast.TextContent = "Подкаст";
                                            podcast.SetAttribute("data-owder_id", fdata.Groups[1].Value);
                                            podcast.SetAttribute("data-id", fdata.Groups[2].Value);
                                            attachments.AppendChild(podcast);
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            }
                            if (attachments.ChildElementCount > 0)
                                node.AppendChild(attachments);
                            skipattachments: kludg.Remove();
                        }
                        var text2 = template.CreateElement<IHtmlSpanElement>();
                        var cont = WebUtility.HtmlDecode(m.LastElementChild.InnerHtml);
                        cont = Regex.Replace(cont, @"((https:\/\/)|(http:\/\/))([-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b[-a-zA-Z0-9@:%_\+.~#?&//=]*)", @"<a id=""im_mention"" href=""$1$4"" target=""_root"">$0</a>");
                        cont = Regex.Replace(cont, @"\[([^|\]\[]+)\|([^\]]+)\]", @"<a href=""https://vk.com/$1"" target=""_root"">$2</a>");
                        text2.InnerHtml = cont;
                        text2.Id = "im_message_text";
                        node.Prepend(text2);
                        currentMessageGroupElement?.QuerySelector<IHtmlUnorderedListElement>("#messages").AppendChild(node);
                    }
                    if (!merged)
                    {
                        if(currentMessageGroupElement is not null) currentDayGroupElement.AppendChild(currentMessageGroupElement);
                        template.QuerySelector("main").AppendChild(currentDayGroupElement);
                        File.WriteAllText(Path.Combine(Parent.ParsedPath, Folder, p.Key + "", f), template.ToHtml());
                    }
                    index++;
                    ConvertProgress?.Report((Count, ++count, Path.Combine(p.Key+"", f)));
                }
                if (merged)
                {
                    if (currentMessageGroupElement is not null) currentDayGroupElement.AppendChild(currentMessageGroupElement);
                    template.QuerySelector("main").AppendChild(currentDayGroupElement);
                    File.WriteAllText(Path.Combine(Parent.ParsedPath, Folder, p.Key + "", "merged.html"), template.ToHtml());
                }
            }
            File.WriteAllText(Path.Combine(Parent.ParsedPath, Folder, "index-messages.html"), templatePeerList.ToHtml());
            File.WriteAllText(Path.Combine(Parent.ParsedPath, Folder, "messages.css"), Properties.Resources.style_messages);
            File.WriteAllBytes(Path.Combine(Parent.ParsedPath, "messages.svg"), Properties.Resources.category_messages);
            File.WriteAllBytes(Path.Combine(Parent.ParsedPath, "generic_attachment.svg"), Properties.Resources.misc_generic_attachment);

            IHtmlDivElement NewMessageGroup(int sender, string senderHref, string senderTitle, int messageId, DateTime date)
            {
                var m = Properties.Resources.template_messages_message_group.StringHtml().QuerySelector<IHtmlDivElement>(".im_message_group");
                m.SetAttribute("data-user_id", sender + "");
                var ic_a = m.QuerySelector<IHtmlAnchorElement>("#im_user_icon");
                ic_a.Target = "_root";
                ic_a.Href = senderHref;
                ic_a.QuerySelector<IHtmlDivElement>("div").SetAttribute("style", "--user-id1: " + sender + ";");
                var mt = m.QuerySelector<IHtmlAnchorElement>("#message_title");
                mt.Target = "_root";
                mt.Href = senderHref;
                m.QuerySelector("#user_name").TextContent = senderTitle;
                m.QuerySelector<IHtmlAnchorElement>("#message_time").Href = "#" + messageId;
                var d = m.QuerySelector<IHtmlTimeElement>("time");
                d.DateTime = date.ToString("yyyy-MM-dd HH:mm:ss");
                d.TextContent = date.ToString("HH:ss");
                return m;
            }
        }

        //Output reference: https://dev.vk.com/method/messages.getHistory and https://dev.vk.com/reference/objects/message
        public override void ConvertToJSON(bool merged = false)
        {
            var count = 0;
            foreach(var d in DirectoryUtils.GetDirectories(InputPath))
            {
                var peerId = int.Parse(d);
                string outputPath = Path.Combine(Parent.ParsedPath, Folder, peerId+"");
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
                    var doc = Path.Combine(InputPath, d, f).PathHtml();
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
                            var fromId = UrlToId(lnk.Href).Value;
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
                            node["from_id"] = Parent.CurrentUser["id"];
                            node["out"] = 1;
                            if (!profiles.ContainsKey(Parent.CurrentUser["id"]))
                                profiles.Add(Parent.CurrentUser["id"], Parent.CurrentUser);
                        }
                        var edited = header.GetElementsByClassName("message-edited");
                        if (edited.Length > 0)
                        {
                            node["update_time"] = ((DateTimeOffset)DateTime.ParseExact(edited.OfType<IHtmlSpanElement>().First().Title.Trim(), "d MMM yyyy в H:mm:ss", VkArchive.DateCulture)).ToUnixTimeSeconds();
                            edited[0].Remove();
                        }
                        node["date"] = ((DateTimeOffset)DateTime.ParseExact(header.TextContent[(header.TextContent.LastIndexOf(",")+1)..].Trim(), "d MMM yyyy в H:mm:ss", VkArchive.DateCulture)).ToUnixTimeSeconds();
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
                                            ["receiver_id"] = peerId > 2000000000 ? peerId : (node["from_id"].AsInt == Parent.CurrentUser["id"].AsInt ? node["from_id"].AsInt : Parent.CurrentUser["id"].AsInt),
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