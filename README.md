This is a very simple program to trigger Sonarr to refresh/re-scan it's library for a specific show. 

Why would you want this? Well, if you've turned off "Completed File Handling" in Sonarr, it won't move the files automatically. This allows you to use something like FileBot (integrated with your torrent client) to do the file moving for you. However, if you do that, Sonarr will not notice that the file has been placed into the correct location. So this program takes a media file as an input parameter, and if that media file exists inside a series directory it will trigger a refresh. 


Usage:

```bash
SonarrRefresh -f "path/to/mediafile.mp4"
```
