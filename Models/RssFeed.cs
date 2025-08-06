using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace book_data_api.Models
{
    [XmlRoot("rss")]
    public class RssFeed
    {
        [XmlElement("channel")]
        public RssChannel? Channel { get; set; }
    }

    public class RssChannel
    {
        [XmlElement("xhtml:meta", Namespace = "http://www.w3.org/1999/xhtml")]
        public RssMeta? Meta { get; set; }
        
        [XmlElement("title")]
        public string? Title { get; set; }
        
        [XmlElement("copyright")]
        public string? Copyright { get; set; }
        
        [XmlElement("link")]
        public string? Link { get; set; }
        
        [XmlElement("atom:link", Namespace = "http://www.w3.org/2005/Atom")]
        public RssAtomLink? AtomLink { get; set; }
        
        [XmlElement("description")]
        public string? Description { get; set; }
        
        [XmlElement("language")]
        public string? Language { get; set; }
        
        [XmlElement("lastBuildDate")]
        public string? LastBuildDate { get; set; }
        
        [XmlElement("ttl")]
        public string? Ttl { get; set; }
        
        [XmlElement("image")]
        public RssImage? Image { get; set; }
        
        [XmlElement("item")]
        public List<RssItem>? Items { get; set; }
    }

    public class RssMeta
    {
        [XmlAttribute("name")]
        public string? Name { get; set; }
        
        [XmlAttribute("content")]
        public string? Content { get; set; }
    }

    public class RssAtomLink
    {
        [XmlAttribute("href")]
        public string? Href { get; set; }
        
        [XmlAttribute("rel")]
        public string? Rel { get; set; }
        
        [XmlAttribute("type")]
        public string? Type { get; set; }
    }

    public class RssImage
    {
        [XmlElement("title")]
        public string? Title { get; set; }
        
        [XmlElement("link")]
        public string? Link { get; set; }
        
        [XmlElement("width")]
        public string? Width { get; set; }
        
        [XmlElement("height")]
        public string? Height { get; set; }
        
        [XmlElement("url")]
        public string? Url { get; set; }
    }

    public class RssItem
    {
        [XmlElement("guid")]
        public RssGuid? Guid { get; set; }
        
        [XmlElement("pubDate")]
        public string? PubDate { get; set; }
        
        public DateTime? ParsedPubDate => !string.IsNullOrEmpty(PubDate) ? 
            DateTime.TryParse(PubDate, out DateTime result) ? DateTime.SpecifyKind(result, DateTimeKind.Utc) : null : null;
        
        [XmlElement("title")]
        public string? Title { get; set; }
        
        [XmlElement("link")]
        public string? Link { get; set; }
        
        [XmlElement("description")]
        public string? Description { get; set; }
    }

    public class RssGuid
    {
        [XmlAttribute("isPermaLink")]
        public string? IsPermaLink { get; set; }
        
        [XmlText]
        public string? Value { get; set; }
    }
} 