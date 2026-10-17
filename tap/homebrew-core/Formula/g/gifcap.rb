class Gifcap < Formula
  desc "Capture video from an Android device and make a gif"
  homepage "https://github.com/outlook/gifcap"
  url "https://github.com/outlook/gifcap/archive/refs/tags/1.0.4.tar.gz"
  sha256 "32747a6cf77f7ea99380752ba35ecd929bb185167e5908cf910d2a92f05029ad"
  license "MIT"
  head "https://github.com/outlook/gifcap.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "6704d42ffe3e03ac2f47c27c3dff752b872368050b79b876e0e4b0c3d09e5518"
  end

  depends_on "ffmpeg"

  def install
    bin.install "gifcap"
  end

  test do
    assert_match(/^usage: gifcap/, shell_output("#{bin}/gifcap --help").strip)
  end
end
