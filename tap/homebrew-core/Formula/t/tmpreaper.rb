class Tmpreaper < Formula
  desc "Clean up files in directories based on their age"
  homepage "https://packages.debian.org/sid/tmpreaper"
  url "https://ftp.debian.org/debian/pool/main/t/tmpreaper/tmpreaper_1.6.18.tar.xz"
  sha256 "2ae7de0775b49abd222d09ad71360d795b40aa7c31253363e64c182966a37c00"
  license "GPL-2.0-only"

  livecheck do
    url "https://deb.debian.org/debian/pool/main/t/tmpreaper/"
    regex(/href=.*?tmpreaper[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "387da6619adfdd6fb7ecd3a6e1ac2d45d3b39f9383de122d7015002f020c11ae"
  end

  depends_on "autoconf" => :build
  depends_on "automake" => :build

  on_linux do
    depends_on "e2fsprogs"
    depends_on "util-linux"
  end

  deny_network_access!

  def install
    system "./configure", "--prefix=#{prefix}", "--sysconfdir=#{etc}"
    system "make", "install"
  end

  test do
    touch "removed"
    sleep 3
    touch "not-removed"
    system sbin/"tmpreaper", "2s", "."
    refute_path_exists testpath/"removed"
    assert_path_exists testpath/"not-removed"
  end
end
