class Winetricks < Formula
  desc "Automatic workarounds for problems in Wine"
  homepage "https://github.com/Winetricks/winetricks"
  url "https://github.com/Winetricks/winetricks/archive/refs/tags/20260125.tar.gz"
  sha256 "2890bd9fbbade4638e58b4999a237273192df03b58516ae7b8771e09c22d2f56"
  license "LGPL-2.1-or-later"
  head "https://github.com/Winetricks/winetricks.git", branch: "master"

  livecheck do
    url :stable
    regex(/^v?(\d{6,8})$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "0220f43d3f01b22b48206056235ab4c49c00f4fe97c76b00379ed9e4cdf9d7c4"
  end

  depends_on "cabextract"
  depends_on "p7zip"

  uses_from_macos "unzip"

  def install
    bin.install "src/winetricks"
    man1.install "src/winetricks.1"
  end

  test do
    system bin/"winetricks", "--version"
  end
end
