class AlsaLib < Formula
  desc "Audio I/O library for Windows (PortAudio over WASAPI, WDM-KS, DirectSound and MME)"
  homepage "https://www.portaudio.com"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-portaudio-1~19.7.0-7-any.pkg.tar.zst"
  version "19.7.0"
  sha256 "085e0ba89a659e6f66379552f7fb736b182ecaa7caf710d611e481f557e20c38"
  license "MIT"

  # ALSA is the Linux kernel's sound driver interface, and alsa-lib (libasound) is its user-space
  # API: PCM playback/capture, mixer controls, MIDI sequencing. On Windows those go through the
  # Windows audio stack (WASAPI, kernel streaming, DirectSound, MME), and the portable C library
  # programs use to reach it is PortAudio (which also drives ALSA on Linux). This keg carries
  # MSYS2's UCRT64 PortAudio with all Windows host APIs: headers (portaudio.h, pa_win_wasapi.h ...),
  # the import library, libportaudio.dll and its pkg-config file. MIDI on Windows is the WinMM
  # midiIn/midiOut API in the system SDK headers.

  keg_only "it is the same library as portaudio"

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build a test program; check the audio API instead.
    assert_match "Pa_OpenStream", (include/"portaudio.h").read
    assert_path_exists include/"pa_win_wasapi.h"
    assert_path_exists lib/"libportaudio.dll.a"
    assert_path_exists bin/"libportaudio.dll"
  end
end
