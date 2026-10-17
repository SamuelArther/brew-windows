class Convmv < Formula
  desc "Filename encoding conversion tool"
  homepage "https://www.j3e.de/linux/convmv/"
  url "https://www.j3e.de/linux/convmv/convmv-2.06.tar.gz"
  sha256 "a37192e266742e7fe33ec19a3be49aea7fd4d066887863a6e193fa345bf2e592"
  license "GPL-2.0-or-later"

  livecheck do
    url :homepage
    regex(/href=.*?convmv[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/convmv-2.06"
    sha256 cellar: :any, x86_64_windows: "53d9b301f86dbc8e07df3e647790d167a90c845ffda4ab8a3e8aafda79b733b6"
  end

  def install
    system "make", "install", "PREFIX=#{prefix}"
  end

  test do
    system bin/"convmv", "--list"
  end
end
