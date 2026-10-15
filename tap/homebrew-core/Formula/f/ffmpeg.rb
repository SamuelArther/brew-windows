class Ffmpeg < Formula
  desc "Play, record, convert, and stream audio and video"
  homepage "https://ffmpeg.org/"
  url "https://github.com/GyanD/codexffmpeg/releases/download/9.0.2/ffmpeg-9.0.2-essentials_build.zip"
  sha256 "60f467265b1e312373dbcd92200c2618a74850f98d3d078e94296bb3fa2047ba"
  license "GPL-3.0-or-later"

  # GyanD also publishes dated git-master builds as releases; only track versioned ones.
  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
    strategy :github_releases
  end

  def install
    install_tree
    windows_exe "libexec/bin/ffmpeg.exe", "ffmpeg"
    windows_exe "libexec/bin/ffprobe.exe", "ffprobe"
    windows_exe "libexec/bin/ffplay.exe", "ffplay"
  end

  test do
    # Create a 5 second test MP4
    mp4out = testpath/"video.mp4"
    system bin/"ffmpeg", "-filter_complex", "testsrc=rate=1:duration=5", mp4out
    assert_path_exists mp4out, "Failed to create video.mp4!"
  end
end
