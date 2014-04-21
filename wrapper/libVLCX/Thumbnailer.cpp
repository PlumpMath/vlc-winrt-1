/*****************************************************************************
* Copyright � 2014 VideoLAN
*
* Authors: Kellen Sunderland
*
* This program is free software; you can redistribute it and/or modify it
* under the terms of the GNU Lesser General Public License as published by
* the Free Software Foundation; either version 2.1 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU Lesser General Public License for more details.
*
* You should have received a copy of the GNU Lesser General Public License
* along with this program; if not, write to the Free Software Foundation,
* Inc., 51 Franklin Street, Fifth Floor, Boston MA 02110-1301, USA.
*****************************************************************************/

#include "pch.h"
#include "Thumbnailer.h"

using namespace libVLCX;

//TODO: dynamic size
#define SEEK_POSITION 0.5f
#define PIXEL_SIZE 4 /* RGBA */

enum thumbnail_state{
    THUMB_SEEKING,
    THUMB_SEEKED,
    THUMB_FIRST_FRAME_DROPPED
};

typedef struct
{
    thumbnail_state                       state;
    task_completion_event<WriteableBitmap^> screenshotCompleteEvent;
    char                                    *thumbData;
    unsigned                                thumbHeight;
    unsigned                                thumbWidth;
    size_t                                  thumbSize;
    HANDLE                                  hLock;
    libvlc_media_player_t*                  mp;
} thumbnailer_sys_t;

Thumbnailer::Thumbnailer()
{
    const char *argv [] = {
        "-I", "dummy",            // Only use options needed for snapshots
        "--vout", "dummy",
        "--no-osd",
        "--verbose=5",
        "--no-video-title-show",
        "--no-stats",
        "--no-audio"
    };
    p_instance = libvlc_new(sizeof(argv) / sizeof(*argv), argv);
    if (!p_instance) {
        //TODO: better error code
        throw ref new Platform::Exception(-1, "Could not initialise libvlc!");
    }
}

/**
* Thumbnailer vout lock
**/
static void *Lock(void *opaque, void **pixels)
{
    thumbnailer_sys_t *sys = (thumbnailer_sys_t*)opaque;
    WaitForSingleObjectEx(sys->hLock, INFINITE, TRUE);
    *pixels = sys->thumbData;
    if (sys->mp && sys->state == THUMB_SEEKING
        && libvlc_media_player_is_playing(sys->mp)
        && libvlc_media_player_get_position(sys->mp) >= SEEK_POSITION){
        sys->state = THUMB_SEEKED;
    }
    return NULL;
}

static WriteableBitmap^ CopyToBitmap(thumbnailer_sys_t* sys)
{
    WriteableBitmap^ bmp = ref new WriteableBitmap(sys->thumbWidth, sys->thumbHeight);
    IBuffer^ pixelBuffer = bmp->PixelBuffer;
    ComPtr<IInspectable> pixelBufferInspectable(reinterpret_cast<IInspectable*>(pixelBuffer));
    ComPtr<IBufferByteAccess> pixelBytes;
    HRESULT hr = pixelBufferInspectable.As(&pixelBytes);
    byte* modifyablePixels(nullptr);
    hr = pixelBytes->Buffer(&modifyablePixels);

    for (unsigned int i = 0; i < sys->thumbSize; i += 4){
        //B
        modifyablePixels[i] = sys->thumbData[i + 2];
        //G
        modifyablePixels[i + 1] = sys->thumbData[i + 1];
        //R
        modifyablePixels[i + 2] = sys->thumbData[i];
        //Alpha
        modifyablePixels[i + 3] = sys->thumbData[i + 3];
    }

    bmp->Invalidate();
    return bmp;
}

/**
* Thumbnailer vout unlock
**/
static void Unlock(void *opaque, void *picture, void *const *pixels){
    thumbnailer_sys_t* sys = (thumbnailer_sys_t*) opaque;

    int state = sys->state;
    if (state == THUMB_SEEKED)
    {
        sys->state = THUMB_FIRST_FRAME_DROPPED;
    }

    if (state != THUMB_FIRST_FRAME_DROPPED)
    {
        SetEvent(sys->hLock);
        return;
    }
   
    CoreWindow^ window = CoreApplication::MainView->CoreWindow;
    auto dispatcher = window->Dispatcher;
    auto priority = CoreDispatcherPriority::Low;
    dispatcher->RunAsync(priority, ref new DispatchedHandler([=]()
    {
        WriteableBitmap^ bitmap = CopyToBitmap(sys);
        sys->screenshotCompleteEvent.set(bitmap);
        SetEvent(sys->hLock);
    }));
}

IAsyncOperation<WriteableBitmap^>^ Thumbnailer::TakeScreenshot(Platform::String^ mrl, int width, int height)
{
    return concurrency::create_async([&] (concurrency::cancellation_token ct) {

        libvlc_media_player_t* mp;
        thumbnailer_sys_t *sys = new thumbnailer_sys_t();
        auto completionTask = concurrency::create_task(sys->screenshotCompleteEvent, ct);

        size_t len2 = WideCharToMultiByte(CP_UTF8, 0, mrl->Data(), -1, NULL, 0, NULL, NULL);
        char* mrl_str = new char[len2];
        WideCharToMultiByte(CP_UTF8, 0, mrl->Data(), -1, mrl_str, len2, NULL, NULL);

        if (p_instance){
            libvlc_media_t* m = libvlc_media_new_location(this->p_instance, mrl_str);

            /* Set media to fast with no options */
            libvlc_media_add_option(m, ":no-audio");
            libvlc_media_add_option(m, ":no-spu");
            libvlc_media_add_option(m, ":no-osd");

            mp = libvlc_media_player_new_from_media(m);
            libvlc_media_release(m);

            /* Set the video format and the callbacks. */
            unsigned int pitch = width * PIXEL_SIZE;
            sys->thumbHeight = height;
            sys->thumbWidth = width;
            sys->thumbSize = pitch * sys->thumbHeight;
            sys->thumbData = (char*) malloc(sys->thumbSize);
            sys->hLock = CreateEventEx(NULL, NULL, NULL, EVENT_ALL_ACCESS);
            sys->mp = mp;
            SetEvent(sys->hLock);

            libvlc_video_set_format(mp, "RGBA", sys->thumbWidth, sys->thumbHeight, pitch);
            libvlc_video_set_callbacks(mp, Lock, Unlock, NULL, (void*) sys);
            sys->state = THUMB_SEEKING;

            if (mp)
            {
                libvlc_media_player_play(mp);
                //TODO: Specify position
                libvlc_media_player_set_position(mp, SEEK_POSITION);
            }

            completionTask.then([mp](WriteableBitmap^ bmp){
                libvlc_media_player_stop(mp);
                libvlc_media_player_release(mp);
            });
        }

        delete [](mrl_str);
        return completionTask;
    });
}

Thumbnailer::~Thumbnailer()
{
    libvlc_release(p_instance);
}