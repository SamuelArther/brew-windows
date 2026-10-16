class Aria2 < Formula
  desc "Download with resuming and segmented downloading"
  homepage "https://aria2.github.io/"
  url "https://github.com/aria2/aria2/releases/download/release-1.37.0/aria2-1.37.0-win-64bit-build1.zip"
  version "1.37.0"
  sha256 "67d015301eef0b612191212d564c5bb0a14b5b9c4796b76454276a4d28d9b288"
  license "GPL-2.0-or-later"
  revision 3

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "aria2c.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/aria2c --version")
  end
end
