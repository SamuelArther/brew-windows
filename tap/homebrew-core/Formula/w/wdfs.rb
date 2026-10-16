class Wdfs < Formula
  desc "Webdav file system"
  homepage "https://noedler.de/projekte/wdfs/"
  url "https://noedler.de/projekte/wdfs/wdfs-1.4.2.tar.gz"
  sha256 "fcf2e1584568b07c7f3683a983a9be26fae6534b8109e09167e5dff9114ba2e5"
  license "GPL-2.0-or-later"
  revision 1

  # Also needs `libfuse@2` and had 0 installs in 90 days on deprecation date
  deprecate! date: "2025-03-06", because: :unmaintained
  disable! date: "2026-03-06", because: :unmaintained

  depends_on "pkgconf" => :build
  depends_on "glib"
  depends_on "libfuse@2"
  depends_on :linux # on macOS, requires closed-source macFUSE
  depends_on "neon"

  def install
    system "./configure", *std_configure_args
    system "make", "install"
  end

  test do
    system bin/"wdfs", "-v"
  end
end
