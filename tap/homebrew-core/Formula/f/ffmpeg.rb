class Ffmpeg < Formula
  desc "Play, record, convert, and stream audio and video"
  homepage "https://ffmpeg.org/"
  version "9.0.2"
  url "https://github.com/GyanD/codexffmpeg/releases/download/9.0.2/ffmpeg-9.0.2-essentials_build.zip"
  sha256 "60f467265b1e312373dbcd92200c2618a74850f98d3d078e94296bb3fa2047ba"
  license "GPL-3.0-or-later"

  def install
    install_tree
    windows_exe "libexec/bin/ffmpeg.exe", "ffmpeg"
    windows_exe "libexec/bin/ffprobe.exe", "ffprobe"
    windows_exe "libexec/bin/ffplay.exe", "ffplay"
  end
end
