class Getparty < Formula
  desc "Multi-part HTTP download manager"
  homepage "https://github.com/vbauerster/getparty"
  url "https://github.com/vbauerster/getparty/releases/download/v1.28.3/getparty_Windows_x86_64.zip"
  sha256 "18c26384295bbeb2fff2ebc7fe41ba5a709ed4effd1119f6941e6c6925725a94"
  license "BSD-3-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/vbauerster/getparty/archive/refs/tags/v1.28.3.tar.gz"
  end

  def install
    bin.install "getparty.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/getparty --version")

    output = shell_output("#{bin}/getparty http://media.vimcasts.org/videos/10/ascii_art.ogv")
    assert_match "\"ascii_art.ogv\" saved", output
  end
end
