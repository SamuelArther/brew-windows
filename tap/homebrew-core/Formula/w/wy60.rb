class Wy60 < Formula
  desc "Wyse 60 compatible terminal emulator"
  homepage "https://code.google.com/archive/p/wy60/"
  url "https://storage.googleapis.com/google-code-archive-downloads/v2/code.google.com/wy60/wy60-2.0.9.tar.gz"
  sha256 "f7379404f0baf38faba48af7b05f9e0df65266ab75071b2ca56195b63fc05ed0"
  license "GPL-2.0-or-later"

  bottle do
    sha256 cellar: :any, x86_64_windows: "3256b7202b7edb2a80a0096d77f98b2776c3af35aad2803f94f362fdd58e4de0"
  end

  uses_from_macos "ncurses"

  def install
    system "./configure", "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    system bin/"wy60", "--version"
  end
end
