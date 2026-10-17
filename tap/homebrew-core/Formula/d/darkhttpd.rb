class Darkhttpd < Formula
  desc "Small static webserver without CGI"
  homepage "https://unix4lyfe.org/darkhttpd/"
  url "https://github.com/emikulic/darkhttpd/archive/refs/tags/v1.17.tar.gz"
  sha256 "4fee9927e2d8bb0a302f0dd62f9ff1e075748fa9f5162c9481a7a58b41462b56"
  license "ISC"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/darkhttpd-1.17"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "9e1286d406fa11224c2f1b4c9f6f8305c0c8f3a03fdf3e664e3448cf0edebe30"
  end

  def install
    system "make"
    bin.install "darkhttpd"
  end

  test do
    system bin/"darkhttpd", "--help"
  end
end
