using AudioHub.Core.Models;
using Xunit;

namespace AudioHub.Core.Tests;

public sealed class MediaControlTests
{
    [Fact]
    public void MediaMetadata_DefaultState_ReportsNoMetadata()
    {
        // Arrange
        var metadata = new MediaMetadata();

        // Act & Assert
        Assert.False(metadata.HasMetadata);
        Assert.Equal("No Media Playing", metadata.DisplayText);
        Assert.Empty(metadata.Title);
        Assert.Empty(metadata.Artist);
        Assert.Empty(metadata.AlbumTitle);
    }

    [Fact]
    public void MediaMetadata_WithTitleAndArtist_FormatsDisplayTextCorrectly()
    {
        // Arrange
        var metadata = new MediaMetadata
        {
            Title = "Midnight City",
            Artist = "M83",
            AlbumTitle = "Hurry Up, We're Dreaming",
            TrackNumber = 5
        };

        // Act & Assert
        Assert.True(metadata.HasMetadata);
        Assert.Equal("M83 — Midnight City", metadata.DisplayText);
    }

    [Fact]
    public void MediaMetadata_WithTitleOnly_FormatsDisplayTextAsTitle()
    {
        // Arrange
        var metadata = new MediaMetadata
        {
            Title = "Podcast Episode 42"
        };

        // Act & Assert
        Assert.True(metadata.HasMetadata);
        Assert.Equal("Podcast Episode 42", metadata.DisplayText);
    }

    [Fact]
    public void MediaPlaybackInfo_PlayingStatus_ReportsIsPlayingTrue()
    {
        // Arrange
        var info = new MediaPlaybackInfo
        {
            Status = MediaPlaybackStatus.Playing,
            CanPlay = false,
            CanPause = true,
            CanSkipNext = true,
            CanSkipPrevious = true
        };

        // Act & Assert
        Assert.True(info.IsPlaying);
        Assert.Equal(MediaPlaybackStatus.Playing, info.Status);
        Assert.True(info.CanPause);
        Assert.False(info.CanPlay);
    }

    [Theory]
    [InlineData(MediaPlaybackStatus.Stopped)]
    [InlineData(MediaPlaybackStatus.Paused)]
    [InlineData(MediaPlaybackStatus.Closed)]
    [InlineData(MediaPlaybackStatus.Changing)]
    public void MediaPlaybackInfo_NonPlayingStatuses_ReportIsPlayingFalse(MediaPlaybackStatus status)
    {
        // Arrange
        var info = new MediaPlaybackInfo
        {
            Status = status
        };

        // Act & Assert
        Assert.False(info.IsPlaying);
    }

    [Fact]
    public void MediaPlaybackStateChangedEventArgs_StoresMetadataAndPlaybackInfo()
    {
        // Arrange
        var metadata = new MediaMetadata
        {
            Title = "Song A",
            Artist = "Artist B"
        };

        var info = new MediaPlaybackInfo
        {
            Status = MediaPlaybackStatus.Playing
        };

        // Act
        var args = new MediaPlaybackStateChangedEventArgs(metadata, info, "test-source-id");

        // Assert
        Assert.Same(metadata, args.Metadata);
        Assert.Same(info, args.PlaybackInfo);
        Assert.Equal("test-source-id", args.SourceOrSessionId);
    }
}
