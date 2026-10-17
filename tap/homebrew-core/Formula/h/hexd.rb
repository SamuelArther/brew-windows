class Hexd < Formula
  desc "Colourful, human-friendly hexdump tool"
  homepage "https://github.com/FireyFly/hexd"
  url "https://github.com/FireyFly/hexd/archive/refs/tags/v1.1.0.tar.gz"
  sha256 "de0db7205c8eb0f170263aca27f5d48963855345bc79ba4842edd21a938d0326"
  license "MIT"

  # BSD install does not understand the GNU "-D" flag
  patch do
    url "https://github.com/FireyFly/hexd/commit/ebfa9b5f76b5c71eea4fc003ab031f83615cea9b.patch?full_index=1"
    sha256 "620b0186cf47a7736c3636017d17dccb13e358c7d030821989ae4d6006192a1c"
    type :backport
    resolves "https://github.com/FireyFly/hexd/commit/ebfa9b5f76b5c71eea4fc003ab031f83615cea9b"
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/hexd-1.1.0"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "c5a4f3e74f246acec8503c062bf7399cd4a8cc29a1b67fac20c3b2e7328c8085"
  end

  def install
    bin.mkdir
    man1.mkpath
    system "make", "install", "PREFIX=#{prefix}"
  end

  test do
    assert_match(/0000\s+48/, pipe_output("#{bin}/hexd -p", "H"))
  end
end
